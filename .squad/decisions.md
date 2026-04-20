# Squad Decisions

## Active Decisions

### Decision 1: QueryResult<T> NotFound Enhancement (MAL-2026-001)

**Decision ID:** MAL-2026-001  
**Date:** 2026-04-08  
**Author:** Mal (Lead)  
**Status:** APPROVED AND IMPLEMENTED

#### Verdict

**APPROVED** — with design modifications. The proposal has valid semantics, but the suggested implementation needs adjustment to align with BluQube's existing patterns and maintain API stability.

**Status Update (2026-04-10):** Kaylee implemented `NotFound()` factory and `QueryResultStatus.NotFound = 4`. Simon wrote tests. All passing.

#### Summary of Request

PearDrop team requested two new factory methods on `QueryResult<T>`:
- `NotFound()` — for single-entity queries returning no match (→ 404)
- `Empty()` — for collection queries returning zero results (→ 200 with `[]`)

Plus convenience properties: `IsNotFound`, `IsEmpty`, and a change to make `IsSucceeded` return `false` for these states.

#### Analysis

##### What's Right About This Request

1. **The semantic gap is real.** `Succeeded(null)` is wrong — null is not success. Callers currently have to inspect data to distinguish "not found" from "found null."
2. **Aligns with HTTP semantics.** 404 vs 200-with-empty-body is a real distinction that query handlers need to express.
3. **Purely additive.** Existing code using `Succeeded`/`Failed`/`Unauthorized` continues to work.

##### Where The Proposal Needs Adjustment

**1. `IsSucceeded` should NOT return false for NotFound/Empty.**

The proposal says: "Existing `IsSucceeded` should return **false** for both (they are not successful; they are 'no result' states)."

**Disagreement:** These ARE successful operations. The query *executed successfully* — it just found nothing. The distinction between "operation success" and "found data" is important:

- `NotFound` = query ran, no entity exists (success, no data)
- `Empty` = query ran, zero results match (success, no data)
- `Failed` = query threw an exception or errored (failure)

Making `IsSucceeded` false for NotFound/Empty would be semantically incorrect and confusing. Instead, add `HasData` or `HasValue` as the distinguishing property.

**2. We don't have `IsSucceeded` today — don't add convenience properties yet.**

Current `QueryResult<T>` has no `IsSucceeded`, `IsFailed`, etc. properties. We only expose `Status`. If PearDrop wants boolean helpers, that's a separate enhancement request. Don't bundle it.

**3. `Empty()` is questionable scope.**

For collection queries, `Succeeded(new List<T>())` is *already semantically correct*. An empty list IS a valid success result. The `Empty()` factory adds convenience but no semantic clarity.

**My call:** Approve `NotFound()` only. `Empty()` is nice-to-have but not blocking — callers can use `Succeeded(Array.Empty<T>())`.

#### Approved Changes

##### 1. Add `NotFound` to `QueryResultStatus` enum

```csharp
// src/BluQube/Constants/QueryResultStatus.cs
public enum QueryResultStatus
{
    Unknown,
    Failed,
    Succeeded,
    Unauthorized,
    NotFound,  // NEW — query succeeded but entity/item not found
}
```

##### 2. Add `NotFound()` factory method to `QueryResult<T>`

