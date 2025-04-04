namespace BluQube.Samples.Blazor.Infrastructure.Data;

public class TodoItem(Guid id, string title)
{
    public Guid Id { get; private set; } = id;
    public string Title { get; private set; } = title;
    public bool IsCompleted { get; private set; }

    public void MarkAsCompleted()
    {
        this.IsCompleted = true;
    }

    public void UpdateTitle(string title)
    {
        this.Title = title;
    }
}