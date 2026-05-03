using Microsoft.Extensions.DependencyInjection;

namespace Turbine;

internal sealed class TurbineConfigurationSet
{
    private readonly HashSet<Type> types = new();

    public bool Add(Type configurationType) => types.Add(configurationType);

    public IReadOnlyCollection<Type> Types => types;

    internal static TurbineConfigurationSet GetOrAdd(IServiceCollection services)
    {
        for (var i = 0; i < services.Count; i++)
        {
            var descriptor = services[i];
            if (descriptor.ServiceType == typeof(TurbineConfigurationSet)
                && descriptor.ImplementationInstance is TurbineConfigurationSet existing)
            {
                return existing;
            }
        }

        var set = new TurbineConfigurationSet();
        services.AddSingleton(set);
        return set;
    }
}
