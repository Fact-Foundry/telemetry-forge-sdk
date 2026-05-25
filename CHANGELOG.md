# Changelog

## [Unreleased]

### Features

- Add `FactFoundry.TelemetryForge.Desktop` package with machine fingerprinting (Windows/Linux/macOS), session tracking, feature navigation, HTTP client with resilience, and SHA-256 hashing utilities
- Add `FactFoundry.TelemetryForge.Web` package with ASP.NET middleware, Blazor Server circuit handler, HTTP client with resilience, and configurable `_ga` cookie support
- Add test suite covering hashing, HTTP client fault tolerance, and desktop session tracking
- Web: replace session-level payloads with per-request `page_view` events (v2 ingestion API)
- Web: add `ITelemetryForge` interface for custom event tracking (`event_type=custom`)
- Web: circuit handler now sends `page_view` on each navigation and `circuit_close` on disconnect
- Desktop: add `session_id` (UUID) and `sequence` counter to all payloads
- Desktop: add configurable heartbeat timer that flushes feature/error deltas periodically

### Removed

- Remove `FactFoundry.TelemetryForge.Core` package — shared code inlined into Web and Desktop for zero transitive dependencies
- Remove `WebSessionPayload` — replaced by `WebEventPayload` for per-event model
