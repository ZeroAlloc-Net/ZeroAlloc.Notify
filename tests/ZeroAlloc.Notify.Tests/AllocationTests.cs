using ZeroAlloc.TestHelpers;

namespace ZeroAlloc.Notify.Tests;

/// <summary>
/// A property change allocates nothing, with or without subscribers. The property event args
/// are structs, and their old and new values are <see cref="PropertyValue"/>s, which keep a small
/// value type inline instead of boxing it.
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

    // With subscribers, a change still allocates nothing when the handlers complete synchronously,
    // for sequential dispatch and for the default parallel dispatch. Before #237 the args were a
    // 40 B class on 64-bit, plus a box for each value-type old and new value.
    [Fact]
    public void SetSequentialStringProperty_OneChangedSubscriber_DoesNotAllocate()
    {
        var vm = new AllocationViewModel();
        vm.PropertyChangedAsync += (_, _) => ValueTask.CompletedTask;
        var flip = false;
        AllocationGate.AssertBudget(0, Iterations, () =>
        {
            flip = !flip;
            Complete(vm.SetSequentialNameAsync(flip ? "a" : "b"));
        }, "SetSequentialNameAsync, one PropertyChanged subscriber");
    }

    [Fact]
    public void SetSequentialIntProperty_OneChangedSubscriber_DoesNotAllocate()
    {
        var vm = new AllocationViewModel();
        vm.PropertyChangedAsync += (_, _) => ValueTask.CompletedTask;
        var i = 0;
        AllocationGate.AssertBudget(0, Iterations, () => Complete(vm.SetSequentialCountAsync(++i)),
            "SetSequentialCountAsync, one PropertyChanged subscriber");
    }

    [Fact]
    public void SetStringProperty_FiveChangedSubscribers_DoesNotAllocate()
    {
        var vm = new AllocationViewModel();
        for (var h = 0; h < 5; h++)
            vm.PropertyChangedAsync += (_, _) => ValueTask.CompletedTask;
        var flip = false;
        AllocationGate.AssertBudget(0, Iterations, () =>
        {
            flip = !flip;
            Complete(vm.SetNameAsync(flip ? "a" : "b"));
        }, "SetNameAsync, five PropertyChanged subscribers, parallel dispatch");
    }

    [Fact]
    public void SetIntProperty_FiveChangedSubscribers_DoesNotAllocate()
    {
        var vm = new AllocationViewModel();
        for (var h = 0; h < 5; h++)
            vm.PropertyChangedAsync += (_, _) => ValueTask.CompletedTask;
        var i = 0;
        AllocationGate.AssertBudget(0, Iterations, () => Complete(vm.SetCountAsync(++i)),
            "SetCountAsync, five PropertyChanged subscribers, parallel dispatch");
    }

    [Fact]
    public void SetIntProperty_ChangingAndChangedSubscribers_DoesNotAllocate()
    {
        var vm = new AllocationViewModel();
        vm.PropertyChangingAsync += (_, _) => ValueTask.CompletedTask;
        vm.PropertyChangedAsync += (_, _) => ValueTask.CompletedTask;
        var i = 0;
        AllocationGate.AssertBudget(0, Iterations, () => Complete(vm.SetCountAsync(++i)),
            "SetCountAsync, one PropertyChanging and one PropertyChanged subscriber");
    }

    [Fact]
    public void RaiseMethods_WithSubscribersAndValueTypeValues_DoNotAllocate()
    {
        var vm = new AllocationViewModel();
        vm.PropertyChangingAsync += (_, _) => ValueTask.CompletedTask;
        vm.PropertyChangedAsync += (_, _) => ValueTask.CompletedTask;
        AllocationGate.AssertBudget(0, Iterations, () =>
        {
            Complete(vm.RaiseChanged("Count", 1, 2));
            Complete(vm.RaiseChanging("Count", 1, 2));
        }, "Raise*Async with int values, one subscriber each");
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
