using System.Text.Json;
using Turbine.Starfleet.Database;

namespace Turbine.Starfleet.Resources.Starships.Handlers;

public interface IPatchStarshipHandler
{
    Task<IResult> Patch(string registry, JsonElement body);
}

public class PatchStarshipHandler(StarshipSchemas schemas, StarfleetDbContext db) : IPatchStarshipHandler
{
    public async Task<IResult> Patch(string registry, JsonElement body)
    {
        var starship = await db.Starships.FindAsync(registry);

        if (starship == null)
        {
            return TypedResults.NotFound();
        }
        
        schemas.Patch.FromJson(body, starship);
        
        await db.SaveChangesAsync();
        
        return TypedResults.NoContent();
    }
}