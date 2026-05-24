using FactFoundry.TelemetryForge.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.Extensions.DependencyInjection;

namespace FactFoundry.TelemetryForge.Web;

/// <summary>
/// Extension methods for registering TelemetryForge web telemetry services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds TelemetryForge web telemetry services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Action to configure <see cref="WebTelemetryOptions"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddTelemetryForge(
        this IServiceCollection services,
        Action<WebTelemetryOptions> configure)
    {
        var options = new WebTelemetryOptions();
        configure(options);

        services.Configure(configure);
        services.AddTelemetryForgeCore(options);
        services.AddSingleton<IpHashingService>();
        services.AddHttpContextAccessor();
        services.AddScoped<TelemetryForgeCircuitHandler>();
        services.AddScoped<CircuitHandler>(sp => sp.GetRequiredService<TelemetryForgeCircuitHandler>());

        return services;
    }

    /// <summary>
    /// Adds TelemetryForge middleware to the HTTP pipeline for non-Blazor request tracking.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The application builder for chaining.</returns>
    public static IApplicationBuilder UseTelemetryForge(this IApplicationBuilder app)
    {
        return app.UseMiddleware<TelemetryForgeMiddleware>();
    }
}
