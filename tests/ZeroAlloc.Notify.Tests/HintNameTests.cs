using System.Linq;

namespace ZeroAlloc.Notify.Tests;

public class HintNameTests
{
    [Fact]
    public void UnderscoreJoinedNames_DoNotCollide()
    {
        var result = GeneratorRunner.Run("""
            using ZeroAlloc.Notify;
            namespace A_B { [NotifyPropertyChangedAsync] public partial class C { } }
            namespace A { [NotifyPropertyChangedAsync] public partial class B_C { } }
            """);

        result.AssertBuilds();
        Assert.Equal(
            new[] { "A.B_C.Notify.g.cs", "A_B.C.Notify.g.cs" },
            result.GeneratedSources.Select(s => s.HintName).OrderBy(n => n, System.StringComparer.Ordinal),
            System.StringComparer.Ordinal);
    }

    [Fact]
    public void GenericAndNonGenericTypesOfTheSameName_BothGenerate()
    {
        var result = GeneratorRunner.Run("""
            using ZeroAlloc.Notify;
            namespace N
            {
                [NotifyPropertyChangedAsync] public partial class Foo { }
                [NotifyPropertyChangedAsync] public partial class Foo<T> { }
            }
            """);

        result.AssertBuilds();
        Assert.Equal(
            new[] { "N.Foo.Notify.g.cs", "N.Foo`1.Notify.g.cs" },
            result.GeneratedSources.Select(s => s.HintName).OrderBy(n => n, System.StringComparer.Ordinal),
            System.StringComparer.Ordinal);
    }

    [Fact]
    public void NestedTypesOfTheSameName_BothGenerate()
    {
        var result = GeneratorRunner.Run("""
            using ZeroAlloc.Notify;
            namespace N
            {
                public partial class A { [NotifyPropertyChangedAsync] public partial class Foo { } }
                public partial class B { [NotifyPropertyChangedAsync] public partial class Foo { } }
            }
            """);

        result.AssertBuilds();
        Assert.Equal(
            new[] { "N.A+Foo.Notify.g.cs", "N.B+Foo.Notify.g.cs" },
            result.GeneratedSources.Select(s => s.HintName).OrderBy(n => n, System.StringComparer.Ordinal),
            System.StringComparer.Ordinal);
    }

    [Fact]
    public void ClassWithSeveralNotifyAttributes_GeneratesOneFile()
    {
        var result = GeneratorRunner.Run("""
            using ZeroAlloc.Notify;
            namespace N
            {
                [NotifyPropertyChangedAsync, NotifyPropertyChangingAsync]
                [NotifyCollectionChangedAsync, NotifyDataErrorInfoAsync]
                public partial class Foo
                {
                    public bool HasErrors => false;
                    public System.Collections.IEnumerable GetErrors(string? propertyName) => System.Array.Empty<object>();
                }
            }
            """);

        result.AssertBuilds();
        Assert.Equal("N.Foo.Notify.g.cs", Assert.Single(result.GeneratedSources).HintName);
    }

    [Fact]
    public void GlobalNamespaceType_HasNoNamespacePart()
    {
        var result = GeneratorRunner.Run("""
            using ZeroAlloc.Notify;
            [NotifyPropertyChangedAsync] public partial class Foo { }
            """);

        result.AssertBuilds();
        Assert.Equal("Foo.Notify.g.cs", Assert.Single(result.GeneratedSources).HintName);
    }

    [Fact]
    public void VerbatimAndNonAsciiNames_AreWrittenWithoutEscapes()
    {
        var result = GeneratorRunner.Run("""
            using ZeroAlloc.Notify;
            namespace @event.Café
            {
                [NotifyPropertyChangedAsync] public partial class @class { }
                [NotifyPropertyChangedAsync] public partial class Ünïcode { }
            }
            """);

        result.AssertBuilds();
        Assert.Equal(
            new[] { "event.Café.class.Notify.g.cs", "event.Café.Ünïcode.Notify.g.cs" },
            result.GeneratedSources.Select(s => s.HintName).OrderBy(n => n, System.StringComparer.Ordinal),
            System.StringComparer.Ordinal);
    }
}
