namespace ZeroAlloc.Notify;

public sealed class AsyncPropertyChangedEventArgs
{
    public string PropertyName { get; }
    public AsyncPropertyChangedEventArgs(string propertyName) => PropertyName = propertyName;
}
