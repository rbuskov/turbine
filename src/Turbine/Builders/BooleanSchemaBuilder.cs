namespace Turbine;

public class BooleanSchemaBuilder : SchemaBuilder<BooleanSchemaBuilder>
{
    internal BooleanSchema Schema { get; }

    internal BooleanSchemaBuilder(BooleanSchema schema)
    {
        Schema = schema;
    }

    public override BooleanSchemaBuilder Nullable(bool? nullable)
    {
        Schema.Nullable = nullable;
        return this;
    }
}
