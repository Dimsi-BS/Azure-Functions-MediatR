using Microsoft.Azure.Functions.Worker.Http;

namespace Azure.Functions.Worker.Extensions.MediatR.ExceptionHandling;

public interface IHttpExceptionHandler
{
    Task<HttpResponseData?> CreateResponseFromException<TRequest>(TRequest command, Exception ex, IHttpResponseDataBuilder responseBuilder, CancellationToken cancellationToken);
}
