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

### 2026-04-10 — Reversed position on Empty(), IsNotFound, IsEmpty (MAL-2026-004)

Re-evaluated the three items deferred from MAL-2026-001 after PearDrop returned with an updated brief. Approved all three plus a binding condition. Key lasting findings:

- **`IsSucceeded` semantics must align with `Data` access guard.** The `Data` property throws for any status other than `Succeeded`. Therefore `IsSucceeded == true` must mean "Data is safe to access." My original MAL-2026-001 position that NotFound/Empty are "successful operations" was philosophically defensible but inconsistent with our own code. When the code says "only Succeeded gets Data," `IsSucceeded` must track that boundary.
- **`Empty()` is NOT redundant with `Succeeded(emptyList)`.** They have different caller contracts: `Succeeded(emptyList)` returns the empty list via `.Data` — caller renders zero items. `Empty()` throws on `.Data` access — caller is forced to handle the empty case explicitly (show "no results" UI). Different semantics, both valid.
- **Boolean properties must ship as a complete set.** Shipping `IsNotFound` and `IsEmpty` without `IsSucceeded`, `IsFailed`, and `IsUnauthorized` creates an inconsistent API. All five or nothing. They're one-line `Status ==` checks — zero cost, full consistency.
- **Pattern matching is the real driver.** C# switch expressions with `{ IsNotFound: true }` are cleaner and more exhaustive than `Status == QueryResultStatus.NotFound`. This is the caller pattern that justifies boolean properties over raw enum access.
- **When the facts change, change the call.** The updated brief addressed every concern from MAL-2026-001: HTTP 404 off the table, concrete caller patterns, `IsSucceeded` aligned with Data guard. Sticking to a prior rejection when the inputs have changed is worse than reversing.

**Orchestration:** Decision documented in MAL-2026-004. Orchestration logged. Decisions merged into `.squad/decisions.md`. Team history updated.

### 2026-04-09 — CommandResult.Unauthorized() scope analysis (MAL-2026-003)

Investigated the full scope of fixing the Unauthorized/Failed asymmetry in command results. Key lasting findings:

- **`CommandResult.Unauthorized()` is a small fix, not a big one.** 23 files total (5 framework, 18 test/snapshot). ~75 lines. Kaylee can ship in 2–3 hours.
- **The constructor is the root cause.** `CommandResult`'s base constructor determines status from which Maybe fields have values. `Unauthorized()` passes an `errorData`, so it becomes `Failed`. Fix: add a new protected constructor that accepts `CommandResultStatus` directly, bypassing the inference logic.
- **Wire format changes — real breaking change.** Old: `{"Status":2,"ErrorData":{"Code":"NotAuthorized",...}}`. New: `{"Status":4}`. Any client converter will throw on `{"Status":4}` unless upgraded. Mitigation: read-side shim in converters to accept old format.
- **`ErrorData` behavior inverts under Option A.** Currently, `ErrorData` is accessible for `Unauthorized()` (since it IS a `Failed`). After fix, it must throw. Tests `ReturnsDataWhenUnauthorized` need to flip to `ThrowsInvalidOperationExceptionWhenUnauthorized`.
- **`CommandResultStatus` still lacks explicit integer values.** Now is the right time to add them alongside `Unauthorized = 4`. Prevents any future reordering from silently breaking JSON.
- **Read-side shim enables safe rollout.** New converter reads `{"Status":2, Code="NotAuthorized"}` as `Unauthorized()` — old-server-to-new-client round-trips safely.

### 2026-04-08 — QueryResult<T> enum values and JSON serialization

Reviewed external request to add `NotFound()` and `Empty()` to `QueryResult<T>`. Key findings:

- **QueryResultStatus uses integer serialization** — JSON format is `{"Status":2}` not `{"Status":"Succeeded"}`. Adding new enum values requires explicit integer assignment to avoid breaking existing clients.
- **Current API has no convenience properties** — No `IsSucceeded`, `IsFailed` etc. Only `Status` is exposed. Keep it that way unless there's strong demand.
- **Source generators don't inspect QueryResultStatus** — They treat `QueryResult<T>` as opaque, so enum changes don't cascade to generated code.
- **Data property guard is already defensive** — Only allows access when `Status == Succeeded`, so new statuses automatically throw. No code change needed.

Decision: Approved `NotFound()` only. Rejected `Empty()` (redundant with `Succeeded(Array.Empty<T>())`). Rejected bundled convenience properties (separate request).

### 2026-04-07 — Full codebase review

Performed a thorough review of all core framework, source generation, tests, sample app, and CI config. Key lasting findings:

- **`CommandResultConverter<TResult>` initializes status to `Succeeded`, not `Unknown`** — bug. Non-generic version correctly uses `Unknown`. Fix: line 18 of `CommandResultConverter`1`.cs`.
- **`CommandResultStatus` has no explicit integer values** — `QueryResultStatus` does (post MAL-2026-001). Inconsistency. Both enums need explicit assignments before next release.
- **`CommandResult.Unauthorized()` is NOT a distinct status** — it maps to `Failed` with `ErrorData.Code = "NotAuthorized"`. JSON round-trip loses the Unauthorized semantic. `QueryResult<T>.Unauthorized()` uses a proper distinct status. Asymmetry will confuse callers.
- **JSON converters throw on unknown properties** — both Command and Query converters use `default: throw new JsonException()`. Any new field from a newer server version will crash older clients. Fix: `reader.Skip()`.
- **Source generators store `SemanticModel` in pipeline** — breaks incremental caching. Every edit triggers full regeneration. This is a Roslyn generator anti-pattern.
- **No Roslyn diagnostics emitted** — malformed attribute paths produce syntax errors in generated code, not generator diagnostics. Very hard to debug.
- **Two `.received.txt` files committed** — unapproved Verify snapshots in source control. Delete them.
- **`NotFound` tests from MAL-2026-001 were never written** — decision approved, implementation shipped, tests missing.
- **`QueryResultTests/Status.cs` has a wrong test name** — `ReturnsFailedWhenUnauthorized` actually verifies `Unauthorized` status (correct behavior, wrong name).
- **CI pushes to MyGet on every main push, not just tags** — NuGet.org is tag-gated; MyGet is not. Clarify intent.
- **`HttpRequestMethod` enum is dead code** — exists in Constants, used nowhere. Generator and GenericQueryProcessor use raw strings.
- **`TreatWarningsAsErrors` is CI-only** — should be in the csproj so it's enforced locally too.
