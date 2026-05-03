using Turbine.Starfleet.Database;

namespace Turbine.Starfleet.Resources.Starships.Handlers;

public interface IGetStarshipHandler
{
    Task<IResult> Get(string registry);
}

public class GetStarshipHandler(StarshipSchemas schemas, StarfleetDbContext db) : IGetStarshipHandler
{
    public async Task<IResult> Get(string registry)
    {
        var starship = await db.Starships.FindAsync(registry);
        
        return starship == null
            ? TypedResults.NotFound()
            : TypedResults.Ok(schemas.Details.ToJson(starship));
    }
}