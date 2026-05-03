namespace Turbine.Tests.Unit;

public class SchemaConfigurationTests
{
    private sealed class StubConfiguration : SchemaConfiguration
    {
        public SchemaConfigurationBuilder? ReceivedBuilder { get; private set; }
        public int ConfigureCallCount { get; private set; }

        public override void Configure(SchemaConfigurationBuilder builder)
        {
            ReceivedBuilder = builder;
            ConfigureCallCount++;
        }
    }

    [Fact]
    public void Configure_receives_the_supplied_builder()
    {
        var configuration = new StubConfiguration();
        var builder = new SchemaConfigurationBuilder();

        configuration.Configure(builder);

        Assert.Same(builder, configuration.ReceivedBuilder);
        Assert.Equal(1, configuration.ConfigureCallCount);
    }

    [Fact]
    public void Configure_can_be_called_multiple_times()
    {
        var configuration = new StubConfiguration();

        configuration.Configure(new SchemaConfigurationBuilder());
        configuration.Configure(new SchemaConfigurationBuilder());

        Assert.Equal(2, configuration.ConfigureCallCount);
    }

    [Fact]
    public void Subclass_can_populate_typed_schema_properties()
    {
        var configuration = new ResourceConfig();

        configuration.Configure(new SchemaConfigurationBuilder());

        Assert.NotNull(configuration.Name);
        Assert.NotNull(configuration.Count);
        Assert.Equal(1, configuration.Name.MinLength);
        Assert.Equal(0, configuration.Count.Minimum);
    }

    private sealed class ResourceConfig : SchemaConfiguration
    {
        public StringSchema Name { get; set; } = null!;
        public NumericSchema<int> Count { get; set; } = null!;

        public override void Configure(SchemaConfigurationBuilder builder)
        {
            builder.Schema(() => Name).MinLength(1);
            builder.Schema(() => Count).Minimum(0);
        }
    }
}
