using BluQube.Samples.Blazor.Client.Infrastructure.Commands;
using FluentValidation;

namespace BluQube.Samples.Blazor.Infrastructure.CommandValidators;

public class DeleteTodoCommandValidator : AbstractValidator<DeleteTodoCommand>
{
    public DeleteTodoCommandValidator()
    {
        this.RuleFor(x => x.TodoId).NotEmpty();
    }
}