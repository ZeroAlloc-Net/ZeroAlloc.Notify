using ZeroAlloc.Notify;

namespace ZeroAlloc.Notify.AotSmoke;

[NotifyPropertyChangedAsync]
[NotifyPropertyChangingAsync]
public partial class UserViewModel
{
    [ObservableProperty] private string _name = "";
    [ObservableProperty] private int _age;
}
