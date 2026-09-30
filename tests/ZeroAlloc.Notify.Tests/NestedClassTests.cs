using System.Linq;
using Microsoft.CodeAnalysis;

namespace ZeroAlloc.Notify.Tests;

/// <summary>
/// Nested and generic classes get their members on the class itself, inside partial
/// declarations of every containing type. Each case compiles code that uses those members on
/// the real class, so a member generated anywhere else fails the build.
/// </summary>
public class NestedClassTests
{
    private const string Usings = "using System.Threading.Tasks;\nusing ZeroAlloc.Notify;\n";

    [Fact]
    public void SameNamedNestedClasses_AreEachGeneratedIntoTheirOwnClass()
    {
        var result = GeneratorRunner.Run(Usings + """
            namespace N
            {
                public partial class A
                {
                    [NotifyPropertyChangedAsync]
                    public partial class Foo { [ObservableProperty] private int _count; }
                }
                public partial class B
                {
                    [NotifyPropertyChangedAsync]
                    public partial class Foo { [ObservableProperty] private string _name = ""; }
                }
                public static class Use
                {
                    public static async Task Run(A.Foo a, B.Foo b)
                    {
                        a.PropertyChangedAsync += (e, ct) => default;
                        b.PropertyChangedAsync += (e, ct) => default;
                        await a.SetCountAsync(a.Count + 1);
                        await b.SetNameAsync(b.Name + "x");
                    }
                }
            }
            """);

        result.AssertBuilds();
        Assert.Empty(result.GeneratorDiagnostics);
        Assert.Equal(new[] { "N.A+Foo.Notify.g.cs", "N.B+Foo.Notify.g.cs" }, result.HintNames);
    }

    [Fact]
    public void NestedClass_NextToATopLevelClassOfTheSameName_BothBuild()
    {
        var result = GeneratorRunner.Run(Usings + """
            namespace N
            {
                [NotifyPropertyChangedAsync]
                public partial class Foo { [ObservableProperty] private int _count; }
                public partial class Outer
                {
                    [NotifyPropertyChangingAsync]
                    public partial class Foo { [ObservableProperty] private int _count; }
                }
                public static class Use
                {
                    public static async Task Run(Foo top, Outer.Foo nested)
                    {
                        top.PropertyChangedAsync += (e, ct) => default;
                        nested.PropertyChangingAsync += (e, ct) => default;
                        await top.SetCountAsync(1);
                        await nested.SetCountAsync(2);
                    }
                }
            }
            """);

        result.AssertBuilds();
        Assert.Equal(new[] { "N.Foo.Notify.g.cs", "N.Outer+Foo.Notify.g.cs" }, result.HintNames);
    }

    [Fact]
    public void GenericClasses_NextToANonGenericClassOfTheSameName_AreGeneratedIntoTheGenericClass()
    {
        var result = GeneratorRunner.Run(Usings + """
            namespace N
            {
                [NotifyPropertyChangedAsync]
                public partial class Foo { [ObservableProperty] private int _count; }

                [NotifyPropertyChangedAsync, NotifyPropertyChangingAsync]
                public partial class Foo<T> where T : notnull { [ObservableProperty] private T _value = default!; }

                [NotifyPropertyChangedAsync, NotifyCollectionChangedAsync]
                public partial class Foo<TKey, TValue> where TKey : struct where TValue : class, new()
                {
                    [ObservableProperty] private TKey _key;
                    [ObservableProperty] private TValue? _value;
                }

                public static class Use
                {
                    public static async Task Run(Foo plain, Foo<string> one, Foo<int, object> two)
                    {
                        plain.PropertyChangedAsync += (e, ct) => default;
                        one.PropertyChangingAsync += (e, ct) => default;
                        two.CollectionChangedAsync += (e, ct) => default;
                        await plain.SetCountAsync(1);
                        await one.SetValueAsync(one.Value + "x");
                        await two.SetKeyAsync(two.Key + 1);
                        await two.SetValueAsync(new object());
                    }
                }
            }
            """);

        result.AssertBuilds();
        Assert.Equal(
            new[] { "N.Foo.Notify.g.cs", "N.Foo`1.Notify.g.cs", "N.Foo`2.Notify.g.cs" },
            result.HintNames);
    }

