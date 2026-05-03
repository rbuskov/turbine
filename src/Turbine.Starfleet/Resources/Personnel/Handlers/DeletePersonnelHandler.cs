using Turbine.Starfleet.Database;

namespace Turbine.Starfleet.Resources.Personnel.Handlers;

public interface IDeletePersonnelHandler
{
    Task<IResult> Delete(string id);
}

public class DeletePersonnelHandler(StarfleetDbContext db) : IDeletePersonnelHandler
{
    public async Task<IResult> Delete(string id)
    {
        if (!int.TryParse(id, out var personnelId))
        {
            return TypedResults.NoContent();
        }

        var personnel = await db.Personnel.FindAsync(personnelId);

        if (personnel != null)
        {
            db.Personnel.Remove(personnel);
            await db.SaveChangesAsync();
        }

        return TypedResults.NoContent();
    }
}
