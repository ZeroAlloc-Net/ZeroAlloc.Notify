# Event Args OldValue/NewValue Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Add `OldValue` and `NewValue` to `AsyncPropertyChangedEventArgs` and `AsyncPropertyChangingEventArgs`, update the source generator to pass both values, and fix all documentation that referenced these properties prematurely.

**Architecture:** Three-layer change — event args types gain two new properties, the generator writer captures the old value before field assignment and passes old+new to both Raise calls, and all snapshot/integration tests are updated to match. Documentation fixes are a separate final task to keep commits clean.

**Tech Stack:** .NET 10, C# 13, Roslyn source generation, xUnit, Verify snapshot testing.

---

## Task 1: Add failing integration tests for OldValue/NewValue

These tests must be written first (TDD). They will not compile until Task 2 is done — that is expected.

**Files:**
- Modify: `tests/ZeroAlloc.Notify.Tests/IntegrationTests.cs`

**Step 1: Add three failing tests at the end of `IntegrationTests.cs`**

Open `tests/ZeroAlloc.Notify.Tests/IntegrationTests.cs` and append these three tests inside the class (before the closing `}`):

```csharp
[Fact]
public async Task SetNameAsync_ChangedEvent_ContainsOldAndNewValues()
{
    var vm = new TestViewModel();
    object? capturedOld = null;
    object? capturedNew = null;

    vm.PropertyChangedAsync += (args, ct) =>
    {
        capturedOld = args.OldValue;
        capturedNew = args.NewValue;
        return ValueTask.CompletedTask;
    };

    await vm.SetNameAsync("Alice");

    Assert.Equal("", capturedOld);   // default field value
    Assert.Equal("Alice", capturedNew);
}

[Fact]
public async Task SetNameAsync_ChangingEvent_ContainsOldAndNewValues()
{
    var vm = new TestViewModel();
    object? capturedOld = null;
    object? capturedNew = null;

    vm.PropertyChangingAsync += (args, ct) =>
    {
        capturedOld = args.OldValue;
        capturedNew = args.NewValue;
        return ValueTask.CompletedTask;
    };

    await vm.SetNameAsync("Bob");

    Assert.Equal("", capturedOld);   // value before assignment
    Assert.Equal("Bob", capturedNew); // value about to be set
}

[Fact]
public async Task SetNameAsync_ChangingEventOldValue_ReflectsValueBeforeAssignment()
{
    var vm = new TestViewModel();
    await vm.SetNameAsync("first");

    object? oldInChanging = null;
    object? oldInChanged  = null;

    vm.PropertyChangingAsync += (args, ct) => { oldInChanging = args.OldValue; return ValueTask.CompletedTask; };
    vm.PropertyChangedAsync  += (args, ct) => { oldInChanged  = args.OldValue; return ValueTask.CompletedTask; };

    await vm.SetNameAsync("second");

    Assert.Equal("first", oldInChanging); // not yet changed when Changing fires
    Assert.Equal("first", oldInChanged);  // same old value captured before assignment
}
```

**Step 2: Try to build — expect compile errors**

```bash
cd c:/Projects/Prive/ZeroAlloc.Notify
dotnet build tests/ZeroAlloc.Notify.Tests/ZeroAlloc.Notify.Tests.csproj -c Release 2>&1 | grep "error CS"
```

Expected: errors like `'AsyncPropertyChangedEventArgs' does not contain a definition for 'OldValue'`. This is correct — the tests are intentionally failing.

**Step 3: Commit the failing tests**

```bash
git add tests/ZeroAlloc.Notify.Tests/IntegrationTests.cs
git commit -m "test: add failing tests for OldValue/NewValue on property event args"
```

---

## Task 2: Add OldValue and NewValue to AsyncPropertyChangedEventArgs

**Files:**
- Modify: `src/ZeroAlloc.Notify/AsyncPropertyChangedEventArgs.cs`

**Step 1: Replace the file contents**

Replace `src/ZeroAlloc.Notify/AsyncPropertyChangedEventArgs.cs` with:

