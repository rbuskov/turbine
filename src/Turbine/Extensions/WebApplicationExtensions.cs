using Microsoft.AspNetCore.Builder;

namespace Turbine;

public static class WebApplicationExtensions
{
    public static WebApplication MapTurbine(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app;
    }
}
