using System.Text.Json;

namespace Turbine;

public class DateTimeOffsetSchema : IValueTypeSchema<DateTimeOffset>
{
    internal DateTimeOffsetSchema() { }

    public bool? Nullable { get; set; }

    public DateTimeOffset FromJson(JsonElement json)
    {
        return (DateTimeOffset) SchemaBinder.NodeToValue(this, json, typeof(DateTimeOffset))!;
    }

    public JsonElement ToJson(DateTimeOffset value)
    {
        return SchemaBinder.NodeToElement(SchemaBinder.ValueToNode(this, value));
    }
}
