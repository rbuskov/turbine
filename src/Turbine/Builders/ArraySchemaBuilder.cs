namespace Turbine;

public class ArraySchemaBuilder<TItem> : PropertySchemaBuilder<TItem, ArraySchemaBuilder<TItem>>
{
    public ArraySchemaBuilder<TItem> MinItems(int minItems)
    {
        return this;
    }
    
    public ArraySchemaBuilder<TItem> MaxItems(int maxItems)
    {
        return this;
    }
}