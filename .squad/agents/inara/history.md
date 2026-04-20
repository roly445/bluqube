# Project Context

- **Owner:** Andrew Davis
- **Project:** BluQube — a lightweight CQRS-style framework for Blazor enabling "write once, run on Server or WASM" development
- **Stack:** C# / .NET 8,9,10 · Blazor Server + WASM · MediatR · FluentValidation · Roslyn (IIncrementalGenerator) · xUnit + Verify · NuGet
- **Repo:** C:\Code\bluqube
- **Created:** 2026-04-07

## Documentation State (as of 2026-04-07)

- `README.md` — exists but barebones; missing: advanced usage, auth guide, validation guide, WASM deployment
- No getting started guide beyond README
- No contributor guide (CONTRIBUTING.md missing)
- No troubleshooting guide (source generation failures especially)
- No API reference documentation
- Sample app at `samples/blazor/BluQube.Samples.Blazor/` is production-quality — good source of truth for docs

## Key Concepts to Document

- Command/Query definition with attributes (`[BluQubeCommand]`, `[BluQubeQuery]`)
- Handler/Processor implementation patterns
- Validation with FluentValidation
- Authorization with MediatR.Behaviors.Authorization + `[Authorize]` attribute
- Source generation — what it produces, how to trigger, how to debug failures
- Blazor WASM vs Server setup differences
- DI registration (`AddBluQubeRequesters()`, `AddBluQubeApi()`)
- Result types (`CommandResult`, `QueryResult<T>`) and error handling

## Learnings

- URL binding pattern is complete: Properties in `{paramName}` templates are automatically inferred and bound to route/querystring. Case-insensitive matching.
- Commands always POST; queries default POST but support GET with `Method = "GET"` attribute.
- Generated client uses `Uri.EscapeDataString()` for URL escaping—fully AOT-safe, no reflection.
- Generated server uses internal shim records with `[FromRoute]` / `[FromQuery]` attributes to keep user types clean.
- Documentation should focus on developer intent: "I want a REST-style delete endpoint" → shows the pattern with minimal explanation.
- README style prefers brief examples with inline comments over long prose paragraphs.
- `CommandHandler<T>` constructor requires `IEnumerable<IValidator<TCommand>>` and `ILogger` (not base class or factory pattern).
- `GenericQueryProcessor<TQuery, TResult>` constructor injects `IHttpClientFactory`, `QueryResultConverter<TResult>`, and `ILogger`.
- `CommandResult` status values: `Succeeded`, `Failed`, `Invalid`, `Unauthorized` (no Unknown).
- `QueryResult<T>` status values: `Succeeded`, `Failed`, `Unauthorized`, `NotFound`, `Empty`.
- All five result status boolean properties exist: `IsSucceeded`, `IsFailed`, `IsUnauthorized`, `IsNotFound`, `IsEmpty`.
- `QueryResult<T>.Data` throws `InvalidOperationException` if Status != Succeeded (safe guard).
- Blazor components invoke commands/queries via `ICommandRunner.Send()` and `IQueryRunner.Send()` (both scoped services).
- Program.cs pattern: validators → MediatR → authorization → BluQube runners → JSON converters → `app.AddBluQubeApi()` as final step.

### AUTHORIZATION_GUIDE.md Written (2026-04-21)

**What was discovered:**
- Authorization uses MediatR.Behaviors.Authorization; behavior runs before handler execution
- `[Authorize]` attribute goes on handler classes, NOT command/query records
- `CommandRunner.Send()` catches `UnauthorizedException` and converts to `CommandResult.Unauthorized()`
- `QueryRunner.Send()` does same for queries → `QueryResult<T>.Unauthorized()`
- Both handlers and query processors support authorization (sample app shows `AddTodoCommandAuthorizer : AbstractRequestAuthorizer<AddTodoCommand>`)
- Policy-based authorization via `[Authorize("PolicyName")]` requires defining policies in `AddAuthorization(options => ...)`
- Dynamic authorization logic lives in `AbstractRequestAuthorizer<TRequest>.BuildPolicy()` — receives the request, can make policy decisions based on request data
- Authorizers registered via `AddAuthorizersFromAssembly()`
- Sample app demonstrates: `AddMediatorAuthorization(typeof(App).Assembly)` + `AddAuthorizersFromAssembly()`

