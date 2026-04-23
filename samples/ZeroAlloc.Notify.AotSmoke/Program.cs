using System;
using System.Threading;
using System.Threading.Tasks;
using ZeroAlloc.Notify;
using ZeroAlloc.Notify.AotSmoke;

// Exercise the generator-emitted ObservableProperty setters and async
// PropertyChanging/PropertyChanged events under PublishAot=true. The generator
// must produce fully-AOT-safe event plumbing — no reflection, no dynamic
// delegate construction.

var vm = new UserViewModel();

var changingCount = 0;
var changedCount = 0;

vm.PropertyChangingAsync += (sender, args, ct) =>
{
    Interlocked.Increment(ref changingCount);
    return ValueTask.CompletedTask;
};

vm.PropertyChangedAsync += (sender, args, ct) =>
{
    Interlocked.Increment(ref changedCount);
    return ValueTask.CompletedTask;
};

// Mutating a property should fire both events exactly once.
vm.Name = "Alice";
if (!string.Equals(vm.Name, "Alice", StringComparison.Ordinal))
    return Fail($"Name assignment: expected 'Alice', got '{vm.Name}'");

// Setting the same value should NOT fire again (the generator emits an
// EqualityComparer<T>.Default short-circuit).
vm.Name = "Alice";
if (changingCount != 1)
    return Fail($"PropertyChanging count expected 1 (same-value setter should skip), got {changingCount}");
if (changedCount != 1)
    return Fail($"PropertyChanged count expected 1, got {changedCount}");

vm.Age = 42;
if (vm.Age != 42) return Fail($"Age assignment: expected 42, got {vm.Age}");
if (changingCount != 2) return Fail($"After Age change, Changing count expected 2, got {changingCount}");
if (changedCount != 2) return Fail($"After Age change, Changed count expected 2, got {changedCount}");

Console.WriteLine("AOT smoke: PASS");
return 0;

static int Fail(string message)
{
    Console.Error.WriteLine($"AOT smoke: FAIL — {message}");
    return 1;
}
