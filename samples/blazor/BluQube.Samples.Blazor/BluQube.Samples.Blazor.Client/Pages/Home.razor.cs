﻿using BluQube.Commands;
using BluQube.Constants;
using BluQube.Queries;
using BluQube.Samples.Blazor.Client.Infrastructure.Commands;
using BluQube.Samples.Blazor.Client.Infrastructure.Queries;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace BluQube.Samples.Blazor.Client.Pages;

public partial class Home
{
    private AddToDoModel _addToDoModel = new();
    private List<ToDoListItem> _toDoList = new();
    
    [Inject]
    private ICommander Commander { get; set; } = default!;
    
    [Inject]
    private IQuerier Querier { get; set; } = default!;

    protected override async Task OnParametersSetAsync()
    {
        var result = await Querier.Send(new GetAllToDoItemsQuery());
        if (result.Status == QueryResultStatus.Succeeded)
        {
            this._toDoList = result.Data.ToDoItems.Select(x => new ToDoListItem
            {
                Id = x.Id, Title = x.Title, IsCompleted = x.IsCompleted
            }).ToList();
        }
    }

    private class AddToDoModel
    {
        public string Title { get; set; } = string.Empty;
    }

    private async Task AddToDo(EditContext arg)
    {
        var result = await Commander.Send(new AddTodoCommand(this._addToDoModel.Title));
        if (result.Status == CommandResultStatus.Succeeded)
        {
            this._toDoList.Add(new ToDoListItem { Id = result.Data.TodoId, Title = this._addToDoModel.Title });
            this._addToDoModel = new();
        }
    }
    
    public class ToDoListItem
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        
        public bool EditModeEnabled { get; set; }
        public bool IsCompleted { get; set; }
        public string? EditedTitle { get; set; }
    }

    private async Task DeleteToDo(ToDoListItem item)
    {
        var result = await Commander.Send(new DeleteTodoCommand(item.Id));
        if (result.Status == CommandResultStatus.Succeeded)
        {
            this._toDoList.Remove(item);
        }
    }

    private async Task SaveToDo(ToDoListItem item)
    {
        var result = await Commander.Send(new UpdateToDoTitleCommand(item.Id, item.EditedTitle!));
        if (result.Status == CommandResultStatus.Succeeded)
        {
            item.Title = item.EditedTitle!;
            item.EditModeEnabled = false;
        }
    }

    private void EnableEditMode(ToDoListItem item)
    {
        item.EditModeEnabled = true;
        item.EditedTitle = item.Title;
    }

    private void CancelEditMode(ToDoListItem item)
    {
        item.EditModeEnabled = false;
    }
}