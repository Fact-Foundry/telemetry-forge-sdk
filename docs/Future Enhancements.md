# Future Enhancements

Items listed here are planned but deferred from the initial implementation. Remove items once they are implemented.

## Packages

- **FactFoundry.TelemetryForge.Mobile** — MAUI mobile app telemetry package (iOS/Android). Should include `session_id` + `sequence` fields, configurable heartbeat interval, and `device_hash_type` field indicating identifier source (`vendor_id`, `android_id`, `generated_guid`). Spec is complete in the SDK design document but lower priority than Web and Desktop packages.

## Web Package

- **Link click tracking (Blazor only, opt-in)** — JS interop to capture anchor clicks and send `link_click` events with `target_url`

## Cross-Language Ingestion Spec

- **Protocol specification** — publish a language-agnostic spec (e.g., OpenAPI) documenting the three ingestion endpoints (`/api/telemetry/web`, `/api/telemetry/desktop`, `/api/telemetry/mobile`), payload schemas, authentication (`X-TelemetryForge-Key` header), and bot detection signals. This would enable community connectors in Python, PHP, Go, etc. without reverse-engineering the .NET SDK
