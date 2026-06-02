# Troubleshooting

## Generated Code Missing Or Stale

Check:

1. The project references the `BluQube` package.
2. Request records use `[BluQubeCommand]` or `[BluQubeQuery]`.
3. Command records implement `ICommand` or `ICommand<TResult>`.
4. Query records implement `IQuery<TResult>`.
5. Client entry point has `[BluQubeRequester]`.
6. Server entry point has `[BluQubeResponder]`.

Then run:

```bash
dotnet clean
dotnet build
```

## Endpoint Returns 404

Likely causes:

- Server is missing `[BluQubeResponder]`.
- Server did not call `builder.Services.AddBluQube(...)` with the handler/processor assembly.
- `app.AddBluQubeApi()` is missing or runs in the wrong place.
- Handler or query processor is not in an assembly scanned by the server.
- Route path in the attribute does not match the requested URL.
- Query expected GET but omitted `Method = "GET"` and generated as POST.

## Client Requester Not Found

Likely causes:

- Client is missing `[BluQubeRequester]`.
- Client did not call `builder.Services.AddBluQubeRequesters()`.
- Named `HttpClient` registration for `"bluqube"` is missing.
- Command/query type is not visible to the client project.
 
`AddBluQubeRequesters()` registers generated HTTP handlers/processors and the client-side BluQube mediator.

## JSON Serialization Fails

Add BluQube converters on the server:

```csharp
builder.Services.Configure<JsonOptions>(options =>
{
    options.AddBluQubeJsonConverters();
});
```

Register custom converters after BluQube converters if custom behavior is required.

## Validation Does Not Run

Check:

- Validator inherits `AbstractValidator<TCommand>`.
- `AddValidatorsFromAssemblyContaining<TValidator>()` scans the assembly containing the validator.
- Handler inherits `CommandHandler<TCommand>` or `CommandHandler<TCommand, TResult>`.
- Handler passes `IEnumerable<IValidator<TCommand>>` to the base constructor.
- Code calls `ICommandRunner.Send(...)` rather than invoking `HandleInternal` directly.

## Unauthorized Becomes 500

Check:

- `builder.Services.AddBluQubeAuthorization(...)` is registered.
- `builder.Services.AddBluQube(...)` is registered on the server.
- Requests go through `ICommandRunner` or `IQueryRunner`.
- `ICommandRunner` and `IQueryRunner` are registered in DI.
- Authorizers are in the assembly passed to `AddBluQubeAuthorization`.

## Path Or Query Parameters Missing

Check:

- `{parameter}` in the route has a matching record property.
- For query-string parameters, the query uses `Method = "GET"`.
- Optional null query parameters are intentionally omitted.
- Complex non-route data should go in the body, not in the path.
