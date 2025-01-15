using System.Reflection;
using Microsoft.AspNetCore.Diagnostics;

namespace Azure.Functions.Worker.Extensions.MediatR.Configuration;

public class OptionsBuilder
{
    private readonly ConfigurationOptions _configurationOptions;

    internal OptionsBuilder(ConfigurationOptions configurationOptions)
    {
        _configurationOptions = configurationOptions;
    }

    public OptionsBuilder RegisterMediatRServicesFromAssemblyContaining<T>()
    {
        _configurationOptions.MediatRAssemblies.Add(typeof(T).Assembly);
        return this;
    }

    public OptionsBuilder RegisterMediatRServicesFromAssemblies(params Assembly[] assemblies)
    {
        _configurationOptions.MediatRAssemblies.AddRange(assemblies);
        return this;
    }
    
    public OptionsBuilder AddFluentValidation(params Assembly[] assemblies)
    {
        _configurationOptions.FluentValidationAssemblies.AddRange(assemblies);
        return this;
    }
    
    public OptionsBuilder RegisterExceptionHandler<TExceptionHandler>()
        where TExceptionHandler : class, IExceptionHandler
    {
        _configurationOptions.ExceptionHandlerTypes.Add(typeof(TExceptionHandler));
        return this;
    }
    
    public OptionsBuilder OpenApiInfos(Action<IOpenApiInfoBuilder> configureOpenApiInfos)
    {
        var openApiInfoBuilder = new OpenApiInfoBuilder(_configurationOptions.OpenApiInfos);
        configureOpenApiInfos(openApiInfoBuilder);
        
        return this;
    }
}
