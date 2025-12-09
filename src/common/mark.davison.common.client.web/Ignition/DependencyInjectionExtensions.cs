using mark.davison.common.client.web.CQRS;
using System.Reflection;

namespace mark.davison.common.client.web.Ignition;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection UseAuthentication(this IServiceCollection services, string clientName)
    {
        services
            .AddAuthorizationCore()
            .AddCascadingAuthenticationState()
            .AddSingleton<AuthenticationStateProvider, CommonAuthenticationStateProvider>()
            .AddSingleton<IClientNavigationManager, ClientNavigationManager>()
            .AddSingleton<IAuthenticationService>(_ => new AuthenticationService(_.GetRequiredService<IHttpClientFactory>(), clientName));

        return services;
    }

    public static IServiceCollection UseCommonClient(this IServiceCollection services)
    {
        services
            .AddMudServices();

        return services;
    }

    // TODO: To source generator
    private static void AddSingleton<TAbstraction, TImplementation>(IServiceCollection services)
        where TAbstraction : class
        where TImplementation : class, TAbstraction
    {
        services.AddScoped<TAbstraction, TImplementation>();
    }

    private static void InvokeRequestResponse(IServiceCollection services, MethodInfo methodInfo, Type genericType, Type type)
    {
        Type genericType2 = genericType;
        Type[] genericArguments = type.GetInterfaces().First((__) => __.IsGenericType && __.GetGenericTypeDefinition() == genericType2).GetGenericArguments();
        if (genericArguments.Length == 2)
        {
            Type type2 = genericArguments[0];
            Type type3 = genericArguments[1];
            Type type4 = genericType2.MakeGenericType(type2, type3);
            MethodInfo methodInfo2 = methodInfo.MakeGenericMethod(type4, type);
            object[] parameters = new IServiceCollection[1] { services };
            methodInfo2.Invoke(null, parameters);
        }
    }

    private static void InvokeAction(IServiceCollection services, MethodInfo methodInfo, Type genericType, Type type)
    {
        Type genericType2 = genericType;
        Type[] genericArguments = type.GetInterfaces().First((__) => __.IsGenericType && __.GetGenericTypeDefinition() == genericType2).GetGenericArguments();
        if (genericArguments.Length == 1)
        {
            Type type2 = genericArguments[0];
            Type type3 = genericType2.MakeGenericType(type2);
            MethodInfo methodInfo2 = methodInfo.MakeGenericMethod(type3, type);
            object[] parameters = new IServiceCollection[1] { services };
            methodInfo2.Invoke(null, parameters);
        }
    }

    public static IServiceCollection UseClientCQRS(this IServiceCollection services, params Type[] types)
    {
        services.AddSingleton<IQueryDispatcher, QueryDispatcher>();
        services.AddSingleton<ICommandDispatcher, CommandDispatcher>();
        services.AddSingleton<IActionDispatcher, ActionDispatcher>();
        services.AddSingleton<ICQRSDispatcher, CQRSDispatcher>();
        var method = typeof(DependencyInjectionExtensions).GetMethod(nameof(AddSingleton), BindingFlags.Static | BindingFlags.NonPublic)!;


        Type commandHandlerType = typeof(ICommandHandler<,>);
        foreach (Type item in (from _ in types.SelectMany((_) => _.Assembly.ExportedTypes)
                               where _.GetInterfaces().Any((__) => __.IsGenericType && __.GetGenericTypeDefinition() == commandHandlerType)
                               select _).ToList())
        {
            InvokeRequestResponse(services, method, commandHandlerType, item);
        }

        Type queryHandlerType = typeof(IQueryHandler<,>);
        foreach (Type item2 in (from _ in types.SelectMany((_) => _.Assembly.ExportedTypes)
                                where _.GetInterfaces().Any((__) => __.IsGenericType && __.GetGenericTypeDefinition() == queryHandlerType)
                                select _).ToList())
        {
            InvokeRequestResponse(services, method, queryHandlerType, item2);
        }

        Type actionHandlerType = typeof(IActionHandler<>);
        foreach (Type item3 in (from _ in types.SelectMany((_) => _.Assembly.ExportedTypes)
                                where _.GetInterfaces().Any((__) => __.IsGenericType && __.GetGenericTypeDefinition() == actionHandlerType)
                                select _).ToList())
        {
            InvokeAction(services, method, actionHandlerType, item3);
        }

        return services;
    }

    public static IServiceCollection UseClientRepository(
        this IServiceCollection services,
        string httpClientName,
        string localBffRoot)
    {
        services
            .AddSingleton<IClientHttpRepository>(_ =>
            {
                var authStateService = _.GetRequiredService<IAuthenticationService>();

                var jsRuntime = _.GetRequiredService<IJSRuntime>();

                if (jsRuntime is IJSInProcessRuntime jsInProcessRuntime)
                {
                    string bffRoot = jsInProcessRuntime.Invoke<string>("GetBffUri", null);

                    if (!string.IsNullOrEmpty(bffRoot))
                    {
                        localBffRoot = bffRoot;
                    }
                }

                var clientHttpRepository = new ClientHttpRepository(
                        localBffRoot,
                        _.GetRequiredService<IHttpClientFactory>().CreateClient(httpClientName),
                        _.GetRequiredService<ILogger<ClientHttpRepository>>());

                clientHttpRepository.OnInvalidHttpResponse += async (object? sender, InvalidHttpResponseEventArgs e) =>
                {
                    if (e.Status == HttpStatusCode.Unauthorized)
                    {
                        Console.Error.WriteLine("Received 401 - Validating auth state");
                        await authStateService.EvaluateAuthentication();
                    }
                    else
                    {
                        Console.Error.WriteLine("Received HttpStatusCode.{0} - Not handling...", e.Status);
                    }
                };

                return clientHttpRepository;
            })
            .AddHttpClient(httpClientName)
            .ConfigureHttpClient(_ =>
            {
                _.BaseAddress = new Uri(localBffRoot);
            })
            .AddHttpMessageHandler(_ => new CookieHandler());

        return services;
    }

}
