using ZeroAlloc.Notify;

namespace ZeroAlloc.Notify.AotSmoke;

// A generic view model nested in a static class. The generator reopens the containing type and
// generates into the generic class itself, so its members use the class's type parameter.
public static partial class Catalog
{
    [NotifyPropertyChangedAsync]
    public partial class Entry<T>
    {
        [ObservableProperty] private T? _value;
    }
}
