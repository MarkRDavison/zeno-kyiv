using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Text;

namespace mark.davison.common.generators.CQRS;

[Generator(LanguageNames.CSharp)]
public class IncrementalMetadataCQRSGenerator : IIncrementalGenerator
{
    public const string GeneratorNamespace = "mark.davison.common.source.generators.CQRS";
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context
            .RegisterPostInitializationOutput(static _ =>
            {
                // .AddEmbeddedAttributeDefinition() TODO: Once in dotnet 10, need to add [global::Microsoft.CodeAnalysis.EmbeddedAttribute] to the attribute
                _.AddSource("UseCQRSServerAttribute.g.cs", SourceText.From(CQRSSources.UseCQRSServerAttribute(GeneratorNamespace), Encoding.UTF8));
                _.AddSource("UseCQRSClientAttribute.g.cs", SourceText.From(CQRSSources.UseCQRSClientAttribute(GeneratorNamespace), Encoding.UTF8));
            });

        var assemblyTypesProvider = context.CompilationProvider
            .Select((compilation, cancellationToken) =>
            {
                var allTypes = new List<INamedTypeSymbol>();
                CollectTypes(compilation.GlobalNamespace, allTypes);

                foreach (var reference in compilation.References)
                {
                    if (compilation.GetAssemblyOrModuleSymbol(reference) is IAssemblySymbol asm)
                    {
                        CollectTypes(asm.GlobalNamespace, allTypes);
                    }
                }

                return allTypes;
            });

