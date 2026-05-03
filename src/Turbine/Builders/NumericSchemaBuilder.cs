using System.Numerics;

namespace Turbine;

public class NumericSchemaBuilder<TNumber> : SchemaBuilder<NumericSchemaBuilder<TNumber>>
    where TNumber : INumber<TNumber>
{
    internal NumericSchemaBuilder() { }

    public NumericSchemaBuilder<TNumber> Minimum(TNumber minimum)
    {
        return this;
    }

    public NumericSchemaBuilder<TNumber> ExclusiveMinimum(TNumber exclusiveMinimum)
    {
        return this;
    }

    public NumericSchemaBuilder<TNumber> Maximum(TNumber maximum)
    {
        return this;
    }

    public NumericSchemaBuilder<TNumber> ExclusiveMaximum(TNumber exclusiveMaximum)
    {
        return this;
    }

    public NumericSchemaBuilder<TNumber> MultipleOf(TNumber multipleOf)
    {
        return this;
    }
}