using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Turbine.StarfleetApi.Tests;

public class StarshipEndpointsTests : IClassFixture<StarfleetAppFixture>
{
    private readonly StarfleetAppFixture fixture;

    public StarshipEndpointsTests(StarfleetAppFixture fixture)
    {
        this.fixture = fixture;
    }

    [Fact]
    public async Task List_returns_seeded_summary()
    {
        using var client = fixture.CreateScopedClient();
        var doc = await client.GetFromJsonAsync<JsonElement>("/starships");
        var enterprise = doc.EnumerateArray().Single(s => s.GetProperty("Registry").GetString() == "NCC-1701-D");
        Assert.Equal("Enterprise", enterprise.GetProperty("Name").GetString());
        Assert.False(enterprise.TryGetProperty("RecentDeployments", out _));
    }

    [Fact]
    public async Task Get_returns_details_with_deployments_and_total()
    {
        using var client = fixture.CreateScopedClient();
        var doc = await client.GetFromJsonAsync<JsonElement>("/starships/NCC-1701-D");
        Assert.Equal("Enterprise", doc.GetProperty("Name").GetString());
        Assert.Equal(1, doc.GetProperty("TotalDeployments").GetInt32());
        var deployment = doc.GetProperty("RecentDeployments").EnumerateArray().Single();
        Assert.Equal("Deep Space Exploration", deployment.GetProperty("MissionName").GetString());
        Assert.Equal(41000m, deployment.GetProperty("StartDate").GetDecimal());
        Assert.Equal(JsonValueKind.Null, deployment.GetProperty("EndDate").ValueKind);
    }

    [Fact]
    public async Task Get_returns_404_for_unknown_registry()
    {
        using var client = fixture.CreateScopedClient();
        var response = await client.GetAsync("/starships/UNKNOWN");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_persists_starship()
    {
        using var client = fixture.CreateScopedClient();
        var body = JsonDocument.Parse("""{"Registry":"NCC-74656","Name":"Voyager"}""").RootElement;
        var post = await client.PostAsJsonAsync("/starships", body);
        Assert.Equal(HttpStatusCode.Created, post.StatusCode);
        Assert.Equal("/starships/NCC-74656", post.Headers.Location!.ToString());

        var fetched = await client.GetFromJsonAsync<JsonElement>("/starships/NCC-74656");
        Assert.Equal("Voyager", fetched.GetProperty("Name").GetString());
        Assert.Equal(0, fetched.GetProperty("TotalDeployments").GetInt32());
        Assert.Empty(fetched.GetProperty("RecentDeployments").EnumerateArray());
    }

    [Fact]
    public async Task Update_replaces_name()
    {
        using var client = fixture.CreateScopedClient();
        await client.PostAsJsonAsync("/starships",
            JsonDocument.Parse("""{"Registry":"NCC-74656","Name":"Voyager"}""").RootElement);

        var put = await client.PutAsJsonAsync("/starships/NCC-74656",
            JsonDocument.Parse("""{"Registry":"NCC-74656","Name":"USS Voyager"}""").RootElement);
        Assert.Equal(HttpStatusCode.NoContent, put.StatusCode);

        var fetched = await client.GetFromJsonAsync<JsonElement>("/starships/NCC-74656");
        Assert.Equal("USS Voyager", fetched.GetProperty("Name").GetString());
    }

    [Fact]
    public async Task Patch_updates_only_supplied_fields()
    {
        using var client = fixture.CreateScopedClient();
        await client.PostAsJsonAsync("/starships",
            JsonDocument.Parse("""{"Registry":"NCC-74656","Name":"Voyager"}""").RootElement);

        var request = new HttpRequestMessage(HttpMethod.Patch, "/starships/NCC-74656")
        {
            Content = JsonContent.Create(JsonDocument.Parse("""{"Name":"Voyager NX"}""").RootElement),
        };
        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var fetched = await client.GetFromJsonAsync<JsonElement>("/starships/NCC-74656");
        Assert.Equal("Voyager NX", fetched.GetProperty("Name").GetString());
        Assert.Equal("NCC-74656", fetched.GetProperty("Registry").GetString());
    }

    [Fact]
    public async Task Update_returns_404_when_missing()
    {
        using var client = fixture.CreateScopedClient();
        var put = await client.PutAsJsonAsync("/starships/NX-9999",
            JsonDocument.Parse("""{"Registry":"NX-9999","Name":"Ghost"}""").RootElement);
        Assert.Equal(HttpStatusCode.NotFound, put.StatusCode);
    }

    [Fact]
    public async Task Patch_returns_404_when_missing()
    {
        using var client = fixture.CreateScopedClient();
        var request = new HttpRequestMessage(HttpMethod.Patch, "/starships/NX-9999")
        {
            Content = JsonContent.Create(JsonDocument.Parse("""{"Name":"Ghost"}""").RootElement),
        };
        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_removes_starship_and_is_idempotent()
    {
        using var client = fixture.CreateScopedClient();
        await client.PostAsJsonAsync("/starships",
            JsonDocument.Parse("""{"Registry":"NCC-74656","Name":"Voyager"}""").RootElement);

        var first = await client.DeleteAsync("/starships/NCC-74656");
        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);

        var second = await client.DeleteAsync("/starships/NCC-74656");
        Assert.Equal(HttpStatusCode.NoContent, second.StatusCode);

        var get = await client.GetAsync("/starships/NCC-74656");
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
    }
}
