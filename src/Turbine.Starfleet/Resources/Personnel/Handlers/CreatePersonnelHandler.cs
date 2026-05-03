using System.Text.Json;
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
        var personnel = schemas.Create.FromJson(body);

        await db.Personnel.AddAsync(personnel);
        await db.SaveChangesAsync();

        return TypedResults.Created($"/personnel/{personnel.Id}");
    }
}
