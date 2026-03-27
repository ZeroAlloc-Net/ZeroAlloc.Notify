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

        Assert.Equal(new[] { "changing", "changed" }, order);
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
}
