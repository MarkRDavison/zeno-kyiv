namespace mark.davison.common.generators.CQRS;
/*
[Generator(LanguageNames.CSharp)]
public class IncrementalCQRSGenerator : IIncrementalGenerator
{
    public const string GeneratorNamespace = "mark.davison.common.source.generators.CQRS";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {


        context
            // .AddEmbeddedAttributeDefinition() TODO: Once in dotnet 10, need to add [global::Microsoft.CodeAnalysis.EmbeddedAttribute] to the attribute
            .RegisterPostInitializationOutput(static _ =>
            {
                _.AddSource("UseCQRSServerAttribute.g.cs", SourceText.From(CQRSSources.UseCQRSServerAttribute(GeneratorNamespace), Encoding.UTF8));
                _.AddSource("UseCQRSClientAttribute.g.cs", SourceText.From(CQRSSources.UseCQRSClientAttribute(GeneratorNamespace), Encoding.UTF8));
            });

        var current = context.SyntaxProvider
            .CreateSyntaxProvider<CQRSSourceGeneratorActivity?>(
                predicate: static (SyntaxNode s, CancellationToken token) => s is ClassDeclarationSyntax c,
                transform: static (GeneratorSyntaxContext ctx, CancellationToken token) =>
                {
                    if (ctx.SemanticModel.GetDeclaredSymbol(ctx.Node, token) is not INamedTypeSymbol symbol)
                    {
                        return default;
                    }

                    if (ParseMarkerAttribute(ctx, symbol) is { } attributeData)
                    {
                        return attributeData;
                    }

                    if (ParseRequestInterface(ctx, symbol) is { } requestData)
                    {
                        return requestData;
                    }

                    if (ParseProcessor(ctx, symbol) is { } processorData)
                    {
                        return processorData;
                    }

                    if (ParseValidator(ctx, symbol) is { } validatorData)
                    {
                        return validatorData;
                    }

                    if (ParseHandler(ctx, symbol) is { } handlerData)
                    {
                        return handlerData;
                    }

                    return null;
                })
            .Where(static m => m is not null)
            .Collect();

        context.RegisterSourceOutput(current, static (spc, source) => Execute(source, spc));
    }

    private static CQRSSourceGeneratorActivity? ParseRequestInterface(GeneratorSyntaxContext ctx, INamedTypeSymbol symbol)
    {
        if (symbol.AllInterfaces.Length == 0)
        {
            return null;
        }

        var commandInterfaceType = ctx.SemanticModel.Compilation
            .GetTypeByMetadataName("mark.davison.common.CQRS.ICommand`2");

        var queryInterfaceType = ctx.SemanticModel.Compilation
            .GetTypeByMetadataName("mark.davison.common.CQRS.IQuery`2");

        var postRequestAttributeType = ctx.SemanticModel.Compilation
            .GetTypeByMetadataName("mark.davison.common.CQRS.PostRequestAttribute");

        var getRequestAttributeType = ctx.SemanticModel.Compilation
            .GetTypeByMetadataName("mark.davison.common.CQRS.GetRequestAttribute");

        if (commandInterfaceType is null ||
            queryInterfaceType is null ||
            postRequestAttributeType is null ||
            getRequestAttributeType is null)
        {
            return null;
        }

        var definitionType = new List<Tuple<INamedTypeSymbol, CQRSActivityType, INamedTypeSymbol>>
            {
                new(commandInterfaceType, CQRSActivityType.Command, postRequestAttributeType),
                new(queryInterfaceType, CQRSActivityType.Query, getRequestAttributeType),
                // TODO: Action, ResponseAction
            };

        var attributes = symbol.GetAttributes();

        foreach (var (definitionSymbol, activity, routeAttribute) in definitionType)
        {
            foreach (var i in symbol.AllInterfaces)
            {
                if (!i.IsGenericType ||
                    i.TypeArguments.Length != 2)
                {
                    continue;
                }

                if (!SymbolEqualityComparer.Default.Equals(i.ConstructedFrom, definitionSymbol))
                {
                    continue;
                }

                var requestType = i.TypeArguments[0];
                var responseType = i.TypeArguments[1];

                if (!SymbolEqualityComparer.Default.Equals(requestType, symbol))
                {
                    // TODO: Analyzer warning for invalid first parameter
                    continue;
                }

                string? endpoint = null;
                bool allowAnonymoous = false;

                var endpointAttribute = attributes.FirstOrDefault(_ =>
                {
                    // TODO: Why doesn't this work
                    if (!SymbolEqualityComparer.Default.Equals(routeAttribute, _.AttributeClass))
                    {
                        return false;
                    }

                    return true;
                });

                if (endpointAttribute is not null)
                {
                    if (endpointAttribute.NamedArguments.Any(na => na.Key == "Path"))
                    {
                        var pathArg = endpointAttribute.NamedArguments.Single(na => na.Key == "Path");

                        if (pathArg.Value.Value is string pathValue && !string.IsNullOrEmpty(pathValue))
                        {
                            endpoint = pathValue;
                        }
                    }
                    if (endpointAttribute.NamedArguments.Any(na => na.Key == "AllowAnonymous"))
                    {
                        var pathArg = endpointAttribute.NamedArguments.Single(na => na.Key == "AllowAnonymous");

                        if (pathArg.Value.Value is bool aa)
                        {
                            allowAnonymoous = aa;
                        }
                    }
                }

                return new CQRSSourceGeneratorActivity(
                    true,
                    activity,
                    SourceGeneratorHelpers.GetFullyQualifiedName(requestType),
                    SourceGeneratorHelpers.GetFullyQualifiedName(responseType),
                    string.Empty,
                    null,
                    null,
                    endpoint,
                    null,
                    allowAnonymoous,
                    []);
            }
        }

        return null;
    }

    private static CQRSSourceGeneratorActivity? ParseProcessor(GeneratorSyntaxContext ctx, INamedTypeSymbol symbol)
    {
        if (symbol.AllInterfaces.Length == 0)
        {
            return null;
        }

        var commandProcessorInterfaceType = ctx.SemanticModel.Compilation
            .GetTypeByMetadataName("mark.davison.common.server.abstractions.CQRS.ICommandProcessor`2");

        var queryProcessorInterfaceType = ctx.SemanticModel.Compilation
            .GetTypeByMetadataName("mark.davison.common.server.abstractions.CQRS.IQueryProcessor`2");

        if (commandProcessorInterfaceType is null ||
            queryProcessorInterfaceType is null)
        {
            return null;
        }

        foreach (var i in symbol.AllInterfaces)
        {
            if (!i.IsGenericType || i.TypeArguments.Length != 2)
            {
                continue;
            }

            var requestType = i.TypeArguments[0];
            var responseType = i.TypeArguments[1];

            if (SymbolEqualityComparer.Default.Equals(i.ConstructedFrom, commandProcessorInterfaceType))
            {
                return new CQRSSourceGeneratorActivity(
                    false,
                    CQRSActivityType.Command,
                    SourceGeneratorHelpers.GetFullyQualifiedName(requestType),
                    SourceGeneratorHelpers.GetFullyQualifiedName(responseType),
                    string.Empty,
                    null,
                    SourceGeneratorHelpers.GetFullyQualifiedName(symbol),
                    null,
                    null,
                    false,
                    []);
            }

            if (SymbolEqualityComparer.Default.Equals(i.ConstructedFrom, queryProcessorInterfaceType))
            {
                return new CQRSSourceGeneratorActivity(
                    false,
                    CQRSActivityType.Query,
                    SourceGeneratorHelpers.GetFullyQualifiedName(requestType),
                    SourceGeneratorHelpers.GetFullyQualifiedName(responseType),
                    string.Empty,
                    null,
                    SourceGeneratorHelpers.GetFullyQualifiedName(symbol),
                    null,
                    null,
                    false,
                    []);
            }

        }

        return null;
    }

    private static CQRSSourceGeneratorActivity? ParseValidator(GeneratorSyntaxContext ctx, INamedTypeSymbol symbol)
    {
        if (symbol.AllInterfaces.Length == 0)
        {
            return null;
        }

        var commandValidatorInterfaceType = ctx.SemanticModel.Compilation
            .GetTypeByMetadataName("mark.davison.common.server.abstractions.CQRS.ICommandValidator`2");

        var queryValidatorInterfaceType = ctx.SemanticModel.Compilation
            .GetTypeByMetadataName("mark.davison.common.server.abstractions.CQRS.IQueryValidator`2");

        if (commandValidatorInterfaceType is null ||
            queryValidatorInterfaceType is null)
        {
            return null;
        }

        foreach (var i in symbol.AllInterfaces)
        {
            if (!i.IsGenericType || i.TypeArguments.Length != 2)
            {
                continue;
            }

            var requestType = i.TypeArguments[0];
            var responseType = i.TypeArguments[1];

            if (SymbolEqualityComparer.Default.Equals(i.ConstructedFrom, commandValidatorInterfaceType))
            {
                return new CQRSSourceGeneratorActivity(
                    false,
                    CQRSActivityType.Command,
                    SourceGeneratorHelpers.GetFullyQualifiedName(requestType),
                    SourceGeneratorHelpers.GetFullyQualifiedName(responseType),
                    string.Empty,
                    SourceGeneratorHelpers.GetFullyQualifiedName(symbol),
                    null,
                    null,
                    null,
                    false,
                    []);
            }

            if (SymbolEqualityComparer.Default.Equals(i.ConstructedFrom, queryValidatorInterfaceType))
            {
                return new CQRSSourceGeneratorActivity(
                    false,
                    CQRSActivityType.Query,
                    SourceGeneratorHelpers.GetFullyQualifiedName(requestType),
                    SourceGeneratorHelpers.GetFullyQualifiedName(responseType),
                    string.Empty,
                    SourceGeneratorHelpers.GetFullyQualifiedName(symbol),
                    null,
                    null,
                    null,
                    false,
                    []);
            }

        }

        return null;
    }

    private static CQRSSourceGeneratorActivity? ParseHandler(GeneratorSyntaxContext ctx, INamedTypeSymbol symbol)
    {
        if (symbol.AllInterfaces.Length == 0)
        {
            return null;
        }

        // TODO: Web vs server
        var commandHandlerInterfaceType = ctx.SemanticModel.Compilation
            .GetTypeByMetadataName("mark.davison.common.server.abstractions.CQRS.ICommandHandler`2");

        var queryHandlerInterfaceType = ctx.SemanticModel.Compilation
            .GetTypeByMetadataName("mark.davison.common.server.abstractions.CQRS.IQueryHandler`2");

        if (commandHandlerInterfaceType is null ||
            queryHandlerInterfaceType is null)
        {
            return null;
        }

        foreach (var i in symbol.AllInterfaces)
        {
            if (!i.IsGenericType || i.TypeArguments.Length != 2)
            {
                continue;
            }

            var requestType = i.TypeArguments[0];
            var responseType = i.TypeArguments[1];

            if (SymbolEqualityComparer.Default.Equals(i.ConstructedFrom, commandHandlerInterfaceType))
            {
                return new CQRSSourceGeneratorActivity(
                    false,
                    CQRSActivityType.Command,
                    SourceGeneratorHelpers.GetFullyQualifiedName(requestType),
                    SourceGeneratorHelpers.GetFullyQualifiedName(responseType),
                    SourceGeneratorHelpers.GetFullyQualifiedName(symbol),
                    null,
                    null,
                    null,
                    null,
                    false,
                    []);
            }

            if (SymbolEqualityComparer.Default.Equals(i.ConstructedFrom, queryHandlerInterfaceType))
            {
                return new CQRSSourceGeneratorActivity(
                    false,
                    CQRSActivityType.Query,
                    SourceGeneratorHelpers.GetFullyQualifiedName(requestType),
                    SourceGeneratorHelpers.GetFullyQualifiedName(responseType),
                    SourceGeneratorHelpers.GetFullyQualifiedName(symbol),
                    null,
                    null,
                    null,
                    null,
                    false,
                    []);
            }

        }

        return null;
    }

    private static CQRSSourceGeneratorActivity? ParseMarkerAttribute(GeneratorSyntaxContext ctx, INamedTypeSymbol symbol)
    {
        var attributes = symbol.GetAttributes();

        if (attributes.Length > 0)
        {
            var markerAttributeType = ctx.SemanticModel.Compilation
                .GetTypeByMetadataName($"{GeneratorNamespace}.UseCQRSServerAttribute")
                    ?? throw new InvalidOperationException("Cannot find UseCQRSServerAttribute type");

            AttributeData? markerAttribute = null;

            foreach (var attr in attributes)
            {
                if (SymbolEqualityComparer.Default.Equals(attr.AttributeClass, markerAttributeType))
                {
                    markerAttribute = attr;
                    break;
                }
            }

            if (markerAttribute?.AttributeClass is not null)
            {
                return new CQRSSourceGeneratorActivity(
                    false,
                    CQRSActivityType.Command,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    null,
                    null,
                    null,
                    SourceGeneratorHelpers.GetNamespace(symbol),
                    false,
                    []);
            }

        }

        return null;
    }

    private static void Execute(ImmutableArray<CQRSSourceGeneratorActivity?> source, SourceProductionContext context)
    {
        var merged = source
            .OfType<CQRSSourceGeneratorActivity>()
            .GroupBy(_ => _.Key)
            .Select(MergeActivities)
            .ToImmutableArray();

        if (CreateServerDependencyInjectionExtensions(merged) is { } diSource && !string.IsNullOrEmpty(diSource))
        {
            context.AddSource("CQRSServerDependecyInjectionExtensions.g.cs", diSource);
        }

        if (CreateServerEndpointRouteExtensions(merged) is { } erSource && !string.IsNullOrEmpty(erSource))
        {
            context.AddSource("GenerateCQRSEndpointRouteExtensions.g.cs", erSource);
        }
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
                    builder.AppendLine($"                    _.GetRequiredService<I{type}Processor<{activity.Request},{activity.Response}>()");
                }
                else
                {
                    builder.AppendLine($"                    _.GetRequiredService<I{type}Processor<{activity.Request},{activity.Response}>(),");
                    builder.AppendLine($"                    _.GetRequiredService<I{type}Validator<{activity.Request},{activity.Response}>()");
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
                builder.AppendLine($"                    var request = await WebUtilities.GetRequestFromQuery<{activity.Request},{activity.Response}>(context.Request);");
            }

            builder.AppendLine($"                    return await dispatcher.Dispatch<{activity.Request},{activity.Response}>(request, cancellationToken);");

            if (activity.AllowAnonymous)
            {
                builder.AppendLine($"                }});");
            }
            else
            {
                builder.AppendLine($"                }}).RequireAuthorization();");
            }
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
        builder.AppendLine("        public static void MapCQRSEndpoints(this IEndpointRouteBuilder endpoints)");
        builder.AppendLine("        {");
        CreateServerEndpointRouteExtensionsForActivityType(source, CQRSActivityType.Command, builder);
        CreateServerEndpointRouteExtensionsForActivityType(source, CQRSActivityType.Query, builder);
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
}
*/