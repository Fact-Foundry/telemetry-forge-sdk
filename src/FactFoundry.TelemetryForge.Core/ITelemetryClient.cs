namespace FactFoundry.TelemetryForge.Core;

/// <summary>
/// Sends telemetry payloads to a TelemetryForge Server instance.
/// </summary>
public interface ITelemetryClient
{
    /// <summary>
    /// Posts a telemetry payload to the specified path on the configured server.
    /// Failures are logged and swallowed — this method never throws.
    /// </summary>
    /// <typeparam name="T">The payload type to serialize as JSON.</typeparam>
    /// <param name="path">The API path (e.g., "/api/telemetry/web").</param>
    /// <param name="payload">The payload object.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendAsync<T>(string path, T payload, CancellationToken cancellationToken = default);
}
