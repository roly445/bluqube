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

### 2026-04-11 — URL Binding Feasibility Analysis

**Deep-dive into Roslyn incremental generators for URL parameter binding:**

1. **RecordDeclarationSyntax.ParameterList** — Positional record parameters are accessible via `.ParameterList.Parameters`, each `ParameterSyntax` exposes `.AttributeLists` for reading per-parameter attributes like `[FromRoute]`. This is how we can support explicit binding hints.

2. **Path template parsing** — Simple regex `@"\{(\w+)\}"` extracts route param names from path strings. Case-insensitive matching (`todoId` vs `TodoId`) avoids friction. The generator already has `GetPath()` extension in `AttributeSyntaxExtensions.cs` to read the `Path` property from attributes.

3. **Client-side URL construction without reflection** — Must be fully source-generated for WASM compatibility. The pattern: add a `protected virtual string BuildPath(TCommand request)` method to the base `GenericCommandHandler<T>` and `GenericQueryProcessor<T,TResult>` classes (default returns literal `Path`), then override in generated subclasses to emit string interpolation code like `$"commands/todo/{Uri.EscapeDataString(request.Id.ToString())}"`.

4. **Server-side mixed binding** — ASP.NET Core 7+ supports `[FromRoute]`, `[FromQuery]`, `[FromBody]` directly on **record constructor parameters**. The model binder inspects the record and binds each parameter from the appropriate source. This means we can keep the clean `MapPost(path, async (ICommandRunner runner, TCommand command) => ...)` pattern without manual decomposition — ASP.NET does it all. No changes to `EndpointRouteBuilderExtensionsOutputDefinitionProcessor.cs` needed if we use native binding.

5. **Key files for URL binding implementation:**
   - Runtime: `GenericCommandHandler`1.cs`, `GenericCommandHandler`2.cs`, `GenericQueryProcessor`2.cs` (add `BuildPath` virtual method)
   - Input processors: `CommandInputDefinitionProcessor.cs`, `QueryInputDefinitionProcessor.cs` (extract parameter metadata from `ParameterList`)
   - Output processors: `GenericCommandHandlerOutputDefinitionProcessor.cs`, `GenericQueryProcessorOutputDefinitionProcessor.cs`, `GenericCommandOfTHandlerOutputDefinitionProcessor.cs` (emit `BuildPath` override when route params detected)
   - New utilities: `PathTemplateParser.cs` (regex-based route param extraction), `ParameterBindingInfo.cs` (data class), `ParameterSyntaxExtensions.cs` (read `[FromRoute]` etc.)

6. **Incremental generator caching** — Adding parameter binding metadata to input definitions requires careful value-equality on the data classes (`ParameterBindingInfo` must implement value equality for incremental caching to work correctly).

**Recommended approach:** Inference-based (detect route params from path template) + optional explicit attributes (`[FromRoute]`, `[FromQuery]`, `[FromBody]`) + native ASP.NET binding on server + source-generated `BuildPath` on client. Avoids reflection, keeps client types clean, leverages ASP.NET's robust binding infrastructure.

Wrote detailed analysis to `.squad/decisions/inbox/kaylee-url-binding-feasibility.md` with code examples, file change matrix, and open questions for team decision.

### 2026-04-20 — URL Binding Implementation Complete

**Full URL parameter binding implemented across all framework layers:**

1. **Client-side (WASM-safe):**
   - Added `protected virtual string BuildPath(TRequest request)` to `GenericCommandHandler<T>`, `GenericCommandHandler<T,TResult>`, and `GenericQueryProcessor<T,TResult>`
   - Generators emit `BuildPath` override when route params detected in path template
   - Uses string interpolation + `Uri.EscapeDataString()` for zero-reflection URL construction
   - For GET queries with non-route params: generated code adds querystring parameters

2. **Server-side (native ASP.NET binding):**
   - Generator emits binding shims for commands/queries with route parameters
   - Shim pattern: separate route params (`[FromRoute]`) from body/querystring params
   - Commands: route params → route, remainder → body shim record
   - Queries GET: route params → route, remainder → querystring shim record with `[FromQuery]`
   - Queries POST: route params → route, remainder → body
   - No changes to user-written record types — clean separation

3. **Attribute changes:**
   - Added `Method` property to `BluQubeQueryAttribute` (default `"POST"`)
   - Commands always POST (no Method property needed)

4. **Generator architecture:**
   - New utilities: `PathTemplateParser` (regex `{param}` extraction), `RecordParameterInfo` (name+type data class)
   - Extended all three input processors (`CommandInputDefinitionProcessor`, `QueryInputDefinitionProcessor`, `CommandWithResultInputDefinitionProcessor`) to read `ParameterList` from `RecordDeclarationSyntax`
   - Extended all three client-side output processors to generate `BuildPath` overrides when route params present
   - `EndpointRouteBuilderExtensionsOutputDefinitionProcessor` now emits shim records + `MapGet`/`MapPost` with explicit binding
   - `Requesting.cs` and `Responding.cs` updated to pass `RecordParameters` and `Method` through pipeline

5. **Key technical decisions:**
   - **Case-insensitive matching:** `{todoId}` in path matches `TodoId` property
   - **Implicit vs explicit:** No `[FromRoute]` attributes on record types — generator detects from path template
   - **Querystring handling:** GET queries use reflection fallback in base class, but generated code overrides with static implementation
   - **Member order:** `BuildPath` placed after `Handle` method to comply with SA1202 (public before protected)

**Build: 0 errors, 0 warnings** across all targets (.NET 8, 9, 10). Existing tests pass (URL binding tests are scaffold-only, currently skipped).

**Files changed:**
- Runtime (3): `GenericCommandHandler`1.cs`, `GenericCommandHandler`2.cs`, `GenericQueryProcessor`2.cs`
- Attributes (1): `BluQubeQueryAttribute.cs`
- Utilities (2 new): `PathTemplateParser.cs`, `RecordParameterInfo.cs`
- Extensions (1): `AttributeSyntaxExtensions.cs`
- Input processors (3): `CommandInputDefinitionProcessor.cs`, `QueryInputDefinitionProcessor.cs`, `CommandWithResultInputDefinitionProcessor.cs`
- Output processors (4): `GenericCommandHandlerOutputDefinitionProcessor.cs`, `GenericCommandOfTHandlerOutputDefinitionProcessor.cs`, `GenericQueryProcessorOutputDefinitionProcessor.cs`, `EndpointRouteBuilderExtensionsOutputDefinitionProcessor.cs`
- Generators (2): `Requesting.cs`, `Responding.cs`

**Total: 16 files modified, 2 new files, ~500 lines added.**

**Orchestration:** Scribe logged orchestration entry `20260420T112126-kaylee-url-binding-impl.md`. Session log written to `.squad/log/20260420T112126-url-binding-implementation.md`. Feature implementation complete; test scaffolding by Simon (15 tests) ready for integration phase.

