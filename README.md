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

