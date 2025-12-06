using System.Reflection;

namespace mark.davison.common.generators.tests;

public static class GeneratorTestHelpers
{
    private static void AddAssemblyAndReferences(
        Assembly asm,
        HashSet<string> visitedPaths,
        List<MetadataReference> refs)
    {
        if (asm is null)
        {
            return;
        }

        string path;
        try
        {
            path = asm.Location;
        }
        catch
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        if (!visitedPaths.Add(path))
        {
            return;
        }

        refs.Add(MetadataReference.CreateFromFile(path));

        foreach (var name in asm.GetReferencedAssemblies())
        {
            try
            {
                var child = System.Reflection.Assembly.Load(name);
                AddAssemblyAndReferences(child, visitedPaths, refs);
            }
            catch
            {
            }
        }
    }

    public static GeneratorDriverRunResult RunSourceGenerator<TGenerator>(
        string source,
        IEnumerable<Type> typesToReference,
        bool suppressErrors = false)
        where TGenerator : IIncrementalGenerator, new()
    {
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source);

        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var references = new List<MetadataReference>();

        foreach (var t in typesToReference.Concat([typeof(object), typeof(Enumerable), typeof(Console)]))
        {
            AddAssemblyAndReferences(t.Assembly, visited, references);
        }

        var compilation = CSharpCompilation.Create(
            assemblyName: "Tests",
            references: [
                ..references
            ],
            syntaxTrees: [syntaxTree],
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new TGenerator();

        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        driver = driver.RunGenerators(compilation);

        var diagnostics = compilation.GetDiagnostics();

        var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();

        if (suppressErrors && errors.Count is not 0)
        {
            var message = string.Join("\n", errors.Select(d => d.ToString()));
            throw new InvalidOperationException($"Compilation has errors:\n{message}");
        }

        return driver.GetRunResult();
    }
}
