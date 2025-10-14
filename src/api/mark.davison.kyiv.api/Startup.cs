using mark.davison.common.abstractions.Services;
using mark.davison.common.authentication.server.Ignition;
using mark.davison.common.authentication.server.Services;
using mark.davison.common.server.Ignition;
using mark.davison.common.server.Models;
using mark.davison.common.Services;
using mark.davison.kyiv.api.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Security.Claims;

namespace mark.davison.kyiv.api;

public sealed class Startup
{
    public IConfiguration Configuration { get; }

    public AppSettings AppSettings { get; } = new();

    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        IConfigurationSection section = Configuration.GetSection(AppSettings.SECTION);
        services.Configure<AppSettings>(section);
        section.Bind(AppSettings);

        services
            .AddCors()
            .AddLogging()
            .AddSingleton<IDateService>(_ => new DateService(DateService.DateMode.Utc))
            .AddScoped<IUserService, UserService>()
            .AddScoped<IUserRoleService, UserRoleService<KyivDbContext>>()
            .AddSingleton<IDataSeeder, KyivDataSeeder>()
            .AddRedis(AppSettings.REDIS, "zeno_kyiv_dev_")
            .AddServerAuthentication<KyivDbContext>(AppSettings.AUTHENTICATION)
            .AddHealthChecks();

        services
            .AddHttpClient()
            .AddHttpContextAccessor()
            .AddDbContextFactory<KyivDbContext>(_ =>
            {
                _.UseSqlite($"Data Source=Kyiv.db");
                _.EnableSensitiveDataLogging();
                _.EnableDetailedErrors();
            })
            .AddHostedService<ApplicationHealthStateHostedService>();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseCors(builder =>
            builder
                .SetIsOriginAllowed(_ => true)
                .AllowAnyMethod()
                .AllowCredentials()
                .AllowAnyHeader());

        app
            .UseHttpsRedirection()
            .UseRouting()
            .UseAuthentication()
            .UseAuthorization()
            .UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/", (HttpContext ctx) => Results.Redirect("/account/login"));

                endpoints.MapGet("/account/login", () =>
                {
                    var html = """
                        <h2>Login</h2>
                        <a href="/account/login/google">Login with Google</a><br/>
                        <a href="/account/login/github">Login with GitHub</a><br/>
                        <a href="/account/login/microsoft">Login with Microsoft</a><br/>
                        <a href="/account/login/keycloak">Login with Keycloak</a><br/>
                    """;
                    return Results.Content(html, "text/html");
                });

                endpoints.MapGet("/account/login/{provider}", (string provider, HttpContext ctx) =>
                {
                    var props = new AuthenticationProperties { RedirectUri = "/account/postlogin" };
                    return Results.Challenge(props, new[] { provider });
                });

