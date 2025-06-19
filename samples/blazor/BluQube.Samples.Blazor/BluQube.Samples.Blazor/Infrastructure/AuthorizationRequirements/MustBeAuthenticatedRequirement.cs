using MediatR.Behaviors.Authorization;

namespace BluQube.Samples.Blazor.Infrastructure.AuthorizationRequirements;

public class MustBeAuthenticatedRequirement : IAuthorizationRequirement
{
    private sealed class MustBeAuthenticatedRequirementHandler(IHttpContextAccessor httpContextAccessor)
        : IAuthorizationHandler<MustBeAuthenticatedRequirement>
    {
        public Task<AuthorizationResult> Handle(MustBeAuthenticatedRequirement request, CancellationToken cancellationToken = default)
        {
            var context = httpContextAccessor.HttpContext;
            return Task.FromResult(
                context!.User.Identity!.IsAuthenticated ? AuthorizationResult.Succeed() : AuthorizationResult.Fail());
        }
    }
}