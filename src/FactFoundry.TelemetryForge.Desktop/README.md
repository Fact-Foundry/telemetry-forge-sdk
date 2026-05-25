# FactFoundry.TelemetryForge.Desktop

Desktop app telemetry for [TelemetryForge](https://github.com/Fact-Foundry/telemetry-forge) — machine fingerprinting, session tracking, feature navigation, and periodic heartbeats.

## Installation

```bash
dotnet add package FactFoundry.TelemetryForge.Desktop
```

## Setup

```csharp
builder.Services.AddTelemetryForge(options =>
{
    options.Endpoint   = "https://telemetry.yourdomain.com";
    options.ApiKey     = "your-app-api-key";
    options.AppVersion = "2.1.0";       // auto-populated from entry assembly if omitted
    options.HeartbeatIntervalMinutes = 15; // default: 15. Set to null/0 to disable
});
```

## Usage

Inject `IFeatureTracker` anywhere in your app to record feature navigation and errors:

```csharp
public class EditorViewModel
{
    private readonly IFeatureTracker _tracker;

    public EditorViewModel(IFeatureTracker tracker)
    {
        _tracker = tracker;
        _tracker.TrackFeature("ModelEditor");
    }

    public void Export()
    {
        try { /* ... */ }
        catch (Exception ex)
        {
            _tracker.TrackError("Export", ex.Message);
        }
    }
}
```

On shutdown, flush remaining data:

```csharp
await host.Services.GetRequiredService<DesktopSessionTracker>().FlushAsync();
```

Or let `IAsyncDisposable` handle it automatically when the DI container is disposed.

## Heartbeats

The tracker periodically flushes feature and error data to the server on a configurable interval (default: every 15 minutes). Each heartbeat sends only the entries accumulated since the last flush — not the full session. A final flush at shutdown sends any remaining data.

Set `HeartbeatIntervalMinutes` to `null` or `0` to disable periodic heartbeats and only flush at shutdown.

## What Gets Sent

Each payload is posted to `POST /api/telemetry/desktop`:

| Field | Description |
|---|---|
| `session_id` | UUID generated once per app session |
| `sequence` | Monotonically increasing counter (0, 1, 2, ...) |
| `fingerprint_hash` | SHA-256 hash of the machine identifier |
| `platform` | `windows`, `linux`, or `macos` |
| `os_version` | Full OS version string |
| `app_version` | Application version |
| `feature_path` | Features navigated since last flush |
| `error_events` | Errors recorded since last flush |

## Machine Fingerprinting

Platform-specific, stable machine identity:

| Platform | Source |
|---|---|
| Windows | `MachineGuid` from the registry |
| Linux | `/etc/machine-id` |
| macOS | `IOPlatformUUID` via `ioreg` |

The raw identifier is SHA-256 hashed before transmission — the server never sees the original value.

## Privacy

- Machine identifiers are hashed client-side before transmission — raw values never leave the device
- No names, emails, or account identifiers collected
- Telemetry failures are logged and swallowed — the SDK never crashes your app

## Requirements

- .NET 8, 9, or 10
- A running [TelemetryForge Server](https://github.com/Fact-Foundry/telemetry-forge) instance

## License

MIT
