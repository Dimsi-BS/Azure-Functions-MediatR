using Microsoft.OpenApi.Models;

namespace Azure.Functions.Worker.Extensions.MediatR.Configuration;

public class OpenApiInfoBuilder : IOpenApiInfoBuilder
{
    private readonly OpenApiInfo _openApiInfo;

    internal OpenApiInfoBuilder(OpenApiInfo openApiInfo)
    {
        _openApiInfo = openApiInfo;
    }
    
    public IOpenApiInfoBuilder Title(string title)
    {
        _openApiInfo.Title = title;
        return this;
    }

    public IOpenApiInfoBuilder Version(string version)
    {
        _openApiInfo.Version = version;
        return this;
    }

    public IOpenApiInfoBuilder Description(string description)
    {
        _openApiInfo.Description = description;
        return this;
    }
}

public interface IOpenApiInfoBuilder
{
    IOpenApiInfoBuilder Title(string title);
    IOpenApiInfoBuilder Version(string version);
    IOpenApiInfoBuilder Description(string description);
}
