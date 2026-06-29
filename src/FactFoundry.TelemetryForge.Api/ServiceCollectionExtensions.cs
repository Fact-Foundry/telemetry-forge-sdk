using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;

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
        var options = new ApiTelemetryOptions();
        configure(options);

        services.Configure(configure);

        services.AddHttpClient<ITelemetryClient, TelemetryForgeHttpClient>(client =>
        {
            client.BaseAddress = new Uri(options.Endpoint.TrimEnd('/'));
            client.DefaultRequestHeaders.Add("X-TelemetryForge-Key", options.ApiKey);
            client.DefaultRequestHeaders.Add("X-TelemetryForge-Sdk-Version", SdkVersion);
        })
        .AddStandardResilienceHandler();

        return services;
    }

    /// <summary>
    /// This SDK's version, read once from the assembly's informational version (build
    /// metadata after '+' stripped). Sent as a header on every telemetry post so the server
    /// can record which SDK version each app is running, without any per-event cost.
    /// </summary>
    private static readonly string SdkVersion = ResolveSdkVersion();

    private static string ResolveSdkVersion()
    {
        var informational = typeof(ServiceCollectionExtensions).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (!string.IsNullOrEmpty(informational))
        {
            var plus = informational.IndexOf('+');
            return plus >= 0 ? informational[..plus] : informational;
        }

        return typeof(ServiceCollectionExtensions).Assembly.GetName().Version?.ToString() ?? "unknown";
    }

    /// <summary>
    /// Adds TelemetryForge API telemetry middleware to the HTTP pipeline.
    /// Call this after <c>UseRouting</c> so the matched route template is available.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The application builder for chaining.</returns>
    public static IApplicationBuilder UseTelemetryForgeApi(this IApplicationBuilder app)
    {
        return app.UseMiddleware<TelemetryForgeApiMiddleware>();
    }
}
