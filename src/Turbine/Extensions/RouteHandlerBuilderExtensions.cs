using System.Linq.Expressions;
using Microsoft.AspNetCore.Builder;

namespace Turbine;

public static class RouteHandlerBuilderExtensions
{
    public static RouteHandlerBuilder Produces<T>(
        this RouteHandlerBuilder builder,
        int statusCode,
        Expression<Func<T, ISchema>> selector)
        where T : SchemaConfiguration
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(selector);
        if (statusCode < 100 || statusCode >= 600)
        {
            throw new ArgumentOutOfRangeException(
                nameof(statusCode),
                statusCode,
                "HTTP status code must be in the range [100, 599].");
        }

        var propertyName = SchemaSelectorParser.ParsePropertyName(selector, nameof(selector));
        var metadata = new TurbineEndpointSchemaMetadata(
            ConfigurationType: typeof(T),
            SchemaPropertyName: propertyName,
            Role: EndpointSchemaRole.Response,
            StatusCode: statusCode,
            ContentType: TurbineEndpointSchemaMetadata.DefaultContentType);
        return builder.WithMetadata(metadata);
    }

    public static RouteHandlerBuilder Accepts<T>(
        this RouteHandlerBuilder builder,
        Expression<Func<T, ISchema>> selector)
        where T : SchemaConfiguration
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(selector);

        var propertyName = SchemaSelectorParser.ParsePropertyName(selector, nameof(selector));
        var metadata = new TurbineEndpointSchemaMetadata(
            ConfigurationType: typeof(T),
            SchemaPropertyName: propertyName,
            Role: EndpointSchemaRole.Request,
            StatusCode: null,
            ContentType: TurbineEndpointSchemaMetadata.DefaultContentType);
        return builder.WithMetadata(metadata);
    }
}
