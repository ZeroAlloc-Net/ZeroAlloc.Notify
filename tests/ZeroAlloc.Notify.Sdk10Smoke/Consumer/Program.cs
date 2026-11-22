using ZeroAlloc.Notify;

// Uses the members the bundled generator emits. If the compiler skips the generator,
// SetNameAsync and PropertyChangedAsync do not exist and this does not build.
var vm = new UserViewModel();
string? changed = null;
vm.PropertyChangedAsync += (args, _) =>
{
    changed = args.PropertyName;
    return ValueTask.CompletedTask;
};

await vm.SetNameAsync("Alice");

if (vm.Name != "Alice" || changed != "Name")
{
    Console.Error.WriteLine("SDK 10 consumer: the generated setter did not raise the event.");
    return 1;
}

Console.WriteLine("SDK 10 consumer: generator loaded and the generated notifications work.");
return 0;

[NotifyPropertyChangedAsync]
public partial class UserViewModel
{
    [ObservableProperty]
    private string _name = "Anonymous";
}
