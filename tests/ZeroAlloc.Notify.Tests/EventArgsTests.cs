using System.Collections.Specialized;

namespace ZeroAlloc.Notify.Tests;

public class EventArgsTests
{
    [Fact]
    public void AsyncPropertyChangedEventArgs_StoresNameAndValues()
    {
        var args = new AsyncPropertyChangedEventArgs("Name", PropertyValue.From("a"), PropertyValue.From("b"));
        Assert.Equal("Name", args.PropertyName);
        Assert.Equal("a", args.OldValue.As<string>());
        Assert.Equal("b", args.NewValue.As<string>());
    }

    [Fact]
    public void AsyncPropertyChangedEventArgs_Create_StoresTypedValues()
    {
        var args = AsyncPropertyChangedEventArgs.Create("Count", 1, 2);
        Assert.Equal("Count", args.PropertyName);
        Assert.Equal(1, args.OldValue.As<int>());
        Assert.Equal(2, args.NewValue.As<int>());
    }

    [Fact]
    public void AsyncPropertyChangingEventArgs_StoresNameAndValues()
    {
        var args = new AsyncPropertyChangingEventArgs("Name", PropertyValue.From("a"), PropertyValue.From("b"));
        Assert.Equal("Name", args.PropertyName);
        Assert.Equal("a", args.OldValue.As<string>());
        Assert.Equal("b", args.NewValue.As<string>());
    }

    [Fact]
    public void AsyncPropertyChangingEventArgs_Create_StoresTypedValues()
    {
        var args = AsyncPropertyChangingEventArgs.Create("Count", 1, 2);
        Assert.Equal("Count", args.PropertyName);
        Assert.Equal(1, args.OldValue.As<int>());
        Assert.Equal(2, args.NewValue.As<int>());
    }

    [Fact]
    public void AsyncErrorsChangedEventArgs_StoresNullablePropertyName()
    {
        var args = new AsyncErrorsChangedEventArgs(null);
        Assert.Null(args.PropertyName);
    }

    [Fact]
    public void AsyncCollectionChangedEventArgs_StoresAction()
    {
        var args = new AsyncCollectionChangedEventArgs(
            NotifyCollectionChangedAction.Reset,
            null, null, -1, -1);
        Assert.Equal(NotifyCollectionChangedAction.Reset, args.Action);
    }
}
