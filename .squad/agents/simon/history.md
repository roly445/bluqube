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

### 2026-04-20 — URL Binding Test Scaffolding Created (pre-Kaylee)

- **Test scaffolding written ahead of implementation** — Created comprehensive test scaffolding for Kaylee's URL binding feature before her implementation begins. Tests are marked with `[Fact(Skip = "...")]` and ready to be enabled once the feature lands.
- **PathTemplateParser already exists** — The `PathTemplateParser.ExtractRouteParameters()` utility is already implemented in `src\BluQube.SourceGeneration\Utilities\PathTemplateParser.cs` as an internal class. It uses regex `\{(\w+)\}` to extract route parameters. Direct unit testing would require `InternalsVisibleTo` or reflection, so instead created generator snapshot tests that verify correct parameter extraction through generated code output.
- **GeneratedSourceResult is a struct** — The Roslyn `GeneratedSourceResult` type returned by `FirstOrDefault()` is a struct, not a class. Cannot use `Assert.NotNull()` (xUnit warning xUnit2002) or null-checking patterns. Initial attempt to use `.HasValue` and `.Value` failed because that's for `Nullable<T>` wrapper, not the struct itself. Simplified tests to check generation occurred without verifying content until implementation is ready.
- **Three categories of test scaffolding created:**
  1. **Generator snapshot tests** (7 tests in `UrlBindingGeneratorTests.cs`) — verify generated code for commands/queries with/without path params, GET vs POST, client vs server side
  2. **Integration tests** (8 tests in `UrlBindingIntegrationTests.cs`) — end-to-end round-trip scenarios with TODO patterns for implementation
  3. **PathTemplateParser tests** (placeholder in `PathTemplateParserTests.cs`) — documented that parser testing happens through generator tests
- **All 15 tests compile and skip successfully** — Build produces 0 errors, `dotnet test --filter UrlBinding` shows all 15 tests skipped as expected. Tests are ready to be enabled when Kaylee completes URL binding implementation.
- **Test gap: URL escaping edge cases** — Integration tests include special character handling, but missing unit-level tests for `Uri.EscapeDataString` usage in generated `BuildPath()` methods. Should verify characters like `/`, `?`, `&`, `%`, spaces, Unicode are properly escaped.
- **Test gap: Generator error handling** — No tests for malformed path templates (unclosed braces, nested braces, invalid parameter names). Generator may silently fail or produce invalid code. Should add tests once generator error handling strategy is defined.

**Files created:**
- `tests\BluQube.Tests\Utilities\PathTemplateParserTests.cs` — placeholder with documentation
- `tests\BluQube.Tests\SourceGeneration\UrlBindingGeneratorTests.cs` — 7 skipped generator snapshot tests
- `tests\BluQube.Tests\Integration\UrlBindingIntegrationTests.cs` — 8 skipped integration test stubs

**Orchestration:** Scribe logged orchestration entry `20260420T112126-simon-url-binding-tests.md`. Session log written to `.squad/log/20260420T112126-url-binding-implementation.md`. Test scaffolding complete and ready to be enabled once Kaylee's implementation lands.

### 2026-04-10 — QueryResult<T> Empty() + five boolean property tests written (pre-Kaylee)

