using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace ZeroAlloc.Notify.Tests;

/// <summary>
/// Attribute uses the generator cannot act on. Nothing is generated for them, and each gets a
/// warning that points at the cause, so the mistake does not first show up as a missing member
/// at a call site. The project still builds when nothing uses the missing members.
/// </summary>
public class MisuseTests
{
    private const string Header = "using ZeroAlloc.Notify;\nnamespace N;\n";

    [Theory]
    [InlineData("NotifyPropertyChangedAsync")]
    [InlineData("NotifyPropertyChangingAsync")]
    [InlineData("NotifyCollectionChangedAsync")]
    [InlineData("NotifyDataErrorInfoAsync")]
    public void NonPartialClass_ReportsZan004AndGeneratesNothing(string attribute)
    {
        var source = Header + $$"""
            [{{attribute}}]
            public class Vm { [ObservableProperty] private int _count; }
            [NotifyPropertyChangedAsync] public partial class Other { }
            """;
        var result = GeneratorRunner.Run(source);

        result.AssertBuilds();
        Assert.Equal(new[] { "N.Other.Notify.g.cs" }, result.HintNames);
        var diagnostic = Assert.Single(result.GeneratorDiagnostics);
        Assert.Equal("ZAN004", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Equal(
            "The notification members of class 'N.Vm' are not generated because it is not partial",
            diagnostic.GetMessage(CultureInfo.InvariantCulture));
        NestedClassTests.AssertLocatedOn(diagnostic, source, "Vm");
    }

    [Fact]
    public void NonPartialClassWithSeveralNotifyAttributes_ReportsZan004Once()
    {
        var result = GeneratorRunner.Run(Header + """
            [NotifyPropertyChangedAsync, NotifyPropertyChangingAsync]
            public class Vm { }
            """);

        Assert.Equal(new[] { "ZAN004" }, result.GeneratorDiagnostics.Select(d => d.Id), System.StringComparer.Ordinal);
        Assert.Empty(result.GeneratedSources);
    }

    [Fact]
    public void NonPartialNestedClass_ReportsZan004RatherThanZan001()
    {
        var result = GeneratorRunner.Run(Header + """
            public class Outer
            {
                [NotifyPropertyChangedAsync]
                public class Vm { }
            }
            """);

        Assert.Equal(new[] { "ZAN004" }, result.GeneratorDiagnostics.Select(d => d.Id), System.StringComparer.Ordinal);
        Assert.Empty(result.GeneratedSources);
    }

    [Theory]
    [InlineData("public partial record Vm")]
    [InlineData("public record Vm")]
    [InlineData("public partial record class Vm")]
    [InlineData("public sealed partial record Vm(int Id)")]
    public void Record_ReportsZan005AndGeneratesNothing(string declaration)
    {
        var source = Header + "[NotifyPropertyChangedAsync]\n" + declaration + " { [ObservableProperty] private int _count; }\n"
            + "[NotifyPropertyChangedAsync] public partial class Other { }";
        var result = GeneratorRunner.Run(source);

        result.AssertBuilds();
        Assert.Equal(new[] { "N.Other.Notify.g.cs" }, result.HintNames);
        var diagnostic = Assert.Single(result.GeneratorDiagnostics);
        Assert.Equal("ZAN005", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Equal(
            "The notification members of record 'N.Vm' are not generated because Notify attributes are not supported on records",
            diagnostic.GetMessage(CultureInfo.InvariantCulture));
        Assert.True(diagnostic.Location.IsInSource);
        Assert.Equal(source.IndexOf(" Vm", System.StringComparison.Ordinal) + 1, diagnostic.Location.SourceSpan.Start);
        Assert.Equal(2, diagnostic.Location.SourceSpan.Length);
    }

    [Fact]
    public void ObservablePropertyWithoutANotifyAttribute_ReportsZan006OnEachField()
    {
        var source = Header + """
            public partial class Vm
            {
                [ObservableProperty] private int _count;
                [ObservableProperty] private string _name = "";
                private int _plain;
            }
            """;
        var result = GeneratorRunner.Run(source);

        result.AssertBuilds();
        Assert.Empty(result.GeneratedSources);
        Assert.Equal(new[] { "ZAN006", "ZAN006" }, result.GeneratorDiagnostics.Select(d => d.Id), System.StringComparer.Ordinal);
        var diagnostic = result.GeneratorDiagnostics.OrderBy(d => d.Location.SourceSpan.Start).First();
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Equal(
            "No property is generated for field '_count' because its class 'N.Vm' has no Notify attribute such as [NotifyPropertyChangedAsync]",
            diagnostic.GetMessage(CultureInfo.InvariantCulture));
        var span = diagnostic.Location.SourceSpan;
        Assert.Equal("_count", source.Substring(span.Start, span.Length));
    }

    [Fact]
    public void ObservablePropertyWithOnlyInvokeSequentially_ReportsZan006()
    {
        var result = GeneratorRunner.Run(Header + """
            [InvokeSequentially]
            public partial class Vm { [ObservableProperty] private int _count; }
            """);

        Assert.Equal(new[] { "ZAN006" }, result.GeneratorDiagnostics.Select(d => d.Id), System.StringComparer.Ordinal);
        Assert.Empty(result.GeneratedSources);
    }

    [Fact]
    public void ObservablePropertyOnSeveralVariablesOfOneField_ReportsZan006OnEach()
    {
        var result = GeneratorRunner.Run(Header + """
            public partial class Vm { [ObservableProperty] private int _x, _y; }
            """);

        Assert.Equal(2, result.GeneratorDiagnostics.Count(d => string.Equals(d.Id, "ZAN006", System.StringComparison.Ordinal)));
    }

    [Theory]
    // The class's own warning covers its fields.
    [InlineData("[NotifyPropertyChangedAsync] public class Vm { [ObservableProperty] private int _count; }", "ZAN004")]
    [InlineData("[NotifyPropertyChangedAsync] public partial record Vm { [ObservableProperty] private int _count; }", "ZAN005")]
    public void ObservablePropertyInAClassWithAWarning_ReportsOnlyTheClassWarning(string declaration, string id)
    {
        var result = GeneratorRunner.Run(Header + declaration);

        Assert.Equal(new[] { id }, result.GeneratorDiagnostics.Select(d => d.Id), System.StringComparer.Ordinal);
    }

    [Fact]
    public void ObservablePropertyInANotifyClass_ReportsNothing()
    {
        var result = GeneratorRunner.Run(Header + """
            [NotifyPropertyChangingAsync]
            public partial class Vm { [ObservableProperty] private int _count; }
            public partial class Outer
            {
                [NotifyPropertyChangedAsync]
                public partial class Inner { [ObservableProperty] private int _count; }
            }
            """);

        result.AssertBuilds();
        Assert.Empty(result.GeneratorDiagnostics);
        Assert.Equal(new[] { "N.Outer+Inner.Notify.g.cs", "N.Vm.Notify.g.cs" }, result.HintNames);
    }

    [Fact]
    public void Warnings_AreSourceLocations_SoPragmasApply()
    {
        var result = GeneratorRunner.Run(Header + """
            [NotifyPropertyChangedAsync] public class A { }
            [NotifyPropertyChangedAsync] public partial record B { }
            public partial class C { [ObservableProperty] private int _count; }
            """);

        Assert.Equal(new[] { "ZAN004", "ZAN005", "ZAN006" }, result.GeneratorDiagnostics.Select(d => d.Id).OrderBy(i => i, System.StringComparer.Ordinal), System.StringComparer.Ordinal);
        Assert.All(result.GeneratorDiagnostics, d => Assert.True(d.Location.IsInSource));
    }
}
