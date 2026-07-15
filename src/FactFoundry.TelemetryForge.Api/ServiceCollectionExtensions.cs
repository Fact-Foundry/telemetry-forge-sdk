using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;

namespace FactFoundry.TelemetryForge.Api;

/// <summary>
/// Extension methods for registering TelemetryForge API telemetry services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds TelemetryForge API telemetry services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Action to configure <see cref="ApiTelemetryOptions"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddTelemetryForgeApi(
        this IServiceCollection services,
        Action<ApiTelemetryOptions> configure)
    {
        services.Configure(configure);

        // A single named client carries the resilience policies; TelemetryForgeHttpClient
        // sets the per-target URL, API key, and SDK-version header per request so it can fan
        // out to the primary endpoint plus any configured mirrors.
        services.AddHttpClient(TelemetryForgeHttpClient.HttpClientName)
            .AddStandardResilienceHandler();

        services.AddSingleton<ITelemetryClient, TelemetryForgeHttpClient>();

        return services;
    }

    /// <summary>
    /// Adds TelemetryForge API telemetry middleware to the HTTP pipeline.
    /// Call this after <c>UseRouting</c> so the matched route template is available.
    /// Safe to call without a prior <see cref="AddTelemetryForgeApi"/> registration — the
    /// middleware will not run and a warning is logged.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The application builder for chaining.</returns>
    public static IApplicationBuilder UseTelemetryForgeApi(this IApplicationBuilder app)
    {
        var checker = app.ApplicationServices.GetService<IServiceProviderIsService>();
        if (checker is not null && !checker.IsService(typeof(ITelemetryClient)))
        {
            var logger = app.ApplicationServices.GetService<ILoggerFactory>()
                ?.CreateLogger("FactFoundry.TelemetryForge.Api");
            logger?.LogWarning(
                "UseTelemetryForgeApi() was called without a prior AddTelemetryForgeApi() registration. " +
                "The middleware will not run.");
            return app;
        }

        return app.UseMiddleware<TelemetryForgeApiMiddleware>();
    }
}
