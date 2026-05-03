using System.ComponentModel.DataAnnotations;

namespace Turbine.Starfleet.Entities;

public sealed class Enlisted : ServiceMember
{
    public EnlistedRate Rate { get; set; }

    [StringLength(100, MinimumLength = 1)]
    public string Specialization { get; set; } = null!;

    public decimal EnlistmentDate { get; set; }

    public override decimal EnteredServiceDate => EnlistmentDate;
}
