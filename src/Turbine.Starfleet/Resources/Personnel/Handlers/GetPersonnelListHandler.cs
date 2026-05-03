using Microsoft.EntityFrameworkCore;
using Turbine.Starfleet.Database;

namespace Turbine.Starfleet.Resources.Personnel.Handlers;

public interface IGetPersonnelListHandler
{
    Task<IResult> GetList();
}

public class GetPersonnelListHandler(PersonnelSchemas schemas, StarfleetDbContext db) : IGetPersonnelListHandler
{
    public async Task<IResult> GetList()
    {
        var personnel = await db
            .Personnel
            .Include(p => p.AssignedShip)
            .AsNoTracking()
            .ToListAsync();

        return TypedResults.Ok(personnel.Select(p => schemas.Summary.ToJson(p)));
    }
}
