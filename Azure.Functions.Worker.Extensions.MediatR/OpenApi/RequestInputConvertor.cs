using MediatR;
using Microsoft.Azure.Functions.Worker.Converters;
using Azure.Functions.Worker.Extensions.MediatR.Extensions;

namespace Azure.Functions.Worker.Extensions.MediatR.OpenApi;

public class RequestInputConvertor : IInputConverter
{
    public async ValueTask<ConversionResult> ConvertAsync(ConverterContext context)
    {
        if (!context.TargetType.IsAssignableTo(typeof(IBaseRequest)))
        {
            return ConversionResult.Unhandled();
        }

        try
        {
            var result = await context.FunctionContext.GetRequestObjectAsync(context.TargetType);
            
            return ConversionResult.Success(result);
        }
        catch (Exception ex)
        {
            return ConversionResult.Failed(ex);
        }
    }
}
