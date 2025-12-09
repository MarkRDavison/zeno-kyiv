namespace mark.davison.common.server.Notifications.Matrix.Client;

public class MatrixClient : IMatrixClient
{
    private readonly MatrixNotificationSettings _settings;
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _serializerOptions;

    public MatrixClient(
        IOptions<MatrixNotificationSettings> settings,
        IHttpClientFactory httpClientFactory)
    {
        _settings = settings.Value;
        _client = httpClientFactory.CreateClient(MatrixConstants.HttpClientName);

        _serializerOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    public async Task<Response> SendMessage(string roomId, NotificationMessage message)
    {
        return await SendMessageInternal(roomId, message, false);
    }

    private async Task<Response> SendMessageInternal(string roomId, NotificationMessage message, bool retry)
    {
        if (_client.DefaultRequestHeaders.Authorization == null)
        {
            var loginResponse = await Login();
            if (!loginResponse.Success)
            {
                return new() { Errors = [.. loginResponse.Errors], Warnings = [.. loginResponse.Warnings] };
            }

            if (loginResponse.Value == null)
            {
                return new() { Errors = [$"Error deserialising login response to matrix client"] };
            }

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginResponse.Value.AccessToken);
        }

        var body = new MessageBody(new TextMessageBody(message.Message));

        var requestMessage = new HttpRequestMessage
        {
            RequestUri = new Uri(_settings.URL.TrimEnd('/') + MatrixConstants.SendMessagePath(roomId)),
            Method = HttpMethod.Post,
            Content = JsonContent.Create(body, options: _serializerOptions)
        };
        var responseMessage = await _client.SendAsync(requestMessage);

        if (!responseMessage.IsSuccessStatusCode)
        {
            if (!retry && responseMessage.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                // Authorization failed, clear it and retry to log in again.
                _client.DefaultRequestHeaders.Authorization = null;
                return await SendMessageInternal(roomId, message, true);
            }

            var responseContent = await responseMessage.Content.ReadAsStringAsync();
            return new Response<LoginResponse> { Errors = [$"Invalid request: {responseMessage.StatusCode} - {responseContent}"] };
        }

        return new();
    }

    private async Task<Response<LoginResponse>> Login()
    {
        var loginUrl = _settings.URL.TrimEnd('/') + MatrixConstants.LoginPath;
        var body = new PasswordLoginBody(_settings.USERNAME, _settings.PASSWORD, _settings.SESSIONNAME);

        var requestMessage = new HttpRequestMessage
        {
            RequestUri = new Uri(loginUrl),
            Method = HttpMethod.Post,
            Content = JsonContent.Create(new LoginBody(body), options: _serializerOptions)
        };

        var responseMessage = await _client.SendAsync(requestMessage);

        if (!responseMessage.IsSuccessStatusCode)
        {
            var responseContent = await responseMessage.Content.ReadAsStringAsync();
            return new Response<LoginResponse> { Errors = [$"Invalid request: {responseMessage.StatusCode} - {responseContent}"] };
        }

        var content = await responseMessage.Content.ReadAsStringAsync();
        var loginResponse = JsonSerializer.Deserialize<LoginResponse>(content, _serializerOptions);

        return new() { Value = loginResponse };
    }
}
