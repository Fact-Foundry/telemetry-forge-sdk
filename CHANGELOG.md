# Changelog

## [1.1.6] — Unreleased

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
