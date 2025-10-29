using MediatR;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Azure.Functions.Worker.Extensions.MediatR.Middlewares;

public class RequestValidationMiddleware(ILogger<RequestValidationMiddleware> logger): IFunctionsWorkerMiddleware
{
    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var requestParameter = GetRequestFunctionParameter(context);

        if (requestParameter != null)
        {
            var requestType = requestParameter.Type;

            var requestObj = ConvertBindingDataToRequest(context.BindingContext.BindingData, requestType);

            // Get the validator for this request type
            var validatorType = typeof(IValidator<>).MakeGenericType(requestType);

            if (context.InstanceServices.GetService(validatorType) is IValidator validator)
            {
                var validationContext = new ValidationContext<object>(requestObj!);
                var validationResult = await validator.ValidateAsync(validationContext, context.CancellationToken);

                if (!validationResult.IsValid)
                {
                    var validationException = new ValidationException(validationResult.Errors);
                    
                    logger.LogError(validationException, $"Validation Failed for request {requestType}:{Environment.NewLine} {JsonConvert.SerializeObject(requestObj)}");
                    
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


        private static object? ConvertBindingDataToRequest(IReadOnlyDictionary<string, object?> bindingData, Type requestType)
    {
        try
        {
            // First, preprocess the binding data to deserialize any nested JSON strings
            var processedData = new Dictionary<string, object?>();
            
            foreach (var kvp in bindingData)
            {
                if (kvp.Value is string stringValue && !string.IsNullOrWhiteSpace(stringValue))
                {
                    // Try to parse as JSON if it looks like JSON (starts with { or [)
                    stringValue = stringValue.Trim();
                    if ((stringValue.StartsWith("{") && stringValue.EndsWith("}")) || 
                        (stringValue.StartsWith("[") && stringValue.EndsWith("]")))
                    {
                        try
                        {
                            // Deserialize the JSON string to a JToken (could be JObject or JArray)
                            processedData[kvp.Key] = JsonConvert.DeserializeObject(stringValue);
                        }
                        catch
                        {
                            // If deserialization fails, keep the original string value
                            processedData[kvp.Key] = kvp.Value;
                        }
                    }
                    else
                    {
                        processedData[kvp.Key] = kvp.Value;
                    }
                }
                else
                {
                    processedData[kvp.Key] = kvp.Value;
                }
            }

            // Convert the processed binding data dictionary to a JObject
            var jObject = JObject.FromObject(processedData);

            // Deserialize the JObject to the target request type
            return jObject.ToObject(requestType);
        }
        catch
        {
            // If direct conversion fails, try to find the request in the binding data
            // Sometimes the request object is nested under a specific key
            foreach (var kvp in bindingData)
            {
                if (kvp.Value != null && requestType.IsAssignableFrom(kvp.Value.GetType()))
                {
                    return kvp.Value;
                }

                // Try to deserialize if the value is a string (JSON)
                if (kvp.Value is string jsonString)
                {
                    try
                    {
                        return JsonConvert.DeserializeObject(jsonString, requestType);
                    }
                    catch
                    {
                        /* Continue to next iteration */
                    }
                }
            }

            return null;
        }
    }
}
