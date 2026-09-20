using BenchmarkDotNet.Running;
using Notify.Benchmarks;

// Switcher rather than Runner<T> so command-line arguments reach BenchmarkDotNet: CI runs
// this with --filter and reduced iteration counts as a smoke check.
BenchmarkSwitcher.FromAssembly(typeof(NotifyComparisonBenchmarks).Assembly).Run(args);
