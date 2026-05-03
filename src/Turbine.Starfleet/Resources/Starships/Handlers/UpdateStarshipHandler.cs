using System.Text.Json;
using Turbine.Starfleet.Database;

namespace Turbine.Starfleet.Resources.Starships.Handlers;

public interface IUpdateStarshipHandler
{
    Task<IResult> Update(string registry, JsonElement body);
}

public class UpdateStarshipHandler(StarshipSchemas schemas, StarfleetDbContext db) : IUpdateStarshipHandler
{
    public async Task<IResult> Update(string registry, JsonElement body)
    {
        var starship = await db.Starships.FindAsync(registry);

        if (starship == null)
        {
            return TypedResults.NotFound();
        }
        
        schemas.Update.FromJson(body, starship);
        
        await db.SaveChangesAsync();
        
        return TypedResults.NoContent();
    }
}