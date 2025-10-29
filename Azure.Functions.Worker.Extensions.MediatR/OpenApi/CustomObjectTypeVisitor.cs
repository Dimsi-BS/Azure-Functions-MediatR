using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Abstractions;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Extensions;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Visitors;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Azure.Functions.Worker.Extensions.MediatR.OpenApi;

public class CustomObjectTypeVisitor(
    VisitorCollection visitorCollection
)
    : ObjectTypeVisitor(visitorCollection)
{
    private readonly HashSet<Type> _noVisitableTypes = [
        typeof(Guid),
        typeof(DateTime),
        typeof(TimeSpan),
        typeof(DateTimeOffset),
        typeof(Uri),
        typeof(Type),
        typeof(object),
        typeof(byte[])
    ];

    private readonly HashSet<string> _noAddedKeys =
    [
        "OBJECT",
        "JTOKEN",
        "JOBJECT",
        "JARRAY"
    ];

    public override void Visit(IAcceptor acceptor, KeyValuePair<string, Type> type, NamingStrategy namingStrategy,
        params Attribute[] attributes)
    {
        var newAcceptor = new OpenApiSchemaAcceptor(); // CustomOpenApiSchemaAcceptor();
        base.Visit(newAcceptor, type, namingStrategy, attributes);

        string str1;
        if (!type.Value.IsGenericType)
            str1 = namingStrategy.GetPropertyName(type.Value.Name, false);
        else
            str1 = namingStrategy.GetPropertyName(type.Value.Name.Split('`').First(), false) + "_" + string.Join("_",
                type.Value.GenericTypeArguments.Select((Func<Type, string>)(a =>
                    namingStrategy.GetPropertyName(a.Name, false))));
        var title = str1;
        var str2 = Visit(acceptor, type.Key, title, "object", null, attributes);
        if (str2.IsNullOrWhiteSpace() || !IsNavigatable(type.Value))
            return;
        var instance = acceptor as OpenApiSchemaAcceptor;
        if (instance!.IsNullOrDefault())
            return;
        
        var isOptIn = type.Value.GetCustomAttribute<JsonObjectAttribute>(false)
            ?.MemberSerialization == MemberSerialization.OptIn;
        
        var dictionary = type.Value
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(p => !p.ExistsCustomAttribute<JsonIgnoreAttribute>() && 
                        (!isOptIn || p.ExistsCustomAttribute<JsonPropertyAttribute>()) &&
                        !p.ExistsCustomAttribute<FromQueryAttribute>() &&
                        !p.ExistsCustomAttribute<FromRouteAttribute>())
            .ToDictionary(p => p.GetJsonPropertyName(namingStrategy), p => p);
        ProcessProperties(instance!, str2, dictionary, namingStrategy);
        var openApiReference = new OpenApiReference
        {
            Type = ReferenceType.Schema,
            Id = type.Value.GetOpenApiReferenceId(false, false, namingStrategy)
        };
        instance!.Schemas[str2].Reference = openApiReference;

        if (newAcceptor.Schemas.TryGetValue(str2, out var schema))
        {
            instance.Schemas[str2].Example = schema.Example;
        }
    }

    private void ProcessProperties(
        IOpenApiSchemaAcceptor instance,
        string schemaName,
        Dictionary<string, PropertyInfo> properties,
        NamingStrategy namingStrategy)
    {
        var dictionary1 = new Dictionary<string, OpenApiSchema>();
        var apiSchemaAcceptor = new OpenApiSchemaAcceptor
        {
            Properties = properties,
            RootSchemas = instance.RootSchemas,
            Schemas = dictionary1
        };
        apiSchemaAcceptor.Accept(VisitorCollection, namingStrategy);
        foreach (var keyValuePair in properties
                     .Where(p => !p.Value.GetCustomAttribute<JsonPropertyAttribute>(false)
                         .IsNullOrDefault<JsonPropertyAttribute>())
                     .Select(p =>
                         new KeyValuePair<string, JsonPropertyAttribute>(p.Key,
                             p.Value.GetCustomAttribute<JsonPropertyAttribute>(false)))
                     .Where(p => p.Value.Required == Required.Always || p.Value.Required == Required.AllowNull))
            instance.Schemas[schemaName].Required.Add(keyValuePair.Key);
        foreach (var keyValuePair in properties
                     .Where(p => !p.Value.GetCustomAttribute<JsonRequiredAttribute>(false)
                         .IsNullOrDefault<JsonRequiredAttribute>())
                     .Select(p =>
                         new KeyValuePair<string, JsonRequiredAttribute>(p.Key,
                             p.Value.GetCustomAttribute<JsonRequiredAttribute>(false))))
        {
            var propertyName = namingStrategy.GetPropertyName(keyValuePair.Key, false);
            if (!instance.Schemas[schemaName].Required.Contains(propertyName))
                instance.Schemas[schemaName].Required.Add(propertyName);
        }

        foreach (var name in properties
                     .Where(p => !p.Value.GetCustomAttribute<RequiredAttribute>(false)
                         .IsNullOrDefault<RequiredAttribute>())
                     .Select(p => p.Key))
        {
            var propertyName = namingStrategy.GetPropertyName(name, false); 
            instance.Schemas[schemaName].Required.Add(propertyName);
        }

        instance.Schemas[schemaName].Properties = apiSchemaAcceptor.Schemas;
        foreach (var keyValuePair in apiSchemaAcceptor.Schemas
                     .Where(
                         (Func<KeyValuePair<string, OpenApiSchema>, bool>)(p =>
                             !instance.Schemas.Keys.Contains<string>(p.Key)))
                     .Where(
                         (Func<KeyValuePair<string, OpenApiSchema>, bool>)(p => p.Value.IsOpenApiSchemaObject()))
                     .GroupBy((Func<KeyValuePair<string, OpenApiSchema>, string>)(p => p.Value.Title))
                     .Select(
                         (Func<IGrouping<string, KeyValuePair<string, OpenApiSchema>>,
                             KeyValuePair<string, OpenApiSchema>>)(p => p.First<KeyValuePair<string, OpenApiSchema>>()))
                     .ToDictionary(
                         (Func<KeyValuePair<string, OpenApiSchema>, string>)(p => p.Value.Title),
                         (Func<KeyValuePair<string, OpenApiSchema>, OpenApiSchema>)(p => p.Value))
                     .Where(
                         (Func<KeyValuePair<string, OpenApiSchema>, bool>)(p =>
                             !_noAddedKeys.Contains(p.Key.ToUpperInvariant()))))
        {
            if (!instance.RootSchemas.ContainsKey(keyValuePair.Key))
                instance.RootSchemas.Add(keyValuePair.Key, keyValuePair.Value);
        }

        IDictionary<string, OpenApiSchema> dictionary2 = instance.Schemas[schemaName].Properties
            .Select(
                (Func<KeyValuePair<string, OpenApiSchema>, KeyValuePair<string, OpenApiSchema>>)(p =>
                {
                    p.Value.Title = null;
                    return new KeyValuePair<string, OpenApiSchema>(p.Key, p.Value);
                })).ToDictionary(
                (Func<KeyValuePair<string, OpenApiSchema>, string>)(p => p.Key),
                (Func<KeyValuePair<string, OpenApiSchema>, OpenApiSchema>)(p => p.Value));
        instance.Schemas[schemaName].Properties = dictionary2;
    }
}
