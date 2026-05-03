using System.Text.Json;

namespace Turbine;

public class DateTimeOffsetSchema : IValueTypeSchema<DateTimeOffset>
{
    internal DateTimeOffsetSchema() { }

    public bool? Nullable { get; set; }

    public DateTimeOffset FromJson(JsonElement json)
    {
        throw new NotImplementedException();
    }
    
    public JsonElement ToJson(DateTimeOffset value)
    {
        throw new NotImplementedException();
    }
}