using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ZeroAlloc.Notify;
using ZeroAlloc.Notify.AotSmoke;

// Exercise the generator-emitted SetXxxAsync methods and async
// PropertyChanging/PropertyChanged events under PublishAot=true, over reference,
// primitive, struct, nullable and enum property types. The generator
// must produce fully-AOT-safe event plumbing — no reflection, no dynamic
// delegate construction.

var vm = new UserViewModel();

var changingCount = 0;
var changedCount = 0;

vm.PropertyChangingAsync += (args, ct) =>
{
    Interlocked.Increment(ref changingCount);
    return ValueTask.CompletedTask;
};

vm.PropertyChangedAsync += (args, ct) =>
{
    Interlocked.Increment(ref changedCount);
    return ValueTask.CompletedTask;
};

// Mutating a property should fire both events exactly once.
await vm.SetNameAsync("Alice").ConfigureAwait(false);
if (!string.Equals(vm.Name, "Alice", StringComparison.Ordinal))
    return Fail($"Name assignment: expected 'Alice', got '{vm.Name}'");

// Setting the same value should NOT fire again (EqualityComparer<T>.Default short-circuit).
await vm.SetNameAsync("Alice").ConfigureAwait(false);
if (changingCount != 1)
    return Fail($"PropertyChanging count expected 1 (same-value setter should skip), got {changingCount}");
if (changedCount != 1)
    return Fail($"PropertyChanged count expected 1, got {changedCount}");

await vm.SetAgeAsync(42).ConfigureAwait(false);
if (vm.Age != 42) return Fail($"Age assignment: expected 42, got {vm.Age}");
if (changingCount != 2) return Fail($"After Age change, Changing count expected 2, got {changingCount}");
if (changedCount != 2) return Fail($"After Age change, Changed count expected 2, got {changedCount}");

// ---- Value-type properties ----
// Each step asserts the exact old and new values the event args carry after boxing,
// and that setting an equal value raises nothing.
var m = new MeasurementViewModel();
var changing = new List<AsyncPropertyChangingEventArgs>();
var changed = new List<AsyncPropertyChangedEventArgs>();
m.PropertyChangingAsync += (args, ct) =>
{
    lock (changing) changing.Add(args);
    return ValueTask.CompletedTask;
};
m.PropertyChangedAsync += (args, ct) =>
{
    lock (changed) changed.Add(args);
    return ValueTask.CompletedTask;
};

// IEquatable struct.
await m.SetPositionAsync(new SmokePoint(1, 2)).ConfigureAwait(false);
if (m.Position.X != 1 || m.Position.Y != 2) return Fail($"Position: expected (1, 2), got {m.Position}");
if (Raised(changing, changed, 1, "Position", new SmokePoint(0, 0), new SmokePoint(1, 2)) is { } e1) return Fail(e1);
await m.SetPositionAsync(new SmokePoint(1, 2)).ConfigureAwait(false);
if (Raised(changing, changed, 1, "Position", new SmokePoint(0, 0), new SmokePoint(1, 2)) is { } e2) return Fail(e2);

// Struct without IEquatable: EqualityComparer falls back to ValueType.Equals.
await m.SetSizeAsync(new SmokeSize(2.5, 4)).ConfigureAwait(false);
if (m.Size.Width != 2.5 || m.Size.Height != 4) return Fail($"Size: expected 2.5x4, got {m.Size}");
if (Raised(changing, changed, 2, "Size", new SmokeSize(0, 0), new SmokeSize(2.5, 4)) is { } e3) return Fail(e3);
await m.SetSizeAsync(new SmokeSize(2.5, 4)).ConfigureAwait(false);
if (Raised(changing, changed, 2, "Size", new SmokeSize(0, 0), new SmokeSize(2.5, 4)) is { } e4) return Fail(e4);

// int?: null to null skips, then null to value, equal value skips, value to value, value to null.
if (m.Count is not null) return Fail($"Count: expected initial null, got {m.Count}");
await m.SetCountAsync(null).ConfigureAwait(false);
if (Raised(changing, changed, 2, "Size", new SmokeSize(0, 0), new SmokeSize(2.5, 4)) is { } e5) return Fail(e5);
await m.SetCountAsync(5).ConfigureAwait(false);
if (m.Count != 5) return Fail($"Count: expected 5, got {m.Count}");
if (Raised(changing, changed, 3, "Count", null, 5) is { } e6) return Fail(e6);
await m.SetCountAsync(5).ConfigureAwait(false);
if (Raised(changing, changed, 3, "Count", null, 5) is { } e7) return Fail(e7);
await m.SetCountAsync(7).ConfigureAwait(false);
if (m.Count != 7) return Fail($"Count: expected 7, got {m.Count}");
if (Raised(changing, changed, 4, "Count", 5, 7) is { } e8) return Fail(e8);
await m.SetCountAsync(null).ConfigureAwait(false);
if (m.Count is not null) return Fail($"Count: expected null, got {m.Count}");
if (Raised(changing, changed, 5, "Count", 7, null) is { } e9) return Fail(e9);

