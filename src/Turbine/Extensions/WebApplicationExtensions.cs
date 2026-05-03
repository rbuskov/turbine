using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Turbine;

public static class WebApplicationExtensions
{
    public static WebApplication MapTurbine(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var set = app.Services.GetService<TurbineConfigurationSet>()
            ?? throw new InvalidOperationException(
                "MapTurbine requires AddTurbine to be called on the service collection before the host is built.");
        var registry = app.Services.GetService<TurbineSchemaRegistry>()
            ?? throw new InvalidOperationException(
                "MapTurbine requires AddTurbine to be called on the service collection before the host is built.");

        var configurations = new List<SchemaConfiguration>(set.Types.Count);
        foreach (var type in set.Types)
        {
            configurations.Add((SchemaConfiguration) app.Services.GetRequiredService(type));
        }

        registry.Build(configurations);

        return app;
    }
}
