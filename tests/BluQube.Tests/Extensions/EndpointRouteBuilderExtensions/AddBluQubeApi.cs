using System.Reflection;
using BluQube.Commands;
using BluQube.Queries;
using BluQube.Tests.ResponderHelpers;
using Moq;

namespace BluQube.Tests.Extensions.EndpointRouteBuilderExtensions;

public class AddBluQubeApi
{
    [Fact]
    public async Task AddsTheRequiredEndpointsForTheApi()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddScoped<IQuerier>(_ => Mock.Of<IQuerier>());
        builder.Services.AddScoped<ICommander>(_ => Mock.Of<ICommander>());
        WebApplication app = builder.Build();

        app.AddBluQubeApi();

        var webAppType = typeof(WebApplication);

        var endpointsProperty = webAppType.GetProperty(
            "DataSources",
            BindingFlags.NonPublic | BindingFlags.Instance);

        if (endpointsProperty == null)
        {
            return;
        }

        var endpoints = endpointsProperty.GetValue(app);

        if (endpoints is not ICollection<EndpointDataSource> endpointDataSource)
        {
            return;
        }

        await Verify(endpointDataSource.First().Endpoints);
    }
}