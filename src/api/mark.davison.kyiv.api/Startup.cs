using mark.davison.kyiv.api.Services;
using mark.davison.kyiv.shared.models.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text.Json;

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
            .AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            })
            .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme);

        services
            .AddScoped<IUserService, UserService>()
            .AddScoped<IUserRoleService, UserRoleService>();

        services.AddMemoryCache();

        foreach (var p in AppSettings.AUTHENTICATION.Providers)
        {
            if (string.Equals(p.Type, "oidc", StringComparison.OrdinalIgnoreCase))
            {
                services.AddAuthentication()
                    .AddOpenIdConnect(p.Name, options =>
                    {
                        options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                        options.Authority = p.Authority;
                        options.ClientId = p.ClientId;
                        options.ClientSecret = p.ClientSecret;
                        options.ResponseType = OpenIdConnectResponseType.Code;
                        options.CallbackPath = $"/signin-{p.Name}";
                        options.SaveTokens = true;
                        options.UsePkce = true;
                        foreach (var s in p.Scope ?? Array.Empty<string>()) options.Scope.Add(s);
                        options.TokenValidationParameters = new TokenValidationParameters
                        {
                            NameClaimType = "name",
                            RoleClaimType = ClaimTypes.Role
                        };
                        options.Events = CreateOidcEvents(p.Name);
                    });
            }
            else if (string.Equals(p.Type, "oauth", StringComparison.OrdinalIgnoreCase))
            {
                services.AddAuthentication()
                    .AddOAuth(p.Name, options =>
                    {
                        options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                        options.ClientId = p.ClientId;
                        options.ClientSecret = p.ClientSecret;
                        options.CallbackPath = $"/signin-{p.Name}";
                        options.AuthorizationEndpoint = p.AuthorizationEndpoint;
                        options.TokenEndpoint = p.TokenEndpoint;
                        options.UserInformationEndpoint = p.UserInformationEndpoint;
                        options.SaveTokens = true;
                        options.Scope.Clear();
                        foreach (var s in p.Scope ?? Array.Empty<string>()) options.Scope.Add(s);

                        options.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "id");
                        options.ClaimActions.MapJsonKey(ClaimTypes.Name, "name");
                        options.ClaimActions.MapJsonKey(ClaimTypes.Email, "email");

                        options.Events = CreateOAuthEvents(p.Name);
                    });
            }
            else
            {
                throw new InvalidOperationException($"Unknown provider type: {p.Type}");
            }
        }

        services.AddAuthorization().AddHealthChecks();
        services.AddHttpClient().AddHttpContextAccessor();
        services.AddDbContextFactory<KyivDbContext>(_ =>
        {
            _.UseSqlite($"Data Source=Kyiv.db");
            _.EnableSensitiveDataLogging();
            _.EnableDetailedErrors();
        });

        services.AddHostedService<ApplicationHealthStateHostedService>();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseCors(builder =>
            builder
                .SetIsOriginAllowed(_ => true)
                .AllowAnyMethod()
                .AllowCredentials()
                .AllowAnyHeader());

        app.UseHttpsRedirection();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
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

            endpoints.MapGet("/account/postlogin", (HttpContext ctx) =>
            {
                var isAuth = ctx.User.Identity?.IsAuthenticated == true;
                var html = $@"
                    <h3>Post-login Debug</h3>
                    <p>Authenticated: {isAuth}</p>
                    <p>AuthenticationType: {ctx.User.Identity?.AuthenticationType ?? "(null)"}</p>
                    <h4>Claims</h4>
                    <ul>{string.Join("", ctx.User.Claims.Select(c => $"<li>{c.Type}: {c.Value}</li>"))}</ul>
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
                var list = string.Join("", user.ExternalLogins.Select(p => $"<li>{p.Provider}</li>"));
                var linkedProviders = user.ExternalLogins.Select(l => l.Provider).ToHashSet();
                var allProviders = AppSettings.AUTHENTICATION.Providers.Select(p => p.Name);

                var linkButtons = string.Join("", allProviders
                    .Where(p => !linkedProviders.Contains(p))
                    .Select(p => $"<li><a href='/account/link/{p}'>Link {p}</a></li>"));

                var html = $"""
    <h2>Profile</h2>
    <p>Signed in as {ctx.User.Identity?.Name}</p>
    <p>Your internal user ID: {userId}</p>
    <p>External providers linked:</p>
    <ul>{string.Join("", user.ExternalLogins.Select(p => $"<li>{p.Provider}</li>"))}</ul>
    <p>Link new provider:</p>
    <ul>{linkButtons}</ul>
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

            endpoints.MapGet("/account/link/{provider}", async (string provider, HttpContext ctx) =>
            {
                if (ctx.User.Identity?.IsAuthenticated != true)
                    return Results.Redirect("/account/login");

                var props = new AuthenticationProperties
                {
                    RedirectUri = "/account/link/callback/" + provider,
                    Items = { ["LinkingUserId"] = ctx.User.FindFirstValue("InternalUserId")! }
                };

                return Results.Challenge(props, new[] { provider });
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
        });


    }

    private OpenIdConnectEvents CreateOidcEvents(string providerName)
    {
        return new OpenIdConnectEvents
        {
            OnTokenValidated = async context =>
            {
                var http = context.HttpContext;
                var db = http.RequestServices.GetRequiredService<KyivDbContext>();
                var roleService = http.RequestServices.GetRequiredService<IUserRoleService>();
                var provider = context.Scheme.Name;

                var principal = context.Principal!;
                var providerSub = principal.FindFirst("sub")?.Value
                                  ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                                  ?? throw new InvalidDataException("No provider subject");

                var email = principal.FindFirst(ClaimTypes.Email)?.Value
                            ?? throw new InvalidDataException("No email from claim");

                // 🔹 Detect linking mode
                var isLinking = context.Properties?.Items.TryGetValue("linking", out var linkFlag) == true && linkFlag == "true";
                var linkingUserId = context.Properties?.Items.TryGetValue("userId", out var uid) == true ? uid : null;

                User user;
                if (isLinking && Guid.TryParse(linkingUserId, out var existingUserId))
                {
                    user = await db.Users.FirstAsync(u => u.Id == existingUserId);

                    var existingLink = await db.ExternalLogins
                        .Include(l => l.User)
                        .FirstOrDefaultAsync(l => l.Provider == provider && l.ProviderSubject == providerSub);

                    var alreadyLinked = existingLink is not null;

                    if (!alreadyLinked)
                    {
                        db.ExternalLogins.Add(new ExternalLogin
                        {
                            Id = Guid.NewGuid(),
                            UserId = user.Id,
                            Provider = provider,
                            ProviderSubject = providerSub,
                            Created = DateTime.UtcNow,
                            LastModified = DateTime.UtcNow
                        });
                        await db.SaveChangesAsync();
                    }
                    else
                    {
                        if (existingLink?.UserId != user.Id)
                        {
                            // Store error message in temp property to show after redirect
                            context.Properties.Items["LinkError"] = $"This {provider} account is already linked to another user.";

                            // Redirect back to profile (or a dedicated page)
                            context.Response.Redirect($"/account/linkerror?message={Uri.EscapeDataString(context.Properties.Items["LinkError"])}");

                            context.HandleResponse(); // prevent further processing
                            return;
                        }
                    }

                    // skip normal sign-in, stay logged in as original user
                    context.HandleResponse();
                    context.Response.Redirect("/account/postlink");
                    return;
                }

                // 🔹 Normal login flow
                var externalLogin = await db.ExternalLogins.Include(l => l.User)
                    .FirstOrDefaultAsync(l => l.Provider == provider && l.ProviderSubject == providerSub);

                if (externalLogin?.User != null)
                    user = externalLogin.User;
                else
                {
                    user = await db.Users.FirstOrDefaultAsync(u => u.Email == email)
                           ?? new User { Id = Guid.NewGuid(), Email = email, DisplayName = email, CreatedAt = DateTime.UtcNow, IsActive = true };

                    if (!db.Users.Any(u => u.Id == user.Id)) db.Users.Add(user);

                    db.ExternalLogins.Add(new ExternalLogin
                    {
                        Id = Guid.NewGuid(),
                        UserId = user.Id,
                        Provider = provider,
                        ProviderSubject = providerSub,
                        Created = DateTime.UtcNow,
                        LastModified = DateTime.UtcNow
                    });

                    await db.SaveChangesAsync();
                }

                var identity = (ClaimsIdentity)principal.Identity!;
                identity.AddClaim(new Claim("InternalUserId", user.Id.ToString()));

                var roles = await roleService.GetRolesForUserAsync(user.Id);
                foreach (var r in roles)
                    identity.AddClaim(new Claim(ClaimTypes.Role, r));
            }
        };
    }

    private OAuthEvents CreateOAuthEvents(string providerName)
    {
        return new OAuthEvents
        {
            OnCreatingTicket = async context =>
            {
                var http = context.HttpContext;
                var db = http.RequestServices.GetRequiredService<KyivDbContext>();
                var roleService = http.RequestServices.GetRequiredService<IUserRoleService>();
                var provider = context.Scheme.Name;
                var identity = (ClaimsIdentity)context.Principal.Identity!;

                var userJson = JsonDocument.Parse(await context.Backchannel.GetStringAsync(context.Options.UserInformationEndpoint));
                var providerSub = userJson.RootElement.GetProperty("id").GetString()!;
                var email = userJson.RootElement.GetProperty("email").GetString()!;

                // 🔹 Detect linking mode
                var isLinking = context.Properties?.Items.TryGetValue("linking", out var linkFlag) == true && linkFlag == "true";
                var linkingUserId = context.Properties?.Items.TryGetValue("userId", out var uid) == true ? uid : null;

                User user;
                if (isLinking && Guid.TryParse(linkingUserId, out var existingUserId))
                {
                    user = await db.Users.FirstAsync(u => u.Id == existingUserId);


                    var existingLink = await db.ExternalLogins
                        .Include(l => l.User)
                        .FirstOrDefaultAsync(l => l.Provider == provider && l.ProviderSubject == providerSub);

                    var alreadyLinked = existingLink is not null;

                    if (!alreadyLinked)
                    {
                        db.ExternalLogins.Add(new ExternalLogin
                        {
                            Id = Guid.NewGuid(),
                            UserId = user.Id,
                            Provider = provider,
                            ProviderSubject = providerSub,
                            Created = DateTime.UtcNow,
                            LastModified = DateTime.UtcNow
                        });
                        await db.SaveChangesAsync();
                    }
                    else
                    {
                        if (existingLink?.UserId != user.Id)
                        {
                            // Store error message in temp property to show after redirect
                            context.Properties.Items["LinkError"] = $"This {provider} account is already linked to another user.";

                            // Redirect back to profile (or a dedicated page)
                            context.Response.Redirect($"/account/linkerror?message={Uri.EscapeDataString(context.Properties.Items["LinkError"])}");

                            return;
                        }
                    }

                    context.Response.Redirect("/account/postlink");
                    return;
                }

                // 🔹 Normal login flow
                var externalLogin = await db.ExternalLogins.Include(l => l.User)
                    .FirstOrDefaultAsync(l => l.Provider == provider && l.ProviderSubject == providerSub);

                if (externalLogin?.User != null)
                    user = externalLogin.User;
                else
                {
                    user = await db.Users.FirstOrDefaultAsync(u => u.Email == email)
                           ?? new User { Id = Guid.NewGuid(), Email = email, DisplayName = email, CreatedAt = DateTime.UtcNow, IsActive = true };

                    if (!db.Users.Any(u => u.Id == user.Id)) db.Users.Add(user);

                    db.ExternalLogins.Add(new ExternalLogin
                    {
                        Id = Guid.NewGuid(),
                        UserId = user.Id,
                        Provider = provider,
                        ProviderSubject = providerSub,
                        Created = DateTime.UtcNow,
                        LastModified = DateTime.UtcNow
                    });
                    await db.SaveChangesAsync();
                }

                identity.AddClaim(new Claim("InternalUserId", user.Id.ToString()));

                var roles = await roleService.GetRolesForUserAsync(user.Id);
                foreach (var r in roles)
                    identity.AddClaim(new Claim(ClaimTypes.Role, r));

                await http.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(identity),
                    new AuthenticationProperties { IsPersistent = true });
            }
        };
    }

}
