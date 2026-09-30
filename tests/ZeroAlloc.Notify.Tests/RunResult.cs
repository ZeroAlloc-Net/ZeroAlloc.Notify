using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace ZeroAlloc.Notify.Tests;

/// <param name="GeneratedSources">The files the generator added.</param>
/// <param name="GeneratorDiagnostics">The diagnostics the generator reported.</param>
/// <param name="CompilationErrors">The errors of the project with the generated files added.</param>
/// <param name="Exception">The exception the generator threw, if any.</param>
internal sealed record RunResult(
    ImmutableArray<GeneratedSourceResult> GeneratedSources,
    ImmutableArray<Diagnostic> GeneratorDiagnostics,
    ImmutableArray<Diagnostic> CompilationErrors,
    System.Exception? Exception)
{
    public string[] HintNames =>
        GeneratedSources.Select(s => s.HintName).OrderBy(n => n, System.StringComparer.Ordinal).ToArray();

    public string Source(string hintName) =>
        GeneratedSources.Single(s => string.Equals(s.HintName, hintName, System.StringComparison.Ordinal)).SourceText.ToString();

    /// <summary>Fails with the errors listed when the generator threw or the project does not build.</summary>
    public void AssertBuilds()
    {
        Assert.Null(Exception);
        Assert.True(
            CompilationErrors.IsEmpty,
            string.Join("\n", CompilationErrors.Select(d => d.ToString())));
    }
}
