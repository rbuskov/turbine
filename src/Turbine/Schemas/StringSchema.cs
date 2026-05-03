using System.Text.Json;
using System.Text.RegularExpressions;

namespace Turbine;

public class StringSchema : IValueTypeSchema<string>
{
    internal StringSchema() { }

    public bool? Nullable { get; set; }
    public int? MinLength { get; set; }
    public int? MaxLength { get; set; }
    public string? Format { get; set; }
    public Regex? Pattern { get; set; }

    public string FromJson(JsonElement json)
    {
        return (string?) SchemaBinder.NodeToValue(this, json, typeof(string)) ?? string.Empty;
    }

    public JsonElement ToJson(string value)
    {
        return SchemaBinder.NodeToElement(SchemaBinder.ValueToNode(this, value));
    }
}
