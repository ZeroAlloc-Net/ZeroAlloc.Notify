using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using ZeroAlloc.Notify.Generator;

namespace ZeroAlloc.Notify.Tests;

/// <summary>
/// The model of a class compares by value, including its fields and its diagnostic, so an edit
/// that does not change the class leaves its model unchanged and its file is not rebuilt.
/// </summary>
public class IncrementalCachingTests
{
    private const string Header = "using ZeroAlloc.Notify;\nnamespace N;\n";

    private const string Observable =
        "[NotifyPropertyChangedAsync] public partial class Foo { [ObservableProperty] private int _count; [ObservableProperty] private string _name = \"\"; }";

    [Theory]
    [InlineData(Observable, "Other")]
    [InlineData("[NotifyPropertyChangedAsync] public partial class Foo<T> { [ObservableProperty] private T? _value; }", "Other")]
    // ZAN001.
    [InlineData("public class Outer { [NotifyPropertyChangedAsync] public partial class Foo { [ObservableProperty] private int _count; } }", "Other")]
    // ZAN004 and ZAN005, whose warnings keep their location.
    [InlineData("[NotifyPropertyChangedAsync] public class Foo { [ObservableProperty] private int _count; }", "Other")]
    [InlineData("[NotifyPropertyChangedAsync] public partial record Foo { [ObservableProperty] private int _count; }", "Other")]
    // ZAN003 against N.foo in A.cs, which sorts first, attached anew on every run.
    [InlineData(Observable, "foo")]
    public void EditToAnotherFile_LeavesTheModelUnchanged(string declaration, string otherName)
    {
        var (first, second) = RunTwice(
            ("Z.cs", Header + declaration),
            ("A.cs", OtherClass(otherName, "int")),
            ("A.cs", OtherClass(otherName, "string")));

        // In discovery order: the class under test, then the other class, which changed.
        Assert.Equal(2, first);
        Assert.Equal(new[] { IncrementalStepRunReason.Unchanged, IncrementalStepRunReason.Modified }, second);
    }

    [Fact]
    public void EditElsewhereInTheSameFile_LeavesTheModelUnchanged()
    {
        // The class is parsed again, so its field list is built anew; it must still compare equal.
        var (first, second) = RunTwice(
            ("A.cs", OtherClass("Other", "int")),
            ("Z.cs", Header + Observable + "\npublic class Unrelated { public int X; }"),
            ("Z.cs", Header + Observable + "\npublic class Unrelated { public long X; }"));

        Assert.Equal(2, first);
        Assert.Equal(new[] { IncrementalStepRunReason.Unchanged, IncrementalStepRunReason.Unchanged }, second);
    }

    /// <summary>
    /// Runs the generator, replaces the second file with the edited one, runs it again and returns
    /// the number of models of the first run and the run reasons of the models of the second.
    /// </summary>
    private static (int First, IncrementalStepRunReason[] Second) RunTwice(
        (string Path, string Source) kept,
        (string Path, string Source) original,
        (string Path, string Source) edited)
    {
        var compilation = GeneratorRunner.CreateCompilation(kept, original);
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            new[] { new NotifyGenerator().AsSourceGenerator() },
            parseOptions: GeneratorRunner.ParseOptions,
            driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        driver = driver.RunGenerators(compilation);
        var first = Outputs(driver).Length;

        var tree = compilation.SyntaxTrees.Single(t => string.Equals(t.FilePath, original.Path, System.StringComparison.Ordinal));
        driver = driver.RunGenerators(compilation.ReplaceSyntaxTree(
            tree, CSharpSyntaxTree.ParseText(edited.Source, GeneratorRunner.ParseOptions, edited.Path)));

        return (first, Outputs(driver).Select(o => o.Reason).ToArray());
    }

    private static (object Value, IncrementalStepRunReason Reason)[] Outputs(GeneratorDriver driver) =>
        driver.GetRunResult().Results[0]
            .TrackedSteps[NotifyGenerator.ModelsTrackingName]
            .SelectMany(step => step.Outputs)
            .ToArray();

    private static string OtherClass(string name, string fieldType) =>
        Header + "[NotifyPropertyChangedAsync] public partial class " + name + " { [ObservableProperty] private " + fieldType + " _y; }";
}
