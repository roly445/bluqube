using System.Reflection;
using BluQube.Authorization;
using BluQube.Commands;
using BluQube.Queries;
using BluQube.Tests.Integration;
using BluQube.Tests.RequesterHelpers;
using Microsoft.AspNetCore.Http.Json;

var builder = WebApplication.CreateBuilder(args);

// Configure minimal services for BluQube
builder.Services.AddHttpClient();
builder.Services.AddBluQube(Assembly.GetExecutingAssembly());
builder.Services.AddBluQubeRequesters();
builder.Services.AddBluQubeAuthorization(typeof(Program).Assembly);
builder.Services.AddScoped<ICommandRunner, CommandRunner>();
builder.Services.AddScoped<IQueryRunner, QueryRunner>();
builder.Services.AddTransient<CommandResultConverter>();

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