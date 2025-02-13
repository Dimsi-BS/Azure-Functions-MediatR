namespace Azure.Functions.Worker.Extensions.MediatR.Middlewares;

public class RequestHandlerException : Exception
{
    public RequestHandlerException(Exception exception, object request) : base("An error occurred while processing the request", exception)
    {
        Request = request;
    }

    public object Request { get; }
}
