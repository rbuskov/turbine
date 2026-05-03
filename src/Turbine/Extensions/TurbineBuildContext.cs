namespace Turbine;

internal sealed class TurbineBuildContext : IDisposable
{
    private static readonly AsyncLocal<TurbineBuildContext?> CurrentSlot = new();

    private readonly TurbineBuildContext? previous;

    private TurbineBuildContext()
    {
        previous = CurrentSlot.Value;
        CurrentSlot.Value = this;
    }

    public static TurbineBuildContext? Current => CurrentSlot.Value;

    public Dictionary<ISchema, SchemaOwner> SchemaOwners { get; } = new(ReferenceEqualityComparer.Instance);

    public HashSet<SchemaConfiguration> Configured { get; } = new();

    public List<DeferredOperation> Deferred { get; } = new();

    public Dictionary<SchemaConfiguration, HashSet<SchemaConfiguration>> Edges { get; } = new();

    public SchemaConfiguration? CurrentConfig { get; private set; }

    public string? CurrentOuterSchemaProperty { get; set; }

    public static TurbineBuildContext Begin() => new();

    public IDisposable EnterConfig(SchemaConfiguration config)
    {
        ArgumentNullException.ThrowIfNull(config);
        return new ConfigScope(this, config);
    }

    public void RegisterOwner(ISchema schema, SchemaConfiguration config, string propertyName)
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(propertyName);
        if (!SchemaOwners.ContainsKey(schema))
        {
            SchemaOwners[schema] = new SchemaOwner(config, propertyName);
        }
    }

    public bool TryDefer(ISchema source, Action apply)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(apply);
        if (CurrentConfig is null)
        {
            return false;
        }
        if (!SchemaOwners.TryGetValue(source, out var owner))
        {
            // Source isn't a tracked schema (e.g., user-constructed); apply immediately.
            return false;
        }
        if (ReferenceEquals(owner.Configuration, CurrentConfig))
        {
            // Same configuration: lexical order in Configure is the user's responsibility.
            return false;
        }

        Deferred.Add(new DeferredOperation(
            CurrentConfig,
            CurrentOuterSchemaProperty,
            owner.Configuration,
            owner.PropertyName,
            apply));
        AddEdge(CurrentConfig, owner.Configuration);
        return true;
    }

    public void AddEdge(SchemaConfiguration from, SchemaConfiguration to)
    {
        if (!Edges.TryGetValue(from, out var set))
        {
            set = new HashSet<SchemaConfiguration>();
            Edges[from] = set;
        }
        set.Add(to);
    }

    public void Dispose()
    {
        CurrentSlot.Value = previous;
    }

    internal readonly record struct SchemaOwner(SchemaConfiguration Configuration, string PropertyName);

    internal sealed record DeferredOperation(
        SchemaConfiguration OwnerConfiguration,
        string? OwnerSchemaProperty,
        SchemaConfiguration DependsOnConfiguration,
        string DependsOnSchemaProperty,
        Action Apply);

    private sealed class ConfigScope : IDisposable
    {
        private readonly TurbineBuildContext context;
        private readonly SchemaConfiguration? previousConfig;
        private readonly string? previousProperty;

        public ConfigScope(TurbineBuildContext context, SchemaConfiguration config)
        {
            this.context = context;
            previousConfig = context.CurrentConfig;
            previousProperty = context.CurrentOuterSchemaProperty;
            context.CurrentConfig = config;
            context.CurrentOuterSchemaProperty = null;
        }

        public void Dispose()
        {
            context.CurrentConfig = previousConfig;
            context.CurrentOuterSchemaProperty = previousProperty;
        }
    }
}
