using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Turbine.Starfleet;

namespace Turbine.StarfleetApi.Tests;

public class PersonnelEndpointsTests : IClassFixture<StarfleetAppFixture>
{
    private readonly StarfleetAppFixture fixture;

    public PersonnelEndpointsTests(StarfleetAppFixture fixture)
    {
        this.fixture = fixture;
    }

    [Fact]
    public async Task List_returns_seeded_personnel_with_summary_shape()
    {
        using var client = fixture.CreateScopedClient();
        var doc = await client.GetFromJsonAsync<JsonElement>("/personnel");

        var items = doc.EnumerateArray().ToList();
        Assert.NotEmpty(items);
        var picard = items.Single(p => p.GetProperty("Name").GetString() == "Jean-Luc Picard");
        Assert.Equal("NCC-1701-D", picard.GetProperty("AssignedShipRegistry").GetString());
        Assert.Equal("Enterprise", picard.GetProperty("AssignedShipName").GetString());
        Assert.Equal(10000m, picard.GetProperty("EnteredServiceDate").GetDecimal());
        // Summary excludes details-only fields.
        Assert.False(picard.TryGetProperty("Rank", out _));
        Assert.False(picard.TryGetProperty("Type", out _));
    }

    [Fact]
    public async Task Get_officer_returns_full_detail_with_discriminator_and_sponsored_civilians()
    {
        using var client = fixture.CreateScopedClient();
        var picard = await GetByName(client, "Jean-Luc Picard");

        var doc = await client.GetFromJsonAsync<JsonElement>($"/personnel/{picard.Id}");
        Assert.Equal("Officer", doc.GetProperty("Type").GetString());
        Assert.Equal("Captain", doc.GetProperty("Rank").GetString());
        Assert.Equal("Commanding Officer", doc.GetProperty("Position").GetString());
        Assert.Equal(10000m, doc.GetProperty("CommissionDate").GetDecimal());
        Assert.Equal(JsonValueKind.Null, doc.GetProperty("EnlistmentDate").ValueKind);
        var sponsored = doc.GetProperty("SponsoredCivilians").EnumerateArray()
            .Select(c => c.GetProperty("Name").GetString())
            .ToList();
        Assert.Contains("Keiko O'Brien", sponsored);
    }

    [Fact]
    public async Task Get_civilian_returns_civilian_shape_with_nested_sponsoring_officer()
    {
        using var client = fixture.CreateScopedClient();
        var keiko = await GetByName(client, "Keiko O'Brien");

        var doc = await client.GetFromJsonAsync<JsonElement>($"/personnel/{keiko.Id}");
        Assert.Equal("Civilian", doc.GetProperty("Type").GetString());
        Assert.Equal("Botanist", doc.GetProperty("Role").GetString());
        Assert.Equal("Jean-Luc Picard", doc.GetProperty("SponsoringOfficer").GetProperty("Name").GetString());
    }

    [Fact]
    public async Task Get_enlisted_returns_enlisted_shape()
    {
        using var client = fixture.CreateScopedClient();
        var obrien = await GetByName(client, "Miles O'Brien");

        var doc = await client.GetFromJsonAsync<JsonElement>($"/personnel/{obrien.Id}");
        Assert.Equal("Enlisted", doc.GetProperty("Type").GetString());
        Assert.Equal("ChiefPettyOfficer", doc.GetProperty("Rate").GetString());
        Assert.Equal("Transporter Chief", doc.GetProperty("Specialization").GetString());
    }

