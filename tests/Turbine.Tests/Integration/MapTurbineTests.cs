using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Turbine.Tests.Integration;

public class MapTurbineTests
{
    public sealed class FixturePerson
    {
        public string Name { get; set; } = "";
        public int Age { get; set; }
    }

    public sealed class HostedConfig : SchemaConfiguration
    {
        public ObjectSchema<FixturePerson> Person { get; set; } = null!;
        public StringSchema Name { get; set; } = null!;

        public override void Configure(SchemaConfigurationBuilder builder)
        {
            builder.Schema(() => Person)
                .Add(p => p.Name)
                .Add(p => p.Age);
            builder.Schema(() => Name).MinLength(2);
        }
    }

    private static WebApplication BuildApp(Action<WebApplicationBuilder>? configure = null)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Environment.EnvironmentName = Environments.Development;
        configure?.Invoke(builder);
        return builder.Build();
    }

    [Fact]
    public void MapTurbine_runs_Configure_on_each_registered_configuration()
    {
        var app = BuildApp(b => b.Services.AddTurbine(typeof(MapTurbineTests).Assembly));

        app.MapTurbine();

        var config = app.Services.GetRequiredService<HostedConfig>();
        Assert.NotNull(config.Person);
        Assert.Equal(new[] { "Name", "Age" }, config.Person.Properties.Select(p => p.Name).ToArray());
        Assert.NotNull(config.Name);
        Assert.Equal(2, config.Name.MinLength);
    }

    [Fact]
    public void MapTurbine_populates_registry_with_resolved_schemas()
    {
        var app = BuildApp(b => b.Services.AddTurbine(typeof(MapTurbineTests).Assembly));

        app.MapTurbine();

        var registry = app.Services.GetRequiredService<TurbineSchemaRegistry>();
        var config = app.Services.GetRequiredService<HostedConfig>();
        Assert.Same(config.Person, registry.Resolve(typeof(HostedConfig), nameof(HostedConfig.Person)));
        Assert.Same(config.Name, registry.Resolve(typeof(HostedConfig), nameof(HostedConfig.Name)));
    }

    [Fact]
    public void MapTurbine_throws_when_AddTurbine_was_not_called()
    {
        var app = BuildApp();

        var ex = Assert.Throws<InvalidOperationException>(() => app.MapTurbine());
        Assert.Contains("AddTurbine", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void MapTurbine_returns_same_app_instance()
    {
        var app = BuildApp(b => b.Services.AddTurbine(typeof(MapTurbineTests).Assembly));

        var returned = app.MapTurbine();

        Assert.Same(app, returned);
    }

    [Fact]
    public void MapTurbine_throws_on_null_app()
    {
        WebApplication app = null!;
        Assert.Throws<ArgumentNullException>(() => app.MapTurbine());
    }
}
