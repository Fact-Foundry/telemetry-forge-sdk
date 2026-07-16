using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;

namespace FactFoundry.TelemetryForge.Desktop;

/// <summary>
/// Extension methods for registering TelemetryForge desktop telemetry services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds TelemetryForge desktop telemetry services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Action to configure <see cref="DesktopTelemetryOptions"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddTelemetryForge(
        this IServiceCollection services,
        Action<DesktopTelemetryOptions> configure)
    {
        services.Configure(configure);

        // A single named client carries the resilience policies; TelemetryForgeHttpClient
        // sets the per-target URL and API key per request so it can fan out to the primary
        // endpoint plus any configured mirrors.
        services.AddHttpClient(TelemetryForgeHttpClient.HttpClientName)
            .AddStandardResilienceHandler();

        services.AddSingleton<ITelemetryClient, TelemetryForgeHttpClient>();

        services.AddSingleton<IMachineFingerprint, MachineFingerprint>();
        services.AddSingleton<DesktopSessionTracker>();
        services.AddSingleton<IFeatureTracker>(sp => sp.GetRequiredService<DesktopSessionTracker>());

        return services;
    }
}
