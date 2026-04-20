# BluQube

[![NuGet](https://img.shields.io/nuget/v/BluQube.svg)](https://nuget.org/packages/BluQube) [![Nuget](https://img.shields.io/nuget/dt/BluQube.svg)](https://nuget.org/packages/BluQube)

BluQube is a framework for Blazor to help with the write once, run on Server or WASM approach.

## Overview

BluQube provides a unified development experience for Blazor applications, allowing you to write your application logic once and deploy it to both Blazor Server and Blazor WebAssembly without modification. The framework includes command and query handling patterns with built-in validation and error handling.

## Features

- **Unified Blazor Development**: Write once, run on both Server and WASM
- **Command & Query Pattern**: Built-in CQRS-style architecture
- **Validation Support**: Integrated validation with detailed error reporting
- **Source Generation**: Automated code generation for improved performance
- **Error Handling**: Comprehensive error handling with structured error data

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
## Contributing
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

