namespace ZeroAlloc.Notify.Tests;

[NotifyPropertyChangedAsync]
[NotifyPropertyChangingAsync]
[NotifyDataErrorInfoAsync]
public partial class AllocationViewModel
{
    [ObservableProperty]
    private string _name = "";

    [ObservableProperty]
    private int _count;

    [ObservableProperty]
    [InvokeSequentially]
    private string _sequentialName = "";

    [ObservableProperty]
    [InvokeSequentially]
    private int _sequentialCount;

    public ValueTask RaiseChanged(string propertyName, object? oldValue, object? newValue)
        => RaisePropertyChangedAsync(propertyName, oldValue, newValue);

    public ValueTask RaiseChanging(string propertyName, object? oldValue, object? newValue)
        => RaisePropertyChangingAsync(propertyName, oldValue, newValue);

    public ValueTask RaiseErrors(string? propertyName) => RaiseErrorsChangedAsync(propertyName);

    public bool HasErrors => false;

    public System.Collections.IEnumerable GetErrors(string? propertyName) => Array.Empty<string>();
}
