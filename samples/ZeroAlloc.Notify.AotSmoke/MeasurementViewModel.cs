using ZeroAlloc.Notify;

namespace ZeroAlloc.Notify.AotSmoke;

// Value-type observable properties. The generated setters compare through
// EqualityComparer<T>.Default and box old and new values into the event args, so
// each shape here takes a different generic instantiation under NativeAOT:
// an IEquatable struct, a struct relying on ValueType.Equals, a nullable primitive,
// a nullable struct and an enum. Nullable<T> is the shape dotnet/runtime#134799
// hangs on, which is how ZeroAlloc.Cache#182 shipped past a string/int-only smoke.
[NotifyPropertyChangedAsync]
[NotifyPropertyChangingAsync]
public partial class MeasurementViewModel
{
    [ObservableProperty] private SmokePoint _position;
    [ObservableProperty] private SmokeSize _size;
    [ObservableProperty] private int? _count;
    [ObservableProperty] private SmokePoint? _anchor;
    [ObservableProperty, InvokeSequentially] private SmokeStatus _status;
}
