namespace ZeroAlloc.Notify;

public sealed class AsyncPropertyChangingEventArgs
{
    public string PropertyName { get; }
    public object? OldValue { get; }
    public object? NewValue { get; }

    public AsyncPropertyChangingEventArgs(string propertyName, object? oldValue, object? newValue)
    {
        PropertyName = propertyName;
        OldValue = oldValue;
        NewValue = newValue;
    }
}
