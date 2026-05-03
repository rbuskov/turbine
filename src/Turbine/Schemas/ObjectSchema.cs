using System.Text.Json;

namespace Turbine;

public class ObjectSchema<TDomain> : IReferenceTypeSchema<TDomain>, IObjectSchema
{
    internal ObjectSchema() { }

    public IList<ObjectProperty> Properties { get; set; } = new List<ObjectProperty>();

    public TDomain FromJson(JsonElement json)
    {
        if (json.ValueKind == JsonValueKind.Null || json.ValueKind == JsonValueKind.Undefined)
        {
            return default!;
        }
        var instance = (TDomain) (object?)
            (Activator.CreateInstance(typeof(TDomain), nonPublic: true)
             ?? Activator.CreateInstance<TDomain>())!;
        FromJson(json, instance);
        return instance;
    }

    public void FromJson(JsonElement json, TDomain value)
    {
        if (value is null)
        {
            return;
        }
        SchemaBinder.PopulateInstance(this, json, value);
    }

    public JsonElement ToJson(TDomain instance)
    {
        if (instance is null)
        {
            return SchemaBinder.NodeToElement(null);
        }
        return SchemaBinder.NodeToElement(SchemaBinder.ValueToNode(this, instance));
    }
}
