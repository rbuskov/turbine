using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Turbine;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTurbine(this IServiceCollection services)
        => AddTurbine(services, Assembly.GetEntryAssembly()
            ?? throw new InvalidOperationException(
                "AddTurbine requires an entry assembly to discover ApiModelConfiguration subclasses; " +
                "use the AddTurbine(Assembly) overload from a test host."));

    public static IServiceCollection AddTurbine(this IServiceCollection services, Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(assembly);

        return services;
    }
}
