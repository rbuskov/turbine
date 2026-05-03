using System.Text.Json;

namespace Turbine;

public class EnumSchema<TEnum> : IValueTypeSchema<TEnum> 
    where TEnum : struct, Enum
{
    public TEnum FromJson(JsonElement json)
    {
        throw new NotImplementedException();
    }
    
    public JsonElement ToJson(TEnum value)
    {
        throw new NotImplementedException();
    }
}