    [Fact]
    public void ClassInGenericContainers_IsGeneratedIntoThemWithTheirTypeParameters()
    {
        var result = GeneratorRunner.Run(Usings + """
            namespace N
            {
                public partial class Outer<TOuter>
                {
                    public partial struct Middle<TMiddle> where TMiddle : struct
                    {
                        [NotifyPropertyChangedAsync]
                        [InvokeSequentially]
                        public partial class Inner<TInner>
                        {
                            [ObservableProperty] private TOuter? _outer;
                            [ObservableProperty] private TMiddle _middle;
                            [ObservableProperty] private TInner? _innerValue;
                        }
                    }
                }
                public static class Use
                {
                    public static async Task Run(Outer<string>.Middle<int>.Inner<object> x)
                    {
                        x.PropertyChangedAsync += (e, ct) => default;
                        await x.SetOuterAsync("a");
                        await x.SetMiddleAsync(x.Middle + 1);
                        await x.SetInnerValueAsync(new object());
                    }
                }
            }
            """);

        result.AssertBuilds();
        var source = result.Source("N.Outer`1+Middle`1+Inner`1.Notify.g.cs");
        Assert.Contains("partial class Outer<TOuter>\n{\n    partial struct Middle<TMiddle>\n    {\n        partial class Inner<TInner>", source.Replace("\r\n", "\n"), System.StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("public partial interface Container { [NotifyPropertyChangedAsync] public partial class Foo { [ObservableProperty] private int _count; } }", "Container.Foo")]
    [InlineData("public partial record struct Container { [NotifyPropertyChangedAsync] public partial class Foo { [ObservableProperty] private int _count; } }", "Container.Foo")]
    [InlineData("public partial record Container(int X) { [NotifyPropertyChangedAsync] public partial class Foo { [ObservableProperty] private int _count; } }", "Container.Foo")]
    [InlineData("public readonly ref partial struct Container { [NotifyPropertyChangedAsync] public partial class Foo { [ObservableProperty] private int _count; } }", "Container.Foo")]
    [InlineData("public static partial class Container { [NotifyPropertyChangedAsync] public partial class Foo { [ObservableProperty] private int _count; } }", "Container.Foo")]
    [InlineData("public abstract partial class Container { [NotifyPropertyChangedAsync] public partial class Foo { [ObservableProperty] private int _count; } }", "Container.Foo")]
    [InlineData("public partial struct Container<T> where T : unmanaged { [NotifyPropertyChangedAsync] public partial class Foo { [ObservableProperty] private T _count; } }", "Container<int>.Foo")]
    public void ContainersOfEveryKind_AreReopened(string declaration, string use)
    {
        var result = GeneratorRunner.Run(Usings + "namespace N\n{\n" + declaration + """

                public static class Use
                {
                    public static Task Run(
            """ + use + """
             x)
                    {
                        x.PropertyChangedAsync += (e, ct) => default;
                        return x.SetCountAsync(x.Count).AsTask();
                    }
                }
            }
            """);

        result.AssertBuilds();
        Assert.Single(result.GeneratedSources);
    }

    [Fact]
    public void VerbatimNames_AreEscaped()
    {
        var result = GeneratorRunner.Run(Usings + """
            namespace N
            {
                public partial class @class<@int>
                {
                    [NotifyPropertyChangedAsync]
                    public partial class @static<@void> { [ObservableProperty] private @int? _value; }
                }
                public static class Use
                {
                    public static Task Run(@class<string>.@static<object> x)
                    {
                        x.PropertyChangedAsync += (e, ct) => default;
                        return x.SetValueAsync("a").AsTask();
                    }
                }
            }
            """);

        result.AssertBuilds();
        Assert.Equal(new[] { "N.class`1+static`1.Notify.g.cs" }, result.HintNames);
    }

    [Fact]
    public void ContainingTypeDeclaredInSeveralParts_IsReopenedOnce()
    {
        var result = GeneratorRunner.Run(
            ("A.cs", Usings + "namespace N; public partial class Outer { public int X; }"),
            ("B.cs", Usings + """
                namespace N;
                partial class Outer
                {
                    [NotifyPropertyChangedAsync]
                    public partial class Foo { [ObservableProperty] private int _count; }
                    public Task Run(Foo f) => f.SetCountAsync(X).AsTask();
                }
                """));

        result.AssertBuilds();
        Assert.Equal(new[] { "N.Outer+Foo.Notify.g.cs" }, result.HintNames);
    }

    [Fact]
    public void PrivateNestedClass_InTheGlobalNamespace_IsGenerated()
    {
        var result = GeneratorRunner.Run(Usings + """
            public partial class Outer
            {
                [NotifyPropertyChangedAsync]
                private partial class Foo { [ObservableProperty] private int _count; }
                public Task Run() => new Foo().SetCountAsync(1).AsTask();
            }
            """);

        result.AssertBuilds();
        Assert.Equal(new[] { "Outer+Foo.Notify.g.cs" }, result.HintNames);
    }

    [Fact]
    public void ContainingTypeNotPartial_ReportsZan001AndGeneratesNothingForTheClass()
    {
        var source = Usings + """
            namespace N
            {
                public class Outer
                {
                    [NotifyPropertyChangedAsync]
                    public partial class Foo { [ObservableProperty] private int _count; }
                }
                [NotifyPropertyChangedAsync]
                public partial class Other { }
            }
            """;
        var result = GeneratorRunner.Run(source);

        var diagnostic = Assert.Single(result.GeneratorDiagnostics);
        Assert.Equal("ZAN001", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Equal(
            "The notification members of class 'N.Outer.Foo' are not generated because its containing type 'N.Outer' is not partial",
            diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture));
        AssertLocatedOn(diagnostic, source, "Foo");
        Assert.Equal(new[] { "N.Other.Notify.g.cs" }, result.HintNames);
        result.AssertBuilds();
    }

    [Fact]
    public void NonPartialOuterTypeAboveAPartialMiddleType_ReportsZan001NamingTheOuterType()
    {
        var source = Usings + """
            namespace N
            {
                public class Outer
                {
                    public partial class Middle
                    {
                        [NotifyPropertyChangedAsync]
                        public partial class Foo { }
                    }
                }
            }
            """;
        var result = GeneratorRunner.Run(source);

        var diagnostic = Assert.Single(result.GeneratorDiagnostics);
        Assert.Equal("ZAN001", diagnostic.Id);
        Assert.Contains("'N.Outer'", diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture), System.StringComparison.Ordinal);
        Assert.Empty(result.GeneratedSources);
        result.AssertBuilds();
    }

