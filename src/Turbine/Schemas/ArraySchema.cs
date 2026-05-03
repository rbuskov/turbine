using System.Text.Json;

namespace Turbine;

public class ArraySchema<TItem> : IReferenceTypeSchema<ICollection<TItem>>
{
    internal ArraySchema() { }

    public int? MinItems { get; set; }
    public int? MaxItems { get; set; }

    public ICollection<TItem> FromJson(JsonElement json)
    {
        throw new NotImplementedException();
    }
    
    public void FromJson(JsonElement json, ICollection<TItem> value)
    {
        // Replace items in instance with new items from json
        throw new NotImplementedException();
    }

    public JsonElement ToJson(ICollection<TItem> items)
    {
        throw new NotImplementedException();
    }
}