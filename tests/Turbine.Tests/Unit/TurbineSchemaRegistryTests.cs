namespace Turbine.Tests.Unit;

public class TurbineSchemaRegistryTests
{
    public sealed class Person
    {
        public string Name { get; set; } = "";
        public int Age { get; set; }
    }

    public sealed class FixtureConfig : SchemaConfiguration
    {
        public StringSchema Name { get; set; } = null!;
        public ObjectSchema<Person> Person { get; set; } = null!;
        internal NumericSchema<int> InternalAge { get; set; } = null!;

        public override void Configure(SchemaConfigurationBuilder builder)
        {
            builder.Schema(() => Name).MinLength(1);
            builder.Schema(() => Person)
                .Add(p => p.Name)
                .Add(p => p.Age);
            builder.Schema(() => InternalAge).Minimum(0);
        }
    }

    public sealed class EmptyConfig : SchemaConfiguration
    {
        public override void Configure(SchemaConfigurationBuilder builder) { }
    }

    [Fact]
    public void Build_runs_Configure_on_each_configuration()
    {
        var config = new FixtureConfig();
        var registry = new TurbineSchemaRegistry();

        registry.Build(new SchemaConfiguration[] { config });

        Assert.NotNull(config.Name);
        Assert.NotNull(config.Person);
        Assert.Equal(new[] { "Name", "Age" }, config.Person.Properties.Select(p => p.Name).ToArray());
    }

    [Fact]
    public void Resolve_returns_schema_by_configuration_type_and_property_name()
    {
        var config = new FixtureConfig();
        var registry = new TurbineSchemaRegistry();
        registry.Build(new SchemaConfiguration[] { config });

        var resolved = registry.Resolve(typeof(FixtureConfig), nameof(FixtureConfig.Name));

        Assert.Same(config.Name, resolved);
    }

    [Fact]
    public void Resolve_returns_object_schema_instance()
    {
        var config = new FixtureConfig();
        var registry = new TurbineSchemaRegistry();
        registry.Build(new SchemaConfiguration[] { config });

        var resolved = registry.Resolve(typeof(FixtureConfig), nameof(FixtureConfig.Person));

        Assert.Same(config.Person, resolved);
    }

    [Fact]
    public void Resolve_finds_internal_schema_properties()
    {
        var config = new FixtureConfig();
        var registry = new TurbineSchemaRegistry();
        registry.Build(new SchemaConfiguration[] { config });

        var resolved = registry.Resolve(typeof(FixtureConfig), nameof(FixtureConfig.InternalAge));

        Assert.Same(config.InternalAge, resolved);
    }

    [Fact]
    public void Resolve_returns_null_for_unknown_property()
    {
        var registry = new TurbineSchemaRegistry();
        registry.Build(new SchemaConfiguration[] { new EmptyConfig() });

        Assert.Null(registry.Resolve(typeof(EmptyConfig), "DoesNotExist"));
    }

    [Fact]
    public void Build_is_idempotent()
    {
        var config = new FixtureConfig();
        var registry = new TurbineSchemaRegistry();

        registry.Build(new SchemaConfiguration[] { config });
        var firstName = config.Name;
        registry.Build(new SchemaConfiguration[] { config });

        Assert.Same(firstName, config.Name);
        Assert.Same(firstName, registry.Resolve(typeof(FixtureConfig), nameof(FixtureConfig.Name)));
    }

    [Fact]
    public void Build_throws_on_null_configurations()
    {
        var registry = new TurbineSchemaRegistry();
        Assert.Throws<ArgumentNullException>(() => registry.Build(null!));
    }

    [Fact]
    public void Resolve_throws_on_null_arguments()
    {
        var registry = new TurbineSchemaRegistry();
        registry.Build(Array.Empty<SchemaConfiguration>());

        Assert.Throws<ArgumentNullException>(() => registry.Resolve(null!, "x"));
        Assert.Throws<ArgumentNullException>(() => registry.Resolve(typeof(EmptyConfig), null!));
    }

    [Fact]
    public void IsBuilt_flips_after_Build()
    {
        var registry = new TurbineSchemaRegistry();
        Assert.False(registry.IsBuilt);

        registry.Build(Array.Empty<SchemaConfiguration>());

        Assert.True(registry.IsBuilt);
    }
}
