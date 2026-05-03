namespace Turbine.Tests.Unit;

public class SchemaBuilderTests
{
    private sealed class StubBuilder : SchemaBuilder<StubBuilder>
    {
        internal StubBuilder() { }
    }

    [Fact]
    public void Nullable_default_returns_same_builder_for_chaining()
    {
        var builder = new StubBuilder();

        Assert.Same(builder, builder.Nullable(true));
        Assert.Same(builder, builder.Nullable(false));
        Assert.Same(builder, builder.Nullable(null));
    }

    private sealed class CapturingBuilder : SchemaBuilder<CapturingBuilder>
    {
        public bool? LastNullable { get; private set; }
        public int CallCount { get; private set; }

        internal CapturingBuilder() { }

        public override CapturingBuilder Nullable(bool? nullable)
        {
            LastNullable = nullable;
            CallCount++;
            return this;
        }
    }

    [Fact]
    public void Override_replaces_default_no_op_behavior()
    {
        var builder = new CapturingBuilder();

        builder.Nullable(true).Nullable(false);

        Assert.Equal(2, builder.CallCount);
        Assert.False(builder.LastNullable);
    }
}
