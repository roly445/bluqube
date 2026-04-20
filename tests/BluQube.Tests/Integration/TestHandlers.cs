using BluQube.Commands;
using FluentValidation;

namespace BluQube.Tests.Integration;

public class DeleteItemCommandHandler(
    IEnumerable<IValidator<DeleteItemCommand>> validators,
    ILogger<DeleteItemCommandHandler> logger)
    : CommandHandler<DeleteItemCommand>(validators, logger)
{
    protected override Task<CommandResult> HandleInternal(DeleteItemCommand request, CancellationToken cancellationToken)
    {
        // Just verify we received the ID correctly from the route
        if (request.Id == Guid.Empty)
        {
            return Task.FromResult(CommandResult.Failed(new BluQubeErrorData("EmptyId", "ID cannot be empty")));
        }

        return Task.FromResult(CommandResult.Succeeded());
    }
}

public class UpdateItemCommandHandler(
    IEnumerable<IValidator<UpdateItemCommand>> validators,
    ILogger<UpdateItemCommandHandler> logger)
    : CommandHandler<UpdateItemCommand>(validators, logger)
{
    protected override Task<CommandResult> HandleInternal(UpdateItemCommand request, CancellationToken cancellationToken)
    {
        // Verify all three parameters came through correctly
        if (request.Id == Guid.Empty || string.IsNullOrEmpty(request.NewTitle) || string.IsNullOrEmpty(request.NewDescription))
        {
            return Task.FromResult(CommandResult.Failed(new BluQubeErrorData("InvalidParameters", "All parameters are required")));
        }

        return Task.FromResult(CommandResult.Succeeded());
    }
}

public class GetBySlugCommandHandler(
    IEnumerable<IValidator<GetBySlugCommand>> validators,
    ILogger<GetBySlugCommandHandler> logger)
    : CommandHandler<GetBySlugCommand>(validators, logger)
{
    protected override Task<CommandResult> HandleInternal(GetBySlugCommand request, CancellationToken cancellationToken)
    {
        // Verify the slug came through unescaped
        if (string.IsNullOrEmpty(request.Slug))
        {
            return Task.FromResult(CommandResult.Failed(new BluQubeErrorData("EmptySlug", "Slug cannot be empty")));
        }

        return Task.FromResult(CommandResult.Succeeded());
    }
}

public class DeleteTenantTodoCommandHandler(
    IEnumerable<IValidator<DeleteTenantTodoCommand>> validators,
    ILogger<DeleteTenantTodoCommandHandler> logger)
    : CommandHandler<DeleteTenantTodoCommand>(validators, logger)
{
    protected override Task<CommandResult> HandleInternal(DeleteTenantTodoCommand request, CancellationToken cancellationToken)
    {
        // Verify both IDs came through in the correct order
        if (request.TenantId == Guid.Empty || request.TodoId == Guid.Empty)
        {
            return Task.FromResult(CommandResult.Failed(new BluQubeErrorData("EmptyIds", "Both IDs are required")));
        }

        return Task.FromResult(CommandResult.Succeeded());
    }
}
