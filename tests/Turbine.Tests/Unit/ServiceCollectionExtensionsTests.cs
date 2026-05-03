using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Turbine.Tests.Unit;

public class ServiceCollectionExtensionsTests
{
    private static readonly Assembly TestAssembly = typeof(ServiceCollectionExtensionsTests).Assembly;

    public sealed class ConcretePublicConfig : SchemaConfiguration
    {
        public override void Configure(SchemaConfigurationBuilder builder) { }
    }

    public sealed class AnotherConcretePublicConfig : SchemaConfiguration
    {
        public override void Configure(SchemaConfigurationBuilder builder) { }
    }

    public abstract class AbstractPublicConfig : SchemaConfiguration { }

    internal sealed class InternalConfig : SchemaConfiguration
    {
        public override void Configure(SchemaConfigurationBuilder builder) { }
    }

    public sealed class GenericConfig<T> : SchemaConfiguration
    {
        public override void Configure(SchemaConfigurationBuilder builder) { }
    }

    private sealed class PrivateNestedConfig : SchemaConfiguration
    {
        public override void Configure(SchemaConfigurationBuilder builder) { }
    }

    [Fact]
    public void AddTurbine_throws_on_null_services()
    {
        IServiceCollection services = null!;
        Assert.Throws<ArgumentNullException>(() => services.AddTurbine());
    }

    [Fact]
    public void AddTurbine_throws_on_null_assemblies_array()
    {
        var services = new ServiceCollection();
        Assert.Throws<ArgumentNullException>(() => services.AddTurbine((Assembly[]) null!));
    }

    [Fact]
    public void AddTurbine_throws_on_null_entry_in_assemblies_array()
    {
        var services = new ServiceCollection();
        Assert.Throws<ArgumentException>(() => services.AddTurbine([TestAssembly, null!]));
    }

    [Fact]
    public void AddTurbine_registers_concrete_public_subclass_as_singleton()
    {
        var services = new ServiceCollection();

        services.AddTurbine(TestAssembly);

        var descriptor = services.Single(d => d.ServiceType == typeof(ConcretePublicConfig));
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }

    [Fact]
    public void AddTurbine_does_not_register_abstract_subclass()
    {
        var services = new ServiceCollection();

        services.AddTurbine(TestAssembly);

        Assert.DoesNotContain(services, d => d.ServiceType == typeof(AbstractPublicConfig));
    }

    [Fact]
    public void AddTurbine_does_not_register_internal_subclass()
    {
        var services = new ServiceCollection();

        services.AddTurbine(TestAssembly);

        Assert.DoesNotContain(services, d => d.ServiceType == typeof(InternalConfig));
    }

    [Fact]
    public void AddTurbine_does_not_register_private_nested_subclass()
    {
        var services = new ServiceCollection();

        services.AddTurbine(TestAssembly);

        Assert.DoesNotContain(services, d => d.ServiceType == typeof(PrivateNestedConfig));
    }

    [Fact]
    public void AddTurbine_does_not_register_generic_subclass()
    {
        var services = new ServiceCollection();

        services.AddTurbine(TestAssembly);

        Assert.DoesNotContain(services, d => d.ServiceType == typeof(GenericConfig<>));
        Assert.DoesNotContain(services, d => d.ServiceType.IsGenericType
                                              && d.ServiceType.GetGenericTypeDefinition() == typeof(GenericConfig<>));
    }

    [Fact]
    public void AddTurbine_idempotent_on_repeated_calls()
    {
        var services = new ServiceCollection();

        services.AddTurbine(TestAssembly);
        services.AddTurbine(TestAssembly);

        var matching = services.Where(d => d.ServiceType == typeof(ConcretePublicConfig)).ToList();
        Assert.Single(matching);
    }

    [Fact]
    public void AddTurbine_dedupes_same_assembly_passed_twice()
    {
        var services = new ServiceCollection();

        services.AddTurbine(TestAssembly, TestAssembly);

        var matching = services.Where(d => d.ServiceType == typeof(ConcretePublicConfig)).ToList();
        Assert.Single(matching);
    }

    [Fact]
    public void AddTurbine_dedupes_overlapping_assembly_sets()
    {
        var services = new ServiceCollection();

        services.AddTurbine(TestAssembly);
        services.AddTurbine(TestAssembly, TestAssembly);

        var matching = services.Where(d => d.ServiceType == typeof(ConcretePublicConfig)).ToList();
        Assert.Single(matching);
    }

    [Fact]
    public void AddTurbine_does_not_clobber_pre_existing_registrations()
    {
        var services = new ServiceCollection();
        var preExisting = new ConcretePublicConfig();
        services.AddSingleton(preExisting);

        services.AddTurbine(TestAssembly);

        var provider = services.BuildServiceProvider();
        Assert.Same(preExisting, provider.GetRequiredService<ConcretePublicConfig>());
    }

    [Fact]
    public void AddTurbine_with_empty_params_falls_back_to_entry_assembly()
    {
        var services = new ServiceCollection();

        // Calling with no assemblies should not throw and should attempt entry-assembly discovery.
        // In the test host, an entry assembly exists, so this just succeeds.
        services.AddTurbine();

        // Sanity: nothing about this test asserts on a particular type — just that the call completes
        // without throwing when an entry assembly is present.
        Assert.NotEmpty(services);
    }

    [Fact]
    public void AddTurbine_resolved_singletons_are_same_instance()
    {
        var services = new ServiceCollection();
        services.AddTurbine(TestAssembly);

        var provider = services.BuildServiceProvider();
        var first = provider.GetRequiredService<ConcretePublicConfig>();
        var second = provider.GetRequiredService<ConcretePublicConfig>();

        Assert.Same(first, second);
    }

    [Fact]
    public void AddTurbine_registers_multiple_distinct_subclasses()
    {
        var services = new ServiceCollection();

        services.AddTurbine(TestAssembly);

        Assert.Contains(services, d => d.ServiceType == typeof(ConcretePublicConfig));
        Assert.Contains(services, d => d.ServiceType == typeof(AnotherConcretePublicConfig));
    }
}
