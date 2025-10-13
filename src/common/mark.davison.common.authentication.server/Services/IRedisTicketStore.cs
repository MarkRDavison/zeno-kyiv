namespace mark.davison.common.authentication.server.Services;

public interface IRedisTicketStore : ITicketStore
{
    Task<IEnumerable<AuthenticationToken>> RefreshTokensAsync(
        string? refreshToken,
        string clientId,
        string clientSecret,
        string tokenEndpoint);
}
