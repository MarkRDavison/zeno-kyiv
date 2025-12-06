using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Text;

namespace mark.davison.common.generators.Configuration;


[Generator(LanguageNames.CSharp)]
public class AppSettingsSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var sources = context.SyntaxProvider
            .CreateSyntaxProvider<AppSettingInfo?>(
                predicate: static (SyntaxNode s, CancellationToken token) => s is ClassDeclarationSyntax,
                transform: static (GeneratorSyntaxContext ctx, CancellationToken token) =>
                {
                    if (ctx.SemanticModel.GetDeclaredSymbol(ctx.Node, token) is not INamedTypeSymbol symbol)
                    {
                        return default;
                    }

                    if (ParseAppSettingsClass(ctx, symbol) is { } info)
                    {
                        return info;
                    }

                    return default;
                })
            .Where(static m => m is not null)
            .Collect();

        context.RegisterSourceOutput(sources, static (spc, source) => Execute(source, spc));
    }

    private static AppSettingInfo RecursiveGetSettings(INamedTypeSymbol parentSymbol, INamedTypeSymbol appSettingSymbol, INamedTypeSymbol namedTypeSymbol)
    {
        var setting = new AppSettingInfo
        {
            IsRoot = false,
            Name = namedTypeSymbol.Name,
            Namespace = SourceGeneratorHelpers.GetNamespace(namedTypeSymbol)
        };

        foreach (var m in namedTypeSymbol.GetMembers())
        {
            if (m is IPropertySymbol propSymbol)
            {
                if (propSymbol.Type is INamedTypeSymbol childSymbol)
                {
                    if (childSymbol.AllInterfaces.Length > 0)
                    {
                        if (childSymbol.AllInterfaces.Any(_ => SymbolEqualityComparer.Default.Equals(appSettingSymbol, _)))
                        {
                            var childSetting = RecursiveGetSettings(namedTypeSymbol, appSettingSymbol, childSymbol);
                            childSetting.PropertyName = propSymbol.Name;
                            setting.Children.Add(childSetting);
                        }
                    }
                }
            }
        }

        return setting;
    }

    private static AppSettingInfo? ParseAppSettingsClass(GeneratorSyntaxContext ctx, INamedTypeSymbol symbol)
    {
        if (symbol.AllInterfaces.Length is 0)
        {
            return null;
        }

        var rootAppSettingsInterfaceType = ctx.SemanticModel.Compilation
            .GetTypeByMetadataName("mark.davison.common.server.abstractions.Configuration.IRootAppSettings");

        var appSettingsInterfaceType = ctx.SemanticModel.Compilation
            .GetTypeByMetadataName("mark.davison.common.server.abstractions.Configuration.IAppSettings");

        if (rootAppSettingsInterfaceType is null ||
            appSettingsInterfaceType is null)
        {
            return null;
        }

        foreach (var i in symbol.AllInterfaces)
        {
            var isRoot = SymbolEqualityComparer.Default.Equals(rootAppSettingsInterfaceType, i);

            if (!isRoot)
            {
                continue;
            }

            var rootSettings = new List<AppSettingInfo>();

            foreach (var m in symbol.GetMembers())
            {
                if (m is IPropertySymbol propSymbol)
                {
                    var origDef = propSymbol.OriginalDefinition.GetType();
                    if (propSymbol.Type is INamedTypeSymbol childSymbol)
                    {
                        if (childSymbol.AllInterfaces.Length > 0)
                        {
                            if (childSymbol.AllInterfaces.Any(_ => SymbolEqualityComparer.Default.Equals(appSettingsInterfaceType, _)))
                            {
                                var setting = RecursiveGetSettings(symbol, appSettingsInterfaceType, childSymbol);
                                setting.PropertyName = propSymbol.Name;
                                rootSettings.Add(setting);
                            }
                        }
                    }
                }
            }

            return new AppSettingInfo
            {
                IsRoot = isRoot,
                Name = symbol.Name,
                Namespace = SourceGeneratorHelpers.GetNamespace(symbol),
                Children = [.. rootSettings]
            };
        }

        return null;
    }

    private static void Recurser(StringBuilder builder, AppSettingInfo info, string currentSection)
    {
        currentSection += $".GetSection(\"{info.PropertyName}\")";

        builder.AppendLine($"            services.Configure<{info.FullyQualifiedName}>({currentSection});");

        foreach (var c in info.Children)
        {
            Recurser(builder, c, currentSection);
        }
    }

    private static void Execute(ImmutableArray<AppSettingInfo?> source, SourceProductionContext spc)
    {
        var root = source.Where(s => s is not null && s.IsRoot).Single() ?? throw new InvalidOperationException();

        var builder = new StringBuilder();

        builder.AppendLine($"namespace {root.Namespace}");
        builder.AppendLine("{");
        builder.AppendLine($"    public static partial class AppSettingExtensions");
        builder.AppendLine($"    {{");
        builder.AppendLine($"        public static {root.FullyQualifiedName} BindAppSettings(this IServiceCollection services, IConfiguration configuration)");
        builder.AppendLine("        {");
        builder.AppendLine($"            var settings = new {root.FullyQualifiedName}();");
        builder.AppendLine($"            IConfigurationSection section = configuration.GetSection(settings.SECTION);");
        builder.AppendLine();
        builder.AppendLine($"            services.Configure<{root.FullyQualifiedName}>(section);");

        foreach (var c in root.Children)
        {
            Recurser(builder, c, "section");
        }

        builder.AppendLine("            ");
        builder.AppendLine("            section.Bind(settings);");
        builder.AppendLine("            return settings;");
        builder.AppendLine("        }");
        builder.AppendLine("    }");
        builder.AppendLine("}");

        var generatedSource = builder.ToString();

        spc.AddSource("AppSettingsExtensions.g.cs", generatedSource);
    }
}
