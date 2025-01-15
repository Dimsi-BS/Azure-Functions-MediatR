using Azure.Functions.Worker.Extensions.MediatR.Configuration;
using Azure.Functions.Worker.Extensions.MediatR.Middlewares;
using Azure.Functions.Worker.Extensions.MediatR.OpenApi;
using MediatR.Extensions.FluentValidation.AspNetCore;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Azure.Functions.Worker.Extensions.MediatR.Extensions;

public static class FunctionsWorkerApplicationBuilderExtensions
{
    internal static IFunctionsWorkerApplicationBuilder AddMediatRAndRequestValidation(this IFunctionsWorkerApplicationBuilder builder, ConfigurationOptions configurationOptions)
    {
        
        builder.Services.AddMediatR(options =>
        {
            options.RegisterServicesFromAssemblies(configurationOptions.MediatRAssemblies.Any()
                ? configurationOptions.MediatRAssemblies.ToArray()
                : AppDomain.CurrentDomain.GetAssemblies());
        });
        
        builder.Services.Configure<WorkerOptions>(options =>
        {
            options.InputConverters.RegisterAt<RequestInputConvertor>(0);
        });

        builder.Services.AddFluentValidation(configurationOptions.FluentValidationAssemblies.Any()
            ? configurationOptions.FluentValidationAssemblies
            : AppDomain.CurrentDomain.GetAssemblies());
        
        builder.UseWhen<RequestsValidationMiddleware>(context =>
            context.FunctionDefinition.InputBindings.Any(b => b.Value.Type == "httpTrigger"));
        return builder;
    }
}
