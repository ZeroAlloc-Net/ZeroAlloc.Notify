using ZeroAlloc.AsyncEvents;

namespace ZeroAlloc.Notify;

public interface INotifyPropertyChangedAsync
{
#pragma warning disable MA0046 // async event delegates intentionally return ValueTask
    event AsyncEvent<AsyncPropertyChangedEventArgs> PropertyChangedAsync;
#pragma warning restore MA0046
}
