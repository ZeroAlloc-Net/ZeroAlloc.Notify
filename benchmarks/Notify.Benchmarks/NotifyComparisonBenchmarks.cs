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

    [GlobalSetup]
    public void Setup()
    {
        _sync = new SyncViewModel();
        _async = new AsyncViewModel();

        for (var i = 0; i < 5; i++)
        {
            _sync.PropertyChanged += (_, _) => { };
            _async.PropertyChangedAsync += (args, ct) => ValueTask.CompletedTask;
        }
    }

    /// <summary>Synchronous INPC — baseline. Does not support awaiting handlers.</summary>
    [Benchmark(Baseline = true)]
    public void Sync_INotifyPropertyChanged()
        => _sync.Name = string.Equals(_sync.Name, "a", StringComparison.Ordinal) ? "b" : "a";  // alternates to avoid early-return skip

    /// <summary>ZeroAlloc async notify — fully awaitable, no fire-and-forget.</summary>
    [Benchmark]
    public ValueTask ZeroAlloc_SetNameAsync()
        => _async.SetNameAsync(string.Equals(_async.Name, "a", StringComparison.Ordinal) ? "b" : "a");
}
