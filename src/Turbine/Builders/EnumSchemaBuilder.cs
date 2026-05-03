namespace Turbine;

public class EnumSchemaBuilder<TEnum> : SchemaBuilder<EnumSchemaBuilder<TEnum>>
    where TEnum : struct, Enum
{
    internal EnumSchema<TEnum> Schema { get; }

    internal EnumSchemaBuilder(EnumSchema<TEnum> schema)
    {
        Schema = schema;
    }

    public override EnumSchemaBuilder<TEnum> Nullable(bool? nullable)
    {
        Schema.Nullable = nullable;
        return this;
    }
}
