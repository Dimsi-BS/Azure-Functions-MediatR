namespace Azure.Functions.Worker.Extensions.MediatR.OpenApi.Extensions;

public static class TypeExtensions
{
    private static readonly Lazy<Type?> DataCollection1Type = new(() => AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes()).FirstOrDefault(t => t.FullName == "Microsoft.Xrm.Sdk.DataCollection`1"));
    private static readonly Lazy<Type?> DataCollection2Type = new(() => AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes()).FirstOrDefault(t => t.FullName == "Microsoft.Xrm.Sdk.DataCollection`2"));
    
    private static Type? DataCollection1TypeValue => DataCollection1Type.Value;
    private static Type? DataCollection2TypeValue => DataCollection2Type.Value;
    
    public static bool IsDataCollectionType(this Type type)
        => DataCollection1TypeValue != null && DataCollection2TypeValue != null && (type.BaseType?.IsGenericType ?? false) && type.IsAssignableTo(type.GetDataCollectionType());

    private static Type GetDataCollectionType(this Type type)
        => type.BaseType?.GenericTypeArguments.Length == 2
            ? DataCollection2TypeValue!.MakeGenericType(type.BaseType.GenericTypeArguments)
            : DataCollection1TypeValue!.MakeGenericType(type.BaseType!.GenericTypeArguments);
}
