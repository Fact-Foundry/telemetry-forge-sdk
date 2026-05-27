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
