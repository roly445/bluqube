# BluQube — Copilot / AI contributor guide

This document gives concise, actionable guidance for AI coding agents working in the BluQube repository. Focus on discoverable project conventions, high-value files, and workflows so you can be productive quickly.

## What this repo is

BluQube is a lightweight CQRS-style framework for Blazor that enables "write once, run on Server or WASM" development. It provides command/query handling, validation, authorization, and source generation for streamlined request/response patterns across Blazor Server and WebAssembly runtimes.

**Key deliverables:**
- `src/BluQube` — main framework library (public NuGet package, targets .NET 10.0, 9.0, 8.0)
- `src/BluQube.SourceGeneration` — source generators for requester/responder wiring
- `samples/blazor/BluQube.Samples.Blazor` — full Blazor Server + WASM sample with authentication and API routing (targets .NET 10.0)
- `tests/` — unit tests using xUnit with Verify snapshots; includes helper test libraries (targets .NET 10.0)

## Architecture & big picture

**Command/Query flow:**
1. Define `[BluQubeCommand(Path = "...")]`/`[BluQubeQuery(Path = "...")]` records on client
2. Source generator (`Requesting.cs`) emits requester classes that HTTP-serialize the command/query
3. Server has `[BluQubeResponder]` program that source-generates responder endpoints via `Responding.cs`
4. Handlers inherit from `CommandHandler<T>` or `GenericQueryProcessor<TQuery, TResult>` and implement `HandleInternal`/`Handle`
5. Results flow back through `CommandResult`/`QueryResult<T>` types with structured error data via `BluQubeErrorData`

**Why this pattern:**
- Unifies client/server code: declare once, generate requesters & responders automatically
- Validation happens before handlers execute; failed validation returns `CommandResult.Invalid(...)` without handler invocation
- Authorization via `MediatR.Behaviors.Authorization` — attributes like `[Authorize]` on handlers prevent unauthorized execution
- JSON converters handle polymorphic result types (`CommandResult` with different statuses)

## Key files to understand first

- `README.md` — overview and quick examples
- `src/BluQube/Commands/Commander.cs` — entry point for sending commands; catches `UnauthorizedException` from MediatR behaviors
- `src/BluQube/Commands/CommandHandler<T>.cs` — base class showing validation pipeline and `PostHandle` extension hook
- `src/BluQube/Commands/CommandResult*.cs` — result types (Success/Failed/Invalid/Unauthorized); uses `MaybeMonad` for optional error data
- `src/BluQube/Queries/GenericQueryProcessor<TQuery, TResult>.cs` — query execution with error handling
- `src/BluQube/Attributes/BluQube*.cs` — all four attributes that trigger source generation
- `src/BluQube.SourceGeneration/Requesting.cs` & `Responding.cs` — incrementally generate requester classes and responder endpoints
- `samples/blazor/BluQube.Samples.Blazor/Program.cs` — demonstrates DI setup, validators, MediatR config, authorization, and `app.AddBluQubeApi()` call

## Project-specific conventions & patterns

**Commands & Queries:**
- Defined as `public record` with `[BluQubeCommand(Path = "...")]` or `[BluQubeQuery(Path = "...")]`
- Implement `ICommand`, `ICommand<TResult>`, `IQuery<TResult>`, etc.
- Include `string PolicyName { get => "PolicyName"; }` override on `ICommand` to require authorization
- Use `GenericCommandHandler<TCommand1, TCommand2>` and `GenericQueryProcessor<,>` for multi-variant handlers

**Handlers & Processors:**
- Inherit from `CommandHandler<T>` (validates, handles, then calls optional `PostHandle` override for side effects)
- Implement `protected abstract Task<CommandResult> HandleInternal(...)` or `Handle` for queries
- Constructor injects `IEnumerable<IValidator<TCommand>>` and `ILogger` (required dependencies for validation pipeline)
- Return `CommandResult.Success()` / `CommandResult.Failed(...)` / `CommandResult.Invalid(...)` / `CommandResult.Unauthorized()`
- Query handlers return `QueryResult<T>.Succeeded(...)` / `QueryResult<T>.Failed(...)`

**Validation:**
- Use FluentValidation validators (inherit from `AbstractValidator<T>`)
- Register via `builder.Services.AddValidatorsFromAssemblyContaining<ValidatorClass>()`
- Validation failures automatically convert to `CommandResult.Invalid(CommandValidationResult { Failures = [...] })`
- Validation runs before handler; see `CommandHandler<T>.Handle` for pipeline order

