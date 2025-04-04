using System.Net;
using BluQube.Tests.TestHelpers.Fakes;
using BluQube.Tests.TestHelpers.Stubs;
using Moq;
using Moq.Contrib.HttpClient;
using Moq.Protected;

namespace BluQube.Tests.Queries.GenericQueryProcessorTests;

public class Handle
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly FakeLogger<StubQueryProcessor> _fakeLogger;
    private readonly StubQueryProcessor _handler;
    private readonly Mock<HttpMessageHandler> _handlerMock;

    public Handle()
    {
        this._fakeLogger = new FakeLogger<StubQueryProcessor>();
        this._handlerMock = new Mock<HttpMessageHandler>();
        this._httpClientFactory = this._handlerMock.CreateClientFactory();
        this._handler = new StubQueryProcessor(
            this._httpClientFactory,
            new StubQueryResultConverter(), this._fakeLogger);
    }

    [Fact]
    public async Task ReturnFailedCommandResultWhenStatusCodeIsNotSuccessful()
    {
        // Arrange
        var query = new StubQuery("query-data");

        this._handlerMock.SetupRequest(HttpMethod.Post, "https://bluqube.local/api/query")
            .ReturnsResponse(HttpStatusCode.BadRequest);

        Mock.Get(this._httpClientFactory).Setup(x => x.CreateClient("bluqube"))
            .Returns(() =>
            {
                var client = this._handlerMock.CreateClient();
                client.BaseAddress = new Uri("https://bluqube.local/api/");
                return client;
            });

        // Act
        var result = await this._handler.Handle(query, CancellationToken.None);

        // Assert
        await Verify(result);
    }

    [Fact]
    public async Task LogsCriticalWhenStatusCodeIsNotSuccessful()
    {
        // Arrange
        var query = new StubQuery("query-data");

        this._handlerMock.SetupRequest(HttpMethod.Post, "https://bluqube.local/api/query")
            .ReturnsResponse(HttpStatusCode.BadRequest);

        Mock.Get(this._httpClientFactory).Setup(x => x.CreateClient("bluqube"))
            .Returns(() =>
            {
                var client = this._handlerMock.CreateClient();
                client.BaseAddress = new Uri("https://bluqube.local/api/");
                return client;
            });

        // Act
        await this._handler.Handle(query, CancellationToken.None);

        // Assert
        await Verify(this._fakeLogger.LogMessages);
    }

    [Fact]
    public async Task UsesTheCorrectUrlWhenCalled()
    {
        // Arrange
        var query = new StubQuery("query-data");

        this._handlerMock.SetupRequest(HttpMethod.Post, "https://bluqube.local/api/query")
            .ReturnsResponse(HttpStatusCode.BadRequest);

        Mock.Get(this._httpClientFactory).Setup(x => x.CreateClient("bluqube"))
            .Returns(() =>
            {
                var client = this._handlerMock.CreateClient();
                client.BaseAddress = new Uri("https://bluqube.local/api/");
                return client;
            });

        // Act
        await this._handler.Handle(query, CancellationToken.None);

        // Assert
        this._handlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Post &&
                req.RequestUri == new Uri("https://bluqube.local/api/query")),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task UsesTheCorrectMethodWhenCalled()
    {
        // Arrange
        var query = new StubQuery("query-data");

        this._handlerMock.SetupRequest(HttpMethod.Post, "https://bluqube.local/api/query")
            .ReturnsResponse(HttpStatusCode.BadRequest);

        Mock.Get(this._httpClientFactory).Setup(x => x.CreateClient("bluqube"))
            .Returns(() =>
            {
                var client = this._handlerMock.CreateClient();
                client.BaseAddress = new Uri("https://bluqube.local/api/");
                return client;
            });

        // Act
        await this._handler.Handle(query, CancellationToken.None);

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
        var query = new StubQuery("query-data");

        this._handlerMock.SetupRequest(HttpMethod.Post, "https://bluqube.local/api/query")
            .ReturnsResponse(HttpStatusCode.OK, string.Empty);

        Mock.Get(this._httpClientFactory).Setup(x => x.CreateClient("bluqube"))
            .Returns(() =>
            {
                var client = this._handlerMock.CreateClient();
                client.BaseAddress = new Uri("https://bluqube.local/api/");
                return client;
            });

        // Act
        var result = await this._handler.Handle(query, CancellationToken.None);

        // Assert
        await Verify(result);
    }

    [Fact]
    public async Task ReturnsValidCommandResultWhenResponseIsValid()
    {
        // Arrange
        var query = new StubQuery("query-data");

        this._handlerMock.SetupRequest(HttpMethod.Post, "https://bluqube.local/api/query")
            .ReturnsResponse(HttpStatusCode.OK, "{\"Status\": 2, \"Data\": {\"Result\": \"result\"}}");

        Mock.Get(this._httpClientFactory).Setup(x => x.CreateClient("bluqube"))
            .Returns(() =>
            {
                var client = this._handlerMock.CreateClient();
                client.BaseAddress = new Uri("https://bluqube.local/api/");
                return client;
            });

        // Act
        var result = await this._handler.Handle(query, CancellationToken.None);

        // Assert
        await Verify(result);
    }
}