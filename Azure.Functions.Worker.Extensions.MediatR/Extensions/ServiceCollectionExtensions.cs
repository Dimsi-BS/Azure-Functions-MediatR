using Azure.Core.Serialization;
using Azure.Functions.Worker.Extensions.MediatR.OpenApi;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.OpenApi.Extensions;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Abstractions;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Configurations;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Visitors;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Azure.Functions.Worker.Extensions.MediatR.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection RegisterCustomOpenApiProviders(this IServiceCollection serviceCollection)
    {   
        serviceCollection.RemoveAll<IOpenApiHttpTriggerContext>();
        serviceCollection.AddSingleton<IOpenApiHttpTriggerContext, CustomOpenApiHttpTriggerContext>();
        serviceCollection.AddSingleton<IOpenApiInfoSetter>(
            sp => sp.GetRequiredService<CustomOpenApiHttpTriggerContext>());
        
        
        serviceCollection.AddScoped<IDocumentHelper, CustomDocumentHelper>();
        serviceCollection.AddScoped<RouteConstraintFilter>();
        serviceCollection.AddScoped<IOpenApiSchemaAcceptor, OpenApiSchemaAcceptor>();

        return serviceCollection;
    }
    
    public static IFunctionsWorkerApplicationBuilder RegisterNewtonSoftJson(this IFunctionsWorkerApplicationBuilder hostBuilder)
    {
        var serializationSettings = NewtonsoftJsonObjectSerializer.CreateJsonSerializerSettings();

        hostBuilder.UseNewtonsoftJson(serializationSettings);
        hostBuilder.Services.AddSingleton(serializationSettings);
        return hostBuilder;
    }
}
