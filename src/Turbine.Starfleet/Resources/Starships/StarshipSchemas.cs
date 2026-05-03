#nullable disable

using Turbine;
using Turbine.Starfleet.Entities;

namespace Turbine.Starfleet.Resources.Starships;

public class StarshipSchemas : SchemaConfiguration
{
    public ObjectSchema<Starship> Details { get; set; } = null!;
    public ArraySchema<Starship> Summary { get; set; } = null!;
    public ObjectSchema<Starship> Create { get; set; } = null!;
    public ObjectSchema<Starship> Update { get; set; } = null!;
    public ObjectSchema<Starship> Patch { get; set; } = null!;

    public override void Configure(SchemaConfigurator builder)
    {
        builder.Schema(() => Details)
            .Add(s => s.Registry)
            .Add(s => s.Name)
            .AddArray(s => s.Deployments, name: "RecentDeployments", itemSchema: s =>
            {
                s.Add(d => d.MissionId);
                s.AddCustom("MissionName", expr: d => d.Mission.Name);
                s.AddCustom("StartDate", expr: d => d.StartDate);
                s.AddCustom("EndDate", expr: d => d.EndDate);
            })
            .AddCustom("TotalDeployments", expr: s => s.Deployments.Count);
        
        builder.Schema(() => Summary)
            .Add(s => s.Registry)
            .Add(s => s.Name);

        builder.Schema(() => Create)
            .Add(s => s.Registry, schema: s => s.Pattern("^(NCC|NX|NAR|NCV|ECS)-\\d{1,5}(-[A-J])?$"))
            .Add(s => s.Name, schema: s =>
            {
                s.MinLength(1);
                s.MaxLength(50);
            });
        
        builder.Schema(() => Update).AddPropertiesFrom(() => Create);
        builder.Schema(() => Patch).AddPropertiesFrom(() => Create, asRequired: false);
    }
}
