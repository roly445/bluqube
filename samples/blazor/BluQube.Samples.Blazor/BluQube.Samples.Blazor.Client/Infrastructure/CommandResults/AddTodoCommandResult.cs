using BluQube.Commands;

namespace BluQube.Samples.Blazor.Client.Infrastructure.CommandResults;

public record AddTodoCommandResult(Guid TodoId) : ICommandResult;