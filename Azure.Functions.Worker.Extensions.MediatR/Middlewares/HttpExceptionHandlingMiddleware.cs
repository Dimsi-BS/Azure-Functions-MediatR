using System.Net;
using Azure.Functions.Worker.Extensions.MediatR.ExceptionHandling;
using Azure.Functions.Worker.Extensions.MediatR.OpenApi;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using Newtonsoft.Json;

namespace Azure.Functions.Worker.Extensions.MediatR.Middlewares;

public class HttpExceptionHandlingMiddleware(
    JsonSerializerSettings jsonSerializerSettings, 
    IEnumerable<IHttpExceptionHandler> handlers)
    : IFunctionsWorkerMiddleware
{
    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (RequestHandlerException ex)
        {
            var httpReqData = await context.GetHttpRequestDataAsync();

            if (httpReqData != null)
            {
                var httpResponseDataBuilder = new HttpResponseDataBuilder(httpReqData);
                
                foreach (var handler in handlers)
                {
                    var response = await handler.CreateResponseFromException(ex.Request, ex.InnerException, httpResponseDataBuilder, context.CancellationToken);
                    if (response is not null)
                    {
                        SetResponse(context, response);
                        return;
                    }
                }
            }
        }
    }

    private sealed class HttpResponseDataBuilder(HttpRequestData httpRequestData) : IHttpResponseDataBuilder
    {
        public HttpResponseData CreateResponse()
            => httpRequestData.CreateResponse();
    }
    
    private static void SetResponse(FunctionContext context, HttpResponseData newHttpResponse)
    {
        var invocationResult = context.GetInvocationResult();

        var httpOutputBindingFromMultipleOutputBindings = GetHttpOutputBindingFromMultipleOutputBinding(context);
        if (httpOutputBindingFromMultipleOutputBindings is not null)
        {
            httpOutputBindingFromMultipleOutputBindings.Value = newHttpResponse;
        }
        else
        {
            invocationResult.Value = newHttpResponse;
        }
    }
    
    private static OutputBindingData<HttpResponseData>? GetHttpOutputBindingFromMultipleOutputBinding(FunctionContext context)
    {
        // The output binding entry name will be "$return" only when the function return type is HttpResponseData
        var httpOutputBinding = context.GetOutputBindings<HttpResponseData>()
            .FirstOrDefault(b => b.BindingType == "http" && b.Name != "$return");

        return httpOutputBinding;
    }
}
