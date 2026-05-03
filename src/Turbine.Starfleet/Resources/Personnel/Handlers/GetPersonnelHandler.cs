using Microsoft.EntityFrameworkCore;
using Turbine.Starfleet.Database;
using Turbine.Starfleet.Entities;

namespace Turbine.Starfleet.Resources.Personnel.Handlers;

public interface IGetPersonnelHandler
{
    Task<IResult> Get(string id);
}

public class GetPersonnelHandler(PersonnelSchemas schemas, StarfleetDbContext db) : IGetPersonnelHandler
{
    public async Task<IResult> Get(string id)
    {
        if (!int.TryParse(id, out var personnelId))
        {
            return TypedResults.NotFound();
        }

        var personnel = await db
            .Personnel
            .Include(p => p.AssignedShip)
            .Include(p => ((Civilian) p).SponsoringOfficer)
            .Include(p => ((ServiceMember) p).Commendations)
            .Include(p => ((Officer) p).SponsoredCivilians)
            .SingleOrDefaultAsync(p => p.Id == personnelId);

        return personnel == null
            ? TypedResults.NotFound()
            : TypedResults.Ok(schemas.Details.ToJson(personnel));
    }
}