- **Tests written ahead of implementation** — Kaylee is implementing `Empty()`, `QueryResultStatus.Empty = 5`, and the five boolean properties (`IsSucceeded`, `IsFailed`, `IsUnauthorized`, `IsNotFound`, `IsEmpty`) in parallel. Tests were written against the approved spec (MAL-2026-004) and snapshot files pre-created so everything is ready to compile and pass when her branch lands.
- **Andrew's placement overrode Mal's structure** — Mal's decision doc called for a new `Properties.cs` file with `[Theory]` tests. Andrew's task brief specified adding the boolean property tests directly to `Status.cs` using individual `[Fact]` methods with `Assert.True`/`Assert.False`. Followed Andrew's explicit instruction.
- **Boolean property tests are non-async** — Since they use `Assert.True`/`Assert.False` instead of `await Verify(...)`, these are plain `void` `[Fact]` methods. This is intentional — no snapshot overhead for simple boolean assertions.
- **DisplayClass4_0 for ThrowsInvalidOperationExceptionWhenEmpty** — The new Data.cs method is at 0-indexed position 4 in the class, producing `<>c__DisplayClass4_0.<ThrowsInvalidOperationExceptionWhenEmpty>b__0()` in the stack trace snapshot. Consistent with the pattern established for prior methods.
- **PowerShell `\r\n` vs `` `r`n ``** — When creating snapshot files in PowerShell, backtick escapes (`` `r`n ``) produce actual CRLF bytes. Using `\r\n` in double-quoted strings produces literal backslash-r-backslash-n, which breaks snapshot matching. Always use backtick escapes for line endings in snapshot creation scripts.
- **Empty read snapshot is 24 bytes** — `{` + CRLF + `  Status: Empty` + CRLF + `}` = 21 bytes content + 3 BOM = 24 bytes total. Matches the pattern of the NotFound read snapshot (27 bytes, with `NotFound` being 3 chars longer than `Empty`).

**Orchestration:** Scribe logged orchestration entry `20260410T113508-simon-empty-tests.md`. Decisions merged from inbox into `.squad/decisions.md` (SIMON-2026-001 + supporting decisions). Team history updated.

### 2026-04-08 — QueryResult<T> NotFound tests written and passing

- **All 4 NotFound tests implemented and green** — Status, Data, Read (converter), Write (converter). Kaylee's implementation (`NotFound()` factory + `QueryResultStatus.NotFound = 4`) was already merged when tests were written, so tests compiled and passed immediately.
- **Verify snapshot file encoding matters** — Existing snapshots use UTF-8 BOM + CRLF line endings. Single-line snapshots (Status, Write) have no trailing newline. Multi-line snapshots (Data exception, Read converter object) use CRLF between lines. Creating snapshot files without matching this byte-for-byte will cause Verify to report a mismatch even when the test output is semantically identical.
- **`<>c__DisplayClass` numbering is method-position-based** — For the Data class, the DisplayClass number equals the 0-indexed position of the method containing the lambda within the class. Third throw-method at index 3 → `DisplayClass3_0`. This is reliable for predicting snapshot content without running tests first.
- **Read.cs theory uses `_json=<Name>` filename pattern** — Verify names parameterised theory snapshots using the first method parameter's name (`json`) as the label, with the value from `UseParameters(name)` as the discriminator. Adding a new `InlineData` case with name `"NotFound"` produces `_json=NotFound.verified.txt`.

### 2026-04-07 — QueryResult<T> NotFound/Empty test plan

- **QueryResultStatus is integer-ordinal in JSON.** `QueryResultConverter<TResult>` serialises the status as `(int)value.Status` and deserialises with `(QueryResultStatus)reader.GetInt32()`. Any new enum values MUST be appended (not inserted) to avoid breaking existing converter tests — current values are Unknown=0, Failed=1, Succeeded=2, Unauthorized=3.
- **`QueryResultOfTConverter<T>` (Verify helper) has no default case.** Its `switch` on `value.Status` silently falls through for unknown statuses. Must be updated before new state snapshot tests are trustworthy.
- **`Data` getter guards on `!= Succeeded` only.** Current condition: `if (this.Status != QueryResultStatus.Succeeded) throw`. New states (NotFound, Empty) will automatically throw without code change — but explicit tests should confirm this so the contract is documented.
- **`Succeeded(null)` ambiguity is real.** MaybeMonad's `Maybe.From(null)` behaviour on reference types is unverified. If it silently wraps null, then `Succeeded(null).Data` would throw from MaybeMonad rather than from BluQube's own guard — confusing for callers migrating to `NotFound()`.
- **Boolean property tests are better as direct Assert, not Verify snapshots.** Snapshot files for `true`/`false` values add overhead without benefit.
- **Test plan written to:** `.squad/decisions/inbox/simon-queryresult-test-plan.md`
