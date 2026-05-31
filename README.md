# TelemetryForge SDK

Client NuGet packages for [TelemetryForge](https://github.com/Fact-Foundry/telemetry-forge) — lightweight, stateless libraries that send telemetry data from your .NET applications to a TelemetryForge Server instance.

## Packages

| Package | Purpose | NuGet |
|---|---|---|
| `FactFoundry.TelemetryForge.Web` | ASP.NET and Blazor web applications | [![NuGet](https://img.shields.io/nuget/v/FactFoundry.TelemetryForge.Web)](https://www.nuget.org/packages/FactFoundry.TelemetryForge.Web) |
| `FactFoundry.TelemetryForge.Desktop` | WPF, WinForms, MAUI desktop, Photino apps | [![NuGet](https://img.shields.io/nuget/v/FactFoundry.TelemetryForge.Desktop)](https://www.nuget.org/packages/FactFoundry.TelemetryForge.Desktop) |
| `FactFoundry.TelemetryForge.Mobile` | MAUI iOS and Android apps | *planned* |

## Quick Start — Web

```csharp
// Program.cs
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddTelemetryForge(options =>
{
    options.Endpoint = "https://telemetry.yourdomain.com";
    options.ApiKey   = "your-site-api-key";
});

app.UseTelemetryForge();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();
```

### Blazor Render Mode

The Web package has two layers — HTTP middleware and a Blazor circuit handler — and which layer fires depends on your render mode:

| Render mode | Middleware (page views) | Circuit handler (navigation, custom events) |
|---|---|---|
| **Interactive Server** | Yes | Yes |
| **Static SSR** | Yes | No — no SignalR circuit exists |
| **Interactive WebAssembly** | Yes | No — runs on the client, not the server |
| **Interactive Auto** | Yes | Only during the server-rendered phase |

The circuit handler tracks in-app navigations, custom events, and circuit lifecycle (`circuit_open` / `circuit_close`). If you need these, your Blazor app must use **Interactive Server** render mode — either globally or on the components where tracking matters. Without it, only the middleware's per-request `page_view` events will be captured.

### Reverse Proxy Headers

If your app runs behind Apache or Nginx, the proxy must forward the headers the SDK reads. Standard headers (`User-Agent`, `Referer`, `Accept-Language`) are forwarded by default. The headers below typically are not and need explicit configuration.

**Client Hints** (browser identification):

| Header | Purpose |
|---|---|
| `Sec-CH-UA` | Browser brand and version list |
| `Sec-CH-UA-Mobile` | Mobile device flag |
| `Sec-CH-UA-Platform` | Operating system |

**CDN Geolocation** (set `GeoProvider` in options to match your CDN):

| CDN | Headers |
|---|---|
| Cloudflare | `CF-IPCountry`, `CF-Region` |
| CloudFront | `CloudFront-Viewer-Country`, `CloudFront-Viewer-Country-Region` |
| Vercel | `x-vercel-ip-country`, `x-vercel-ip-country-region` |
| Akamai | `X-Akamai-Edgescape` |

**Nginx** — add to your `location` block:

```nginx
proxy_pass_request_headers on;  # default, but be explicit

# Client Hints
proxy_set_header Sec-CH-UA            $http_sec_ch_ua;
proxy_set_header Sec-CH-UA-Mobile     $http_sec_ch_ua_mobile;
proxy_set_header Sec-CH-UA-Platform   $http_sec_ch_ua_platform;

# Cloudflare geo (swap for your CDN's headers)
proxy_set_header CF-IPCountry         $http_cf_ipcountry;
proxy_set_header CF-Region            $http_cf_region;

# Standard forwarding
proxy_set_header X-Forwarded-For      $proxy_add_x_forwarded_for;
proxy_set_header X-Forwarded-Proto    $scheme;
```

**Apache** — enable `mod_proxy` and `mod_headers`, then add to your virtual host or `<Location>` block:

```apache
ProxyPreserveHost On
RequestHeader set X-Forwarded-For "%{REMOTE_ADDR}s"

# Client Hints — passed through by default with ProxyPass,
# but if stripped by another module, re-add them:
RequestHeader set Sec-CH-UA            "%{Sec-CH-UA}i"           early
RequestHeader set Sec-CH-UA-Mobile     "%{Sec-CH-UA-Mobile}i"    early
RequestHeader set Sec-CH-UA-Platform   "%{Sec-CH-UA-Platform}i"  early

# Cloudflare geo (swap for your CDN's headers)
RequestHeader set CF-IPCountry         "%{CF-IPCountry}i"        early
RequestHeader set CF-Region            "%{CF-Region}i"           early
```

Without these, the SDK still works but geolocation and Client Hints fields will be empty.

## Quick Start — Desktop

```csharp
builder.Services.AddTelemetryForge(options =>
{
    options.Endpoint   = "https://telemetry.yourdomain.com";
    options.ApiKey     = "your-app-api-key";
    options.AppVersion = Assembly.GetExecutingAssembly()
                                 .GetName().Version?.ToString();
});
```

### Configuration Options

| Option | Type | Default | Description |
|---|---|---|---|
| `Endpoint` | `string` | `""` | URL of your TelemetryForge Server instance |
| `ApiKey` | `string` | `""` | Per-app API key issued during registration |
| `AppVersion` | `string?` | Entry assembly version | Application version string (e.g. `"1.2.3"`). Auto-populated from the entry assembly if not set |
| `HeartbeatIntervalMinutes` | `int?` | `15` | Minutes between heartbeat flushes. Set to `0` or `null` to disable (only flush at shutdown) |

The following payload fields are **auto-populated** and have no configuration option:

| Field | Source |
|---|---|
| `session_id` | Generated UUID per session |
| `platform` | Detected at runtime (`"Windows"`, `"Linux"`, `"macOS"`) |
| `os_version` | Friendly OS name + kernel via `OsInfo.Get()` (e.g. `"macOS 14.5 \| Darwin 23.5.0"`, `"Arch Linux \| Linux 6.8.9"`) |
| `fingerprint_hash` | SHA-256 hash of machine-specific identifiers (raw values are never transmitted) |

### OS Detection Utility

The Desktop package ships a public `OsInfo` class that you can reuse in your own code:

```csharp
using FactFoundry.TelemetryForge.Desktop;

var fullDescription = OsInfo.Get();          // "Arch Linux | Linux 6.8.9"
var friendlyName    = OsInfo.GetFriendlyName(); // "Arch Linux"
```

> **Namespace collision warning:** If your project has its own `OsInfo` class, importing both namespaces will produce an ambiguous reference error. Use a namespace alias or fully qualify one of them.

### Payload Schema

Each heartbeat sends a JSON payload to `POST /api/telemetry/desktop`:

| JSON field | Type | Description |
|---|---|---|
| `session_id` | `string` | UUID identifying this session |
| `sequence` | `int` | Monotonically increasing counter for heartbeat ordering |
| `app_version` | `string?` | Application version |
| `platform` | `string` | Runtime platform (`"Windows"`, `"Linux"`, `"macOS"`) |
| `os_version` | `string` | Friendly OS name + kernel version |
| `fingerprint_hash` | `string` | SHA-256 machine fingerprint hash |
| `session_start` | `ISO 8601` | When the session started (UTC) |
| `session_end` | `ISO 8601` | Timestamp of this flush (UTC) |
| `duration_ms` | `long` | Milliseconds from session start to this flush |
| `feature_path` | `string[]` | Features visited since the last flush (delta) |
| `error_events` | `object[]` | Errors since the last flush — each has `feature`, `message`, `timestamp` |

## What Gets Collected

- **Web:** per-request page view events, referrer, browser/OS (from User-Agent), custom events, CDN geolocation (Cloudflare/CloudFront/Vercel/Akamai) — all server-side, no JavaScript
- **Desktop:** feature/screen navigation, error events, app version, platform, machine fingerprint (hashed), periodic heartbeats

## What Does NOT Happen

- No cookies set by the library
- No JavaScript emitted
- No raw IP addresses retained — hashed server-side with a rotating salt
- No cross-site tracking
- No advertising identifiers

## Test App

A Blazor Server test app is included for interactive testing of both the Web and Desktop SDKs against a running TelemetryForge Server.

```bash
dotnet run --project tests/FactFoundry.TelemetryForge.TestApp
```

Open the URL shown in the terminal (typically `http://localhost:5000`), enter your server endpoint and API key, then click **Start Session**. From there you can navigate features, simulate errors, and flush the session to verify payloads reach the server.

In web mode, a **Client Persona** selector lets you impersonate bots (Googlebot, Bingbot, Headless Chrome) to test server-side bot detection logic against realistic crawler traffic.

## Requirements

- .NET 8, 9, or 10
- A running [TelemetryForge Server](https://github.com/Fact-Foundry/telemetry-forge) instance

## License

MIT — see [LICENSE](LICENSE) for details.

*A [Fact Foundry](https://fact-foundry.com) product*
