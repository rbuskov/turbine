using System.Numerics;

namespace Turbine;

public class NumericSchemaBuilder<TNumber> : SchemaBuilder<NumericSchemaBuilder<TNumber>>
    where TNumber : struct, INumber<TNumber>
{
    internal NumericSchema<TNumber> Schema { get; }

    internal NumericSchemaBuilder(NumericSchema<TNumber> schema)
    {
        Schema = schema;
    }

    public override NumericSchemaBuilder<TNumber> Nullable(bool? nullable)
    {
        Schema.Nullable = nullable;
        return this;
    }

    public NumericSchemaBuilder<TNumber> Minimum(TNumber minimum)
    {
        Schema.Minimum = minimum;
        return this;
    }

    public NumericSchemaBuilder<TNumber> ExclusiveMinimum(TNumber exclusiveMinimum)
    {
        Schema.ExclusiveMinimum = exclusiveMinimum;
        return this;
    }

    public NumericSchemaBuilder<TNumber> Maximum(TNumber maximum)
    {
        Schema.Maximum = maximum;
        return this;
    }

    public NumericSchemaBuilder<TNumber> ExclusiveMaximum(TNumber exclusiveMaximum)
    {
        Schema.ExclusiveMaximum = exclusiveMaximum;
        return this;
    }

    public NumericSchemaBuilder<TNumber> MultipleOf(TNumber multipleOf)
    {
        if (multipleOf <= TNumber.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(multipleOf),
                multipleOf,
                "multipleOf must be strictly positive.");
        }

        Schema.MultipleOf = multipleOf;
        return this;
    }
}
