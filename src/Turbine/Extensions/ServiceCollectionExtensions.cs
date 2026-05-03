using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Turbine;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTurbine(this IServiceCollection services, params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(assemblies);

        for (var i = 0; i < assemblies.Length; i++)
        {
            if (assemblies[i] is null)
            {
                throw new ArgumentException(
                    $"Assemblies array contains a null entry at index {i}.",
                    nameof(assemblies));
            }
        }

        var resolved = assemblies.Length == 0
            ? new[]
            {
                Assembly.GetEntryAssembly()
                ?? throw new InvalidOperationException(
                    "AddTurbine requires an entry assembly to discover SchemaConfiguration subclasses; " +
                    "pass one or more assemblies explicitly when called from a context without an entry assembly."),
            }
            : assemblies;

        var set = TurbineConfigurationSet.GetOrAdd(services);

        foreach (var assembly in resolved)
        {
            foreach (var type in assembly.GetTypes())
            {
                if (!IsDiscoverableSchemaConfiguration(type))
                {
                    continue;
                }

                if (set.Add(type))
                {
                    services.TryAddSingleton(type);
                }
            }
        }

        return services;
    }

    private static bool IsDiscoverableSchemaConfiguration(Type type)
    {
        return type.IsClass
            && !type.IsAbstract
            && type.IsVisible
            && !type.IsGenericTypeDefinition
            && type.IsSubclassOf(typeof(SchemaConfiguration));
    }
}
