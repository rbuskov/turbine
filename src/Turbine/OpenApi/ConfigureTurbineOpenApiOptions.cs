using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Options;

namespace Turbine;

internal sealed class ConfigureTurbineOpenApiOptions : IConfigureNamedOptions<OpenApiOptions>
{
    public void Configure(OpenApiOptions options) => Configure(name: null, options);

    public void Configure(string? name, OpenApiOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.AddDocumentTransformer<TurbineOpenApiDocumentTransformer>();
    }
}
