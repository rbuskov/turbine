using System.Text.Json;

namespace Turbine;

public class DateOnlySchema : IValueTypeSchema<DateOnly>
{
    public DateOnly FromJson(JsonElement json)
    {
        throw new NotImplementedException();
    }
    
    public JsonElement ToJson(DateOnly value)
    {
        throw new NotImplementedException();
    }
}