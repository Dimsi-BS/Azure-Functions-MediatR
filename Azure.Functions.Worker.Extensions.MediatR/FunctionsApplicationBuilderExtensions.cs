using Azure.Functions.Worker.Extensions.HttpApi.Config;
using Azure.Functions.Worker.Extensions.MediatR.Configuration;
using Azure.Functions.Worker.Extensions.MediatR.ExceptionHandling;
using Azure.Functions.Worker.Extensions.MediatR.Extensions;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global

// ReSharper disable once CheckNamespace
namespace Microsoft.Azure.Functions.Worker.Builder;

public static class FunctionsApplicationBuilderExtensions
{   
    public static IFunctionsWorkerApplicationBuilder UseMediatR(this IFunctionsWorkerApplicationBuilder builder,
        Action<IOptionsBuilder>? configureOptions = null)
    {
        var configurationOptions = new ConfigurationOptions();
        var optionsBuilder = new OptionsBuilder(configurationOptions);

        configureOptions?.Invoke(optionsBuilder);

        builder.AddMediatRAndRequestValidation(configurationOptions);

        foreach (var exceptionHandler in configurationOptions.ExceptionHandlerTypes)
        {
            builder.Services.AddTransient(typeof(IHttpExceptionHandler), exceptionHandler);
        }
        
        builder.Services.RegisterCustomOpenApiProviders();
        builder.RegisterNewtonSoftJson(configurationOptions.JsonSerializerSettings);
        
        builder.AddHttpApi();

        return builder;
    }
}