```csharp
namespace ZeroAlloc.Notify;

public sealed class AsyncPropertyChangedEventArgs
{
    public string PropertyName { get; }
    public object? OldValue { get; }
    public object? NewValue { get; }

    public AsyncPropertyChangedEventArgs(string propertyName, object? oldValue, object? newValue)
    {
        PropertyName = propertyName;
        OldValue = oldValue;
        NewValue = newValue;
    }
}
```

**Step 2: Build just the core library — expect it to fail because the generator still passes only `propertyName`**

```bash
dotnet build src/ZeroAlloc.Notify/ZeroAlloc.Notify.csproj -c Release 2>&1 | grep "error CS"
```

Expected: no errors in the library itself (the constructor change is additive at source level, but existing callers in the generator will break at that build step). If the library builds clean, continue.

**Step 3: Commit**

```bash
git add src/ZeroAlloc.Notify/AsyncPropertyChangedEventArgs.cs
git commit -m "feat: add OldValue and NewValue to AsyncPropertyChangedEventArgs"
```

---

## Task 3: Add OldValue and NewValue to AsyncPropertyChangingEventArgs

`PropertyChangingAsync` fires *before* the assignment. Both old value (current field) and new value (incoming parameter) are known at that point.

**Files:**
- Modify: `src/ZeroAlloc.Notify/AsyncPropertyChangingEventArgs.cs`

**Step 1: Replace the file contents**

```csharp
namespace ZeroAlloc.Notify;

public sealed class AsyncPropertyChangingEventArgs
{
    public string PropertyName { get; }
    public object? OldValue { get; }
    public object? NewValue { get; }

    public AsyncPropertyChangingEventArgs(string propertyName, object? oldValue, object? newValue)
    {
        PropertyName = propertyName;
        OldValue = oldValue;
        NewValue = newValue;
    }
}
```

**Step 2: Commit**

```bash
git add src/ZeroAlloc.Notify/AsyncPropertyChangingEventArgs.cs
git commit -m "feat: add OldValue and NewValue to AsyncPropertyChangingEventArgs"
```

---

## Task 4: Update the generator writer to pass old and new values

This is the core change. The generator must:
1. Update `RaisePropertyChangedAsync`/`RaisePropertyChangingAsync` signatures to accept `oldValue` and `newValue`
2. In each generated `SetXxxAsync` method, capture the old field value before assignment and pass both to the Raise calls
3. Update the inline sequential `InvokeAsync` calls in the same way

**Files:**
- Modify: `src/ZeroAlloc.Notify.Generator/Writers/NotifyWriter.cs`

**Step 1: Read the current file**

Read `src/ZeroAlloc.Notify.Generator/Writers/NotifyWriter.cs` in full before editing.

**Step 2: Update `WriteEventMembers` — change the Raise method signatures**

In `WriteEventMembers`, find the `WriteEventMember` call for `PropertyChanged` (lines ~60-65) and change:

Old raiseSignature:
```
"    protected global::System.Threading.Tasks.ValueTask RaisePropertyChangedAsync(string propertyName, global::System.Threading.CancellationToken ct = default)"
```
New raiseSignature:
```
"    protected global::System.Threading.Tasks.ValueTask RaisePropertyChangedAsync(string propertyName, object? oldValue, object? newValue, global::System.Threading.CancellationToken ct = default)"
```

Old raiseBody:
```
"        => _propertyChangedAsync.InvokeAsync(new global::ZeroAlloc.Notify.AsyncPropertyChangedEventArgs(propertyName), ct);"
```
New raiseBody:
```
"        => _propertyChangedAsync.InvokeAsync(new global::ZeroAlloc.Notify.AsyncPropertyChangedEventArgs(propertyName, oldValue, newValue), ct);"
```

Do the same for `PropertyChanging`:

Old raiseSignature:
```
"    protected global::System.Threading.Tasks.ValueTask RaisePropertyChangingAsync(string propertyName, global::System.Threading.CancellationToken ct = default)"
```
New raiseSignature:
```
"    protected global::System.Threading.Tasks.ValueTask RaisePropertyChangingAsync(string propertyName, object? oldValue, object? newValue, global::System.Threading.CancellationToken ct = default)"
```

