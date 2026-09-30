using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using ZeroAlloc.Notify.Generator;

namespace ZeroAlloc.Notify.Tests;

/// <summary>
/// Runs the generator over source files and compiles the result, so a test can check what was
/// generated, what the generator reported and whether the project builds.
/// </summary>
internal static class GeneratorRunner
{
    public static readonly CSharpParseOptions ParseOptions = new(LanguageVersion.Latest);

    public static RunResult Run(string source) => Run(("Source.cs", source));

    public static RunResult Run(params (string Path, string Source)[] files)
    {
        var compilation = CreateCompilation(files);
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            new[] { new NotifyGenerator().AsSourceGenerator() }, parseOptions: ParseOptions);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var output, out _);
        var result = driver.GetRunResult().Results.Single();
        return new RunResult(
            result.GeneratedSources,
            result.Diagnostics,
            output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToImmutableArray(),
            result.Exception);
    }

    public static CSharpCompilation CreateCompilation(params (string Path, string Source)[] files)
    {
        var refs = new List<MetadataReference>(Basic.Reference.Assemblies.Net90.References.All)
        {
            MetadataReference.CreateFromFile(typeof(NotifyPropertyChangedAsyncAttribute).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(ZeroAlloc.AsyncEvents.AsyncEventHandler<>).Assembly.Location),
        };
        return CSharpCompilation.Create(
            "TestAssembly",
            files.Select(f => CSharpSyntaxTree.ParseText(f.Source, ParseOptions, f.Path)),
            refs,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }
}
