namespace ZeroAlloc.Notify;

public sealed class AsyncPropertyChangingEventArgs
{
    public string PropertyName { get; }
    public AsyncPropertyChangingEventArgs(string propertyName) => PropertyName = propertyName;
}
