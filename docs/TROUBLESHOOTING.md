# BluQube Troubleshooting Guide

**Can't find your problem? Ctrl+F the error message or symptom below. Issues are indexed by what you see, not what caused it.**

---

## Source Generation

### Generated files not appearing / Source generator not running

**You see:**
- No `Requesting.cs` or responder endpoints after adding `[BluQubeCommand]` / `[BluQubeQuery]`
- Project compiles but attribute seems ignored

**Cause:**
Incremental generators cache based on file content. Changing only attributes sometimes doesn't trigger regeneration.

**Fix:**
```powershell
dotnet clean BluQube.sln
dotnet build BluQube.sln
```

If still not working, check:
1. Attribute is spelled correctly: `[BluQubeCommand]`, not `[BluQubeCmd]`
2. Command/query record implements `ICommand`, `ICommand<T>`, `IQuery<T>`, etc.
3. `Path` property is set: `[BluQubeCommand(Path = "commands/add-todo")]`
4. You're in a project that has `BluQube` NuGet package referenced

---

### GET query returns 404 or empty

**You see:**
- `[BluQubeQuery]` endpoint always returns 404 or no response
- POST works fine, but GET doesn't

**Cause:**
Queries default to POST. GET endpoints require explicit opt-in via `Method = "GET"`.

**Fix:**
```csharp
[BluQubeQuery(Path = "queries/get-todos", Method = "GET")]  // Add Method = "GET"
public record GetTodosQuery : IQuery<List<TodoItem>>;
```

Queries with `Method = "GET"` serialize non-path-parameter properties to querystring; commands always use POST.

---

### Compile error: "`EqualityContract` appearing as parameter"

**You see:**
- Compiler error mentioning `EqualityContract` or unexpected property parameter in handler
- Error in generated code or type mismatch

**Cause:**
Old version of source generator (now fixed). The generator was emitting record properties that shouldn't appear.

**Fix:**
Update NuGet package:
```powershell
dotnet package update BluQube --highest-minor
dotnet clean && dotnet build
```

If already on latest version, this shouldn't occur. Report to GitHub issues if it does.

---

### Shim records causing compile errors

**You see:**
- Errors about missing types or weird generated classes that don't match your records

**Cause:**
Shim record generation bug (now fixed in latest version). Generator may have emitted malformed intermediate types.

**Fix:**
1. Update BluQube NuGet:
   ```powershell
   dotnet package update BluQube
   ```
2. Clean and rebuild:
   ```powershell
   dotnet clean && dotnet build
   ```
3. If still broken, delete the `.cs` files in `obj/` folder and rebuild

---

## Commands

### Handler not invoked; validation fails silently

**You see:**
- `CommandResult.Invalid(...)` returned, but your handler wasn't called
- Validator exists but seems to be ignored
- No error message, just returns invalid

**Cause:**
Validator is defined but not registered in DI. Validation pipeline runs before handler, so unregistered validators don't execute. Handler never runs.

**Fix:**
Ensure validators are registered in `Program.cs`:
```csharp
builder.Services.AddValidatorsFromAssemblyContaining<AddTodoCommandValidator>();
```

Must happen **before** `AddMediatR()`:
```csharp
// Program.cs
builder.Services.AddValidatorsFromAssemblyContaining<MyValidator>();  // FIRST
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<App>());
```

Verify your `MyValidator` inherits from `AbstractValidator<T>`:
```csharp
public class AddTodoCommandValidator : AbstractValidator<AddTodoCommand>
{
    public AddTodoCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(100);
    }
}
```

---

### Unauthorized exceptions returning 500

**You see:**
- `[Authorize]` attribute on handler, but HTTP 500 error instead of 401/403
- `UnauthorizedException` in logs

**Cause:**
`AddMediatorAuthorization()` not called in DI setup, so `[Authorize]` attributes aren't recognized. Exception escapes uncaught.

