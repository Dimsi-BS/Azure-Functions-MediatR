using FluentValidation;
using MediatR;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Azure.Functions.Worker.Extensions.MediatR.Extensions;

namespace Azure.Functions.Worker.Extensions.MediatR.Middlewares;

public class RequestValidationMiddleware(ILogger<RequestValidationMiddleware> logger): IFunctionsWorkerMiddleware
{
    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var requestParameter = GetRequestFunctionParameter(context);
        
        if (requestParameter != null)
        {
            object? requestObj = null;
            var requestType = requestParameter.Type;
            try
            { 
                requestObj = await context.GetRequestObjectAsync(requestType);
            }
            catch (ArgumentException ex) when (ex.Message.Contains("HttpContext"))
            {
                await next(context);
            }

            // Get the validator for this request type
            var validatorType = typeof(IValidator<>).MakeGenericType(requestType);

            if (context.InstanceServices.GetService(validatorType) is IValidator validator)
            {
                var validationContext = new ValidationContext<object>(requestObj!);
                var validationResult = await validator.ValidateAsync(validationContext, context.CancellationToken);

                if (!validationResult.IsValid)
                {
                    var validationException = new ValidationException(validationResult.Errors);

                    logger.LogError(validationException,
                        $"Validation Failed for request {requestType}:{Environment.NewLine} {JsonConvert.SerializeObject(requestObj)}");

                    throw new RequestHandlerException(validationException, requestObj);
                }
            }
        }

        await next(context);
    }

    private FunctionParameter? GetRequestFunctionParameter(FunctionContext context)
    {
        var requestParameter =
            context.FunctionDefinition.Parameters.FirstOrDefault(p => typeof(IBaseRequest).IsAssignableFrom(p.Type));

        if (requestParameter?.Properties.Any(p => p.Value is HttpTriggerAttribute) ?? false)
        {
            return requestParameter;
        }

        return null;
    }


}
