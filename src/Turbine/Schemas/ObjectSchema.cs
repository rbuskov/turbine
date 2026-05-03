using System.Text.Json;

namespace Turbine;

public class ObjectSchema<TDomain> : IReferenceTypeSchema<TDomain>, IObjectSchema
{
    internal ObjectSchema() { }

    public IList<ObjectProperty> Properties { get; set; } = new List<ObjectProperty>();
    
    public TDomain FromJson(JsonElement json)
    {
        // Use Activator.CreateInstance<TDomain>();
        throw new NotImplementedException();
    }

    public void FromJson(JsonElement json, TDomain value)
    {
        throw new NotImplementedException();
    }

    public JsonElement ToJson(TDomain instance)
    {
        throw new NotImplementedException();
    }
}