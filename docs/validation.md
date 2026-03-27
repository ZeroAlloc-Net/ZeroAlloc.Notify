# Validation

## Overview

`INotifyDataErrorInfoAsync` provides async-first error tracking. Use `[NotifyDataErrorInfoAsync]` to generate async error management on your viewmodel.

## Basic Setup

```csharp
using ZeroAlloc.Notify;

public partial class RegistrationViewModel
{
    [ObservableProperty]
    private string email = "";

    [NotifyPropertyChangedAsync]
    partial void OnEmailChanged(string oldValue, string newValue)
    {
        if (string.IsNullOrWhiteSpace(newValue))
            AddError(nameof(Email), "Email is required");
        else if (!newValue.Contains('@'))
            AddError(nameof(Email), "Invalid email format");
        else
            ClearErrors(nameof(Email));
    }

    [NotifyDataErrorInfoAsync]
    partial void OnErrorsChanged();
}
```

## Reading Errors

```csharp
var vm = new RegistrationViewModel();
await vm.SetEmailAsync("not-an-email");

var hasErrors = await vm.GetHasErrorsAsync();           // true
var errors = await vm.GetErrorsAsync(nameof(vm.Email)); // ["Invalid email format"]
```

## Subscribing to Error Changes

```csharp
vm.ErrorsChangedAsync += async (args, ct) =>
{
    Console.WriteLine($"Errors changed for: {args.PropertyName}");
};
```

## Async Validation

For async validation (e.g. database lookup), subscribe to the event directly:

```csharp
vm.PropertyChangedAsync += async (args, ct) =>
{
    if (args.PropertyName == nameof(vm.Username))
    {
        var taken = await _service.IsUsernameTakenAsync(args.NewValue?.ToString(), ct);
        if (taken)
            vm.AddError(nameof(vm.Username), "Username already taken");
    }
};
```

## Next Steps

- [Observable Properties](observable-properties.md)
- [Async Notifications](async-notifications.md)
