using System.Net;
using Azure.Functions.Worker.Extensions.MediatR.OpenApi;
using FluentValidation;
using Microsoft.Azure.Functions.Worker.Http;
using Newtonsoft.Json;

namespace Azure.Functions.Worker.Extensions.MediatR.ExceptionHandling;

public class RequestValidationExceptionHandler(JsonSerializerSettings jsonSerializerSettings) : IHttpExceptionHandler
{
    public async Task<HttpResponseData?> CreateResponseFromException<TCommand>(TCommand command, Exception ex, IHttpResponseDataBuilder responseBuilder, CancellationToken cancellationToken)
    {
        if (ex is not ValidationException validationException)
        {
            return null;
        }
        
        var newHttpResponse = responseBuilder.CreateResponse();
        
        newHttpResponse.StatusCode = HttpStatusCode.BadRequest;
        newHttpResponse.Headers.Add("Content-Type", "application/json");

        var errorResult = new ValidationErrors
        {
            Errors = validationException.Errors.Select(x => new Error
            {
                ErrorCode = x.ErrorCode,
                Message = x.ErrorMessage,
                Property = x.PropertyName
            }).ToArray()
        };
                
        var errorContent = JsonConvert.SerializeObject(errorResult, jsonSerializerSettings);

        await newHttpResponse.WriteStringAsync(errorContent, cancellationToken);

        return newHttpResponse;
    }
}
