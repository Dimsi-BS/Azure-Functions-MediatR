using System.Collections;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace Azure.Functions.Worker.Extensions.MediatR.OpenApi;

public class RequestInputConvertor : IInputConverter
{
    public async ValueTask<ConversionResult> ConvertAsync(ConverterContext context)
    {
        if (!context.TargetType.IsAssignableTo(typeof(IBaseRequest)))
        {
            return ConversionResult.Unhandled();
        }

        if (!context.FunctionContext.Items.TryGetValue("HttpRequestContext", out var item) 
            || item is not HttpContext httpContext)
        {
            return ConversionResult.Unhandled();
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

                result = JsonConvert.DeserializeObject(stringContent, context.TargetType, serializerSettings);
            }
                
            result ??= Activator.CreateInstance(context.TargetType);

            modelMetadataProvider
                .GetMetadataForProperties(context.TargetType)
                .OfType<DefaultModelMetadata>()
                .ToList()
                .ForEach(metadata =>
                {
                    if (
                        (!hasBody 
                         || 
                         metadata.Attributes.PropertyAttributes!
                             .Any(p => p is FromRouteAttribute || p is FromQueryAttribute))
                        && result != null && context.FunctionContext.BindingContext.BindingData
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

            return ConversionResult.Success(result);
        }
        catch (Exception ex)
        {
            return ConversionResult.Failed(ex);
        }

        return ConversionResult.Unhandled();
    }
}
