using System.ComponentModel.DataAnnotations;

namespace Turbine.Starfleet.Entities;

public sealed class Civilian : Personnel
{
    [StringLength(100, MinimumLength = 1)] 
    public string Role { get; set; } = null!;
    
    public decimal JoinedDate { get; set; }
    
    public override decimal EnteredServiceDate => JoinedDate;
    
    public int? SponsoringOfficerId { get; set; }
    
    public Officer? SponsoringOfficer { get; set; }
}
