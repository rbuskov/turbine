using System.Net.Http.Json;
using System.Text.Json;

namespace Turbine.StarfleetApi.Tests;

public class OpenApiDocumentTests : IClassFixture<StarfleetAppFixture>
{
    private readonly StarfleetAppFixture fixture;

    public OpenApiDocumentTests(StarfleetAppFixture fixture)
    {
        this.fixture = fixture;
    }

    [Fact]
    public async Task Document_lists_all_resource_endpoints()
    {
        using var client = fixture.CreateScopedClient();
        var doc = await client.GetFromJsonAsync<JsonElement>("/openapi/v1.json");
        var paths = doc.GetProperty("paths").EnumerateObject().Select(p => p.Name).ToHashSet();
        Assert.Contains("/personnel", paths);
        Assert.Contains("/personnel/{id}", paths);
        Assert.Contains("/starships", paths);
        Assert.Contains("/starships/{registry}", paths);
        Assert.Contains("/stardate", paths);
    }

    [Fact]
    public async Task Personnel_summary_schema_is_an_array_of_summary_items()
    {
        using var client = fixture.CreateScopedClient();
        var doc = await client.GetFromJsonAsync<JsonElement>("/openapi/v1.json");
        var schema = doc.GetProperty("components").GetProperty("schemas").GetProperty("PersonnelSummary");
        Assert.Equal("array", schema.GetProperty("type").GetString());
        var item = schema.GetProperty("items");
        var props = item.GetProperty("properties").EnumerateObject().Select(p => p.Name).ToHashSet();
        Assert.Contains("Id", props);
        Assert.Contains("Name", props);
        Assert.Contains("EnteredServiceDate", props);
        Assert.Contains("AssignedShipName", props);
    }

    [Fact]
    public async Task Personnel_create_uses_oneOf_with_discriminator()
    {
        using var client = fixture.CreateScopedClient();
        var doc = await client.GetFromJsonAsync<JsonElement>("/openapi/v1.json");
        var schema = doc.GetProperty("components").GetProperty("schemas").GetProperty("PersonnelCreate");
        Assert.True(schema.TryGetProperty("oneOf", out var oneOf));
        Assert.Equal(3, oneOf.GetArrayLength());
        Assert.Equal("Type", schema.GetProperty("discriminator").GetProperty("propertyName").GetString());
    }

    [Fact]
    public async Task Patch_schema_marks_no_field_as_required()
    {
        using var client = fixture.CreateScopedClient();
        var doc = await client.GetFromJsonAsync<JsonElement>("/openapi/v1.json");
        var schema = doc.GetProperty("components").GetProperty("schemas").GetProperty("StarshipPatch");
        // Patch schema marks fields not required, so the JSON Schema "required" array should be absent or empty.
        if (schema.TryGetProperty("required", out var required))
        {
            Assert.Equal(0, required.GetArrayLength());
        }
    }

    [Fact]
    public async Task Officer_rank_enum_exposes_all_values_as_strings()
    {
        using var client = fixture.CreateScopedClient();
        var doc = await client.GetFromJsonAsync<JsonElement>("/openapi/v1.json");
        var officerVariant = doc.GetProperty("components").GetProperty("schemas")
            .GetProperty("PersonnelCreate").GetProperty("oneOf")
            .EnumerateArray()
            .Single(v => v.GetProperty("properties").GetProperty("Type").GetProperty("enum")
                .EnumerateArray().Single().GetString() == "Officer");
        var rank = officerVariant.GetProperty("properties").GetProperty("Rank");
        Assert.Equal("string", rank.GetProperty("type").GetString());
        var values = rank.GetProperty("enum").EnumerateArray().Select(v => v.GetString()).ToList();
        Assert.Contains("Captain", values);
        Assert.Contains("Ensign", values);
        Assert.Contains("FleetAdmiral", values);
    }

    [Fact]
    public async Task Starship_registry_pattern_is_exposed()
    {
        using var client = fixture.CreateScopedClient();
        var doc = await client.GetFromJsonAsync<JsonElement>("/openapi/v1.json");
        var schema = doc.GetProperty("components").GetProperty("schemas").GetProperty("StarshipCreate");
        var registry = schema.GetProperty("properties").GetProperty("Registry");
        var pattern = registry.GetProperty("pattern").GetString();
        Assert.NotNull(pattern);
        Assert.Contains("NCC", pattern);
    }
}
