using BluQube.Authorization;
using BluQube.Samples.Blazor.Client.Infrastructure.Commands;

namespace BluQube.Samples.Blazor.Infrastructure.CommandAuthorizers;

public class AddTodoCommandAuthorizer(IHttpContextAccessor httpContextAccessor)
    : AuthenticatedUserAuthorizer<AddTodoCommand>(httpContextAccessor);

public class DeleteTodoCommandAuthorizer(IHttpContextAccessor httpContextAccessor)
    : AuthenticatedUserAuthorizer<DeleteTodoCommand>(httpContextAccessor);

public class MarkTodoAsCompletedCommandAuthorizer(IHttpContextAccessor httpContextAccessor)
    : AuthenticatedUserAuthorizer<MarkTodoAsCompletedCommand>(httpContextAccessor);

public class UpdateToDoTitleCommandAuthorizer(IHttpContextAccessor httpContextAccessor)
    : AuthenticatedUserAuthorizer<UpdateToDoTitleCommand>(httpContextAccessor);

public abstract class AuthenticatedUserAuthorizer<TCommand>(IHttpContextAccessor httpContextAccessor)
    : IBluQubeAuthorizer<TCommand>
{
    public Task<AuthorizationResult> Authorize(TCommand request, CancellationToken cancellationToken)
    {
        var context = httpContextAccessor.HttpContext;
        return Task.FromResult(
            context?.User.Identity?.IsAuthenticated == true
                ? AuthorizationResult.Succeed()
                : AuthorizationResult.Fail("User must be authenticated to modify todos."));
    }
}