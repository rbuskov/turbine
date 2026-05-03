namespace Turbine;

public class OneOfSchemaBuilder<TBase>
{
    internal OneOfSchema<TBase> Schema { get; }

    internal OneOfSchemaBuilder(OneOfSchema<TBase> schema)
    {
        Schema = schema;
    }

    public OneOfSchemaBuilder<TBase> Nullable(bool? nullable)
    {
        Schema.Nullable = nullable;
        return this;
    }

    public OneOfSchemaBuilder<TBase> Discriminator(string? discriminator)
    {
        ArgumentNullException.ThrowIfNull(discriminator);
        Schema.Discriminator = discriminator;
        return this;
    }

    public OneOfSchemaBuilder<TBase> AddMapping<TMap>(Action<ObjectSchemaBuilder<TMap>>? schema)
        where TMap : TBase
    {
        var mappingSchema = new ObjectSchema<TMap>();
        var mappingBuilder = new ObjectSchemaBuilder<TMap>(mappingSchema);
        schema?.Invoke(mappingBuilder);
        Schema.AddSchema(typeof(TMap).Name, mappingSchema);
        return this;
    }

    public OneOfSchemaBuilder<TBase> AddMappingsFrom<TSource>(Func<OneOfSchema<TSource>> schema, bool? asRequired = null)
    {
        return this;
    }
}
