using System.Text.Json;

namespace Turbine;

public class BooleanSchema : IValueTypeSchema<bool>
{
    internal BooleanSchema() { }

    public bool? Nullable { get; set; }

    public bool FromJson(JsonElement json)
    {
        throw new NotImplementedException();
    }

    public JsonElement ToJson(bool value)
    {
        throw new NotImplementedException();
    }

}