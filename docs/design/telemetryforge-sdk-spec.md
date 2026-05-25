# TelemetryForge SDK — Design Specification

*A Fact Foundry product*

## Overview

Client-side NuGet packages that .NET applications install to send telemetry data to a TelemetryForge Server instance. Designed as privacy-first, GDPR-friendly libraries with zero UI, no JavaScript dependencies, no cookies set by the library, and no cross-site tracking.

**Packages:**

| Package | Target | License |
|---|---|---|
| `FactFoundry.TelemetryForge.Web` | ASP.NET / Blazor Server web apps | MIT |
| `FactFoundry.TelemetryForge.Desktop` | WPF / WinForms / Avalonia / console apps | MIT |
| `FactFoundry.TelemetryForge.Mobile` | .NET MAUI (iOS / Android) | MIT (deferred) |

The server (`FactFoundry.TelemetryForge.Server`, AGPL-3.0) is a separate repository.

---

## Design Principles

1. **Never crash the host** — telemetry failures are logged and swallowed. No exception ever escapes the SDK.
2. **Privacy by default** — raw IP addresses are hashed before transmission. Machine fingerprints are SHA-256 hashed client-side. DNT headers are respected.
3. **Minimal dependencies** — prefer .NET built-in APIs. Every transitive dependency becomes a cost for consumers.
4. **Self-contained packages** — each package ships its own HTTP client, interfaces, and options. No shared "core" library to version-lock consumers.
5. **Resilient delivery** — HTTP clients use `AddStandardResilienceHandler()` for automatic retry, timeout, and circuit-breaker policies.

---

## Authentication

All telemetry requests include the site's API key in the `X-TelemetryForge-Key` HTTP header. API keys are generated and managed through the TelemetryForge Server admin UI.

---

## Web Package

**NuGet:** `FactFoundry.TelemetryForge.Web`
**Target:** ASP.NET Core / Blazor Server applications
**Endpoint:** `POST /api/telemetry/web`

### Architecture

The web package has two complementary components:

1. **TelemetryForgeMiddleware** — ASP.NET request pipeline middleware for non-Blazor (traditional) requests. Each HTTP request produces one telemetry event.
2. **TelemetryForgeCircuitHandler** — Blazor Server circuit lifecycle handler. Sends a `page_view` event on each navigation and a `circuit_close` event when the circuit disconnects. Also implements `ITelemetryForge` for custom event tracking.

Both are registered via a single `AddTelemetryForge()` call.

### Middleware (non-Blazor requests)

For each inbound HTTP request, the middleware:

1. Lets the request pass through the pipeline
2. After the response, captures telemetry data from `HttpContext`
3. Posts a single `page_view` event to the server

**Skipped requests:** Static files and framework paths are excluded automatically — `/_framework`, `/_blazor`, `/css`, `/js`, `/lib`, and common static extensions (`.css`, `.js`, `.map`, `.ico`, `.png`, `.jpg`, `.svg`, `.woff`, `.woff2`).

**Data captured from HttpContext:**

| Field | Source |
|---|---|
| IP address | `X-Forwarded-For` header (first), then `RemoteIpAddress` |
| User-Agent | `User-Agent` header |
| Referrer | `Referer` header |
| Language | `Accept-Language` header (first value) |
| Page path | `HttpContext.Request.Path` |
| Status code | `HttpContext.Response.StatusCode` |
| DNT | `DNT` header |
| GA cookie | `_ga` cookie value (only if `UseGaCookie` is enabled) |
| Duration | `Stopwatch` around request pipeline |

### Circuit Handler (Blazor Server)

For Blazor Server apps, the circuit handler sends events throughout the circuit lifecycle:

1. **Circuit opens** — captures IP/UA/referrer/language from the initial HTTP context, sends a `page_view` event for the initial page
2. **Navigation events** — consumer calls `TrackNavigation(path)` from a `NavigationManager.LocationChanged` handler, which sends a `page_view` event immediately
3. **Custom events** — consumer calls `TrackEvent(name, data)` via the `ITelemetryForge` interface to send `custom` events
4. **Circuit closes** — sends a `circuit_close` event so the server can calculate last-page duration

### Configuration

