using mark.davison.common.client.Repository;
using mark.davison.common.CQRS;
using mark.davison.common.test;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace mark.davison.common.client.tests.Repository;

internal class TestGetResponse : Response
{
    public string TestValue { get; set; } = string.Empty;
}

internal class TestPostResponse : Response
{
    public string TestValue { get; set; } = string.Empty;
}

[GetRequest(Path = "/get")]
internal class TestGetRequest : IQuery<TestGetRequest, TestGetResponse>, ICommand<TestGetRequest, TestGetResponse>
{
    public string Value { get; set; } = string.Empty;
    public string? NullableValue { get; set; }
}

[PostRequest(Path = "/post")]
internal class TestPostRequest : ICommand<TestPostRequest, TestPostResponse>, IQuery<TestPostRequest, TestPostResponse>
{

}
public sealed class ClientHttpRepositoryTests
{
    private readonly ClientHttpRepository _clientHttpRepository;
    private readonly MockHttpMessageHandler _httpMessageHandler;
    private readonly ILogger<ClientHttpRepository> _logger;
    private const string _remoteEndpoint = "https://localhost:8080/";

    public ClientHttpRepositoryTests()
    {
        _logger = LoggerFactory.Create(_ => { }).CreateLogger<ClientHttpRepository>();
        _httpMessageHandler = new MockHttpMessageHandler();
        _clientHttpRepository = new ClientHttpRepository(_remoteEndpoint, new HttpClient(_httpMessageHandler), _logger);
    }

    [Test]
    public async Task Get_ByRequest_RetrievesResponseViaAttributePath()
    {
        var expectedResponse = new TestGetResponse
        {
            TestValue = "abcdefghijklmnopqrstuvwxy"
        };

        _httpMessageHandler.SendAsyncFunc = async _ =>
        {

            await Assert.That(_.RequestUri?.ToString()).IsEqualTo($"{_remoteEndpoint.Trim('/')}/api/get");
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(expectedResponse))
            };
        };

        var response = await _clientHttpRepository
            .Get<TestGetRequest, TestGetResponse>(
                new TestGetRequest(),
                CancellationToken.None);

        await Assert.That(response.TestValue).IsEqualTo(expectedResponse.TestValue);
    }

    [Test]
    public async Task Get_PopulatesQueryParameter()
    {
        var expectedResponse = new TestGetResponse
        {
            TestValue = "abcdefghijklmnopqrstuvwxy"
        };
        var request = new TestGetRequest
        {
            Value = "abc"
        };

        _httpMessageHandler.SendAsyncFunc = async _ =>
        {
            await Assert.That(_.RequestUri?.ToString()).IsEqualTo($"{_remoteEndpoint.Trim('/')}/api/get?value=abc");
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(expectedResponse))
            };
        };

        var response = await _clientHttpRepository
            .Get<TestGetRequest, TestGetResponse>(
                request,
                CancellationToken.None);

        await Assert.That(response.TestValue).IsEqualTo(expectedResponse.TestValue);
    }

    [Test]
    public async Task Get_ByType_RetrievesResponseViaAttributePath()
    {
        var expectedResponse = new TestGetResponse
        {
            TestValue = "abcdefghijklmnopqrstuvwxy"
        };

        _httpMessageHandler.SendAsyncFunc = async _ =>
        {
            await Assert.That(_.RequestUri?.ToString()).IsEqualTo($"{_remoteEndpoint.Trim('/')}/api/get");
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(expectedResponse))
            };
        };

        var response = await _clientHttpRepository
            .Get<TestGetRequest, TestGetResponse>(CancellationToken.None);

        await Assert.That(response.TestValue).IsEqualTo(expectedResponse.TestValue);
    }

    [Test]
    public async Task Get_WhereNullValueReturned_CreatesNewResponse()
    {
        _httpMessageHandler.SendAsyncFunc = async _ =>
        {
            await Assert.That(_.RequestUri?.ToString()).IsEqualTo($"{_remoteEndpoint.Trim('/')}/api/get");
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("null")
            };
        };

        var response = await _clientHttpRepository
            .Get<TestGetRequest, TestGetResponse>(CancellationToken.None);

        await Assert.That(response.TestValue).IsEqualTo(new TestGetResponse().TestValue);
    }

    [Test]
    public async Task Post_ByRequest_RetrievesResponseViaAttributePath()
    {
        var expectedResponse = new TestPostResponse
        {
            TestValue = "abcdefghijklmnopqrstuvwxy"
        };

        _httpMessageHandler.SendAsyncFunc = async _ =>
        {
            await Assert.That(_.RequestUri?.ToString()).IsEqualTo($"{_remoteEndpoint.Trim('/')}/api/post");
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(expectedResponse))
            };
        };

        var response = await _clientHttpRepository
            .Post<TestPostRequest, TestPostResponse>(
                new TestPostRequest(),
                CancellationToken.None);

        await Assert.That(response.TestValue).IsEqualTo(expectedResponse.TestValue);
    }

    [Test]
    public async Task Post_ByType_RetrievesResponseViaAttributePath()
    {
        var expectedResponse = new TestPostResponse
        {
            TestValue = "abcdefghijklmnopqrstuvwxy"
        };

        _httpMessageHandler.SendAsyncFunc = async _ =>
        {
            await Assert.That(_.RequestUri?.ToString()).IsEqualTo($"{_remoteEndpoint.Trim('/')}/api/post");
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(expectedResponse))
            };
        };

        var response = await _clientHttpRepository
            .Post<TestPostRequest, TestPostResponse>(CancellationToken.None);

        await Assert.That(response.TestValue).IsEqualTo(expectedResponse.TestValue);
    }

    [Test]
    public async Task Post_WhereNullValueReturned_CreatesNewResponse()
    {
        _httpMessageHandler.SendAsyncFunc = async _ =>
        {
            await Assert.That(_.RequestUri?.ToString()).IsEqualTo($"{_remoteEndpoint.Trim('/')}/api/post");
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("null")
            };
        };

        var response = await _clientHttpRepository
            .Post<TestPostRequest, TestPostResponse>(CancellationToken.None);

        await Assert.That(response.TestValue).IsEqualTo(new TestPostResponse().TestValue);
    }

    [Test]
    public async Task Get_WhereAttributeNotPresent_Throws()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await _clientHttpRepository
                .Get<TestPostRequest, TestPostResponse>(CancellationToken.None);
        });

    }

    [Test]
    public async Task Post_WhereAttributeNotPresent_Throws()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await _clientHttpRepository
                .Post<TestGetRequest, TestGetResponse>(CancellationToken.None);
        });
    }
}
