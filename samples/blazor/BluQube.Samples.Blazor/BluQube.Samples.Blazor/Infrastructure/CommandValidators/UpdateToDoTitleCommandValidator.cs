using BluQube.Samples.Blazor.Client.Infrastructure.Commands;
using BluQube.Samples.Blazor.Infrastructure.Data;
using FluentValidation;

namespace BluQube.Samples.Blazor.Infrastructure.CommandValidators;

public class UpdateToDoTitleCommandValidator : AbstractValidator<UpdateToDoTitleCommand>
{
    private readonly ITodoService _todoService;

    public UpdateToDoTitleCommandValidator(ITodoService todoService)
    {
        this._todoService = todoService;
        this.RuleFor(command => command.ToDoId)
            .NotEmpty();

        this.RuleFor(command => command.Title)
            .NotEmpty()
            .Must((command, title) =>
                this.IsTitleUniqueAsync(command.ToDoId, title));
    }

    private bool IsTitleUniqueAsync(Guid toDoId, string title)
    {
        return this._todoService.Todos.Any(
            x => x.Id != toDoId && x.Title.Equals(title, StringComparison.OrdinalIgnoreCase));
    }
}