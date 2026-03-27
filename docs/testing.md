# Testing

## Overview

ZeroAlloc.Notify viewmodels are plain C# classes — no test framework adapter needed. Subscribe to events, set properties, and assert on captured values.

## Basic Property Change Test

```csharp
[Fact]
public async Task SetNameAsync_RaisesPropertyChangedAsync()
{
    var vm = new UserViewModel();
    AsyncPropertyChangedEventArgs? captured = null;

    vm.PropertyChangedAsync += (args, ct) =>
    {
        captured = args;
        return ValueTask.CompletedTask;
    };

    await vm.SetNameAsync("Alice");

    Assert.NotNull(captured);
    Assert.Equal(nameof(vm.Name), captured.PropertyName);
    Assert.Equal("Alice", captured.NewValue);
}
```

## Testing Old and New Values

```csharp
[Fact]
public async Task SetNameAsync_ProvidesBothValues()
{
    var vm = new UserViewModel();
    // set initial value without triggering notifications
    await vm.SetNameAsync("Before");

    string? oldCaptured = null, newCaptured = null;
    vm.PropertyChangedAsync += (args, ct) =>
    {
        oldCaptured = args.OldValue?.ToString();
        newCaptured = args.NewValue?.ToString();
        return ValueTask.CompletedTask;
    };

    await vm.SetNameAsync("After");

    Assert.Equal("Before", oldCaptured);
    Assert.Equal("After", newCaptured);
}
```

## Testing No-Change Optimization

```csharp
[Fact]
public async Task SetNameAsync_DoesNotRaise_WhenValueUnchanged()
{
    var vm = new UserViewModel();
    await vm.SetNameAsync("Alice");

    var count = 0;
    vm.PropertyChangedAsync += (args, ct) => { count++; return ValueTask.CompletedTask; };

    await vm.SetNameAsync("Alice"); // same value — should not raise

    Assert.Equal(0, count);
}
```

## Testing Sequential Handler Order

```csharp
[Fact]
public async Task SequentialHandlers_RunInSubscribeOrder()
{
    var vm = new ProcessViewModel(); // uses [InvokeSequentially]
    var order = new List<int>();

    vm.PropertyChangedAsync += async (args, ct) => { order.Add(1); await Task.Yield(); };
    vm.PropertyChangedAsync += async (args, ct) => { order.Add(2); await Task.Yield(); };

    await vm.SetStatusAsync("active");

    Assert.Equal(new[] { 1, 2 }, order);
}
```

## Testing Cancellation

```csharp
[Fact]
public async Task SetNameAsync_PropagatesCancellation()
{
    var vm = new UserViewModel();
    var cts = new CancellationTokenSource();

    vm.PropertyChangedAsync += async (args, ct) =>
    {
        await Task.Delay(Timeout.Infinite, ct);
    };

    cts.Cancel();
    await Assert.ThrowsAnyAsync<OperationCanceledException>(
        () => vm.SetNameAsync("Alice", cts.Token).AsTask());
}
```

## Testing Validation Errors

```csharp
[Fact]
public async Task SetEmailAsync_AddsError_WhenInvalid()
{
    var vm = new RegistrationViewModel();

    await vm.SetEmailAsync("not-an-email");

    Assert.True(await vm.GetHasErrorsAsync());
    var errors = await vm.GetErrorsAsync(nameof(vm.Email));
    Assert.Contains(errors, e => e.Contains("Invalid"));
}
```

## Running Tests

```bash
dotnet test tests/ZeroAlloc.Notify.Tests/ZeroAlloc.Notify.Tests.csproj -c Release --verbosity normal
```

## Next Steps

- [Observable Properties](observable-properties.md)
- [Advanced Patterns](advanced-patterns.md)
- [Performance](performance.md)
