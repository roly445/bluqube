﻿using System.Net;
using BluQube.Commands;
using BluQube.Tests.RequesterHelpers.Stubs;
using BluQube.Tests.TestHelpers.Fakes;
using Moq;
using Moq.Contrib.HttpClient;
using Moq.Protected;

namespace BluQube.Tests.Commands.GenericCommandHandlerTests;

public class Handle
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly FakeLogger<StubCommandGenericHandler> _fakeLogger;
    private readonly StubCommandGenericHandler _handler;
    private readonly Mock<HttpMessageHandler> _handlerMock;

    public Handle()
    {
        this._fakeLogger = new FakeLogger<StubCommandGenericHandler>();
        this._handlerMock = new Mock<HttpMessageHandler>();
        this._httpClientFactory = this._handlerMock.CreateClientFactory();
        this._handler = new StubCommandGenericHandler(
            this._httpClientFactory,
            new CommandResultConverter(), this._fakeLogger);
    }

    [Fact]
    public async Task ReturnFailedCommandResultWhenStatusCodeIsNotSuccessful()
    {
        // Arrange
        var command = new StubCommand("data");

        this._handlerMock.SetupRequest(HttpMethod.Post, "https://bluqube.local/api/command/stub")
            .ReturnsResponse(HttpStatusCode.BadRequest);

        Mock.Get(this._httpClientFactory).Setup(x => x.CreateClient("bluqube"))
            .Returns(() =>
            {
                var client = this._handlerMock.CreateClient();
                client.BaseAddress = new Uri("https://bluqube.local/api/");
                return client;
            });

        // Act
        var result = await this._handler.Handle(command, CancellationToken.None);

        // Assert
        await Verify(result);
    }

    [Fact]
    public async Task LogsCriticalWhenStatusCodeIsNotSuccessful()
    {
        // Arrange
        var command = new StubCommand("data");

        this._handlerMock.SetupRequest(HttpMethod.Post, "https://bluqube.local/api/command/stub")
            .ReturnsResponse(HttpStatusCode.BadRequest);

        Mock.Get(this._httpClientFactory).Setup(x => x.CreateClient("bluqube"))
            .Returns(() =>
            {
                var client = this._handlerMock.CreateClient();
                client.BaseAddress = new Uri("https://bluqube.local/api/");
                return client;
            });

        // Act
        await this._handler.Handle(command, CancellationToken.None);

        // Assert
        await Verify(this._fakeLogger.LogMessages);
    }

    [Fact]
    public async Task UsesTheCorrectUrlWhenCalled()
    {
        // Arrange
        var command = new StubCommand("data");

        this._handlerMock.SetupRequest(HttpMethod.Post, "https://bluqube.local/api/command/stub")
            .ReturnsResponse(HttpStatusCode.BadRequest);

        Mock.Get(this._httpClientFactory).Setup(x => x.CreateClient("bluqube"))
            .Returns(() =>
            {
                var client = this._handlerMock.CreateClient();
                client.BaseAddress = new Uri("https://bluqube.local/api/");
                return client;
            });

        // Act
        await this._handler.Handle(command, CancellationToken.None);

        // Assert
        this._handlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Post &&
                req.RequestUri == new Uri("https://bluqube.local/api/command/stub")),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task UsesTheCorrectMethodWhenCalled()
    {
        // Arrange
        var command = new StubCommand("data");

        this._handlerMock.SetupRequest(HttpMethod.Post, "https://bluqube.local/api/command/stub")
            .ReturnsResponse(HttpStatusCode.BadRequest);

        Mock.Get(this._httpClientFactory).Setup(x => x.CreateClient("bluqube"))
            .Returns(() =>
            {
                var client = this._handlerMock.CreateClient();
                client.BaseAddress = new Uri("https://bluqube.local/api/");
                return client;
            });

        // Act
        await this._handler.Handle(command, CancellationToken.None);

        // Assert
        this._handlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Post),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task ReturnsFailedCommandResultWhenResponseIsNotValid()
    {
        // Arrange
        var command = new StubCommand("data");

        this._handlerMock.SetupRequest(HttpMethod.Post, "https://bluqube.local/api/command/stub")
            .ReturnsResponse(HttpStatusCode.OK, string.Empty);

        Mock.Get(this._httpClientFactory).Setup(x => x.CreateClient("bluqube"))
            .Returns(() =>
            {
                var client = this._handlerMock.CreateClient();
                client.BaseAddress = new Uri("https://bluqube.local/api/");
                return client;
            });

        // Act
        var result = await this._handler.Handle(command, CancellationToken.None);

        // Assert
        await Verify(result);
    }

    [Fact]
    public async Task ReturnsValidCommandResultWhenResponseIsValid()
    {
        // Arrange
        var command = new StubCommand("data");

        this._handlerMock.SetupRequest(HttpMethod.Post, "https://bluqube.local/api/command/stub")
            .ReturnsResponse(HttpStatusCode.OK, "{}");

        Mock.Get(this._httpClientFactory).Setup(x => x.CreateClient("bluqube"))
            .Returns(() =>
            {
                var client = this._handlerMock.CreateClient();
                client.BaseAddress = new Uri("https://bluqube.local/api/");
                return client;
            });

        // Act
        var result = await this._handler.Handle(command, CancellationToken.None);

        // Assert
        await Verify(result);
    }
}