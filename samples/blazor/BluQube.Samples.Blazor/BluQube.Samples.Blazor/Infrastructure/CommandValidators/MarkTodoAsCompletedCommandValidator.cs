using BluQube.Samples.Blazor.Client.Infrastructure.Commands;
using FluentValidation;

namespace BluQube.Samples.Blazor.Infrastructure.CommandValidators;

public class MarkTodoAsCompletedCommandValidator : AbstractValidator<MarkTodoAsCompletedCommand>
{
    public MarkTodoAsCompletedCommandValidator()
    {
        this.RuleFor(x => x.TodoId).NotEmpty();
    }
}