# Diagnostics

The ZeroAlloc.Notify source generator reports these diagnostics. All of them use the `ZAN` prefix.
Each one means that nothing is generated for the class it points at. Every other class in the
project is still generated.

| ID | Severity | Title |
|----|----------|-------|
| [ZAN001](#zan001) | Warning | Containing type of a Notify class is not partial |
| [ZAN002](#zan002) | Error | File-local Notify class is not generated |
| [ZAN003](#zan003) | Error | Class name differs only in case from another Notify class |

## ZAN001

**Containing type of a Notify class is not partial.**

A class nested in another type gets its members generated into the real nested class. To do that,
the generated file reopens every containing type as `partial`, so each containing type has to be
declared `partial`. When one is not, the generator reports ZAN001 on the class, naming the
outermost containing type that is not partial, and generates nothing for it.

```csharp
public class Orders                       // ZAN001: Orders is not partial
{
    [NotifyPropertyChangedAsync]
    public partial class OrderViewModel
    {
        [ObservableProperty] private string _status = "";
    }
}
```

Fix it by declaring every containing type `partial`:

```csharp
public partial class Orders
{
    [NotifyPropertyChangedAsync]
    public partial class OrderViewModel
    {
        [ObservableProperty] private string _status = "";
    }
}
```

The diagnostic is a warning, so a build that does not use the generated members keeps compiling.
Under `TreatWarningsAsErrors` it fails the build.

## ZAN002

**File-local Notify class is not generated.**

A `file` type is visible only in the source file that declares it, so the generated file cannot
extend it. The generator reports the error ZAN002 on a class that is declared `file`, or that is
nested in a `file` type, and generates nothing for it.

```csharp
[NotifyPropertyChangedAsync]
file partial class OrderViewModel         // ZAN002
{
    [ObservableProperty] private string _status = "";
}
```

Fix it by removing the `file` modifier, for example by making the class `internal`.

## ZAN003

**Class name differs only in case from another Notify class.**

Each class gets a generated file named after its namespace, its containing types and its name,
such as `App.OrderViewModel.Notify.g.cs`. The compiler compares these file names ignoring case, so
two classes whose names differ only in case, such as `App.OrderViewModel` and
`App.orderViewModel`, cannot both get a file. The class declared first, by file path and then
position, is generated. Every later one gets the error ZAN003 and is not generated.

```csharp
namespace App;

[NotifyPropertyChangedAsync] public partial class OrderViewModel { }
[NotifyPropertyChangedAsync] public partial class orderViewModel { }   // ZAN003
```

Fix it by renaming one of the classes, or by moving it to another namespace or containing type.

## Next Steps

- [Getting Started](getting-started.md)
- [Observable Properties](observable-properties.md)
