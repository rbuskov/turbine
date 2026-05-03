namespace Turbine;

internal static class TurbineSchemaComponentNamer
{
    private const string SchemasSuffix = "Schemas";

    public static string GetComponentName(Type configurationType, string schemaPropertyName)
    {
        ArgumentNullException.ThrowIfNull(configurationType);
        ArgumentNullException.ThrowIfNull(schemaPropertyName);

        var name = configurationType.Name;
        if (name.Length > SchemasSuffix.Length
            && name.EndsWith(SchemasSuffix, StringComparison.Ordinal))
        {
            name = name[..^SchemasSuffix.Length];
        }
        return $"{name}_{schemaPropertyName}";
    }
}
