namespace FactFoundry.TelemetryForge.Desktop;

/// <summary>
/// Tracks feature/component navigation within a desktop application session.
/// </summary>
public interface IFeatureTracker
{
    /// <summary>
    /// Records that the user navigated to a feature or component.
    /// </summary>
    /// <param name="featureName">The feature or component name (e.g., "EditorPanel", "Settings").</param>
    void TrackFeature(string featureName);

    /// <summary>
    /// Records an error that occurred within a specific feature.
    /// </summary>
    /// <param name="featureName">The feature where the error occurred.</param>
    /// <param name="message">Human-readable error message.</param>
    void TrackError(string featureName, string message);
}
