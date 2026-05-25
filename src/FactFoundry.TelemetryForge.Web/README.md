# FactFoundry.TelemetryForge.Web

ASP.NET and Blazor Server telemetry for [TelemetryForge](https://github.com/Fact-Foundry/telemetry-forge) — privacy-first, server-side request tracking with no JavaScript.

## Installation

```bash
dotnet add package FactFoundry.TelemetryForge.Web
```

## Setup

```csharp
builder.Services.AddTelemetryForge(options =>
{
    options.Endpoint   = "https://telemetry.yourdomain.com";
    options.ApiKey     = "your-site-api-key";
    options.UseGaCookie = false;            // default: false — include _ga cookie for cross-session identity
    options.GeoProvider = GeoProvider.Auto; // default: Auto — or Cloudflare, CloudFront, Vercel, Akamai, None
});

app.UseTelemetryForge(); // register middleware in the request pipeline
```

## How It Works

- **ASP.NET requests** — the middleware sends a `page_view` event per HTTP request, capturing page path, status code, duration, referrer, User-Agent, and language
- **Blazor Server** — the circuit handler sends a `page_view` event on each navigation and a `circuit_close` event when the circuit disconnects

Both components are registered automatically by `AddTelemetryForge()`.

## Custom Events (Blazor)

Inject `ITelemetryForge` to send custom events from anywhere in a Blazor circuit:

```csharp
@inject ITelemetryForge Telemetry

<button @onclick="OnSubmit">Submit</button>

@code {
    private void OnSubmit()
    {
        Telemetry.TrackEvent("form_submit", new Dictionary<string, object>
        {
            ["form"] = "contact"
        });
    }
}
```

## Blazor Navigation Tracking

Call `TrackNavigation` from a `NavigationManager.LocationChanged` handler to track page views within a Blazor circuit:

```csharp
@inject TelemetryForgeCircuitHandler CircuitHandler
@inject NavigationManager Navigation
@implements IDisposable

@code {
    protected override void OnInitialized()
    {
        Navigation.LocationChanged += OnLocationChanged;
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        CircuitHandler.TrackNavigation(new Uri(e.Location).AbsolutePath);
    }

    public void Dispose() => Navigation.LocationChanged -= OnLocationChanged;
}
```

## What Gets Sent

Each event is a lightweight JSON payload posted to `POST /api/telemetry/web`:

| Field | Description |
|---|---|
| `session_id` | UUID grouping events into a session (per circuit or per request) |
| `event_type` | `page_view`, `custom`, or `circuit_close` |
| `platform` | `aspnet` or `blazor-server` |
| `ip_address` | Client IP (server handles hashing) |
| `user_agent` | Browser User-Agent string |
| `sec_ch_ua` | Sec-CH-UA client hint (browser brand/version list) |
| `sec_ch_ua_mobile` | Sec-CH-UA-Mobile client hint (`?0` or `?1`) |
| `sec_ch_ua_platform` | Sec-CH-UA-Platform client hint (OS name) |
| `page_path` | The request or navigation path |
| `status_code` | HTTP status code (middleware only) |
| `duration_ms` | Request duration (middleware only) |
| `country` | Country code from CDN geo header (if available) |
| `region` | Region/state from CDN geo header (if available) |

## Privacy

- No cookies set by the library
- No JavaScript emitted
- Raw IPs are hashed server-side — only the hash with rotating salt is stored, which cannot identify individual users across multiple sessions

## Requirements

- .NET 8, 9, or 10
- A running [TelemetryForge Server](https://github.com/Fact-Foundry/telemetry-forge) instance

## License

MIT
