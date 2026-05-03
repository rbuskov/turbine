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

        if (set.Types.Count == 0)
        {
            throw new InvalidOperationException(
                "MapTurbine found no SchemaConfiguration subclasses to initialize. " +
                "Ensure AddTurbine is called with one or more assemblies that contain " +
                "concrete public SchemaConfiguration subclasses.");
        }

        var configurations = new List<SchemaConfiguration>(set.Types.Count);
        foreach (var type in set.Types)
        {
            configurations.Add((SchemaConfiguration) app.Services.GetRequiredService(type));
        }

        registry.Build(configurations);

        return app;
    }
}
