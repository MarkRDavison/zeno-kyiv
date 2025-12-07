using Microsoft.CodeAnalysis;

namespace mark.davison.common.generators.CQRS;

[Generator(LanguageNames.CSharp)]
public class IncrementalMetadataCQRSGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Create a provider that gives the Compilation for this project
        var compilationProvider = context.CompilationProvider;

        // You can use Select to transform it
        var assemblyTypesProvider = compilationProvider
            .Select((compilation, cancellationToken) =>
        {
            var allTypes = new List<INamedTypeSymbol>();

            foreach (var reference in compilation.References)
            {
                if (compilation.GetAssemblyOrModuleSymbol(reference) is IAssemblySymbol asm)
                {
                    CollectTypes(asm.GlobalNamespace, allTypes);
                }
            }

            return allTypes;
        });

        // Eventually register a source output
        context.RegisterSourceOutput(assemblyTypesProvider, (spc, allTypes) =>
        {
            var commandDtos = new List<INamedTypeSymbol>();
            var queryDtos = new List<INamedTypeSymbol>();
            var commandHandlers = new List<INamedTypeSymbol>();
            var queryHandlers = new List<INamedTypeSymbol>();
            var queryValidators = new List<INamedTypeSymbol>();
            var queryProcessors = new List<INamedTypeSymbol>();
            var commandValidators = new List<INamedTypeSymbol>();
            var commandProcessors = new List<INamedTypeSymbol>();

            var activities = new List<CQRSSourceGeneratorActivity>();

            foreach (var type in allTypes)
            {
                // Skip if not a named type
                if (type.TypeKind != TypeKind.Class)
                {
                    continue;
                }

                if (!type.ContainingNamespace.ToDisplayString().StartsWith("mark.davison"))
                {
                    continue;
                }

                // Check for ICommand<T> or IQuery<T>
                foreach (var iface in type.AllInterfaces)
                {
                    if (iface.IsGenericType && iface.ConstructedFrom.ToDisplayString() == "mark.davison.common.CQRS.ICommand<TCommand, TResponse>")
                    {
                        commandDtos.Add(type);
                        if (ProcessCommand(type, iface) is { } activity)
                        {
                            activities.Add(activity);
                        }
                    }

                    if (iface.IsGenericType && iface.ConstructedFrom.ToDisplayString() == "mark.davison.common.CQRS.IQuery<TQuery, TResponse>")
                    {
                        queryDtos.Add(type);
                        if (ProcessQuery(type, iface) is { } activity)
                        {
                            activities.Add(activity);
                        }
                    }

                    if (iface.IsGenericType && iface.ConstructedFrom.ToDisplayString() == "mark.davison.common.server.abstractions.CQRS.ICommandHandler<in TCommand, TCommandResult>")
                    {
                        commandHandlers.Add(type);
                    }

                    if (iface.IsGenericType && iface.ConstructedFrom.ToDisplayString() == "mark.davison.common.server.abstractions.CQRS.IQueryHandler<in TQuery, TQueryResult>")
                    {
                        queryHandlers.Add(type);
                    }

                    if (iface.IsGenericType && iface.ConstructedFrom.ToDisplayString() == "mark.davison.common.server.abstractions.CQRS.IQueryHandler<TRequest, TResponse>")
                    {
                        queryValidators.Add(type);
                        if (ProcessQueryValidator(type, iface) is { } activity)
                        {
                            activities.Add(activity);
                        }
                    }

                    if (iface.IsGenericType && iface.ConstructedFrom.ToDisplayString() == "mark.davison.common.server.abstractions.CQRS.IQueryProcessor<TRequest, TResponse>")
                    {
                        queryProcessors.Add(type);
                        if (ProcessQueryProcessor(type, iface) is { } activity)
                        {
                            activities.Add(activity);
                        }
                    }

                    if (iface.IsGenericType && iface.ConstructedFrom.ToDisplayString() == "mark.davison.common.server.abstractions.CQRS.ICommandHandler<TRequest, TResponse>")
                    {
                        commandValidators.Add(type);
                    }

                    if (iface.IsGenericType && iface.ConstructedFrom.ToDisplayString() == "mark.davison.common.server.abstractions.CQRS.ICommandProcessor<TRequest, TResponse>")
                    {
                        commandProcessors.Add(type);
                    }
                }
            }

            foreach (var t in queryDtos)
            {
                var queryInterface = t.AllInterfaces.Single(_ =>
                    _.IsGenericType &&
                    _.ConstructedFrom.ToDisplayString() == "mark.davison.common.CQRS.IQuery<TQuery, TResponse>");

                var requestType = queryInterface.TypeArguments[0];
                var responseType = queryInterface.TypeArguments[0];

                if (!SymbolEqualityComparer.Default.Equals(t, requestType))
                {
                    continue;
                }

                string? path = null;
                string[]? roles = null;
                bool allowAnonymous = false;

                foreach (var attr in t.GetAttributes())
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
            }


            // TODO: Generate DI / endpoint code using these lists
        });
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