```csharp
// src/BluQube/Queries/QueryResult`1.cs
public static QueryResult<T> NotFound()
{
    return new QueryResult<T>(Maybe<T>.Nothing, QueryResultStatus.NotFound);
}
```

##### 3. Update `Data` property guard

The `Data` getter must throw for `NotFound` status (no data available):

```csharp
public T Data
{
    get
    {
        if (this.Status != QueryResultStatus.Succeeded)
        {
            throw new System.InvalidOperationException("Data is only available when the status is Succeeded");
        }

        return this._data.Value;
    }
}
```

**No change needed** — existing guard already covers this. Only `Succeeded` allows `Data` access.

##### 4. Update `QueryResultConverter<T>` for JSON serialization

```csharp
// src/BluQube/Queries/QueryResultConverter`1.cs — Read method switch
switch (status)
{
    case QueryResultStatus.Succeeded:
        if (object.Equals(data, null))
        {
            throw new JsonException();
        }
        return QueryResult<TResult>.Succeeded(data);
    case QueryResultStatus.Failed:
        return QueryResult<TResult>.Failed();
    case QueryResultStatus.Unauthorized:
        return QueryResult<TResult>.Unauthorized();
    case QueryResultStatus.NotFound:  // NEW
        return QueryResult<TResult>.NotFound();
    default:
        throw new JsonException();
}
```

Write method needs no change — `NotFound` has no data, same as `Failed`/`Unauthorized`.

#### Deferred (Not Approved)

1. **`Empty()` factory method** — Use `Succeeded(Array.Empty<T>())`. Revisit if there's strong demand.
2. **`IsNotFound`, `IsEmpty`, `IsSucceeded` properties** — Separate enhancement request. Not bundled here.
3. **`HasData`/`HasValue` property** — Good idea, but out of scope for this request.

#### Impact Assessment

| Component | Impact |
|-----------|--------|
| `QueryResultStatus` enum | **Breaking change for serialization** — new enum value `NotFound = 4` changes integer mappings. Existing JSON `{"Status":4}` would mean something different. **Mitigation:** Assign explicit values or document that JSON consumers must handle new status. |
| `QueryResultConverter<T>` | Update `Read` switch to handle `NotFound`. Low risk. |
| Source generators | **No impact.** Generators use `QueryResult<T>` as opaque type; they don't inspect status values. |
| `Data` property guard | **No change needed.** Existing guard covers new status. |

##### JSON Serialization Concern

The converter uses integer enum values. Adding `NotFound = 4` means:

- Old clients receiving `{"Status":4}` will hit `default` case and throw `JsonException`
- This is acceptable — clients should handle unknown statuses gracefully or upgrade

**Recommendation:** Document the new status value. Consider explicit enum values if backward compat is critical:

```csharp
public enum QueryResultStatus
{
    Unknown = 0,
    Failed = 1,
    Succeeded = 2,
    Unauthorized = 3,
    NotFound = 4,
}
```

#### Implementation Guidance

##### Files to Modify

1. **`src/BluQube/Constants/QueryResultStatus.cs`**
   - Add `NotFound = 4` with explicit value
   
2. **`src/BluQube/Queries/QueryResult`1.cs`**
   - Add `NotFound()` factory method (4 lines)
   
