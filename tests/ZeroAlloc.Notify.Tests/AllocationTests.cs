using ZeroAlloc.TestHelpers;

namespace ZeroAlloc.Notify.Tests;

/// <summary>
/// With no subscribers, a property change must not allocate: no event args object, and no
/// boxing of a value-type old or new value. Args are created only when someone listens.
/// </summary>
public class AllocationTests
{
    private const int Iterations = 10_000;

    [Fact]
    public void SetStringProperty_NoSubscribers_DoesNotAllocate()
    {
        var vm = new AllocationViewModel();
        var flip = false;
        AllocationGate.AssertBudget(0, Iterations, () =>
        {
            flip = !flip;
            Complete(vm.SetNameAsync(flip ? "a" : "b"));
        }, "SetNameAsync, no subscribers");
    }

    [Fact]
    public void SetIntProperty_NoSubscribers_DoesNotAllocate()
    {
        var vm = new AllocationViewModel();
        var i = 0;
        AllocationGate.AssertBudget(0, Iterations, () => Complete(vm.SetCountAsync(++i)),
            "SetCountAsync, no subscribers");
    }

    [Fact]
    public void SetSequentialIntProperty_NoSubscribers_DoesNotAllocate()
    {
        var vm = new AllocationViewModel();
        var i = 0;
        AllocationGate.AssertBudget(0, Iterations, () => Complete(vm.SetSequentialCountAsync(++i)),
            "SetSequentialCountAsync, no subscribers");
    }

    [Fact]
    public void RaiseMethods_NoSubscribers_DoNotAllocate()
    {
        var vm = new AllocationViewModel();
        AllocationGate.AssertBudget(0, Iterations, () =>
        {
            Complete(vm.RaiseChanged("Name", "a", "b"));
            Complete(vm.RaiseChanging("Name", "a", "b"));
            Complete(vm.RaiseErrors("Name"));
        }, "Raise*Async, no subscribers");
    }

    // With a subscriber, the only allocation Notify adds is the event args object, 40 B on 64-bit,
    // plus a box for each value-type old and new value. Making that allocation-free changes the
    // public args shape and is tracked as next-major work in #237. These gates pin the cost so
    // it cannot grow unnoticed. They use sequential dispatch: the default parallel dispatch in
    // ZeroAlloc.AsyncEvents adds its own allocation on net8 and net9, tracked in
    // ZeroAlloc-Net/ZeroAlloc.AsyncEvents#229, and that is not this library's cost to gate.
    [Fact]
    public void SetSequentialStringProperty_OneChangedSubscriber_AllocatesOnlyTheArgs()
    {
        var vm = new AllocationViewModel();
        vm.PropertyChangedAsync += (_, _) => ValueTask.CompletedTask;
        var flip = false;
        AllocationGate.AssertBudget(40, Iterations, () =>
        {
            flip = !flip;
            Complete(vm.SetSequentialNameAsync(flip ? "a" : "b"));
        }, "SetSequentialNameAsync, one PropertyChanged subscriber");
    }

    [Fact]
    public void SetSequentialIntProperty_OneChangedSubscriber_AllocatesOnlyTheArgsAndBoxes()
    {
        var vm = new AllocationViewModel();
        vm.PropertyChangedAsync += (_, _) => ValueTask.CompletedTask;
        var i = 0;
        AllocationGate.AssertBudget(88, Iterations, () => Complete(vm.SetSequentialCountAsync(++i)),
            "SetSequentialCountAsync, one PropertyChanged subscriber");
    }

    [Fact]
    public async Task SetIntProperty_AfterUnsubscribe_StillRaisesForRemainingAndSkipsWhenEmpty()
    {
        var vm = new AllocationViewModel();
        var raised = 0;
        AsyncEvents.AsyncEvent<AsyncPropertyChangedEventArgs> handler = (_, _) =>
        {
            raised++;
            return ValueTask.CompletedTask;
        };

        vm.PropertyChangedAsync += handler;
        await vm.SetCountAsync(1);
        vm.PropertyChangedAsync -= handler;
        await vm.SetCountAsync(2);

        Assert.Equal(1, raised);
        Assert.Equal(2, vm.Count);
    }

    private static void Complete(ValueTask task)
    {
        Assert.True(task.IsCompletedSuccessfully);
        task.GetAwaiter().GetResult();
    }
}
