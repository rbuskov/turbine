using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Turbine.Starfleet.Resources.Personnel.Handlers;

namespace Turbine.Starfleet.Resources.Personnel;

public static class PersonnelEndpoints
{
    public static WebApplication MapPersonnelEndpoints(this WebApplication app)
    {
        var endpoints = app.MapGroup("personnel").WithTags("Personnel");

        endpoints
            .MapGet("", async ([FromServices] IGetPersonnelListHandler handler) => await handler.GetList())
            .Produces(200, (PersonnelSchemas personnel) => personnel.Summary);
        
        endpoints
            .MapGet("{id}", async (
                [FromRoute] string id, 
                [FromServices] IGetPersonnelHandler handler) => await handler.Get(id))
            .Produces(200, (PersonnelSchemas personnel) => personnel.Details);

        endpoints
            .MapPost("", async (
                [FromBody] JsonElement body, 
                [FromServices] ICreatePersonnelHandler handler) => await handler.Create(body))
            .Produces(201, (PersonnelSchemas personnel) => personnel.CreateResult);
        
        endpoints
            .MapPut("{id}", async (
                [FromRoute] string id, 
                [FromBody] JsonElement body, 
                [FromServices] IUpdatePersonnelHandler handler) => await handler.Update(id, body))
            .Produces(204);

        endpoints
            .MapPatch("{id}", async (
                [FromRoute] string id, 
                [FromBody] JsonElement body, 
                [FromServices] IPatchPersonnelHandler handler) => await handler.Patch(id, body))
            .Produces(204);

        endpoints
            .MapDelete("{id}", async (
                [FromRoute] string id, 
                [FromServices] IDeletePersonnelHandler handler) => await handler.Delete(id))
            .Produces(204);

        return app;
    }
}
