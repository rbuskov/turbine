using Turbine.Starfleet.Database;

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

        var personnel = await db.Personnel.FindAsync(personnelId);

        return personnel == null
            ? TypedResults.NotFound()
            : TypedResults.Ok(schemas.Details.ToJson(personnel));
    }
}
