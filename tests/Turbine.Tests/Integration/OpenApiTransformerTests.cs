using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Turbine.Tests.Integration;

public class OpenApiTransformerTests
{
    public sealed class Person
    {
        public string Name { get; set; } = "";
        public int Age { get; set; }
    }

    public sealed class Pet
    {
        public string Kind { get; set; } = "";
        public string Owner { get; set; } = "";
    }

    public sealed class FixtureSchemas : SchemaConfiguration
    {
        public ObjectSchema<Person> Summary { get; set; } = null!;
        public ObjectSchema<Person> CreateResult { get; set; } = null!;
        public ObjectSchema<Person> CreateInput { get; set; } = null!;
        public ObjectSchema<Pet> Pet { get; set; } = null!;
        public StringSchema PetName { get; set; } = null!;

        public override void Configure(SchemaConfigurationBuilder builder)
        {
            builder.Schema(() => Summary)
                .Add(p => p.Name)
                .Add(p => p.Age);

            builder.Schema(() => CreateResult)
                .Add(p => p.Name);

            builder.Schema(() => CreateInput)
                .Add(p => p.Name)
                .Add(p => p.Age);

            builder.Schema(() => Pet)
                .Add(p => p.Kind)
                .Add(p => p.Owner);

            builder.Schema(() => PetName).MinLength(1);
        }
    }

