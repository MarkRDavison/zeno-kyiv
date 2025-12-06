using mark.davison.common.authentication.server.Models;
using mark.davison.common.server.Models;
using Microsoft.AspNetCore.Mvc;

namespace mark.davison.kyiv.api;

public sealed class Startup
{
    public IConfiguration Configuration { get; }

    public AppSettings AppSettings { get; set; } = new();

    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        AppSettings = services.BindAppSettings(Configuration);

        services
            .AddCors()
            .AddLogging()
            .AddSingleton<IDateService>(_ => new DateService(DateService.DateMode.Utc))
            .AddSingleton<IDataSeeder, KyivDataSeeder>()
            .AddAuthorization()
            .AddJwtAuthentication(AppSettings.AUTHENTICATION)
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
                endpoints.MapGet(
                    "/api/external-login",
                    async (HttpContext context, [FromQuery] string provider, [FromQuery] string providerSub) =>
                    {
                        var dbContext = context.RequestServices.GetRequiredService<KyivDbContext>();

                        var externalLogin = await dbContext.ExternalLogins
                            .AsNoTracking()
                            .Where(_ => _.Provider == provider && _.ProviderSubject == providerSub)
                            .FirstOrDefaultAsync(context.RequestAborted);

                        if (externalLogin is null)
                        {
                            return Results.NotFound();
                        }

                        return Results.Ok(new ExternalLoginDto(externalLogin.Id, externalLogin.UserId, externalLogin.Provider, externalLogin.ProviderSubject));
                    });

                endpoints.MapGet(
                    "/api/user-roles",
                    async (HttpContext context, [FromQuery] Guid userId) =>
                    {
                        var dbContext = context.RequestServices.GetRequiredService<KyivDbContext>();

                        var user = await dbContext.Users
                            .AsNoTracking()
                            .Include(_ => _.UserRoles)
                            .ThenInclude(_ => _.Role)
                            .Where(_ => _.Id == userId)
                            .FirstOrDefaultAsync(context.RequestAborted);

                        if (user is null)
                        {
                            return Results.NotFound();
                        }

                        return Results.Ok(user.UserRoles
                            .Select(_ => new UserRoleDto(_.Id, _.UserId, _.Role!.Name))
                            .ToList());
                    });

                endpoints.MapGet(
                    "/api/tenant",
                    async (HttpContext context, [FromQuery] Guid tenantId) =>
                    {
                        var dbContext = context.RequestServices.GetRequiredService<KyivDbContext>();

                        var tenant = await dbContext.Tenants
                            .AsNoTracking()
                            .FirstOrDefaultAsync(_ => _.Id == tenantId, context.RequestAborted);

                        if (tenant is null)
                        {
                            return Results.NotFound();
                        }

                        return Results.Ok(new TenantDto(tenant.Id, tenant.Name));
                    });

                endpoints.MapGet(
                    "/api/external-logins",
                    async (HttpContext context, [FromQuery] Guid userId) =>
                    {
                        var dbContext = context.RequestServices.GetRequiredService<KyivDbContext>();

                        var user = await dbContext.Users
                            .AsNoTracking()
                            .Include(_ => _.ExternalLogins)
                            .Where(_ => _.Id == userId)
                            .FirstOrDefaultAsync(context.RequestAborted);

                        if (user is null)
                        {
                            return Results.NotFound();
                        }

                        return Results.Ok(user.ExternalLogins
                            .Select(_ => new ExternalLoginDto(_.Id, _.UserId, _.Provider, _.ProviderSubject))
                            .ToList());
                    });

                endpoints.MapGet(
                    "/api/user",
                    async (HttpContext context, [FromQuery] Guid? userId, [FromQuery] string? email) =>
                    {
                        var dbContext = context.RequestServices.GetRequiredService<KyivDbContext>();

                        var userQuery = dbContext.Users
                            .AsNoTracking();

                        if (userId is not null)
                        {
                            userQuery = userQuery.Where(_ => _.Id == userId);
                        }
                        if (email is not null)
                        {
                            userQuery = userQuery.Where(_ => _.Email == email);
                        }

                        var user = await userQuery
                            .FirstOrDefaultAsync(context.RequestAborted);

                        if (user is null)
                        {
                            return Results.NotFound();
                        }

                        return Results.Ok(new UserDto(user.Id, user.TenantId, user.Email, user.DisplayName ?? user.Email, user.IsActive, user.CreatedAt, user.LastModified));
                    });

