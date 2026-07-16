# Background telemetry sends (Web + Api packages)

**Status:** implemented. Written 2026-07-14 after a consumer-site
performance investigation.

## Problem

`TelemetryForgeMiddleware` (Web) and `TelemetryForgeApiMiddleware` (Api) `await` the
telemetry POST **inside the request pipeline, after `_next(context)`**. The response does
not complete until the telemetry server has answered, so every non-static request in a
consuming app pays the full telemetry round trip as user-visible latency.

Measured on a consumer site (2026-07-14, SDK Web 1.1.3 and 1.1.6): every page load
took ~1.8–2.5s. Server log showed the telemetry POST
returning **202 after ~1824ms — a single attempt, no Polly retries** ("Attempt: '0',
Handled: 'False'"). After decorating `ITelemetryClient` with a fire-and-forget wrapper on
the consumer side, the same pages served in **1–4ms** with telemetry still arriving.

The blocking sites:

- `src/FactFoundry.TelemetryForge.Web/TelemetryForgeMiddleware.cs` — `await _client.SendAsync("/api/telemetry/web", payload)` at the end of `InvokeAsync` (line ~92)
- `src/FactFoundry.TelemetryForge.Web/TelemetryForgeCircuitHandler.cs` — `await _client.SendAsync(...)` (line ~130); this one delays Blazor **circuit lifecycle**, i.e. how quickly pages become interactive
- `src/FactFoundry.TelemetryForge.Api/TelemetryForgeApiMiddleware.cs` — `await _client.SendAsync("/api/telemetry/api", payload)` (line ~83); every consumer of the Api package (licensing API etc.) pays the same tax

## Coordination warning: in-flight uncommitted work

As of 2026-07-14 the working tree (branch `develop`) has a large **uncommitted**
cross-package feature: per-target sends with mirrors (`TelemetryTarget.cs` new in all
three packages; `TelemetryForgeHttpClient.cs`, `TelemetryOptionsBase.cs`,
`ServiceCollectionExtensions.cs`, READMEs, CHANGELOG all modified). Check `git status`
before starting:

- If that work is still uncommitted, **do not** clobber it. The middleware files are NOT
  part of it, but `ServiceCollectionExtensions.cs` is — background-send registration will
  touch the same file. Commit or coordinate first.
- The released **1.1.6** Web client is the OLD implementation: ctor takes `HttpClient`
  (typed client), posts to a **relative** path, and depends on the `BaseAddress` that
  `AddTelemetryForge()` configures. The absolute-URL/targets implementation in the working
  tree is unreleased. Any DI change must keep the client resolvable exactly as the SDK
  registers it — constructing `TelemetryForgeHttpClient` outside its own registration
  throws `InvalidOperationException: ... BaseAddress must be set` on ≤1.1.6.

## Proposed design: queue-backed ITelemetryClient + hosted drainer

Make the **registered `ITelemetryClient` itself non-blocking** rather than editing each
call site. Middleware, circuit handler, and Api middleware then need **zero changes**,
and any future caller is safe by construction.

1. **`QueuedTelemetryClient : ITelemetryClient`** — `SendAsync` writes
   `(path, serialized payload or payload object)` to a `Channel<T>` and returns a
   completed task. Payloads are already fully materialized DTOs (`WebEventPayload` etc.)
   built before the send, so nothing captured references `HttpContext` after the response
   — keep it that way (enqueue the DTO, never a closure over the context).
2. **Bounded channel**: `Channel.CreateBounded` with
   `BoundedChannelOptions(capacity) { FullMode = BoundedChannelFullMode.DropOldest, SingleReader = true }`.
   Capacity as a new option on `TelemetryOptionsBase` or `WebTelemetryOptions`
   (e.g. `SendQueueCapacity`, default ~1000). Log a warning (rate-limited or counted) when
   events are dropped — never throw. This is the backpressure story: a slow/down telemetry
   server costs bounded memory and drops oldest events instead of piling up tasks.
3. **Hosted `BackgroundService`** (e.g. `TelemetrySendWorker`) reads the channel and calls
   the inner HTTP client (`TelemetryForgeHttpClient`), which already catches and logs all
   failures. Registered inside `AddTelemetryForge()` via `AddHostedService<>` — consumers
   change nothing.
4. **Graceful drain on shutdown**: in `StopAsync`, complete the writer and drain remaining
   events with a short cap (~5s). This mirrors the Desktop package's philosophy (commit
   `aa14d36` — "flush pending telemetry before setting disposed flag") so the tail isn't
   lost on app stop.
5. **Keep contracts intact**:
   - `ITelemetryClient.SendAsync` doc says "never throws" — still true (truer: it no
     longer even blocks).
   - `UseTelemetryForge()`'s no-op guard checks `IServiceProviderIsService.IsService(typeof(ITelemetryClient))`
     — still satisfied since `ITelemetryClient` remains registered.
   - Keep `AddStandardResilienceHandler()` on the named client — with sends off the
     request path, retries no longer cost users anything.
6. **Apply the same pattern to the Api package** (`AddTelemetryForgeApi`). Desktop already
   has its own queue/flush design — verify, don't duplicate.

### Alternative considered (rejected)

Plain fire-and-forget (`_ = Task.Run(...)`) inside each middleware: two-line diff, but no
backpressure, no shutdown drain, and every call site must remember the pattern. The
website is running exactly this as a stopgap today; the SDK should do better.

## Testing

Test project: `tests/FactFoundry.TelemetryForge.Tests` (has existing
`Desktop/TelemetryForgeHttpClientTests.cs` to copy conventions from). Worth covering:

- `SendAsync` returns synchronously/immediately even when the inner client is slow (fake handler with delay)
- events enqueued before shutdown are delivered by the drain (and drain respects the time cap)
- channel at capacity drops oldest, logs, and never throws or blocks
- inner HTTP failure is logged, not propagated

## Release

- Version comes from the git tag via `publish.yml` (`dotnet pack -p:PackageVersion=<tag>`,
  pushed to nuget.org). This is a behavior change → bump **minor** (e.g. 1.2.0),
  especially if it rides along with the mirrors feature.
- Add a CHANGELOG.md entry (root file; entries are grouped per release, prose bullets).

## Downstream cleanup after release

Any consumer site that added its own fire-and-forget wrapper around `ITelemetryClient` as a
stopgap should remove it after upgrading to the SDK version that includes background sends.

## Related, out of scope here

The telemetry **server** takes ~1.8s to return 202 for one authenticated
`/api/telemetry/web` event (unauthenticated requests get rejected in ~120ms, so it's the
accept path — GeoIP? synchronous DB write?). Background sending hides this from users but
it still caps ingest throughput. Fix lives in the telemetry server codebase, not the SDK.
