using ZeroAlloc.AsyncEvents;
using ZeroAlloc.Notify;

namespace ZeroAlloc.Notify.Tests;

[NotifyPropertyChangedAsync]
[NotifyPropertyChangingAsync]
public partial class TestViewModel
{
    [ObservableProperty]
    private string _name = "";

    [ObservableProperty]
    private int _count;
}