**Fix:**
Add to `Program.cs` before `AddMediatR()`:
```csharp
builder.Services.AddMediatorAuthorization(typeof(Program).Assembly);
builder.Services.AddAuthorizersFromAssembly(typeof(Program).Assembly);  // Also needed for custom authorizers
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<App>());
```

`CommandRunner.cs` catches `UnauthorizedException` and converts to `CommandResult.Unauthorized()`. If exception escapes, the middleware above didn't run.

Verify your handler has the attribute:
```csharp
[Authorize]  // or [Authorize("PolicyName")]
public class DeleteTodoCommandHandler : CommandHandler<DeleteTodoCommand>
{
    // ...
}
```

---

### All authorization checks fail (even valid users)

**You see:**
- Every `[Authorize]` handler returns `CommandResult.Unauthorized()`
- Valid authenticated users blocked

**Cause:**
Authorization policy doesn't exist or hasn't been configured.

**Fix:**
1. Define the policy in `Program.cs`:
   ```csharp
   builder.Services.AddAuthorization(options =>
   {
       options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
   });
   ```

2. Use policy name in handler:
   ```csharp
   [Authorize("AdminOnly")]
   public class DeleteTodoCommandHandler : CommandHandler<DeleteTodoCommand>
   {
       // ...
   }
   ```

Or just use `[Authorize]` with no policy name — it checks if user is authenticated (any user with identity).

---

## Queries

### QueryResult returns null even though query succeeded

**You see:**
- `result.Status == QueryResultStatus.Succeeded` but `result.Data` is null
- Accessing `result.Data` throws `InvalidOperationException`

**Cause:**
Query returned `Succeeded(null)`, which is semantically wrong. Null is not success; it's no data.

**Fix:**
Use the correct factory for the situation:
- `QueryResult<T>.Succeeded(data)` — query ran and found data
- `QueryResult<T>.NotFound()` — query ran but no matching entity exists (single-entity queries)
- `QueryResult<T>.Empty()` — query ran but collection is empty (collection queries)
- `QueryResult<T>.Failed()` — query threw exception or errored

```csharp
public async Task<QueryResult<TodoItem>> Handle(GetTodoQuery request, CancellationToken cancellationToken)
{
    var todo = await _service.GetByIdAsync(request.Id, cancellationToken);
    
    if (todo == null)
        return QueryResult<TodoItem>.NotFound();  // NOT Succeeded(null)
    
    return QueryResult<TodoItem>.Succeeded(todo);
}
```

Use boolean helpers to check status:
```csharp
var result = await runner.Send(new GetTodoQuery(id));
if (result.IsNotFound)
{
    // handle 404
}
else if (result.IsEmpty)
{
    // handle empty collection
}
else if (result.IsSucceeded)
{
    var data = result.Data;  // Safe to access
}
```

---

## JSON / Serialization

### CommandResult / QueryResult fails to deserialize

**You see:**
- `JsonException` or malformed response when parsing `CommandResult` or `QueryResult<T>`
- Error about missing status or invalid JSON shape

**Cause:**
`AddBluQubeJsonConverters()` not called in JSON configuration.

**Fix:**
Add to `Program.cs`:
```csharp
builder.Services.Configure<JsonOptions>(options =>
{
    options.AddBluQubeJsonConverters();  // REQUIRED
});
```

This must be in the server (`Program.cs`), not the client. Converters handle polymorphic serialization of result types.

Verify the service registration chain in Program.cs:
```csharp
builder.Services.AddValidatorsFromAssemblyContaining<MyValidator>();
builder.Services.AddMediatR(...);
builder.Services.AddMediatorAuthorization(...);
builder.Services.Configure<JsonOptions>(options => options.AddBluQubeJsonConverters());  // After MediatR setup
```

---

### JSON response returns empty/null when single-project app

**You see:**
- `CommandResult` serializes as `{"Status":0}` (no data, no error detail)
- Response looks incomplete or truncated

