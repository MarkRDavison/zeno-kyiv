namespace mark.davison.common.server.tests.Notifications.Matrix.Client;

public sealed class MatrixClientTests
{
    private readonly MatrixClient _matrixClient;
    private readonly Mock<IHttpClientFactory> _factory;
    private readonly HttpClient _httpClient;
    private readonly MatrixNotificationSettings _settings;
    private readonly TestHttpMessageHandler _httpMessageHandler;
    private readonly JsonSerializerOptions _serializerOptions;

    public MatrixClientTests()
    {
        _httpMessageHandler = new();
        _httpClient = new(_httpMessageHandler);
        _factory = new(MockBehavior.Strict);

        _serializerOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        _factory
            .Setup(_ => _.CreateClient(It.IsAny<string>()))
            .Returns(_httpClient);

        _settings = new()
        {
            URL = "https://matrix-client.matrix.org",
            ROOMID = "!theroomid:matrix.org",
            USERNAME = "@theusername:matrix.org",
            PASSWORD = "thepassword"
        };

        _matrixClient = new(Options.Create(_settings), _factory.Object);
    }

    [Test]
    public async Task SendMessage_WhenAuthNotPresent_LogsIn()
    {
        bool loginHit = false;
        bool sendMessageHit = false;
        var accessToken = "validtoken";
        _httpMessageHandler.Callback = async (HttpRequestMessage requestMessage) =>
        {
            var responseMessage = new HttpResponseMessage();

            if (requestMessage.RequestUri!.ToString().Contains(MatrixConstants.LoginPath))
            {
                loginHit = true;

                responseMessage.Content = JsonContent.Create(new LoginResponse
                {
                    AccessToken = accessToken
                }, options: _serializerOptions);
                responseMessage.StatusCode = HttpStatusCode.OK;
            }
            else
            {
                sendMessageHit = true;
                await Assert.That(requestMessage.Headers.Authorization?.Parameter).IsNotNull();
                await Assert.That(requestMessage.Headers.Authorization?.Parameter).IsEqualTo(accessToken);
            }

            return responseMessage;
        };

        var message = "Hello world from C#";
        var response = await _matrixClient.SendMessage(_settings.ROOMID, new() { Message = message });

        await Assert.That(response.Success).IsTrue();
        await Assert.That(loginHit).IsTrue();
        await Assert.That(sendMessageHit).IsTrue();
    }

    [Test]
    public async Task SendMessage_WhenAuthPresent_DoesNotLogIn()
    {
        var loginHitCount = 0;
        var sendMessageHitCount = 0;
        var accessToken = "validtoken";
        _httpMessageHandler.Callback = async (HttpRequestMessage requestMessage) =>
        {
            var responseMessage = new HttpResponseMessage();

            if (requestMessage.RequestUri!.ToString().Contains(MatrixConstants.LoginPath))
            {
                loginHitCount++;

                responseMessage.Content = JsonContent.Create(new LoginResponse
                {
                    AccessToken = accessToken
                }, options: _serializerOptions);
                responseMessage.StatusCode = HttpStatusCode.OK;
            }
            else
            {
                sendMessageHitCount++;
                await Assert.That(requestMessage.Headers.Authorization?.Parameter).IsNotNull();
                await Assert.That(requestMessage.Headers.Authorization?.Parameter).IsEqualTo(accessToken);
            }

            return responseMessage;
        };

        var message = "Hello world from C#";
        var response1 = await _matrixClient.SendMessage(_settings.ROOMID, new() { Message = message });
        await Assert.That(response1.Success).IsTrue();

        var response2 = await _matrixClient.SendMessage(_settings.ROOMID, new() { Message = message });
        await Assert.That(response2.Success).IsTrue();

        await Assert.That(loginHitCount).IsEqualTo(1);
        await Assert.That(sendMessageHitCount).IsEqualTo(2);
    }

    [Test]
    public async Task SendMessage_WhenAuthPresentButInvalid_AttemptsToLogIn()
    {
        var validSendMessageHit = false;
        var loginHitCount = 0;
        var sendMessageHitCount = 0;
        var accessToken = "validtoken";
        var invalidAccessToken = "invalidtoken";

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", invalidAccessToken);

        _httpMessageHandler.Callback = async (HttpRequestMessage requestMessage) =>
        {
            var responseMessage = new HttpResponseMessage();

            if (requestMessage.RequestUri!.ToString().Contains(MatrixConstants.LoginPath))
            {
                loginHitCount++;
                responseMessage.Content = JsonContent.Create(new LoginResponse
                {
                    AccessToken = accessToken
                }, options: _serializerOptions);
                responseMessage.StatusCode = HttpStatusCode.OK;
            }
            else
            {
                sendMessageHitCount++;

                if (requestMessage.Headers.Authorization?.Parameter == invalidAccessToken)
                {
                    responseMessage.StatusCode = HttpStatusCode.Unauthorized;
                }
                else
                {
                    await Assert.That(requestMessage.Headers.Authorization?.Parameter).IsNotNull();
                    await Assert.That(requestMessage.Headers.Authorization?.Parameter).IsEqualTo(accessToken);
                    validSendMessageHit = true;
                }
            }

            return responseMessage;
        };

        var message = "Hello world from C#";
        var response = await _matrixClient.SendMessage(_settings.ROOMID, new() { Message = message });
        await Assert.That(response.Success).IsTrue();

        await Assert.That(loginHitCount).IsEqualTo(1);
        await Assert.That(sendMessageHitCount).IsEqualTo(2);
        await Assert.That(validSendMessageHit).IsTrue();
    }
}