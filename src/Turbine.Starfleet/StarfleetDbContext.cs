using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Turbine.Starfleet.Entities;
using Turbine.Starfleet.Types;

namespace Turbine.Starfleet.Database;

public sealed class StarfleetDbContext(DbContextOptions<StarfleetDbContext> options) : DbContext(options)
{
    public DbSet<Personnel> Personnel { get; set; } = null!;
    public DbSet<Civilian> Civilians { get; set; } = null!;
    public DbSet<ServiceMember> ServiceMembers { get; set; } = null!;
    public DbSet<Enlisted> EnlistedPersonnel { get; set; } = null!;
    public DbSet<Officer> Officers { get; set; } = null!;
    public DbSet<Commendation> Commendations { get; set; } = null!;
    public DbSet<Starship> Starships { get; set; } = null!;
    public DbSet<Mission> Missions { get; set; } = null!;
    public DbSet<MissionDeployment> MissionDeployments { get; set; } = null!;

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<Stardate>()
            .HaveConversion<StardateToDecimalConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Personnel>()
            .HasDiscriminator<string>("Type")
            .HasValue<Civilian>("Civilian")
            .HasValue<Officer>("Officer")
            .HasValue<Enlisted>("Enlisted");

        modelBuilder.Entity<ServiceMember>()
            .HasIndex(sm => sm.SerialNumber)
            .IsUnique();

        modelBuilder.Entity<Officer>()
            .Property(o => o.Rank)
            .HasConversion<string>()
            .HasMaxLength(50);

        modelBuilder.Entity<Enlisted>()
            .Property(e => e.Rate)
            .HasConversion<string>()
            .HasMaxLength(50);

        modelBuilder.Entity<Civilian>()
            .HasOne(c => c.SponsoringOfficer)
            .WithMany(o => o.SponsoredCivilians)
            .HasForeignKey(c => c.SponsoringOfficerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Commendation>()
            .HasOne(c => c.ServiceMember)
            .WithMany(sm => sm.Commendations)
            .HasForeignKey(c => c.ServiceMemberId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<MissionDeployment>()
            .HasOne(d => d.Mission)
            .WithMany(m => m.Deployments)
            .HasForeignKey(d => d.MissionId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<MissionDeployment>()
            .HasOne(d => d.Starship)
            .WithMany(s => s.Deployments)
            .HasForeignKey(d => d.StarshipRegistry)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private sealed class StardateToDecimalConverter() : ValueConverter<Stardate, decimal>(
        s => (decimal)s.Value,
        d => new Stardate((double)d));
}
