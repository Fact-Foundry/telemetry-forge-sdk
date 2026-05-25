# Changelog

## [Unreleased]

### Features

- Add `FactFoundry.TelemetryForge.Desktop` package with machine fingerprinting (Windows/Linux/macOS), session tracking, feature navigation, HTTP client with resilience, and SHA-256 hashing utilities
- Add `FactFoundry.TelemetryForge.Web` package with ASP.NET middleware, Blazor Server circuit handler, HTTP client with resilience, and configurable `_ga` cookie support
- Add test suite covering hashing, HTTP client fault tolerance, and desktop session tracking

### Removed

- Remove `FactFoundry.TelemetryForge.Core` package — shared code inlined into Web and Desktop for zero transitive dependencies
