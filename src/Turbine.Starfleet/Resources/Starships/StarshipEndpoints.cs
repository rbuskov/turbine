using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Turbine.Starfleet.Resources.Starships.Handlers;

namespace Turbine.Starfleet.Resources.Starships;

public static class StarshipEndpoints
{
    public static WebApplication MapStarshipEndpoints(this WebApplication app)
    {
        var endpoints = app.MapGroup("starships").WithTags("Starship");

        endpoints
            .MapGet("", async ([FromServices] IGetStarshipListHandler handler) => await handler.GetList())
            .Produces(200, (StarshipSchemas starship) => starship.Summary);
        
        endpoints
            .MapGet("{registry}", async (
                [FromRoute] string registry, 
                [FromServices] IGetStarshipHandler handler) => await handler.Get(registry))
            .Produces(200, (StarshipSchemas starship) => starship.Details);

        endpoints
            .MapPost("", async (
                [FromBody] JsonElement body,
                [FromServices] ICreateStarshipHandler handler) => await handler.Create(body))
            .Accepts((StarshipSchemas starship) => starship.Create)
            .Produces(201);

        endpoints
            .MapPut("{registry}", async (
                [FromRoute] string registry,
                [FromBody] JsonElement body,
                [FromServices] IUpdateStarshipHandler handler) => await handler.Update(registry, body))
            .Accepts((StarshipSchemas starship) => starship.Update)
            .Produces(204);

        endpoints
            .MapPatch("{registry}", async (
                [FromRoute] string registry,
                [FromBody] JsonElement body,
                [FromServices] IPatchStarshipHandler handler) => await handler.Patch(registry, body))
            .Accepts((StarshipSchemas starship) => starship.Patch)
            .Produces(204);
        
        endpoints
            .MapDelete("{registry}", async (
                [FromRoute] string registry, 
                [FromServices] IDeleteStarshipHandler handler) => await handler.Delete(registry))
            .Produces(204);
        
        return app;
    }
}
