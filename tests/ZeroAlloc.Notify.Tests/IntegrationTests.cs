namespace ZeroAlloc.Notify.Tests;

public class IntegrationTests
{
    [Fact]
    public async Task SetNameAsync_RaisesBothChangingAndChangedEvents()
    {
        var vm = new TestViewModel();
        var changingRaised = false;
        var changedRaised = false;

        vm.PropertyChangingAsync += (args, ct) => { changingRaised = true; return ValueTask.CompletedTask; };
        vm.PropertyChangedAsync  += (args, ct) => { changedRaised = true;  return ValueTask.CompletedTask; };

        await vm.SetNameAsync("Alice");

        Assert.True(changingRaised);
        Assert.True(changedRaised);
    }

    [Fact]
    public async Task SetNameAsync_SameValue_DoesNotRaiseEvents()
    {
        var vm = new TestViewModel();
        var raised = false;
        vm.PropertyChangedAsync += (args, ct) => { raised = true; return ValueTask.CompletedTask; };

        await vm.SetNameAsync("");  // same as default ""

        Assert.False(raised);
    }

    [Fact]
    public async Task SetNameAsync_ChangedEvent_ContainsPropertyName()
    {
        var vm = new TestViewModel();
        string? receivedName = null;
        vm.PropertyChangedAsync += (args, ct) => { receivedName = args.PropertyName; return ValueTask.CompletedTask; };

        await vm.SetNameAsync("Bob");

        Assert.Equal("Name", receivedName);
    }

    [Fact]
    public async Task SetNameAsync_ChangingEvent_ContainsPropertyName()
    {
        var vm = new TestViewModel();
        string? receivedName = null;
        vm.PropertyChangingAsync += (args, ct) => { receivedName = args.PropertyName; return ValueTask.CompletedTask; };

        await vm.SetNameAsync("Carol");

        Assert.Equal("Name", receivedName);
    }

    [Fact]
    public async Task SetNameAsync_CancellationToken_PropagatedToHandlers()
    {
        var vm = new TestViewModel();
        CancellationToken received = default;
        vm.PropertyChangedAsync += (args, ct) => { received = ct; return ValueTask.CompletedTask; };

        using var cts = new CancellationTokenSource();
        await vm.SetNameAsync("Dave", cts.Token);

        Assert.Equal(cts.Token, received);
    }

    [Fact]
    public async Task SetNameAsync_ChangingFiredBeforeChanged()
    {
        var vm = new TestViewModel();
        var order = new List<string>();
        vm.PropertyChangingAsync += (args, ct) => { order.Add("changing"); return ValueTask.CompletedTask; };
        vm.PropertyChangedAsync  += (args, ct) => { order.Add("changed");  return ValueTask.CompletedTask; };

        await vm.SetNameAsync("Eve");

        Assert.Equal(new[] { "changing", "changed" }, order, StringComparer.Ordinal);
    }

    [Fact]
    public async Task SetCountAsync_WorksWithValueType()
    {
        var vm = new TestViewModel();
        var changed = false;
        vm.PropertyChangedAsync += (args, ct) => { changed = true; return ValueTask.CompletedTask; };

        await vm.SetCountAsync(42);

        Assert.True(changed);
        Assert.Equal(42, vm.Count);
    }

    [Fact]
    public async Task SetNameAsync_ChangedEvent_ContainsOldAndNewValues()
    {
        var vm = new TestViewModel();
        string? capturedOld = null;
        string? capturedNew = null;

        vm.PropertyChangedAsync += (args, ct) =>
        {
            capturedOld = args.OldValue.As<string>();
            capturedNew = args.NewValue.As<string>();
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
        string? capturedOld = null;
        string? capturedNew = null;

        vm.PropertyChangingAsync += (args, ct) =>
        {
            capturedOld = args.OldValue.As<string>();
            capturedNew = args.NewValue.As<string>();
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

        string? oldInChanging = null;
        string? oldInChanged  = null;

        vm.PropertyChangingAsync += (args, ct) => { oldInChanging = args.OldValue.As<string>(); return ValueTask.CompletedTask; };
        vm.PropertyChangedAsync  += (args, ct) => { oldInChanged  = args.OldValue.As<string>(); return ValueTask.CompletedTask; };

        await vm.SetNameAsync("second");

        Assert.Equal("first", oldInChanging); // not yet changed when Changing fires
        Assert.Equal("first", oldInChanged);  // same old value captured before assignment
    }

    [Fact]
    public async Task SetCountAsync_ChangingAndChangedEvents_CarryTypedIntValues()
    {
        var vm = new TestViewModel();
        await vm.SetCountAsync(3);

        (string Name, int Old, int New)? changing = null;
        (string Name, int Old, int New)? changed = null;
        vm.PropertyChangingAsync += (args, ct) =>
        {
            changing = (args.PropertyName, args.OldValue.As<int>(), args.NewValue.As<int>());
            return ValueTask.CompletedTask;
        };
        vm.PropertyChangedAsync += (args, ct) =>
        {
            changed = (args.PropertyName, args.OldValue.As<int>(), args.NewValue.As<int>());
            return ValueTask.CompletedTask;
        };

        await vm.SetCountAsync(4);

        Assert.Equal(("Count", 3, 4), changing);
        Assert.Equal(("Count", 3, 4), changed);
    }

    [Fact]
    public async Task SetCountAsync_ChangedEvent_ValueDoesNotReadAsAnotherValueType()
    {
        var vm = new TestViewModel();
        var readAsLong = true;
        vm.PropertyChangedAsync += (args, ct) =>
        {
            readAsLong = args.NewValue.TryGet<long>(out _);
            return ValueTask.CompletedTask;
        };

        await vm.SetCountAsync(5);

        Assert.False(readAsLong);
    }
}
