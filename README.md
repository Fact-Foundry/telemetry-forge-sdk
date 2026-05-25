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
builder.Services.AddTelemetryForge(options =>
{
    options.Endpoint = "https://telemetry.yourdomain.com";
    options.ApiKey   = "your-site-api-key";
});

app.UseTelemetryForge();
```

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
