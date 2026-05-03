namespace Turbine;

public class ObjectSchemaBuilder<TDomain> : PropertySchemaBuilder<TDomain, ObjectSchemaBuilder<TDomain>>
{
    internal ObjectSchema<TDomain> Schema { get; }

    internal ObjectSchemaBuilder(ObjectSchema<TDomain> schema)
    {
        Schema = schema;
    }
}
