using System.Numerics;
using System.Text.Json;

namespace Turbine;

public class NumericSchema<TNumber> : IValueTypeSchema<TNumber>
    where TNumber : struct, INumber<TNumber>
{
    public TNumber? Minimum { get; set; }
    public TNumber? Maximum { get; set; }
    public TNumber? MultipleOf { get; set; }

    public TNumber FromJson(JsonElement json)
    {
        throw new NotImplementedException();
    }
    
    public JsonElement ToJson(TNumber value)
    {
        throw new NotImplementedException();
    }
}