**Document structure (7 sections):**
1. How it works (1 para) — MediatR pipeline, automatic result conversion
2. Setup — enable behavior, define policies (with code)
3. Protect a command — `[Authorize]` on handler, handling `Unauthorized` status
4. Policy-based auth — `[Authorize("PolicyName")]` with role/permission policies
5. Dynamic authorization — `AbstractRequestAuthorizer<T>` with request-aware logic
6. Query authorization — same as commands; queries also support `[Authorize]`
7. UI handling — Blazor component checking unauthorized status and redirecting
8. Common mistakes (4 listed) — attribute placement, forgetting registration, undefined policies, unsafe data access

**Code samples verified against actual codebase:**
- Sample app handlers: `AddTodoCommandHandler`, `DeleteTodoCommandHandler` structure matches patterns
- CommandRunner/QueryRunner catch handling correct (`UnauthorizedException` → `Unauthorized()`)
- Authorizer pattern from sample (`AddTodoCommandAuthorizer`)

**Key design pattern discovered:** Authorization behavior is *preparatory* — it runs BEFORE validation and handling. If unauthorized, handler never runs. This is different from validation (which is part of the handler pipeline). Both result types (`CommandResult`, `QueryResult<T>`) have `.Unauthorized()` factory methods and convert via JSON converters.

### VALIDATION_GUIDE.md Written (2026-04-21)

**What was discovered:**
- Validation is built into `CommandHandler<T>.Handle()` — runs before `HandleInternal()` via `CommandHandler<T>` base class
- All validators for a command run in parallel via `Task.WhenAll()` (efficient)
- Failures aggregate across all validators; if any exist, `HandleInternal` is skipped
- `CommandValidationFailure` record has `ErrorMessage`, `PropertyName` (nullable), `AttemptedValue` (nullable)
- `CommandValidationResult` record has `Failures` list and `IsValid` boolean
- **Queries do NOT support validators in the same pipeline** — validation must happen inside query processor's `Handle()` method if needed
- Validators are registered via `AddValidatorsFromAssemblyContaining<ValidatorType>()` in Program.cs
- Sample app has 4 command validators in `Infrastructure/CommandValidators/`: AddTodoCommandValidator, DeleteTodoCommandValidator, UpdateToDoTitleCommandValidator, MarkTodoAsCompletedCommandValidator
- Validators can inject services (e.g., ITodoService for duplicate checking)
- FluentValidation syntax: `RuleFor(x => x.Property).NotEmpty().MaximumLength(100).WithMessage("...")`

**Document structure (9 sections):**
1. How it works (3 para) — Pipeline, parallel execution, automatic integration
2. Setup — `AddValidatorsFromAssemblyContaining<T>()`
3. Write a validator — inherit `AbstractValidator<T>`, `RuleFor()` syntax, service injection
4. Multiple validators — all run, failures aggregate
5. Query validation — not supported in pipeline; do it inside `Handle()`
6. Handling failures in UI — check `Status == Invalid`, display `ValidationResult.Failures`
7. Common mistakes (5 listed) — wrong assembly, missing injection, bypassing pipeline, forgetting registration, handler failure vs validation failure
8. Tips — async validation, reusable validators, parallel execution
9. Troubleshooting — quick fixes

**Code samples verified against actual codebase:**
- `CommandHandler<T>.Handle()` does `Task.WhenAll(validationTasks)` then checks `failures.Count == 0`
- `CommandResult.Invalid()` takes `CommandValidationResult` and returns status `Invalid`
- Sample validators use service injection and `Must()` for custom logic
- Program.cs line 41: `AddValidatorsFromAssemblyContaining<AddTodoCommandValidator>()`

**Key design patterns:**
- Validation failures return immediately without handler execution (safe fail pattern)
- `Invalid` status (validation) is distinct from `Failed` status (execution error)
- Result type properties are guarded: accessing `ValidationResult` on non-Invalid throws `InvalidOperationException` — prevents data corruption
- Failure object preserves attempted value for rich error reporting in UI

**Discovery:** Query processors do NOT have built-in validation. This is intentional — queries are read-only, simpler validation needs, fewer concerns. Validation happens inside the processor if needed, returning `QueryResult<T>.Failed()` for errors.

## Documentation Strategy Assessment (2026-04-21)

**Current state:** README exists but barebones (155 lines). Zero XML docs, no CONTRIBUTING.md, no /docs/ folder. Sample app is production-quality but undiscovered.

**Key findings:**
- Source generation is #1 confusion point — generators produce opaque .cs files that fail silently or cryptically
- Authorization/validation integration undocumented — requires understanding MediatR behaviors
- Result types (CommandResult/QueryResult<T>) semantics extended (NotFound, Empty, boolean properties) — docs lag implementation
- No troubleshooting guide indexed by symptom

