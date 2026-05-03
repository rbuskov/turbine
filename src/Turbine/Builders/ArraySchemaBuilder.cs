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

    internal override void AddProperty(ObjectProperty property)
    {
        throw new NotSupportedException(
            "Adding item-shape properties to an ArraySchemaBuilder is not yet wired. " +
            "Configure array bounds (MinItems / MaxItems) instead.");
    }

    internal override void RemoveProperty(string propertyName)
    {
        throw new NotSupportedException(
            "Removing item-shape properties on an ArraySchemaBuilder is not yet wired.");
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
