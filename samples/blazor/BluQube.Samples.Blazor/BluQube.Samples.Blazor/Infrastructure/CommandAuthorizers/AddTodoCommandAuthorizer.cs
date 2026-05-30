using BluQube.Authorization;
using BluQube.Samples.Blazor.Client.Infrastructure.Commands;

namespace BluQube.Samples.Blazor.Infrastructure.CommandAuthorizers;

public class AddTodoCommandAuthorizer(IHttpContextAccessor httpContextAccessor) : IBluQubeAuthorizer<AddTodoCommand>
{
    public Task<AuthorizationResult> Authorize(AddTodoCommand request, CancellationToken cancellationToken)
    {
        var context = httpContextAccessor.HttpContext;
        return Task.FromResult(
            context?.User.Identity?.IsAuthenticated == true
                ? AuthorizationResult.Succeed()
                : AuthorizationResult.Fail("User must be authenticated to add a todo."));
    }
}