using MediatR;

namespace Azure.Functions.Worker.Extensions.MediatR.Middlewares;

public class ExceptionPipeline<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        try
        {
            return await next();
        }
        catch (Exception e)
        {
            throw new RequestHandlerException(e, request);
        }
    }
}