**Cause:**
Rare issue with JSON converter in single-project Blazor Server setup (fixed in latest version).

**Fix:**
Update BluQube NuGet:
```powershell
dotnet package update BluQube --highest-minor
```

---

### Custom JSON converters not being used

**You see:**
- You defined a custom converter for a type, but it's not being called
- Default JSON serialization used instead

**Cause:**
Custom converter registered after `AddBluQubeJsonConverters()`, which may override it.

**Fix:**
Register your custom converters **after** `AddBluQubeJsonConverters()`:
```csharp
builder.Services.Configure<JsonOptions>(options =>
{
    options.AddBluQubeJsonConverters();      // BluQube converters first
    options.JsonSerializerOptions.Converters.Add(new MyCustomConverter());  // Your converters after
});
```

---

## Authorization & Authentication

### Policy-based authorization not working

**You see:**
- Handler with `[Authorize("MyPolicy")]` always returns unauthorized
- User is authenticated but handler still rejects

**Cause:**
Policy undefined or user doesn't meet policy requirements.

**Fix:**
1. Define policy in `Program.cs`:
   ```csharp
   builder.Services.AddAuthorization(options =>
   {
       options.AddPolicy("MyPolicy", policy =>
           policy.RequireAssertion(context =>
               context.User.HasClaim("permission", "write:todos")
           )
       );
   });
   ```

2. Verify user has required claim/role:
   ```csharp
   // In your AuthenticationHandler or similar
   var claims = new List<Claim>
   {
       new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
       new Claim("permission", "write:todos"),  // Add required claim
   };
   ```

3. Ensure `AddMediatorAuthorization()` is called:
   ```csharp
   builder.Services.AddMediatorAuthorization(typeof(Program).Assembly);
   ```

---

## Testing

### Roslyn compilation test fails with `<global namespace>` errors

**You see:**
- Test using `RoslynTestHelper` or similar fails with cryptic `<global namespace>` message
- Error in generated code snapshot or compilation stage

**Cause:**
Test input code wasn't wrapped in a namespace. Roslyn compilation requires valid namespace context.

**Fix:**
Wrap test input in a namespace:
```csharp
var code = @"
namespace Test
{
    public record MyQuery : IQuery<string>;
}
";
```

Don't do:
```csharp
var code = @"public record MyQuery : IQuery<string>;";  // Wrong — no namespace
```

---

### Snapshot tests failing after intentional code change

**You see:**
- xUnit test fails with snapshot mismatch
- Expected vs Received diff shows your intentional changes

**Cause:**
Snapshot file is outdated after you modified the code.

**Fix:**
Review the diff carefully. If it's correct, accept the new snapshot:
```powershell
dotnet test --verify --autoAccept
```

Or rebuild snapshot selectively:
```powershell
dotnet test BluQube.Tests --filter "MyTest" --verify --autoAccept
```

Snapshot files live alongside tests (`.verified.txt` files). Commit them with your code change.

---

## Validation

### Validation runs but error message is generic

**You see:**
- `CommandResult.Invalid(...)` returned with `Failures` list
- Error message doesn't match what you wrote in validator rule

**Cause:**
FluentValidation property names in error messages are auto-generated from property names, not your `WithMessage()` override.

**Fix:**
Customize validation messages in your validator:
```csharp
public class AddTodoCommandValidator : AbstractValidator<AddTodoCommand>
{
    public AddTodoCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(100).WithMessage("Title cannot exceed 100 characters");
    }
}
```

Messages appear in `CommandResult.Invalid(...).Error.Value.Failures`.

---

### Validation works locally but fails in production

**You see:**
- Validator passes in dev, fails in production
- Dependency injection issue or assembly scan problem

**Cause:**
Validator assembly not being scanned in production setup. `AddValidatorsFromAssemblyContaining<T>()` uses the specified assembly; if build output differs, validators may not be found.