                endpoints.MapGet("/account/postlogin", async (HttpContext ctx, KyivDbContext db) =>
                {
                    var authResult = await ctx.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    var isAuth = ctx.User.Identity?.IsAuthenticated == true;

                    string tokenInfo = "(no access token)";
                    if (authResult.Succeeded && authResult.Properties != null)
                    {
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

                    List<string> roles = new();
                    var internalId = ctx.User.FindFirstValue("InternalUserId");
                    if (internalId != null)
                    {
                        var userId = Guid.Parse(internalId);
                        roles = await db.UserRoles
                            .Include(ur => ur.Role)
                            .Where(ur => ur.UserId == userId)
                            .Select(ur => ur.Role.Name)
                            .ToListAsync();
                    }

                    var html = $@"
                        <h3>Post-login Debug</h3>
                        <p>Authenticated: {isAuth}</p>
                        <p>AuthenticationType: {ctx.User.Identity?.AuthenticationType ?? "(null)"}</p>
                        <p>Access Token Remaining: {tokenInfo}</p>
                        <h4>Claims</h4>
                        <ul>{string.Join("", ctx.User.Claims.Select(c => $"<li>{c.Type}: {c.Value}</li>"))}</ul>
                        <h4>Roles</h4>
                        <ul>{string.Join("", roles.Select(r => $"<li>{r}</li>"))}</ul>
                        <h4>Cookies</h4>
                        <ul>{string.Join("", ctx.Request.Cookies.Select(c => $"<li>{c.Key}: {c.Value}</li>"))}</ul>
                        <a href='/account/profile'>Profile</a>
                    ";
                    return Results.Content(html, "text/html");
                });

                endpoints.MapGet("/account/profile", async (HttpContext ctx, [FromServices] IUserService users) =>
                {
                    if (ctx.User.Identity?.IsAuthenticated != true)
                        return Results.Redirect("/account/login");

                    var internalId = ctx.User.FindFirstValue("InternalUserId");
                    if (internalId == null) return Results.Problem("No InternalUserId claim found");

                    var userId = Guid.Parse(internalId);
                    var user = await users.GetUserByIdAsync(userId);

                    // Find which provider was used for the current login
                    var currentProvider = ctx.User.FindFirst("LoggedInProvider")?.Value ?? "(unknown)";

                    // External logins the user already has
                    var linkedProviders = user.ExternalLogins.Select(p => p.Provider).ToHashSet(StringComparer.OrdinalIgnoreCase);

                    // Generate HTML list of linked providers
                    var linkedList = string.Join("", linkedProviders.Select(p => $"<li>{p}<a href='/account/unlink/{p}'>Unlink</a></li>"));

                    // Generate "link new provider" buttons for unlinked providers
                    var allProviders = new[] { "google", "keycloak", "gitHub", "microsoft" };
                    var linkButtons = string.Join("", allProviders
                        .Where(p => !linkedProviders.Contains(p))
                        .Select(p => $"<a href='/account/link/{p}'>Link {p}</a><br/>"));

                    var html = $"""
            <h2>Profile</h2>
            <p>Signed in as: {ctx.User.Identity?.Name}</p>
            <p>Current session provider: {currentProvider}</p>
            <p>Your internal user ID: {userId}</p>
            <p>External providers linked:</p>
            <ul>{linkedList}</ul>
            <h3>Link another provider</h3>
            {linkButtons}
            <a href='/account/logout'>Logout</a>
        """;

                    return Results.Content(html, "text/html");
                });


                endpoints.MapGet("/account/logout", async (HttpContext ctx) =>
                {
                    await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    return Results.Redirect("/");
                });

                endpoints.MapGet("/account/links", async (HttpContext http, KyivDbContext db) =>
                {
                    var userIdClaim = http.User.FindFirst("InternalUserId");
                    if (userIdClaim is null)
                        return Results.Unauthorized();

                    var userId = Guid.Parse(userIdClaim.Value);

                    var links = await db.ExternalLogins
                        .Where(l => l.UserId == userId)
                        .Select(l => l.Provider)
                        .ToListAsync();

                    var allProviders = new[] { "google", "keycloak" };
                    var unlinked = allProviders.Except(links).ToList();

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
                });

                endpoints.MapGet("/account/link/callback/{provider}", async (string provider, HttpContext ctx, [FromServices] KyivDbContext db) =>
                {
                    var result = await ctx.AuthenticateAsync(provider);
                    if (!result.Succeeded || result.Principal == null)
                        return Results.Problem("External login failed");

                    var externalId = result.Principal.FindFirst("sub")?.Value
                                     ?? result.Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                    if (externalId == null)
                        return Results.Problem("External ID not found");

                    var linkingUserId = Guid.Parse(ctx.AuthenticateAsync().Result.Principal.FindFirstValue("InternalUserId")!);

                    // Check if this external account is already linked to another user
                    var existingLink = await db.ExternalLogins.FirstOrDefaultAsync(l => l.Provider == provider && l.ProviderSubject == externalId);
                    if (existingLink != null && existingLink.UserId != linkingUserId)
                        return Results.Content($"<p>Cannot link {provider}: account is already linked to another user.</p><a href='/account/profile'>Back</a>", "text/html");

                    // Link the external account to the current user if not already linked
                    if (existingLink == null)
                    {
                        db.ExternalLogins.Add(new ExternalLogin
                        {
                            Id = Guid.NewGuid(),
                            Provider = provider,
                            ProviderSubject = externalId,
                            UserId = linkingUserId,
                            Created = DateTime.UtcNow,
                            LastModified = DateTime.UtcNow
                        });
                        await db.SaveChangesAsync();
                    }

                    return Results.Content($"<p>{provider} successfully linked!</p><a href='/account/profile'>Back to Profile</a>", "text/html");
                });

                endpoints.MapGet("/account/link/callback", async (HttpContext ctx, [FromServices] KyivDbContext db, [FromServices] IUserRoleService roleService) =>
                {
                    var linkingUserId = ctx.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme)
                                           .Result.Principal?.FindFirstValue("InternalUserId");

                    if (linkingUserId == null)
                        return Results.Redirect("/account/login");

                    var userId = Guid.Parse(linkingUserId);

                    var result = await ctx.AuthenticateAsync(); // Authenticate from the external provider
                    if (!result.Succeeded || result.Principal == null)
                        return Results.Content("<p>External login failed</p><a href='/account/profile'>Back to profile</a>", "text/html");

                    // TODO: Is ".AuthScheme" the right one?
                    var provider = result.Properties?.Items[".AuthScheme"] ?? result.Ticket?.AuthenticationScheme ?? "unknown";
                    var providerSub = result.Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                                      ?? result.Principal.FindFirst("sub")?.Value
                                      ?? throw new InvalidDataException("No provider subject");

                    // Check if already linked to a different user
                    var existingLogin = await db.ExternalLogins.FirstOrDefaultAsync(l => l.Provider == provider && l.ProviderSubject == providerSub);
                    if (existingLogin != null && existingLogin.UserId != userId)
                    {
                        return Results.Content("<p>This external account is already linked to another user.</p><a href='/account/profile'>Back to profile</a>", "text/html");
                    }

                    // Add external login if not already linked
                    if (existingLogin == null)
                    {
                        db.ExternalLogins.Add(new ExternalLogin
                        {
                            Id = Guid.NewGuid(),
                            UserId = userId,
                            Provider = provider,
                            ProviderSubject = providerSub,
                            Created = DateTime.UtcNow,
                            LastModified = DateTime.UtcNow
                        });
                        await db.SaveChangesAsync();
                    }

                    return Results.Content("<p>Provider linked successfully!</p><a href='/account/profile'>Back to profile</a>", "text/html");
                });

