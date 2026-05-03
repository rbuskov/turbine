namespace Turbine.Tests.Unit;

public class NumericSchemaBuilderTests
{
    private static (NumericSchema<int> schema, NumericSchemaBuilder<int> builder) IntSubject()
    {
        var schema = new NumericSchema<int>();
        return (schema, new NumericSchemaBuilder<int>(schema));
    }

    private static (NumericSchema<decimal> schema, NumericSchemaBuilder<decimal> builder) DecimalSubject()
    {
        var schema = new NumericSchema<decimal>();
        return (schema, new NumericSchemaBuilder<decimal>(schema));
    }

    [Fact]
    public void Minimum_writes_to_schema()
    {
        var (schema, builder) = IntSubject();

        builder.Minimum(5);

        Assert.Equal(5, schema.Minimum);
    }

    [Fact]
    public void Maximum_writes_to_schema()
    {
        var (schema, builder) = IntSubject();

        builder.Maximum(99);

        Assert.Equal(99, schema.Maximum);
    }

    [Fact]
    public void ExclusiveMinimum_writes_to_schema()
    {
        var (schema, builder) = IntSubject();

        builder.ExclusiveMinimum(0);

        Assert.Equal(0, schema.ExclusiveMinimum);
    }

    [Fact]
    public void ExclusiveMaximum_writes_to_schema()
    {
        var (schema, builder) = IntSubject();

        builder.ExclusiveMaximum(100);

        Assert.Equal(100, schema.ExclusiveMaximum);
    }

    [Fact]
    public void MultipleOf_writes_to_schema()
    {
        var (schema, builder) = IntSubject();

        builder.MultipleOf(3);

        Assert.Equal(3, schema.MultipleOf);
    }

    [Fact]
    public void MultipleOf_zero_throws()
    {
        var (_, builder) = IntSubject();

        Assert.Throws<ArgumentOutOfRangeException>(() => builder.MultipleOf(0));
    }

    [Fact]
    public void MultipleOf_negative_throws()
    {
        var (_, builder) = IntSubject();

        Assert.Throws<ArgumentOutOfRangeException>(() => builder.MultipleOf(-2));
    }

    [Fact]
    public void Methods_return_same_builder_for_chaining()
    {
        var (_, builder) = IntSubject();

        Assert.Same(builder, builder.Minimum(0));
        Assert.Same(builder, builder.Maximum(10));
        Assert.Same(builder, builder.ExclusiveMinimum(-1));
        Assert.Same(builder, builder.ExclusiveMaximum(11));
        Assert.Same(builder, builder.MultipleOf(1));
        Assert.Same(builder, builder.Nullable(true));
    }

    [Fact]
    public void Nullable_writes_to_schema()
    {
        var (schema, builder) = IntSubject();

        builder.Nullable(true);

        Assert.True(schema.Nullable);
    }

    [Fact]
    public void Later_call_overwrites_earlier_call()
    {
        var (schema, builder) = IntSubject();

        builder.Minimum(1).Minimum(7);

        Assert.Equal(7, schema.Minimum);
    }

    [Fact]
    public void Works_with_decimal_number_type()
    {
        var (schema, builder) = DecimalSubject();

        builder.Minimum(0.5m).MultipleOf(0.25m);

        Assert.Equal(0.5m, schema.Minimum);
        Assert.Equal(0.25m, schema.MultipleOf);
    }
}
