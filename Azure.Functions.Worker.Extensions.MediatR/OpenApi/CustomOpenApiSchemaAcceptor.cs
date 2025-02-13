using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Abstractions;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Visitors;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Azure.Functions.Worker.Extensions.MediatR.OpenApi;

public class CustomOpenApiSchemaAcceptor : OpenApiSchemaAcceptor
{
    /// <inheritdoc />
    public void Accept(VisitorCollection collection, NamingStrategy namingStrategy)
    {
        if (Properties.Any())
        {
            foreach (KeyValuePair<string, PropertyInfo> property in Properties)
            {
                List<Attribute> attributeList = new List<Attribute>()
                {
                    property.Value.GetCustomAttribute<OpenApiSchemaVisibilityAttribute>(false)!,
                    property.Value.GetCustomAttribute<OpenApiPropertyAttribute>(false)!
                };
                attributeList.AddRange(property.Value.GetCustomAttributes<ValidationAttribute>(false));
                attributeList.AddRange(property.Value.GetCustomAttributes<JsonPropertyAttribute>(false));
                foreach (IVisitor visitor in collection.Visitors)
                {
                    if (!visitor.IsVisitable(property.Value.PropertyType)) continue;
                    
                    KeyValuePair<string, Type> type = new KeyValuePair<string, Type>(property.Key, property.Value.PropertyType);
                    visitor.Visit(this, type, namingStrategy, attributeList.ToArray());
                }
            }
        }
        else
        {
            foreach (KeyValuePair<string, Type> type in Types)
            {
                foreach (IVisitor visitor in collection.Visitors)
                {
                    if (!visitor.IsVisitable(type.Value)) continue;
                    
                    visitor.Visit(this, type, namingStrategy);
                }
            }
        }
    }

    /// <inheritdoc />
    public Dictionary<string, OpenApiSchema> RootSchemas { get; set; } = new();

    /// <inheritdoc />
    public Dictionary<string, OpenApiSchema> Schemas { get; set; } = new();

    /// <inheritdoc />
    public Dictionary<string, Type> Types { get; set; } = new();
    
    public Dictionary<string, PropertyInfo> Properties { get; set; } = new();
}
