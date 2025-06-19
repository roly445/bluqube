namespace BluQube.Samples.Blazor.Infrastructure.Data;

public interface ITodoService
{
    IQueryable<TodoItem> Todos { get; }

    TodoItem AddTodo(string title);

    bool DeleteTodo(Guid id);
}