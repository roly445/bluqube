# Project Context

- **Owner:** Andrew Davis
- **Project:** BluQube — a lightweight CQRS-style framework for Blazor enabling "write once, run on Server or WASM" development
- **Stack:** C# / .NET 8,9,10 · Blazor Server + WASM · MediatR · FluentValidation · Roslyn (IIncrementalGenerator) · xUnit + Verify · NuGet
- **Repo:** C:\Code\bluqube
- **Created:** 2026-04-07

## Key Files I Own

- `src/BluQube/` — main framework library
- `src/BluQube.SourceGeneration/Requesting.cs` — client-side IIncrementalGenerator
- `src/BluQube.SourceGeneration/Responding.cs` — server-side IIncrementalGenerator
- `src/BluQube.SourceGeneration/*InputDefinitionProcessor.cs` — Roslyn syntax tree parsers
- `src/BluQube.SourceGeneration/*OutputDefinitionProcessor.cs` — C# code emitters
- `samples/blazor/BluQube.Samples.Blazor/` — full Blazor Server + WASM sample

## Generator Architecture

- `Requesting.cs` — scans for `[BluQubeRequester]`, finds all commands/queries, emits HTTP requesters + DI extensions
- `Responding.cs` — scans for `[BluQubeResponder]`, finds all handlers, emits endpoint mappings + JSON config
- Attribute changes in `src/BluQube/Attributes/` require corresponding generator updates
- Always do a clean build after generator changes (`dotnet build --no-incremental`)

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->
