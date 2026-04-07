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

## Governance

- All meaningful changes require team consensus
- Document architectural decisions here
- Keep history focused on work, decisions focused on direction
