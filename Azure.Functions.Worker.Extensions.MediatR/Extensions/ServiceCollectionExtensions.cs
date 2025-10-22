using Azure.Core.Serialization;
using Azure.Functions.Worker.Extensions.MediatR.OpenApi;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.OpenApi;
using Microsoft.Azure.Functions.Worker.Extensions.OpenApi.Extensions;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Abstractions;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Configurations;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Visitors;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Newtonsoft.Json;

namespace Azure.Functions.Worker.Extensions.MediatR.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection RegisterCustomOpenApiProviders(this IServiceCollection serviceCollection)
    {
        var iOpenApiTriggerFunctionDescriptor = serviceCollection.FirstOrDefault(d => d.ServiceType == typeof(IOpenApiTriggerFunction));
        if (iOpenApiTriggerFunctionDescriptor != null)
        {
            serviceCollection.Remove(iOpenApiTriggerFunctionDescriptor);
            
            var scopedDescriptor = new ServiceDescriptor(iOpenApiTriggerFunctionDescriptor.ServiceType, iOpenApiTriggerFunctionDescriptor.ImplementationType!, ServiceLifetime.Scoped);
            
            serviceCollection.Add(scopedDescriptor);
        }
        
        serviceCollection.RemoveAll<IOpenApiHttpTriggerContext>();
        serviceCollection.AddScoped<IOpenApiHttpTriggerContext, CustomOpenApiHttpTriggerContext>();
        serviceCollection.AddScoped<IOpenApiInfoSetter>(
            sp => sp.GetRequiredService<CustomOpenApiHttpTriggerContext>());
        
        serviceCollection.AddScoped<IDocumentHelper, CustomDocumentHelper>();
        serviceCollection.AddScoped<RouteConstraintFilter>();
        serviceCollection.AddScoped<IOpenApiSchemaAcceptor, CustomOpenApiSchemaAcceptor>();

        return serviceCollection;
    }
    
    public static IFunctionsWorkerApplicationBuilder RegisterNewtonSoftJson(
        this IFunctionsWorkerApplicationBuilder hostBuilder,
        JsonSerializerSettings? configurationOptionsJsonSerializerSettings = null)
    {
        var serializationSettings = configurationOptionsJsonSerializerSettings ?? NewtonsoftJsonObjectSerializer.CreateJsonSerializerSettings();

        hostBuilder.UseNewtonsoftJson(serializationSettings);
        hostBuilder.Services.AddSingleton(serializationSettings);
        return hostBuilder;
    }
}
