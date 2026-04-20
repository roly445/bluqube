using BluQube.Commands;
using BluQube.Queries;
using BluQube.Tests.Integration;
using MediatR.Behaviors.Authorization.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http.Json;

var builder = WebApplication.CreateBuilder(args);

// Configure minimal services for BluQube
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<EntryPoint>());
builder.Services.AddMediatorAuthorization(typeof(EntryPoint).Assembly);
builder.Services.AddScoped<ICommandRunner, CommandRunner>();
builder.Services.AddScoped<IQueryRunner, QueryRunner>();

// Configure JSON converters
builder.Services.Configure<JsonOptions>(options =>
{
    options.AddBluQubeJsonConverters();
});

var app = builder.Build();

// Map BluQube endpoints
app.AddBluQubeApi();

await app.RunAsync();

// Make Program accessible to WebApplicationFactory
public partial class Program
{
    protected Program()
    {
    }
}