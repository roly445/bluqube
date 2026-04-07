# Project Context

- **Owner:** Andrew Davis
- **Project:** BluQube — a lightweight CQRS-style framework for Blazor enabling "write once, run on Server or WASM" development
- **Stack:** C# / .NET 8,9,10 · Blazor Server + WASM · MediatR · FluentValidation · Roslyn (IIncrementalGenerator) · xUnit + Verify · NuGet
- **Repo:** C:\Code\bluqube
- **Created:** 2026-04-07

## Key Files I Own

- `src/BluQube/` — main framework library
- `src/BluQube.SourceGeneration/Requesting.cs` — client-side IIncrementalGenerator
- `src/BluQube.SourceGeneration/Responding.cs` — server-side IIncrementalGenerator
- `src/BluQube.SourceGeneration/*InputDefinitionProcessor.cs` — Roslyn syntax tree parsers
- `src/BluQube.SourceGeneration/*OutputDefinitionProcessor.cs` — C# code emitters
- `samples/blazor/BluQube.Samples.Blazor/` — full Blazor Server + WASM sample

## Generator Architecture

- `Requesting.cs` — scans for `[BluQubeRequester]`, finds all commands/queries, emits HTTP requesters + DI extensions
- `Responding.cs` — scans for `[BluQubeResponder]`, finds all handlers, emits endpoint mappings + JSON config
- Attribute changes in `src/BluQube/Attributes/` require corresponding generator updates
- Always do a clean build after generator changes (`dotnet build --no-incremental`)

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->

### 2026-04-08 — QueryResultStatus explicit integer values

When adding a new value to `QueryResultStatus`, always use explicit integer values (e.g. `NotFound = 4`). The converter (`QueryResultConverter<T>`) serializes/deserializes status as an integer, so implicit ordering is fragile — explicit values make the contract clear and stable across versions. Mal flagged this explicitly in MAL-2026-001.

### 2026-04-08 — CommandResultConverter<TResult> default status and CommandResultStatus explicit values

`CommandResultConverter<TResult>` was initializing `status` to `CommandResultStatus.Succeeded` before reading the JSON. This meant that if the `Status` property was absent, the converter would try to return a success result (and throw if `data` was null) rather than failing cleanly. Always initialize to `Unknown` so the `default` case in the switch throws `JsonException` on missing/unrecognized status — matching `CommandResultConverter` (non-generic).

Added explicit integer values to all `CommandResultStatus` members (`Unknown = 0`, `Invalid = 1`, `Failed = 2`, `Succeeded = 3`). Enum integer values drive JSON serialization; without explicit assignments, any future reordering silently breaks deserialization. This aligns with the pattern already applied to `QueryResultStatus` per MAL-2026-001.

### 2026-04-08 — QueryResult<T>.NotFound() implementation

Added `NotFound` status to `QueryResultStatus` enum, `NotFound()` factory to `QueryResult<T>`, and the corresponding `case` in `QueryResultConverter<T>.Read`. No change needed to the `Data` property guard — the existing `Status != Succeeded` check already prevents access on `NotFound`. Deferred: `IsNotFound`, `IsEmpty`, `HasData`, and `Empty()` factory (per MAL-2026-001).

### 2026-04-10 — Empty() factory + five boolean properties (MAL-2026-004)

Implemented the full approved set from MAL-2026-004:
- Added `Empty = 5` to `QueryResultStatus` (explicit integer value, consistent with prior pattern)
- Added `Empty()` factory to `QueryResult<T>` — mirrors `NotFound()`, stores `Maybe<T>.Nothing`
- Added all five boolean properties: `IsSucceeded`, `IsFailed`, `IsUnauthorized`, `IsNotFound`, `IsEmpty` — each a one-liner expression-body property checking `this.Status`
- Added `case QueryResultStatus.Empty` in `QueryResultConverter<T>.Read` — mirrors `NotFound` case
- Write side needed no change: `Empty` has no data payload, existing `if (value.Status == Succeeded)` guard already skips the Data write

StyleCop SA1516 requires a blank line between each auto-property (expression-body properties count as elements). Group five booleans together but separate each with a blank line. Build with 0 warnings/errors confirmed on net10.0.

Mal's binding condition was "all five or nothing" — do not ship a partial set of the boolean properties.

**Orchestration:** Scribe logged orchestration entry `20260410T113508-kaylee-empty-impl.md`. Decisions merged from inbox into `.squad/decisions.md` (MAL-2026-004 + supporting decisions). Team history updated.
