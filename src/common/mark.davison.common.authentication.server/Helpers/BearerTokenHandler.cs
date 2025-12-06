using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;

namespace mark.davison.common.authentication.server.Helpers;

public sealed class BearerTokenHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<BearerTokenHandler> _logger;

    public BearerTokenHandler(IHttpContextAccessor httpContextAccessor, ILogger<BearerTokenHandler> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var accessToken = _httpContextAccessor.HttpContext?.User?.FindFirst("id_token")?.Value;

        if (string.IsNullOrEmpty(accessToken) &&
            _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated is true)
        {
            accessToken = await _httpContextAccessor.HttpContext.GetTokenAsync("id_token");
        }

        if (!string.IsNullOrEmpty(accessToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
