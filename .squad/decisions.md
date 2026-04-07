# Squad Decisions

## Active Decisions

### Decision: QueryResult<T> NotFound/Empty Enhancement

**Decision ID:** MAL-2026-001  
**Date:** 2026-04-08  
**Author:** Mal (Lead)  
**Status:** APPROVED WITH MODIFICATIONS

#### Verdict

**APPROVED** — but with design modifications. The proposal has valid semantics, but the suggested implementation needs adjustment to align with BluQube's existing patterns and maintain API stability.

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

## Governance

- All meaningful changes require team consensus
- Document architectural decisions here
- Keep history focused on work, decisions focused on direction
