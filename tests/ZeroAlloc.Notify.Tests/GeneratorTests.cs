using System.Collections.Generic;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using VerifyXunit;
using ZeroAlloc.Notify.Generator;

namespace ZeroAlloc.Notify.Tests;

public class GeneratorTests
{
    [Fact]
    public Task NotifyPropertyChangedAsync_GeneratesPlumbing()
        => Verify("""
            using ZeroAlloc.Notify;
            [NotifyPropertyChangedAsync]
            public partial class MyViewModel { }
            """);

    [Fact]
    public Task ObservableProperty_GeneratesPropertyAndAsyncSetter()
        => Verify("""
            using ZeroAlloc.Notify;
            [NotifyPropertyChangedAsync]
            [NotifyPropertyChangingAsync]
            public partial class MyViewModel
            {
                [ObservableProperty]
                private string _name = "";
                [ObservableProperty]
                private int _count;
            }
            """);

    [Fact]
    public Task InvokeSequentially_OnClass_SetsSequentialMode()
        => Verify("""
            using ZeroAlloc.Notify;
            [NotifyPropertyChangedAsync]
            [InvokeSequentially]
            public partial class MyViewModel
            {
                [ObservableProperty]
                private string _name = "";
            }
            """);

    [Fact]
    public Task NotifyCollectionChangedAsync_GeneratesPlumbing()
        => Verify("""
            using ZeroAlloc.Notify;
            [NotifyCollectionChangedAsync]
            public partial class MyCollection { }
            """);

    [Fact]
    public Task NotifyDataErrorInfoAsync_GeneratesPlumbing()
        => Verify("""
            using ZeroAlloc.Notify;
            [NotifyDataErrorInfoAsync]
            public partial class MyModel { }
            """);

    private static Task Verify(string source)
    {
        var compilation = CreateCompilation(source);
        var generator = new NotifyGenerator();
        var driver = CSharpGeneratorDriver.Create(generator).RunGenerators(compilation);
        return Verifier.Verify(driver).UseDirectory("Snapshots");
    }

    private static IEnumerable<MetadataReference> GetProjectReferences()
    {
        // Include ZeroAlloc.Notify (attributes) and ZeroAlloc.AsyncEvents (interfaces/event types)
        yield return MetadataReference.CreateFromFile(typeof(ZeroAlloc.Notify.NotifyPropertyChangedAsyncAttribute).Assembly.Location);
        yield return MetadataReference.CreateFromFile(typeof(ZeroAlloc.AsyncEvents.AsyncEventHandler<>).Assembly.Location);
    }

    private static CSharpCompilation CreateCompilation(string source)
    {
        var refs = new List<MetadataReference>(Basic.Reference.Assemblies.Net90.References.All);
        refs.AddRange(GetProjectReferences());

        return CSharpCompilation.Create(
            "TestAssembly",
            new[] { CSharpSyntaxTree.ParseText(source) },
            refs,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }
}
