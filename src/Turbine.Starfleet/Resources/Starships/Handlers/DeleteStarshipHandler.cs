using Turbine.Starfleet.Database;

namespace Turbine.Starfleet.Resources.Starships.Handlers;

public interface IDeleteStarshipHandler
{
    Task<IResult> Delete(string registry);
}

public class DeleteStarshipHandler(StarfleetDbContext db) : IDeleteStarshipHandler
{
    public async Task<IResult> Delete(string registry)
    {
        var starship = await db.Starships.FindAsync(registry);

        if (starship != null)
        {
            db.Starships.Remove(starship);
            await db.SaveChangesAsync();
            
        }
        return TypedResults.NoContent();
    }
}