        context.RegisterSourceOutput(assemblyTypesProvider, (spc, allTypes) =>
        {
            var activities = new List<CQRSSourceGeneratorActivity>();

            foreach (var type in allTypes)
            {
                if (type.TypeKind is not TypeKind.Class ||
                    type.IsAbstract)
                {
                    continue;
                }

                if (type.GetAttributes().Any(_ => _.AttributeClass?.ToDisplayString() == "mark.davison.common.source.generators.CQRS.UseCQRSServerAttribute"))
                {
                    activities.Add(new CQRSSourceGeneratorActivity(
                        false,
                        CQRSActivityType.Command,
                        string.Empty,
                        string.Empty,
                        null,
                        null,
                        null,
                        null,
                        SourceGeneratorHelpers.GetNamespace(type),
                        false,
                        []));
                }

                foreach (var iface in type.AllInterfaces)
                {
                    if (iface.IsGenericType && iface.ConstructedFrom.ToDisplayString() == "mark.davison.common.CQRS.ICommand<TCommand, TResponse>")
                    {
                        if (ProcessCommand(type, iface) is { } activity)
                        {
                            activities.Add(activity);
                        }
                    }

                    if (iface.IsGenericType && iface.ConstructedFrom.ToDisplayString() == "mark.davison.common.CQRS.IQuery<TQuery, TResponse>")
                    {
                        if (ProcessQuery(type, iface) is { } activity)
                        {
                            activities.Add(activity);
                        }
                    }

                    if (iface.IsGenericType && iface.ConstructedFrom.ToDisplayString() == "mark.davison.common.server.abstractions.CQRS.ICommandHandler<in TCommand, TCommandResult>")
                    {

                    }

                    if (iface.IsGenericType && iface.ConstructedFrom.ToDisplayString() == "mark.davison.common.server.abstractions.CQRS.IQueryHandler<in TQuery, TQueryResult>")
                    {

                    }

                    if (iface.IsGenericType && iface.ConstructedFrom.ToDisplayString() == "mark.davison.common.server.abstractions.CQRS.IQueryHandler<TRequest, TResponse>")
                    {
                        if (ProcessQueryValidator(type, iface) is { } activity)
                        {
                            activities.Add(activity);
                        }
                    }

                    if (iface.IsGenericType && iface.ConstructedFrom.ToDisplayString() == "mark.davison.common.server.abstractions.CQRS.IQueryProcessor<TRequest, TResponse>")
                    {
                        if (ProcessQueryProcessor(type, iface) is { } activity)
                        {
                            activities.Add(activity);
                        }
                    }

                    if (iface.IsGenericType && iface.ConstructedFrom.ToDisplayString() == "mark.davison.common.server.abstractions.CQRS.ICommandHandler<TRequest, TResponse>")
                    {
                        if (ProcessCommandValidator(type, iface) is { } activity)
                        {
                            activities.Add(activity);
                        }
                    }

                    if (iface.IsGenericType && iface.ConstructedFrom.ToDisplayString() == "mark.davison.common.server.abstractions.CQRS.ICommandProcessor<TRequest, TResponse>")
                    {
                        if (ProcessCommandProcessor(type, iface) is { } activity)
                        {
                            activities.Add(activity);
                        }
                    }
                }
            }

            var merged = activities
                .OfType<CQRSSourceGeneratorActivity>()
                .GroupBy(_ => _.Key)
                .Select(MergeActivities)
                .ToImmutableArray();

            if (CreateServerDependencyInjectionExtensions(merged) is { } diSource && !string.IsNullOrEmpty(diSource))
            {
                spc.AddSource("CQRSServerDependecyInjectionExtensions.g.cs", diSource);
            }

            if (CreateServerEndpointRouteExtensions(merged) is { } erSource && !string.IsNullOrEmpty(erSource))
            {
                spc.AddSource("GenerateCQRSEndpointRouteExtensions.g.cs", erSource);
            }
        });
    }

    private static void CreateServerDependencyRegistrationsForActivityType(
        ImmutableArray<CQRSSourceGeneratorActivity> source,
        CQRSActivityType type,
        StringBuilder builder)
    {
        foreach (var activity in source.Where(_ => _.IsRequestDefinition && _.Type == type))
        {
            if (!string.IsNullOrEmpty(activity.Handler))
            {
                // If a handler is defined it takes priority.
                builder.AppendLine($"            services.AddScoped<I{type}Handler<{activity.Request},{activity.Response}>,{activity.Handler}>();");
            }
            else if (!string.IsNullOrEmpty(activity.Processor))
            {
                builder.AppendLine($"            services.AddScoped<I{type}Processor<{activity.Request},{activity.Response}>,{activity.Processor}>();");

                if (!string.IsNullOrEmpty(activity.Validator))
                {
                    builder.AppendLine($"            services.AddScoped<I{type}Validator<{activity.Request},{activity.Response}>,{activity.Validator}>();");
                }

                builder.AppendLine($"            services.AddScoped<I{type}Handler<{activity.Request},{activity.Response}>>(_ =>");
                builder.AppendLine($"            {{");
                builder.AppendLine($"                return new mark.davison.common.server.CQRS.ValidateAndProcess{type}Handler<{activity.Request},{activity.Response}>(");

                if (string.IsNullOrEmpty(activity.Validator))
                {
                    builder.AppendLine($"                    _.GetRequiredService<I{type}Processor<{activity.Request},{activity.Response}>>()");
                }
                else
                {
                    builder.AppendLine($"                    _.GetRequiredService<I{type}Processor<{activity.Request},{activity.Response}>>(),");
                    builder.AppendLine($"                    _.GetRequiredService<I{type}Validator<{activity.Request},{activity.Response}>>()");
                }

                builder.AppendLine($"                );");
                builder.AppendLine($"            }});");
            }
            else
            {
                // No way to handle/process/validate.
                continue;
            }

            builder.AppendLine();
        }
    }

    private static void CreateServerEndpointRouteExtensionsForActivityType(
        ImmutableArray<CQRSSourceGeneratorActivity> source,
        CQRSActivityType type,
        StringBuilder builder)
    {
        foreach (var activity in source.Where(_ => _.IsRequestDefinition && _.Type == type))
        {
            if (type == CQRSActivityType.Command)
            {
                builder.AppendLine("            endpoints.MapPost(");
            }
            else if (type == CQRSActivityType.Query)
            {
                builder.AppendLine("            endpoints.MapGet(");
            }

            builder.AppendLine($"                \"/api/{activity.Endpoint}\",");
            builder.AppendLine($"                async (HttpContext context, CancellationToken cancellationToken) =>");
            builder.AppendLine($"                {{");
            builder.AppendLine($"                    var dispatcher = context.RequestServices.GetRequiredService<I{type}Dispatcher>();");

            if (type == CQRSActivityType.Command)
            {
                builder.AppendLine($"                    var request = await WebUtilities.GetRequestFromBody<{activity.Request},{activity.Response}>(context.Request);");
            }
            else
            {
                builder.AppendLine($"                    var request = WebUtilities.GetRequestFromQuery<{activity.Request},{activity.Response}>(context.Request);");
            }

            builder.AppendLine($"                    return await dispatcher.Dispatch<{activity.Request},{activity.Response}>(request, cancellationToken);");

            if (activity.AllowAnonymous)
            {
                builder.AppendLine($"                }});");
            }
            else if (activity.RequiredRoles.Count > 0)
            {
                var roles = string.Join("", activity.RequiredRoles.Select(_ => $".RequireRole(\"{_}\")"));
                builder.AppendLine($"                }}).RequireAuthorization(p => p{roles});");
            }
            else
            {
                builder.AppendLine($"                }}).RequireAuthorization();");
            }
            builder.AppendLine();
        }
    }

    private static string CreateServerEndpointRouteExtensions(ImmutableArray<CQRSSourceGeneratorActivity> source)
    {
        var builder = new StringBuilder();

        var markerActivity = source.FirstOrDefault(_ => !string.IsNullOrEmpty(_.RootNamespace));

        if (string.IsNullOrEmpty(markerActivity?.RootNamespace))
        {
            return string.Empty;
        }

        builder.AppendLine("using mark.davison.common.CQRS;");
        builder.AppendLine("using mark.davison.common.server.CQRS;");
        builder.AppendLine("using mark.davison.common.server.abstractions.CQRS;");
        builder.AppendLine("using mark.davison.common.server.Utilities;");
        builder.AppendLine("using Microsoft.AspNetCore.Builder;");
        builder.AppendLine("using Microsoft.Extensions.DependencyInjection;");
        builder.AppendLine();
        builder.AppendLine($"namespace {markerActivity!.RootNamespace}");
        builder.AppendLine("{");
        builder.AppendLine();
        builder.AppendLine("    public static class GenerateEndpointRouteExtensions");
        builder.AppendLine("    {");
        builder.AppendLine();
        builder.AppendLine("        public static IEndpointRouteBuilder MapCQRSEndpoints(this IEndpointRouteBuilder endpoints)");
        builder.AppendLine("        {");
        CreateServerEndpointRouteExtensionsForActivityType(source, CQRSActivityType.Command, builder);
        CreateServerEndpointRouteExtensionsForActivityType(source, CQRSActivityType.Query, builder);
        builder.AppendLine("            return endpoints;");
        builder.AppendLine("        }");
        builder.AppendLine("    }");
        builder.AppendLine("}");

        return builder.ToString();
    }

    private static string CreateServerDependencyInjectionExtensions(ImmutableArray<CQRSSourceGeneratorActivity> source)
    {
        var builder = new StringBuilder();

        var markerActivity = source.FirstOrDefault(_ => !string.IsNullOrEmpty(_.RootNamespace));

        if (string.IsNullOrEmpty(markerActivity?.RootNamespace))
        {
            return string.Empty;
        }

        builder.AppendLine("using mark.davison.common.CQRS;");
        builder.AppendLine("using mark.davison.common.server.CQRS;");
        builder.AppendLine("using mark.davison.common.server.abstractions.CQRS;");
        builder.AppendLine("using mark.davison.common.server.Utilities;");
        builder.AppendLine("using Microsoft.Extensions.DependencyInjection;");
        builder.AppendLine();
        builder.AppendLine($"namespace {markerActivity!.RootNamespace}");
        builder.AppendLine("{");
        builder.AppendLine();
        builder.AppendLine("    public static class CQRSDependecyInjectionExtensions");
        builder.AppendLine("    {");
        builder.AppendLine();
        builder.AppendLine("        public static IServiceCollection AddCQRSServer(this IServiceCollection services)");
        builder.AppendLine("        {");
        builder.AppendLine();
        builder.AppendLine("            services.AddScoped<mark.davison.common.CQRS.IQueryDispatcher, mark.davison.common.server.CQRS.QueryDispatcher>();");
        builder.AppendLine("            services.AddScoped<mark.davison.common.CQRS.ICommandDispatcher, mark.davison.common.server.CQRS.CommandDispatcher>();");
        builder.AppendLine();

        CreateServerDependencyRegistrationsForActivityType(source, CQRSActivityType.Command, builder);
        CreateServerDependencyRegistrationsForActivityType(source, CQRSActivityType.Query, builder);

        builder.AppendLine("            return services;");
        builder.AppendLine("        }");
        builder.AppendLine("    }");
        builder.AppendLine("}");

        return builder.ToString();
    }

    private static CQRSSourceGeneratorActivity MergeActivities(IGrouping<string, CQRSSourceGeneratorActivity> grouping)
    {
        var root = grouping.First();

        foreach (var activity in grouping.Skip(1))
        {
            root = new CQRSSourceGeneratorActivity(
                true,
                root.Type is null ? activity.Type : root.Type,
                string.IsNullOrEmpty(root.Request) ? activity.Request : root.Request,
                string.IsNullOrEmpty(root.Response) ? activity.Response : root.Response,
                string.IsNullOrEmpty(root.Handler) ? activity.Handler : root.Handler,
                string.IsNullOrEmpty(root.Validator) ? activity.Validator : root.Validator,
                string.IsNullOrEmpty(root.Processor) ? activity.Processor : root.Processor,
                string.IsNullOrEmpty(root.Endpoint) ? activity.Endpoint : root.Endpoint,
                string.IsNullOrEmpty(root.RootNamespace) ? activity.RootNamespace : root.RootNamespace,
                root.AllowAnonymous || activity.AllowAnonymous,
                new HashSet<string>([.. root.RequiredRoles, .. activity.RequiredRoles]).ToList());
        }

        return root;
    }

    private CQRSSourceGeneratorActivity? ProcessCommand(INamedTypeSymbol type, INamedTypeSymbol iface)
    {
        var requestType = iface.TypeArguments[0];
        var responseType = iface.TypeArguments[1];

        if (!SymbolEqualityComparer.Default.Equals(type, requestType))
        {
            return null;
        }

        string? path = null;
        string[] roles = [];
        bool allowAnonymous = false;

        foreach (var attr in type.GetAttributes())
        {
            if (attr.AttributeClass?.ToDisplayString() == "mark.davison.common.CQRS.PostRequestAttribute")
            {
                foreach (var arg in attr.NamedArguments)
                {
                    if (arg.Key == "Path" && arg.Value.Value is string p)
                    {
                        path = p;
                    }
                    if (arg.Key == "RequireRoles" && arg.Value.Values is { } arr)
                    {
                        roles = arr.Select(v => v.Value?.ToString() ?? string.Empty).ToArray();
                    }
                    if (arg.Key == "AllowAnonymous" && arg.Value.Value is bool aa)
                    {
                        allowAnonymous = aa;
                    }
                }
            }
        }

        return new CQRSSourceGeneratorActivity(
            true,
            CQRSActivityType.Command,
            SourceGeneratorHelpers.GetFullyQualifiedName(requestType),
            SourceGeneratorHelpers.GetFullyQualifiedName(responseType),
            string.Empty,
            null,
            null,
            path,
            null,
            allowAnonymous,
            [.. roles]);
    }

    private CQRSSourceGeneratorActivity? ProcessCommandProcessor(INamedTypeSymbol type, INamedTypeSymbol iface)
    {
        var requestType = iface.TypeArguments[0];
        var responseType = iface.TypeArguments[1];

        return new CQRSSourceGeneratorActivity(
            false,
            CQRSActivityType.Command,
            SourceGeneratorHelpers.GetFullyQualifiedName(requestType),
            SourceGeneratorHelpers.GetFullyQualifiedName(responseType),
            string.Empty,
            null,
            SourceGeneratorHelpers.GetFullyQualifiedName(type),
            null,
            null,
            false,
            []);
    }

    private CQRSSourceGeneratorActivity? ProcessCommandValidator(INamedTypeSymbol type, INamedTypeSymbol iface)
    {
        var requestType = iface.TypeArguments[0];
        var responseType = iface.TypeArguments[1];

        return new CQRSSourceGeneratorActivity(
            false,
            CQRSActivityType.Command,
            SourceGeneratorHelpers.GetFullyQualifiedName(requestType),
            SourceGeneratorHelpers.GetFullyQualifiedName(responseType),
            string.Empty,
            SourceGeneratorHelpers.GetFullyQualifiedName(type),
            null,
            null,
            null,
            false,
            []);
    }

    private CQRSSourceGeneratorActivity? ProcessQueryProcessor(INamedTypeSymbol type, INamedTypeSymbol iface)
    {
        var requestType = iface.TypeArguments[0];
        var responseType = iface.TypeArguments[1];

        return new CQRSSourceGeneratorActivity(
            false,
            CQRSActivityType.Query,
            SourceGeneratorHelpers.GetFullyQualifiedName(requestType),
            SourceGeneratorHelpers.GetFullyQualifiedName(responseType),
            string.Empty,
            null,
            SourceGeneratorHelpers.GetFullyQualifiedName(type),
            null,
            null,
            false,
            []);
    }

    private CQRSSourceGeneratorActivity? ProcessQueryValidator(INamedTypeSymbol type, INamedTypeSymbol iface)
    {
        var requestType = iface.TypeArguments[0];
        var responseType = iface.TypeArguments[1];

        return new CQRSSourceGeneratorActivity(
            false,
            CQRSActivityType.Query,
            SourceGeneratorHelpers.GetFullyQualifiedName(requestType),
            SourceGeneratorHelpers.GetFullyQualifiedName(responseType),
            string.Empty,
            SourceGeneratorHelpers.GetFullyQualifiedName(type),
            null,
            null,
            null,
            false,
            []);
    }

    private CQRSSourceGeneratorActivity? ProcessQuery(INamedTypeSymbol type, INamedTypeSymbol iface)
    {
        var requestType = iface.TypeArguments[0];
        var responseType = iface.TypeArguments[1];

        if (!SymbolEqualityComparer.Default.Equals(type, requestType))
        {
            return null;
        }

        string? path = null;
        string[] roles = [];
        bool allowAnonymous = false;

        foreach (var attr in type.GetAttributes())
        {
            if (attr.AttributeClass?.ToDisplayString() == "mark.davison.common.CQRS.GetRequestAttribute")
            {
                foreach (var arg in attr.NamedArguments)
                {
                    if (arg.Key == "Path" && arg.Value.Value is string p)
                    {
                        path = p;
                    }
                    if (arg.Key == "RequireRoles" && arg.Value.Values is { } arr)
                    {
                        roles = arr.Select(v => v.Value?.ToString() ?? string.Empty).ToArray();
                    }
                    if (arg.Key == "AllowAnonymous" && arg.Value.Value is bool aa)
                    {
                        allowAnonymous = aa;
                    }
                }
            }
        }

        return new CQRSSourceGeneratorActivity(
            true,
            CQRSActivityType.Query,
            SourceGeneratorHelpers.GetFullyQualifiedName(requestType),
            SourceGeneratorHelpers.GetFullyQualifiedName(responseType),
            string.Empty,
            null,
            null,
            path,
            null,
            allowAnonymous,
            [.. roles]);
    }

    private static void CollectTypes(INamespaceSymbol ns, List<INamedTypeSymbol> output, string? namespacePrefix = null)
    {
        // Only process types in this namespace if it matches the prefix (or if no prefix)
        foreach (var t in ns.GetTypeMembers())
        {
            if (namespacePrefix is null || t.ContainingNamespace.ToDisplayString().StartsWith(namespacePrefix))
            {
                output.Add(t);
            }
        }

        // Recurse into child namespaces
        foreach (var child in ns.GetNamespaceMembers())
        {
            // Skip child namespace if prefix is set and it doesn’t match
            if (namespacePrefix is null || child.ToDisplayString().StartsWith(namespacePrefix))
            {
                CollectTypes(child, output, namespacePrefix);
            }
        }
    }
}
