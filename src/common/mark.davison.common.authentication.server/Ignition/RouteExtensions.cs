using mark.davison.common.server.abstractions.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace mark.davison.common.authentication.server.Ignition;

public static class RouteExtensions
{
    public static IEndpointRouteBuilder MapAuthenticationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/", GetRoot);
        endpoints.MapGet("/account/login", GetLogin);
        endpoints.MapGet("/account/login/{provider}", GetLoginForProvider);
        endpoints.MapGet("/account/postlogin", GetPostLogin);
        endpoints.MapGet("/account/profile", GetAccountProfile);
        endpoints.MapGet("/account/logout", GetLogout);
        endpoints.MapGet("/account/links", GetLinks);
        endpoints.MapGet("/account/link/{provider}", GetLinkForProvider);
        endpoints.MapGet("/account/link-callback", GetLinkCallback2);
        endpoints.MapGet("/account/postlink", GetPostLink);
        endpoints.MapGet("/account/linkerror", GetLinkError);
        endpoints.MapGet("/account/unlink/{provider}", GetUnlinkForProvider);
        endpoints.MapGet("/admin/secret", GetAdminSecret);
        endpoints.MapGet("/account/tenant/create", CreateTenant);

        return endpoints;
    }
    private static IResult GetRoot(HttpContext context)
    {
        return Results.Redirect("/account/login");
    }

    private static IResult GetLogin(HttpContext context)
    {
        var providersService = context.RequestServices.GetRequiredService<IAuthenticationProvidersService>();

        var builder = new StringBuilder();

        builder.AppendLine("<h2>Login</h2>");

        foreach (var p in providersService.GetConfiguredProviders())
        {
            builder.AppendLine($"<a href=\"/account/login/{p.ToLower()}\">Login with {p}</a><br/>");
        }

        return Results.Content(builder.ToString(), "text/html");
    }

    private static IResult GetLoginForProvider(string provider, HttpContext context)
    {
        var props = new AuthenticationProperties
        {
            RedirectUri = "/account/postlogin"
        };

        var providersService = context.RequestServices.GetRequiredService<IAuthenticationProvidersService>();

        foreach (var p in providersService.GetConfiguredProviders())
        {
            if (string.Equals(provider, p, StringComparison.OrdinalIgnoreCase))
            {
                return Results.Challenge(props, [p]);
            }
        }

        throw new InvalidOperationException("Invalid authentication provider");
    }

    private static async Task<IResult> GetPostLogin(HttpContext context, CancellationToken _)
    {
        var userAuthenticationService = context.RequestServices.GetRequiredService<IUserAuthenticationService>();
        var authResult = await context.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        var isAuth = context.User.Identity?.IsAuthenticated == true;

        string tokenInfo = "(no access token)";
        if (authResult.Succeeded && authResult.Properties != null)
        {
            if (authResult.Properties.GetTokenValue("id_token") is { } accessToken &&
                context.User.FindFirstValue("LoggedInProvider") is { } provider)
            {
                userAuthenticationService.SetToken(accessToken);
            }

            var expiresAtValue = authResult.Properties.GetTokenValue("expires_at");
            if (DateTime.TryParse(expiresAtValue, out var expiresAtLocal))
            {
                var expiresAtUtc = expiresAtLocal.ToUniversalTime();
                var remaining = expiresAtUtc - DateTime.UtcNow;
                tokenInfo = remaining > TimeSpan.Zero
                    ? $"{remaining.TotalSeconds} seconds remaining"
                    : "Expired";
            }
        }

        List<string> roles = [];
        var internalId = context.User.FindFirstValue("InternalUserId");
        if (internalId != null)
        {
            var userId = Guid.Parse(internalId);
            roles = [.. await userAuthenticationService.GetRolesForUserIdAsync(userId, context.RequestAborted)];
        }

        var html = $@"
            <h3>Post-login Debug</h3>
            <p>Authenticated: {isAuth}</p>
            <p>AuthenticationType: {context.User.Identity?.AuthenticationType ?? "(null)"}</p>
            <p>Access Token Remaining: {tokenInfo}</p>
            <h4>Claims</h4>
            <ul>{string.Join("", context.User.Claims.Select(c => $"<li>{c.Type}: {c.Value}</li>"))}</ul>
            <h4>Roles</h4>
            <ul>{string.Join("", roles.Select(r => $"<li>{r}</li>"))}</ul>
            <h4>Cookies</h4>
            <ul>{string.Join("", context.Request.Cookies.Select(c => $"<li>{c.Key}: {c.Value}</li>"))}</ul>
            <a href='/account/profile'>Profile</a>
        ";

        return Results.Content(html, "text/html");
    }

    private static async Task<IResult> GetAccountProfile(HttpContext context, CancellationToken _)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            return Results.Redirect("/account/login");
        }

        var internalId = context.User.FindFirstValue("InternalUserId");

        if (internalId == null)
        {
            return Results.Problem("No InternalUserId claim found");
        }

        var userAuthenticationService = context.RequestServices.GetRequiredService<IUserAuthenticationService>();

        var userId = Guid.Parse(internalId);
        var user = await userAuthenticationService.GetUserByIdAsync(userId, context.RequestAborted);

        if (user is null)
        {
            return Results.Redirect("/account/login");
        }

        // Find which provider was used for the current login
        var currentProvider = context.User.FindFirst("LoggedInProvider")?.Value ?? "(unknown)";

        var externalLogins = await userAuthenticationService.GetExternalLoginsForUserIdAsync(userId, context.RequestAborted);

        // External logins the user already has
        var linkedProviders = externalLogins.Select(p => p.Provider).ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Generate HTML list of linked providers
        var linkedList = string.Join("", linkedProviders.Select(p => $"<li>{p}<a href='/account/unlink/{p}'>Unlink</a></li>"));

        // Generate "link new provider" buttons for unlinked providers
        var allProviders = new[] { "Google", "Keycloak", "Github", "Microsoft" };
        var linkButtons = string.Join("", allProviders
            .Where(p => !linkedProviders.Contains(p))
            .Select(p => $"<a href='/account/link/{p}'>Link {p}</a><br/>"));

        var tenant = await userAuthenticationService.GetTenantById(user.TenantId, CancellationToken.None);

        var createTenantLink = string.Empty;
        if (tenant?.Id == Guid.Parse("F4380FD7-99C7-4512-A23E-2962AE65381A"))
        {
            createTenantLink = $"<a href='/account/tenant/create'>Create a personal tenant</a>";
        }

        var html = $"""
            <h2>Profile</h2>
            <p>Signed in as: {context.User.Identity?.Name}</p>
            <p>Current session provider: {currentProvider}</p>
            <p>Your internal user ID: {userId}</p>
            <p>Your tenant: {(tenant?.Name ?? "<NO TENANT>")}</p>
            {createTenantLink}
            <p>External providers linked:</p>
            <ul>{linkedList}</ul>
            <h3>Link another provider</h3>
            {linkButtons}
            <a href='/account/logout'>Logout</a>
        """;

        return Results.Content(html, "text/html");
    }

    private static async Task<IResult> GetLogout(HttpContext context, CancellationToken _)
    {
        await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        return Results.Redirect("/");
    }

    private static async Task<IResult> GetLinks(HttpContext context, [FromServices] IUserAuthenticationService userAuthenticationService)
    {
        var userIdClaim = context.User.FindFirst("InternalUserId");
        if (userIdClaim is null)
        {
            return Results.Unauthorized();
        }

        var userId = Guid.Parse(userIdClaim.Value);

        var links = await userAuthenticationService.GetExternalLoginsForUserIdAsync(userId, context.RequestAborted);

        var allProviders = new[] { "Google", "Keycloak", "Github", "Microsoft" };
        var unlinked = allProviders.Except(links.Select(_ => _.Provider)).ToList();

        var html = $"""
            <html>
            <body>
                <h2>Linked Accounts</h2>
                <ul>
                    {string.Join("", links.Select(p => $"<li>{p} ✅</li>"))}
                </ul>

                {(unlinked.Any() ? "<h3>Link another provider</h3>" : "<p>All providers linked.</p>")}

                {string.Join("", unlinked.Select(p => $"<a href=\"/account/link/{p}\">Link {p}</a><br/>"))}
            </body>
            </html>
        """;

        return Results.Content(html, "text/html");
    }

    private static IResult GetLinkForProvider(string provider, HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            return Results.Redirect("/account/login");
        }

        var props = new AuthenticationProperties
        {
            RedirectUri = "/account/link-callback",
        };
        props.Items["LinkingUserId"] = context.User.FindFirstValue("InternalUserId")!;
        props.Items["LinkingProvider"] = provider;

        return Results.Challenge(props, [provider]);
    }

    // TODO: This versus GetLinkCallback
    private static async Task<IResult> GetLinkCallback2(HttpContext context, [FromServices] IUserAuthenticationService userAuthenticationService)
    {
        if (!context.User.Identity?.IsAuthenticated ?? true)
        {
            return Results.Redirect("/account/login");
        }

        var tempAuth = await context.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        if (tempAuth?.Properties == null || !tempAuth.Properties.Items.TryGetValue("LinkingProvider", out var provider))
        {
            return Results.Problem("No linking info found. Did you start the link process?");
        }

        // Determine which provider just finished authentication
        if (string.IsNullOrEmpty(provider))
        {
            return Results.Problem("Provider not specified.");
        }

        // Authenticate using that provider's scheme
        var result = await context.AuthenticateAsync(provider);
        if (!result.Succeeded)
        {
            return Results.Redirect("/account/login");
        }

        var principal = result.Principal!;
        var linkingUserId = Guid.Parse(result.Properties.Items["LinkingUserId"]!);

        var providerSub = principal.FindFirst("sub")?.Value
                          ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? throw new InvalidDataException("No provider subject");

        var existing = await userAuthenticationService.GetExternalLoginForProviderAsync(provider, providerSub, context.RequestAborted);
        if (existing != null && existing.UserId != linkingUserId)
        {
            return Results.Content($"This {provider} account is already linked to another user.", "text/html");
        }

        if (existing == null)
        {
            await userAuthenticationService.AddExternalLoginAsync(linkingUserId, provider, providerSub, context.RequestAborted);
        }

        return Results.Redirect("/account/profile");
    }

    private static IResult GetPostLink(HttpContext context, CancellationToken _)
    {
        var html = """
            <html><body>
            <h3>✅ Account linked successfully.</h3>
            <a href="/account/links">Back to Linked Accounts</a>
            </body></html>
        """;

        return Results.Content(html, "text/html");
    }

    private static IResult GetLinkError(HttpContext context, CancellationToken _)
    {
        var message = context.Request.Query["message"].ToString();
        if (string.IsNullOrEmpty(message) && context.Items["LinkError"] is string tempMsg)
        {
            message = tempMsg;
        }

        var html = $"""
            <h3>Linking Error</h3>
            <p>{message ?? "An unknown error occurred while linking the account."}</p>
            <a href='/account/profile'>Back to profile</a>
        """;
        return Results.Content(html, "text/html");
    }

    private static async Task<IResult> GetUnlinkForProvider(string provider, HttpContext ctx)
    {
        if (ctx.User.Identity?.IsAuthenticated != true)
        {
            return Results.Redirect("/account/login");
        }

        var internalId = ctx.User.FindFirstValue("InternalUserId");
        if (internalId == null)
        {
            return Results.Problem("No InternalUserId claim found");
        }

        var userId = Guid.Parse(internalId);

        var userAuthenticationService = ctx.RequestServices.GetRequiredService<IUserAuthenticationService>();

        var userExternalLogins = await userAuthenticationService.GetExternalLoginsForUserIdAsync(userId, ctx.RequestAborted);

        if (!userExternalLogins.Any())
        {
            return Results.Problem("No linked providers found for this user");
        }

        if (userExternalLogins.Count == 1)
        {
            return Results.Content("<p>Cannot unlink the last provider. You must have at least one linked account.</p><a href='/account/profile'>Back to profile</a>", "text/html");
        }

        var externalLogin = userExternalLogins.FirstOrDefault(l => l.Provider == provider);
        if (externalLogin == null)
        {
            return Results.Content($"<p>No linked account for provider '{provider}'</p><a href='/account/profile'>Back to profile</a>", "text/html");
        }

        await userAuthenticationService.RemoveExternalLogin(userId, externalLogin.Id, ctx.RequestAborted);

        var html = $@"
            <p>Unlinked {provider} successfully.</p>
            <a href='/account/profile'>Back to profile</a>
        ";

        return Results.Content(html, "text/html");
    }

    private static async Task<IResult> GetAdminSecret(HttpContext context, [FromServices] IAuthorizationService authorizationService)
    {
        /*
            services.AddAuthorization(options =>
            {
                options.AddPolicy("RequireAdmin", policy => policy.RequireRole("Admin"));
            });

            // ...

            var result = await auth.AuthorizeAsync(user, null, "RequireAdmin");

            this is a policy equivelant
         */

        // TODO: Admin -> Constant
        if (await authorizationService.AuthorizeAsync(context.User, null, new RolesAuthorizationRequirement(["Admin"])) is { Succeeded: true })
        {
            return Results.Content("<h2>Welcome, Admin!</h2><p>You have access to this protected resource.</p>", "text/html");
        }
        return Results.Unauthorized();
    }

    private static async Task<IResult> CreateTenant(HttpContext context, CancellationToken _)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            return Results.Redirect("/account/login");
        }

        var internalId = context.User.FindFirstValue("InternalUserId");

        if (internalId == null)
        {
            return Results.Problem("No InternalUserId claim found");
        }

        var userAuthenticationService = context.RequestServices.GetRequiredService<IUserAuthenticationService>();

        var userId = Guid.Parse(internalId);
        var user = await userAuthenticationService.GetUserByIdAsync(userId, context.RequestAborted);

        if (user is null)
        {
            return Results.Redirect("/account/login");
        }

        await userAuthenticationService.CreateTenantForUser(userId, $"{user.DisplayName}'s Tenant", context.RequestAborted);

        return Results.Redirect("/account/profile");
    }
}
