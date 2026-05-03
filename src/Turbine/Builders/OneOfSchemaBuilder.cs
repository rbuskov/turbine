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
        ArgumentNullException.ThrowIfNull(schema);
        var source = schema();
        var ctx = TurbineBuildContext.Current;
        if (ctx is not null && ctx.TryDefer(source, () => ApplyAddMappingsFrom(source, asRequired)))
        {
            return this;
        }
        ApplyAddMappingsFrom(source, asRequired);
        return this;
    }

    private void ApplyAddMappingsFrom<TSource>(OneOfSchema<TSource> source, bool? asRequired)
    {
        foreach (var (type, mapping) in source.Mappings)
        {
            var entry = asRequired is null ? mapping : CopyWithRequired(mapping, asRequired.Value);
            Schema.AddMapping(type, entry);
        }
    }

    private static IObjectSchema CopyWithRequired(IObjectSchema source, bool required)
    {
        var copy = (IObjectSchema) Activator.CreateInstance(source.GetType(), nonPublic: true)!;
        foreach (var property in source.Properties)
        {
            copy.Properties.Add(new ObjectProperty
            {
                Name = property.Name,
                Schema = property.Schema,
                Required = required,
                ValueExpression = property.ValueExpression,
                ToJson = property.ToJson,
                FromJson = property.FromJson,
            });
        }
        return copy;
    }
}
