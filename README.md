# TelemetryForge SDK

Client NuGet packages for [TelemetryForge](https://github.com/FactFoundry/telemetry-forge) — lightweight, stateless libraries that send telemetry data from your .NET applications to a TelemetryForge Server instance.

## Packages

| Package | Purpose | NuGet |
|---|---|---|
| `FactFoundry.TelemetryForge.Web` | ASP.NET and Blazor web applications | *coming soon* |
| `FactFoundry.TelemetryForge.Desktop` | WPF, WinForms, MAUI desktop, Photino apps | *coming soon* |
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

- **Web:** page paths, session duration, referrer, browser/OS (from User-Agent), entry/exit pages — all server-side, no JavaScript
- **Desktop:** session duration, feature/screen navigation, app version, platform, error events, machine fingerprint (hashed)

## What Does NOT Happen

- No cookies set by the library
- No JavaScript emitted
- No raw IP addresses stored — hashing and geolocation handled server-side
- No cross-site tracking
- No advertising identifiers

## Requirements

- .NET 8, 9, or 10
- A running [TelemetryForge Server](https://github.com/FactFoundry/telemetry-forge) instance

## License

MIT — see [LICENSE](LICENSE) for details.

*A [Fact Foundry](https://fact-foundry.com) product*