Old raiseBody:
```
"        => _propertyChangingAsync.InvokeAsync(new global::ZeroAlloc.Notify.AsyncPropertyChangingEventArgs(propertyName), ct);"
```
New raiseBody:
```
"        => _propertyChangingAsync.InvokeAsync(new global::ZeroAlloc.Notify.AsyncPropertyChangingEventArgs(propertyName, oldValue, newValue), ct);"
```

**Step 3: Update `WriteFieldMember` — capture old value and pass to Raise calls**

In `WriteFieldMember`, the current generated `SetXxxAsync` body is:

```csharp
if (EqualityComparer<T>.Default.Equals(field, value)) return;
// [optional] await RaisePropertyChangingAsync(nameof(Prop), ct)  OR inline InvokeAsync
field = value;
// [optional] await RaisePropertyChangedAsync(nameof(Prop), ct)   OR inline InvokeAsync
```

It must become:

```csharp
if (EqualityComparer<T>.Default.Equals(field, value)) return;
var __old = field;
// [optional] await RaisePropertyChangingAsync(nameof(Prop), __old, value, ct)  OR inline InvokeAsync
field = value;
// [optional] await RaisePropertyChangedAsync(nameof(Prop), __old, value, ct)   OR inline InvokeAsync
```

In `WriteFieldMember`, after the equality-check line (`sb.AppendLine($"        if (...)`), add:

```csharp
sb.AppendLine($"        var __old = {field.FieldName};");
```

Then update the four `sb.AppendLine` calls that contain `RaisePropertyChangingAsync` / `RaisePropertyChangedAsync` / inline `InvokeAsync`:

**For `PropertyChanging` sequential (inline InvokeAsync):**

Old:
```csharp
sb.AppendLine($"        await _propertyChangingAsync.InvokeAsync(new global::ZeroAlloc.Notify.AsyncPropertyChangingEventArgs(nameof({field.PropertyName})), global::ZeroAlloc.AsyncEvents.InvokeMode.Sequential, ct).ConfigureAwait(false);");
```
New:
```csharp
sb.AppendLine($"        await _propertyChangingAsync.InvokeAsync(new global::ZeroAlloc.Notify.AsyncPropertyChangingEventArgs(nameof({field.PropertyName}), __old, value), global::ZeroAlloc.AsyncEvents.InvokeMode.Sequential, ct).ConfigureAwait(false);");
```

**For `PropertyChanging` non-sequential (RaisePropertyChangingAsync):**

Old:
```csharp
sb.AppendLine($"        await RaisePropertyChangingAsync(nameof({field.PropertyName}), ct).ConfigureAwait(false);");
```
New:
```csharp
sb.AppendLine($"        await RaisePropertyChangingAsync(nameof({field.PropertyName}), __old, value, ct).ConfigureAwait(false);");
```

**For `PropertyChanged` sequential (inline InvokeAsync):**

Old:
```csharp
sb.AppendLine($"        await _propertyChangedAsync.InvokeAsync(new global::ZeroAlloc.Notify.AsyncPropertyChangedEventArgs(nameof({field.PropertyName})), global::ZeroAlloc.AsyncEvents.InvokeMode.Sequential, ct).ConfigureAwait(false);");
```
New:
```csharp
sb.AppendLine($"        await _propertyChangedAsync.InvokeAsync(new global::ZeroAlloc.Notify.AsyncPropertyChangedEventArgs(nameof({field.PropertyName}), __old, value), global::ZeroAlloc.AsyncEvents.InvokeMode.Sequential, ct).ConfigureAwait(false);");
```

**For `PropertyChanged` non-sequential (RaisePropertyChangedAsync):**

Old:
```csharp
sb.AppendLine($"        await RaisePropertyChangedAsync(nameof({field.PropertyName}), ct).ConfigureAwait(false);");
```
New:
```csharp
sb.AppendLine($"        await RaisePropertyChangedAsync(nameof({field.PropertyName}), __old, value, ct).ConfigureAwait(false);");
```

**Step 4: Build the full solution — expect snapshot test failures but no compile errors**

```bash
dotnet build ZeroAlloc.Notify.slnx -c Release
```

