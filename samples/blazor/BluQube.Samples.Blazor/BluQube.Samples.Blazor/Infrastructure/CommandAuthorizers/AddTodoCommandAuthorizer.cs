using BluQube.Samples.Blazor.Client.Infrastructure.Commands;
using BluQube.Samples.Blazor.Infrastructure.AuthorizationRequirements;
using MediatR.Behaviors.Authorization;

namespace BluQube.Samples.Blazor.Infrastructure.CommandAuthorizers;

public class AddTodoCommandAuthorizer : AbstractRequestAuthorizer<AddTodoCommand>
{
    public override void BuildPolicy(AddTodoCommand request)
    {
        this.UseRequirement(new MustBeAuthenticatedRequirement());
    }
}