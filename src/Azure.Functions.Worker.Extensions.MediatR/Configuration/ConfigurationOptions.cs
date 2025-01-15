using System.Reflection;
using Microsoft.OpenApi.Models;

namespace Azure.Functions.Worker.Extensions.MediatR.Configuration;

internal class ConfigurationOptions
{
    public ICollection<Assembly> MediatRAssemblies { get; } = new List<Assembly>();

    public ICollection<Assembly> FluentValidationAssemblies { get; } = new List<Assembly>();

    public OpenApiInfo OpenApiInfos { get; } = new OpenApiInfo();

    public ICollection<Type> ExceptionHandlerTypes { get; } = new List<Type>();
}