```csharp
builder.Services.AddTelemetryForge(options =>
{
    options.Endpoint   = "https://telemetry.yourdomain.com";
    options.ApiKey     = "your-site-api-key";
    options.RespectDnt = true;   // default: true — skip tracking if DNT header is set
    options.UseGaCookie = false; // default: false — hash _ga cookie for cross-session identity
});

app.UseTelemetryForge(); // register middleware in the request pipeline
```

### Payload Schema (WebEventPayload)

```json
{
  "event_type": "page_view",
  "platform": "aspnet | blazor-server",
  "timestamp": "2026-05-25T10:00:00Z",
  "ip_address": "203.0.113.42",
  "ga_value": "GA1.2.123456789.1234567890",
  "user_agent": "Mozilla/5.0 ...",
  "referrer": "https://google.com",
  "language": "en-US",
  "page_path": "/products",
  "status_code": 200,
  "duration_ms": 45,
  "dnt": false,
  "event_name": null,
  "event_data": null,
  "target_url": null
}
```

Null fields are omitted from the serialized JSON.

### Identity Resolution (Web)

| Identifier | Source | Purpose |
|---|---|---|
| `ip_address` | Client IP from HttpContext | Server hashes this to create a daily-salted visitor hash for first-visit detection |
| `ga_value` | `_ga` cookie (opt-in) | If present, server uses this hash for more stable cross-session identity |

The server hashes the IP with a daily-rotating salt for visitor identity, performs geolocation, then discards the raw address. Hashing is a server-side concern — the SDK sends the raw IP so the server can geolocate before discarding it.

---

## Desktop Package

**NuGet:** `FactFoundry.TelemetryForge.Desktop`
**Target:** WPF, WinForms, Avalonia, console applications
**Endpoint:** `POST /api/telemetry/desktop`

### Architecture

The desktop package provides:

1. **DesktopSessionTracker** — singleton that tracks the session lifecycle from app start to shutdown
2. **MachineFingerprint** — platform-specific machine identity, SHA-256 hashed before transmission
3. **IFeatureTracker** — interface for recording feature navigation and errors throughout the session

### Session Tracking

`DesktopSessionTracker` is registered as a singleton and manages one session per application lifetime:

1. **Construction** — generates a `session_id` (UUID), records `SessionStart`, resolves machine fingerprint and platform info, starts heartbeat timer if configured
2. **During session** — consumer calls `TrackFeature("FeatureName")` and `TrackError("Feature", "message")` to record usage
3. **Heartbeat** — on a configurable interval (default 15 minutes), flushes only the feature/error entries accumulated since the last flush. Each flush increments the `sequence` counter
4. **Shutdown** — consumer calls `FlushAsync()` or lets `IAsyncDisposable`/`IDisposable` auto-flush. Sends any remaining deltas with the final sequence number

Delta tracking: subsequent flushes only include new entries since the last send. If no new data has been recorded, the heartbeat is skipped. Thread-safe feature/error accumulation via locking.

### Machine Fingerprinting

Platform-specific machine identity resolution:

| Platform | Source |
|---|---|
| Windows | `HKLM\SOFTWARE\Microsoft\Cryptography\MachineGuid` (Registry) |
| Linux | `/etc/machine-id` file |
| macOS | `ioreg -rd1 -c IOPlatformExpertDevice` → `IOPlatformUUID` |
| Fallback | `fallback:{MachineName}:{UserName}:{ProcessorCount}` |

The raw identifier is SHA-256 hashed before storage or transmission. The hash is cached after first computation.

### Configuration

```csharp
builder.Services.AddTelemetryForge(options =>
{
    options.Endpoint   = "https://telemetry.yourdomain.com";
    options.ApiKey     = "your-app-api-key";
    options.AppVersion = "2.1.0";       // auto-populated from entry assembly if omitted
    options.LicenseJwt = "optional-jwt"; // optional, for license tier correlation
    options.HeartbeatIntervalMinutes = 15; // default: 15. Set to null/0 to disable
});
```

### Usage

```csharp
// Inject the feature tracker anywhere in the app
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

// On app shutdown — flush the session
await host.Services.GetRequiredService<DesktopSessionTracker>().FlushAsync();
```

### Payload Schema (DesktopSessionPayload)

Each heartbeat (and the initial flush) sends a payload with only the delta since the last send:

