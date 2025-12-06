namespace mark.davison.common.authentication.server.Helpers;

public static class AuthTokenHelpers
{
    public static void NormalizeTokenTimes(AuthenticationProperties props)
    {
        var expiresAt = props.GetTokenValue(AuthConstants.ExpiresAt);
        if (DateTime.TryParse(expiresAt, out var parsed))
        {
            var utc = parsed.ToUniversalTime();
            props.UpdateTokenValue(AuthConstants.ExpiresAt, utc.ToString("o"));
        }
    }

    public static async Task<bool> RefreshTokenIfNeeded(
        IDateService dateService,
        IRedisTicketStore store,
        AuthenticationProperties ticket)
    {
        NormalizeTokenTimes(ticket);

        var refreshToken = ticket.GetTokenValue(AuthConstants.RefreshToken);
        if (!DateTime.TryParse(ticket.GetTokenValue(AuthConstants.ExpiresAt), out var expiresAt))
        {
            return false;
        }

        var client_id = ticket.Items.FirstOrDefault(_ => _.Key == AuthConstants.ClientId).Value;
        var client_secret = ticket.Items.FirstOrDefault(_ => _.Key == AuthConstants.ClientSecret).Value;
        var token_endpoint = ticket.Items.FirstOrDefault(_ => _.Key == AuthConstants.TokenEndpoint).Value;

        if (string.IsNullOrEmpty(client_id) ||
            string.IsNullOrEmpty(client_secret) ||
            string.IsNullOrEmpty(token_endpoint))
        {
            return false;
        }

        var expiresAtUtc = expiresAt.ToUniversalTime();

        if (dateService.Now > expiresAtUtc.AddSeconds(-60) && !string.IsNullOrEmpty(refreshToken))
        {
            if (await store.RefreshTokensAsync(refreshToken, client_id, client_secret, token_endpoint) is { } newTokens &&
                newTokens.Any())
            {
                ticket.StoreTokens(newTokens);

                return true;
            }
        }

        return false;
    }
}