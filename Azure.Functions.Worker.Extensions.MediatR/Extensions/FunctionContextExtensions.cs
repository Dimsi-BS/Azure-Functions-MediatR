using System.Collections;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace Azure.Functions.Worker.Extensions.MediatR.Extensions;

public static class FunctionContextExtensions
{
    public static async Task<object?> GetRequestObjectAsync(this FunctionContext context, Type requestType)
    {

        if (!context.Items.TryGetValue("HttpRequestContext", out var item)
            || item is not HttpContext httpContext)
        {
            throw new ArgumentException($"The request context does not contain a '{nameof(HttpContext)}'.");
        }
        
        try
        {
            var serializerSettings = (JsonSerializerSettings)httpContext.RequestServices.GetRequiredService(typeof(JsonSerializerSettings));
            var modelMetadataProvider = (IModelMetadataProvider)httpContext.RequestServices.GetRequiredService(typeof(IModelMetadataProvider));

            object? result = null;

            var hasBody = httpContext.Request.Method != "GET" && httpContext.Request.Method != "DELETE";

            if (hasBody)
            {
                var stringContent = await httpContext.Request.ReadAsStringAsync();

                result = JsonConvert.DeserializeObject(stringContent, requestType, serializerSettings);
            }

            result ??= Activator.CreateInstance(requestType);

            modelMetadataProvider
                .GetMetadataForProperties(requestType)
                .OfType<DefaultModelMetadata>()
                .ToList()
                .ForEach(metadata =>
                {
                    if (
                        (!hasBody
                         ||
                         metadata.Attributes.PropertyAttributes!
                             .Any(p => p is FromRouteAttribute || p is FromQueryAttribute))
                        && result != null && context.BindingContext.BindingData
                            .TryGetValue(metadata.PropertyName!, out var value))
                    {
                        if (value is string stringValue)
                        {
                            if (metadata.ModelType == typeof(Guid))
                            {
                                metadata.PropertySetter?.Invoke(result, new Guid(stringValue));
                                return;
                            }

                            if (metadata.ModelType.IsEnum && int.TryParse(stringValue, out int intValue))
                            {
                                metadata.PropertySetter?.Invoke(result, Enum.ToObject(metadata.ModelType, intValue));
                                return;
                            }

                            if (metadata.ModelType.IsAssignableTo(typeof(IEnumerable)) &&
                                metadata.ModelType != typeof(string))
                            {

                                stringValue.Split(",").ToList().ForEach(v =>
                                {
                                    if (metadata.ModelType.GenericTypeArguments[0] == typeof(Guid))
                                    {
                                        var resultValue = metadata.PropertyGetter?.Invoke(result) as ICollection<Guid>;
                                        if (resultValue == null)
                                        {
                                            resultValue = new List<Guid>();
                                            metadata.PropertySetter?.Invoke(result, resultValue);
                                        }
                                        resultValue!.Add(new Guid(v));
                                    }
                                    if (metadata.ModelType.GenericTypeArguments[0] == typeof(string))
                                    {
                                        var resultValue = metadata.PropertyGetter?.Invoke(result) as ICollection<string>;
                                        if (resultValue == null)
                                        {
                                            resultValue = new List<string>();
                                            metadata.PropertySetter?.Invoke(result, resultValue);
                                        }
                                        resultValue!.Add(v);
                                    }
                                });

                                return;
                            }
                        }

                        metadata.PropertySetter?.Invoke(result, Convert.ChangeType(value, metadata.ModelType));
                    }
                });

            return result;
        }
        catch (Exception)
        {
            return null;
        }
    }
}
