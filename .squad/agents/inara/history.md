# Project Context

- **Owner:** Andrew Davis
- **Project:** BluQube — a lightweight CQRS-style framework for Blazor enabling "write once, run on Server or WASM" development
- **Stack:** C# / .NET 8,9,10 · Blazor Server + WASM · MediatR · FluentValidation · Roslyn (IIncrementalGenerator) · xUnit + Verify · NuGet
- **Repo:** C:\Code\bluqube
- **Created:** 2026-04-07

## Documentation State (as of 2026-04-07)

- `README.md` — exists but barebones; missing: advanced usage, auth guide, validation guide, WASM deployment
- No getting started guide beyond README
- No contributor guide (CONTRIBUTING.md missing)
- No troubleshooting guide (source generation failures especially)
- No API reference documentation
- Sample app at `samples/blazor/BluQube.Samples.Blazor/` is production-quality — good source of truth for docs

## Key Concepts to Document

- Command/Query definition with attributes (`[BluQubeCommand]`, `[BluQubeQuery]`)
- Handler/Processor implementation patterns
- Validation with FluentValidation
- Authorization with MediatR.Behaviors.Authorization + `[Authorize]` attribute
- Source generation — what it produces, how to trigger, how to debug failures
- Blazor WASM vs Server setup differences
- DI registration (`AddBluQubeRequesters()`, `AddBluQubeApi()`)
- Result types (`CommandResult`, `QueryResult<T>`) and error handling

## Learnings

- URL binding pattern is complete: Properties in `{paramName}` templates are automatically inferred and bound to route/querystring. Case-insensitive matching.
- Commands always POST; queries default POST but support GET with `Method = "GET"` attribute.
- Generated client uses `Uri.EscapeDataString()` for URL escaping—fully AOT-safe, no reflection.
- Generated server uses internal shim records with `[FromRoute]` / `[FromQuery]` attributes to keep user types clean.
- Documentation should focus on developer intent: "I want a REST-style delete endpoint" → shows the pattern with minimal explanation.
- README style prefers brief examples with inline comments over long prose paragraphs.