                endpoints.MapPost(
                    "/api/tenant/create",
                    async (HttpContext context) =>
                    {
                        var dbContext = context.RequestServices.GetRequiredService<KyivDbContext>();

                        var payload = await context.Request.ReadFromJsonAsync<CreateTenantDto>(context.RequestAborted);

                        if (payload is null)
                        {
                            return Results.BadRequest();
                        }

                        var user = await dbContext.Users
                            .Include(_ => _.UserRoles)
                            .ThenInclude(_ => _.Role)
                            .FirstOrDefaultAsync(_ => _.Id == payload.UserId, context.RequestAborted);

                        if (user is null)
                        {
                            return Results.NotFound();
                        }

                        var tenant = new Tenant
                        {
                            Id = Guid.NewGuid(),
                            CreatedAt = DateTime.UtcNow,
                            LastModified = DateTime.UtcNow,
                            Name = payload.TenantName
                        };

                        user.TenantId = tenant.Id;

                        const string AdminRoleName = "Admin";
                        if (!user.UserRoles.Any(_ => _.Role?.Name == AdminRoleName))
                        {
                            var adminRoleId = await dbContext.Roles.Where(_ => _.Name == AdminRoleName).Select(_ => _.Id).FirstOrDefaultAsync(context.RequestAborted);

                            if (adminRoleId != Guid.Empty)
                            {
                                await dbContext.AddAsync(
                                    new UserRole
                                    {
                                        Id = Guid.NewGuid(),
                                        RoleId = adminRoleId,
                                        UserId = user.Id,
                                        LastModified = DateTime.UtcNow,
                                        Created = DateTime.UtcNow
                                    },
                                    context.RequestAborted);
                            }
                        }

                        await dbContext.AddAsync(tenant, context.RequestAborted);
                        dbContext.Update(user);
                        await dbContext.SaveChangesAsync(context.RequestAborted);

                        return Results.Ok();
                    });

                endpoints.MapPost(
                    "/api/user/create",
                    async (HttpContext context) =>
                    {
                        var payload = await context.Request.ReadFromJsonAsync<CreateUserDto>(context.RequestAborted);

                        if (payload is null)
                        {
                            return Results.BadRequest();
                        }

                        var dbContext = context.RequestServices.GetRequiredService<KyivDbContext>();

                        var user = new User
                        {
                            Id = payload.User.Id,
                            Email = payload.User.Email,
                            DisplayName = payload.User.DisplayName,
                            TenantId = payload.User.TenantId,
                            CreatedAt = DateTime.UtcNow,
                            LastModified = DateTime.UtcNow
                        };

                        await dbContext.AddAsync(user, context.RequestAborted);

                        var roles = await dbContext.Roles
                            .Where(_ => payload.Roles.Contains(_.Name))
                            .ToListAsync(context.RequestAborted);
                        foreach (var roleName in payload.Roles)
                        {
                            var userRole = new UserRole
                            {
                                Id = Guid.NewGuid(),
                                UserId = user.Id,
                                RoleId = roles.Single(_ => _.Name == roleName).Id,
                                Created = DateTime.UtcNow,
                                LastModified = DateTime.UtcNow
                            };
                            await dbContext.AddAsync(userRole, context.RequestAborted);
                        }

                        await dbContext.SaveChangesAsync(context.RequestAborted);

                        return Results.Ok();
                    });

                endpoints.MapPost(
                    "/api/user/{userId}/external-logins",
                    async (HttpContext context, Guid userId) =>
                    {
                        var payload = await context.Request.ReadFromJsonAsync<CreateExternalLoginDto>(context.RequestAborted);

                        if (payload is null)
                        {
                            return Results.BadRequest();
                        }

                        var dbContext = context.RequestServices.GetRequiredService<KyivDbContext>();

                        var user = await dbContext.Users
                            .Include(_ => _.ExternalLogins)
                            .FirstOrDefaultAsync(_ => _.Id == userId, context.RequestAborted);

                        if (user is null)
                        {
                            return Results.NotFound();
                        }

                        var exernalLogin = new ExternalLogin
                        {
                            Id = Guid.NewGuid(),
                            UserId = userId,
                            Created = DateTime.UtcNow,
                            LastModified = DateTime.UtcNow,
                            Provider = payload.Provider,
                            ProviderSubject = payload.ProviderSub
                        };

                        await dbContext.AddAsync(exernalLogin, context.RequestAborted);
                        await dbContext.SaveChangesAsync(context.RequestAborted);

                        return Results.Ok();
                    });

                endpoints.MapDelete(
                    "/api/user/{userId}/external-logins/{externalLoginId}",
                    async (HttpContext context, Guid userId, Guid externalLoginId) =>
                    {
                        var dbContext = context.RequestServices.GetRequiredService<KyivDbContext>();

                        var user = await dbContext.Users
                            .Include(_ => _.ExternalLogins)
                            .FirstOrDefaultAsync(_ => _.Id == userId, context.RequestAborted);

                        if (user is null)
                        {
                            return Results.NotFound();
                        }

                        var externalLogin = user.ExternalLogins.FirstOrDefault(_ => _.Id == externalLoginId);

                        if (externalLogin is null)
                        {
                            return Results.NotFound();
                        }

                        user.ExternalLogins.Remove(externalLogin);
                        dbContext.ExternalLogins.Remove(externalLogin);
                        await dbContext.SaveChangesAsync(context.RequestAborted);

                        return Results.Ok();
                    });
            });
    }

}