```json
{
  "session_id": "f47ac10b-58cc-4372-a567-0e02b2c3d479",
  "sequence": 0,
  "app_version": "2.1.0",
  "platform": "windows",
  "os_version": "Microsoft Windows NT 10.0.22631.0",
  "fingerprint_hash": "a1b2c3d4e5f6...",
  "license_jwt": null,
  "session_start": "2026-05-25T09:00:00Z",
  "session_end": "2026-05-25T09:15:00Z",
  "duration_ms": 900000,
  "feature_path": ["ModelEditor", "Export"],
  "error_events": [
    {
      "Feature": "Export",
      "Message": "Connection timeout",
      "Timestamp": "2026-05-25T09:12:00Z"
    }
  ]
}
```

The `session_id` is stable for the entire app session. The `sequence` starts at 0 and increments with each flush. The `feature_path` and `error_events` contain only new entries since the previous flush.

### Identity Resolution (Desktop)

| Identifier | Source | Purpose |
|---|---|---|
| `fingerprint_hash` | SHA-256 of platform machine ID | First-install detection. Stable across app restarts on the same machine |

---

## Mobile Package (Deferred)

**NuGet:** `FactFoundry.TelemetryForge.Mobile`
**Target:** .NET MAUI (iOS / Android)
**Endpoint:** `POST /api/telemetry/mobile`
**Status:** Spec only — implementation deferred. Lower priority than Web and Desktop.

### Planned Architecture

Similar to the Desktop package but with mobile-specific identity resolution:

| Platform | Identifier | Hash Type |
|---|---|---|
| iOS | `UIDevice.identifierForVendor` | `vendor_id` |
| Android | `Settings.Secure.ANDROID_ID` | `android_id` |
| Fallback | Client-generated GUID (persisted to app storage) | `generated_guid` |

### Planned Payload Schema (MobileSessionPayload)

```json
{
  "app_version": "1.0.0",
  "platform": "iOS",
  "os_version": "17.5",
  "device_hash": "d4e5f6a7b8c9...",
  "device_hash_type": "vendor_id",
  "session_start": "2026-05-25T14:00:00Z",
  "session_end": "2026-05-25T14:03:00Z",
  "duration_ms": 180000,
  "feature_path": ["Dashboard", "Scanner"],
  "error_events": []
}
```

---

## Privacy and GDPR

### What the SDK collects

| Data | Web | Desktop | Mobile |
|---|---|---|---|
| IP address (raw, for server-side hashing) | Yes | No | No |
| Machine fingerprint (SHA-256 hash) | No | Yes | No |
| Device identifier (SHA-256 hash) | No | No | Yes |
| User-Agent string | Yes | No | No |
| Page / feature paths | Yes | Yes | Yes |
| Error messages | No | Yes | Yes |
| Cookies | Only `_ga` if opted in | No | No |

### What the SDK never collects

- Personally identifiable information (names, emails, account IDs)
- Form input or page content
- Keystroke or mouse tracking data
- Cross-site tracking identifiers
- Unrelated cookies or local storage

### DNT Support

The web package respects the `DNT` HTTP header by default (`RespectDnt = true`). When DNT is detected, the middleware skips telemetry entirely. This can be disabled if the consumer has a separate consent mechanism.

---

## HTTP Client

Both packages use a shared pattern (implemented independently in each package):

- `ITelemetryClient` interface with `SendAsync<T>(string path, T payload, CancellationToken)`
- `TelemetryForgeHttpClient` implementation using `HttpClient.PostAsJsonAsync`
- Configured with `AddStandardResilienceHandler()` for built-in retry, timeout, and circuit-breaker
- Non-success responses are logged as warnings, never thrown
- `OperationCanceledException` is silently ignored (app shutting down)
- All other exceptions are caught, logged, and swallowed

---

## Server Compatibility

These SDK packages are designed to work with `FactFoundry.TelemetryForge.Server`. The server handles:

- API key validation (via `X-TelemetryForge-Key` header)
- IP hashing with daily-rotating salt
- Visitor/device first-seen detection
- User-Agent parsing (browser, OS, device type)
- IP geolocation (country/region via MaxMind GeoLite2)
- Event enrichment and storage
- Session materialization (for web events)
- Downstream sink forwarding

The SDK packages are intentionally thin — they capture and transmit raw telemetry data, and the server handles all enrichment and analysis.
