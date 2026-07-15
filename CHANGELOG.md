# Changelog

## [Unreleased]

### Features

- **Multi-server mirroring (Web, Desktop, Api)** — telemetry can now be sent to more than one TelemetryForge server at once. Add `options.Mirrors.Add(new(endpoint, apiKey))` for each additional server alongside the primary `Endpoint`/`ApiKey`. Every payload fans out to the primary plus each mirror **concurrently and best-effort**: a slow or unavailable mirror never blocks or fails the app or the primary feed. Fully backward compatible — no mirrors configured means identical behavior to before. Useful for standing up a new server next to an existing one to compare and seed real data before cutting over

### Internal

- The per-platform `TelemetryForgeHttpClient` changed from a single fixed-`BaseAddress` client to a fan-out over one-or-more targets, using a named resilient `HttpClient` from `IHttpClientFactory` and setting the per-target URL + API key (+ SDK-version header on Api) per request. Constructor signature changed accordingly; unit tests updated and extended to cover the mirror fan-out and failing-mirror isolation

## [1.1.6] — 2026-06-28

### Features

- Web + Api: `UseTelemetryForge()` and `UseTelemetryForgeApi()` now no-op with a warning when called without a prior `AddTelemetryForge()`/`AddTelemetryForgeApi()` registration — a missing telemetry config can no longer crash the host at startup
- New package `FactFoundry.TelemetryForge.Api` — stateless ASP.NET middleware that posts one event per matched HTTP request (route template, method, status code, latency) to `/api/telemetry/api`; captures low-cardinality route templates only, no caller IP, body, or PII
- `FactFoundry.TelemetryForge.Api` now reports the caller's `country` (ISO 3166-1 alpha-2) resolved from a CDN geolocation header on the inbound request (Cloudflare/CloudFront/Vercel/Akamai), still without ever handling an IP. Because the SDK posts telemetry from the app server, server-side IP geolocation would have located the app server rather than the caller; reading the edge-injected header is the only way to get the real caller's country. New `ApiTelemetryOptions.GeoProvider` (default `Auto`) selects the provider, or `None` to disable
- `FactFoundry.TelemetryForge.Api` stamps an `X-TelemetryForge-Sdk-Version` header (auto-read from the assembly's informational version, build metadata stripped) on every telemetry post, so the server can record which SDK version each app runs without any per-event cost
- `FactFoundry.TelemetryForge.Api` supports a per-request business outcome — call `HttpContext.SetTelemetryOutcome("license_valid")` in a handler and the middleware includes it as `outcome` in the event. Low-cardinality consumer-defined label, distinct from the HTTP status code (a 200 can still carry a business failure). The header and the JSON `outcome` field are plain HTTP, so non-.NET clients (PHP, Python, etc.) can replicate both

### Docs

- README: added Desktop configuration options table with defaults and auto-populated fields
- README: documented `OsInfo` as a public reusable utility with namespace collision warning
- README: added Desktop payload schema reference table
- Desktop: added XML doc comments to all `DesktopSessionPayload` properties
- README: updated "Conditional Registration" section — `UseTelemetryForge()` is now safe to call unconditionally

## [1.1.5] — 2026-05-31

### Fixes

- Desktop: flush pending telemetry before setting disposed flag — prevents final flush from being silently skipped on shutdown
- Desktop: OS version now reported via shared `OsInfo` helper (friendly name + kernel) instead of `Environment.OSVersion.ToString()` — gives "macOS 14.5", Linux distro PRETTY_NAME, and Windows marketing name instead of "Unix 26.4.0"; matches the Semantic Modeler licensing service format

### Docs

- README: added Blazor render mode reference table — documents which telemetry layers (middleware vs circuit handler) are active under each render mode
- README: added reverse proxy header forwarding guide for Nginx and Apache — covers Client Hints and CDN geolocation headers

## [1.1.3] — 2026-05-26

### Fixes

- Web: middleware stores request context under both forwarded IP and connection IP — fixes cache lookup failure for circuit handler behind reverse proxies (Apache, nginx) where WebSocket connections lack X-Forwarded-For

## [1.1.2] — 2026-05-26

### Fixes

- Web: shared session ID between middleware and circuit handler — all events within a circuit now use the same session ID instead of generating a new one per request

## [1.1.1] — 2026-05-26

### Fixes

- Web: fixed Client Hints missing on circuit handler events — Brave (and other Chromium forks) showed as "Chrome" on all events except the initial page load
- Web: circuit open event now sends `circuit_open` instead of `page_view` — prevents inflated page view counts
- Web: circuit open page path now shows the actual landing page instead of `/_blazor`
- Test app: added `AddTelemetryForge()` and `UseTelemetryForge()` registration — circuit handler was never invoked because services were not registered

### Features

- Web: extracted `RequestContext` to consolidate duplicated header-reading logic between middleware and circuit handler
- Test app: SDK pipeline configured via `appsettings.json` (endpoint + API key) — real middleware and circuit handler run alongside the bot simulation mode

## [1.1.0] — 2026-05-25

### Features

- Web: replace session-level payloads with per-request `page_view` events (v2 ingestion API)
- Web: add `session_id` (UUID) to all events for session grouping
- Web: add `ITelemetryForge` interface for custom event tracking (`event_type=custom`)
- Web: circuit handler now sends `page_view` on each navigation and `circuit_close` on disconnect
- Web: add `GeoProvider` option for CDN geolocation headers (Cloudflare, CloudFront, Vercel, Akamai, or auto-detect)
- Web: add `country` and `region` fields to payload from CDN geo headers
- Web: capture `Sec-CH-UA`, `Sec-CH-UA-Mobile`, and `Sec-CH-UA-Platform` client hints for accurate browser identification
- Web: add bot persona selector to test app for server-side bot detection testing
- Web: add per-package NuGet README
- Desktop: add `session_id` (UUID) and `sequence` counter to all payloads
- Desktop: add configurable heartbeat timer that flushes feature/error deltas periodically
- Desktop: add per-package NuGet README

### Removed

- Remove `WebSessionPayload` — replaced by `WebEventPayload` for per-event model
- Remove `RespectDnt` option and `dnt` payload field — DNT header is deprecated and not relevant to telemetry
- Remove `LicenseJwt` option and `license_jwt` payload field — telemetry should not carry licensing data

## [1.0.1] — 2026-05-25

### Features

- Add `FactFoundry.TelemetryForge.Desktop` package with machine fingerprinting (Windows/Linux/macOS), session tracking, feature navigation, HTTP client with resilience, and SHA-256 hashing utilities
- Add `FactFoundry.TelemetryForge.Web` package with ASP.NET middleware, Blazor Server circuit handler, HTTP client with resilience, and configurable `_ga` cookie support
- Add test suite covering hashing, HTTP client fault tolerance, and desktop session tracking

### Removed

- Remove `FactFoundry.TelemetryForge.Core` package — shared code inlined into Web and Desktop for zero transitive dependencies

## [1.0.0] — 2026-05-25

- Initial release
