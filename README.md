# ZeroAlloc.Notify

[![NuGet](https://img.shields.io/nuget/v/ZeroAlloc.Notify.svg)](https://www.nuget.org/packages/ZeroAlloc.Notify)
[![Build](https://github.com/ZeroAlloc-Net/ZeroAlloc.Notify/actions/workflows/ci.yml/badge.svg)](https://github.com/ZeroAlloc-Net/ZeroAlloc.Notify/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://github.com/ZeroAlloc-Net/ZeroAlloc.Notify/blob/main/LICENSE)
[![AOT](https://img.shields.io/badge/AOT--Compatible-passing-brightgreen)](https://learn.microsoft.com/dotnet/core/deploying/native-aot/)
[![GitHub Sponsors](https://img.shields.io/github/sponsors/MarcelRoozekrans?style=flat&logo=githubsponsors&color=ea4aaa&label=Sponsor)](https://github.com/sponsors/MarcelRoozekrans)

Source-generated, awaitable `INotifyPropertyChanged` for .NET: `await vm.SetNameAsync("Alice")` returns only after every async handler has finished, with no reflection and NativeAOT support.

## Why ZeroAlloc.Notify

- **Setters you can `await`.** Async validation, saving or coordinated state changes complete before the setter returns. CommunityToolkit.Mvvm and PropertyChanged.Fody only offer synchronous notifications.
- **0 B when nobody listens.** The generated setter creates event args only when there are subscribers; tests assert 0 B for reference-type and value-type properties. With subscribers, a change costs its args object, 40 B, plus a box per value-type value; tests pin both figures.
- **Works under NativeAOT and trimming.** All plumbing is generated at compile time; CI publishes and runs a NativeAOT sample on every build.
- **Compile-time diagnostics.** A class that is not `partial`, a record, or a stray `[ObservableProperty]` is reported by the build, not discovered at runtime.

## Install

```bash
dotnet add package ZeroAlloc.Notify
```

The source generator ships inside the package.

## Quick start

```csharp
using ZeroAlloc.Notify;

var user = new UserViewModel();

user.PropertyChangedAsync += async (args, ct) =>
{
    await Task.Delay(100, ct);  // e.g. save to a server
    Console.WriteLine($"{args.PropertyName}: {args.OldValue} -> {args.NewValue}, saved");
};

await user.SetNameAsync("Alice");  // returns after the handler has saved
await user.SetAgeAsync(30);

[NotifyPropertyChangedAsync]
public partial class UserViewModel
{
    [ObservableProperty] private string _name = "";
    [ObservableProperty] private int _age;
}
```

The generator writes the `Name` and `Age` properties, the `SetNameAsync` and `SetAgeAsync` setters and the `PropertyChangedAsync` event.

## What you get

- **Async property notifications**, before and after a change: `[NotifyPropertyChangedAsync]` and `[NotifyPropertyChangingAsync]`. [Observable properties](https://github.com/ZeroAlloc-Net/ZeroAlloc.Notify/blob/main/docs/observable-properties.md)
- **Async collection notifications** with `[NotifyCollectionChangedAsync]`. [Collection changes](https://github.com/ZeroAlloc-Net/ZeroAlloc.Notify/blob/main/docs/collection-changes.md)
- **Async validation** through `INotifyDataErrorInfoAsync`. [Validation](https://github.com/ZeroAlloc-Net/ZeroAlloc.Notify/blob/main/docs/validation.md)
- **Parallel handlers by default, sequential on request** with `[InvokeSequentially]`, and a `CancellationToken` on every setter. [Async notifications](https://github.com/ZeroAlloc-Net/ZeroAlloc.Notify/blob/main/docs/async-notifications.md)
- **Build-time checks**, ZAN001 to ZAN006. [Diagnostics](https://github.com/ZeroAlloc-Net/ZeroAlloc.Notify/blob/main/docs/diagnostics.md)
- **Targets** `netstandard2.0`, `net8.0` and `net10.0`.
- **Built on [ZeroAlloc.AsyncEvents](https://github.com/ZeroAlloc-Net/ZeroAlloc.AsyncEvents)**, the async event primitive behind every handler.

## Performance

One property change with five handlers attached. .NET 10, BenchmarkDotNet, same run; the ratio is against a hand-written `INotifyPropertyChanged`, computed from the published means.

| Library | Ratio | Allocated | Async handlers |
|---|---:|---:|:---:|
| Manual `INotifyPropertyChanged` | 1.00 | 24 B | no |
| PropertyChanged.Fody | 0.82 | 0 B | no |
| CommunityToolkit.Mvvm | 1.43 | 0 B | no |
| **ZeroAlloc.Notify** | **3.20** | **40 B** | **yes** |

Awaitable handlers have a cost: Notify is the slowest of the four and allocates its args object when someone listens. For purely synchronous view models, Fody is the faster choice. Allocation breakdown, design notes and full numbers: [docs/performance.md](https://github.com/ZeroAlloc-Net/ZeroAlloc.Notify/blob/main/docs/performance.md).

## Learn more

- Documentation: [notify.zeroalloc.net](https://notify.zeroalloc.net)
- Runnable example: [`samples/ZeroAlloc.Notify.AotSmoke`](https://github.com/ZeroAlloc-Net/ZeroAlloc.Notify/tree/main/samples/ZeroAlloc.Notify.AotSmoke), published with NativeAOT in CI
- Related packages: [ZeroAlloc.AsyncEvents](https://github.com/ZeroAlloc-Net/ZeroAlloc.AsyncEvents) and the rest of the [ZeroAlloc family](https://github.com/ZeroAlloc-Net)
- Contributing: issues and pull requests are welcome on [GitHub](https://github.com/ZeroAlloc-Net/ZeroAlloc.Notify/issues)
- License: [MIT](https://github.com/ZeroAlloc-Net/ZeroAlloc.Notify/blob/main/LICENSE)
