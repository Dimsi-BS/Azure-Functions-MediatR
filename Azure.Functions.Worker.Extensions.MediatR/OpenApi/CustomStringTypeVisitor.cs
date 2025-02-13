using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Visitors;

namespace Azure.Functions.Worker.Extensions.MediatR.OpenApi;

public class CustomStringTypeVisitor(
    VisitorCollection visitorCollection
)
    : StringTypeVisitor(visitorCollection)
{
    public override bool IsVisitable(Type type)
        => base.IsVisitable(type) && !type.IsStringType();
}

public static class TypeExtensions
{
    public static bool IsStringType(this Type type)
    {
        return type == typeof(string);
    }
}