    [Fact]
    public async Task Get_returns_404_for_unknown_id()
    {
        using var client = fixture.CreateScopedClient();
        var response = await client.GetAsync("/personnel/9999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Get_returns_404_for_non_numeric_id()
    {
        using var client = fixture.CreateScopedClient();
        var response = await client.GetAsync("/personnel/not-an-int");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_officer_then_fetch_returns_persisted_data()
    {
        using var client = fixture.CreateScopedClient();
        var body = JsonDocument.Parse("""
        {
            "Type":"Officer",
            "Name":"William Riker",
            "SerialNumber":"SC-955-002",
            "Rank":"Commander",
            "Position":"First Officer",
            "EnlistmentDate":null,
            "CommissionDate":40000,
            "AssignedShipRegistry":"NCC-1701-D"
        }
        """).RootElement;

        var post = await client.PostAsJsonAsync("/personnel", body);
        Assert.Equal(HttpStatusCode.Created, post.StatusCode);
        var location = post.Headers.Location!.ToString();
        Assert.Matches(@"/personnel/\d+", location);

        var fetched = await client.GetFromJsonAsync<JsonElement>(location);
        Assert.Equal("Officer", fetched.GetProperty("Type").GetString());
        Assert.Equal("William Riker", fetched.GetProperty("Name").GetString());
        Assert.Equal("SC-955-002", fetched.GetProperty("SerialNumber").GetString());
        Assert.Equal("Commander", fetched.GetProperty("Rank").GetString());
        Assert.Equal(40000m, fetched.GetProperty("CommissionDate").GetDecimal());
        Assert.Equal(JsonValueKind.Null, fetched.GetProperty("EnlistmentDate").ValueKind);
    }

    [Fact]
    public async Task Create_enlisted_then_fetch_returns_persisted_data()
    {
        using var client = fixture.CreateScopedClient();
        var body = JsonDocument.Parse("""
        {
            "Type":"Enlisted",
            "Name":"Tasha Yar",
            "SerialNumber":"SE-999-001",
            "Rate":"PettyOfficer",
            "Specialization":"Tactical",
            "EnlistmentDate":40500,
            "AssignedShipRegistry":"NCC-1701-D"
        }
        """).RootElement;

        var post = await client.PostAsJsonAsync("/personnel", body);
        Assert.Equal(HttpStatusCode.Created, post.StatusCode);

        var fetched = await client.GetFromJsonAsync<JsonElement>(post.Headers.Location!.ToString());
        Assert.Equal("Enlisted", fetched.GetProperty("Type").GetString());
        Assert.Equal("PettyOfficer", fetched.GetProperty("Rate").GetString());
        Assert.Equal("Tactical", fetched.GetProperty("Specialization").GetString());
    }

    [Fact]
    public async Task Create_civilian_then_fetch_returns_persisted_data()
    {
        using var client = fixture.CreateScopedClient();
        var picard = await GetByName(client, "Jean-Luc Picard");
        var body = JsonDocument.Parse($$"""
        {
            "Type":"Civilian",
            "Name":"Guinan",
            "Role":"Bartender",
            "JoinedDate":42000,
            "AssignedShipRegistry":"NCC-1701-D",
            "SponsoringOfficerId":{{picard.Id}}
        }
        """).RootElement;

        var post = await client.PostAsJsonAsync("/personnel", body);
        Assert.Equal(HttpStatusCode.Created, post.StatusCode);

        var fetched = await client.GetFromJsonAsync<JsonElement>(post.Headers.Location!.ToString());
        Assert.Equal("Civilian", fetched.GetProperty("Type").GetString());
        Assert.Equal("Bartender", fetched.GetProperty("Role").GetString());
        Assert.Equal(42000m, fetched.GetProperty("JoinedDate").GetDecimal());
        Assert.Equal("Jean-Luc Picard", fetched.GetProperty("SponsoringOfficer").GetProperty("Name").GetString());
    }

    [Fact]
    public async Task Update_replaces_fields_and_returns_204()
    {
        using var client = fixture.CreateScopedClient();
        var created = await CreateOfficer(client, "Update Target", "SC-UPD-001");

        var body = JsonDocument.Parse($$"""
        {
            "Type":"Officer",
            "Name":"Update Target Renamed",
            "SerialNumber":"SC-UPD-001",
            "Rank":"Captain",
            "Position":"CO",
            "EnlistmentDate":null,
            "CommissionDate":50000,
            "AssignedShipRegistry":"NCC-1701-D"
        }
        """).RootElement;

        var put = await client.PutAsJsonAsync($"/personnel/{created.Id}", body);
        Assert.Equal(HttpStatusCode.NoContent, put.StatusCode);

        var after = await client.GetFromJsonAsync<JsonElement>($"/personnel/{created.Id}");
        Assert.Equal("Update Target Renamed", after.GetProperty("Name").GetString());
        Assert.Equal("Captain", after.GetProperty("Rank").GetString());
        Assert.Equal(50000m, after.GetProperty("CommissionDate").GetDecimal());
    }

    [Fact]
    public async Task Patch_only_updates_supplied_fields()
    {
        using var client = fixture.CreateScopedClient();
        var created = await CreateOfficer(client, "Patch Target", "SC-PCH-001", rank: "Lieutenant");

        var body = JsonDocument.Parse("""{"Type":"Officer","Rank":"Commander"}""").RootElement;
        var patch = new HttpRequestMessage(HttpMethod.Patch, $"/personnel/{created.Id}")
        {
            Content = JsonContent.Create(body),
        };
        var response = await client.SendAsync(patch);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var after = await client.GetFromJsonAsync<JsonElement>($"/personnel/{created.Id}");
        Assert.Equal("Patch Target", after.GetProperty("Name").GetString());
        Assert.Equal("SC-PCH-001", after.GetProperty("SerialNumber").GetString());
        Assert.Equal("Commander", after.GetProperty("Rank").GetString());
    }

    [Fact]
    public async Task Patch_can_set_nullable_field_to_null()
    {
        using var client = fixture.CreateScopedClient();
        var created = await CreateOfficer(client, "Null Target", "SC-NUL-001");

        // Confirm assigned ship is set
        var before = await client.GetFromJsonAsync<JsonElement>($"/personnel/{created.Id}");
        Assert.Equal("NCC-1701-D", before.GetProperty("AssignedShipRegistry").GetString());

        var body = JsonDocument.Parse("""{"Type":"Officer","AssignedShipRegistry":null}""").RootElement;
        var patch = new HttpRequestMessage(HttpMethod.Patch, $"/personnel/{created.Id}")
        {
            Content = JsonContent.Create(body),
        };
        var response = await client.SendAsync(patch);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var after = await client.GetFromJsonAsync<JsonElement>($"/personnel/{created.Id}");
        Assert.Equal(JsonValueKind.Null, after.GetProperty("AssignedShipRegistry").ValueKind);
    }

    [Fact]
    public async Task Update_returns_404_when_target_missing()
    {
        using var client = fixture.CreateScopedClient();
        var body = JsonDocument.Parse("""
        {
            "Type":"Officer",
            "Name":"Ghost",
            "SerialNumber":"X",
            "Rank":"Captain",
            "Position":"X",
            "CommissionDate":0
        }
        """).RootElement;
        var response = await client.PutAsJsonAsync("/personnel/9999", body);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Patch_returns_404_when_target_missing()
    {
        using var client = fixture.CreateScopedClient();
        var body = JsonDocument.Parse("""{"Type":"Officer","Name":"X"}""").RootElement;
        var request = new HttpRequestMessage(HttpMethod.Patch, "/personnel/9999")
        {
            Content = JsonContent.Create(body),
        };
        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_removes_personnel_and_is_idempotent()
    {
        using var client = fixture.CreateScopedClient();
        var target = await CreateOfficer(client, "Delete Target", "SC-DEL-001");

        var first = await client.DeleteAsync($"/personnel/{target.Id}");
        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);

        var second = await client.DeleteAsync($"/personnel/{target.Id}");
        Assert.Equal(HttpStatusCode.NoContent, second.StatusCode);

        var get = await client.GetAsync($"/personnel/{target.Id}");
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
    }

    [Fact]
    public async Task Delete_returns_204_for_non_numeric_id()
    {
        using var client = fixture.CreateScopedClient();
        var response = await client.DeleteAsync("/personnel/not-a-number");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Roundtrip_get_put_get_is_byte_identical()
    {
        using var client = fixture.CreateScopedClient();
        var picard = await GetByName(client, "Jean-Luc Picard");
        var before = await client.GetFromJsonAsync<JsonElement>($"/personnel/{picard.Id}");

        var body = new System.Text.Json.Nodes.JsonObject
        {
            ["Type"] = before.GetProperty("Type").GetString(),
            ["Name"] = before.GetProperty("Name").GetString(),
            ["SerialNumber"] = before.GetProperty("SerialNumber").GetString(),
            ["Rank"] = before.GetProperty("Rank").GetString(),
            ["Position"] = before.GetProperty("Position").GetString(),
            ["EnlistmentDate"] = null,
            ["CommissionDate"] = before.GetProperty("CommissionDate").GetDecimal(),
            ["AssignedShipRegistry"] = before.GetProperty("AssignedShipRegistry").GetString(),
        };
        var put = await client.PutAsJsonAsync($"/personnel/{picard.Id}", body);
        Assert.Equal(HttpStatusCode.NoContent, put.StatusCode);

        var after = await client.GetFromJsonAsync<JsonElement>($"/personnel/{picard.Id}");
        Assert.Equal(before.GetRawText(), after.GetRawText());
    }

    [Fact]
    public async Task Create_returns_400_when_discriminator_missing()
    {
        using var client = fixture.CreateScopedClient();
        var body = JsonDocument.Parse("""{"Name":"Mystery"}""").RootElement;
        var response = await client.PostAsJsonAsync("/personnel", body);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains("discriminator", problem.GetProperty("error").GetString());
    }

    [Fact]
    public async Task Create_returns_400_for_unknown_discriminator()
    {
        using var client = fixture.CreateScopedClient();
        var body = JsonDocument.Parse("""{"Type":"Klingon","Name":"Kahless"}""").RootElement;
        var response = await client.PostAsJsonAsync("/personnel", body);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_returns_400_for_invalid_enum_value()
    {
        using var client = fixture.CreateScopedClient();
        var body = JsonDocument.Parse("""
        {
            "Type":"Officer","Name":"X","SerialNumber":"X",
            "Rank":"GrandPoobah","Position":"X","CommissionDate":1
        }
        """).RootElement;
        var response = await client.PostAsJsonAsync("/personnel", body);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static async Task<(int Id, string Name)> GetByName(HttpClient client, string name)
    {
        var list = await client.GetFromJsonAsync<JsonElement>("/personnel");
        var item = list.EnumerateArray().Single(e => e.GetProperty("Name").GetString() == name);
        return (item.GetProperty("Id").GetInt32(), name);
    }

    private static async Task<(int Id, string Name)> CreateOfficer(
        HttpClient client,
        string name,
        string serial,
        string rank = "Lieutenant")
    {
        var body = JsonDocument.Parse($$"""
        {
            "Type":"Officer",
            "Name":"{{name}}",
            "SerialNumber":"{{serial}}",
            "Rank":"{{rank}}",
            "Position":"Test",
            "EnlistmentDate":null,
            "CommissionDate":50000,
            "AssignedShipRegistry":"NCC-1701-D"
        }
        """).RootElement;
        var response = await client.PostAsJsonAsync("/personnel", body);
        response.EnsureSuccessStatusCode();
        var path = response.Headers.Location!.ToString();
        var id = int.Parse(path.Split('/')[^1]);
        return (id, name);
    }
}
