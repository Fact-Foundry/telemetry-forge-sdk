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
        var options = new DesktopTelemetryOptions();
        configure(options);

        services.Configure(configure);

        services.AddHttpClient<ITelemetryClient, TelemetryForgeHttpClient>(client =>
        {
            client.BaseAddress = new Uri(options.Endpoint.TrimEnd('/'));
            client.DefaultRequestHeaders.Add("X-TelemetryForge-Key", options.ApiKey);
        })
        .AddStandardResilienceHandler();

        services.AddSingleton<IMachineFingerprint, MachineFingerprint>();
        services.AddSingleton<DesktopSessionTracker>();
        services.AddSingleton<IFeatureTracker>(sp => sp.GetRequiredService<DesktopSessionTracker>());

        return services;
    }
}
