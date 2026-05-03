using System.ComponentModel.DataAnnotations;

namespace Turbine.Starfleet.Entities;

public sealed class Mission
{
    public int Id { get; set; }

    [StringLength(100, MinimumLength = 1)]
    public string Name { get; set; } = null!;

    public List<MissionDeployment> Deployments { get; set; } = [];
}
