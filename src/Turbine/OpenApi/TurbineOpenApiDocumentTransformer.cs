using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;

namespace Turbine;

internal sealed class TurbineOpenApiDocumentTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(context);

        var registry = context.ApplicationServices.GetService<TurbineSchemaRegistry>();
        if (registry is null || !registry.IsBuilt)
        {
            // No Turbine in this app, or registry not built yet (MapTurbine not called):
            // there is nothing to contribute.
            return Task.CompletedTask;
        }

        document.Components ??= new OpenApiComponents();
        document.Components.Schemas ??= new Dictionary<string, IOpenApiSchema>(StringComparer.Ordinal);

        var ownedReverse = BuildReverseLookup(registry);
        var componentBuilder = new ComponentBuilder(document, registry, ownedReverse);
        var displacedRefs = new HashSet<string>(StringComparer.Ordinal);

        foreach (var group in context.DescriptionGroups)
        {
            foreach (var description in group.Items)
            {
                ApplyToOperation(document, description, componentBuilder, displacedRefs);
            }
        }

        PruneOrphanedDisplacedComponents(document, displacedRefs);

        return Task.CompletedTask;
    }

    private static Dictionary<ISchema, (Type ConfigurationType, string PropertyName)> BuildReverseLookup(
        TurbineSchemaRegistry registry)
    {
        var result = new Dictionary<ISchema, (Type, string)>(ReferenceEqualityComparer.Instance);
        foreach (var (configType, propertyName, schema) in registry.Entries)
        {
            result[schema] = (configType, propertyName);
        }
        return result;
    }

    private static void ApplyToOperation(
        OpenApiDocument document,
        ApiDescription description,
        ComponentBuilder components,
        HashSet<string> displacedRefs)
    {
        var endpointMetadata = description.ActionDescriptor?.EndpointMetadata;
        if (endpointMetadata is null)
        {
            return;
        }

        TurbineEndpointSchemaMetadata[] turbineMetadata = endpointMetadata
            .OfType<TurbineEndpointSchemaMetadata>()
            .ToArray();
        if (turbineMetadata.Length == 0)
        {
            return;
        }

        var operation = FindOperation(document, description);
        if (operation is null)
        {
            return;
        }

        var configuredStatuses = new HashSet<int>();
        foreach (var entry in turbineMetadata)
        {
            var reference = components.GetOrAddComponentRef(entry.ConfigurationType, entry.SchemaPropertyName);
            switch (entry.Role)
            {
                case EndpointSchemaRole.Request:
                    ApplyRequestBody(operation, entry, reference, displacedRefs);
                    break;
                case EndpointSchemaRole.Response:
                    ApplyResponse(operation, entry, reference);
                    if (entry.StatusCode is { } status)
                    {
                        configuredStatuses.Add(status);
                    }
                    break;
            }
        }

        DropInferredEmpty200(operation, configuredStatuses);
    }

    private static void DropInferredEmpty200(OpenApiOperation operation, HashSet<int> configuredStatuses)
    {
        // ASP.NET's OpenAPI generator emits a default "200 OK" with no content for handlers
        // that return Task<IResult>. When the user configured a different success status via
        // .Produces(...) and never asked for 200, the inferred 200 is just noise — drop it.
        if (configuredStatuses.Count == 0 || configuredStatuses.Contains(200))
        {
            return;
        }
        if (operation.Responses is null)
        {
            return;
        }
        if (!operation.Responses.TryGetValue("200", out var existing) || existing is not OpenApiResponse response)
        {
            return;
        }
        if (response.Content is { Count: > 0 })
        {
            return;
        }
        operation.Responses.Remove("200");
    }

    private static OpenApiOperation? FindOperation(OpenApiDocument document, ApiDescription description)
    {
        if (document.Paths is null)
        {
            return null;
        }
        var path = "/" + (description.RelativePath ?? string.Empty).TrimStart('/');
        // Strip query string component if present.
        var queryIndex = path.IndexOf('?', StringComparison.Ordinal);
        if (queryIndex >= 0)
        {
            path = path[..queryIndex];
        }
        // ApiDescription.RelativePath can carry a trailing slash for routes mapped
        // through a group with an empty child pattern (e.g. MapGroup("x").MapGet("")).
        // OpenApiDocument.Paths keys do not include the trailing slash, so try the
        // trimmed form when the literal lookup misses.
        if (!document.Paths.TryGetValue(path, out var pathItem) || pathItem is null)
        {
            if (path.Length > 1 && path.EndsWith('/'))
            {
                var trimmed = path.TrimEnd('/');
                if (!document.Paths.TryGetValue(trimmed, out pathItem) || pathItem is null)
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }
        if (description.HttpMethod is null)
        {
            return null;
        }
        var method = System.Net.Http.HttpMethod.Parse(description.HttpMethod);
        if (pathItem is OpenApiPathItem concrete && concrete.Operations is not null
            && concrete.Operations.TryGetValue(method, out var op))
        {
            return op;
        }
        return null;
    }

    private static void ApplyRequestBody(
        OpenApiOperation operation,
        TurbineEndpointSchemaMetadata entry,
        IOpenApiSchema reference,
        HashSet<string> displacedRefs)
    {
        var body = operation.RequestBody as OpenApiRequestBody;
        if (body is null)
        {
            body = new OpenApiRequestBody
            {
                Required = true,
                Content = new Dictionary<string, OpenApiMediaType>(StringComparer.Ordinal),
            };
            operation.RequestBody = body;
        }
        body.Content ??= new Dictionary<string, OpenApiMediaType>(StringComparer.Ordinal);
        if (body.Content.TryGetValue(entry.ContentType, out var existingMedia)
            && existingMedia.Schema is OpenApiSchemaReference displaced
            && displaced.Reference?.Id is { Length: > 0 } id)
        {
            displacedRefs.Add(id);
        }
        body.Content[entry.ContentType] = new OpenApiMediaType { Schema = reference };
    }

    private static void PruneOrphanedDisplacedComponents(OpenApiDocument document, HashSet<string> displacedRefs)
    {
        if (displacedRefs.Count == 0 || document.Components?.Schemas is not { } schemas)
        {
            return;
        }
        var stillReferenced = CollectReferencedComponentIds(document);
        foreach (var id in displacedRefs)
        {
            if (!stillReferenced.Contains(id))
            {
                schemas.Remove(id);
            }
        }
    }

    private static HashSet<string> CollectReferencedComponentIds(OpenApiDocument document)
    {
        var refs = new HashSet<string>(StringComparer.Ordinal);
        if (document.Paths is null)
        {
            return refs;
        }
        foreach (var (_, pathItem) in document.Paths)
        {
            if (pathItem is not OpenApiPathItem concrete || concrete.Operations is null)
            {
                continue;
            }
            foreach (var (_, op) in concrete.Operations)
            {
                CollectFromOperation(op, refs);
            }
        }
        if (document.Components?.Schemas is { } schemas)
        {
            foreach (var (_, schema) in schemas)
            {
                CollectFromSchema(schema, refs);
            }
        }
        return refs;
    }

    private static void CollectFromOperation(OpenApiOperation operation, HashSet<string> refs)
    {
        if (operation.RequestBody is OpenApiRequestBody body && body.Content is not null)
        {
            foreach (var (_, media) in body.Content)
            {
                CollectFromSchema(media.Schema, refs);
            }
        }
        if (operation.Responses is not null)
        {
            foreach (var (_, resp) in operation.Responses)
            {
                if (resp is OpenApiResponse r && r.Content is not null)
                {
                    foreach (var (_, media) in r.Content)
                    {
                        CollectFromSchema(media.Schema, refs);
                    }
                }
            }
        }
        if (operation.Parameters is not null)
        {
            foreach (var p in operation.Parameters)
            {
                if (p is OpenApiParameter param)
                {
                    CollectFromSchema(param.Schema, refs);
                }
            }
        }
    }

    private static void CollectFromSchema(IOpenApiSchema? schema, HashSet<string> refs)
    {
        switch (schema)
        {
            case null:
                return;
            case OpenApiSchemaReference reference when reference.Reference?.Id is { Length: > 0 } id:
                if (refs.Add(id))
                {
                    // Also walk the referenced component itself.
                    if (reference.Target is OpenApiSchema concrete)
                    {
                        WalkSchemaChildren(concrete, refs);
                    }
                }
                return;
            case OpenApiSchema concrete:
                WalkSchemaChildren(concrete, refs);
                return;
        }
    }

    private static void WalkSchemaChildren(OpenApiSchema schema, HashSet<string> refs)
    {
        if (schema.Properties is not null)
        {
            foreach (var (_, child) in schema.Properties)
            {
                CollectFromSchema(child, refs);
            }
        }
        CollectFromSchema(schema.Items, refs);
        if (schema.AllOf is not null)
        {
            foreach (var s in schema.AllOf) CollectFromSchema(s, refs);
        }
        if (schema.OneOf is not null)
        {
            foreach (var s in schema.OneOf) CollectFromSchema(s, refs);
        }
        if (schema.AnyOf is not null)
        {
            foreach (var s in schema.AnyOf) CollectFromSchema(s, refs);
        }
        CollectFromSchema(schema.AdditionalProperties, refs);
    }

    private static void ApplyResponse(
        OpenApiOperation operation,
        TurbineEndpointSchemaMetadata entry,
        IOpenApiSchema reference)
    {
        operation.Responses ??= new OpenApiResponses();
        var statusKey = entry.StatusCode!.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (!operation.Responses.TryGetValue(statusKey, out var existing) || existing is not OpenApiResponse response)
        {
            response = new OpenApiResponse
            {
                Description = Microsoft.AspNetCore.WebUtilities.ReasonPhrases.GetReasonPhrase(entry.StatusCode.Value),
                Content = new Dictionary<string, OpenApiMediaType>(StringComparer.Ordinal),
            };
            operation.Responses[statusKey] = response;
        }
        response.Content ??= new Dictionary<string, OpenApiMediaType>(StringComparer.Ordinal);
        response.Content[entry.ContentType] = new OpenApiMediaType { Schema = reference };
    }

    private sealed class ComponentBuilder
    {
        private readonly OpenApiDocument document;
        private readonly TurbineSchemaRegistry registry;
        private readonly Dictionary<ISchema, (Type ConfigurationType, string PropertyName)> ownedReverse;
        private readonly SchemaToOpenApiConverter converter;
        private readonly HashSet<string> inProgress = new(StringComparer.Ordinal);

        public ComponentBuilder(
            OpenApiDocument document,
            TurbineSchemaRegistry registry,
            Dictionary<ISchema, (Type ConfigurationType, string PropertyName)> ownedReverse)
        {
            this.document = document;
            this.registry = registry;
            this.ownedReverse = ownedReverse;
            this.converter = new SchemaToOpenApiConverter(ResolveOwnedReference);
        }

        public IOpenApiSchema GetOrAddComponentRef(Type configurationType, string propertyName)
        {
            var schema = registry.Resolve(configurationType, propertyName)
                ?? throw new InvalidOperationException(
                    $"Turbine OpenAPI generation could not find schema '{configurationType.FullName}.{propertyName}' in the registry. " +
                    $"Ensure the schema is declared on the SchemaConfiguration and that AddTurbine discovered it.");

            var name = TurbineSchemaComponentNamer.GetComponentName(configurationType, propertyName);
            var schemas = document.Components!.Schemas!;
            if (!schemas.ContainsKey(name))
            {
                schemas[name] = new OpenApiSchema(); // placeholder so cycles see something
                inProgress.Add(name);
                var converted = converter.Convert(schema);
                schemas[name] = converted;
                inProgress.Remove(name);
            }
            return new OpenApiSchemaReference(name, document, externalResource: null);
        }

        private IOpenApiSchema? ResolveOwnedReference(ISchema schema)
        {
            if (!ownedReverse.TryGetValue(schema, out var owner))
            {
                return null;
            }
            return GetOrAddComponentRef(owner.ConfigurationType, owner.PropertyName);
        }
    }
}
