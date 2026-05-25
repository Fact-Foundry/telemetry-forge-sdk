# Claude Code Instructions for TelemetryForge SDK

## Project Overview

Client-side NuGet packages for TelemetryForge — lightweight, stateless libraries that .NET applications install to send telemetry data to a TelemetryForge Server instance. Licensed under MIT.

The server is a separate repository (`telemetry-forge`, AGPL-3.0).

## First Steps

**Read `docs/design/telemetryforge-sdk-spec.md` at the start of every session** — it contains the full system design including payload schemas and identity resolution. Subsequent decisions will be in documents in the `docs/design/decisions/` folder.

## Architecture

- .NET 10 class libraries
- Zero UI — these are middleware and service libraries only
- All packages POST session payloads to a TelemetryForge Server instance via HTTP

## Project Structure

| Project | Purpose |
|---|---|
| `FactFoundry.TelemetryForge.Web` | ASP.NET/Blazor middleware — captures web session telemetry from `HttpContext` and Blazor circuit events. Self-contained with its own HTTP client, interfaces, and options |
| `FactFoundry.TelemetryForge.Desktop` | Desktop app telemetry — machine fingerprinting, session tracking, feature navigation. Self-contained with its own HTTP client, hashing, interfaces, and options |
| `FactFoundry.TelemetryForge.Mobile` | MAUI mobile app telemetry — device identification, session tracking (deferred) |
| `FactFoundry.TelemetryForge.Tests` | Test projects |

## Build Commands

- **Web:** `dotnet build src/FactFoundry.TelemetryForge.Web/FactFoundry.TelemetryForge.Web.csproj`
- **Desktop:** `dotnet build src/FactFoundry.TelemetryForge.Desktop/FactFoundry.TelemetryForge.Desktop.csproj`
- **Mobile:** `dotnet build src/FactFoundry.TelemetryForge.Mobile/FactFoundry.TelemetryForge.Mobile.csproj`
- **All:** `dotnet build TelemetryForgeSDK.slnx`
- **Tests:** `dotnet test`

## Server API Endpoints (consumed by these packages)

| Endpoint | Package | Description |
|---|---|---|
| `POST /api/telemetry/web` | `FactFoundry.TelemetryForge.Web` | Web session payloads |
| `POST /api/telemetry/desktop` | `FactFoundry.TelemetryForge.Desktop` | Desktop session payloads |
| `POST /api/telemetry/mobile` | `FactFoundry.TelemetryForge.Mobile` | Mobile session payloads |

## Coding Standards

- **XML comments required on all public APIs** — all public classes, methods, and properties must have XML doc comments (`/// <summary>`). These power IntelliSense for NuGet consumers. Internal/private members use XML comments when clarification is needed, otherwise prefer self-documenting code
- All catch blocks should log meaningful error context — never swallow exceptions silently
- **Minimize external dependencies** — every dependency added to these packages becomes a transitive dependency for all consumers. Prefer .NET built-in APIs over third-party libraries. New dependencies require explicit justification
- These packages must never throw exceptions that crash the host application — telemetry failures should be logged and swallowed gracefully
- Raw IP addresses and machine identifiers must be hashed before transmission — never send raw values to the server

## Reference Implementation

The semantic-modeler repo at `/home/kevin/Repositories/FactFoundry/semantic-modeler/` has existing machine fingerprinting code in `SemanticModeler.Core/Services/MachineFingerprint.cs` that the Desktop package should follow.

## Workflow Rules

- **Do not commit, push, or tag** unless explicitly asked
- **Do not create markdown files** for planning/tracking in the repo
- **Deferred features** go in `docs/Future Enhancements.md` — remove items once they are implemented
- **Log all changes in `CHANGELOG.md`** under the current unreleased version. Group entries under Features, Fixes, UI Improvements, or Docs. Keep entries concise (one line each)

## Testing Rules

- **Run tests after every change** — build and run `dotnet test` before reporting a change as complete
- **Never silently fix a failing test** — if a code change breaks or invalidates an existing test, STOP and flag it. The test exists because that behavior was intentional. Ask whether the behavior change is correct before modifying the test to pass
- **Add tests for new logic with branches** — error mapping, status transitions, fallback paths, and computed values all need test coverage. If it has an `if`, it probably needs a test
- **Update tests when contracts change** — if a method's return type, exception behavior, or public API changes, update the corresponding tests to reflect the new contract and explain why the old behavior is no longer correct

## Implementation Approval Workflow

For any non-trivial change, follow this sequence — do not skip to writing code:

1. **Read** all relevant files first
2. **Restate** your interpretation of the requirement
3. **Propose** your implementation plan
4. **Wait for explicit approval** before writing any code
5. **Implement** once approved

**Skip the workflow** for simple, clearly scoped tasks (typo, single CSS fix, rename).
