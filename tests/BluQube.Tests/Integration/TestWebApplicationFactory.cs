using BluQube.Attributes;
using BluQube.Commands;
using BluQube.Queries;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BluQube.Tests.Integration;

[BluQubeResponder]
public class EntryPoint
{
    // Marker class for BluQube source generation
}

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Services are configured in Program.cs
        });
        
        builder.Configure(app =>
        {
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                // Manually register test endpoints since source generator doesn't work for same-assembly scenario
                RegisterCommandEndpoints(endpoints);
                RegisterQueryEndpoints(endpoints);
            });
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        // Set content root to actual test project directory
        builder.UseContentRoot(Directory.GetCurrentDirectory());
        return base.CreateHost(builder);
    }

    private static void RegisterCommandEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("test/item/{id}", async (
            [FromServices] ICommandRunner runner,
            [FromRoute] Guid id,
            [FromBody] DeleteItemCommand command) =>
        {
            var result = await runner.Send(command with { Id = id });
            return Results.Json(result);
        });

        endpoints.MapPost("test/item/{id}/update", async (
            [FromServices] ICommandRunner runner,
            [FromRoute] Guid id,
            [FromBody] UpdateItemCommand command) =>
        {
            var result = await runner.Send(command with { Id = id });
            return Results.Json(result);
        });

        endpoints.MapPost("test/slug/{slug}", async (
            [FromServices] ICommandRunner runner,
            [FromRoute] string slug,
            [FromBody] GetBySlugCommand command) =>
        {
            var result = await runner.Send(command with { Slug = slug });
            return Results.Json(result);
        });

        endpoints.MapPost("test/tenant/{tenantId}/todo/{todoId}", async (
            [FromServices] ICommandRunner runner,
            [FromRoute] Guid tenantId,
            [FromRoute] Guid todoId,
            [FromBody] DeleteTenantTodoCommand command) =>
        {
            var result = await runner.Send(command with { TenantId = tenantId, TodoId = todoId });
            return Results.Json(result);
        });
    }

    private static void RegisterQueryEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("test/item/{id}", async (
            [FromServices] IQueryRunner runner,
            [FromRoute] Guid id,
            [FromQuery] string? Filter) =>
        {
            var query = new GetItemQuery(id, Filter);
            var result = await runner.Send(query);
            return Results.Json(result);
        });

        endpoints.MapGet("test/todos", async (
            [FromServices] IQueryRunner runner,
            [FromQuery] string? Status) =>
        {
            var query = new ListTodosQuery(Status);
            var result = await runner.Send(query);
            return Results.Json(result);
        });

        endpoints.MapPost("test/search/{category}", async (
            [FromServices] IQueryRunner runner,
            [FromRoute] string category,
            ComplexFilter filter) =>
        {
            var query = new SearchQuery(category, filter);
            var result = await runner.Send(query);
            return Results.Json(result);
        });
    }
}