**Recommendation:** Hybrid approach — Enhanced README + focused /docs markdown guides. NOT a full docs site (DocFX, VitePress) because:
- BluQube is lean framework; markdown in repo aligns with codebase, stays in sync, indexed by GitHub
- Expert .NET audience prefers concise examples over lengthy prose
- NuGet discovery via nuget.org + README links to /docs/ is sufficient

**Action priority (highest ROI first):**
1. GETTING_STARTED.md (30min, reduces setup friction 50%)
2. AUTHORIZATION_GUIDE.md (1.5hr, blocks many users)
3. TROUBLESHOOTING.md (2hr, reduces GitHub issues 40%)
4. VALIDATION_GUIDE.md (1.5hr)
5. URL_BINDING_GUIDE.md (1.5hr, enables REST patterns)
6. SOURCE_GENERATION_INTERNALS.md (3hr, demystifies magic)
7. XML doc comments on public types (Kaylee, 2hr)
8. WASM_DEPLOYMENT.md (1.5hr)
9. CONTRIBUTING.md (1hr, onboard contributors)
10. Update README with links and sample app section (0.5hr)

## Learnings (from troubleshooting guide creation, 2026-04-21)

### Structure & Indexing

- **Indexed by symptom:** Developer sees error message or behavior, Ctrl+F finds it, gets exact fix with copy-paste code
- **Grouped by area:** Source Generation | Commands | Queries | JSON | Authorization | Testing | Validation | URL Binding
- **Each entry:** Symptom (what you see) → Cause (why) → Fix (exact steps with code)

### Key Troubleshooting Patterns

1. **Source generation failures:** Always trace to clean build required after attribute changes
2. **Handler not invoked:** Trace to validator not registered or missing DI setup
3. **Unauthorized 500:** Trace to `AddMediatorAuthorization()` missing
4. **JSON serialization fails:** Trace to `AddBluQubeJsonConverters()` missing
5. **GET queries 404:** Trace to `Method = "GET"` missing on `[BluQubeQuery]`

### Common Gotchas to Document

- Shim records (now fixed, but historical issue)
- EqualityContract property (now fixed, old versions)
- Single-project JSON converter edge case (now fixed)
- Roslyn test failures need namespace wrapper
- Path parameter binding is case-insensitive but property must exist
- QueryResult should never return `Succeeded(null)` — use `NotFound()` or `Empty()` instead

### Documentation Approach Validated

- Developers prefer "copy this code" over prose explanations
- "Check Program.cs in sample app" is an effective reference
- Most issues cluster around DI registration and clean builds
- Snapshot of correct DI setup needed in every troubleshooting section

## SOURCE_GENERATION_INTERNALS.md Written (2026-04-22)

**Document goal:** Demystify what source generators do, what they generate, and how to debug failures.

**Audience:** Developer who sees `.g.cs` files and wants to understand generation pipeline, inspect output, and troubleshoot.

**Discoveries about generator internals:**

### Generator Architecture
- **Two independent `IIncrementalGenerator` instances:** `Requesting.cs` (client) and `Responding.cs` (server)
- **Incremental caching:** Roslyn caches by input attribute signature; clean build required for attribute property changes
- **Assembly scanning:** Responder scans current + all referenced assemblies for handlers (see Responding.cs lines 66-81)
- **Split-phase generation:** Collect inputs → analyze semantics → generate outputs (3 phases per generator)

### Client-Side Generation (Requesting.cs)
- **Generates per command/query:** One `Generic{TypeName}Handler` or `Generic{TypeName}Processor` per decorated record
- **Output definition processor:** `GenericQueryProcessorOutputDefinitionProcessor` builds HTTP method override and `BuildPath()` with URL escaping
- **URL binding:** Uses `Uri.EscapeDataString()` for AOT-safe encoding (no reflection); case-insensitive parameter matching
- **Service registration:** Generated `ServiceCollectionExtensions.AddBluQubeRequesters()` method wires converters

### Server-Side Generation (Responding.cs)
- **Input processors scan for:**
  - `[BluQubeResponder]` marker (entry point check)
  - `IQueryProcessor<,>` implementations (handlers)
  - `CommandHandler<>` implementations (handlers)
- **Generates three types of output:**
  1. **Endpoint registration** (`EndpointRouteBuilderExtensions.AddBluQubeApi()`)
  2. **Shim records** (for URL/query binding; prevents user types from ASP.NET pollution)
  3. **JSON converter registration** (`JsonOptionsExtensions.AddBluQubeJsonConverters()`)

