# Contributing to BluQube

Thank you for your interest in contributing to BluQube! This guide helps you get started—whether you're fixing a bug, adding a feature, or improving documentation.

## Getting Started

### Prerequisites

- **.NET 10 SDK** or later ([download](https://dotnet.microsoft.com/download))
- **Git** ([download](https://git-scm.com/))
- A text editor or IDE (Visual Studio, Rider, VS Code recommended)

### Clone & Build

```bash
git clone https://github.com/roly445/bluqube.git
cd bluqube

dotnet build BluQube.sln
```

### Run Tests

BluQube uses xUnit with snapshot testing (Verify). Before submitting a PR, all tests must pass:

```bash
dotnet test BluQube.sln
```

To run a single test:

```bash
dotnet test BluQube.sln --filter "NameOfTest"
```

## Project Structure

```
bluqube/
├── src/
│   ├── BluQube/                    # Main framework library (published to NuGet)
│   │   ├── Commands/               # Command base classes and result types
│   │   ├── Queries/                # Query base classes and result types
│   │   ├── Attributes/             # [BluQubeCommand], [BluQubeQuery], etc.
│   │   └── Utilities/              # Helpers and extensions
│   └── BluQube.SourceGeneration/   # Roslyn incremental generators
│       ├── Requesting.cs           # Client-side code generation
│       └── Responding.cs           # Server-side endpoint generation
├── tests/
│   ├── BluQube.Tests/              # Main unit test suite (xUnit + Verify)
│   ├── BluQube.Tests.RequesterHelpers/   # Test helpers for client generation
│   └── BluQube.Tests.ResponderHelpers/   # Test helpers for server generation
├── samples/
│   └── blazor/BluQube.Samples.Blazor/    # Full Blazor Server+WASM sample app
└── docs/                            # Documentation guides
```

## Development Workflow (TDD Red/Green)

BluQube uses Test-Driven Development. Follow this pattern:

### 1. Write a Failing Test (Red)

Create a test file or add a test case in the appropriate location under `tests/BluQube.Tests/`. Write the test *before* implementing the feature.

Example: Testing a new command handler feature

```csharp
[Fact]
public async Task Handle_WithValidCommand_ReturnsSuccess()
{
    // Arrange
    var command = new AddTodoCommand("Buy milk", "From the store");
    var handler = new AddTodoCommandHandler(
        validators: Array.Empty<IValidator<AddTodoCommand>>(),
        logger: new FakeLogger<AddTodoCommandHandler>());

    // Act
    var result = await handler.Handle(command, CancellationToken.None);

    // Assert
    Assert.Equal(CommandResultStatus.Succeeded, result.Status);
}
```

### 2. Confirm the Test Fails

```bash
dotnet test BluQube.sln --filter "Handle_WithValidCommand_ReturnsSuccess"
```

You should see a **red** (failing) test. This proves the test is meaningful.

### 3. Implement the Minimum Code to Make It Green

Write just enough code to make the test pass. Don't over-engineer.

### 4. Refactor (Optional)

If needed, clean up and improve the code while keeping the test passing.

### 5. Repeat

Add more test cases for edge cases, error conditions, etc. Each new test should fail first, then pass.

## Source Generator Development

Changes to `src/BluQube.SourceGeneration/` require special care:

### Clean Build Required

After modifying attributes in `src/BluQube/Attributes/` or generator logic:

```bash
dotnet clean BluQube.sln
dotnet build BluQube.sln
```

The Roslyn incremental cache needs a full rebuild to pick up attribute changes.

### Snapshot Tests

Generator tests live in `tests/BluQube.Tests/SourceGeneration/` and use snapshot testing (Verify). When snapshot files change intentionally:

```bash
dotnet test BluQube.sln --filter "RequestingGeneratorTests or UrlBindingGeneratorTests" -- --verify --autoAccept
```

Or update individual snapshots in the IDE.

### Known Patterns

- **Attribute coupling:** Changes to `[BluQubeCommand]`, `[BluQubeQuery]`, etc. must be reflected in both `Requesting.cs` (client) and `Responding.cs` (server)
- **Shim records:** Server-side generation creates internal shim records with `[FromRoute]`/`[FromQuery]` to keep user types clean
- **Namespace wrapping:** Roslyn test inputs must wrap generated code in `global namespace` to avoid name collision issues

## Code Style & Conventions

### Public API

- `src/BluQube` is published on NuGet; **breaking changes require discussion** via a GitHub issue first
- All new public types must have XML doc comments (`/// <summary>`, etc.)
- Prefer immutable patterns: use `record` for data types, `sealed record` for concrete implementations

### Private Implementation

- Use modern C# patterns: expression-bodied members, pattern matching, nullability annotations
- Add comments only when intent is non-obvious; don't over-comment obvious code
- Keep methods small and focused

### Attributes & Generators

Attributes are entry points for source generation. When modifying:

1. Update attribute properties in `src/BluQube/Attributes/`
2. Update both generators to read the new property
3. Add tests in `tests/BluQube.Tests/SourceGeneration/`
4. Update relevant documentation

### Examples from Existing Code

- `CommandHandler<T>` shows required constructor pattern (validators, logger)
- `GenericQueryProcessor<TQuery, TResult>` shows query processor structure
- `CommandResult`, `QueryResult<T>` show result type patterns

## Submitting a PR

### Branch Naming

Use descriptive branch names:

```
feature/url-binding-enhancements
fix/snapshot-cache-issue
docs/add-authorization-guide
refactor/command-handler-validation
```

### PR Checklist

Before opening a pull request, ensure:

- [ ] **All tests pass**: `dotnet test BluQube.sln`
- [ ] **No new warnings**: Build with `dotnet build --configuration Release` (warnings treated as errors in CI)
- [ ] **New public types have XML docs**: Summary and parameter descriptions
- [ ] **Snapshot tests updated**: If generators or converters changed, run `--verify --autoAccept`
- [ ] **Clean build tested**: For generator/attribute changes, run `dotnet clean && dotnet build`
- [ ] **Code follows conventions**: Modern C#, clear variable names, no dead code
- [ ] **Commit messages are clear**: Describe *what* changed and *why*

### CI Pipeline

The GitHub Actions workflow (`.github/workflows/build-and-test.yml`) runs automatically on every push and PR:

1. Restores dependencies
2. Builds with warnings-as-errors (`TreatWarningsAsErrors=true`)
3. Runs tests with code coverage (Cobertura)
4. Uploads coverage to Codecov
5. (On release tags) Packs and publishes to NuGet

**Your PR cannot merge until CI passes.**

## Documentation Contributions

Documentation lives in `docs/` and is linked from `README.md`:

- **AUTHORIZATION_GUIDE.md** — How to use `[Authorize]`, MediatR behaviors, authorization patterns
- **VALIDATION_GUIDE.md** — How to define validators, wire them in DI, handle validation results
- **URL_BINDING_GUIDE.md** — How to use path parameters, query strings, and route inference
- **SOURCE_GENERATION_INTERNALS.md** — Debugging generators, understanding `.g.cs` files
- **TROUBLESHOOTING.md** — Symptom-indexed solutions for common problems
- **GETTING_STARTED.md** — Prerequisites, first commands, sample walkthrough

To contribute documentation:

1. Check `README.md` for links and style
2. Follow the existing markdown style: short paragraphs, code blocks for examples, tables for reference
3. Test any code examples in the sample app
4. Update `README.md` links if adding new guides

## Issue Reporting

Found a bug or want to propose a feature?

### For Bugs

1. **Search first:** Check if the issue already exists
2. **Include reproduction steps:** Exact commands and code to reproduce
3. **Environment details:** .NET version, OS, package version
4. **Error message/log:** Full stack trace if available

### For Features

1. **Describe the use case:** What problem does it solve?
2. **Show examples:** How would the API look?
3. **Link to related patterns:** Point to existing code that inspired this

## Questions?

- **Discussions:** Use GitHub Discussions for questions and design conversations
- **Issues:** File a bug or feature request on GitHub Issues
- **Sample App:** Check `samples/blazor/BluQube.Samples.Blazor/` for working examples

---

**Thank you for contributing to BluQube!** Questions or stuck? Open a discussion—the team is here to help.
