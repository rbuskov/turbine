namespace Turbine.Tests.Unit;

public class SchemaDependencyOrderingTests
{
    public sealed class Person
    {
        public string Name { get; set; } = "";
        public int Age { get; set; }
    }

    public abstract class Animal
    {
        public string Name { get; set; } = "";
    }

    public sealed class Dog : Animal { }

    public sealed class Cat : Animal { }

    internal sealed class BaseConfig : SchemaConfiguration
    {
        public ObjectSchema<Person> Summary { get; set; } = null!;
        public OneOfSchema<Animal> Variants { get; set; } = null!;

        public override void Configure(SchemaConfigurationBuilder builder)
        {
            builder.Schema(() => Summary)
                .Add(p => p.Name)
                .Add(p => p.Age);
            builder.Schema(() => Variants)
                .AddMapping<Dog>(_ => { })
                .AddMapping<Cat>(_ => { });
        }
    }

    internal sealed class DownstreamConfig : SchemaConfiguration
    {
        private readonly BaseConfig baseConfig;

        public DownstreamConfig(BaseConfig baseConfig)
        {
            this.baseConfig = baseConfig;
        }

        public ObjectSchema<Person> Detailed { get; set; } = null!;
        public OneOfSchema<Animal> AllVariants { get; set; } = null!;

        public override void Configure(SchemaConfigurationBuilder builder)
        {
            builder.Schema(() => Detailed)
                .AddPropertiesFrom(() => baseConfig.Summary)
                .Add(p => p.Name, name: "DisplayName");
            builder.Schema(() => AllVariants)
                .AddMappingsFrom(() => baseConfig.Variants);
        }
    }

    [Fact]
    public void Cross_config_AddPropertiesFrom_resolves_in_dependency_order()
    {
        var baseConfig = new BaseConfig();
        var downstream = new DownstreamConfig(baseConfig);
        var registry = new TurbineSchemaRegistry();

        // Register downstream first to force the deferred path.
        registry.Build([downstream, baseConfig]);

        Assert.NotNull(downstream.Detailed);
        Assert.Equal(
            new[] { "Name", "Age", "DisplayName" },
            downstream.Detailed.Properties.Select(p => p.Name).ToArray());
    }

    [Fact]
    public void Cross_config_AddMappingsFrom_resolves_in_dependency_order()
    {
        var baseConfig = new BaseConfig();
        var downstream = new DownstreamConfig(baseConfig);
        var registry = new TurbineSchemaRegistry();

        registry.Build([downstream, baseConfig]);

        Assert.NotNull(downstream.AllVariants);
        Assert.True(downstream.AllVariants.Mappings.ContainsKey(nameof(Dog)));
        Assert.True(downstream.AllVariants.Mappings.ContainsKey(nameof(Cat)));
    }

    internal sealed class ChainA : SchemaConfiguration
    {
        public ObjectSchema<Person> A { get; set; } = null!;
        public override void Configure(SchemaConfigurationBuilder builder)
        {
            builder.Schema(() => A).Add(p => p.Name);
        }
    }

    internal sealed class ChainB : SchemaConfiguration
    {
        private readonly ChainA chainA;
        public ChainB(ChainA chainA) { this.chainA = chainA; }
        public ObjectSchema<Person> B { get; set; } = null!;
        public override void Configure(SchemaConfigurationBuilder builder)
        {
            builder.Schema(() => B).AddPropertiesFrom(() => chainA.A).Add(p => p.Age);
        }
    }

    internal sealed class ChainC : SchemaConfiguration
    {
        private readonly ChainB chainB;
        public ChainC(ChainB chainB) { this.chainB = chainB; }
        public ObjectSchema<Person> C { get; set; } = null!;
        public override void Configure(SchemaConfigurationBuilder builder)
        {
            builder.Schema(() => C).AddPropertiesFrom(() => chainB.B);
        }
    }

    [Fact]
    public void Linear_chain_resolves_transitively()
    {
        var a = new ChainA();
        var b = new ChainB(a);
        var c = new ChainC(b);
        var registry = new TurbineSchemaRegistry();

        // Reverse order to force deferred resolution.
        registry.Build([c, b, a]);

        Assert.Equal(new[] { "Name" }, a.A.Properties.Select(p => p.Name).ToArray());
        Assert.Equal(new[] { "Name", "Age" }, b.B.Properties.Select(p => p.Name).ToArray());
        Assert.Equal(new[] { "Name", "Age" }, c.C.Properties.Select(p => p.Name).ToArray());
    }

