using System.Text.Json;
using Turbine.Starfleet.Database;

namespace Turbine.Starfleet.Resources.Personnel.Handlers;

public interface IPatchPersonnelHandler
{
    Task<IResult> Patch(string id, JsonElement body);
}

public class PatchPersonnelHandler(PersonnelSchemas schemas, StarfleetDbContext db) : IPatchPersonnelHandler
{
    public async Task<IResult> Patch(string id, JsonElement body)
    {
        if (!int.TryParse(id, out var personnelId))
        {
            return TypedResults.NotFound();
        }

        var personnel = await db.Personnel.FindAsync(personnelId);

        if (personnel == null)
        {
            return TypedResults.NotFound();
        }

        schemas.Patch.FromJson(body, personnel);

        await db.SaveChangesAsync();

        return TypedResults.NoContent();
    }
}
