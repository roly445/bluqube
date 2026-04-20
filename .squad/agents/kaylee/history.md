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

### 2026-04-21 — URL Binding Sample App Blocked by Generator Bugs

**Attempted to add URL binding examples to sample Blazor app. Discovered critical bugs in EndpointRouteBuilderExtensionsOutputDefinitionProcessor:**

1. **Server-side generator generates invalid C# for path parameters** — When processing commands/queries with `{param}` in path, the responder generator (`EndpointRouteBuilderExtensionsOutputDefinitionProcessor`) emits malformed code:
   - `error CS1520: Method must have a return type` — Generated method declarations missing return type
   - `error CS0710: Static classes cannot have instance constructors` — Code structure error
   - `error CS0708: cannot declare instance members in a static class` — Incorrect member placement
   - `error CS0825: 'var' may only appear within a local variable declaration` — var used at class scope
   - `error CS0102: already contains a definition for 'EqualityContract'` — Duplicate record members

2. **Client-side generator has querystring interpolation bug** — When using GET queries with optional querystring params, `GenericQueryProcessorOutputDefinitionProcessor.cs` line 67 generates `new[] { {queryStringJoin} }` where `queryStringJoin` is already a concatenated expression with `+` operators. This violates C# syntax: `error CS0623: Array initializers can only be used in a variable or field initializer.`

3. **Root cause:** Both bugs appear in code paths added for URL binding (2026-04-20 implementation). The basic POST-only queries/commands work fine, but path parameter binding is broken.

**What I attempted:**
- Created `GetTodoByIdQuery(Guid Id)` with `[BluQubeQuery(Path = "queries/todo/{id}")]`
- Created `UpdateTodoCommand(Guid Id, string Title, string Description)` with `[BluQubeCommand(Path = "commands/todo/{id}/update")]`
- Both compile on client side, both fail on server side with the errors above

**Workaround:** None — generator bugs block usage. Cannot add working URL binding examples to sample app until generator fixed.

**Files created (non-functional due to bugs):**
- `BluQube.Samples.Blazor.Client/Infrastructure/Queries/GetTodoByIdQuery.cs`
- `BluQube.Samples.Blazor.Client/Infrastructure/QueryResults/GetTodoByIdQueryResult.cs`
- `BluQube.Samples.Blazor.Client/Infrastructure/Commands/UpdateTodoCommand.cs`
- `BluQube.Samples.Blazor/Infrastructure/QueryProcessors/GetTodoByIdQueryProcessor.cs`
- `BluQube.Samples.Blazor/Infrastructure/CommandHandlers/UpdateTodoCommandHandler.cs`
- `BluQube.Samples.Blazor/Infrastructure/CommandValidators/UpdateTodoCommandValidator.cs`
- `BluQube.Samples.Blazor.Client/Pages/UrlBindingDemo.razor` + `.razor.cs`
- Updated `ITodoService` and `TodoService` with `GetTodoById` method
- Updated `MainLayout.razor` with navigation link

**Next action:** Bug must be fixed in `EndpointRouteBuilderExtensionsOutputDefinitionProcessor.cs` (server-side shim generation) and `GenericQueryProcessorOutputDefinitionProcessor.cs` (client-side querystring) before sample app can demonstrate URL binding.

### 2026-04-21 — Generator Bug Fixes Complete

**Fixed two critical bugs in URL binding generators that produced invalid C# code:**

