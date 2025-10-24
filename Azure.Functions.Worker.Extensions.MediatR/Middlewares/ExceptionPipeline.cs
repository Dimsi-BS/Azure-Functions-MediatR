using MediatR;

namespace Azure.Functions.Worker.Extensions.MediatR.Middlewares;

public class ExceptionPipeline<TRequest, TResponse> 
    : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        try
        {
            return await next(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            throw new RequestHandlerException(e, request);
        }
    }
}
