using System.ComponentModel;

namespace Notify.Benchmarks.Models;

/// <summary>
/// PropertyChanged.Fody IL-weaved INPC viewmodel.
/// Auto-properties have PropertyChanged raised automatically via IL weaving at build time.
/// Sync only — no async handler awaiting.
/// </summary>
public class FodyViewModel : INotifyPropertyChanged
{
    // CS0067: Fody weaves the invocation at IL level, invisible to Roslyn.
#pragma warning disable CS0067
    public event PropertyChangedEventHandler? PropertyChanged;
#pragma warning restore CS0067

    public string Name { get; set; } = "";
}
