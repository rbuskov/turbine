using System.Reflection;

namespace Turbine;

internal sealed class TurbineSchemaRegistry
{
    private readonly Dictionary<(Type ConfigurationType, string PropertyName), ISchema> schemas = new();
    private bool built;

    public bool IsBuilt => built;

    public ISchema? Resolve(Type configurationType, string propertyName)
    {
        ArgumentNullException.ThrowIfNull(configurationType);
        ArgumentNullException.ThrowIfNull(propertyName);
        return schemas.TryGetValue((configurationType, propertyName), out var schema) ? schema : null;
    }

    public IEnumerable<(Type ConfigurationType, string PropertyName, ISchema Schema)> Entries
        => schemas.Select(kv => (kv.Key.ConfigurationType, kv.Key.PropertyName, kv.Value));

    public void Build(IReadOnlyList<SchemaConfiguration> configurations)
    {
        ArgumentNullException.ThrowIfNull(configurations);
        if (built)
        {
            return;
        }

        foreach (var configuration in configurations)
        {
            configuration.Configure(new SchemaConfigurationBuilder());
        }

        foreach (var configuration in configurations)
        {
            CollectSchemas(configuration);
        }

        built = true;
    }

    private void CollectSchemas(SchemaConfiguration configuration)
    {
        var type = configuration.GetType();
        var properties = type.GetProperties(
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        foreach (var property in properties)
        {
            if (!typeof(ISchema).IsAssignableFrom(property.PropertyType))
            {
                continue;
            }
            if (property.GetIndexParameters().Length > 0)
            {
                continue;
            }
            var value = property.GetValue(configuration);
            if (value is ISchema schema)
            {
                schemas[(type, property.Name)] = schema;
            }
        }
    }
}
