using System.ComponentModel.DataAnnotations;
using Turbine.Starfleet.Types;

namespace Turbine.Starfleet.Entities;

public sealed class Commendation
{
    public int Id { get; set; }

    [StringLength(100, MinimumLength = 1)]
    public required string Name { get; set; } = null!;

    public decimal AwardedDate { get; set; }

    [StringLength(1000, MinimumLength = 100)]
    public string Citation { get; set; } = null!;

    public int ServiceMemberId { get; set; }

    public ServiceMember ServiceMember { get; set; } = null!;
}