Expected: Build succeeded, 0 errors. The generator now compiles with the updated output.

**Step 5: Commit**

```bash
git add src/ZeroAlloc.Notify.Generator/Writers/NotifyWriter.cs
git commit -m "feat: pass OldValue and NewValue through generator to property event args"
```

---

## Task 5: Update snapshot tests

The generator output changed — all snapshot files that contain property notifications must be updated.

**Files:**
- Modify: `tests/ZeroAlloc.Notify.Tests/Snapshots/GeneratorTests.ObservableProperty_GeneratesPropertyAndAsyncSetter#MyViewModel.Notify.g.verified.cs`
- Modify: `tests/ZeroAlloc.Notify.Tests/Snapshots/GeneratorTests.InvokeSequentially_OnClass_SetsSequentialMode#MyViewModel.Notify.g.verified.cs`
- Modify: `tests/ZeroAlloc.Notify.Tests/Snapshots/GeneratorTests.InvokeSequentially_OnField_OnlyAffectsThatField#MyViewModel.Notify.g.verified.cs`
- Modify: `tests/ZeroAlloc.Notify.Tests/Snapshots/GeneratorTests.NotifyPropertyChangedAsync_GeneratesPlumbing#MyViewModel.Notify.g.verified.cs`

**Step 1: Run tests to produce `.received.cs` files**

```bash
cd c:/Projects/Prive/ZeroAlloc.Notify
dotnet test tests/ZeroAlloc.Notify.Tests/ -c Release --verbosity normal 2>&1 | grep -E "FAIL|PASS|received"
```

Expected: Several snapshot tests fail. Verify creates `.received.cs` files next to the `.verified.cs` files in `Snapshots/`.

**Step 2: Accept the new snapshots by overwriting verified files with received files**

```bash
for f in tests/ZeroAlloc.Notify.Tests/Snapshots/*.received.cs; do
  verified="${f/.received./.verified.}"
  cp "$f" "$verified"
  rm "$f"
done
```

**Step 3: Verify the snapshot contents are correct**

Read `tests/ZeroAlloc.Notify.Tests/Snapshots/GeneratorTests.ObservableProperty_GeneratesPropertyAndAsyncSetter#MyViewModel.Notify.g.verified.cs` and confirm:
- `RaisePropertyChangingAsync(nameof(Name), __old, value, ct)` is present
- `RaisePropertyChangedAsync(nameof(Name), __old, value, ct)` is present
- `var __old = _name;` is present before the Raise calls
- `RaisePropertyChangedAsync` signature includes `object? oldValue, object? newValue`

**Step 4: Run all tests — all must pass**

```bash
dotnet test ZeroAlloc.Notify.slnx -c Release --verbosity normal
```

Expected: All tests pass, including the three new OldValue/NewValue integration tests from Task 1.

**Step 5: Commit**

```bash
git add tests/ZeroAlloc.Notify.Tests/Snapshots/
git commit -m "test: update snapshots for OldValue/NewValue generator output"
```

---

## Task 6: Fix documentation — remove invalid patterns, fix API references

Several docs use patterns that don't exist (`partial void OnXxxChanged`, `ZeroAlloc.Notify.Attributes` namespace, `AddError`/`ClearErrors`, `GetHasErrorsAsync`/`GetErrorsAsync`).

**Files:**
- Modify: `README.md`
- Modify: `docs/getting-started.md`
- Modify: `docs/observable-properties.md`
- Modify: `docs/async-notifications.md`
- Modify: `docs/validation.md`
- Modify: `docs/testing.md`
- Modify: `docs/performance.md`

### README.md

Read the file. Find the Quick Example section (around lines 26–50). The `partial void OnNameChanged` and `partial void OnAgeChanged` declarations don't exist in the generator. Remove them. The correct usage is just the class-level attributes + event subscription:

Replace:
```csharp
// 1. Define a ViewModel with observable properties
public partial class UserViewModel
{
    [ObservableProperty]
    private string name;

    [ObservableProperty]
    private int age;

    [NotifyPropertyChangedAsync]
    partial void OnNameChanged(string oldValue, string newValue);

    [NotifyPropertyChangedAsync]
    partial void OnAgeChanged(int oldValue, int newValue);
}
```