// Nullable struct: same transitions.
await m.SetAnchorAsync(null).ConfigureAwait(false);
if (Raised(changing, changed, 5, "Count", 7, null) is { } e10) return Fail(e10);
await m.SetAnchorAsync(new SmokePoint(3, 4)).ConfigureAwait(false);
if (m.Anchor is not { X: 3, Y: 4 }) return Fail($"Anchor: expected (3, 4), got {m.Anchor}");
if (Raised(changing, changed, 6, "Anchor", null, new SmokePoint(3, 4)) is { } e11) return Fail(e11);
await m.SetAnchorAsync(new SmokePoint(3, 4)).ConfigureAwait(false);
if (Raised(changing, changed, 6, "Anchor", null, new SmokePoint(3, 4)) is { } e12) return Fail(e12);
await m.SetAnchorAsync(new SmokePoint(5, 6)).ConfigureAwait(false);
if (Raised(changing, changed, 7, "Anchor", new SmokePoint(3, 4), new SmokePoint(5, 6)) is { } e13) return Fail(e13);
await m.SetAnchorAsync(null).ConfigureAwait(false);
if (m.Anchor is not null) return Fail($"Anchor: expected null, got {m.Anchor}");
if (Raised(changing, changed, 8, "Anchor", new SmokePoint(5, 6), null) is { } e14) return Fail(e14);

// Enum, on the per-field sequential invoke path.
await m.SetStatusAsync(SmokeStatus.Running).ConfigureAwait(false);
if (m.Status != SmokeStatus.Running) return Fail($"Status: expected Running, got {m.Status}");
if (Raised(changing, changed, 9, "Status", SmokeStatus.Idle, SmokeStatus.Running) is { } e15) return Fail(e15);
await m.SetStatusAsync(SmokeStatus.Running).ConfigureAwait(false);
if (Raised(changing, changed, 9, "Status", SmokeStatus.Idle, SmokeStatus.Running) is { } e16) return Fail(e16);

// ---- Generic class nested in another type ----
var entry = new Catalog.Entry<int>();
var entryChanged = 0;
entry.PropertyChangedAsync += (args, ct) =>
{
    if (string.Equals(args.PropertyName, "Value", StringComparison.Ordinal) && args.NewValue is 7)
        Interlocked.Increment(ref entryChanged);
    return ValueTask.CompletedTask;
};
await entry.SetValueAsync(7).ConfigureAwait(false);
if (entry.Value != 7) return Fail($"Catalog.Entry<int>.Value: expected 7, got {entry.Value}");
if (entryChanged != 1) return Fail($"Catalog.Entry<int> PropertyChanged count expected 1, got {entryChanged}");

Console.WriteLine("AOT smoke: PASS");
return 0;

// Checks that exactly expectedCount events of each kind have been raised, and that the
// last of each names the property and carries the expected boxed old and new values,
// matching both runtime type and value. A boxed Nullable<T> must box to its underlying
// T, or to null when empty.
static string? Raised(
    List<AsyncPropertyChangingEventArgs> changing,
    List<AsyncPropertyChangedEventArgs> changed,
    int expectedCount,
    string propertyName,
    object? expectedOld,
    object? expectedNew)
{
    if (changing.Count != expectedCount)
        return $"{propertyName}: PropertyChanging count expected {expectedCount}, got {changing.Count}";
    if (changed.Count != expectedCount)
        return $"{propertyName}: PropertyChanged count expected {expectedCount}, got {changed.Count}";

    var before = changing[^1];
    if (Mismatch("PropertyChanging", propertyName, expectedOld, expectedNew,
            before.PropertyName, before.OldValue, before.NewValue) is { } changingError)
        return changingError;

    var after = changed[^1];
    return Mismatch("PropertyChanged", propertyName, expectedOld, expectedNew,
        after.PropertyName, after.OldValue, after.NewValue);
}

static string? Mismatch(
    string eventName,
    string propertyName,
    object? expectedOld,
    object? expectedNew,
    string actualName,
    object? actualOld,
    object? actualNew)
{
    if (!string.Equals(actualName, propertyName, StringComparison.Ordinal))
        return $"{eventName}: expected property {propertyName}, got {actualName}";
    if (!SameBoxed(expectedOld, actualOld))
        return $"{eventName} {propertyName}: OldValue expected {Describe(expectedOld)}, got {Describe(actualOld)}";
    if (!SameBoxed(expectedNew, actualNew))
        return $"{eventName} {propertyName}: NewValue expected {Describe(expectedNew)}, got {Describe(actualNew)}";
    return null;
}

static bool SameBoxed(object? expected, object? actual)
    => expected is null
        ? actual is null
        : actual is not null && expected.GetType() == actual.GetType() && expected.Equals(actual);

static string Describe(object? value) => value is null ? "null" : $"{value.GetType().Name} {value}";

static int Fail(string message)
{
    Console.Error.WriteLine($"AOT smoke: FAIL — {message}");
    return 1;
}
