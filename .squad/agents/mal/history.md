# Project Context

- **Owner:** Andrew Davis
- **Project:** BluQube — a lightweight CQRS-style framework for Blazor enabling "write once, run on Server or WASM" development
- **Stack:** C# / .NET 8,9,10 · Blazor Server + WASM · MediatR · FluentValidation · Roslyn (IIncrementalGenerator) · xUnit + Verify · NuGet
- **Repo:** C:\Code\bluqube
- **Created:** 2026-04-07

## Key Files

- `src/BluQube/` — main framework library (public NuGet package)
- `src/BluQube.SourceGeneration/` — Roslyn incremental generators (Requesting.cs, Responding.cs)
- `tests/BluQube.Tests/` — xUnit + Verify snapshot tests
- `samples/blazor/BluQube.Samples.Blazor/` — full Blazor Server + WASM sample (todo app with auth)
- `.github/workflows/build-and-test.yml` — CI pipeline (build, test, pack, publish to NuGet/MyGet)

## Architecture Notes

- Commands/Queries declared as `record` types with `[BluQubeCommand]`/`[BluQubeQuery]` attributes
- Source generators emit requester classes (client HTTP calls) and responder endpoints (server API)
- Handlers inherit from `CommandHandler<T>` or `GenericQueryProcessor<TQuery, TResult>`
- Results: `CommandResult` (Success/Failed/Invalid/Unauthorized) and `QueryResult<T>`
- Authorization via `MediatR.Behaviors.Authorization` — `[Authorize]` on handlers
- Validation via FluentValidation, runs before handler

## Known Gaps (as of 2026-04-07)

- Source generation testing is minimal (~50 lines) — high risk area
- No integration tests (client→server round-trip)
- README and docs are barebones
- Authorization generation path not well tested

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->

### 2026-04-08 — QueryResult<T> enum values and JSON serialization

Reviewed external request to add `NotFound()` and `Empty()` to `QueryResult<T>`. Key findings:

- **QueryResultStatus uses integer serialization** — JSON format is `{"Status":2}` not `{"Status":"Succeeded"}`. Adding new enum values requires explicit integer assignment to avoid breaking existing clients.
- **Current API has no convenience properties** — No `IsSucceeded`, `IsFailed` etc. Only `Status` is exposed. Keep it that way unless there's strong demand.
- **Source generators don't inspect QueryResultStatus** — They treat `QueryResult<T>` as opaque, so enum changes don't cascade to generated code.
- **Data property guard is already defensive** — Only allows access when `Status == Succeeded`, so new statuses automatically throw. No code change needed.

Decision: Approved `NotFound()` only. Rejected `Empty()` (redundant with `Succeeded(Array.Empty<T>())`). Rejected bundled convenience properties (separate request).
