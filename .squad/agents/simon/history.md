# Project Context

- **Owner:** Andrew Davis
- **Project:** BluQube — a lightweight CQRS-style framework for Blazor enabling "write once, run on Server or WASM" development
- **Stack:** C# / .NET 8,9,10 · Blazor Server + WASM · MediatR · FluentValidation · Roslyn (IIncrementalGenerator) · xUnit + Verify · NuGet
- **Repo:** C:\Code\bluqube
- **Created:** 2026-04-07

## Test Infrastructure

- Framework: xUnit + Verify (snapshot testing)
- Mocking: Moq + Moq.Contrib.HttpClient
- Coverage: CoverLet (Cobertura format) — excludes BluQube.SourceGeneration projects
- Helpers: BluQube.Tests.RequesterHelpers, BluQube.Tests.ResponderHelpers
- Snapshots: `*.verified.txt` files alongside test files

## Known Coverage Gaps (as of 2026-04-07)

- **Source generation testing: CRITICAL** — only ~50 lines, `RequestingGeneratorTests.cs`
  - Need: edge cases, malformed attributes, nested types, generic inheritance, missing handlers
- **Integration tests: MISSING** — no client→server→handler round-trip tests
- **Authorization generation: MINIMAL** — parser works, generated code path not validated
- **HTTP error scenarios: THIN** — GenericQueryProcessor only tests success path
- **JSON serialization edge cases: THIN** — basic converter tests only

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->

### 2026-04-07 — QueryResult<T> NotFound/Empty test plan

- **QueryResultStatus is integer-ordinal in JSON.** `QueryResultConverter<TResult>` serialises the status as `(int)value.Status` and deserialises with `(QueryResultStatus)reader.GetInt32()`. Any new enum values MUST be appended (not inserted) to avoid breaking existing converter tests — current values are Unknown=0, Failed=1, Succeeded=2, Unauthorized=3.
- **`QueryResultOfTConverter<T>` (Verify helper) has no default case.** Its `switch` on `value.Status` silently falls through for unknown statuses. Must be updated before new state snapshot tests are trustworthy.
- **`Data` getter guards on `!= Succeeded` only.** Current condition: `if (this.Status != QueryResultStatus.Succeeded) throw`. New states (NotFound, Empty) will automatically throw without code change — but explicit tests should confirm this so the contract is documented.
- **`Succeeded(null)` ambiguity is real.** MaybeMonad's `Maybe.From(null)` behaviour on reference types is unverified. If it silently wraps null, then `Succeeded(null).Data` would throw from MaybeMonad rather than from BluQube's own guard — confusing for callers migrating to `NotFound()`.
- **Boolean property tests are better as direct Assert, not Verify snapshots.** Snapshot files for `true`/`false` values add overhead without benefit.
- **Test plan written to:** `.squad/decisions/inbox/simon-queryresult-test-plan.md`
