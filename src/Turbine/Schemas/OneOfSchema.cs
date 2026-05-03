using System.Text.Json;

namespace Turbine;

public class OneOfSchema<TBase> : IReferenceTypeSchema<TBase>
{
    private readonly Dictionary<string, IObjectSchema> mappings = new();

    internal OneOfSchema() { }

    public string Discriminator { get; set; } = "Type";

    public void AddSchema<TMap>(string type, ObjectSchema<TMap> schema) where TMap : TBase, new()
    {
        mappings.Add(type, schema);
    }
    
    public TBase FromJson(JsonElement json)
    {
        // Todo: Resolve schema from discriminator value in json
        throw new NotImplementedException();
    }

    public void FromJson(JsonElement json, TBase value)
    {
        // Todo: Resolve schema from discriminator value in json
        throw new NotImplementedException();
    }

    public JsonElement ToJson(TBase instance)
    {
        throw new NotImplementedException();
    }
}