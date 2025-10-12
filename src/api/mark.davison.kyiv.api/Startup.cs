using mark.davison.kyiv.api.Services;
using mark.davison.kyiv.shared.models.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System.Security;
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

    private static bool IsLinking(AuthenticationProperties? properties)
    {
        if (properties is null)
        {
            return false;
        }

        return properties.Items.TryGetValue("linking", out var isLinking) && isLinking == "true";
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
                options.DefaultScheme = AppSettings.AUTHENTICATION.DefaultScheme ?? CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = AppSettings.AUTHENTICATION.DefaultChallengeScheme ?? OpenIdConnectDefaults.AuthenticationScheme;
            })
            .AddCookie(AppSettings.AUTHENTICATION.DefaultScheme ?? CookieAuthenticationDefaults.AuthenticationScheme);


        services
            .AddScoped<IUserService, UserService>()
            .AddScoped<IUserRoleService, UserRoleService>();

        services.AddMemoryCache();

        foreach (var p in AppSettings.AUTHENTICATION.Providers)
        {
            if (string.Equals(p.Type, "oidc", StringComparison.OrdinalIgnoreCase))
            {
                services
                    .AddAuthentication(p.Name)
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
                        foreach (var s in p.Scope ?? Array.Empty<string>()) { options.Scope.Add(s); }
                        options.TokenValidationParameters = new TokenValidationParameters
                        {
                            NameClaimType = "name",
                            RoleClaimType = ClaimTypes.Role
                        };
                        options.Events = CreateOidcEvents(p.Name);
                        options.TokenValidationParameters.NameClaimType = "name";
                        options.TokenValidationParameters.RoleClaimType = "role";
                    });
            }
            else if (string.Equals(p.Type, "oauth", StringComparison.OrdinalIgnoreCase))
            {
                services
                    .AddAuthentication()
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

                        // options.TokenValidationParameters.NameClaimType = "name";
                        // options.TokenValidationParameters.RoleClaimType = "role";
                    });
            }
            else
            {
                throw new InvalidOperationException($"Unknown provider type: {p.Type}");
            }
        }

        services
            .AddAuthorization()
            .AddHealthChecks();

        services
            .AddHttpClient()
            .AddHttpContextAccessor();

        services
            .AddDbContextFactory<KyivDbContext>(_ =>
            {
                _.UseSqlite($"Data Source=Kyiv.db", sqliteOptions =>
                {
                    //sqliteOptions.MigrationsAssembly
                });

                _.EnableSensitiveDataLogging();
                _.EnableDetailedErrors();
            });

        services.AddHostedService<ApplicationHealthStateHostedService>();

    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseCors(builder =>
            builder
                .SetIsOriginAllowedToAllowWildcardSubdomains()
                .SetIsOriginAllowed(_ => true) // TODO: Config driven
                .AllowAnyMethod()
                .AllowCredentials()
                .AllowAnyHeader());

        app.UseHttpsRedirection();

        if (env.IsDevelopment())
        {

        }

        app
            .UseRouting()
            .UseAuthentication()
            .UseAuthorization()
            .UseEndpoints(endpoints =>
            {
                endpoints
                    .MapHealthChecks("health");

                endpoints.MapGet("/", (HttpContext ctx) =>
                {
                    var user = ctx.User.Identity?.IsAuthenticated == true
                        ? $"Signed in as {ctx.User.Identity?.Name}"
                        : "Not signed in";

                    var html = $@"
                        <h1>Home</h1>
                        <p>{user}</p>
                        <a href=""/account/login"">Login</a> |
                        <a href=""/account/logout"">Logout</a> |
                        <a href=""/account/profile"">Profile</a>
                    ";

                    return Results.Content(html, "text/html");
                });

                // --- LOGIN PAGE ---
                endpoints.MapGet("/account/login", () =>
                {
                    // In a real app, render proper HTML or Razor
                    var html = """
        <h2>Login</h2>
        <a href="/account/login/google">Login with Google</a><br/>
        <a href="/account/login/github">Login with GitHub</a><br/>
        <a href="/account/login/microsoft">Login with Microsoft</a><br/>
        <a href="/account/login/keycloak">Login with Keycloak</a><br/>
    """;
                    return Results.Content(html, "text/html");
                });

                // --- START LOGIN ---
                endpoints.MapGet("/account/login/{provider}", (string provider, HttpContext ctx) =>
                {
                    var redirectUrl = "/account/postlogin";
                    var props = new AuthenticationProperties { RedirectUri = redirectUrl };
                    return Results.Challenge(props, new[] { provider });
                });

                // --- POST LOGIN LANDING ---
                endpoints.MapGet("/account/postlogin", (HttpContext ctx) =>
                {
                    if (ctx.User.Identity?.IsAuthenticated != true)
                        return Results.Redirect("/account/login");

                    return Results.Content($@"
        <h3>Welcome, {ctx.User.Identity?.Name}</h3>
        <a href=""/account/profile"">Go to Profile</a>
    ", "text/html");
                });

                // --- PROFILE PAGE ---
                endpoints.MapGet("/account/profile", async (HttpContext ctx, [FromServices] IUserService users) =>
                {
                    if (ctx.User.Identity?.IsAuthenticated != true)
                        return Results.Redirect("/account/login");

                    var userId = Guid.Parse(ctx.User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                    var linked = await users.GetUserByIdAsync(userId);

                    var list = string.Join("", linked.ExternalLogins.Select(p => $"<li>{p.Provider}</li>"));
                    var html = $"""
        <h2>Profile</h2>
        <p>Signed in as {ctx.User.Identity?.Name}</p>
        <ul>{list}</ul>
        <h3>Link a new provider</h3>
        <a href="/account/link/google">Link Google</a><br/>
        <a href="/account/link/github">Link GitHub</a><br/>
        <a href="/account/link/microsoft">Link Microsoft</a><br/>
        <a href="/account/link/keycloak">Link Keycloak</a><br/>
    """;
                    return Results.Content(html, "text/html");
                });

                // --- START LINKING FLOW ---
                endpoints.MapGet("/account/link/{provider}", (string provider, HttpContext ctx) =>
                {
                    if (ctx.User.Identity?.IsAuthenticated != true)
                        return Results.Redirect("/account/login");

                    var props = new AuthenticationProperties
                    {
                        RedirectUri = "/account/linked",
                        Items = { ["linking"] = "true" }
                    };

                    return Results.Challenge(props, new[] { provider });
                });

                // --- LINK SUCCESS PAGE ---
                endpoints.MapGet("/account/linked", (HttpContext ctx) =>
                {
                    if (ctx.User.Identity?.IsAuthenticated != true)
                        return Results.Redirect("/account/login");

                    return Results.Content("""
        <p>Your provider was successfully linked!</p>
        <a href="/account/profile">Back to profile</a>
    """, "text/html");
                });

                // --- LOGOUT ---
                endpoints.MapGet("/account/logout", async (HttpContext ctx) =>
                {
                    if (ctx.User.Identity?.IsAuthenticated == true)
                        await ctx.SignOutAsync();

                    return Results.Redirect("/");
                });
            });
    }


    private OpenIdConnectEvents CreateOidcEvents(string providerName)
    {
        return new OpenIdConnectEvents
        {
            OnRedirectToIdentityProvider = context =>
            {
                if (IsLinking(context.Properties))
                {
                    context.ProtocolMessage.RedirectUri = $"{context.Request.Scheme}://{context.Request.Host}/signin-{providerName}";
                }

                return Task.CompletedTask;
            },
            OnTokenValidated = async context =>
            {
                var http = context.HttpContext;
                var db = http.RequestServices.GetRequiredService<KyivDbContext>();
                var provider = context.Scheme.Name; // "keycloak"
                var providerSub = context.Principal!.FindFirst("sub")?.Value
                    ?? context.Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? throw new InvalidDataException("NO PROVIDER SUBJECT");
                var email = context.Principal.FindFirst(ClaimTypes.Email)?.Value
                    ?? throw new InvalidDataException("NO EMAIL FROM CLAIM");

                // Handle linking flow
                if (IsLinking(context.Properties))
                {
                    var currentUserId = http.User.FindFirstValue(ClaimTypes.NameIdentifier)
                        ?? throw new SecurityException("Must be signed in to link accounts.");

                    var existing = await db.ExternalLogins
                        .FirstOrDefaultAsync(l => l.Provider == provider && l.ProviderSubject == providerSub);

                    if (existing != null)
                        throw new InvalidOperationException("External login already linked to another account.");

                    db.ExternalLogins.Add(new ExternalLogin
                    {
                        Id = Guid.NewGuid(),
                        UserId = Guid.Parse(currentUserId),
                        Provider = provider,
                        ProviderSubject = providerSub,
                        Created = DateTime.UtcNow,
                        LastModified = DateTime.UtcNow
                    });

                    await db.SaveChangesAsync();

                    context.HandleResponse();
                    http.Response.Redirect("/ManageLogins?message=Linked");
                    return;
                }

                // Normal sign-in or registration flow
                var externalLogin = await db.ExternalLogins
                    .Include(l => l.User)
                    .FirstOrDefaultAsync(l => l.Provider == provider && l.ProviderSubject == providerSub);

                User user;
                if (externalLogin?.User is not null)
                {
                    user = externalLogin.User;
                }
                else
                {
                    var existingUser = await db.Users.FirstOrDefaultAsync(u => u.Email == email);
                    user = existingUser ?? new User
                    {
                        Id = Guid.NewGuid(),
                        Email = email,
                        IsActive = true,
                        DisplayName = email,
                        CreatedAt = DateTime.UtcNow,
                        LastModified = DateTime.UtcNow
                    };

                    if (existingUser is null)
                    {
                        db.Users.Add(user);
                        await db.SaveChangesAsync();
                    }

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

                // Replace the existing NameIdentifier (provider sub) with our internal user ID
                var identity = (ClaimsIdentity)context.Principal.Identity!;
                var oldSub = identity.FindFirst(ClaimTypes.NameIdentifier);
                if (oldSub != null)
                    identity.RemoveClaim(oldSub);

                identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()));

                // Add app-specific roles
                var roleService = http.RequestServices.GetRequiredService<IUserRoleService>();
                var roles = await roleService.GetRolesForUserAsync(user.Id);
                foreach (var role in roles)
                {
                    identity.AddClaim(new Claim(ClaimTypes.Role, role));
                }
            }
        };
    }

    private OAuthEvents CreateOAuthEvents(string providerName)
    {
        return new OAuthEvents
        {
            OnCreatingTicket = async context =>
            {
                // Map GitHub id -> "sub" for consistency, populate claims
                var userJson = JsonDocument.Parse(context.User.ToString());
                var id = userJson.RootElement.GetProperty("id").GetRawText().Trim('"');
                context.Identity?.AddClaim(new Claim("sub", id));

                if (userJson.RootElement.TryGetProperty("email", out var emailEl))
                {
                    var email = emailEl.GetString();
                    if (!string.IsNullOrEmpty(email))
                    {
                        context.Identity?.AddClaim(new Claim(ClaimTypes.Email, email));
                    }
                }

                // Reuse same linking/login logic by manually invoking the same flow:
                // Build a fake TokenValidated-like context is complex; easiest approach:
                // After successful OAuth ticket, redirect to a small endpoint that will finish linking/login using the claims.
                // But for simplicity we can call the same DB logic here (access services)
                var http = context.HttpContext;
                var db = http.RequestServices.GetRequiredService<KyivDbContext>();
                var provider = context.Scheme.Name;
                var providerSub = id;
                var emailClaim = context.Identity?.FindFirst(ClaimTypes.Email)?.Value;
                var currentUserId = http.User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? throw new SecurityException("Must be signed in to link accounts.");

                // If linking flag present in properties
                if (context.Properties.Items.TryGetValue("linking", out var isLinking) && isLinking == "true")
                {

                    var existing = await db.ExternalLogins
                        .FirstOrDefaultAsync(l => l.Provider == provider && l.ProviderSubject == providerSub);

                    if (existing != null)
                        throw new InvalidOperationException("External login already linked to another account.");

                    db.ExternalLogins.Add(new ExternalLogin
                    {
                        Id = Guid.NewGuid(),
                        UserId = Guid.Parse(currentUserId),
                        Provider = provider,
                        ProviderSubject = providerSub,
                        Created = DateTime.UtcNow,
                        LastModified = DateTime.UtcNow
                    });
                    await db.SaveChangesAsync();

                    context.Response.Redirect("/ManageLogins?message=Linked");
                    context.Success();
                    return;
                }

                // Normal login flow for OAuth-based provider
                var externalLogin = await db.ExternalLogins
                    .Include(l => l.User)
                    .FirstOrDefaultAsync(l => l.Provider == provider && l.ProviderSubject == providerSub);

                User user;
                if (externalLogin != null) user = externalLogin.User;
                else
                {
                    user = await db.Users.FirstOrDefaultAsync(u => u.Email == emailClaim);
                    if (user == null)
                    {
                        user = new User { Email = emailClaim, DisplayName = emailClaim, CreatedAt = DateTime.UtcNow };
                        db.Users.Add(user); await db.SaveChangesAsync();
                    }
                    db.ExternalLogins.Add(new ExternalLogin
                    {
                        Id = Guid.NewGuid(),
                        UserId = Guid.Parse(currentUserId),
                        Provider = provider,
                        ProviderSubject = providerSub,
                        Created = DateTime.UtcNow,
                        LastModified = DateTime.UtcNow
                    });
                    await db.SaveChangesAsync();
                }

                // attach app roles
                var roleService = http.RequestServices.GetRequiredService<IUserRoleService>();
                var roles = await roleService.GetRolesForUserAsync(user.Id);
                foreach (var r in roles) context.Identity.AddClaim(new Claim(ClaimTypes.Role, r));
                // add name identifier
                context.Identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()));
            }
        };
    }

}
