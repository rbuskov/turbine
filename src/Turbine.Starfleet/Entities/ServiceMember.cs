using System.ComponentModel.DataAnnotations;

namespace Turbine.Starfleet.Entities;

public abstract class ServiceMember : Personnel
{
    [StringLength(100, MinimumLength = 1)]
    public string SerialNumber { get; set; } = null!;

    public List<Commendation> Commendations { get; set; } = [];
}
