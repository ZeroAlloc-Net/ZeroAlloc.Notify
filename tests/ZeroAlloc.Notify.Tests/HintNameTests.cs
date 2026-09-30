using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using ZeroAlloc.Notify.Generator;

namespace ZeroAlloc.Notify.Tests;

public class HintNameTests
{
    [Fact]
    public void UnderscoreJoinedNames_DoNotCollide()
    {
        var result = Run("""
            using ZeroAlloc.Notify;
            namespace A_B { [NotifyPropertyChangedAsync] public partial class C { } }
            namespace A { [NotifyPropertyChangedAsync] public partial class B_C { } }
            """);

        Assert.Null(result.Exception);
        Assert.Equal(
            new[] { "A.B_C.Notify.g.cs", "A_B.C.Notify.g.cs" },
            result.GeneratedSources.Select(s => s.HintName).OrderBy(n => n, System.StringComparer.Ordinal),
            System.StringComparer.Ordinal);
    }

    [Fact]
    public void GenericAndNonGenericTypesOfTheSameName_BothGenerate()
    {
        var result = Run("""
            using ZeroAlloc.Notify;
            namespace N
            {
                [NotifyPropertyChangedAsync] public partial class Foo { }
                [NotifyPropertyChangedAsync] public partial class Foo<T> { }
            }
            """);

        Assert.Null(result.Exception);
        Assert.Equal(
            new[] { "N.Foo.Notify.g.cs", "N.Foo`1.Notify.g.cs" },
            result.GeneratedSources.Select(s => s.HintName).OrderBy(n => n, System.StringComparer.Ordinal),
            System.StringComparer.Ordinal);
    }

    [Fact]
    public void NestedTypesOfTheSameName_BothGenerate()
    {
        var result = Run("""
            using ZeroAlloc.Notify;
            namespace N
            {
                public partial class A { [NotifyPropertyChangedAsync] public partial class Foo { } }
                public partial class B { [NotifyPropertyChangedAsync] public partial class Foo { } }
            }
            """);

        Assert.Null(result.Exception);
        Assert.Equal(
            new[] { "N.A+Foo.Notify.g.cs", "N.B+Foo.Notify.g.cs" },
            result.GeneratedSources.Select(s => s.HintName).OrderBy(n => n, System.StringComparer.Ordinal),
            System.StringComparer.Ordinal);
    }

    [Fact]
    public void ClassWithSeveralNotifyAttributes_GeneratesOneFile()
    {
        var result = Run("""
            using ZeroAlloc.Notify;
            namespace N
            {
                [NotifyPropertyChangedAsync, NotifyPropertyChangingAsync]
                [NotifyCollectionChangedAsync, NotifyDataErrorInfoAsync]
                public partial class Foo { }
            }
            """);

        Assert.Null(result.Exception);
        Assert.Equal("N.Foo.Notify.g.cs", Assert.Single(result.GeneratedSources).HintName);
    }

    [Fact]
    public void GlobalNamespaceType_HasNoNamespacePart()
    {
        var result = Run("""
            using ZeroAlloc.Notify;
            [NotifyPropertyChangedAsync] public partial class Foo { }
            """);

        Assert.Equal("Foo.Notify.g.cs", Assert.Single(result.GeneratedSources).HintName);
    }

    [Fact]
    public void VerbatimAndNonAsciiNames_AreWrittenWithoutEscapes()
    {
        var result = Run("""
            using ZeroAlloc.Notify;
            namespace @event.Café
            {
                [NotifyPropertyChangedAsync] public partial class @class { }
                [NotifyPropertyChangedAsync] public partial class Ünïcode { }
            }
            """);

        Assert.Null(result.Exception);
        Assert.Equal(
            new[] { "event.Café.class.Notify.g.cs", "event.Café.Ünïcode.Notify.g.cs" },
            result.GeneratedSources.Select(s => s.HintName).OrderBy(n => n, System.StringComparer.Ordinal),
            System.StringComparer.Ordinal);
    }

    private static GeneratorRunResult Run(string source)
    {
        var refs = new List<MetadataReference>(Basic.Reference.Assemblies.Net90.References.All)
        {
            MetadataReference.CreateFromFile(typeof(NotifyPropertyChangedAsyncAttribute).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(ZeroAlloc.AsyncEvents.AsyncEventHandler<>).Assembly.Location),
        };
        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { CSharpSyntaxTree.ParseText(source) },
            refs,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var driver = CSharpGeneratorDriver.Create(new NotifyGenerator()).RunGenerators(compilation);
        return driver.GetRunResult().Results.Single();
    }
}
