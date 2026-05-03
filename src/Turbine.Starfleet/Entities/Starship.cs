using System.ComponentModel.DataAnnotations;

namespace Turbine.Starfleet.Entities;

public sealed class Starship
{
    [Key]
    [StringLength(10, MinimumLength = 8)]
    public string Registry { get; set; } = null!;

    [StringLength(100, MinimumLength = 1)]
    public string Name { get; set; } = null!;

    public List<MissionDeployment> Deployments { get; set; } = [];
}
