namespace Turbine;

public sealed class SchemaDependencyCycleException : InvalidOperationException
{
    public SchemaDependencyCycleException(IReadOnlyList<string> cycle)
        : base(BuildMessage(cycle))
    {
        ArgumentNullException.ThrowIfNull(cycle);
        Cycle = cycle;
    }

    public IReadOnlyList<string> Cycle { get; }

    private static string BuildMessage(IReadOnlyList<string> cycle)
    {
        ArgumentNullException.ThrowIfNull(cycle);
        return "A cyclic dependency between Turbine schemas was detected: "
            + string.Join(" -> ", cycle)
            + ". Break the cycle by removing or restructuring one of the AddPropertiesFrom / AddMappingsFrom calls.";
    }
}
