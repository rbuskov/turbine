using System.Numerics;
using System.Text.Json;

namespace Turbine;

public class NumericSchema<TNumber> : IValueTypeSchema<TNumber>
    where TNumber : struct, INumber<TNumber>
{
    internal NumericSchema() { }

    public bool? Nullable { get; set; }
    public TNumber? Minimum { get; set; }
    public TNumber? ExclusiveMinimum { get; set; }
    public TNumber? Maximum { get; set; }
    public TNumber? ExclusiveMaximum { get; set; }
    public TNumber? MultipleOf { get; set; }

    public TNumber FromJson(JsonElement json)
    {
        return (TNumber) SchemaBinder.NodeToValue(this, json, typeof(TNumber))!;
    }

    public JsonElement ToJson(TNumber value)
    {
        return SchemaBinder.NodeToElement(SchemaBinder.ValueToNode(this, value));
    }
}
