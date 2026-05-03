namespace Turbine.Tests.Unit;

public class ArraySchemaBuilderTests
{
    private sealed record Item(string Name);

    private static (ArraySchema<Item> schema, ArraySchemaBuilder<Item> builder) Subject()
    {
        var schema = new ArraySchema<Item>();
        return (schema, new ArraySchemaBuilder<Item>(schema));
    }

    [Fact]
    public void MinItems_writes_to_schema()
    {
        var (schema, builder) = Subject();

        builder.MinItems(2);

        Assert.Equal(2, schema.MinItems);
    }

    [Fact]
    public void MinItems_zero_is_valid()
    {
        var (schema, builder) = Subject();

        builder.MinItems(0);

        Assert.Equal(0, schema.MinItems);
    }

    [Fact]
    public void MinItems_throws_on_negative()
    {
        var (_, builder) = Subject();

        Assert.Throws<ArgumentOutOfRangeException>(() => builder.MinItems(-1));
    }

    [Fact]
    public void MaxItems_writes_to_schema()
    {
        var (schema, builder) = Subject();

        builder.MaxItems(50);

        Assert.Equal(50, schema.MaxItems);
    }

    [Fact]
    public void MaxItems_zero_is_valid()
    {
        var (schema, builder) = Subject();

        builder.MaxItems(0);

        Assert.Equal(0, schema.MaxItems);
    }

    [Fact]
    public void MaxItems_throws_on_negative()
    {
        var (_, builder) = Subject();

        Assert.Throws<ArgumentOutOfRangeException>(() => builder.MaxItems(-1));
    }

    [Fact]
    public void Bounds_default_to_unset()
    {
        var (schema, _) = Subject();

        Assert.Null(schema.MinItems);
        Assert.Null(schema.MaxItems);
    }

    [Fact]
    public void Methods_return_same_builder_for_chaining()
    {
        var (_, builder) = Subject();

        Assert.Same(builder, builder.MinItems(0));
        Assert.Same(builder, builder.MaxItems(10));
        Assert.Same(builder, builder.Nullable(true));
    }

    [Fact]
    public void Nullable_writes_to_schema()
    {
        var (schema, builder) = Subject();

        builder.Nullable(true);

        Assert.True(schema.Nullable);
    }

    [Fact]
    public void Later_call_overwrites_earlier_call()
    {
        var (schema, builder) = Subject();

        builder.MaxItems(5).MaxItems(20);

        Assert.Equal(20, schema.MaxItems);
    }
}
