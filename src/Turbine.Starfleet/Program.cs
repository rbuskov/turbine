using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Turbine;
using Turbine.Starfleet.Database;
using Turbine.Starfleet.Resources.Personnel;
using Turbine.Starfleet.Resources.Starships;
using Turbine.Starfleet.Types;

var builder = WebApplication.CreateBuilder(args);

SqliteConnection connection = new("Data Source=:memory:");
connection.Open();
builder.Services.AddSingleton(connection);
builder.Services.AddDbContext<StarfleetDbContext>(options => options.UseSqlite(connection));

builder.Services.AddSingleton<TimeProvider>(new ScienceFictionTimeProvider(TimeProvider.System));
builder.Services.Scan(scan =>
    scan.FromAssemblyOf<Program>()
        .AddClasses(classes => classes.Where(c => c.Namespace?.EndsWith(".Handlers", StringComparison.Ordinal) ?? false))
        .AsImplementedInterfaces()
        .WithScopedLifetime());

builder.Services.AddOpenApi();
builder.Services.AddTurbine(typeof(Program).Assembly);

var app = builder.Build();

app.SeedDatabase();

app.MapPersonnelEndpoints();
app.MapStarshipEndpoints();
app.MapTurbine();
app.MapOpenApi();
app.MapScalarApiReference();

app.MapGet("/stardate", ([FromServices] TimeProvider clock)
        => TypedResults.Ok(Stardate.FromDateTimeOffset(clock.GetUtcNow()).RoundedValue))
    .Produces<decimal>()
    .WithTags("Stardate");

app.Run();

namespace Turbine.Starfleet
{
    public partial class Program;
}
