using mark.davison.common.authentication.server.Models;
using System.Net.Http.Json;
using System.Web;

namespace mark.davison.common.authentication.server.Services;

public sealed class RemoteUserAuthenticationService : IUserAuthenticationService
{
    private readonly HttpClient _httpClient;

    public RemoteUserAuthenticationService(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient(nameof(RemoteUserAuthenticationService));
    }

    public void SetToken(string token)
    {
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
    }

    public async Task AddExternalLoginAsync(Guid userId, string provider, string providerSub, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri($"/api/user/{HttpUtility.UrlEncode(userId.ToString())}/external-logins", UriKind.Relative),
            Content = JsonContent.Create(new CreateExternalLoginDto(provider, providerSub))
        };

        await _httpClient.SendAsync(request, cancellationToken);
    }

    public async Task CreateUserWithRolesAsync(UserDto user, IEnumerable<string> roles, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri($"/api/user/create", UriKind.Relative),
            Content = JsonContent.Create(new CreateUserDto(user, [.. roles]))
        };

        await _httpClient.SendAsync(request, cancellationToken);
    }

    public async Task<ExternalLoginDto?> GetExternalLoginForProviderAsync(string provider, string providerSub, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri($"/api/external-login?provider={HttpUtility.UrlEncode(provider)}&providerSub={HttpUtility.UrlEncode(providerSub)}", UriKind.Relative)
        };

        var response = await _httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<ExternalLoginDto>(cancellationToken);
    }

    public async Task<IReadOnlyList<ExternalLoginDto>> GetExternalLoginsForUserIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri($"/api/external-logins?userId={HttpUtility.UrlEncode(userId.ToString())}", UriKind.Relative)
        };

        var response = await _httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return [];
        }

        return await response.Content.ReadFromJsonAsync<List<ExternalLoginDto>>(cancellationToken) ?? [];
    }

    public async Task<IReadOnlyList<string>> GetRolesForUserIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri($"/api/user-roles?userId={HttpUtility.UrlEncode(userId.ToString())}", UriKind.Relative)
        };

        var response = await _httpClient.SendAsync(request, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            var userRoles = await response.Content.ReadFromJsonAsync<List<UserRoleDto>>(cancellationToken);

            if (userRoles is not null)
            {
                return [.. userRoles.Select(_ => _.Name)];
            }
        }

        return [];
    }

    public async Task<UserDto?> GetUserByEmailAsync(string email, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri($"/api/user?email={HttpUtility.UrlEncode(email)}", UriKind.Relative)
        };

        var response = await _httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<UserDto>(cancellationToken);
    }

    public async Task<UserDto?> GetUserByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri($"/api/user?userId={HttpUtility.UrlEncode(id.ToString())}", UriKind.Relative)
        };

        var response = await _httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<UserDto>(cancellationToken);
    }

    public async Task RemoveExternalLogin(Guid userId, Guid externalLoginId, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Delete,
            RequestUri = new Uri($"/api/user/{HttpUtility.UrlEncode(userId.ToString())}/external-logins/{HttpUtility.UrlEncode(externalLoginId.ToString())}", UriKind.Relative)
        };

        var response = await _httpClient.SendAsync(request, cancellationToken);
    }

    public async Task<TenantDto?> GetTenantById(Guid tenantId, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri($"/api/tenant?tenantId={HttpUtility.UrlEncode(tenantId.ToString())}", UriKind.Relative)
        };

        var response = await _httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<TenantDto>(cancellationToken);
    }

    public async Task CreateTenantForUser(Guid userId, string name, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri($"/api/tenant/create", UriKind.Relative),
            Content = JsonContent.Create(new CreateTenantDto(userId, name))
        };

        await _httpClient.SendAsync(request, cancellationToken);
    }
}
