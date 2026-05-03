namespace Turbine.Tests.Unit;

public class EnumSchemaBuilderTests
{
    private enum Status
    {
        Pending,
        Active,
        Retired,
    }

    private static (EnumSchema<Status> schema, EnumSchemaBuilder<Status> builder) Subject()
    {
        var schema = new EnumSchema<Status>();
        return (schema, new EnumSchemaBuilder<Status>(schema));
    }

    [Fact]
    public void Nullable_default_is_unset()
    {
        var (schema, _) = Subject();

        Assert.Null(schema.Nullable);
    }

    [Fact]
    public void Nullable_true_writes_to_schema()
    {
        var (schema, builder) = Subject();

        builder.Nullable(true);

        Assert.True(schema.Nullable);
    }

    [Fact]
    public void Nullable_false_writes_to_schema()
    {
        var (schema, builder) = Subject();

        builder.Nullable(false);

        Assert.False(schema.Nullable);
    }

    [Fact]
    public void Nullable_null_resets_schema()
    {
        var (schema, builder) = Subject();

        builder.Nullable(true).Nullable(null);

        Assert.Null(schema.Nullable);
    }

    [Fact]
    public void Nullable_returns_same_builder_for_chaining()
    {
        var (_, builder) = Subject();

        Assert.Same(builder, builder.Nullable(true));
    }
}
