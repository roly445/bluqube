﻿namespace BluQube.Samples.Blazor.Infrastructure.Data;

public class TodoService : ITodoService
{
    private readonly List<TodoItem> _todos = new();

    public IQueryable<TodoItem> Todos => _todos.AsQueryable();

    public TodoService()
    {
        var taskNames = new[]
        {
            "Buy groceries", "Clean the house", "Finish project report", "Call mom", "Schedule doctor appointment",
            "Prepare dinner", "Workout", "Read a book", "Respond to emails", "Plan weekend trip",
            "Pay bills", "Walk the dog", "Fix the sink", "Write blog post", "Review pull request",
            "Book flight tickets", "Organize desk", "Attend team meeting", "Update resume", "Practice guitar"
        };

        _todos = taskNames.Select((name, index) =>
        {
            var todo = new TodoItem(Guid.NewGuid(), name);
            if (index % 2 == 0)
            {
                todo.MarkAsCompleted();
            }

            return todo;
        }).ToList();
    }
    public TodoItem AddTodo(string title)
    {
        var todo = new TodoItem(Guid.NewGuid(), title);
        _todos.Add(todo);
        return todo;
    }

    public bool DeleteTodo(Guid id)
    {
        var todo = _todos.FirstOrDefault(t => t.Id == id);
        if (todo is null)
        {
            return false;
        }

        _todos.Remove(todo);
        return true;
    }
}