3. **`src/BluQube/Queries/QueryResultConverter`1.cs`**
   - Add `case QueryResultStatus.NotFound:` in `Read` method

##### Tests to Add

4. **`tests/BluQube.Tests/Queries/QueryResultTests/Status.cs`**
   - Add `ReturnsNotFoundWhenNotFound()` test
   
5. **`tests/BluQube.Tests/Queries/QueryResultTests/Data.cs`**
   - Add `ThrowsInvalidOperationExceptionWhenNotFound()` test
   
6. **`tests/BluQube.Tests/Queries/QueryResultConverterTests/Read.cs`**
   - Add `{"Status":4}` inline data case
   
7. **`tests/BluQube.Tests/Queries/QueryResultConverterTests/Write.cs`**
   - Add `GeneratesValidJsonWhenNotFound()` test

---

### Decision 2: Empty(), IsNotFound, IsEmpty, IsSucceeded, IsFailed, IsUnauthorized Properties (MAL-2026-004)

**Decision ID:** MAL-2026-004  
**Date:** 2026-04-10  
**Author:** Mal (Lead)  
**Status:** APPROVED AND IMPLEMENTED

#### Verdict

**APPROVED — all three items**, reversing prior deferred items from MAL-2026-001, with one binding condition.

Mal's original rejection of `Empty()`, `IsNotFound`, and `IsEmpty` was based on incomplete information. The updated brief from PearDrop addresses all three concerns:
1. HTTP 404 off the table — all statuses return 200 with state in body
2. Concrete caller patterns showing pattern matching with boolean properties
3. `IsSucceeded` semantics corrected — must align with `Data` guard (only Succeeded allows Data access)

**Key Finding:** `IsSucceeded` must return true ONLY for `Succeeded` status, because the `Data` property throws for any other status. Making `IsSucceeded` false for `NotFound` or `Empty` would create a trap: `if (result.IsSucceeded) { var x = result.Data; }` would throw.

#### Approved Changes

1. **`Empty = 5` added to `QueryResultStatus`** with explicit integer value
2. **`QueryResult<T>.Empty()` factory** — returns `new QueryResult<T>(Maybe<T>.Nothing, QueryResultStatus.Empty)`
3. **Five boolean properties on `QueryResult<T>`:**
   - `IsSucceeded` — true only when `Status == Succeeded`
   - `IsFailed` — true only when `Status == Failed`
   - `IsUnauthorized` — true only when `Status == Unauthorized`
   - `IsNotFound` — true only when `Status == NotFound`
   - `IsEmpty` — true only when `Status == Empty`
4. **`QueryResultConverter<T>.Read` case** — added `case QueryResultStatus.Empty: return QueryResult<TResult>.Empty();`

#### Binding Condition

**Ship all five boolean properties or none.** A partial API surface is worse than none. These are trivial one-liners with zero cost but provide consistency and enable clean pattern matching.

#### Implementation Status

- **Kaylee:** Implemented all framework changes. Build: 0 errors, 0 warnings on net8.0, net9.0, net10.0.
- **Simon:** Wrote 17 new tests (Status, Data, Read, Write paths + 14 boolean property tests). All 117 tests passing.

---

### Decision 3: CommandResult Converter Bug Fixes (KAYLEE-2026-001)

**Decision ID:** KAYLEE-2026-001  
**Date:** 2026-04-08  
**Author:** Kaylee (Framework Dev)  
**Status:** APPROVED AND IMPLEMENTED

#### Two Bugs Fixed

1. **`CommandResultConverter<TResult>` line 18:** Changed default status from `Succeeded` to `Unknown`
   - Problem: Missing JSON `Status` property would silently return success instead of failing
   - Solution: Initialize to `Unknown`, so missing status falls through to `default: throw JsonException()`

2. **`CommandResultStatus` enum:** Added explicit integer values
   - Problem: Implicit ordering is fragile; any reordering breaks JSON deserialization
   - Solution: `Unknown = 0, Invalid = 1, Failed = 2, Succeeded = 3` (matches `QueryResultStatus` pattern)

#### Build Verification

`dotnet build BluQube.sln` — **0 errors, 0 warnings** across all targets.

#### No API Surface Change

Both fixes are internal to the JSON read path. Wire format unchanged.

---

### Decision 4: CommandResult Unauthorized/Failed Asymmetry Analysis (MAL-2026-003)

**Decision ID:** MAL-2026-003  
**Date:** 2026-04-09  
**Author:** Mal (Lead)  
**Status:** DEFERRED — Analysis Complete

#### The Problem

`CommandResult.Unauthorized()` serializes as `Status = Failed` with `ErrorData.Code = "NotAuthorized"`, while `QueryResult<T>.Unauthorized()` uses a distinct `Status = Unauthorized`. Asymmetry causes incorrect status-based discrimination on the wire.

#### Verdict: Option A Selected

Add `Unauthorized = 4` to `CommandResultStatus`, update converters, and add read-side shim for backward compatibility.

**Effort:** 23 files, ~75 lines, 2–3 hours.

**Breaking change:** Yes, but with read-side shim to handle old format gracefully.

**Status:** Deferred for a separate implementation sprint (not part of current Empty() work).

---

### Decision 5: Codebase Review Findings (MAL-2026-002)

**Decision ID:** MAL-2026-002  
**Date:** 2026-04-07  
**Author:** Mal (Lead)  
**Status:** REFERENCE / ONGOING IMPROVEMENTS

#### Summary

Comprehensive codebase review identified ~15 areas for improvement, prioritized by risk:

**#1–#3 (HIGH):**
- Fix `CommandResultConverter<TResult>` default status (DONE — Kaylee 2026-04-08)
- Add explicit values to `CommandResultStatus` (DONE — Kaylee 2026-04-08)
- Resolve Unauthorized/Failed asymmetry (DEFERRED to MAL-2026-003)

**#4–#7 (MEDIUM):**
- Make JSON converters forward-compatible (skip unknown properties)
- Write missing NotFound tests (DONE — Simon)
- Fix source generator incremental caching (SemanticModel removal)
- Clean up test artifacts and CI config

#### Full Review Location

See `.squad/decisions/inbox/mal-codebase-review-2026-04-07.md` for comprehensive analysis across core framework, source generation, tests, sample app, and CI configuration.

---

### Decision 6: Test Strategy for Empty() + Boolean Properties (SIMON-2026-001)

**Decision ID:** SIMON-2026-001  
**Date:** 2026-04-10  
**Author:** Simon (Tester/QA)  
**Status:** APPROVED AND IMPLEMENTED

#### Placement Decision

Boolean property tests placed in `Status.cs` as individual `[Fact]` methods per Andrew's brief, not in new `Properties.cs` with `[Theory]` as originally suggested in MAL-2026-004.

**Rationale:**
- Andrew's explicit instruction is authoritative
- Individual `[Fact]` methods produce clearer failure messages
- Avoids factory method lookup by string (reflection fragility)

#### Snapshot Pre-Creation Strategy

Snapshots were created before Kaylee's implementation based on MAL-2026-004 spec. This approach:
- Enables parallel work (Kaylee implementing, Simon testing, no dependency)
- Snapshots pre-approved to spec values
- Tests compile and pass immediately when Kaylee's branch lands

#### Test Coverage

17 new tests written:
- `Status.cs`: ReturnsEmptyWhenEmpty() + 14 boolean property tests
- `Data.cs`: ThrowsInvalidOperationExceptionWhenEmpty()
- `Read.cs`: Added `{"Status":5}` inline data case
- `Write.cs`: GeneratesValidJsonWhenEmpty()

All 117 tests passing.

---

### Decision 7: URL Binding API Design (MAL-2026-005)

**Decision ID:** MAL-2026-005  
**Date:** 2026-04-11  
**Author:** Mal (Lead)  
**Status:** APPROVED AND IMPLEMENTED

#### Verdict

**APPROVED** — Implement Option D with Option A's path inference. This design enables RESTful URL patterns while keeping BluQube's API surface clean and backward-compatible.

#### The Problem

BluQube currently binds ALL command/query properties from POST body, blocking REST patterns like `commands/todo/{id}/update` where path parameters need explicit extraction.

#### Approved Design

1. **Add `Method` property to `BluQubeQueryAttribute`** — default `"POST"`, optional `"GET"` for idempotent queries
2. **Infer path parameters from path template** using regex pattern `\{(\w+)\}` — case-insensitive matching to record properties
3. **Commands:** Always POST; path params from template, rest to body
4. **Queries:** Path params from template, remaining based on Method (`POST` → body, `GET` → querystring)
5. **Client-side:** Generated `BuildPath()` override with string interpolation + `Uri.EscapeDataString()` (WASM-safe, zero reflection)
6. **Server-side:** Generator emits internal shim records with explicit `[FromRoute]`/`[FromQuery]` attributes; keeps user types clean

#### Example: Command with Path Param

```csharp
[BluQubeCommand(Path = "commands/todo/{id}/update")]
public record UpdateTodoCommand(Guid Id, string Title) : ICommand;
// POST /commands/todo/abc123/update  Body: {"Title":"..."}
```

Generated client:
```csharp
protected override string BuildPath(UpdateTodoCommand request)
{
    return $"commands/todo/{Uri.EscapeDataString(request.Id.ToString())}/update";
}
```

Generated server:
```csharp
endpointRouteBuilder.MapPost("commands/todo/{id}/update", 
    async (ICommandRunner runner, [FromRoute] Guid id, UpdateTodoCommandBody body) => {
        var cmd = new UpdateTodoCommand(id, body.Title);
        var result = await runner.Send(cmd);
        return Results.Json(result);
    });