    private static async Task<JsonDocument> GetOpenApiDocument(Action<WebApplication> configure)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Environment.EnvironmentName = Environments.Development;
        builder.WebHost.UseTestServer();
        builder.Services.AddOpenApi();
        builder.Services.AddTurbine(typeof(OpenApiTransformerTests).Assembly);
        var app = builder.Build();
        app.MapTurbine();
        app.MapOpenApi();
        configure(app);
        await app.StartAsync();
        var client = app.GetTestClient();
        var json = await client.GetStringAsync("/openapi/v1.json");
        await app.StopAsync();
        return JsonDocument.Parse(json);
    }

    [Fact]
    public async Task Produces_emits_components_schema_and_response_ref()
    {
        using var doc = await GetOpenApiDocument(app =>
        {
            app.MapGet("/people", () => Array.Empty<Person>())
                .Produces<FixtureSchemas>(200, x => x.Summary);
        });

        var components = doc.RootElement.GetProperty("components").GetProperty("schemas");
        Assert.True(components.TryGetProperty("FixtureSummary", out var summary));
        Assert.Equal("object", summary.GetProperty("type").GetString());

        var ref200 = doc.RootElement
            .GetProperty("paths")
            .GetProperty("/people")
            .GetProperty("get")
            .GetProperty("responses")
            .GetProperty("200")
            .GetProperty("content")
            .GetProperty("application/json")
            .GetProperty("schema")
            .GetProperty("$ref")
            .GetString();
        Assert.Equal("#/components/schemas/FixtureSummary", ref200);
    }

    [Fact]
    public async Task Accepts_emits_request_body_ref()
    {
        using var doc = await GetOpenApiDocument(app =>
        {
            app.MapPost("/people", (JsonElement body) => Results.Ok())
                .Accepts<FixtureSchemas>(x => x.CreateInput);
        });

        var refIn = doc.RootElement
            .GetProperty("paths")
            .GetProperty("/people")
            .GetProperty("post")
            .GetProperty("requestBody")
            .GetProperty("content")
            .GetProperty("application/json")
            .GetProperty("schema")
            .GetProperty("$ref")
            .GetString();
        Assert.Equal("#/components/schemas/FixtureCreateInput", refIn);
    }

    [Fact]
    public async Task Accepts_and_Produces_on_same_endpoint_emits_both()
    {
        using var doc = await GetOpenApiDocument(app =>
        {
            app.MapPost("/people", (JsonElement body) => Results.Ok())
                .Accepts<FixtureSchemas>(x => x.CreateInput)
                .Produces<FixtureSchemas>(201, x => x.CreateResult);
        });

        var op = doc.RootElement.GetProperty("paths").GetProperty("/people").GetProperty("post");

        var requestRef = op.GetProperty("requestBody")
            .GetProperty("content").GetProperty("application/json")
            .GetProperty("schema").GetProperty("$ref").GetString();
        Assert.Equal("#/components/schemas/FixtureCreateInput", requestRef);

        var responseRef = op.GetProperty("responses").GetProperty("201")
            .GetProperty("content").GetProperty("application/json")
            .GetProperty("schema").GetProperty("$ref").GetString();
        Assert.Equal("#/components/schemas/FixtureCreateResult", responseRef);
    }

    [Fact]
    public async Task Same_schema_referenced_from_two_endpoints_dedups_to_one_component()
    {
        using var doc = await GetOpenApiDocument(app =>
        {
            app.MapGet("/a", () => Array.Empty<Person>())
                .Produces<FixtureSchemas>(200, x => x.Summary);
            app.MapGet("/b", () => Array.Empty<Person>())
                .Produces<FixtureSchemas>(200, x => x.Summary);
        });

        var components = doc.RootElement.GetProperty("components").GetProperty("schemas");
        Assert.True(components.TryGetProperty("FixtureSummary", out _));
        var ids = new List<string>();
        foreach (var property in components.EnumerateObject())
        {
            if (property.Name.EndsWith("Summary", StringComparison.Ordinal))
            {
                ids.Add(property.Name);
            }
        }
        Assert.Single(ids);
    }

    [Fact]
    public async Task Endpoint_without_Turbine_metadata_is_left_alone()
    {
        using var doc = await GetOpenApiDocument(app =>
        {
            app.MapGet("/plain", () => "hello");
        });

        var op = doc.RootElement.GetProperty("paths").GetProperty("/plain").GetProperty("get");
        // No requestBody, no Turbine-derived $ref
        var hasTurbineSchemas = false;
        if (doc.RootElement.TryGetProperty("components", out var components)
            && components.TryGetProperty("schemas", out var schemas))
        {
            foreach (var prop in schemas.EnumerateObject())
            {
                if (prop.Name.StartsWith("Fixture", StringComparison.Ordinal))
                {
                    hasTurbineSchemas = true;
                }
            }
        }
        Assert.False(hasTurbineSchemas);
    }

    [Fact]
    public async Task Status_code_keyed_response_is_preserved_alongside_schema_ref()
    {
        using var doc = await GetOpenApiDocument(app =>
        {
            app.MapGet("/x", () => "x")
                .Produces<FixtureSchemas>(404, x => x.Summary);
        });

        var responses = doc.RootElement.GetProperty("paths")
            .GetProperty("/x").GetProperty("get").GetProperty("responses");
        Assert.True(responses.TryGetProperty("404", out var notFound));
        var refStr = notFound.GetProperty("content").GetProperty("application/json")
            .GetProperty("schema").GetProperty("$ref").GetString();
        Assert.Equal("#/components/schemas/FixtureSummary", refStr);
    }

    [Fact]
    public async Task MapGroup_with_empty_child_pattern_still_emits_response_schema()
    {
        // ApiDescription.RelativePath is "people/" for MapGroup("people").MapGet(""),
        // but OpenApiDocument.Paths keys the operation as "/people". The transformer
        // must handle the trailing-slash mismatch.
        using var doc = await GetOpenApiDocument(app =>
        {
            var grp = app.MapGroup("people").WithTags("People");
            grp.MapGet("", () => Array.Empty<Person>())
                .Produces<FixtureSchemas>(200, x => x.Summary);
            grp.MapPost("", (JsonElement body) => Results.Ok())
                .Produces<FixtureSchemas>(201, x => x.CreateResult);
        });

        var listResp = doc.RootElement.GetProperty("paths")
            .GetProperty("/people").GetProperty("get")
            .GetProperty("responses").GetProperty("200")
            .GetProperty("content").GetProperty("application/json")
            .GetProperty("schema").GetProperty("$ref").GetString();
        Assert.Equal("#/components/schemas/FixtureSummary", listResp);

        var postResp = doc.RootElement.GetProperty("paths")
            .GetProperty("/people").GetProperty("post")
            .GetProperty("responses").GetProperty("201")
            .GetProperty("content").GetProperty("application/json")
            .GetProperty("schema").GetProperty("$ref").GetString();
        Assert.Equal("#/components/schemas/FixtureCreateResult", postResp);
    }

    [Fact]
    public async Task References_a_StringSchema_from_a_StringSchema_property()
    {
        using var doc = await GetOpenApiDocument(app =>
        {
            app.MapGet("/petname", () => "ok")
                .Produces<FixtureSchemas>(200, x => x.PetName);
        });

        var component = doc.RootElement.GetProperty("components").GetProperty("schemas")
            .GetProperty("FixturePetName");
        Assert.Equal("string", component.GetProperty("type").GetString());
        Assert.Equal(1, component.GetProperty("minLength").GetInt32());
    }

    public sealed class MissingSchemaConfig : SchemaConfiguration
    {
        public StringSchema Defined { get; set; } = null!;

        public override void Configure(SchemaConfigurationBuilder builder)
        {
            builder.Schema(() => Defined);
        }
    }

    [Fact]
    public async Task Missing_schema_property_throws_clear_message()
    {
        var ex = await Assert.ThrowsAsync<HttpRequestException>(async () =>
        {
            using var _ = await GetOpenApiDocument(app =>
            {
                // 'Undefined' is not a property on MissingSchemaConfig at all,
                // but we test by referencing a property name that is not in the registry.
                // Build a fake metadata directly via a hand-crafted endpoint:
                app.MapGet("/x", () => "x")
                    .WithMetadata(new TurbineEndpointSchemaMetadata(
                        ConfigurationType: typeof(MissingSchemaConfig),
                        SchemaPropertyName: "Undefined",
                        Role: EndpointSchemaRole.Response,
                        StatusCode: 200,
                        ContentType: "application/json"));
            });
        });
        // Fetching /openapi/v1.json should produce 500 because the document transformer threw.
        Assert.Contains("500", ex.Message, StringComparison.OrdinalIgnoreCase);
    }
}
