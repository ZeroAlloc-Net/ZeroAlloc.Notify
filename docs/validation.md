# Validation

## Overview

`INotifyDataErrorInfoAsync` provides async-first error tracking. Apply `[NotifyDataErrorInfoAsync]` to the class and implement `HasErrors` and `GetErrors` yourself. The generator emits `RaiseErrorsChangedAsync` and the `ErrorsChangedAsync` event.

## Basic Setup

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
    public async ValueTask ValidateEmailAsync(string value, CancellationToken ct)
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

## Subscribing to Errors

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

## Reading Errors

```csharp
var hasErrors = vm.HasErrors;
var errors = vm.GetErrors(nameof(vm.Email));
```

## Async Validation

For async validation (e.g. database lookup), call your validation method from the `PropertyChangedAsync` handler:

```csharp
vm.PropertyChangedAsync += async (args, ct) =>
{
    if (args.PropertyName == nameof(vm.Username))
    {
        var taken = await _service.IsUsernameTakenAsync((string?)args.NewValue, ct);
        if (taken)
        {
            vm._errors[nameof(vm.Username)] = new List<string> { "Username already taken" };
            await vm.RaiseErrorsChangedAsync(nameof(vm.Username), ct);
        }
    }
};
```

## Next Steps

- [Observable Properties](observable-properties.md)
- [Async Notifications](async-notifications.md)
