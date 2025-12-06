using System.Text.Json;

namespace mark.davison.common.client.web.Authentication;

public class AuthenticationService : IAuthenticationService
{
    private readonly HttpClient _client;

    public AuthenticationService(IHttpClientFactory httpClientFactory)
    {
        _client = httpClientFactory.CreateClient("API");
    }

    public void AuthenticateUser(ClaimsPrincipal user)
    {
        UserChanged?.Invoke(this, user);
    }

    public static (bool, ClaimsPrincipal) FromJson(string json)
    {
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        bool isAuthenticated = root.GetProperty("isAuthenticated").GetBoolean();

        if (!isAuthenticated)
        {
            return (false, new ClaimsPrincipal(new ClaimsIdentity()));
        }

        var claims = new List<Claim>();

        // Name / Email / UserId
        if (root.TryGetProperty("name", out var nameProp))
        {
            claims.Add(new Claim(ClaimTypes.Name, nameProp.GetString() ?? ""));
        }
        if (root.TryGetProperty("email", out var emailProp))
        {
            claims.Add(new Claim(ClaimTypes.Email, emailProp.GetString() ?? ""));
        }
        if (root.TryGetProperty("userId", out var userIdProp))
        {
            claims.Add(new Claim(ClaimTypes.NameIdentifier, userIdProp.GetString() ?? ""));
        }
        if (root.TryGetProperty("loggedInProvider", out var providerProp))
        {
            claims.Add(new Claim("provider", providerProp.GetString() ?? ""));
        }

        // Roles / claims
        if (root.TryGetProperty("claims", out var claimsProp) && claimsProp.ValueKind == JsonValueKind.Array)
        {
            foreach (var c in claimsProp.EnumerateArray())
            {
                var role = c.GetString();
                if (!string.IsNullOrEmpty(role))
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }
            }
        }

        var identity = new ClaimsIdentity(claims, "KYIV");
        return (true, new ClaimsPrincipal(identity));
    }

    public async Task EvaluateAuthentication()
    {
        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri("https://localhost:40000/account/user")
        };

        var response = await _client.SendAsync(request);

        if (response.IsSuccessStatusCode)
        {
            // WOOP!
            var context = await response.Content.ReadAsStringAsync();

            if (!string.IsNullOrEmpty(context))
            {
                var (authorized, principal) = FromJson(context);

                if (authorized)
                {
                    Console.WriteLine("USER AUTHENTICATED");
                    AuthenticateUser(principal);
                }
            }
        }
    }

    public event EventHandler<ClaimsPrincipal> UserChanged = delegate { };
}
