using BluQube.Attributes;
using BluQube.Commands;

namespace BluQube.Tests.Integration;

[BluQubeCommand(Path = "test/item/{id}")]
public record DeleteItemCommand(Guid Id) : ICommand;

[BluQubeCommand(Path = "test/item/{id}/update")]
public record UpdateItemCommand(Guid Id, string NewTitle, string NewDescription) : ICommand;

[BluQubeCommand(Path = "test/slug/{slug}")]
public record GetBySlugCommand(string Slug) : ICommand;

[BluQubeCommand(Path = "test/tenant/{tenantId}/todo/{todoId}")]
public record DeleteTenantTodoCommand(Guid TenantId, Guid TodoId) : ICommand;