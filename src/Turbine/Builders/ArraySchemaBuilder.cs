namespace Turbine;

public class ArraySchemaBuilder<TItem> : PropertySchemaBuilder<TItem, ArraySchemaBuilder<TItem>>
{
    internal ArraySchema<TItem> Schema { get; }

    internal ArraySchemaBuilder(ArraySchema<TItem> schema)
    {
        Schema = schema;
    }

    public override ArraySchemaBuilder<TItem> Nullable(bool? nullable)
    {
        Schema.Nullable = nullable;
        return this;
    }

    public ArraySchemaBuilder<TItem> MinItems(int minItems)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(minItems);
        Schema.MinItems = minItems;
        return this;
    }

    public ArraySchemaBuilder<TItem> MaxItems(int maxItems)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(maxItems);
        Schema.MaxItems = maxItems;
        return this;
    }
}
