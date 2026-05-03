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
        
        return builder.WithMetadata(/* metadata */);
    }

    public static RouteHandlerBuilder Accepts<T>(
        this RouteHandlerBuilder builder,
        Expression<Func<T, ISchema>> selector)
        where T : SchemaConfiguration
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(selector);

        return builder.WithMetadata(/* metadata */);
    }
    
}