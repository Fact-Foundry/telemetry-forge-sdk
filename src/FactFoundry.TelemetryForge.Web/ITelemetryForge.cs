namespace FactFoundry.TelemetryForge.Web;

/// <summary>
/// Public API for sending custom telemetry events to the TelemetryForge Server.
/// Inject this interface to track application-specific events within a Blazor circuit.
/// </summary>
public interface ITelemetryForge
{
    /// <summary>
    /// Sends a custom event to the TelemetryForge Server.
    /// This method is fire-and-forget — it never throws and does not block the caller.
    /// </summary>
    /// <param name="eventName">The custom event name (e.g., "form_submit", "button_click").</param>
    /// <param name="data">Optional key-value data associated with the event.</param>
    void TrackEvent(string eventName, Dictionary<string, object>? data = null);
}
