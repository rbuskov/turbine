using System.Text.Json;

namespace Turbine;

public class EnumSchema<TEnum> : IValueTypeSchema<TEnum>
    where TEnum : struct, Enum
{
    internal EnumSchema() { }

    public bool? Nullable { get; set; }

    public TEnum FromJson(JsonElement json)
    {
        throw new NotImplementedException();
    }
    
    public JsonElement ToJson(TEnum value)
    {
        throw new NotImplementedException();
    }
}