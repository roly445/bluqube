---
name: bluqube
description: Build, configure, debug, or explain applications that use BluQube, the Blazor command/query framework for write-once Server and WebAssembly workflows. Use when working with BluQube commands, queries, command handlers, query processors, runners, source-generated requesters/responders, URL binding, validation, authorization, JSON converters, WASM deployment, or troubleshooting generated endpoints.
---

# BluQube

## Overview

Use this skill to help a project adopt or maintain BluQube. BluQube lets Blazor apps define command/query records once, then run them through generated client requesters and server responders.

## Load References

Load only the reference needed for the current task:

- `references/setup.md` for project setup, DI, Server/WASM boundaries, and source-generation markers.
- `references/commands-queries.md` for defining commands, queries, handlers, processors, runners, and result handling.
- `references/url-binding.md` for route parameters, GET queries, query strings, and body binding.
- `references/validation-authorization.md` for FluentValidation, authorizers, unauthorized results, and authorize-by-default.
- `references/troubleshooting.md` for generation, DI, JSON, route, and runtime problems.

## Default Workflow

1. Identify the app shape: Blazor Server, Blazor WebAssembly with a server backend, or single-project hosted Blazor.
2. Place command/query records in code shared by the caller and handler side.
3. Put handlers, processors, validators, authorizers, database access, and ASP.NET dependencies on the server side.
4. Ensure source generation markers are present: `[BluQubeRequester]` on the client entry point and `[BluQubeResponder]` on the server entry point.
5. Register BluQube services, first-party mediation, generated requesters/responders, JSON converters, validators, and authorization as needed.
6. Use `ICommandRunner.Send(...)` and `IQueryRunner.Send(...)` from components or services.
7. Check result status before reading `Data`, `ValidationResult`, or `ErrorData`.

## Core Rules

- Commands modify state and implement `ICommand` or `ICommand<TResult>`.
- Queries read state and implement `IQuery<TResult>`.
- Commands use `[BluQubeCommand(Path = "...")]`.
- Queries use `[BluQubeQuery(Path = "...")]`; they POST by default and require `Method = "GET"` for query-string endpoints.
- Command handlers should usually inherit `CommandHandler<TCommand>` or `CommandHandler<TCommand, TResult>`.
- Query processors should return `QueryResult<TResult>` and use `Succeeded`, `NotFound`, `Empty`, or `Failed` intentionally.
- BluQube owns its mediation layer; use `AddBluQube(...)` on the server and do not add MediatR or martinothamar/Mediator packages.
- Command validation is built into `CommandHandler<T>` and runs before `HandleInternal`.
- Query validation is not built into the same pipeline; validate inside the processor if needed.
- Authorization uses `IBluQubeAuthorizer<TRequest>` and is registered with `AddBluQubeAuthorization(...)`.

## Implementation Bias

Prefer complete, working BluQube examples over abstract CQRS explanation. When editing an app, update all required pieces together: record, handler/processor, validator/authorizer if needed, DI registration, component call site, and result handling.

## Common Checks

- Source generator did not run: confirm attributes, interfaces, package reference, and run a clean build.
- Handler or processor not resolved: confirm server setup calls `AddBluQube(...)` with the assembly containing handlers/processors.
- Client 404: confirm server has `[BluQubeResponder]`, handler/processor exists, and `app.AddBluQubeApi()` runs.
- Client cannot send request: confirm `[BluQubeRequester]`, `AddHttpClient("bluqube", ...)`, and `AddBluQubeRequesters()`.
- JSON errors: confirm server config calls `options.AddBluQubeJsonConverters()`.
- GET query not using query string: confirm `[BluQubeQuery(..., Method = "GET")]`.
- Validation skipped: confirm validators are registered with `AddValidatorsFromAssemblyContaining<T>()`.
- Unauthorized becomes 500: confirm requests go through `CommandRunner` or `QueryRunner` and `AddBluQubeAuthorization(...)` is registered.

## Useful Prompts

- "Use $bluqube to add a new command and handler for this Blazor feature."
- "Use $bluqube to wire a WASM client to generated BluQube requesters."
- "Use $bluqube to debug why this generated endpoint returns 404."
- "Use $bluqube to convert this API call into a query with URL binding."
