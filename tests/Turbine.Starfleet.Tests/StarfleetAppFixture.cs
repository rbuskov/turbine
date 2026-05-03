using Microsoft.AspNetCore.Mvc.Testing;
using Turbine.Starfleet;

namespace Turbine.StarfleetApi.Tests;

/// <summary>
/// xUnit class fixture that hands out an isolated <see cref="HttpClient"/> per test —
/// each call spins up its own <see cref="WebApplicationFactory{TEntryPoint}"/>, so the
/// in-memory SQLite database is reseeded fresh and tests cannot bleed into each other.
/// </summary>
public sealed class StarfleetAppFixture : IDisposable
{
    private readonly List<WebApplicationFactory<Program>> factories = [];

    public HttpClient CreateScopedClient()
    {
        var factory = new WebApplicationFactory<Program>();
        factories.Add(factory);
        return factory.CreateClient();
    }

    public void Dispose()
    {
        foreach (var factory in factories)
        {
            factory.Dispose();
        }
    }
}
