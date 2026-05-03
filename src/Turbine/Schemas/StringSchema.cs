using System.Text.Json;
using System.Text.RegularExpressions;

namespace Turbine;

public class StringSchema : IValueTypeSchema<string>
{
    public int? MinLength { get; set; }
    public int? MaxLength { get; set; }
    public string? Format { get; set; } 
    public Regex? Pattern { get; set; }

    public string FromJson(JsonElement json)
    {
        throw new NotImplementedException();
    }
    
    public JsonElement ToJson(string value)
    {
        throw new NotImplementedException();
    }
}