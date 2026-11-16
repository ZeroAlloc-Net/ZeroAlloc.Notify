# AGENTS.md: ZeroAlloc.Notify

Source-generated, awaitable `INotifyPropertyChanged` for .NET. Two packages ship: `ZeroAlloc.Notify`
(runtime types, bundles the generator) and `ZeroAlloc.Notify.Generator` (the Roslyn incremental
generator, `netstandard2.0`). `samples/` and `benchmarks/` do not ship.

## Layout

- `src/ZeroAlloc.Notify.Generator` emits all plumbing; `Writers/NotifyWriter.cs` writes the output.
  The main package carries the generator DLL under `analyzers/dotnet/cs` through a custom target in its
  csproj, so a generator change needs a rebuild of both projects.
- Generator tests compare output with Verify snapshots in `tests/ZeroAlloc.Notify.Tests/Snapshots/`
  (`*.verified.cs`). A generator output change means updating the snapshots in the same PR; review the
  diff, do not accept blindly.
- `samples/ZeroAlloc.Notify.AotSmoke` is a NativeAOT app (`net11.0`) that CI publishes and runs.

## Build and test

Prerequisites: the SDK pinned in `global.json` (11.0.100, roll forward to latest feature), plus the
.NET 8, 10 and 11 runtimes. The tests target `net8.0`, `net10.0` and `net11.0`, and each needs its own
runtime. Warnings are errors, including RS0016 and RS0017.

CI job `build` in `.github/workflows/ci.yml`:

```bash
dotnet tool restore
dotnet restore
dotnet build --no-restore -c Release -p:Version=$VERSION
dotnet test --no-build -c Release --collect "XPlat Code Coverage" --settings coverage.runsettings --results-directory TestResults
dotnet pack src/ZeroAlloc.Notify/ZeroAlloc.Notify.csproj --no-build -c Release -p:PackageVersion=$VERSION -o ./artifacts
```

`$VERSION` comes from `dotnet gitversion /showvariable SemVer`. It must be passed to the build too:
`dotnet pack` fails with ZAVERSION001 or ZAVERSION002 otherwise. The same job packs the Generator
project the same way and gates coverage at 80 percent line coverage.

CI job `aot-smoke`:

```bash
dotnet publish samples/ZeroAlloc.Notify.AotSmoke/ZeroAlloc.Notify.AotSmoke.csproj -r linux-x64 -c Release -o ./aot-out
timeout --kill-after=10s 120s ./aot-out/ZeroAlloc.Notify.AotSmoke
```

CI job `api-compat` calls the shared workflow in ZeroAlloc-Net/.github.

Fast inner loop, verified locally:

```bash
dotnet build -c Release -p:Version=1.2.3
dotnet test --no-build -c Release -f net8.0
dotnet test --no-build -c Release -f net10.0 --filter "FullyQualifiedName~AllocationTests"
```

## Conventions and gotchas

- Public API is tracked: every public change goes into `src/ZeroAlloc.Notify/PublicAPI.Unshipped.txt`,
  and a new or changed ZAN diagnostic into `AnalyzerReleases.Unshipped.md` in the Generator project. The
  Shipped files are moved by the release workflow, never by hand.
- The runtime library enables AOT analyzers on the concrete targets (`IsAotCompatible`), so reflection
  in `src/ZeroAlloc.Notify` fails the build.
- Allocation budgets are pinned by `AllocationTests`: 0 B with no subscribers, 40 B for the args object
  with a subscriber on 64-bit, plus a box per value-type value. A change to the setter hot path must keep
  these figures or change the tests and the README claim together.
- Generator code must stay incremental-cache friendly: model types are equatable (`EquatableArray`,
  `LocationInfo`), and `IncrementalCachingTests` guards it. Do not put `ISymbol` or `SyntaxNode` in a
  pipeline model.
- Test code compiles generator input against a per-framework reference set
  (`Basic.Reference.Assemblies.Net80`, `Net100`, `Net110`); a new target framework needs a matching
  reference package.

<!-- org-core:start sha256=c42b83cb821d67a5547aafe02bbf9d3f36a7d19379a5d378bc38d452b7db8451 (generated from docker-nuc scripts/agents-md/org-core-zeroalloc.md; do not edit here) -->
## Org rules

