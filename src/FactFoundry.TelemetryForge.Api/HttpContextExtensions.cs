using Microsoft.AspNetCore.Http;

namespace FactFoundry.TelemetryForge.Api;

/// <summary>
/// Extensions for tagging the current request with consumer-defined telemetry dimensions.
/// </summary>
public static class HttpContextExtensions
{
    internal const string OutcomeItemKey = "TelemetryForge:Outcome";

    /// <summary>
    /// Records a business outcome for the current request (e.g. "license_valid",
    /// "license_rejected"). Distinct from the HTTP status code — a 200 response can still
    /// carry a business failure. The API telemetry middleware reads this and includes it as
    /// <c>outcome</c> in the event. Keep values low-cardinality (a small fixed set), not
    /// free-form text or identifiers.
    /// </summary>
    /// <param name="context">The current HTTP context.</param>
    /// <param name="outcome">A short, low-cardinality outcome label.</param>
    public static void SetTelemetryOutcome(this HttpContext context, string outcome)
    {
        context.Items[OutcomeItemKey] = outcome;
    }
}
