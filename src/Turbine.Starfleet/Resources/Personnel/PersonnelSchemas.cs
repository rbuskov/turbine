#nullable disable

using Turbine;
using Turbine.Starfleet.Entities;

namespace Turbine.Starfleet.Resources.Personnel;

public class PersonnelSchemas : SchemaConfiguration
{
    public OneOfSchema<Entities.Personnel> Details { get; set; } = null!;
    public ObjectSchema<Entities.Personnel> Summary { get; set; } = null!;
    public OneOfSchema<Entities.Personnel> Create { get; set; } = null!;
    public ObjectSchema<Entities.Personnel> CreateResult { get; set; } = null!;
    public OneOfSchema<Entities.Personnel> Update { get; set; } = null!;
    public OneOfSchema<Entities.Personnel> Patch { get; set; } = null!;
    
    private ObjectSchema<ServiceMember> ServiceMemberDetails { get; set; } = null!;
    
    public override void Configure(SchemaConfigurator builder)
    {
        builder.Schema(() => Summary)
            .Add(p => p.Id)
            .Add(p => p.Name)
            .Add(p => p.EnteredServiceDate)
            .Add(p => p.AssignedShipRegistry!)
            .AddCustom("AssignedShipName", expr: p => p.AssignedShip?.Name);

        ConfigureDetails(builder);

        builder.Schema(() => Create)
            .AddMapping<Civilian>(schema: c =>
            {
                c.AddAtomicProperties();
                c.Remove(cs => cs.Id);
            })
            .AddMapping<Enlisted>(schema: e =>
            {
                e.AddAtomicProperties();
                e.Remove(cs => cs.Id);
            })
            .AddMapping<Officer>(schema: o =>
            {
                o.AddAtomicProperties();
                o.Remove(cs => cs.Id);
            });
        
        builder.Schema(() => CreateResult)
            .Add(p => p.Id);
        
        builder.Schema(() => Update).AddMappingsFrom(() => Create);
        builder.Schema(() => Patch).AddMappingsFrom(() => Create, asRequired: false);
    }

    private void ConfigureDetails(SchemaConfigurator builder)
    {
        builder.Schema(() => ServiceMemberDetails)
            .AddPropertiesFrom(() => Summary)
            .Add(s => s.SerialNumber)
            .AddArray(s => s.Commendations, itemSchema: s =>
            {
                s.Add(c => c.Name);
                s.Add(c => c.AwardedDate);
            });

        builder.Schema(() => Details)
            .AddMapping<Civilian>(schema: cs =>
            {
                cs.AddPropertiesFrom(() => Summary);
                cs.Add(c => c.Role);
                cs.Add(c => c.JoinedDate);
                cs.AddObject(c => c.SponsoringOfficer, schema: os =>
                {
                    os.Add(o => o.Name);
                    os.Add(o => o.Name);
                });
            })
            .AddMapping<Enlisted>(schema: s =>
            {
                s.AddPropertiesFrom(() => ServiceMemberDetails);
                s.Add(e => e.Rate);
                s.Add(e => e.Specialization);
                s.Add(e => e.EnlistmentDate);
            })
            .AddMapping<Officer>(schema: s =>
            {
                s.AddPropertiesFrom(() => ServiceMemberDetails);
                s.Add(o => o.Rank);
                s.Add(o => o.Position);
                s.Add(e => e.EnlistmentDate);
                s.Add(e => e.CommissionDate);
                s.AddArray(e => e.SponsoredCivilians, itemSchema: c =>
                {
                    c.Add(c => c.Id);
                    c.Add(c => c.Name);
                    c.Add(c => c.Role);
                });
            });
    }
}
