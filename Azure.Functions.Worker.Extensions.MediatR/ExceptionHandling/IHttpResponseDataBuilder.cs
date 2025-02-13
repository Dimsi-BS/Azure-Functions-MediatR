using Microsoft.Azure.Functions.Worker.Http;

namespace Azure.Functions.Worker.Extensions.MediatR.ExceptionHandling;

public interface IHttpResponseDataBuilder
{
    HttpResponseData CreateResponse();
}
