# BluQube

[![Build](https://github.com/roly445/bluqube/actions/workflows/build-and-test.yml/badge.svg)](https://github.com/roly445/bluqube/actions/workflows/build-and-test.yml) [![NuGet](https://img.shields.io/nuget/v/BluQube.svg)](https://www.nuget.org/packages/BluQube) [![Codecov](https://codecov.io/gh/roly445/bluqube/branch/main/graph/badge.svg)](https://codecov.io/gh/roly445/bluqube)

BluQube is a framework for Blazor to help with the write once, run on Server or WASM approach.

## Overview

BluQube provides a unified development experience for Blazor applications, allowing you to write your application logic once and deploy it to both Blazor Server and Blazor WebAssembly without modification. The framework includes command and query handling patterns with built-in validation and error handling.

## Features

- **Unified Blazor Development**: Write once, run on both Server and WASM
- **Command & Query Pattern**: Built-in CQRS-style architecture
- **Validation Support**: Integrated validation with detailed error reporting
- **Source Generation**: Automated code generation for improved performance
- **Error Handling**: Comprehensive error handling with structured error data

## Documentation

Comprehensive guides for getting started, advanced patterns, and troubleshooting:

| Guide | Description |
|-------|-------------|
| [Getting Started](docs/GETTING_STARTED.md) | Zero to working app—setup, DI configuration, and your first command |
| [URL Binding](docs/URL_BINDING_GUIDE.md) | Automatic extraction of path parameters, query strings, and request bodies |
| [Validation](docs/VALIDATION_GUIDE.md) | FluentValidation integration with before-handler pipeline |
| [Authorization](docs/AUTHORIZATION_GUIDE.md) | MediatR authorization behaviors with role and dynamic policy support |
| [WASM Deployment](docs/WASM_DEPLOYMENT.md) | Server and WebAssembly deployment patterns and configuration |
| [Source Generation Internals](docs/SOURCE_GENERATION_INTERNALS.md) | How generators work, inspecting output, and troubleshooting |
| [Troubleshooting](docs/TROUBLESHOOTING.md) | Symptom-indexed resolution guide for common issues |

## Quick Start

### Installation

Install the BluQube package via NuGet:

```bash
dotnet add package BluQube
```
### Basic Usage
#### Server side
```csharp
// Handle the command
public class MyCommandHandler(IEnumerable<IValidator<MyCommandCommand>> validators, ILogger<MyCommandCommandHandler> logger)
    : ICommandHandler<MyCommand>(validators, logger)
{
    protected override Task<CommandResult> HandleInternal(AddTodoCommand request, CancellationToken cancellationToken)
    {
        // Your business logic here
        return Task.FromResult(CommandResult.Success());
    }
}

// Process the query
public class MyQueryProcessor() : IQueryProcessor<MyQuery, MyQueryResult>
{
    public Task<QueryResult<GMyQueryResult>> Handle(MyQuery request, CancellationToken cancellationToken)
    {
         return Task.FromResult(QueryResult<MyQueryResult>.Succeeded(new MyQueryResult()));
    }
}
```
#### Client side
```csharp
// Define a command
[BluQubeCommand(Path = "commands/mycommand")]
public record MyCommand(string Name) : ICommand;

[BluQubeQuery(Path = "queries/myquery")]
public record MyQuery(string Name) : IQuery<MyQueryResult>;

public record MyQueryResult(string Info) : IQueryResult;
```

## Core Concepts
### Commands
Commands represent actions that modify application state. They return `CommandResult` objects that can indicate success, failure, or validation errors.
### Queries
Queries retrieve data without modifying state, following the CQRS pattern.
### Error Handling
BluQube provides structured error handling with `BluQubeErrorData` containing error codes and messages.
### Validation
BluQube integrates validation using [FluentValidation](https://github.com/FluentValidation/FluentValidation), allowing you to define validation rules for commands and queries.
### Authorization
BluQube supports authorization checks for commands, ensuring that only authorized users can perform certain actions.  This is performed using the Mediatr behavior [MediatR.Behaviors.Authorization](https://github.com/AustinDavies/MediatR.Behaviors.Authorization/tree/master).

## URL Binding

BluQube enables RESTful URL patterns by automatically binding command and query properties to URL path parameters, query strings, and request bodies—all inferred from your path template.

### Path Parameter Inference

Properties are matched to path parameters using the `{paramName}` pattern in your route template. Matching is case-insensitive.

#### Command with Path Parameter

Properties named in the path template are extracted from the route; remaining properties come from the POST body.

```csharp
[BluQubeCommand(Path = "commands/todo/{id}")]
public record DeleteTodoCommand(Guid Id) : ICommand;

// DELETE POST to: /commands/todo/abc-123-def
// Request body: empty (or omitted)
// Id is bound from the route
```

#### Command with Path Parameter and Body Fields

Mix route parameters with body fields naturally—the generator handles the split.

```csharp
[BluQubeCommand(Path = "commands/todo/{id}/update")]
public record UpdateTodoCommand(Guid Id, string Title) : ICommand;

// POST to: /commands/todo/abc-123-def/update
// Request body: {"Title":"New title"}
// Id from route, Title from body
```

#### GET Query with Path Parameter and Query String

Set `Method = "GET"` for idempotent queries. Path parameters bind to the route; remaining properties become query string parameters.

```csharp
[BluQubeQuery(Path = "queries/todo/{id}", Method = "GET")]
public record GetTodoQuery(Guid Id, string? Filter = null) : IQuery<TodoResult>;

// GET to: /queries/todo/abc-123-def?Filter=completed
// Id from route, Filter from querystring
```

### Automatic URL Building

The client-side request generator produces a `BuildPath()` method that properly escapes URL parameters—fully AOT-safe with zero reflection.

```csharp
// Generated requester handles URL escaping internally
var request = new UpdateTodoCommand(todoId, "New Title");
// Generated code produces: /commands/todo/[todoId-escaped]/update
```

### POST Queries (Default)

If `Method` is not specified, queries POST. Path parameters bind to the route; remaining properties go in the body.

```csharp
[BluQubeQuery(Path = "queries/todo/{id}")]
public record GetTodoDetailQuery(Guid Id) : IQuery<TodoDetailResult>;

// POST to: /queries/todo/abc-123-def
// Request body: empty (if only path params)
```

## Sample Application

The repository includes a full-featured Blazor sample app at `samples/blazor/BluQube.Samples.Blazor/` that demonstrates:

- Command and query definitions with URL binding
- Handler and processor implementation patterns
- Validation and authorization in action
- Blazor Server and WebAssembly interoperability
- Complete DI setup and middleware configuration

Run it locally: `dotnet run --project samples/blazor/BluQube.Samples.Blazor/BluQube.Samples.Blazor.csproj`

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for development setup and contribution guidelines.

Development workflow:
1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests for new functionality
5. Ensure all tests pass
6. Submit a pull request

## License
This project is licensed under the terms specified in the LICENSE file.
## Support
For questions, issues, or contributions, please visit the [GitHub repository](https://github.com/[your-username]/bluqube).

