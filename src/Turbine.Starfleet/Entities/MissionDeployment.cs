using System.ComponentModel.DataAnnotations;

namespace Turbine.Starfleet.Entities;

public sealed class MissionDeployment
{
    public int Id { get; set; }

    public int MissionId { get; set; }

    public Mission Mission { get; set; } = null!;

    [StringLength(10, MinimumLength = 8)]
    public string StarshipRegistry { get; set; } = null!;
    
    public Starship Starship { get; set; } = null!;

    public decimal StartDate { get; set; }

    public decimal? EndDate { get; set; }
}
