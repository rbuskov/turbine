using System.Text.Json;

namespace Turbine;

public class ArraySchema<TItem> : IReferenceTypeSchema<ICollection<TItem>>
{
    internal ArraySchema() { }

    public bool? Nullable { get; set; }
    public int? MinItems { get; set; }
    public int? MaxItems { get; set; }
    public ObjectSchema<TItem>? Items { get; set; }

    public ICollection<TItem> FromJson(JsonElement json)
    {
        var raw = SchemaBinder.NodeToValue(this, json, typeof(ICollection<TItem>));
        return raw switch
        {
            null => new List<TItem>(),
            ICollection<TItem> typed => typed,
            System.Collections.IEnumerable enumerable => MaterializeFrom(enumerable),
            _ => throw new InvalidOperationException(
                $"Unexpected ArraySchema deserialization result of type {raw.GetType().FullName}."),
        };
    }

    public void FromJson(JsonElement json, ICollection<TItem> value)
    {
        ArgumentNullException.ThrowIfNull(value);
        value.Clear();
        foreach (var item in FromJson(json))
        {
            value.Add(item);
        }
    }

    public JsonElement ToJson(ICollection<TItem> items)
    {
        if (items is null)
        {
            return SchemaBinder.NodeToElement(null);
        }
        return SchemaBinder.NodeToElement(SchemaBinder.ValueToNode(this, items));
    }

    private static ICollection<TItem> MaterializeFrom(System.Collections.IEnumerable source)
    {
        var list = new List<TItem>();
        foreach (var item in source)
        {
            list.Add((TItem) item!);
        }
        return list;
    }
}
