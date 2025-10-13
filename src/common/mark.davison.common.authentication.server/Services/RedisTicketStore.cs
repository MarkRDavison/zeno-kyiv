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
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = refreshToken,
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret
        });

        var response = await _httpClient.PostAsync(tokenEndpoint, content);

        if (!response.IsSuccessStatusCode)
        {
            // TODO: THROW;
            return [];
        }

        // TODO: Better
        var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

        // TODO: Constants
        var newAccessToken = payload.GetProperty("access_token").GetString();
        var newRefreshToken = payload.TryGetProperty("refresh_token", out var rt) ? rt.GetString() : refreshToken;
        var expiresIn = payload.TryGetProperty("expires_in", out var exp) ? exp.GetInt32() : 3600;
        var expiresAt = DateTime.UtcNow.AddSeconds(expiresIn);

        return new List<AuthenticationToken>
        {
            new AuthenticationToken { Name = "access_token", Value = newAccessToken },
            new AuthenticationToken { Name = "refresh_token", Value = newRefreshToken },
            new AuthenticationToken { Name = "expires_at", Value = expiresAt.ToString("o") }
        };
    }
}
