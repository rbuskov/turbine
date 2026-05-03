namespace Turbine;

public class DateOnlySchemaBuilder : SchemaBuilder<DateOnlySchemaBuilder>
{
    internal DateOnlySchema Schema { get; }

    internal DateOnlySchemaBuilder(DateOnlySchema schema)
    {
        Schema = schema;
    }

    public override DateOnlySchemaBuilder Nullable(bool? nullable)
    {
        Schema.Nullable = nullable;
        return this;
    }
}
