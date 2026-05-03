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

        foreach (var group in context.DescriptionGroups)
        {
            foreach (var description in group.Items)
            {
                ApplyToOperation(document, description, componentBuilder);
            }
        }

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
        ComponentBuilder components)
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

        foreach (var entry in turbineMetadata)
        {
            var reference = components.GetOrAddComponentRef(entry.ConfigurationType, entry.SchemaPropertyName);
            switch (entry.Role)
            {
                case EndpointSchemaRole.Request:
                    ApplyRequestBody(operation, entry, reference);
                    break;
                case EndpointSchemaRole.Response:
                    ApplyResponse(operation, entry, reference);
                    break;
            }
        }
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
        if (!document.Paths.TryGetValue(path, out var pathItem) || pathItem is null)
        {
            return null;
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
        IOpenApiSchema reference)
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
        body.Content[entry.ContentType] = new OpenApiMediaType { Schema = reference };
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
