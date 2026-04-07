# Project Context

- **Owner:** Andrew Davis
- **Project:** BluQube — a lightweight CQRS-style framework for Blazor enabling "write once, run on Server or WASM" development
- **Stack:** C# / .NET 8,9,10 · Blazor Server + WASM · MediatR · FluentValidation · Roslyn (IIncrementalGenerator) · xUnit + Verify · NuGet
- **Repo:** C:\Code\bluqube
- **Created:** 2026-04-07

## Test Infrastructure

- Framework: xUnit + Verify (snapshot testing)
- Mocking: Moq + Moq.Contrib.HttpClient
- Coverage: CoverLet (Cobertura format) — excludes BluQube.SourceGeneration projects
- Helpers: BluQube.Tests.RequesterHelpers, BluQube.Tests.ResponderHelpers
- Snapshots: `*.verified.txt` files alongside test files

## Known Coverage Gaps (as of 2026-04-07)

- **Source generation testing: CRITICAL** — only ~50 lines, `RequestingGeneratorTests.cs`
  - Need: edge cases, malformed attributes, nested types, generic inheritance, missing handlers
- **Integration tests: MISSING** — no client→server→handler round-trip tests
- **Authorization generation: MINIMAL** — parser works, generated code path not validated
- **HTTP error scenarios: THIN** — GenericQueryProcessor only tests success path
- **JSON serialization edge cases: THIN** — basic converter tests only

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->