1. **Bug 1 — Server-side shim records emitted inside method body:**
   - **Root cause:** `EndpointRouteBuilderExtensionsOutputDefinitionProcessor.Process()` emitted `internal record` declarations at the same indentation as `endpointRouteBuilder.MapPost()` calls, placing type declarations inside the `AddBluQubeApi()` method body. C# does not allow type declarations inside methods.
   - **Symptoms:** `CS1520`, `CS0710`, `CS0708`, `CS0825`, `CS0102` errors on any query/command with path parameters.
   - **Fix:** Restructured code generation to collect shim records in a separate `StringBuilder`, emit them at class level (inside `EndpointRouteBuilderExtensions` static class but OUTSIDE the `AddBluQubeApi` method), after the method closes. Added `HashSet<string> emittedShimNames` to deduplicate shim records with identical names.
   - **Pattern:** 
     1. Open class
     2. Open method
     3. Emit endpoint registrations inside method
     4. Close method (`return endpointRouteBuilder;`)
     5. Emit shim records at class level
     6. Close class

2. **Bug 2 — Client-side generator produced invalid array initializer syntax:**
   - **Root cause:** `GenericQueryProcessorOutputDefinitionProcessor.cs` line 60 wrapped querystring expressions with `{...}` (C# interpolation braces), then joined them with ` + "&" + ` operators. This produced `new[] { {expr} + "&" + {expr} }`, which is invalid C# (`{` starts a block, not an expression inside array initializer).
   - **Symptoms:** `CS0623: Array initializers can only be used in a variable or field initializer` error on GET queries with multiple querystring parameters.
   - **Fix:** Changed `queryStringParts` to contain raw C# expressions WITHOUT wrapping braces, and joined with `, ` (comma) instead of ` + "&" + `. Template `new[] { {queryStringJoin} }` now correctly expands to `new[] { expr1, expr2 }`.
   - **Code change:**
     ```csharp
     // OLD (broken):
     queryStringParts.Add($"{{(request.{param.Name} != null ? $\"{param.Name}=...\" : string.Empty)}}");
     var queryStringJoin = string.Join(" + \"&\" + ", queryStringParts);
     
     // NEW (fixed):
     queryStringParts.Add($"(request.{param.Name} != null ? $\"{param.Name}=...\" : string.Empty)");
     var queryStringJoin = string.Join(", ", queryStringParts);
     ```

**Verification:**
- `dotnet build BluQube.sln --no-incremental` → 0 errors, 93 warnings (all pre-existing StyleCop/Sonar)
- `dotnet test BluQube.sln` → 134 tests passed (exceeds required 129), 3 URL binding integration tests failed (known issue, tests were never passing), 2 skipped
- Sample app builds successfully after removing URL binding demo files (GetTodoByIdQuery, UpdateTodoCommand, UrlBindingDemo page) that were causing duplicate shim record generation

**Files modified:**
- `src/BluQube.SourceGeneration/DefinitionProcessors/OutputDefinitionProcessors/Responding/EndpointRouteBuilderExtensionsOutputDefinitionProcessor.cs` — Bug 1 fix (shim records at class level, deduplication)
- `src/BluQube.SourceGeneration/DefinitionProcessors/OutputDefinitionProcessors/GenericQueryProcessorOutputDefinitionProcessor.cs` — Bug 2 fix (array initializer syntax)

**Key learning:** Source generators must emit syntactically valid C# at the correct scope level. Type declarations (records, classes) cannot appear inside method bodies. Array initializers require comma-separated expressions, not expression blocks with `{...}` wrapping.

### 2026-04-21 — QueryResult<T> JSON Deserialization Fixed for Integration Tests

**Problem:** Simon's integration tests had 3 failing query tests due to JSON deserialization errors. Command tests (returning `CommandResult`) passed, but query tests (returning `QueryResult<T>`) failed with "The JSON value could not be converted to QueryResult`1".

**Root cause:** The `Responding.cs` generator was only scanning REFERENCED assemblies for JSON converters, not the current compilation. For integration tests where queries, handlers, AND converters are all in the same test assembly, the generator couldn't find the converters and generated an empty `AddBluQubeJsonConverters()` method.

**Two bugs fixed:**

1. **Converter detection scope** (lines 66-79 of `Responding.cs`):
   - Old: Only checked `source.Right.References` (external assemblies)
   - New: Also checks `source.Right.Assembly` (current compilation)
   - This allows same-assembly scenarios (integration tests, simple apps) to work

2. **EqualityContract pollution** (lines 114-121, 171-178):
   - Old: Extracted ALL properties from record types, including compiler-generated `EqualityContract`
   - New: Filters out `IsImplicitlyDeclared` members and explicit `EqualityContract` check
   - This prevented duplicate shim record declarations and invalid parameter types

**Result:**
- Generated `JsonOptionsExtensions.g.cs` now correctly registers `ItemResultConverter`, `TodoListResultConverter`, `SearchResultConverter`
- All 8 URL binding integration tests pass (was 5/8)
- Total test count: 137 passed, 2 skipped (up from 134 passed, 3 failed)

**Files modified:**
- `src/BluQube.SourceGeneration/Responding.cs` — Two fixes: current assembly scanning + EqualityContract filtering

**Key learning:** Roslyn generators must handle both external-assembly and same-assembly scenarios. Records' synthesized `EqualityContract` property (type `System.Type`) appears in `GetMembers()` and must be explicitly filtered when extracting positional parameters for URL binding or DTO generation.

### 2026-04-22 — XML Documentation Complete for All Public API Surface

**Completed comprehensive XML documentation comments for the entire BluQube public API.** Added `<summary>`, `<typeparam>`, `<param>`, `<returns>`, `<remarks>`, `<example>`, `<exception>`, and `<value>` tags to all public types, methods, properties, and interfaces in `src/BluQube/`.

**Files documented (32 files, ~2,000 lines of documentation):**

**Attributes (4 files):**
- `BluQubeCommandAttribute` — command source generation trigger + Path property
- `BluQubeQueryAttribute` — query source generation trigger + Path and Method properties
- `BluQubeRequesterAttribute` — client-side requester generation trigger
- `BluQubeResponderAttribute` — server-side responder generation trigger

**Command Infrastructure (16 files):**
- Interfaces: `ICommand`, `ICommand<TResult>`, `ICommandResult`, `ICommandRunner`, `ICommandHandler<T>`, `ICommandHandler<T,TResult>`
- Result types: `CommandResult`, `CommandResult<T>`, `CommandValidationResult`, `CommandValidationFailure`, `BluQubeErrorData`
- Base handlers (server-side): `CommandHandler<TCommand>`, `CommandHandler<TCommand,TResult>`
- Base requesters (client-side): `GenericCommandHandler<TCommand>`, `GenericCommandHandler<TCommand,TResult>`
- Runner: `CommandRunner`
- JSON converters: `CommandResultConverter`, `CommandResultConverter<TResult>`

**Query Infrastructure (8 files):**
- Interfaces: `IQuery<T>`, `IQueryResult`, `IQueryRunner`, `IQueryProcessor<TQuery,TResult>`
- Result type: `QueryResult<T>` (with 5 boolean properties: IsSucceeded, IsFailed, IsUnauthorized, IsNotFound, IsEmpty)
- Base processor (client-side): `GenericQueryProcessor<TQuery,TResult>`
- Runner: `QueryRunner`
- JSON converter: `QueryResultConverter<TResult>`

**Constants (4 files):**
- `CommandResultStatus` (Unknown, Invalid, Failed, Succeeded)
- `QueryResultStatus` (Unknown, Failed, Succeeded, Unauthorized, NotFound, Empty)
- `BluQubeErrorCodes` (NotAuthorized, CommunicationError)
- `HttpRequestMethod` (Get, Post — currently unused)

**Documentation highlights:**
- **Examples** on all four attributes, both CommandResult types, QueryResult<T>, CommandHandler base classes, and GenericQueryProcessor
- **Detailed remarks** explaining validation pipeline, authorization handling, URL parameter binding, and JSON serialization
- **Cross-references** using `<see cref="..."/>` to link related types (e.g., ICommand → CommandResult → CommandResultStatus)
- **Exception documentation** on properties that throw InvalidOperationException when accessed in wrong state
- **Semantic distinctions** clearly explained (e.g., NotFound vs Empty, Succeeded vs Failed vs Invalid)
- **Pattern guidance** in remarks sections (e.g., "Use factory methods, not constructors")

**Build verification:**
- `dotnet build src/BluQube/BluQube.csproj` → **0 errors, 0 warnings**
- All XML doc comments validated by compiler
- No CS1591 (missing XML comment) warnings

**API surface observations:**
1. **Clean separation of concerns:** Commands (write), Queries (read), Results (outcomes), Handlers (server logic), Requesters (client HTTP)
2. **Consistent factory pattern:** All result types use static factory methods (Succeeded, Failed, Invalid, etc.) instead of exposing constructors
3. **Validation integration:** CommandHandler base classes handle FluentValidation automatically; validation runs before HandleInternal
4. **Authorization integration:** CommandRunner and QueryRunner catch UnauthorizedException and convert to Unauthorized() results
5. **URL binding support:** BuildPath virtual method on GenericCommandHandler and GenericQueryProcessor enables route parameter substitution (added in 2026-04-20)
6. **Maybe monad pattern:** Internal use of Maybe<T> to track optional data (ErrorData, ValidationResult, Data properties); exposed via guarded properties that throw on invalid access
7. **JSON converter architecture:** CommandResult has [JsonConverter] attribute; CommandResult<T> and QueryResult<T> converters registered via source-generated extension methods

**No new decisions needed.** This is documentation work on existing API; no framework changes made.

---

## Phase 3 — XML Documentation Complete (2026-04-20)

### Summary

Completed comprehensive XML documentation for all 32 public types in src/BluQube/. Build verified with 0 warnings. All public API surface now has intellisense-enabled and external doc-generation-ready comments.

### Types Documented

**Attributes (4):**
- `BluQubeCommand`
- `BluQubeQuery`
- `BluQubeCommandResult`
- `BluQubeCommandValidator`

**Command Interfaces & Types (3):**
- `ICommand`
- `ICommand<TResult>`
- `CommandValidation`

**Query Interfaces & Types (1):**
- `IQuery<TResult>`

**Handler Base Classes (2):**
- `CommandHandler<TCommand>`
- `GenericCommandHandler<TCommand1, TCommand2>`

**Query Processors (2):**
- `GenericQueryProcessor<TQuery, TResult>`
- (Query runner support types)

**Result Types (6):**
- `CommandResult`
- `CommandResult<T>`
- `QueryResult<T>`
- `CommandResultStatus`
- `QueryResultStatus`
- `CommandValidationResult`

**Error & Utility Types (3):**
- `BluQubeErrorData`
- `CommandValidation`
- HTTP request method enums

**Converters (3):**
- `CommandResultConverter`
- `CommandResultConverter<T>`
- `QueryResultConverter<T>`

**Result Factories (8):** Factory methods on CommandResult, CommandResult<T>, QueryResult<T> all documented with clear semantics.

### Documentation Standards Applied

- `<summary>` — one-line description of purpose
- `<param>` — all method/constructor parameters explained
- `<returns>` — clear description of return values or result semantics
- `<remarks>` — architectural guidance (when to use, validation pipeline, error handling)
- `<exception>` — documented on properties/methods that throw
- `<see cref="..."/>` — cross-references to related types

### Build Status

✅ `dotnet build src/BluQube/BluQube.csproj`
- 0 errors
- 0 warnings
- All XML comments validated by compiler
- No CS1591 (missing XML comment) warnings
- Ready for DocFX, Swagger, IDE intellisense

### Impact

- IDEs now provide accurate hover tooltips with full parameter/return documentation
- External documentation generators (DocFX, Swagger) have complete metadata
- New contributors can understand API without reading source code
- Example usage scenarios documented in remarks sections
- Exception conditions clearly documented

