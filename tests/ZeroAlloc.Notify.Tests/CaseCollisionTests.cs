using System.Linq;
using Microsoft.CodeAnalysis;

namespace ZeroAlloc.Notify.Tests;

/// <summary>
/// Roslyn compares hint names ignoring case. Of two classes whose names differ only in case, the
/// one declared first keeps its file and the later one gets ZAN003, instead of the generator
/// failing for the whole project.
/// </summary>
public class CaseCollisionTests
{
    private const string Header = "using ZeroAlloc.Notify;\nnamespace N;\n";

    [Fact]
    public void ClassesDifferingOnlyInCase_ReportZan003OnTheLaterOne()
    {
        var source = Header + """
            [NotifyPropertyChangedAsync] public partial class Foo { }
            [NotifyPropertyChangedAsync] public partial class foo { }
            [NotifyPropertyChangedAsync] public partial class Other { }
            """;
        var result = GeneratorRunner.Run(source);

        Assert.Null(result.Exception);
        var diagnostic = Assert.Single(result.GeneratorDiagnostics);
        Assert.Equal("ZAN003", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Equal(
            "The notification members of class 'N.foo' are not generated because its file name 'N.foo.Notify.g.cs' differs only in case from that of class 'N.Foo'",
            diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture));
        NestedClassTests.AssertLocatedOn(diagnostic, source, "foo");
        Assert.Equal(new[] { "N.Foo.Notify.g.cs", "N.Other.Notify.g.cs" }, result.HintNames);
    }

    [Fact]
    public void ThreeNamesDifferingOnlyInCase_GenerateOneFile()
    {
        var result = GeneratorRunner.Run(Header + """
            [NotifyPropertyChangedAsync] public partial class App { }
            [NotifyPropertyChangedAsync] public partial class APP { }
            [NotifyPropertyChangedAsync] public partial class app { }
            """);

        Assert.Equal(new[] { "ZAN003", "ZAN003" }, result.GeneratorDiagnostics.Select(d => d.Id), System.StringComparer.Ordinal);
        Assert.Equal(new[] { "N.App.Notify.g.cs" }, result.HintNames);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AcrossFiles_TheEarlierFilePathWins_WhateverTheCompilationOrder(bool reversed)
    {
        var a = ("A.cs", Header + "[NotifyPropertyChangedAsync] public partial class foo { }");
        var b = ("B.cs", Header + "[NotifyPropertyChangedAsync] public partial class Foo { }");
        var result = reversed ? GeneratorRunner.Run(b, a) : GeneratorRunner.Run(a, b);

        var diagnostic = Assert.Single(result.GeneratorDiagnostics);
        Assert.Equal("ZAN003", diagnostic.Id);
        Assert.Equal("B.cs", diagnostic.Location.SourceTree!.FilePath);
        Assert.Equal(new[] { "N.foo.Notify.g.cs" }, result.HintNames);
    }

    [Fact]
    public void ClassSkippedForAnotherDiagnostic_DoesNotTakeTheName()
    {
        var result = GeneratorRunner.Run(Header + """
            [NotifyPropertyChangedAsync] file partial class Foo { }
            [NotifyPropertyChangedAsync] public partial class foo { }
            """);

        Assert.Equal(new[] { "ZAN002" }, result.GeneratorDiagnostics.Select(d => d.Id), System.StringComparer.Ordinal);
        Assert.Equal(new[] { "N.foo.Notify.g.cs" }, result.HintNames);
    }
}
