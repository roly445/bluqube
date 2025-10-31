# BluQube — Copilot / AI contributor guide

This document gives concise, actionable guidance for AI coding agents working in the BluQube repository. Focus on discoverable project conventions, high-value files, and workflows so you can be productive quickly.

What this repo is
- BluQube is a lightweight framework for Blazor that implements a command & query (CQRS-like) pattern with integrated validation, authorization, and source-generation.
- Key deliverables: `src/BluQube` (framework), `BluQube.SourceGeneration` (source generators), `samples/blazor` (sample app), `tests` (unit tests).

Quick orientation (big picture)
- Core runtime lives in `src/BluQube/`.
  - Commands: `src/BluQube/Commands/*` (ICommand, ICommandHandler, Commander, CommandResult types, validation types).
  - Queries: `src/BluQube/Queries/*` (IQuery, query processors).
  - Source generation: `src/BluQube.SourceGeneration/` — generators that create requester/responder wiring and attributes usage.
- Samples are in `samples/blazor/BluQube.Samples.Blazor` showing how to wire up services, authentication, and the HTTP API.
- Tests under `tests/` demonstrate common unit-test patterns and test helpers (use them as examples).

Key files to read first
- `README.md` — high-level goals and quick examples.
- `src/BluQube/Commander.cs` — application entry for sending commands; shows command dispatch patterns.
- `src/BluQube/Commands/ICommand*.cs` and `CommandHandler*.cs` — shows error/validation handling and result shapes.
- `src/BluQube.SourceGeneration/Requesting.cs` and `Responding.cs` — shows how attributes ([BluQubeCommand], [BluQubeQuery], [BluQubeResponder], [BluQubeRequester]) are interpreted by generators.
- `samples/blazor/BluQube.Samples.Blazor/Program.cs` — shows how sample app wires authentication, services, and maps endpoints using `app.AddBluQubeApi()`.

Project-specific conventions and patterns (concrete)
- Attributes control source generation and routing
  - Commands and Queries are decorated with `[BluQubeCommand(Path = "...")]` or `[BluQubeQuery(Path = "...")]`. The generator relies on these attributes to emit requesters/responders.
- Command handlers inherit from `CommandHandler<T>` or `GenericCommandHandler<T1,T2>` and implement `HandleInternal`.
  - Use `CommandResult.Success()` / `CommandResult.Failed()` / `CommandResult.ValidationErrors(...)` for consistent error semantics.
- Validation is implemented with FluentValidation and the repository uses `AddValidatorsFromAssemblyContaining<TValidator>()` in samples.
- Authorization uses `MediatR.Behaviors.Authorization` and project provides `AddMediatorAuthorization` wiring in sample.
- JSON: Project registers BluQube-specific JSON converters via `options.AddBluQubeJsonConverters()` (see `Program.cs`).

Build, test, and run (developer commands)
- Build solution
  - dotnet build BluQube.sln
- Run all tests
  - dotnet test BluQube.sln --no-build
- Run sample Blazor app (from `samples/blazor/BluQube.Samples.Blazor`)
  - Use the IDE to run, or run `dotnet run --project samples/blazor/BluQube.Samples.Blazor/BluQube.Samples.Blazor.csproj`
- Packaging / publishing is via normal NuGet flows (see `artifacts/` for published packages).

Repository patterns to preserve when editing
- Keep public APIs stable: many consumers depend on the `src/BluQube` library and its attributes. Avoid renaming public types without updates to source generators and samples.
- Source generators: changes often require updating both `BluQube.SourceGeneration` and the consumers (attributes/signatures). When editing attributes, update `Requesting.cs`/`Responding.cs` accordingly.

Integration points and external dependencies
- FluentValidation for validators (look for `AddValidatorsFromAssemblyContaining` usage).
- MediatR and `MediatR.Behaviors.Authorization` for pipeline behaviors and authorization.
- Blazor-specific packages (RazorComponents, WASM render mode) in samples.

Testing patterns and helpers
- Tests use xUnit (see `tests/BluQube.Tests/`). Look for `Initialization.cs` for test setup.
- Use test helper stubs in `tests/*/Stubs` when creating new tests that need requester/responder behavior.

Examples (copy-paste friendly)
- Define a command (found in samples)

  [BluQubeCommand(Path = "commands/mycommand")]
  public record MyCommand(string Name) : ICommand;

- Handler pattern (from `Commands/CommandHandler` classes)

  public class MyCommandHandler : CommandHandler<MyCommand>
  {
      protected override Task<CommandResult> HandleInternal(MyCommand request, CancellationToken cancellationToken)
      {
          return Task.FromResult(CommandResult.Success());
      }
  }

What to watch for (common pitfalls)
- Source generator surprises: changing attribute properties will not affect compiled consumers until generator re-runs; run a full rebuild after edits.
- Public API compatibility: package consumers may break if method signatures or attribute names change.
- Tests that depend on environment: some sample wiring expects authentication and HttpContext; prefer unit tests that mock these where possible.

If you need to modify build or CI
- There is no CI config in-repo. If adding GitHub Actions, follow the pattern: restore, build, run tests, then pack/publish packages.

If more context is needed
- Read `src/BluQube.SourceGeneration/*` for generator logic and emitted shapes.
- Inspect `tests/` for real usage examples.

Finish and iterate
- If anything above is unclear or you want more examples (e.g., more test samples or generator internals), tell me which area to expand and I will update this file.
