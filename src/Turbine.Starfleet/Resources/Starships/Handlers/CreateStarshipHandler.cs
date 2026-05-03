using System.Text.Json;
using Turbine.Starfleet.Database;

namespace Turbine.Starfleet.Resources.Starships.Handlers;

public interface ICreateStarshipHandler
{
    Task<IResult> Create(JsonElement body);
}

public class CreateStarshipHandler(StarshipSchemas schemas, StarfleetDbContext db) : ICreateStarshipHandler
{
    public async Task<IResult> Create(JsonElement body)
    {
        var starship = schemas.Create.FromJson(body);
        
        await db.Starships.AddAsync(starship);
        await db.SaveChangesAsync();
        
        return TypedResults.Created($"/starships/{starship.Registry}");
    }
}