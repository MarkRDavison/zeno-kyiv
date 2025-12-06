using mark.davison.common.authentication.server.Configuration;
using mark.davison.common.authentication.server.Models;
using mark.davison.common.Constants;
using mark.davison.common.server.abstractions.Services;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Net.Http.Headers;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Text.Json;

namespace mark.davison.common.authentication.server.Ignition;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, AuthenticationSettings authenticationSettings)
    {
        var authBuilder = services
            .AddSingleton<IAuthenticationProvidersService, AuthenticationProvidersService>()
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = AuthConstants.DynamicScheme;
                options.DefaultChallengeScheme = AuthConstants.DynamicScheme;
            })
            .AddPolicyScheme(AuthConstants.DynamicScheme, AuthConstants.DynamicScheme, options =>
            {
                options.ForwardDefaultSelector = context =>
                {
                    var authHeader = context.Request.Headers[HeaderNames.Authorization].ToString();

                    string issuer;

                    if (AuthenticationHeaderValue.TryParse(authHeader, out var headerValue))
                    {
                        var scheme = headerValue.Scheme;
                        if (!string.Equals(scheme, "Bearer", StringComparison.OrdinalIgnoreCase))
                        {
                            throw new InvalidOperationException("Missing or unknown authentication scheme");
                        }
                        var parameter = headerValue.Parameter;
                        var handler = new JwtSecurityTokenHandler();
                        if (!handler.CanReadToken(parameter))
                        {
                            throw new InvalidOperationException("Missing or unknown authentication provider");
                        }

                        var jwt = handler.ReadJwtToken(parameter);
                        issuer = jwt.Issuer;
                    }
                    else
                    {
                        throw new InvalidOperationException("Missing or unknown authentication header");
                    }

                    foreach (var provider in authenticationSettings.Providers)
                    {
                        if (!string.IsNullOrWhiteSpace(provider.Authority) &&
                            provider.Authority.StartsWith(issuer))
                        {
                            return provider.Name;
                        }
                    }

                    throw new InvalidOperationException("Missing or unknown authentication provider");
                };
            });

        foreach (var provider in authenticationSettings.Providers)
        {
            authBuilder
                .AddJwtBearer(provider.Name, options =>
                {
                    options.Authority = provider.Authority;
                    options.Audience = provider.ClientId;
                });
        }


        return services;
    }

    public static IServiceCollection AddRemoteForwarderAuthentication(
        this IServiceCollection services,
        string remoteEndpointUri)
    {
        services
            .AddTransient<BearerTokenHandler>()
            .AddHttpContextAccessor()
            .AddHttpClient<RemoteUserAuthenticationService>((s, c) =>
            {
                c.BaseAddress = new Uri(remoteEndpointUri);
            })
            .AddHttpMessageHandler<BearerTokenHandler>();
        services.AddScoped<IUserAuthenticationService, RemoteUserAuthenticationService>();

        return services;
    }
    public static IServiceCollection AddOidcCookieAuthentication(
        this IServiceCollection services,
        AuthenticationSettings authenticationSettings,
        Func<IServiceProvider, string, string, UserDto> createUser)
    {
        services
            .AddSingleton<IAuthenticationProvidersService, AuthenticationProvidersService>()
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
            .AddSingleton<IOidcAuthenticationService, OidcAuthenticationService>()
            .AddSingleton<IRedisTicketStore, RedisTicketStore>()
            .AddAuthenticationProviders(authenticationSettings, createUser)
            .AddAuthorization();

        return services;
    }

    private static IServiceCollection AddAuthenticationProviders(this IServiceCollection services, AuthenticationSettings authenticationSettings, Func<IServiceProvider, string, string, UserDto> createUser)
    {
        foreach (var provider in authenticationSettings.Providers)
        {
            if (string.Equals(provider.Type, AuthConstants.ProviderType_Oidc, StringComparison.OrdinalIgnoreCase))
            {
                services.AddOidcAuthenticationProvider(provider, authenticationSettings, createUser);
            }
            else if (string.Equals(provider.Type, AuthConstants.ProviderType_Oauth, StringComparison.OrdinalIgnoreCase))
            {
                services.AddOauthAuthenticationProvider(provider, authenticationSettings, createUser);
            }
            else
            {
                throw new InvalidOperationException("Unhandled authentication provider type: " + provider.Type);
            }
        }

        return services;
    }

    private static void AddOidcAuthenticationProvider(this IServiceCollection services, AuthenticationProviderConfiguration providerConfiguration, AuthenticationSettings authenticationSettings, Func<IServiceProvider, string, string, UserDto> createUser)
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
                options.Events = CreateOidcEvents(providerConfiguration.Name, authenticationSettings, createUser);
            });
    }
    private static void AddOauthAuthenticationProvider(this IServiceCollection services, AuthenticationProviderConfiguration providerConfiguration, AuthenticationSettings authenticationSettings, Func<IServiceProvider, string, string, UserDto> createUser)
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
                foreach (var s in providerConfiguration.Scope ?? [])
                {
                    options.Scope.Add(s);
                }

                options.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "id");
                options.ClaimActions.MapJsonKey(ClaimTypes.Name, "name");
                options.ClaimActions.MapJsonKey(ClaimTypes.Email, "email");

                options.Events = CreateOAuthEvents(providerConfiguration.Name, authenticationSettings, createUser);
            });
    }

    // TODO: Consolidate
    private static OAuthEvents CreateOAuthEvents(string providerName, AuthenticationSettings authenticationSettings, Func<IServiceProvider, string, string, UserDto> createUser)
    {
        return new OAuthEvents
        {
            OnCreatingTicket = async context =>
            {
                var http = context.HttpContext;
                var userAuthenticationService = http.RequestServices.GetRequiredService<IUserAuthenticationService>();
                var provider = context.Scheme.Name;
                var identity = (ClaimsIdentity)context.Principal.Identity!;

                // await context.Backchannel.GetStringAsync(context.Options.UserInformationEndpoint)
                var userJson = JsonDocument.Parse(await context.Backchannel.GetStringAsync(context.Options.UserInformationEndpoint));
                var providerSub = userJson.RootElement.GetProperty("id").GetString()!;
                var email = userJson.RootElement.GetProperty("email").GetString()!;
                var name = userJson.RootElement.GetProperty("name").GetString() ?? email;

                //  Detect linking mode
                var isLinking = context.Properties?.Items.TryGetValue("linking", out var linkFlag) == true && linkFlag == "true";
                var linkingUserId = context.Properties?.Items.TryGetValue("userId", out var uid) == true ? uid : null;

                if (isLinking && Guid.TryParse(linkingUserId, out var existingUserId))
                {
                    var user = await userAuthenticationService.GetUserByIdAsync(existingUserId, http.RequestAborted);

                    var existingLink = await userAuthenticationService.GetExternalLoginForProviderAsync(provider, providerSub, http.RequestAborted);

                    var alreadyLinked = existingLink is not null;

                    if (!alreadyLinked)
                    {
                        await userAuthenticationService.AddExternalLoginAsync(existingUserId, provider, providerSub, http.RequestAborted);
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

                //  Normal login flow
                var externalLogin = await userAuthenticationService.GetExternalLoginForProviderAsync(provider, providerSub, http.RequestAborted);

                Guid userId;
                if (externalLogin != null)
                {
                    userId = externalLogin.UserId;
                }
                else
                {
                    var user = await userAuthenticationService.GetUserByEmailAsync(email, http.RequestAborted)
                           ?? createUser(http.RequestServices, email, name);

                    var userById = await userAuthenticationService.GetUserByIdAsync(user.Id, http.RequestAborted);

                    if (userById is null)
                    {
                        var rolesToCreate = new List<string> { RoleConstants.User };
                        if (string.Equals(user.Email, authenticationSettings.ADMIN_EMAIL, StringComparison.OrdinalIgnoreCase))
                        {
                            rolesToCreate.Add(RoleConstants.Admin);
                        }

                        await userAuthenticationService.CreateUserWithRolesAsync(user, rolesToCreate, http.RequestAborted);
                    }


                    userId = user.Id;

                    await userAuthenticationService.AddExternalLoginAsync(user.Id, provider, providerSub, http.RequestAborted);
                }

                identity.AddClaim(new Claim("InternalUserId", userId.ToString()));
                identity.AddClaim(new Claim("LoggedInProvider", provider));

                var roles = await userAuthenticationService.GetRolesForUserIdAsync(userId, http.RequestAborted);
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

    // TODO: Consolidate
    private static OpenIdConnectEvents CreateOidcEvents(string providerName, AuthenticationSettings authenticationSettings, Func<IServiceProvider, string, string, UserDto> createUser)
    {
        return new OpenIdConnectEvents
        {
            OnRedirectToIdentityProvider = async context =>
            {
                context.ProtocolMessage.SetParameter("access_type", "offline");
                context.ProtocolMessage.SetParameter("prompt", "consent");
                await Task.CompletedTask;
            },
            OnTokenValidated = async context =>
            {
                if (context.Properties is null)
                {
                    throw new InvalidOperationException("OnTokenValidated - invalid properties");
                }

                AuthTokenHelpers.NormalizeTokenTimes(context.Properties);

                var accessToken = context.TokenEndpointResponse?.IdToken;
                string tokenEndpoint = string.Empty;

                if (context.Options.ConfigurationManager is { } cm)
                {
                    var config = await cm.GetConfigurationAsync(CancellationToken.None);

                    tokenEndpoint = config.TokenEndpoint;
                }

                var http = context.HttpContext;
                var provider = context.Scheme.Name;

                var principal = context.Principal!;
                var providerSub = principal.FindFirst("sub")?.Value
                                  ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                                  ?? throw new InvalidDataException("No provider subject");

                var email = principal.FindFirst(ClaimTypes.Email)?.Value
                            ?? throw new InvalidDataException("No email from claim");

                var name = principal.FindFirst(ClaimTypes.Name)?.Value
                            ?? email;

                //  Detect linking mode
                var isLinking = context.Properties?.Items.TryGetValue("linking", out var linkFlag) == true && linkFlag == "true";
                var linkingUserId = context.Properties?.Items.TryGetValue("userId", out var uid) == true ? uid : null;

                var userAuthenticationService = http.RequestServices.GetRequiredService<IUserAuthenticationService>();

                if (!string.IsNullOrEmpty(accessToken))
                {
                    userAuthenticationService.SetToken(accessToken);
                }

                if (isLinking && Guid.TryParse(linkingUserId, out var existingUserId))
                {
                    var user = await userAuthenticationService.GetUserByIdAsync(existingUserId, http.RequestAborted);

                    var existingLink = await userAuthenticationService.GetExternalLoginForProviderAsync(provider, providerSub, http.RequestAborted);

                    var alreadyLinked = existingLink is not null;

                    if (!alreadyLinked)
                    {
                        await userAuthenticationService.AddExternalLoginAsync(existingUserId, provider, providerSub, http.RequestAborted);
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

                //  Normal login flow
                var externalLogin = await userAuthenticationService.GetExternalLoginForProviderAsync(provider, providerSub, http.RequestAborted);

                Guid userId;
                if (externalLogin != null)
                {
                    userId = externalLogin.UserId;
                }
                else
                {
                    var user = await userAuthenticationService.GetUserByEmailAsync(email, http.RequestAborted)
                           ?? createUser(http.RequestServices, email, name);

                    var userById = await userAuthenticationService.GetUserByIdAsync(user.Id, http.RequestAborted);

                    if (userById is null)
                    {
                        var rolesToCreate = new List<string> { RoleConstants.User };
                        if (string.Equals(user.Email, authenticationSettings.ADMIN_EMAIL, StringComparison.OrdinalIgnoreCase))
                        {
                            rolesToCreate.Add(RoleConstants.Admin);
                        }

                        await userAuthenticationService.CreateUserWithRolesAsync(user, rolesToCreate, http.RequestAborted);
                    }

                    userId = user.Id;
                    await userAuthenticationService.AddExternalLoginAsync(user.Id, provider, providerSub, http.RequestAborted);
                }

                var identity = (ClaimsIdentity)principal.Identity!;
                identity.AddClaim(new Claim("InternalUserId", userId.ToString()));
                identity.AddClaim(new Claim("LoggedInProvider", provider));

                if (context.Properties is { } properties)
                {
                    properties.Items["provider"] = providerName;
                    properties.Items["client_id"] = context.Options.ClientId;
                    properties.Items["client_secret"] = context.Options.ClientSecret;
                    properties.Items["token_endpoint"] = tokenEndpoint;
                }

                var roles = await userAuthenticationService.GetRolesForUserIdAsync(userId, http.RequestAborted);
                foreach (var r in roles)
                {
                    identity.AddClaim(new Claim(ClaimTypes.Role, r));
                }
            }
        };
    }

}