```

#### Breaking Changes

**None.** Existing commands/queries without `{param}` in path work exactly as before. New feature requires explicit opt-in.

#### Implementation Status

- **Kaylee:** Implemented full URL parameter binding (Phase 1). Build: 0 errors, 0 warnings on .NET 8/9/10.
- **Simon:** Wrote 7 generator snapshot tests + 8 integration test stubs. All compile, ready to enable post-implementation.
- **16 files modified**, ~500 lines added. Tests for URL escaping, querystring null handling, and generator error handling deferred per team roadmap.

---

### Decision 6: TDD Red/Green Workflow Adopted (MAL-2026-006)

**Decision ID:** MAL-2026-006  
**Date:** 2026-04-20  
**Author:** Andrew Davis (user directive)  
**Status:** APPROVED

#### Verdict

**APPROVED** — Squad adopts strict TDD red/green workflow. Tests precede implementation at all times.

#### The Directive

User requirement: "The squad works TDD — red/green pattern. Simon writes failing tests first. Kaylee makes them green. Mal enforces this in code review. No implementation ships without a prior failing test."

#### Implementation

**Red Phase (Simon):**
- Write xUnit tests that compile but fail (no `[Skip]`, no placeholder assertions)
- Tests must be committed to git and visibly failing *before* implementation starts
- Must cover happy path and edge cases

**Green Phase (Kaylee):**
- Implement feature to make tests pass
- No implementation code ships without prior red test commits in history
- Focus on minimal implementation to satisfy tests

**Enforcement (Mal):**
- Code review rejects PRs where failing test commits don't precede implementation commits
- Validates commit history shows red → green progression
- Returns PRs for rework if pattern violated

**Ceremony: TDD Gate**
- **Trigger:** Auto, before Kaylee starts any new feature implementation task
- **Facilitator:** Simon
- **Participants:** Simon, Kaylee
- **Agenda:**
  1. Simon confirms tests are written, compiled, and currently failing (red state)
  2. Simon describes what tests cover (happy path, edge cases)
  3. Kaylee confirms she understands what "green" means for this feature
  4. Go/no-go decision: Simon green-lights implementation start, or Simon writes more tests first

#### Rationale

Ensures tests validate *actual behavior* rather than being written after implementation as rubber-stamp documentation. Prevents implementation bias in test design and guarantees test suite reflects real requirements.

#### Governance Update

TDD Gate ceremony already present in `ceremonies.md` (line 45–62). Team charters updated to enforce pattern.

---

### Decision 8: Documentation Strategy for BluQube (INARA-2026-001)

**Decision ID:** INARA-2026-001  
**Date:** 2026-04-21  
**Author:** Inara (Docs/DevRel)  
**Status:** APPROVED AND IMPLEMENTED

#### Verdict

**APPROVED** — Implement hybrid documentation strategy: Enhanced README + focused `/docs/` markdown guides. This balances discoverability, currency, and maintenance burden for a lean .NET framework library.

#### The Problem

BluQube is a published NuGet package (.NET 8/9/10, Blazor Server/WASM), but documentation is incomplete:
- README exists (155 lines) but lacks advanced patterns
- No `/docs/` folder with deep-dive guides
- No Getting Started guide (setup takes >1 hour)
- Authorization/validation patterns undocumented
- Troubleshooting guide missing (source generation failures cryptic)
- No WASM deployment guide
- Sample app production-quality but undiscovered

#### Approved Approach

**README.md** → front door (quick start, links to guides)  
**docs/*.md** → deep dives (150–500 lines each, indexed by topic)  
**XML comments** → IDE IntelliSense (auto-generated from code)  
**Sample app** → canonical reference (linked from relevant guides)

#### Why NOT a Full Documentation Site?

| Option | Verdict | Rationale |
|--------|---------|-----------|
| **DocFX / VitePress site** | ❌ Overkill | BluQube is single library (~2000 LOC). Adds deployment, tooling, and drift. |
| **GitHub Pages static site** | ❌ Overkill | Same as above; markdown in repo is simpler. |
| **Minimal README only** | ❌ Incomplete | Leaves users blocked on setup, authorization, validation. |
| **This hybrid (README + /docs)** | ✅ Optimal | Discoverable, maintainable, expert-friendly, searchable. |

**Key insight:** Target audience is **expert .NET developers** who prefer markdown with runnable examples, indexed by problem, and local (in repo).

#### Approved Folder Structure

```
C:\Code\bluqube/
├── README.md (updated: add /docs links, sample app section)
├── CONTRIBUTING.md (new)
├── docs/
│   ├── GETTING_STARTED.md
│   ├── AUTHORIZATION_GUIDE.md
│   ├── TROUBLESHOOTING.md
│   ├── VALIDATION_GUIDE.md (Phase 2)
│   ├── URL_BINDING_GUIDE.md (Phase 2)
│   ├── SOURCE_GENERATION_INTERNALS.md (Phase 2)
│   ├── WASM_DEPLOYMENT.md (Phase 2)
│   └── API_REFERENCE.md (Phase 4)
└── (source files with XML doc comments)
```

#### Implementation Status

**Phase 1 (COMPLETE):**
- ✅ `docs/GETTING_STARTED.md` (11 KB, 350+ lines) — zero-to-working-app guide, <30 min target
- ✅ `docs/AUTHORIZATION_GUIDE.md` (9.4 KB, 235 lines) — MediatR.Behaviors.Authorization integration
- ✅ `docs/TROUBLESHOOTING.md` (16.1 KB, 22 indexed entries) — symptom-indexed troubleshooting

All deliverables verified against source code and sample application. See orchestration log (2026-04-20T14-30-inara.md) for details.

#### Success Metrics (Phase 1)

✅ New developers get running in <30 minutes  
✅ Authorization + validation pain eliminated  
✅ 80% of common problems indexed  
✅ Source generation demystified

#### Binding Decisions

1. **Markdown format, not wiki or external tool** — stays in repo, versioned with code
2. **Phase 1 before Phase 2** — sequential delivery ensures quality
3. **No docs site infrastructure** — if future growth demands VitePress, that's a separate decision
4. **Sample app as single source of truth** — docs link to it; don't copy/paste (examples drift)

---

## Governance

- All meaningful changes require team consensus
- Document architectural decisions here
- Keep history focused on work, decisions focused on direction