    internal sealed class CycleA : SchemaConfiguration
    {
        public CycleB? Other { get; set; }
        public ObjectSchema<Person> Foo { get; set; } = null!;
        public override void Configure(SchemaConfigurationBuilder builder)
        {
            builder.Schema(() => Foo).AddPropertiesFrom(() => Other!.Bar);
        }
    }

    internal sealed class CycleB : SchemaConfiguration
    {
        public CycleA? Other { get; set; }
        public ObjectSchema<Person> Bar { get; set; } = null!;
        public override void Configure(SchemaConfigurationBuilder builder)
        {
            builder.Schema(() => Bar).AddPropertiesFrom(() => Other!.Foo);
        }
    }

    [Fact]
    public void Cycle_between_two_configurations_throws_with_participants_in_message()
    {
        var a = new CycleA();
        var b = new CycleB();
        a.Other = b;
        b.Other = a;
        var registry = new TurbineSchemaRegistry();

        var ex = Assert.Throws<SchemaDependencyCycleException>(() => registry.Build([a, b]));
        Assert.Contains(nameof(CycleA), ex.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(CycleB), ex.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(CycleA.Foo), ex.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(CycleB.Bar), ex.Message, StringComparison.Ordinal);
    }

    internal sealed class TripleX : SchemaConfiguration
    {
        public TripleY? Y { get; set; }
        public ObjectSchema<Person> X { get; set; } = null!;
        public override void Configure(SchemaConfigurationBuilder builder)
        {
            builder.Schema(() => X).AddPropertiesFrom(() => Y!.Y);
        }
    }

    internal sealed class TripleY : SchemaConfiguration
    {
        public TripleZ? Z { get; set; }
        public ObjectSchema<Person> Y { get; set; } = null!;
        public override void Configure(SchemaConfigurationBuilder builder)
        {
            builder.Schema(() => Y).AddPropertiesFrom(() => Z!.Z);
        }
    }

    internal sealed class TripleZ : SchemaConfiguration
    {
        public TripleX? X { get; set; }
        public ObjectSchema<Person> Z { get; set; } = null!;
        public override void Configure(SchemaConfigurationBuilder builder)
        {
            builder.Schema(() => Z).AddPropertiesFrom(() => X!.X);
        }
    }

    [Fact]
    public void Cycle_in_three_configurations_throws_with_all_participants()
    {
        var x = new TripleX();
        var y = new TripleY();
        var z = new TripleZ();
        x.Y = y;
        y.Z = z;
        z.X = x;
        var registry = new TurbineSchemaRegistry();

        var ex = Assert.Throws<SchemaDependencyCycleException>(() => registry.Build([x, y, z]));
        Assert.Contains(nameof(TripleX), ex.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(TripleY), ex.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(TripleZ), ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Pre_allocation_creates_schema_instances_before_Configure_runs()
    {
        // Even though DownstreamConfig.Detailed is set inside Configure, the registry
        // pre-allocates it so the cross-config Func<> resolves to a real instance.
        var baseConfig = new BaseConfig();
        var downstream = new DownstreamConfig(baseConfig);
        var registry = new TurbineSchemaRegistry();

        // baseConfig is built first here, but downstream's `() => baseConfig.Summary`
        // can resolve regardless of order because of pre-allocation.
        registry.Build([downstream, baseConfig]);

        Assert.NotNull(baseConfig.Summary);
        Assert.NotNull(downstream.Detailed);
    }

    internal sealed class IntraConfig : SchemaConfiguration
    {
        public ObjectSchema<Person> Source { get; set; } = null!;
        public ObjectSchema<Person> Derived { get; set; } = null!;
        public override void Configure(SchemaConfigurationBuilder builder)
        {
            builder.Schema(() => Source).Add(p => p.Name);
            builder.Schema(() => Derived).AddPropertiesFrom(() => Source).Add(p => p.Age);
        }
    }

    [Fact]
    public void Intra_config_AddPropertiesFrom_preserves_lexical_order()
    {
        var config = new IntraConfig();
        var registry = new TurbineSchemaRegistry();

        registry.Build([config]);

        // Within a single configuration, lexical order is the user's responsibility
        // and should be honoured (no deferring needed).
        Assert.Equal(new[] { "Name", "Age" }, config.Derived.Properties.Select(p => p.Name).ToArray());
    }
}
