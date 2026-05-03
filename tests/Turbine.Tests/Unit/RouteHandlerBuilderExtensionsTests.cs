using System.Linq.Expressions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Turbine.Tests.Unit;

public class RouteHandlerBuilderExtensionsTests
{
    public sealed class FixtureSchemas : SchemaConfiguration
    {
        public StringSchema Foo { get; set; } = null!;
        public StringSchema Bar { get; set; } = null!;

        public override void Configure(SchemaConfigurationBuilder builder)
        {
            builder.Schema(() => Foo);
            builder.Schema(() => Bar);
        }
    }

    private static (WebApplication App, Action<RouteHandlerBuilder> Capture) Build()
    {
        var app = WebApplication.CreateBuilder().Build();
        return (app, _ => { });
    }

    private static IReadOnlyList<TurbineEndpointSchemaMetadata> Materialize(WebApplication app)
    {
        var routeBuilder = (IEndpointRouteBuilder) app;
        var endpoints = routeBuilder.DataSources.SelectMany(d => d.Endpoints).ToArray();
        var endpoint = endpoints.Single();
        return endpoint.Metadata.OfType<TurbineEndpointSchemaMetadata>().ToArray();
    }

    [Fact]
    public void Produces_attaches_response_metadata()
    {
        var (app, _) = Build();
        app.MapGet("/x", () => "x").Produces<FixtureSchemas>(200, x => x.Foo);

        var metadata = Materialize(app);
        var entry = Assert.Single(metadata);
        Assert.Equal(typeof(FixtureSchemas), entry.ConfigurationType);
        Assert.Equal("Foo", entry.SchemaPropertyName);
        Assert.Equal(EndpointSchemaRole.Response, entry.Role);
        Assert.Equal(200, entry.StatusCode);
        Assert.Equal("application/json", entry.ContentType);
    }

    [Fact]
    public void Accepts_attaches_request_metadata()
    {
        var (app, _) = Build();
        app.MapPost("/x", () => "x").Accepts<FixtureSchemas>(x => x.Bar);

        var metadata = Materialize(app);
        var entry = Assert.Single(metadata);
        Assert.Equal("Bar", entry.SchemaPropertyName);
        Assert.Equal(EndpointSchemaRole.Request, entry.Role);
        Assert.Null(entry.StatusCode);
        Assert.Equal("application/json", entry.ContentType);
    }

    [Fact]
    public void Produces_called_twice_with_different_status_codes_attaches_two_entries()
    {
        var (app, _) = Build();
        app.MapGet("/x", () => "x")
            .Produces<FixtureSchemas>(200, x => x.Foo)
            .Produces<FixtureSchemas>(201, x => x.Foo);

        var metadata = Materialize(app);
        Assert.Equal(2, metadata.Count);
        Assert.Contains(metadata, m => m.StatusCode == 200);
        Assert.Contains(metadata, m => m.StatusCode == 201);
    }

    [Fact]
    public void Accepts_and_Produces_on_same_endpoint_both_attach()
    {
        var (app, _) = Build();
        app.MapPost("/x", () => "x")
            .Accepts<FixtureSchemas>(x => x.Bar)
            .Produces<FixtureSchemas>(201, x => x.Foo);

        var metadata = Materialize(app);
        Assert.Equal(2, metadata.Count);
        Assert.Contains(metadata, m => m.Role == EndpointSchemaRole.Request);
        Assert.Contains(metadata, m => m.Role == EndpointSchemaRole.Response);
    }

    [Fact]
    public void Produces_throws_on_null_builder()
    {
        RouteHandlerBuilder builder = null!;
        Assert.Throws<ArgumentNullException>(() =>
            builder.Produces<FixtureSchemas>(200, x => x.Foo));
    }

    [Fact]
    public void Produces_throws_on_null_selector()
    {
        var (app, _) = Build();
        var route = app.MapGet("/x", () => "x");
        Assert.Throws<ArgumentNullException>(() =>
            route.Produces<FixtureSchemas>(200, null!));
    }

    [Fact]
    public void Accepts_throws_on_null_builder()
    {
        RouteHandlerBuilder builder = null!;
        Assert.Throws<ArgumentNullException>(() => builder.Accepts<FixtureSchemas>(x => x.Foo));
    }

    [Fact]
    public void Accepts_throws_on_null_selector()
    {
        var (app, _) = Build();
        var route = app.MapPost("/x", () => "x");
        Assert.Throws<ArgumentNullException>(() => route.Accepts<FixtureSchemas>(null!));
    }

    [Theory]
    [InlineData(99)]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(600)]
    [InlineData(700)]
    public void Produces_rejects_status_codes_outside_HTTP_range(int statusCode)
    {
        var (app, _) = Build();
        var route = app.MapGet("/x", () => "x");
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            route.Produces<FixtureSchemas>(statusCode, x => x.Foo));
    }

    [Fact]
    public void Produces_rejects_method_call_selector()
    {
        var (app, _) = Build();
        var route = app.MapGet("/x", () => "x");
        Expression<Func<FixtureSchemas, ISchema>> selector = x => GetSchema(x);
        var ex = Assert.Throws<ArgumentException>(() =>
            route.Produces<FixtureSchemas>(200, selector));
        Assert.Contains("property access", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("FixtureSchemas", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Accepts_rejects_constant_selector()
    {
        var (app, _) = Build();
        var route = app.MapPost("/x", () => "x");
        Expression<Func<FixtureSchemas, ISchema>> selector = x => default(StringSchema)!;
        Assert.Throws<ArgumentException>(() => route.Accepts<FixtureSchemas>(selector));
    }

    private static StringSchema GetSchema(FixtureSchemas s) => s.Foo;
}
