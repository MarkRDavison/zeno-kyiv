using mark.davison.common.authentication.server.Configuration;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text.Json;

namespace mark.davison.common.authentication.server.Ignition;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddServerAuthentication<TDbContext>(this IServiceCollection services, AuthenticationSettings authenticationSettings)
        where TDbContext : DbContext
    {

        services
            .AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            })
            .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme);

        services
            .AddMemoryCache();

        services
            .AddOptions<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme)
            .Configure<IRedisTicketStore, IDateService>(
                (options, store, dateService) =>
                {
                    options.SessionStore = store;
                    options.SlidingExpiration = true;
                    options.Events.OnValidatePrincipal = async context =>
                    {
                        if (await AuthTokenHelpers.RefreshTokenIfNeeded(dateService, store, context.Properties))
                        {
                            context.ShouldRenew = true;
                        }
                    };
                });

        services
            .AddScoped<IUserService, UserService<TDbContext>>()
            .AddScoped<IUserRoleService, UserRoleService<TDbContext>>()
            .AddSingleton<IRedisTicketStore, RedisTicketStore>()
            .AddAuthenticationProviders<TDbContext>(authenticationSettings)
            .AddAuthorization();

        return services;
    }

    private static IServiceCollection AddAuthenticationProviders<TDbContext>(this IServiceCollection services, AuthenticationSettings authenticationSettings)
        where TDbContext : DbContext
    {
        foreach (var provider in authenticationSettings.Providers)
        {
            if (string.Equals(provider.Type, AuthConstants.ProviderType_Oidc, StringComparison.OrdinalIgnoreCase))
            {
                services.AddOidcAuthenticationProvider<TDbContext>(provider, authenticationSettings);
            }
            else if (string.Equals(provider.Type, AuthConstants.ProviderType_Oauth, StringComparison.OrdinalIgnoreCase))
            {
                services.AddOauthAuthenticationProvider<TDbContext>(provider, authenticationSettings);
            }
            else
            {
                throw new InvalidOperationException("Unhandled authentication provider type: " + provider.Type);
            }
        }

        return services;
    }

    private static void AddOidcAuthenticationProvider<TDbContext>(this IServiceCollection services, AuthenticationProviderConfiguration providerConfiguration, AuthenticationSettings authenticationSettings)
        where TDbContext : DbContext
    {
        services
            .AddAuthentication()
            .AddOpenIdConnect(providerConfiguration.Name, options =>
            {
                options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.Authority = providerConfiguration.Authority;
                options.ClientId = providerConfiguration.ClientId;
                options.ClientSecret = providerConfiguration.ClientSecret;
                options.ResponseType = OpenIdConnectResponseType.Code;
                options.CallbackPath = $"/signin-{providerConfiguration.Name}";
                options.SaveTokens = true;
                options.UsePkce = true;
                foreach (var s in providerConfiguration.Scope ?? Array.Empty<string>()) options.Scope.Add(s);
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    NameClaimType = "name",
                    RoleClaimType = ClaimTypes.Role
                };
                options.Events = CreateOidcEvents<TDbContext>(providerConfiguration.Name, authenticationSettings);
            });
    }
    private static void AddOauthAuthenticationProvider<TDbContext>(this IServiceCollection services, AuthenticationProviderConfiguration providerConfiguration, AuthenticationSettings authenticationSettings)
        where TDbContext : DbContext
    {
        services
            .AddAuthentication()
            .AddOAuth(providerConfiguration.Name, options =>
            {
                options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.ClientId = providerConfiguration.ClientId ?? string.Empty;
                options.ClientSecret = providerConfiguration.ClientSecret ?? string.Empty;
                options.CallbackPath = $"/signin-{providerConfiguration.Name}";
                options.AuthorizationEndpoint = providerConfiguration.AuthorizationEndpoint ?? string.Empty;
                options.TokenEndpoint = providerConfiguration.TokenEndpoint ?? string.Empty;
                options.UserInformationEndpoint = providerConfiguration.UserInformationEndpoint ?? string.Empty;
                options.SaveTokens = true;
                options.Scope.Clear();
                foreach (var s in providerConfiguration.Scope ?? Array.Empty<string>()) options.Scope.Add(s);

                options.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "id");
                options.ClaimActions.MapJsonKey(ClaimTypes.Name, "name");
                options.ClaimActions.MapJsonKey(ClaimTypes.Email, "email");

                options.Events = CreateOAuthEvents<TDbContext>(providerConfiguration.Name, authenticationSettings);
            });
    }


    private static OpenIdConnectEvents CreateOidcEvents<TDbContext>(string providerName, AuthenticationSettings authenticationSettings)
        where TDbContext : DbContext
    {
        return new OpenIdConnectEvents
        {
            OnTokenValidated = async context =>
            {
                AuthTokenHelpers.NormalizeTokenTimes(context.Properties);

                var http = context.HttpContext;
                var db = http.RequestServices.GetRequiredService<TDbContext>();
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
                    user = await db.Set<User>().FirstAsync(u => u.Id == existingUserId);

                    var existingLink = await db.Set<ExternalLogin>()
                        .Include(l => l.User)
                        .FirstOrDefaultAsync(l => l.Provider == provider && l.ProviderSubject == providerSub);

                    var alreadyLinked = existingLink is not null;

                    if (!alreadyLinked)
                    {
                        db.Set<ExternalLogin>().Add(new ExternalLogin
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
                var externalLogin = await db.Set<ExternalLogin>().Include(l => l.User)
                    .FirstOrDefaultAsync(l => l.Provider == provider && l.ProviderSubject == providerSub);

                if (externalLogin?.User != null)
                    user = externalLogin.User;
                else
                {
                    user = await db.Set<User>().FirstOrDefaultAsync(u => u.Email == email)
                           ?? new User { Id = Guid.NewGuid(), Email = email, DisplayName = email, CreatedAt = DateTime.UtcNow, IsActive = true };

                    if (!db.Set<User>().Any(u => u.Id == user.Id))
                    {
                        db.Set<User>().Add(user);
                        var defaultRole = await db.Set<Role>().FirstAsync(r => r.Name == "User"); // TODO: Use constant ids??? here and other place
                        db.Set<UserRole>().Add(new UserRole
                        {
                            Id = Guid.NewGuid(),
                            UserId = user.Id,
                            RoleId = defaultRole.Id,
                            Created = DateTime.UtcNow,
                            LastModified = DateTime.UtcNow
                        });

                        if (string.Equals(user.Email, authenticationSettings.ADMIN_EMAIL, StringComparison.OrdinalIgnoreCase))
                        {
                            var adminRole = await db.Set<Role>().FirstAsync(r => r.Name == "Admin"); // TODO: Use constant ids??? here and other place
                            var alreadyAdmin = await db.Set<UserRole>().AnyAsync(ur => ur.UserId == user.Id && ur.RoleId == adminRole.Id);
                            if (!alreadyAdmin)
                            {
                                db.Set<UserRole>().Add(new UserRole
                                {
                                    Id = Guid.NewGuid(),
                                    UserId = user.Id,
                                    RoleId = adminRole.Id,
                                    Created = DateTime.UtcNow,
                                    LastModified = DateTime.UtcNow
                                });
                            }
                        }
                    }

                    db.Set<ExternalLogin>().Add(new ExternalLogin
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
                identity.AddClaim(new Claim("LoggedInProvider", provider));

                context.Properties.Items["provider"] = providerName;
                context.Properties.Items["client_id"] = context.Options.ClientId;
                context.Properties.Items["client_secret"] = context.Options.ClientSecret;
                context.Properties.Items["token_endpoint"] = context.Options.Authority + "/protocol/openid-connect/token"; // adjust per provider

                var roles = await roleService.GetRolesForUserAsync(user.Id);
                foreach (var r in roles)
                {
                    identity.AddClaim(new Claim(ClaimTypes.Role, r));
                }
            }
        };
    }
    private static OAuthEvents CreateOAuthEvents<TDbContext>(string providerName, AuthenticationSettings authenticationSettings)
        where TDbContext : DbContext
    {
        return new OAuthEvents
        {
            OnCreatingTicket = async context =>
            {
                var http = context.HttpContext;
                var db = http.RequestServices.GetRequiredService<TDbContext>();
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
                    user = await db.Set<User>().FirstAsync(u => u.Id == existingUserId);


                    var existingLink = await db.Set<ExternalLogin>()
                        .Include(l => l.User)
                        .FirstOrDefaultAsync(l => l.Provider == provider && l.ProviderSubject == providerSub);

                    var alreadyLinked = existingLink is not null;

                    if (!alreadyLinked)
                    {
                        db.Set<ExternalLogin>().Add(new ExternalLogin
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
                var externalLogin = await db.Set<ExternalLogin>().Include(l => l.User)
                    .FirstOrDefaultAsync(l => l.Provider == provider && l.ProviderSubject == providerSub);

                if (externalLogin?.User != null)
                    user = externalLogin.User;
                else
                {
                    user = await db.Set<User>().FirstOrDefaultAsync(u => u.Email == email)
                           ?? new User { Id = Guid.NewGuid(), Email = email, DisplayName = email, CreatedAt = DateTime.UtcNow, IsActive = true };

                    if (!db.Set<User>().Any(u => u.Id == user.Id))
                    {
                        db.Set<User>().Add(user);
                        var defaultRole = await db.Set<Role>().FirstAsync(r => r.Name == "User");
                        db.Set<UserRole>().Add(new UserRole
                        {
                            Id = Guid.NewGuid(),
                            UserId = user.Id,
                            RoleId = defaultRole.Id,
                            Created = DateTime.UtcNow,
                            LastModified = DateTime.UtcNow
                        });

                        if (string.Equals(user.Email, authenticationSettings.ADMIN_EMAIL, StringComparison.OrdinalIgnoreCase))
                        {
                            var adminRole = await db.Set<Role>().FirstAsync(r => r.Name == "Admin"); // TODO: Use constant ids??? here and other place
                            var alreadyAdmin = await db.Set<UserRole>().AnyAsync(ur => ur.UserId == user.Id && ur.RoleId == adminRole.Id);
                            if (!alreadyAdmin)
                            {
                                db.Set<UserRole>().Add(new UserRole
                                {
                                    Id = Guid.NewGuid(),
                                    UserId = user.Id,
                                    RoleId = adminRole.Id,
                                    Created = DateTime.UtcNow,
                                    LastModified = DateTime.UtcNow
                                });
                            }
                        }
                    }

                    db.Set<ExternalLogin>().Add(new ExternalLogin
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
                identity.AddClaim(new Claim("LoggedInProvider", provider));

                var roles = await roleService.GetRolesForUserAsync(user.Id);
                foreach (var r in roles)
                {
                    identity.AddClaim(new Claim(ClaimTypes.Role, r));
                }

                // Cookie is issued manually here
                var props = new AuthenticationProperties
                {
                    IsPersistent = true
                };
                props.Items["provider"] = providerName;
                props.Items["client_id"] = context.Options.ClientId;
                props.Items["client_secret"] = context.Options.ClientSecret;
                props.Items["token_endpoint"] = context.Options.TokenEndpoint;

                await http.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(identity),
                    props);
            }
        };
    }

}
