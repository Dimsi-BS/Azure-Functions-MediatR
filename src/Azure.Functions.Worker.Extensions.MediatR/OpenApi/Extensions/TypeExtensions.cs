﻿using Microsoft.Xrm.Sdk;

namespace Azure.Functions.Worker.Extensions.MediatR.OpenApi.Extensions;

public static class TypeExtensions
{
    public static bool IsDataCollectionType(this Type type)
        => (type.BaseType?.IsGenericType ?? false) && type.IsAssignableTo(type.GetDataCollectionType());

    public static Type GetDataCollectionType(this Type type)
        => type.BaseType?.GenericTypeArguments.Length == 2
            ? typeof(DataCollection<,>).MakeGenericType(type.BaseType.GenericTypeArguments)
            : typeof(DataCollection<>).MakeGenericType(type.BaseType!.GenericTypeArguments);
}
