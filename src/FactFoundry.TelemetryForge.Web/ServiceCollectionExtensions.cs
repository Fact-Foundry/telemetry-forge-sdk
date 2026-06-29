using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;

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

        services.AddHttpClient<ITelemetryClient, TelemetryForgeHttpClient>(client =>
        {
            client.BaseAddress = new Uri(options.Endpoint.TrimEnd('/'));
            client.DefaultRequestHeaders.Add("X-TelemetryForge-Key", options.ApiKey);
        })
        .AddStandardResilienceHandler();

        services.AddHttpContextAccessor();
        services.AddSingleton<RequestContextAccessor>();
        services.AddScoped<TelemetryForgeCircuitHandler>();
        services.AddScoped<CircuitHandler>(sp => sp.GetRequiredService<TelemetryForgeCircuitHandler>());
        services.AddScoped<ITelemetryForge>(sp => sp.GetRequiredService<TelemetryForgeCircuitHandler>());

        return services;
    }

    /// <summary>
    /// Adds TelemetryForge middleware to the HTTP pipeline for non-Blazor request tracking.
    /// Safe to call without a prior <see cref="AddTelemetryForge"/> registration — the
    /// middleware will not run and a warning is logged.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The application builder for chaining.</returns>
    public static IApplicationBuilder UseTelemetryForge(this IApplicationBuilder app)
    {
        var checker = app.ApplicationServices.GetService<IServiceProviderIsService>();
        if (checker is not null && !checker.IsService(typeof(ITelemetryClient)))
        {
            var logger = app.ApplicationServices.GetService<ILoggerFactory>()
                ?.CreateLogger("FactFoundry.TelemetryForge.Web");
            logger?.LogWarning(
                "UseTelemetryForge() was called without a prior AddTelemetryForge() registration. " +
                "The middleware will not run.");
            return app;
        }

        return app.UseMiddleware<TelemetryForgeMiddleware>();
    }
}
