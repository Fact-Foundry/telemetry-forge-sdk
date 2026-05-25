using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;

namespace FactFoundry.TelemetryForge.Core;

/// <summary>
/// Extension methods for registering the TelemetryForge HTTP client with resilience.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the <see cref="ITelemetryClient"/> with resilience policies configured
    /// from the provided options.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="options">Telemetry options containing endpoint and API key.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddTelemetryForgeCore(
        this IServiceCollection services,
        TelemetryOptionsBase options)
    {
        services.AddHttpClient<ITelemetryClient, TelemetryForgeHttpClient>(client =>
        {
            client.BaseAddress = new Uri(options.Endpoint.TrimEnd('/'));
            client.DefaultRequestHeaders.Add("X-TelemetryForge-Key", options.ApiKey);
        })
        .AddStandardResilienceHandler();

        return services;
    }
}
