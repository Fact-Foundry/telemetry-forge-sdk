using System.Reflection;
using FactFoundry.TelemetryForge.Desktop.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FactFoundry.TelemetryForge.Desktop;

/// <summary>
/// Tracks the desktop application session lifecycle and flushes telemetry payloads
/// to the server on a configurable heartbeat interval and at shutdown.
/// Each heartbeat sends only the feature/error entries accumulated since the last flush.
/// </summary>
public sealed class DesktopSessionTracker : IFeatureTracker, IAsyncDisposable, IDisposable
{
    private readonly ITelemetryClient _client;
    private readonly IMachineFingerprint _fingerprint;
    private readonly DesktopTelemetryOptions _options;
    private readonly ILogger<DesktopSessionTracker> _logger;
    private readonly string _sessionId = Guid.NewGuid().ToString();
    private readonly DateTimeOffset _sessionStart = DateTimeOffset.UtcNow;
    private readonly List<string> _featurePath = [];
    private readonly List<ErrorEvent> _errorEvents = [];
    private readonly object _lock = new();
    private readonly Timer? _heartbeatTimer;

    private int _sequence;
    private int _featuresSentCount;
    private int _errorsSentCount;
    private bool _disposed;

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

        if (_options.HeartbeatIntervalMinutes is > 0)
        {
            var interval = TimeSpan.FromMinutes(_options.HeartbeatIntervalMinutes.Value);
            _heartbeatTimer = new Timer(OnHeartbeat, null, interval, interval);
        }
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
    /// Flushes any unsent feature/error data to the TelemetryForge Server.
    /// Called automatically by the heartbeat timer and on dispose.
    /// Safe to call manually for explicit flush.
    /// </summary>
    public async Task FlushAsync(CancellationToken cancellationToken = default)
    {
        List<string> featureDelta;
        List<ErrorEvent> errorDelta;
        int sequence;

        lock (_lock)
        {
            if (_disposed)
                return;

            featureDelta = _featurePath.Skip(_featuresSentCount).ToList();
            errorDelta = _errorEvents.Skip(_errorsSentCount).ToList();

            if (featureDelta.Count == 0 && errorDelta.Count == 0 && _sequence > 0)
                return;

            _featuresSentCount = _featurePath.Count;
            _errorsSentCount = _errorEvents.Count;
            sequence = _sequence++;
        }

        var sessionEnd = DateTimeOffset.UtcNow;
        var payload = new DesktopSessionPayload
        {
            SessionId = _sessionId,
            Sequence = sequence,
            AppVersion = _options.AppVersion ?? GetEntryAssemblyVersion(),
            Platform = _fingerprint.GetPlatform(),
            OsVersion = Environment.OSVersion.ToString(),
            FingerprintHash = _fingerprint.GetFingerprintHash(),
            SessionStart = _sessionStart,
            SessionEnd = sessionEnd,
            DurationMs = (long)(sessionEnd - _sessionStart).TotalMilliseconds,
            FeaturePath = featureDelta,
            ErrorEvents = errorDelta
        };

        await _client.SendAsync("/api/telemetry/desktop", payload, cancellationToken);
        _logger.LogDebug(
            "Desktop telemetry flushed (seq={Sequence}, features={Features}, errors={Errors})",
            sequence, featureDelta.Count, errorDelta.Count);
    }

    public async ValueTask DisposeAsync()
    {
        if (_heartbeatTimer is not null)
            await _heartbeatTimer.DisposeAsync();

        lock (_lock)
        {
            _disposed = true;
        }

        await FlushAsync();
    }

    public void Dispose()
    {
        _heartbeatTimer?.Dispose();

        lock (_lock)
        {
            _disposed = true;
        }

        FlushAsync().GetAwaiter().GetResult();
    }

    private void OnHeartbeat(object? state)
    {
        try
        {
            FlushAsync().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Heartbeat flush failed");
        }
    }

    private static string? GetEntryAssemblyVersion()
    {
        return Assembly.GetEntryAssembly()?.GetName().Version?.ToString();
    }
}
