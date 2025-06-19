using BluQube.Samples.Blazor.Client.Infrastructure.Commands;
using BluQube.Samples.Blazor.Infrastructure.Data;
using FluentValidation;

namespace BluQube.Samples.Blazor.Infrastructure.CommandValidators;

public class AddTodoCommandValidator : AbstractValidator<AddTodoCommand>
{
    public AddTodoCommandValidator(ITodoService todoService)
    {
        this.RuleFor(x => x.Title)
            .NotEmpty()
            .Must((s) => !todoService.Todos.Any(t => t.Title.Equals(s, StringComparison.OrdinalIgnoreCase)));
    }
}