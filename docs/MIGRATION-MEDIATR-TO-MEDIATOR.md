# Migration Guide: MediatR → martinothamar/Mediator

This guide covers the changes needed when upgrading from the MediatR-based BluQube to the current version which uses [martinothamar/Mediator](https://github.com/martinothamar/Mediator).

## Why the Change?

MediatR's commercial licensing terms changed. The replacement — `martinothamar/Mediator` — is MIT licensed, source-generator based (zero reflection at runtime), and offers significantly higher throughput. For most BluQube users the migration is a small diff.

---

## What Changed for You

### Summary

| Area | Before | After |
|------|--------|-------|
| NuGet packages | `MediatR` + `MediatR.Behaviors.Authorization` | `Mediator.Abstractions` + `Mediator.SourceGenerator` |
| DI setup | `AddMediatR(...)` + `AddMediatorAuthorization(...)` + `AddAuthorizersFromAssembly(...)` | `AddMediator()` + `AddBluQubeAuthorization(assembly)` |
| Authorizer base class | `AbstractRequestAuthorizer<T>` | `IBluQubeAuthorizer<T>` |
| `UnauthorizedException` namespace | `MediatR.Behaviors.Authorization.Exceptions` | `BluQube.Authorization` |

### What Did NOT Change

Everything you write in application code is unchanged:

- `ICommand`, `ICommand<T>`, `IQuery<T>` — same
- `CommandHandler<T>` / `CommandHandler<T,TResult>` — `HandleInternal` and `PostHandle` signatures unchanged
- `GenericQueryProcessor<TQuery, TResult>` — unchanged
- `CommandResult`, `QueryResult<T>`, `CommandValidationResult`, `BluQubeErrorData` — unchanged
- JSON converters and `AddBluQubeJsonConverters()` — unchanged
- `[BluQubeCommand]`, `[BluQubeQuery]`, `[BluQubeResponder]`, `[BluQubeRequester]` — unchanged
- `ICommandRunner.Send()` / `IQueryRunner.Send()` — unchanged
- `AddBluQubeApi()` — unchanged

---

## Step-by-Step Migration

### 1. Update NuGet Packages

In your server project (`.csproj`):

```xml
<!-- Remove -->
<PackageReference Include="MediatR" Version="..." />
<PackageReference Include="MediatR.Behaviors.Authorization" Version="..." />

<!-- Add -->
<PackageReference Include="Mediator.Abstractions" Version="3.*" />
<PackageReference Include="Mediator.SourceGenerator" Version="3.*">
  <PrivateAssets>all</PrivateAssets>
  <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
</PackageReference>
```

In your Blazor WASM client project, no Mediator packages are needed — only `BluQube` is referenced there.

> **Note:** `Mediator.SourceGenerator` must be added as an analyzer. The XML above uses the standard pattern; if you reference it via `ProjectReference` for local development, add `OutputItemType="Analyzer" ReferenceOutputAssembly="false"`.

### 2. Update `Program.cs` (Server)

**Before:**
```csharp
using MediatR.Behaviors.Authorization.Extensions.DependencyInjection;
using System.Reflection;

builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssemblyContaining<App>());

builder.Services.AddMediatorAuthorization(typeof(App).Assembly);
builder.Services.AddAuthorizersFromAssembly(Assembly.GetExecutingAssembly());
```

**After:**
```csharp
using BluQube.Authorization;

builder.Services.AddMediator();
builder.Services.AddBluQubeAuthorization(typeof(App).Assembly);
```

> `AddBluQubeAuthorization` scans the provided assembly for `IBluQubeAuthorizer<T>` implementations and registers the authorization pipeline behavior automatically.

### 3. Replace `AbstractRequestAuthorizer<T>` with `IBluQubeAuthorizer<T>`

This is the most code-impactful change. The old system used a requirement-based builder pattern; the new system uses a direct `Task<AuthorizationResult>` return.

**Before:**
```csharp
using MediatR.Behaviors.Authorization;
using MediatR.Behaviors.Authorization.Requirements;

public class AddTodoCommandAuthorizer : AbstractRequestAuthorizer<AddTodoCommand>
{
    public override void BuildPolicy(AddTodoCommand request)
    {
        this.UseRequirement(new MustBeAuthenticatedRequirement());
    }
}
```

**After:**
```csharp
using BluQube.Authorization;

public class AddTodoCommandAuthorizer(IHttpContextAccessor httpContextAccessor)
    : IBluQubeAuthorizer<AddTodoCommand>
{
    public Task<AuthorizationResult> Authorize(
        AddTodoCommand request, CancellationToken cancellationToken)
    {
        var user = httpContextAccessor.HttpContext?.User;
        return Task.FromResult(
            user?.Identity?.IsAuthenticated == true
                ? AuthorizationResult.Succeed()
                : AuthorizationResult.Fail("User must be authenticated."));
    }
}
```

For authorizers that previously used `this.GetUserId()` or required injecting a service:

**Before:**
```csharp
public class EditTodoCommandAuthorizer : AbstractRequestAuthorizer<EditTodoCommand>
{
    private readonly ITodoService _todoService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public EditTodoCommandAuthorizer(ITodoService todoService, IHttpContextAccessor httpContextAccessor)
    {
        _todoService = todoService;
        _httpContextAccessor = httpContextAccessor;
    }

    public override void BuildPolicy(EditTodoCommand request)
    {
        var userId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value;
        var todo = _todoService.GetTodo(request.TodoId);

        if (todo?.OwnerId == userId)
            return; // owner passes
        
        this.UseRequirement(new RoleRequirement("Admin"));
    }
}
```

**After:**
```csharp
public class EditTodoCommandAuthorizer(
    ITodoService todoService,
    IHttpContextAccessor httpContextAccessor)
    : IBluQubeAuthorizer<EditTodoCommand>
{
    public async Task<AuthorizationResult> Authorize(
        EditTodoCommand request, CancellationToken cancellationToken)
    {
        var userId = httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value;
        var todo = await todoService.GetTodoAsync(request.TodoId, cancellationToken);

        if (todo?.OwnerId == userId)
            return AuthorizationResult.Succeed();

        var isAdmin = httpContextAccessor.HttpContext?.User.IsInRole("Admin") == true;
        return isAdmin
            ? AuthorizationResult.Succeed()
            : AuthorizationResult.Fail("Only the owner or an admin can edit this todo.");
    }
}
```

> Authorizers are registered automatically by `AddBluQubeAuthorization(assembly)` and run whenever one is registered for the request type. You no longer need a separate `AddAuthorizersFromAssembly()` call.

### 4. Remove Custom `UnauthorizedException` Handling (if any)

If you were catching `MediatR.Behaviors.Authorization.Exceptions.UnauthorizedException` directly (e.g., in a custom exception filter), update the namespace:

**Before:**
```csharp
catch (MediatR.Behaviors.Authorization.Exceptions.UnauthorizedException)
```

**After:**
```csharp
catch (BluQube.Authorization.UnauthorizedException)
```

Note: `CommandRunner.Send()` and `QueryRunner.Send()` handle this internally — you only need to change this if you have your own try/catch around them.

### 5. Update Test Projects

If you have test projects that use `AddMediatR` or `ISender`/`IMediator` from MediatR:

```csharp
// Before
using MediatR;
var mockSender = new Mock<ISender>();

// After
using Mediator;
var mockMediator = new Mock<Mediator.IMediator>();
```

> **Naming collision:** `Mediator` (the namespace) exports `ICommand`, `IQuery<T>`, etc. that clash with BluQube's types. To avoid `CS0104` ambiguous reference errors, avoid `using Mediator;` in files that also use `using BluQube.Commands;` or `using BluQube.Queries;`. Use the fully qualified name `Mediator.IMediator` instead.

Add `Mediator.SourceGenerator` to test projects that host a test server (so Mediator can discover handlers):

```xml
<PackageReference Include="Mediator.SourceGenerator" Version="3.*">
  <PrivateAssets>all</PrivateAssets>
  <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
</PackageReference>
```

---

## Migration Checklist

- [ ] Remove `MediatR` and `MediatR.Behaviors.Authorization` NuGet packages
- [ ] Add `Mediator.Abstractions` and `Mediator.SourceGenerator` to server project(s)
- [ ] Replace `AddMediatR(...)` with `builder.Services.AddMediator()`
- [ ] Replace `AddMediatorAuthorization(...)` + `AddAuthorizersFromAssembly(...)` with `AddBluQubeAuthorization(assembly)`
- [ ] Rewrite each `AbstractRequestAuthorizer<T>` as `IBluQubeAuthorizer<T>`
- [ ] Update any direct catches of `UnauthorizedException` namespace
- [ ] Update test project mocks from `ISender` → `Mediator.IMediator`
- [ ] Run `dotnet build` — fix any remaining `using` references
- [ ] Run `dotnet test` — confirm all tests pass

---

## Troubleshooting

### `Cannot resolve scoped service 'IEnumerable<IPipelineBehavior<...>>' from root provider`

Mediator's source generator registers its `ContainerMetadata` as a Singleton and resolves pipeline behaviors from the root `IServiceProvider` at startup. If your behaviors are registered as `Scoped`, this fails.

`BluQubeAuthorizationBehavior` is registered as `Singleton` (the correct lifetime for Mediator's pipeline). It uses `IHttpContextAccessor.HttpContext?.RequestServices` at runtime to get per-request scoped services (like `IBluQubeAuthorizer<T>`). If you write your own `IPipelineBehavior` implementations, register them as `Singleton` too.

### `MediatorGenerator found message without any registered handler: ...`

Mediator's source generator scans ALL referenced assemblies and warns about message types that have no handler. If you have client-side requester stubs in a test helper library, they show up as messages but the corresponding handlers (client-side HTTP requesters) may not be registered. This is a warning, not an error. The handlers ARE registered by `AddMediator()` — the warning just means Mediator found the message type before finding the handler class in the scan order.

### `MediatorGenerator found multiple handlers of message type ...`

If you have both server-side handlers (from your integration module) and client-side requester stubs (from a test helper library) that handle the same message type, Mediator warns about duplicates. To fix this in test projects: ensure the server-side handler and the requester stub don't exist in the same `AddMediator()` scope. Separate server and client concerns into separate test fixtures where possible.

### Source Generation Not Reflecting Changes

If you change a `[BluQubeCommand]` or `[BluQubeQuery]` attribute and don't see updated generated code:

```shell
dotnet clean && dotnet build
```

Mediator.SourceGenerator uses incremental generation — a clean build forces full regeneration.

---

## See Also

- [AUTHORIZATION_GUIDE.md](AUTHORIZATION_GUIDE.md) — full authorization documentation
- [martinothamar/Mediator README](https://github.com/martinothamar/Mediator) — upstream documentation
