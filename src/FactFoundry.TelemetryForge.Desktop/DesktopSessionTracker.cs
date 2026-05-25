using System.Reflection;
using FactFoundry.TelemetryForge.Desktop.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FactFoundry.TelemetryForge.Desktop;

/// <summary>
/// Tracks the desktop application session lifecycle and flushes the telemetry payload on dispose.
/// </summary>
public sealed class DesktopSessionTracker : IFeatureTracker, IAsyncDisposable, IDisposable
{
    private readonly ITelemetryClient _client;
    private readonly IMachineFingerprint _fingerprint;
    private readonly DesktopTelemetryOptions _options;
    private readonly ILogger<DesktopSessionTracker> _logger;
    private readonly DateTimeOffset _sessionStart = DateTimeOffset.UtcNow;
    private readonly List<string> _featurePath = [];
    private readonly List<ErrorEvent> _errorEvents = [];
    private readonly object _lock = new();
    private bool _flushed;

    public DesktopSessionTracker(
        ITelemetryClient client,
        IMachineFingerprint fingerprint,
        IOptions<DesktopTelemetryOptions> options,
        ILogger<DesktopSessionTracker> logger)
    {
        _client = client;
        _fingerprint = fingerprint;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public void TrackFeature(string featureName)
    {
        lock (_lock)
        {
            _featurePath.Add(featureName);
        }
    }

    /// <inheritdoc />
    public void TrackError(string featureName, string message)
    {
        lock (_lock)
        {
            _errorEvents.Add(new ErrorEvent
            {
                Feature = featureName,
                Message = message,
                Timestamp = DateTimeOffset.UtcNow
            });
        }
    }

    /// <summary>
    /// Flushes the session payload to the TelemetryForge Server.
    /// Called automatically on dispose; safe to call manually for explicit flush.
    /// </summary>
    public async Task FlushAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (_flushed)
                return;
            _flushed = true;
        }

        var sessionEnd = DateTimeOffset.UtcNow;
        var payload = new DesktopSessionPayload
        {
            AppVersion = _options.AppVersion ?? GetEntryAssemblyVersion(),
            Platform = _fingerprint.GetPlatform(),
            OsVersion = Environment.OSVersion.ToString(),
            FingerprintHash = _fingerprint.GetFingerprintHash(),
            LicenseJwt = _options.LicenseJwt,
            SessionStart = _sessionStart,
            SessionEnd = sessionEnd,
            DurationMs = (long)(sessionEnd - _sessionStart).TotalMilliseconds,
            FeaturePath = _featurePath.ToList(),
            ErrorEvents = _errorEvents.ToList()
        };

        await _client.SendAsync("/api/telemetry/desktop", payload, cancellationToken);
        _logger.LogDebug("Desktop telemetry session flushed ({DurationMs}ms)", payload.DurationMs);
    }

    public async ValueTask DisposeAsync()
    {
        await FlushAsync();
    }

    public void Dispose()
    {
        FlushAsync().GetAwaiter().GetResult();
    }

    private static string? GetEntryAssemblyVersion()
    {
        return Assembly.GetEntryAssembly()?.GetName().Version?.ToString();
    }
}
