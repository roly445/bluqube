# BluQube Setup

## Install

Add the NuGet package to the projects that define or use BluQube records:

```bash
dotnet add package BluQube
```

## Server Setup

Use `[BluQubeResponder]` on the server entry point so source generation emits endpoint responders.

```csharp
using BluQube.Attributes;
using BluQube.Authorization;
using BluQube.Commands;
using BluQube.Queries;
using FluentValidation;
using Microsoft.AspNetCore.Http.Json;
using System.Reflection;

[BluQubeResponder]
public static class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddValidatorsFromAssemblyContaining<SomeValidator>();
        builder.Services.AddBluQube(Assembly.GetExecutingAssembly());
        builder.Services.AddBluQubeAuthorization(typeof(Program).Assembly);
        builder.Services.AddScoped<ICommandRunner, CommandRunner>();
        builder.Services.AddScoped<IQueryRunner, QueryRunner>();

        builder.Services.Configure<JsonOptions>(options =>
        {
            options.AddBluQubeJsonConverters();
        });

        var app = builder.Build();
        app.AddBluQubeApi();
        app.Run();
    }
}
```

Call `app.AddBluQubeApi()` before `app.Run()`. Register validators before handlers are resolved. Pass the assembly containing server handlers, processors, validators, and authorizers to `AddBluQube(...)` / `AddBluQubeAuthorization(...)`.

## Client WASM Setup

Use `[BluQubeRequester]` on the client entry point so source generation emits HTTP requesters.

```csharp
using BluQube.Attributes;
using BluQube.Commands;
using BluQube.Queries;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

[BluQubeRequester]
public static class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault(args);

        builder.Services.AddScoped<ICommandRunner, CommandRunner>();
        builder.Services.AddScoped<IQueryRunner, QueryRunner>();

        builder.Services.AddHttpClient(
            "bluqube",
            client => client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress));

        builder.Services.AddTransient<CommandResultConverter>();
        builder.Services.AddBluQubeRequesters();

        await builder.Build().RunAsync();
    }
}
```

The named `bluqube` `HttpClient` is used by generated requesters. `AddBluQubeRequesters()` registers generated HTTP command handlers/query processors and the BluQube mediator for the client.

## Project Boundaries

Recommended layout:

```text
Shared or Client: command/query records and result records
Server: handlers, processors, validators, authorizers, database access
Client WASM: components and generated requesters
```

Do not put server-only dependencies such as EF Core, ASP.NET middleware, or handler implementations in the WASM client.

## Source Generation Markers

- `[BluQubeRequester]`: generate HTTP requesters and `AddBluQubeRequesters()`.
- `[BluQubeResponder]`: generate ASP.NET endpoints and `AddBluQubeApi()`.
- `[BluQubeCommand]` and `[BluQubeQuery]`: identify request records and routes.

## Mediation

BluQube uses its own first-party mediator. Do not add `MediatR`, `Mediator.Abstractions`, or `Mediator.SourceGenerator` for BluQube dispatch.

- Server: call `builder.Services.AddBluQube(assembly)` to register `IBluQubeMediator` and scan for `ICommandHandler<...>` / `IQueryProcessor<...>`.
- Client: call `builder.Services.AddBluQubeRequesters()` to register generated HTTP handlers/processors and the mediator.
- Runners: `CommandRunner` and `QueryRunner` use `IBluQubeMediator` internally.

Run `dotnet clean` followed by `dotnet build` if generated files appear stale.
