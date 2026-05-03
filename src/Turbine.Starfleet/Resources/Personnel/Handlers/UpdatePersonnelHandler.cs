using System.Text.Json;
using Turbine;
using Turbine.Starfleet.Database;

namespace Turbine.Starfleet.Resources.Personnel.Handlers;

public interface IUpdatePersonnelHandler
{
    Task<IResult> Update(string id, JsonElement body);
}

public class UpdatePersonnelHandler(PersonnelSchemas schemas, StarfleetDbContext db) : IUpdatePersonnelHandler
{
    public async Task<IResult> Update(string id, JsonElement body)
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

        try
        {
            schemas.Update.FromJson(body, personnel);
        }
        catch (TurbineBindingException ex)
        {
            return TypedResults.BadRequest(new { error = ex.Message });
        }

        await db.SaveChangesAsync();

        return TypedResults.NoContent();
    }
}