**Fix:**
1. Explicitly specify the assembly that contains validators:
   ```csharp
   builder.Services.AddValidatorsFromAssemblyContaining<AddTodoCommandValidator>();
   ```

2. Verify validators are in the same assembly as specified type:
   ```powershell
   dotnet build -c Release
   # Check bin/Release/net10.0/ for compiled validators
   ```

3. If using multiple validator projects, register each:
   ```csharp
   builder.Services.AddValidatorsFromAssemblyContaining<TodoValidators>();
   builder.Services.AddValidatorsFromAssemblyContaining<UserValidators>();
   ```

---

## URL Binding / REST Patterns

### Path parameters not being bound

**You see:**
- `[BluQubeCommand(Path = "commands/todo/{id}/update")]` defined
- Handler receives `id = Guid.Empty` or default value
- Path parameter in URL but handler sees null/default

**Cause:**
Path parameter name in `{...}` doesn't match record property name (case-sensitive matching) OR property type doesn't support URL encoding.

**Fix:**
Match parameter names exactly (case-insensitive):
```csharp
[BluQubeCommand(Path = "commands/todo/{id}/update")]
public record UpdateTodoCommand(Guid id, string Title) : ICommand;  // ✓ Matches {id}

// NOT this:
public record UpdateTodoCommand(Guid Id, string Title) : ICommand;  // ✗ Case mismatch
```

Path parameter binding is case-insensitive internally, but ensure property exists. Supported types:
- `Guid`, `string`, `int`, `long`, `double`, `decimal`, `bool`, `DateTime`, `DateOnly`, `TimeOnly`

---

### Query string parameters not being passed

**You see:**
- `[BluQubeQuery(Path = "queries/todos", Method = "GET")]` defined
- Query properties not appearing in querystring
- All properties in POST body instead

**Cause:**
Query not using `Method = "GET"`. Only GET queries serialize non-path properties to querystring; POST queries use body.

**Fix:**
```csharp
[BluQubeQuery(Path = "queries/todos", Method = "GET")]  // Add Method = "GET"
public record GetTodosQuery(int? PageSize, int? Page) : IQuery<List<TodoItem>>;

// Generated endpoint: GET /queries/todos?PageSize=10&Page=1
```

Path parameters are always in the URL; remaining properties depend on method:
- POST → body
- GET → querystring

---

## General Debugging

### Still stuck? Try this first:

1. **Clean and rebuild:**
   ```powershell
   dotnet clean BluQube.sln
   dotnet build BluQube.sln
   ```

2. **Check generated files:**
   - For client/WASM: Look in `obj/Release/net10.0/generated/` for `Requesting.cs`
   - For server: Look in `obj/Release/net10.0/generated/` for `Responding.cs`
   - If missing, source generator didn't run (see "Source Generation" section above)

3. **Enable detailed logging:**
   ```csharp
   builder.Logging.AddConsole().SetMinimumLevel(LogLevel.Debug);
   ```

4. **Verify DI setup:**
   Look at `samples/blazor/BluQube.Samples.Blazor/BluQube.Samples.Blazor/Program.cs` for the complete checklist:
   - `AddValidatorsFromAssemblyContaining<T>()`
   - `AddMediatR(...)`
   - `AddMediatorAuthorization(...)`
   - `AddAuthorizersFromAssembly(...)`
   - `AddScoped<ICommandRunner, CommandRunner>()`
   - `AddScoped<IQueryRunner, QueryRunner>()`
   - `Configure<JsonOptions>(options => options.AddBluQubeJsonConverters())`

### Still need help?

- Check the [README.md](../README.md) for quick start and API overview
- Search [GitHub issues](https://github.com/roly445/bluqube/issues) for similar problems
- Report a new issue with: error message, reproduction steps, and DI setup from Program.cs

**Remember:** Most issues trace back to missing DI registration, clean builds, or namespace mismatches. Start there.
