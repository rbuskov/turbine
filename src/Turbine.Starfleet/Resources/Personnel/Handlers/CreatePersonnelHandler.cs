using System.Text.Json;
using Turbine;
using Turbine.Starfleet.Database;

namespace Turbine.Starfleet.Resources.Personnel.Handlers;

public interface ICreatePersonnelHandler
{
    Task<IResult> Create(JsonElement body);
}

public class CreatePersonnelHandler(PersonnelSchemas schemas, StarfleetDbContext db) : ICreatePersonnelHandler
{
    public async Task<IResult> Create(JsonElement body)
    {
        Entities.Personnel personnel;
        try
        {
            personnel = schemas.Create.FromJson(body);
        }
        catch (TurbineBindingException ex)
        {
            return TypedResults.BadRequest(new { error = ex.Message });
        }

        await db.Personnel.AddAsync(personnel);
        await db.SaveChangesAsync();

        return TypedResults.Created($"/personnel/{personnel.Id}");
    }
}
