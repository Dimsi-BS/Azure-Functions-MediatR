using Azure.Functions.Worker.Extensions.MediatR.Configuration;
using Azure.Functions.Worker.Extensions.MediatR.Middlewares;
using Azure.Functions.Worker.Extensions.MediatR.OpenApi;
using FluentValidation;
using MediatR;
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

        builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ExceptionPipeline<,>));
        
        builder.Services.Configure<WorkerOptions>(options =>
        {
            options.InputConverters.RegisterAt<RequestInputConvertor>(0);
        });
        
        builder.Services.AddValidatorsFromAssemblies(configurationOptions.FluentValidationAssemblies.Any()
            ? configurationOptions.FluentValidationAssemblies
            : AppDomain.CurrentDomain.GetAssemblies());
        
        builder.UseWhen<HttpExceptionHandlingMiddleware>(context =>
            context.FunctionDefinition.InputBindings.Any(b => b.Value.Type == "httpTrigger"));
        
        if (configurationOptions.ValidateOnlyHttpTriggerRequest)
        {
             builder.UseWhen<RequestValidationMiddleware>(context =>
                 context.FunctionDefinition.InputBindings.Any(b => b.Value.Type == "httpTrigger"));
        }
        else
        {
            builder.Services.Add(new ServiceDescriptor(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>), ServiceLifetime.Transient));
        }

        return builder;
    }
}
