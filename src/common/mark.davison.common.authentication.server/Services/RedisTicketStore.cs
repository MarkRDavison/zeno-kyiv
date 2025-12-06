using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace mark.davison.common.authentication.server.Services;

public class RedisTicketStore : IRedisTicketStore
{
    private readonly IDistributedCache _cache;
    private readonly TimeSpan _expiration = TimeSpan.FromHours(1);
    private readonly HttpClient _httpClient;

    public RedisTicketStore(IDistributedCache cache)
    {
        _cache = cache;
        _httpClient = new HttpClient();
    }

    public async Task<string> StoreAsync(AuthenticationTicket ticket)
    {
        var key = Guid.NewGuid().ToString();
        await RenewAsync(key, ticket);
        return key;
    }

    public async Task RenewAsync(string key, AuthenticationTicket ticket)
    {

        byte[] bytes = SerializeToBytes(ticket);
        await _cache.SetAsync(key, bytes, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = _expiration
        });
    }

    public async Task<AuthenticationTicket?> RetrieveAsync(string key)
    {
        var bytes = await _cache.GetAsync(key);

        if (bytes is null)
        {
            return null;
        }

        return DeserializeFromBytes(bytes);
    }
    private static byte[] SerializeToBytes(AuthenticationTicket source)
    {
        return TicketSerializer.Default.Serialize(source);
    }

    public async Task RemoveAsync(string key)
    {
        await _cache.RemoveAsync(key);
    }

    private static AuthenticationTicket? DeserializeFromBytes(byte[] source)
    {
        return source == null ? null : TicketSerializer.Default.Deserialize(source);
    }

    public async Task<IEnumerable<AuthenticationToken>> RefreshTokensAsync(
        string? refreshToken,
        string clientId,
        string clientSecret,
        string tokenEndpoint)
    {
        if (string.IsNullOrEmpty(refreshToken))
        {
            throw new InvalidOperationException("No refresh token available.");
        }

        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            [AuthConstants.GrantType] = AuthConstants.RefreshToken,
            [AuthConstants.RefreshToken] = refreshToken,
            [AuthConstants.ClientId] = clientId,
            [AuthConstants.ClientSecret] = clientSecret
        });

        var response = await _httpClient.PostAsync(tokenEndpoint, content);

        if (!response.IsSuccessStatusCode)
        {
            return [];
        }

        var responseContent = await response.Content.ReadAsStringAsync();
        // TODO: Named properties not JsonElement
        var payload = JsonDocument.Parse(responseContent).RootElement;

        var newAccessToken = payload.GetProperty(AuthConstants.IdToken).GetString();
        var newRefreshToken = payload.TryGetProperty(AuthConstants.RefreshToken, out var rt) ? rt.GetString() : refreshToken;
        var expiresIn = payload.TryGetProperty(AuthConstants.ExpiresIn, out var exp) ? exp.GetInt32() : 3600;
        var expiresAt = DateTime.UtcNow.AddSeconds(expiresIn);

        return
        [
            new AuthenticationToken { Name = AuthConstants.IdToken, Value = newAccessToken! },
            new AuthenticationToken { Name = AuthConstants.RefreshToken, Value = newRefreshToken! },
            new AuthenticationToken { Name = AuthConstants.ExpiresAt, Value = expiresAt.ToString("o") }
        ];
    }
}