    [Theory]
    [InlineData("file partial class Foo { [ObservableProperty] private int _count; }", "N.Foo")]
    [InlineData("file partial class Outer { [NotifyPropertyChangedAsync] public partial class Foo { } }", "N.Outer.Foo")]
    public void FileLocalClass_ReportsZan002AndGeneratesNothingForTheClass(string declaration, string displayName)
    {
        var source = Usings + "namespace N;\n[NotifyPropertyChangedAsync]\npublic partial class Other { }\n"
            + (declaration.StartsWith("file partial class Foo", System.StringComparison.Ordinal) ? "[NotifyPropertyChangedAsync]\n" : "")
            + declaration;
        var result = GeneratorRunner.Run(source);

        var diagnostic = Assert.Single(result.GeneratorDiagnostics);
        Assert.Equal("ZAN002", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Equal(
            $"The notification members of class '{displayName}' are not generated because it is file-local or nested in a file-local type, and a generated file cannot extend a file-local type",
            diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture));
        AssertLocatedOn(diagnostic, source, "Foo");
        Assert.Equal(new[] { "N.Other.Notify.g.cs" }, result.HintNames);
        result.AssertBuilds();
    }

    internal static void AssertLocatedOn(Diagnostic diagnostic, string source, string name)
    {
        Assert.True(diagnostic.Location.IsInSource);
        var span = diagnostic.Location.SourceSpan;
        Assert.Equal(name, source.Substring(span.Start, span.Length));
        Assert.Equal(source.IndexOf("class " + name, System.StringComparison.Ordinal) + "class ".Length, span.Start);
    }
}
