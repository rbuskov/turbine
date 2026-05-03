using System.ComponentModel.DataAnnotations;

namespace Turbine.Starfleet.Entities;

public sealed class Officer : ServiceMember
{
    public OfficerRank Rank { get; set; }

    [StringLength(100, MinimumLength = 1)]
    public string Position { get; set; } = null!;

    public decimal? EnlistmentDate { get; set; }

    public decimal CommissionDate { get; set; }
    
    public override decimal EnteredServiceDate => EnlistmentDate ?? CommissionDate;
    
    public ICollection<Civilian> SponsoredCivilians { get; set; } = [];
}
