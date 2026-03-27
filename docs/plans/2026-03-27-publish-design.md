# ZeroAlloc.Notify — Publication Design

**Date:** 2026-03-27
**Status:** Approved

## Goal

Publish `ZeroAlloc.Notify` and `ZeroAlloc.Notify.Generator` to NuGet with full CI/CD automation, extended framework-comparison benchmarks, and complete documentation matching the Mediator project style.

## Scope

Four deliverables:
1. AsyncEvents dependency resolved (prerequisite)
2. Benchmarks extended to compare CommunityToolkit.Mvvm and PropertyChanged.Fody
3. Missing documentation pages created
4. GitHub CI/release workflows added

## Approach

Sequential release: publish `ZeroAlloc.AsyncEvents` first, switch the local `ProjectReference` to a `PackageReference`, then deliver all three remaining items.

---

## Section 1 — Prerequisites

### 1a. Trigger AsyncEvents Release

`ZeroAlloc.AsyncEvents` (sibling repo at `github.com/ZeroAlloc-Net/ZeroAlloc.AsyncEvents`) already has release-please configured. Create a release PR there and merge it to push `ZeroAlloc.AsyncEvents` to NuGet.

### 1b. Switch Dependency in Notify

In `src/ZeroAlloc.Notify/ZeroAlloc.Notify.csproj`, replace:

```xml
<!-- Local reference until ZeroAlloc.AsyncEvents is published -->
<ProjectReference Include="..\..\..\ZeroAlloc.AsyncEvents\src\ZeroAlloc.AsyncEvents\ZeroAlloc.AsyncEvents.csproj" />
```

With:

```xml
<PackageReference Include="ZeroAlloc.AsyncEvents" Version="[published-version]" />
```

---

## Section 2 — Benchmarks

### 2a. Project Changes

- Bump `TargetFramework` from `net9.0` → `net10.0` in `benchmarks/Notify.Benchmarks/Notify.Benchmarks.csproj`
- Add NuGet references:
  - `CommunityToolkit.Mvvm`
  - `PropertyChanged.Fody`

### 2b. New Model Classes

**`Models/CommunityToolkitViewModel.cs`**
Uses `[ObservableProperty]` via CommunityToolkit.Mvvm source generator. Inherits `ObservableObject`. Sync INPC only — no async handler awaiting.

**`Models/FodyViewModel.cs`**
Bare class implementing `INotifyPropertyChanged` with a plain `Name` auto-property. PropertyChanged.Fody weaves `PropertyChanged` invocation at IL level at build time. Sync only.

### 2c. Benchmark Cases

Extend `NotifyComparisonBenchmarks` with:

| Benchmark method | Framework | Notes |
|-----------------|-----------|-------|
| `Sync_INotifyPropertyChanged` (existing, baseline) | Manual INPC | `[Benchmark(Baseline = true)]` |
| `CommunityToolkit_SetName` (new) | CommunityToolkit.Mvvm | Source-generated sync INPC |
| `Fody_SetName` (new) | PropertyChanged.Fody | IL-weaved sync INPC |
| `ZeroAlloc_SetNameAsync` (existing) | ZeroAlloc.Notify | Fully awaitable ValueTask |

All models register 5 handlers in `GlobalSetup` for a realistic MVVM scenario.

### 2d. README / docs/performance.md Update

Update the performance table in both `README.md` and `docs/performance.md` to a 4-row comparison after running benchmarks on the CI machine. Add prose notes explaining that CommunityToolkit and Fody do not support awaitable async handler dispatch.

---

## Section 3 — Documentation

Create the following missing pages (referenced in README.md but absent from `docs/`):

| File | Topic |
|------|-------|
| `docs/collection-changes.md` | `INotifyCollectionChangedAsync`, observable collection usage |
| `docs/validation.md` | `INotifyDataErrorInfoAsync`, async error handling patterns |
| `docs/diagnostics.md` | ZAN001–ZAN010 source generator diagnostic reference with causes and fixes |
| `docs/advanced-patterns.md` | Cancellation, scoped bindings, `[InvokeSequentially]`, parallel handler dispatch |
| `docs/testing.md` | Unit-testing observable models and notification flows |

Also add a minimal `samples/` directory with placeholder READMEs for the four scenarios listed in README.md (WPF MVVM, ASP.NET Core, Console, Collection).

---

## Section 4 — GitHub Workflows

Three workflow files under `.github/workflows/`, mirroring `ZeroAlloc.Mediator` style:

### `ci.yml`

- **Trigger:** push to `main` or `release-please--**` branches, PR to `main`, `workflow_dispatch`
- **Steps:** checkout → setup .NET 10 → restore tools → GitVersion → restore → build → test → pack both packages → push pre-release to NuGet on `main` push

Both packages packed:
- `src/ZeroAlloc.Notify/ZeroAlloc.Notify.csproj`
- `src/ZeroAlloc.Notify.Generator/ZeroAlloc.Notify.Generator.csproj`

### `release-please.yml`

- **Trigger:** push to `main`
- **Permissions:** `contents: write`, `pull-requests: write`
- **Steps:** release-please-action v4 (release-type: simple) → on release created: build + test + pack both + push stable NuGet + upload `.nupkg` to GitHub release assets

### `release.yml`

- **Trigger:** GitHub release published
- **Steps:** checkout → setup .NET 10 → restore tools → extract version from tag → restore → build → test → pack both → push to NuGet

---

## Out of Scope

- Samples with full working code (stubs only)
- AsyncEvents CI setup (already done in that repo)
- Collection/validation runtime implementation changes (docs only)
