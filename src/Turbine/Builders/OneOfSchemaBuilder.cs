
namespace Turbine;

public class OneOfSchemaBuilder<TBase>
{
    internal OneOfSchemaBuilder() { }

    public OneOfSchemaBuilder<TBase> Nullable(bool? nullable)
    {
        return this;
    }
    
    public OneOfSchemaBuilder<TBase> Discriminator(string? discriminator)
    {
        return this;
    }
    
    public OneOfSchemaBuilder<TBase> AddMapping<TMap>(Action<ObjectSchemaBuilder<TMap>> schema)
        where TMap : TBase
    {
        return this;
    }
    
    public OneOfSchemaBuilder<TBase> AddMappingsFrom<TSource>(Func<OneOfSchema<TSource>> schema, bool? asRequired = null)
    {
        return this;
    }

}