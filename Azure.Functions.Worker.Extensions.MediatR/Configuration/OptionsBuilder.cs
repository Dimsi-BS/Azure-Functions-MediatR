using System.Reflection;
using Azure.Functions.Worker.Extensions.MediatR.ExceptionHandling;
using Microsoft.AspNetCore.Diagnostics;

namespace Azure.Functions.Worker.Extensions.MediatR.Configuration;

public interface IOptionsBuilder
{
    IOptionsBuilder RegisterMediatRServicesFromAssemblyContaining<T>();
    IOptionsBuilder RegisterMediatRServicesFromAssemblies(params Assembly[] assemblies);
    IOptionsBuilder AddFluentValidation(params Assembly[] assemblies);

    IOptionsBuilder RegisterHttpExceptionHandler<TExceptionHandler>() where TExceptionHandler : class, IHttpExceptionHandler;
    
    IOptionsBuilder OpenApiInfos(Action<IOpenApiInfoBuilder> configureOpenApiInfos);
}

internal class OptionsBuilder(ConfigurationOptions configurationOptions) : IOptionsBuilder
{
    public IOptionsBuilder RegisterMediatRServicesFromAssemblyContaining<T>()
    {
        configurationOptions.MediatRAssemblies.Add(typeof(T).Assembly);
        return this;
    }

    public IOptionsBuilder RegisterMediatRServicesFromAssemblies(params Assembly[] assemblies)
    {
        configurationOptions.MediatRAssemblies.AddRange(assemblies);
        return this;
    }
    
    public IOptionsBuilder AddFluentValidation(params Assembly[] assemblies)
    {
        configurationOptions.FluentValidationAssemblies.AddRange(assemblies);
        return this;
    }
    
    public IOptionsBuilder RegisterHttpExceptionHandler<TExceptionHandler>() 
        where TExceptionHandler : class, IHttpExceptionHandler
    {
        configurationOptions.ExceptionHandlerTypes.Add(typeof(TExceptionHandler));
        return this;
    }
    
    public IOptionsBuilder OpenApiInfos(Action<IOpenApiInfoBuilder> configureOpenApiInfos)
    {
        var openApiInfoBuilder = new OpenApiInfoBuilder(configurationOptions.OpenApiInfos);
        configureOpenApiInfos(openApiInfoBuilder);
        
        return this;
    }
}
