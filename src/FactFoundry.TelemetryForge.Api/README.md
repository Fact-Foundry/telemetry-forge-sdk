# FactFoundry.TelemetryForge.Api

HTTP API telemetry for [TelemetryForge](https://github.com/Fact-Foundry/telemetry-forge) — privacy-first, server-side request health tracking for ASP.NET Minimal APIs, controllers, and gRPC. No JavaScript, no caller IPs, no payloads.

## Installation

```bash
dotnet add package FactFoundry.TelemetryForge.Api
```

## Setup

```csharp
builder.Services.AddTelemetryForgeApi(options =>
{
    options.Endpoint = "https://telemetry.yourdomain.com";
    options.ApiKey   = "your-api-app-api-key";
    options.ExcludedPathPrefixes.Add("/health");  // optional — skip noisy paths
    options.ExcludedPathPrefixes.Add("/swagger");
});

app.UseRouting();
app.UseTelemetryForgeApi(); // must come after UseRouting so the route template is known
```

## Mirroring to Multiple Servers

Send the same telemetry to more than one TelemetryForge server — for example, to stand up a new server alongside your current one and compare, or to start collecting data before you cut over. Add one or more `Mirrors`, each with its own API key:

```csharp
builder.Services.AddTelemetryForgeApi(options =>
{
    options.Endpoint = "https://telemetry.yourdomain.com";   // primary
    options.ApiKey   = "your-api-app-api-key";

    // Also send to a second server (add when it's ready):
    options.Mirrors.Add(new("https://new-server.yourdomain.com", "new-server-key"));
});
```

Every payload posts to the primary and each mirror **concurrently and best-effort** — a slow or unavailable mirror never blocks or fails your app or the primary feed. Each server resolves visitor identity independently, so the datasets are self-consistent but their visitor hashes won't line up (expected).

## How It Works

The middleware times each request and sends one event per matched route. It captures the
low-cardinality **route template** (e.g. `/license/{id}`) rather than the raw resolved path,
keeping cardinality low and avoiding any IDs or query values leaking into telemetry.

Requests that don't match a route (404s, static files) are skipped, as are any paths under a
configured `ExcludedPathPrefixes` entry.

This package is stateless — one HTTP request maps to one event, with no session or circuit lifecycle.

## Business Outcomes

The HTTP status code tells you *whether the request succeeded*, not *what the application decided*. A
license check that returns `200 OK` can still be a business failure (`license_expired`, `seat_limit_reached`).
Tag that decision from your handler and it rides along on the event as `outcome`:

```csharp
app.MapPost("/api/license/validate", (LicenseRequest req, HttpContext ctx) =>
{
    var result = Validate(req);
    ctx.SetTelemetryOutcome(result.Valid ? "license_valid" : "license_expired");
    return Results.Ok(result);
});
```

Keep outcomes **low-cardinality** — a small fixed set of labels, never free-form text or identifiers
(no emails, license keys, or IDs). A good pattern is to map your response/result codes to labels in one
shared helper so the labels stay consistent across endpoints. The middleware reads whatever the handler
set last; if nothing is set, `outcome` is omitted.

## What Gets Sent

Each event is a lightweight JSON payload posted to `POST /api/telemetry/api`:

| Field | Description |
|---|---|
| `route_template` | Matched route template (e.g. `/license/{id}`) — never the raw path |
| `method` | HTTP method (GET, POST, etc.) |
| `status_code` | HTTP response status code |
| `latency_ms` | Request handling latency in milliseconds |
| `timestamp` | When the request occurred |
| `country` | Caller's ISO 3166-1 alpha-2 country, resolved from a CDN geolocation header (see below). Omitted when unavailable |
| `outcome` | Consumer-defined business outcome from `SetTelemetryOutcome(...)`. Omitted when none set |

Every post also carries an `X-TelemetryForge-Sdk-Version` request header (auto-read from this assembly's
version) so the server can record which SDK version each app runs — no per-event cost.

## Caller Country (CDN geolocation)

Because the SDK posts telemetry from your app server, the connection IP the server sees is the *app
server*, not the caller — so server-side IP geolocation can't identify the real caller. Instead the
middleware reads the caller's country from the **CDN geolocation header** on the original inbound request
(e.g. Cloudflare's `CF-IPCountry`) and sends only the resolved 2-letter code. No IP is ever handled.

Set `GeoProvider` to match your edge, or leave it `Auto`:

```csharp
options.GeoProvider = GeoProvider.Cloudflare; // or CloudFront, Vercel, Akamai, Auto, None
```

## Using This From Other Languages

The wire contract is plain HTTP — nothing here is .NET-specific. Any client (PHP, Python, Go, …) can
replicate it by `POST`ing the JSON above to `/api/telemetry/api` with the `X-TelemetryForge-Key` header.
The two optional dimensions are just data you supply: set `"outcome": "license_valid"` in the JSON body,
and (optionally) send your own `X-TelemetryForge-Sdk-Version` header. `country` can be set directly in the
body if you resolve it yourself; otherwise omit it.

## Privacy

- No caller IP, request body, headers, or query values are ever sent
- No cookies set, no JavaScript emitted
- Only aggregate request health data leaves the application

## Requirements

- .NET 8, 9, or 10
- A running [TelemetryForge Server](https://github.com/Fact-Foundry/telemetry-forge) instance

## License

MIT