With:
```csharp
// 1. Define a ViewModel with observable properties
[NotifyPropertyChangedAsync]
public partial class UserViewModel
{
    [ObservableProperty]
    private string _name = "";

    [ObservableProperty]
    private int _age;
}
```

The subscription and set examples (steps 2 and 3) are correct and use `args.OldValue`/`args.NewValue` which now work — leave them as-is.

### docs/getting-started.md

Read the file. Find:
1. `using ZeroAlloc.Notify.Attributes;` — remove this line (namespace doesn't exist; attributes are in `ZeroAlloc.Notify`)
2. The partial class definition section — remove `[NotifyPropertyChangedAsync] partial void OnNameChanged(...)` method declarations
3. The `[NotifyPropertyChangedAsync]` attribute in the class definition belongs on the class, not on partial methods — ensure it is shown on the class

Also fix:
```csharp
Console.WriteLine($"{args.PropertyName} changed to {args.NewValue}");
```
This is now correct — leave it.

### docs/observable-properties.md

Read the file. This doc contains the most `OnXxxChanged(old, new)` partial method examples. For each example that shows:
```csharp
[NotifyPropertyChangedAsync]
partial void OnFirstNameChanged(string oldValue, string newValue)
{
    ...body...
}
```

The pattern shown here is **not generated**. The `[NotifyPropertyChangedAsync]` attribute goes on the **class**, not on a partial method. Replace these patterns with the equivalent using event subscription:

Before (partial method pattern — wrong):
```csharp
[NotifyPropertyChangedAsync]
partial void OnAgeChanged(int oldValue, int newValue)
{
    if (newValue < 0 || newValue > 150)
        throw new ArgumentOutOfRangeException(nameof(Age), "Age must be 0-150");
}
```

After (event subscription pattern — correct):
```csharp
public PersonViewModel()
{
    PropertyChangedAsync += (args, ct) =>
    {
        if (args.PropertyName == nameof(Age))
        {
            var newAge = (int)args.NewValue!;
            if (newAge < 0 || newAge > 150)
                throw new ArgumentOutOfRangeException(nameof(Age), "Age must be 0-150");
        }
        return ValueTask.CompletedTask;
    };
}
```

Also fix the "Computed Properties" example — remove the `RaisePropertyChangedAsync(nameof(FullName), null, FullName)` call (wrong signature). Replace with the correct 3-arg signature:
```csharp
await RaisePropertyChangedAsync(nameof(FullName), oldFullName, FullName, ct);
```

### docs/async-notifications.md

Read the file. The examples using `args.NewValue` are now correct — no change needed there. Check for any `partial void` patterns and remove them if present.

### docs/validation.md

Read the file. This doc uses `AddError()`, `ClearErrors()`, `GetHasErrorsAsync()`, `GetErrorsAsync()` — **none of these exist**.

`INotifyDataErrorInfoAsync` provides:
- `event AsyncEvent<AsyncErrorsChangedEventArgs> ErrorsChangedAsync`
- `bool HasErrors { get; }` — must be implemented by the user
- `IEnumerable GetErrors(string? propertyName)` — must be implemented by the user

Rewrite the doc to show the actual pattern — user implements `HasErrors` and `GetErrors` manually, and calls `RaiseErrorsChangedAsync` (generated) to notify:

```csharp
using System.Collections;
using System.Collections.Concurrent;
using ZeroAlloc.Notify;

[NotifyDataErrorInfoAsync]
public partial class RegistrationViewModel : INotifyDataErrorInfoAsync
{
    private readonly ConcurrentDictionary<string, List<string>> _errors = new();

    [ObservableProperty]
    private string _email = "";

    // INotifyDataErrorInfoAsync implementation
    public bool HasErrors => !_errors.IsEmpty;

    public IEnumerable GetErrors(string? propertyName)
        => propertyName is null
            ? _errors.Values.SelectMany(e => e)
            : _errors.TryGetValue(propertyName, out var list) ? list : Enumerable.Empty<string>();

    // Called from a PropertyChangedAsync handler or other logic
    private async ValueTask ValidateEmailAsync(string value, CancellationToken ct)
    {
        _errors.Remove(nameof(Email), out _);

        if (string.IsNullOrWhiteSpace(value))
            _errors[nameof(Email)] = new List<string> { "Email is required" };
        else if (!value.Contains('@'))
            _errors[nameof(Email)] = new List<string> { "Invalid email format" };

        await RaiseErrorsChangedAsync(nameof(Email), ct);
    }
}
```

And for subscribing to error changes:
```csharp
var vm = new RegistrationViewModel();

vm.ErrorsChangedAsync += async (args, ct) =>
{
    Console.WriteLine($"Errors changed for: {args.PropertyName}");
    Console.WriteLine($"Has errors: {vm.HasErrors}");
};

vm.PropertyChangedAsync += async (args, ct) =>
{
    if (args.PropertyName == nameof(vm.Email))
        await vm.ValidateEmailAsync((string)args.NewValue!, ct);
};

await vm.SetEmailAsync("not-an-email");
```

Reading errors:
```csharp
var hasErrors = vm.HasErrors;                              // bool
var errors = vm.GetErrors(nameof(vm.Email));               // IEnumerable
```

### docs/testing.md

Read the file. Fix:
1. `captured.NewValue` — now correct after Task 2
2. `args.OldValue` — now correct after Task 2
3. The validation test uses `GetHasErrorsAsync()` / `GetErrorsAsync()` — change to `vm.HasErrors` / `vm.GetErrors(propertyName)`:

```csharp
[Fact]
public async Task Validation_HasErrors_AfterInvalidEmail()
{
    var vm = new RegistrationViewModel();

    await vm.SetEmailAsync("not-an-email");

    Assert.True(vm.HasErrors);
    var errors = vm.GetErrors(nameof(vm.Email)).Cast<string>().ToList();
    Assert.Contains(errors, e => e.Contains("Invalid"));
}
```

### docs/performance.md

Read the file. Fix two things:

1. In the `### 3. Readonly Struct Event Args` section, the code example incorrectly shows `AsyncPropertyChangedEventArgs` as a `readonly struct` with `public readonly` fields. Fix it to show the actual `sealed class` with properties:

```csharp
// AsyncPropertyChangedEventArgs — sealed class, passed by reference
public sealed class AsyncPropertyChangedEventArgs
{
    public string PropertyName { get; }
    public object? OldValue { get; }
    public object? NewValue { get; }
}
```

Also update the section title and prose: it's not a struct, but the allocation is bounded and fixed per notification.

2. Remove the placeholder collection and data validation benchmark tables (lines ~18–31) since no benchmark code backs them. Replace with a note:

```markdown
> **Note:** Collection and validation benchmarks are not yet included in the benchmark suite. The property notification table above is the only measured result. See [benchmarks/Notify.Benchmarks/](../benchmarks/Notify.Benchmarks/) to add more.
```

**Step: Commit all doc fixes together**

After all doc files are updated:
```bash
git add README.md docs/getting-started.md docs/observable-properties.md docs/async-notifications.md docs/validation.md docs/testing.md docs/performance.md
git commit -m "docs: fix API examples to match actual implementation (remove partial method pattern, fix event args properties, correct validation API)"
```

---

## Task 7: Final verification

**Step 1: Full build**

```bash
cd c:/Projects/Prive/ZeroAlloc.Notify
dotnet build ZeroAlloc.Notify.slnx -c Release
```

Expected: Build succeeded, 0 errors, 0 warnings.

**Step 2: Full test run**

```bash
dotnet test ZeroAlloc.Notify.slnx -c Release --verbosity normal
```

Expected: All tests pass. Total count should be 25 (22 previous + 3 new OldValue/NewValue tests).

**Step 3: Verify new test names appear in output**

Confirm these three tests pass:
- `SetNameAsync_ChangedEvent_ContainsOldAndNewValues`
- `SetNameAsync_ChangingEvent_ContainsOldAndNewValues`
- `SetNameAsync_ChangingEventOldValue_ReflectsValueBeforeAssignment`
