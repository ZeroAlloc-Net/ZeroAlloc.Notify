using ZeroAlloc.Notify;

namespace Notify.Benchmarks.Models;

[NotifyPropertyChangedAsync]
[NotifyPropertyChangingAsync]
public partial class AsyncViewModel
{
    [ObservableProperty]
    private string _name = "";
}
