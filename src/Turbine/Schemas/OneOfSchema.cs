using System.Text.Json;

namespace Turbine;

public class OneOfSchema<TBase> : IReferenceTypeSchema<TBase>
{
    private readonly Dictionary<string, IObjectSchema> mappings = new();

    internal OneOfSchema() { }

    public bool? Nullable { get; set; }

    public string Discriminator { get; set; } = "Type";

    internal IReadOnlyDictionary<string, IObjectSchema> Mappings => mappings;

    public void AddSchema<TMap>(string type, ObjectSchema<TMap> schema) where TMap : TBase
    {
        mappings.Add(type, schema);
    }

    internal void AddMapping(string type, IObjectSchema schema)
    {
        mappings.Add(type, schema);
    }

    public TBase FromJson(JsonElement json)
    {
        if (json.ValueKind == JsonValueKind.Null || json.ValueKind == JsonValueKind.Undefined)
        {
            return default!;
        }
        return (TBase) SchemaBinder.NodeToValue(this, json, typeof(TBase))!;
    }

    public void FromJson(JsonElement json, TBase value)
    {
        if (value is null)
        {
            return;
        }
        var (mapping, _, _) = SchemaBinder.ResolveOneOfMapping(this, value.GetType().Name);
        if (mapping is null)
        {
            throw new InvalidOperationException(
                $"OneOf has no mapping for runtime type '{value.GetType().Name}'.");
        }
        SchemaBinder.PopulateInstance(mapping, json, value);
    }

    public JsonElement ToJson(TBase instance)
    {
        if (instance is null)
        {
            return SchemaBinder.NodeToElement(null);
        }
        return SchemaBinder.NodeToElement(SchemaBinder.ValueToNode(this, instance));
    }
}
