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

        using var ctx = TurbineBuildContext.Begin();

        PreAllocateSchemas(configurations, ctx);

        foreach (var configuration in configurations)
        {
            using (ctx.EnterConfig(configuration))
            {
                configuration.Configure(new SchemaConfigurationBuilder());
            }
            ctx.Configured.Add(configuration);
        }

        if (ctx.Deferred.Count > 0)
        {
            ApplyDeferredInTopologicalOrder(configurations, ctx);
        }

        foreach (var configuration in configurations)
        {
            CollectSchemas(configuration);
        }

        built = true;
    }

    private static void PreAllocateSchemas(
        IReadOnlyList<SchemaConfiguration> configurations,
        TurbineBuildContext ctx)
    {
        foreach (var configuration in configurations)
        {
            var type = configuration.GetType();
            foreach (var property in type.GetProperties(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                if (!typeof(ISchema).IsAssignableFrom(property.PropertyType))
                {
                    continue;
                }
                if (property.GetIndexParameters().Length > 0 || !property.CanWrite)
                {
                    continue;
                }
                var existing = property.GetValue(configuration);
                if (existing is ISchema existingSchema)
                {
                    ctx.RegisterOwner(existingSchema, configuration, property.Name);
                    continue;
                }
                var instance = TryCreateSchema(property.PropertyType);
                if (instance is null)
                {
                    continue;
                }
                property.SetValue(configuration, instance);
                ctx.RegisterOwner(instance, configuration, property.Name);
            }

            foreach (var field in type.GetFields(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                if (!typeof(ISchema).IsAssignableFrom(field.FieldType))
                {
                    continue;
                }
                var existing = field.GetValue(configuration);
                if (existing is ISchema existingSchema)
                {
                    ctx.RegisterOwner(existingSchema, configuration, field.Name);
                    continue;
                }
                var instance = TryCreateSchema(field.FieldType);
                if (instance is null)
                {
                    continue;
                }
                field.SetValue(configuration, instance);
                ctx.RegisterOwner(instance, configuration, field.Name);
            }
        }
    }

    private static ISchema? TryCreateSchema(Type schemaType)
    {
        if (schemaType.IsAbstract || schemaType.IsInterface || schemaType.IsGenericTypeDefinition)
        {
            return null;
        }
        try
        {
            return Activator.CreateInstance(schemaType, nonPublic: true) as ISchema;
        }
        catch (MissingMethodException)
        {
            return null;
        }
    }

    private static void ApplyDeferredInTopologicalOrder(
        IReadOnlyList<SchemaConfiguration> configurations,
        TurbineBuildContext ctx)
    {
        var order = TopologicalSort(configurations, ctx);

        foreach (var configuration in order)
        {
            foreach (var deferred in ctx.Deferred)
            {
                if (ReferenceEquals(deferred.OwnerConfiguration, configuration))
                {
                    deferred.Apply();
                }
            }
        }
    }

    private static IReadOnlyList<SchemaConfiguration> TopologicalSort(
        IReadOnlyList<SchemaConfiguration> configurations,
        TurbineBuildContext ctx)
    {
        // Edges: from -> {to, to, ...} meaning "from depends on to" (to must complete first).
        // Compute in-degree as the number of incoming "depends-on" edges (i.e., things that depend on me).
        // We want to process leaves (no dependencies) first.

        var remainingDeps = new Dictionary<SchemaConfiguration, HashSet<SchemaConfiguration>>();
        foreach (var configuration in configurations)
        {
            remainingDeps[configuration] = ctx.Edges.TryGetValue(configuration, out var deps)
                ? new HashSet<SchemaConfiguration>(deps)
                : new HashSet<SchemaConfiguration>();
        }

        var ordered = new List<SchemaConfiguration>(configurations.Count);
        var ready = new Queue<SchemaConfiguration>();
        foreach (var configuration in configurations)
        {
            if (remainingDeps[configuration].Count == 0)
            {
                ready.Enqueue(configuration);
            }
        }

        while (ready.Count > 0)
        {
            var configuration = ready.Dequeue();
            ordered.Add(configuration);

            foreach (var (other, deps) in remainingDeps)
            {
                if (deps.Remove(configuration) && deps.Count == 0 && !ordered.Contains(other) && !ready.Contains(other))
                {
                    ready.Enqueue(other);
                }
            }
        }

        if (ordered.Count != configurations.Count)
        {
            ThrowCycle(configurations, ctx);
        }

        return ordered;
    }

    private static void ThrowCycle(
        IReadOnlyList<SchemaConfiguration> configurations,
        TurbineBuildContext ctx)
    {
        var unresolved = new HashSet<SchemaConfiguration>();
        var ordered = new List<SchemaConfiguration>();
        var remainingDeps = new Dictionary<SchemaConfiguration, HashSet<SchemaConfiguration>>();
        foreach (var configuration in configurations)
        {
            remainingDeps[configuration] = ctx.Edges.TryGetValue(configuration, out var deps)
                ? new HashSet<SchemaConfiguration>(deps)
                : new HashSet<SchemaConfiguration>();
        }
        var ready = new Queue<SchemaConfiguration>();
        foreach (var configuration in configurations)
        {
            if (remainingDeps[configuration].Count == 0)
            {
                ready.Enqueue(configuration);
            }
        }
        while (ready.Count > 0)
        {
            var configuration = ready.Dequeue();
            ordered.Add(configuration);
            foreach (var (other, deps) in remainingDeps)
            {
                if (deps.Remove(configuration) && deps.Count == 0 && !ordered.Contains(other) && !ready.Contains(other))
                {
                    ready.Enqueue(other);
                }
            }
        }
        foreach (var configuration in configurations)
        {
            if (!ordered.Contains(configuration))
            {
                unresolved.Add(configuration);
            }
        }

        var cycle = FindShortestCycle(unresolved, ctx);
        throw new SchemaDependencyCycleException(cycle);
    }

    private static IReadOnlyList<string> FindShortestCycle(
        HashSet<SchemaConfiguration> unresolved,
        TurbineBuildContext ctx)
    {
        var participants = new List<string>();
        var deferredByOwner = ctx.Deferred
            .GroupBy(d => d.OwnerConfiguration)
            .ToDictionary(g => g.Key, g => g.ToList());

        // BFS / DFS to find one cycle in the unresolved sub-graph.
        var start = unresolved.First();
        var path = new List<SchemaConfiguration>();
        var visited = new HashSet<SchemaConfiguration>();
        var found = TryFindCycle(start, start, path, visited, ctx, unresolved);

        if (found.Count == 0)
        {
            // Fallback: just list all unresolved with their deferred deps.
            foreach (var configuration in unresolved)
            {
                if (deferredByOwner.TryGetValue(configuration, out var deferred))
                {
                    foreach (var op in deferred)
                    {
                        participants.Add(FormatNode(op.OwnerConfiguration, op.OwnerSchemaProperty));
                    }
                }
            }
            participants.Add(FormatNode(start, null));
            return participants;
        }

        for (var i = 0; i < found.Count; i++)
        {
            var fromConfig = found[i];
            var toConfig = found[(i + 1) % found.Count];
            var op = FindEdgeOperation(fromConfig, toConfig, ctx);
            participants.Add(FormatNode(fromConfig, op?.OwnerSchemaProperty));
        }
        participants.Add(FormatNode(found[0], null));
        return participants;
    }

    private static List<SchemaConfiguration> TryFindCycle(
        SchemaConfiguration start,
        SchemaConfiguration current,
        List<SchemaConfiguration> path,
        HashSet<SchemaConfiguration> visited,
        TurbineBuildContext ctx,
        HashSet<SchemaConfiguration> scope)
    {
        path.Add(current);
        visited.Add(current);

        if (ctx.Edges.TryGetValue(current, out var neighbours))
        {
            foreach (var neighbour in neighbours)
            {
                if (!scope.Contains(neighbour))
                {
                    continue;
                }
                if (ReferenceEquals(neighbour, start) && path.Count > 1)
                {
                    return new List<SchemaConfiguration>(path);
                }
                if (path.Contains(neighbour))
                {
                    var cycleStart = path.IndexOf(neighbour);
                    return path.GetRange(cycleStart, path.Count - cycleStart);
                }
                if (visited.Contains(neighbour))
                {
                    continue;
                }
                var found = TryFindCycle(start, neighbour, path, visited, ctx, scope);
                if (found.Count > 0)
                {
                    return found;
                }
            }
        }

        path.RemoveAt(path.Count - 1);
        return new List<SchemaConfiguration>();
    }

    private static TurbineBuildContext.DeferredOperation? FindEdgeOperation(
        SchemaConfiguration from,
        SchemaConfiguration to,
        TurbineBuildContext ctx)
    {
        foreach (var op in ctx.Deferred)
        {
            if (ReferenceEquals(op.OwnerConfiguration, from)
                && ReferenceEquals(op.DependsOnConfiguration, to))
            {
                return op;
            }
        }
        return null;
    }

    private static string FormatNode(SchemaConfiguration config, string? schemaProperty)
    {
        var typeName = config.GetType().Name;
        return string.IsNullOrEmpty(schemaProperty) ? typeName : $"{typeName}.{schemaProperty}";
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

        foreach (var field in type.GetFields(
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
        {
            if (!typeof(ISchema).IsAssignableFrom(field.FieldType))
            {
                continue;
            }
            var value = field.GetValue(configuration);
            if (value is ISchema schema)
            {
                schemas[(type, field.Name)] = schema;
            }
        }
    }
}
