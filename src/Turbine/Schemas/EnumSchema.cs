using System.Text.Json;

namespace Turbine;

public class EnumSchema<TEnum> : IValueTypeSchema<TEnum>
    where TEnum : struct, Enum
{
    internal EnumSchema() { }

    public bool? Nullable { get; set; }

    public TEnum FromJson(JsonElement json)
    {
        return (TEnum) SchemaBinder.NodeToValue(this, json, typeof(TEnum))!;
    }

    public JsonElement ToJson(TEnum value)
    {
        return SchemaBinder.NodeToElement(SchemaBinder.ValueToNode(this, value));
    }
}