                endpoints.MapGet("/account/link/{provider}", (string provider, HttpContext ctx) =>
                {
                    if (ctx.User.Identity?.IsAuthenticated != true)
                        return Results.Redirect("/account/login");

                    var props = new AuthenticationProperties
                    {
                        RedirectUri = "/account/link-callback",
                    };
                    props.Items["LinkingUserId"] = ctx.User.FindFirstValue("InternalUserId")!;
                    props.Items["LinkingProvider"] = provider;

                    return Results.Challenge(props, new[] { provider });
                });

                endpoints.MapGet("/account/link-callback", async (HttpContext ctx, KyivDbContext db) =>
                {
                    if (!ctx.User.Identity?.IsAuthenticated ?? true)
                    {
                        return Results.Redirect("/account/login");
                    }

                    var tempAuth = await ctx.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);

                    if (tempAuth?.Properties == null || !tempAuth.Properties.Items.TryGetValue("LinkingProvider", out var provider))
                        return Results.Problem("No linking info found. Did you start the link process?");

                    // Determine which provider just finished authentication
                    if (string.IsNullOrEmpty(provider))
                        return Results.Problem("Provider not specified.");

                    // Authenticate using that provider's scheme
                    var result = await ctx.AuthenticateAsync(provider);
                    if (!result.Succeeded)
                        return Results.Redirect("/account/login");

                    var principal = result.Principal!;
                    var linkingUserId = Guid.Parse(result.Properties.Items["LinkingUserId"]!);

                    var providerSub = principal.FindFirst("sub")?.Value
                                      ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                                      ?? throw new InvalidDataException("No provider subject");

                    var existing = await db.ExternalLogins.FirstOrDefaultAsync(l => l.Provider == provider && l.ProviderSubject == providerSub);
                    if (existing != null && existing.UserId != linkingUserId)
                        return Results.Content($"This {provider} account is already linked to another user.", "text/html");

                    if (existing == null)
                    {
                        db.ExternalLogins.Add(new ExternalLogin
                        {
                            Id = Guid.NewGuid(),
                            Provider = provider,
                            ProviderSubject = providerSub,
                            UserId = linkingUserId,
                            Created = DateTime.UtcNow,
                            LastModified = DateTime.UtcNow
                        });
                        await db.SaveChangesAsync();
                    }

                    return Results.Redirect("/account/profile");
                });

                endpoints.MapGet("/account/postlink", () =>
                {
                    var html = """
            <html><body>
            <h3>✅ Account linked successfully.</h3>
            <a href="/account/links">Back to Linked Accounts</a>
            </body></html>
        """;
                    return Results.Content(html, "text/html");
                });

                endpoints.MapGet("/account/linkerror", (HttpContext ctx) =>
                {
                    ctx.Response.ContentType = "text/html";

                    var message = ctx.Request.Query["message"].ToString();
                    if (string.IsNullOrEmpty(message) && ctx.Items["LinkError"] is string tempMsg)
                        message = tempMsg;

                    var html = $"""
            <h3>Linking Error</h3>
            <p>{message ?? "An unknown error occurred while linking the account."}</p>
            <a href='/account/profile'>Back to profile</a>
        """;
                    return Results.Content(html, "text/html");
                });

                endpoints.MapGet("/account/unlink/{provider}", async (string provider, HttpContext ctx, [FromServices] KyivDbContext db, [FromServices] IUserService users) =>
                {
                    if (ctx.User.Identity?.IsAuthenticated != true)
                        return Results.Redirect("/account/login");

                    var internalId = ctx.User.FindFirstValue("InternalUserId");
                    if (internalId == null) return Results.Problem("No InternalUserId claim found");

                    var userId = Guid.Parse(internalId);

                    var userExternalLogins = await db.ExternalLogins
                        .Where(l => l.UserId == userId)
                        .ToListAsync();

                    if (!userExternalLogins.Any())
                        return Results.Problem("No linked providers found for this user");

                    if (userExternalLogins.Count == 1)
                        return Results.Content("<p>Cannot unlink the last provider. You must have at least one linked account.</p><a href='/account/profile'>Back to profile</a>", "text/html");

                    var externalLogin = userExternalLogins.FirstOrDefault(l => l.Provider == provider);
                    if (externalLogin == null)
                        return Results.Content($"<p>No linked account for provider '{provider}'</p><a href='/account/profile'>Back to profile</a>", "text/html");

                    db.ExternalLogins.Remove(externalLogin);
                    await db.SaveChangesAsync();

                    var html = $@"
            <p>Unlinked {provider} successfully.</p>
            <a href='/account/profile'>Back to profile</a>
        ";
                    return Results.Content(html, "text/html");
                });

                // TODO: Admin -> Constant
                endpoints.MapGet("/admin/secret", async (HttpContext ctx, IAuthorizationService auth) =>
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

                    if (await auth.AuthorizeAsync(ctx.User, null, new RolesAuthorizationRequirement(["Admin"])) is { Succeeded: true })
                    {
                        return Results.Content("<h2>Welcome, Admin!</h2><p>You have access to this protected resource.</p>", "text/html");
                    }
                    return Results.Unauthorized();
                });
            });


    }

}
