using System.Text.Json;

namespace Turbine;

public class BooleanSchema : IValueTypeSchema<bool>
{
    internal BooleanSchema() { }

    public bool? Nullable { get; set; }

    public bool FromJson(JsonElement json)
    {
        return (bool) (SchemaBinder.NodeToValue(this, json, typeof(bool)) ?? false);
    }

    public JsonElement ToJson(bool value)
    {
        return SchemaBinder.NodeToElement(SchemaBinder.ValueToNode(this, value));
    }
}