**Authorization:**
- Enabled via `builder.Services.AddMediatorAuthorization(assembly)`
- Apply `[Authorize]` or `[AuthorizeAttribute("PolicyName")]` to handlers
- Commands with `PolicyName` property will check that policy; unauthorized requests return `CommandResult.Unauthorized()`
- `Commander.Send` catches `UnauthorizedException` and converts to result

**Source Generation & JSON:**
- Attributes on commands/queries trigger generators to emit HTTP requesters (client) and responders (server endpoints)
- `Commander.cs` and `Querier.cs` use `ISender`/`IReceiver` from MediatR
- JSON converters (`CommandResultConverter`, `CommandResultConverter<T>`) handle polymorphic serialization
- Register converters via `options.AddBluQubeJsonConverters()` in `Configure<JsonOptions>`

## Build, test, and run (developer commands)

Run these from the repository root:

| Task | Command |
|------|---------|
| **Build solution** | `dotnet build BluQube.sln` |
| **Run all tests** | `dotnet test BluQube.sln` (includes code coverage via Cobertura) |
| **Run tests with coverage report** | `dotnet test BluQube.sln /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura` |
| **Build release (pack)** | `dotnet pack --configuration Release --output ./artifacts` |
| **Run sample Blazor app** | `dotnet run --project samples/blazor/BluQube.Samples.Blazor/BluQube.Samples.Blazor.csproj` |

**CI/CD pipeline** (see `.github/workflows/build-and-test.yml`):
- On push to `main` or PR: restore → build with warnings-as-errors (.NET 10.x) → test with coverage → upload to Codecov
- On version tags (`v*`): same as above, then push packages to both MyGet and NuGet.org
- Coverage is excluded for `BluQube.SourceGeneration` assembly (generated code)
- Main library targets .NET 10.0, 9.0, and 8.0 for multi-version support

## Repository patterns to preserve

**Public API stability:**
- `src/BluQube` is a published NuGet package; breaking changes to public types require careful coordination
- Attributes (`BluQubeCommand`, `BluQubeQuery`, etc.) are entry points for source generation — changing attribute properties requires updates in generators
- `CommandResult`, `CommandValidationResult`, `BluQubeErrorData` shapes are serialized; preserve JSON compatibility

**Source generator coupling:**
- Changes to attributes in `src/BluQube/Attributes/*` must be reflected in `src/BluQube.SourceGeneration/Requesting.cs` and `Responding.cs`
- Generators use `IIncrementalGenerator`; incremental caching means attribute changes may not trigger regeneration without a clean build
- Always perform a full rebuild after editing attributes or generator logic

**Testing & verification:**
- Tests use xUnit + Verify (snapshot testing); snapshot files live in `*.verified.txt` alongside test files
- `BluQube.Tests.RequesterHelpers` and `BluQube.Tests.ResponderHelpers` provide stubs for testing generated code
- `Initialization.cs` registers Verify converters for `CommandResult` and `QueryResult<T>`

## Integration points & dependencies

| Component | Purpose | Usage |
|-----------|---------|-------|
| **FluentValidation** | Validator definitions | Inherit `AbstractValidator<T>`, register with `AddValidatorsFromAssemblyContaining<T>()` |
| **MediatR** | Pipeline/handler dispatch | `ISender` injected into `Commander`; behaviors for authorization |
| **MediatR.Behaviors.Authorization** | Authorization pipeline | `[Authorize]` attributes, `UnauthorizedException` handling |
| **MaybeMonad** | Optional error data | `Maybe<BluQubeErrorData>` in `CommandResult` |
| **Blazor (WASM + Server)** | Runtime | Sample uses `AddInteractiveWebAssemblyComponents()` for dual render mode |

## Common code patterns (examples)

**Define a command:**
```csharp
[BluQubeCommand(Path = "commands/add-todo")]
public record AddTodoCommand(string Title, string Description) : ICommand;
```

