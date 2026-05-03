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

    private sealed class Commendation
    {
        public string Name { get; set; } = "";
        public DateOnly AwardedDate { get; set; }
        public int? Stars { get; set; }
    }

    private static (ArraySchema<Commendation> schema, ArraySchemaBuilder<Commendation> builder) CommendationSubject()
    {
        var schema = new ArraySchema<Commendation>();
        return (schema, new ArraySchemaBuilder<Commendation>(schema));
    }

    [Fact]
    public void Items_default_is_null()
    {
        var (schema, _) = CommendationSubject();

        Assert.Null(schema.Items);
    }

    [Fact]
    public void Add_lazily_allocates_Items_and_registers_property()
    {
        var (schema, builder) = CommendationSubject();

        builder.Add(c => c.Name);

        Assert.NotNull(schema.Items);
        var property = Assert.Single(schema.Items!.Properties);
        Assert.Equal("Name", property.Name);
        Assert.IsType<StringSchema>(property.Schema);
    }

    [Fact]
    public void Add_preserves_call_order_inside_Items()
    {
        var (schema, builder) = CommendationSubject();

        builder
            .Add(c => c.Name)
            .Add(c => c.AwardedDate)
            .Add(c => c.Stars);

        Assert.Equal(
            new[] { "Name", "AwardedDate", "Stars" },
            schema.Items!.Properties.Select(p => p.Name).ToArray());
    }

    [Fact]
    public void Add_uses_nullability_for_required_default()
    {
        var (schema, builder) = CommendationSubject();

        builder.Add(c => c.Stars);

        var stars = Assert.Single(schema.Items!.Properties);
        Assert.False(stars.Required);
    }

    [Fact]
    public void Remove_drops_a_previously_added_item_property()
    {
        var (schema, builder) = CommendationSubject();
        builder.Add(c => c.Name).Add(c => c.AwardedDate);

        builder.Remove(c => c.Name);

        var remaining = Assert.Single(schema.Items!.Properties);
        Assert.Equal("AwardedDate", remaining.Name);
    }

    [Fact]
    public void Remove_when_Items_is_null_is_no_op()
    {
        var (schema, builder) = CommendationSubject();

        builder.Remove("Anything");

        Assert.Null(schema.Items);
    }

    [Fact]
    public void AddAtomicProperties_populates_Items_via_reflection()
    {
        var (schema, builder) = CommendationSubject();

        builder.AddAtomicProperties();

        Assert.NotNull(schema.Items);
        Assert.Equal(
            new[] { "Name", "AwardedDate", "Stars" },
            schema.Items!.Properties.Select(p => p.Name).ToArray());
    }
}
