using BenchmarkDotNet.Attributes;
using Notify.Benchmarks.Models;

namespace Notify.Benchmarks;

/// <summary>
/// Compares ZeroAlloc.Notify async property notification against framework alternatives.
/// Each model registers 5 handlers (realistic MVVM scenario).
/// </summary>
[MemoryDiagnoser]
public class NotifyComparisonBenchmarks
{
    private SyncViewModel _sync = null!;
    private AsyncViewModel _async = null!;
    private CommunityToolkitViewModel _communityToolkit = null!;
    private FodyViewModel _fody = null!;

    [GlobalSetup]
    public void Setup()
    {
        _sync = new SyncViewModel();
        _async = new AsyncViewModel();
        _communityToolkit = new CommunityToolkitViewModel();
        _fody = new FodyViewModel();

        for (var i = 0; i < 5; i++)
        {
            _sync.PropertyChanged += (_, _) => { };
            _async.PropertyChangedAsync += (args, ct) => ValueTask.CompletedTask;
            _communityToolkit.PropertyChanged += (_, _) => { };
            _fody.PropertyChanged += (_, _) => { };
        }
    }

    /// <summary>Manual INPC — baseline. No async support.</summary>
    [Benchmark(Baseline = true)]
    public void Sync_INotifyPropertyChanged()
        => _sync.Name = string.Equals(_sync.Name, "a", StringComparison.Ordinal) ? "b" : "a";

    /// <summary>CommunityToolkit.Mvvm source-generated INPC. No async support.</summary>
    [Benchmark]
    public void CommunityToolkit_SetName()
        => _communityToolkit.Name = string.Equals(_communityToolkit.Name, "a", StringComparison.Ordinal) ? "b" : "a";

    /// <summary>PropertyChanged.Fody IL-weaved INPC. No async support.</summary>
    [Benchmark]
    public void Fody_SetName()
        => _fody.Name = string.Equals(_fody.Name, "a", StringComparison.Ordinal) ? "b" : "a";

    /// <summary>ZeroAlloc.Notify — fully awaitable ValueTask, zero allocation.</summary>
    [Benchmark]
    public ValueTask ZeroAlloc_SetNameAsync()
        => _async.SetNameAsync(string.Equals(_async.Name, "a", StringComparison.Ordinal) ? "b" : "a");
}
