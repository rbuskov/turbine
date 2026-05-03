using Microsoft.EntityFrameworkCore;
using Turbine.Starfleet.Database;

namespace Turbine.Starfleet.Resources.Starships.Handlers;

public interface IGetStarshipListHandler
{
    Task<IResult> GetList();
}

public class GetStarshipListHandler(StarshipSchemas schemas, StarfleetDbContext db) : IGetStarshipListHandler
{
    public async Task<IResult> GetList()
    {
        var starships = await db.Starships.AsNoTracking().ToListAsync();

        return TypedResults.Ok(schemas.Summary.ToJson(starships));
    }
}