**Implement a handler:**
```csharp
public class AddTodoCommandHandler(
    IEnumerable<IValidator<AddTodoCommand>> validators,
    ILogger<AddTodoCommandHandler> logger,
    ITodoService todoService)
    : CommandHandler<AddTodoCommand>(validators, logger)
{
    protected override async Task<CommandResult> HandleInternal(
        AddTodoCommand request, CancellationToken cancellationToken)
    {
        await todoService.AddAsync(request.Title, request.Description, cancellationToken);
        return CommandResult.Success();
    }

    protected override async Task<CommandResult> PostHandle(
        AddTodoCommand request, CommandResult originalResult, CancellationToken cancellationToken)
    {
        // Side effects after success: logging, events, etc.
        logger.LogInformation("Todo added: {Title}", request.Title);
        return await base.PostHandle(request, originalResult, cancellationToken);
    }
}
```

**Define a query:**
```csharp
using BluQube.Constants;

// Default POST method for body-based serialization
[BluQubeQuery(Path = "queries/get-todos")]
public record GetTodosQuery : IQuery<GetTodosResult>;

// Override to GET for RESTful read semantics (requires simple query types)
[BluQubeQuery(Path = "queries/get-todos-simple", HttpMethod = HttpRequestMethod.Get)]
public record GetTodosSimpleQuery : IQuery<GetTodosResult>;

public record GetTodosResult(List<TodoItem> Items) : IQueryResult;
```
- `HttpMethod` defaults to `HttpRequestMethod.Post` for body-based serialization
- Set to `HttpRequestMethod.Get` for RESTful query endpoints (suitable for simple query types)
- Endpoint generation automatically uses `MapPost` or `MapGet` based on configuration

**Implement a query processor:**
```csharp
public class GetTodosQueryProcessor(ITodoService todoService)
    : GenericQueryProcessor<GetTodosQuery, GetTodosResult>
{
    public async Task<QueryResult<GetTodosResult>> Handle(
        GetTodosQuery request, CancellationToken cancellationToken)
    {
        var items = await todoService.GetAllAsync(cancellationToken);
        return QueryResult<GetTodosResult>.Succeeded(new GetTodosResult(items));
    }
}
```

**Add authorization to a handler:**
```csharp
[Authorize] // or [Authorize("AdminOnly")]
public class DeleteTodoCommandHandler : CommandHandler<DeleteTodoCommand>
{
    // ... constructor and implementation
}
```

## Common pitfalls & troubleshooting

| Issue | Cause | Solution |
|-------|-------|----------|
| Handler not invoked; validation fails silently | Validator registered but handler not injected | Ensure `AddValidatorsFromAssemblyContaining<T>()` before MediatR setup |
| Source generation not reflecting attribute changes | Incremental generator caching | Run `dotnet clean` then `dotnet build`, or touch a file in `SourceGeneration` folder |
| Unauthorized exceptions in response | `[Authorize]` policy doesn't exist | Define policy in DI setup, e.g., `builder.Services.AddAuthorization(opts => opts.AddPolicy(...))` |
| JSON serialization fails for `CommandResult` | Converter not registered | Call `options.AddBluQubeJsonConverters()` in `Configure<JsonOptions>` |
| Test snapshot mismatch after intentional change | Verify snapshot is outdated | Review diff, accept with `--verify --autoAccept` if correct, or rebuild snapshot |

## When modifying key areas

**Adding a new result status:**
- Update `CommandResultStatus` or `QueryResultStatus` constants
- Update converters (`CommandResultConverter.cs`) to handle new status
- Update `CommandResult`/`QueryResult` factory methods to emit new status
- Add test cases and snapshot verifications

**Extending validation pipeline:**
- Add new validators to assembly scanned by `AddValidatorsFromAssemblyContaining<T>()`
- Override `PostHandle` in `CommandHandler<T>` for handler-specific side effects, not validation
- Do NOT bypass validation by directly invoking `HandleInternal`

**Adding a new source generator:**
- Follow pattern in `Requesting.cs` / `Responding.cs`: define input processors, output generators, and syntax provider predicates
- Use `IIncrementalGenerator` (not `ISourceGenerator`)
- Test with snapshot comparisons in `BluQube.Tests`

## Files that exemplify good patterns

- `samples/blazor/BluQube.Samples.Blazor/Program.cs` — complete DI/authorization/JSON setup
- `tests/BluQube.Tests/Commands/` — unit test examples using stubs and Verify snapshots
- `src/BluQube/Commands/CommandHandler<T>.cs` — validation pipeline and extension hooks
- `src/BluQube.SourceGeneration/{Requesting,Responding}.cs` — incremental generator patterns

---

If any section is unclear or you need more context on specific topics (e.g., generator internals, test patterns, or deployment), let me know and I'll expand it.
