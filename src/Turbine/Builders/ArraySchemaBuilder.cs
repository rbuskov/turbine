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
        Schema.Items ??= new ObjectSchema<TItem>();
        Schema.Items.Properties.Add(property);
    }

    internal override void RemoveProperty(string propertyName)
    {
        if (Schema.Items is null)
        {
            return;
        }
        for (var i = Schema.Items.Properties.Count - 1; i >= 0; i--)
        {
            if (Schema.Items.Properties[i].Name == propertyName)
            {
                Schema.Items.Properties.RemoveAt(i);
            }
        }
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