These rules hold in every ZeroAlloc-Net repo. Each library makes two promises: it works under
NativeAOT and trimming, and its hot path allocates nothing. The 2026-10-11 sweep found 16 of 28 repos
allocating on the hot path, most with docs claiming 0 B.

### Hot path

The operation a consumer runs repeatedly: `Send`, a cache hit, `Validate`, `Map`, `Serialize`, a
resilience call, a state transition, an append, a worker poll, including the idle poll. Startup and
registration are not hot path. For a source generator, the hot path is the code it emits: judge the
snapshots and the generated output.

### Allocation traps the sweep found

| Pattern | Instead |
|---|---|
| `async ValueTask` that only awaits and returns | `return new ValueTask<T>(inner.XAsync(ct));` |
| `Task.WhenAll` over tasks that already completed | check `IsCompletedSuccessfully`, await only what is pending |
| `TagList` or log `params object[]` built before `Enabled`/`IsEnabled` | check first, or `LoggerMessage.Define` |
| event args allocated before checking for subscribers | copy the handler to a local, check, then allocate |
| `new Serializer()` / `new Utf8JsonWriter()` per call | cache stateless instances, reuse writers with `Reset` |
| `GetString()` / `ToString()` round-trips in JSON converters | parse and format through spans |
| `ToArray()` / `ToList()` copies between layers | pass `ReadOnlyMemory<T>` or spans, copy once at most |
| LINQ in emitted code | emit `for` loops |
| `yield` iterator behind a public `GetEnumerator` | public struct enumerator, from the first release |
| `Enum.IsDefined(typeof(E), v)`, `decimal.GetBits(v)` | `Enum.IsDefined<E>`, `v.Scale` |
| configuration parsed or read per call | parse once and cache |
| an `AsyncLocal` write per call | pass state explicitly |
| a capturing lambda or method-group delegate per call | cache the delegate in a field, or a static lambda |
| a per-key object that is never removed | remove on release; otherwise it is a leak |

Inherent allocations are fine when documented: the consumer's own result, an exception on a failure
path, a one-time cache fill.

### Tests and benchmarks

- Every hot path has an allocation gate test (`AllocationGate` from ZeroAlloc.TestHelpers) that measures
  the whole public call, including any state setup.
- Budgets are exact. If a path allocates, assert that number and comment what it is.
- Test the generated code, not a hand-written stand-in, and cover every shape: each attribute, nested
  collections, and the asynchronous case with a fake that returns pending tasks.
- Benchmarks run in CI through the shared `benchmark-smoke.yml` and target the library's LTS
  frameworks. The benchmark body allocates nothing besides the library itself. Run locally with
  `--inProcess`.
- Publish exact allocations and same-run ratios against a `Baseline`, never absolute timings. Run with
  `--launchCount 3`, pin CPUs with `taskset`, give the spread, and label every table.

### NativeAOT and trimming

- Packable runtime projects, and projects consumers publish, set `IsAotCompatible=true` on modern
  target frameworks. Analyzers and source generators target `netstandard2.0`.
- No reflection or runtime code generation on a supported path. An opt-in reflection fallback carries
  honest `[RequiresUnreferencedCode]` / `[RequiresDynamicCode]`. Every suppression must be true;
  remove dead ones.
- The AOT smoke sample publishes with `TreatWarningsAsErrors=true`. If a third-party warning really
  blocks it, exclude only that code with `WarningsNotAsErrors` and name the assembly.
- The smoke runs the hot path of every package in the repo. A package without a smoke has unknown AOT
  status. Adapters run against real services in CI service containers; publish-only is a last resort.
  App and template smokes make authenticated requests.

### Docs and shipping

- Docs match measurements. After a hot-path change, re-measure and update `docs/performance.md` and the
  README. Never write "0 B" without a test that asserts it.
- New public API is `feat:`, even for a performance change. Public API changes are additive: never add
  an optional parameter to a shipped signature. A breaking end state goes in a `next-major` issue.
- A behaviour change gets a `## Behaviour change` section in the PR body.
- Conventional Commits with release-please. Subjects start lowercase, body lines stay within 100
  characters, and a body never contains nested parentheses: release-please drops the whole commit.
- Local packs use `9.9.9-local.<sha>`. After a release, verify the package on NuGet.
- Fix review findings in the PR; file an issue only for what cannot go in it.

Full rules, template wiring lessons and the reasons behind each rule: https://github.com/ZeroAlloc-Net/.github/blob/main/AGENTS.md
<!-- org-core:end -->
