namespace Turbine;

public class DateTimeOffsetSchemaBuilder : SchemaBuilder<DateTimeOffsetSchemaBuilder>
{
    internal DateTimeOffsetSchema Schema { get; }

    internal DateTimeOffsetSchemaBuilder(DateTimeOffsetSchema schema)
    {
        Schema = schema;
    }

    public override DateTimeOffsetSchemaBuilder Nullable(bool? nullable)
    {
        Schema.Nullable = nullable;
        return this;
    }
}
