using System.Text.Json;

namespace Turbine;

public class DateOnlySchema : IValueTypeSchema<DateOnly>
{
    internal DateOnlySchema() { }

    public bool? Nullable { get; set; }

    public DateOnly FromJson(JsonElement json)
    {
        return (DateOnly) SchemaBinder.NodeToValue(this, json, typeof(DateOnly))!;
    }

    public JsonElement ToJson(DateOnly value)
    {
        return SchemaBinder.NodeToElement(SchemaBinder.ValueToNode(this, value));
    }
}
