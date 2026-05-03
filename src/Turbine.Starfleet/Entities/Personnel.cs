using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Turbine.Starfleet.Entities;

public abstract class Personnel
{
    public int Id { get; set; }

    [StringLength(100, MinimumLength = 1)] 
    public string Name { get; set; } = null!;
   
    public abstract decimal EnteredServiceDate { get; }

    [ForeignKey(nameof(AssignedShip))]
    [StringLength(10, MinimumLength = 8), JsonIgnore]
    public string? AssignedShipRegistry { get; set; }
    
    public Starship? AssignedShip { get; set; }
}
