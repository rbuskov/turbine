using Turbine.Starfleet.Entities;

namespace Turbine.Starfleet.Database;

public static class Seeder
{
    public static WebApplication SeedDatabase(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<StarfleetDbContext>();
        db.Database.EnsureCreated();

        var enterprise = new Starship
        {
            Name = "Enterprise",
            Registry = "NCC-1701-D",
        };

        var picard = new Officer
        {
            Id = 0,
            Name = "Jean-Luc Picard",
            SerialNumber = "SP-1701-D-1",
            Rank = OfficerRank.Captain,
            Position = "Commanding Officer",
            EnlistmentDate = null,
            CommissionDate = 10000,
            AssignedShip = enterprise,
        };

        var worf = new Officer
        {
            Id = 0,
            Name = "Worf",
            SerialNumber = "SC-231-454",
            Rank = OfficerRank.Lieutenant,
            Position = "Chief of Security",
            EnlistmentDate = null,
            CommissionDate = 41000,
            AssignedShip = enterprise,
        };

        var data = new Officer
        {
            Id = 0,
            Name = "Data",
            SerialNumber = "SC-336-491",
            Rank = OfficerRank.LieutenantCommander,
            Position = "Operations Officer",
            EnlistmentDate = null,
            CommissionDate = 22000,
            AssignedShip = enterprise,
        };

        var obrien = new Enlisted
        {
            Id = 0,
            Name = "Miles O'Brien",
            SerialNumber = "TE-3477-2",
            Rate = EnlistedRate.ChiefPettyOfficer,
            Specialization = "Transporter Chief",
            EnlistmentDate = 23000,
            AssignedShip = enterprise,
        };

        var keiko = new Civilian
        {
            Id = 0,
            Name = "Keiko O'Brien",
            Role = "Botanist",
            JoinedDate = 43000,
            SponsoringOfficer = picard,
            AssignedShip = enterprise,
        };

        var alexander = new Civilian
        {
            Id = 0,
            Name = "Alexander Rozhenko",
            Role = "Family",
            JoinedDate = 44000,
            SponsoringOfficer = worf,
            AssignedShip = enterprise,
        };

        var deepSpaceExploration = new Mission
        {
            Name = "Deep Space Exploration",
        };

        var enterpriseDeepSpace = new MissionDeployment
        {
            Mission = deepSpaceExploration,
            Starship = enterprise,
            StarshipRegistry = enterprise.Registry,
            StartDate = 41000,
            EndDate = null,
        };

        db.Starships.Add(enterprise);
        db.Personnel.AddRange(picard, worf, data, obrien, keiko, alexander);
        db.Missions.Add(deepSpaceExploration);
        db.MissionDeployments.Add(enterpriseDeepSpace);
        db.SaveChanges();

        return app;
    }
}