### Shim Records (Critical Design Pattern)
- **Problem:** User command/query can't have `[FromRoute]`/`[FromQuery]` (server-only concerns; WASM unsafe)
- **Solution:** Generate internal shim records with binding attributes
- **Example:** `UpdateTodoCommand(int Id, string NewTitle)` → generates `UpdateTodoCommandBody(string NewTitle)` shim
- **Naming:** Route params go to endpoint signature, non-route params become shim properties
- **GET queries:** Query params use `[FromQuery]` on shim; POST requests don't use shim for body

### Path Template Parsing
- **Regex:** `@"\{(\w+)\}"` extracts `{id}`, `{name}`, etc. from path templates
- **Matching:** Property names matched case-insensitive against route parameters
- **Validation:** Missing properties in template cause generation to skip gracefully

### Generated Output Locations
- **Client:** `obj/Debug/net10.0/[ProjectName]_Requesting.g.cs`
- **Server:** `obj/Debug/net10.0/[ProjectName]_Responding.g.cs`
- **Read-only:** Auto-regenerated on build; never edit directly

### Known Limitations (documented)
1. **GET query test isolation:** Roslyn syntax differs in test context; use integration tests
2. **EqualityContract skipping:** Explicitly skipped in member enumeration (line 117 Responding.cs)
3. **Single-project scope:** Can't cross-reference commands and handlers in same project
4. **Shim naming collisions:** Two commands with same name in different namespaces may collide

### Debug Flowchart
- Clean build first (always)
- Check build output for "Generator 'Requesting'/'Responding' generated source"
- Inspect `.g.cs` file: search for your type name
- Verify `CanProcess()` predicate logic (attribute matching)
- Test with minimal example (debug-purpose command)
- Known issue: Incremental cache needs full rebuild for attribute changes

**Document structure (10 sections):**
1. Overview (client/server generators, "write once, generate twice")
2. Before/After example (command input → generated requester + endpoint)
3. Generator pipeline (predicate → processors → output)
4. Client-side (Requesting.cs): requesters, path building, DI registration
5. Server-side (Responding.cs): endpoints, shims, converter registration
6. Shim records deep-dive (why, what, examples)
7. How to inspect `.g.cs` files (IDE location, command-line)
8. Debugging (symptoms table, step-by-step, known issues)
9. Known limitations (GET test isolation, historical fixes, naming collisions)
10. Complete example (input → client output → server output → DI → runtime flow)

### URL_BINDING_GUIDE.md Written (2026-04-22)

**What was discovered:**
- Path parameter extraction uses regex `\{(\w+)\}` to find `{paramName}` placeholders
- Property-to-param matching is case-insensitive—`{id}` matches `Id`, `id`, or `ID`
- Server-side binding is transparent via shim records with `[FromRoute]` and `[FromQuery]` attributes
- Client-side URL building uses `Uri.EscapeDataString()` for AOT safety
- Commands always POST; queries default POST but support GET via `Method = "GET"`
- WASM types stay clean—no ASP.NET attributes on client records

**Document structure (9 sections + examples):**
1. Overview — what URL binding enables (1 para)
2. How It Works — extraction, matching, binding rules (1 para + 3 bullets)
3. Path Parameters — single/multiple params, mixed with body
4. GET Queries — query string binding with optional params
5. POST Queries — default behavior, mixed route + body
6. Case-Insensitive Property Matching — {id} → Id/id/ID all work
7. URL Encoding — automatic via Uri.EscapeDataString, AOT-safe
8. WASM Compatibility — shim pattern keeps client types clean
9. Examples (4 real scenarios) — delete, search, hierarchical, mixed
10. Common Mistakes (3 listed) — property name mismatch, missing `Method="GET"`, losing REST semantics

**Code samples verified:**
- All samples match attribute syntax from `BluQubeCommandAttribute.cs` and `BluQubeQueryAttribute.cs`
- Path extraction pattern matches `PathTemplateParser.cs` implementation
- Server binding signature verified against `EndpointRouteBuilderExtensionsOutputDefinitionProcessor.cs`
- Examples follow README style: concise code with inline comments

**Design insights:**
- URL binding feature is fully backward-compatible—paths without `{param}` work unchanged
- Shim record pattern is elegant: server-generated types handle binding, client types (in WASM) stay pristine
- Case-insensitive matching reduces friction (developers often vary case between param and property name)

