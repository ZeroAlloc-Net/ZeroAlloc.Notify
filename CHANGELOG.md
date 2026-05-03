# Changelog

## [1.2.0](https://github.com/ZeroAlloc-Net/ZeroAlloc.Notify/compare/v1.1.1...v1.2.0) (2026-05-03)


### Features

* add notify attribute definitions ([58220bd](https://github.com/ZeroAlloc-Net/ZeroAlloc.Notify/commit/58220bd51d0614347b6aa62db4b526cf8acce046))
* add NotifyGenerator with all four async notify interfaces ([11d4c5e](https://github.com/ZeroAlloc-Net/ZeroAlloc.Notify/commit/11d4c5e8c559beb27384ad40482766ca8013578c))
* add OldValue and NewValue to AsyncPropertyChangedEventArgs ([dd6796a](https://github.com/ZeroAlloc-Net/ZeroAlloc.Notify/commit/dd6796a47ecb76e036ca6c91b6094908ad25b4bd))
* add OldValue and NewValue to AsyncPropertyChangingEventArgs ([6428def](https://github.com/ZeroAlloc-Net/ZeroAlloc.Notify/commit/6428def25b58d3d80e413f0c59cfbb1e29f0e7a8))
* bundle source generator into ZeroAlloc.Notify package ([971f2cb](https://github.com/ZeroAlloc-Net/ZeroAlloc.Notify/commit/971f2cb9ecda322e9d1cae9a197ee98b1b6ecef7))
* bundle source generator into ZeroAlloc.Notify package ([97bbaa6](https://github.com/ZeroAlloc-Net/ZeroAlloc.Notify/commit/97bbaa68d54da6cc236caba4e60b57a423c2b772))
* lock public API surface (PublicApiAnalyzers + api-compat gate) ([#43](https://github.com/ZeroAlloc-Net/ZeroAlloc.Notify/issues/43)) ([64c1f74](https://github.com/ZeroAlloc-Net/ZeroAlloc.Notify/commit/64c1f7400331560e418687a6db1d48330379bf58))
* pass OldValue and NewValue through generator to property event args ([97e63f2](https://github.com/ZeroAlloc-Net/ZeroAlloc.Notify/commit/97e63f217fdcbf3ee26ece91969c73badb184cd5))


### Bug Fixes

* add frontmatter with slug: / to getting-started for root URL ([2244813](https://github.com/ZeroAlloc-Net/ZeroAlloc.Notify/commit/22448135578f924357b6e6214da3833b044bc81c))
* pack generator DLL under analyzers/dotnet/cs ([baabb5a](https://github.com/ZeroAlloc-Net/ZeroAlloc.Notify/commit/baabb5a8ae2a458e88000622404638d868c7de7d))
* pack generator DLL under analyzers/dotnet/cs ([f2dde99](https://github.com/ZeroAlloc-Net/ZeroAlloc.Notify/commit/f2dde993175b724dafd7969c25e3dc627d9a6973))
* replace out-of-docs relative link in performance.md with inline code ([3bb1531](https://github.com/ZeroAlloc-Net/ZeroAlloc.Notify/commit/3bb15310828a121c533398ccdf6f280b649d003c))
* respect field-level InvokeSequentially in SetXxxAsync; add snapshot test ([6475907](https://github.com/ZeroAlloc-Net/ZeroAlloc.Notify/commit/64759073c1d2c01cb3a82551ccdb55e7ff84c3ec))


### Performance

* add CommunityToolkitViewModel for benchmark comparison ([14338a6](https://github.com/ZeroAlloc-Net/ZeroAlloc.Notify/commit/14338a69fe6df0da85e2778feeacc440e52548a3))
* add FodyViewModel for benchmark comparison ([c4a5b7d](https://github.com/ZeroAlloc-Net/ZeroAlloc.Notify/commit/c4a5b7d3f4fa50f2dc51e0d9fa287e2d1c4c2d52))
* add INPC framework comparison benchmarks ([e5b8642](https://github.com/ZeroAlloc-Net/ZeroAlloc.Notify/commit/e5b86425418c6f7818c664acbc8742050fc8c05d))
* extend benchmark with CommunityToolkit.Mvvm and PropertyChanged.Fody comparisons ([6a8351d](https://github.com/ZeroAlloc-Net/ZeroAlloc.Notify/commit/6a8351d863025b6e155117f3d4c7bbeac9c287ca))
* update benchmark tables with real 4-way comparison results ([3fc3814](https://github.com/ZeroAlloc-Net/ZeroAlloc.Notify/commit/3fc381490c2d790588c268116f0b8c8de5b0ecb3))


### Refactoring

* update generator to emit ZeroAlloc.Notify namespace for interfaces and event args ([db7e8a7](https://github.com/ZeroAlloc-Net/ZeroAlloc.Notify/commit/db7e8a7e0a6ac376b32dbb0fe506f5e14a40f75c))


### Documentation

* add GitHub Sponsors badge to README ([0cc72f7](https://github.com/ZeroAlloc-Net/ZeroAlloc.Notify/commit/0cc72f71da9e85f25d9aab6a87215245154f53d2))
* add GitHub Sponsors badge to README ([13329a2](https://github.com/ZeroAlloc-Net/ZeroAlloc.Notify/commit/13329a2f1f290d8a80c5940da94ea01f5cab7d48))
* add missing documentation pages and sample directory stubs ([e7ecdc0](https://github.com/ZeroAlloc-Net/ZeroAlloc.Notify/commit/e7ecdc054b6957b4bda2ef7fab839cbca5526436))
* add pre-existing getting-started, observable-properties, async-notifications pages ([4376759](https://github.com/ZeroAlloc-Net/ZeroAlloc.Notify/commit/4376759465c41ea3db5f3ab877da6c72dcc28731))
* add publication design doc ([30e873c](https://github.com/ZeroAlloc-Net/ZeroAlloc.Notify/commit/30e873cee03444caba5f2720ddb05bac1a27a9f9))
* add publication implementation plan ([aa58d38](https://github.com/ZeroAlloc-Net/ZeroAlloc.Notify/commit/aa58d388d3d8861507add4bbbffe3cf837e7a657))
* correct allocation claims to reflect real benchmark results ([0f7e5d3](https://github.com/ZeroAlloc-Net/ZeroAlloc.Notify/commit/0f7e5d339c375851d5827142d95398acb4974199))
* fix API examples to match actual implementation (remove partial method pattern, fix event args properties, correct validation API) ([5c22964](https://github.com/ZeroAlloc-Net/ZeroAlloc.Notify/commit/5c229646f0a5eaf206ba620329403fff55eff331))
* **readme:** standardize 5-badge set ([305f4d5](https://github.com/ZeroAlloc-Net/ZeroAlloc.Notify/commit/305f4d533baabe73ce2a77f6a3db0d5500f2ac8b))
* **readme:** standardize 5-badge set (NuGet/Build/License/AOT/Sponsors) ([773f8b5](https://github.com/ZeroAlloc-Net/ZeroAlloc.Notify/commit/773f8b551efc8d39d06ae623a703c928fdf09d2c))
* update install instructions for bundled generator ([902e5e4](https://github.com/ZeroAlloc-Net/ZeroAlloc.Notify/commit/902e5e4237c1ffda5b5d2d36d899401d4598b873))


### Tests

* add end-to-end integration tests for ZeroAlloc.Notify ([3af1283](https://github.com/ZeroAlloc-Net/ZeroAlloc.Notify/commit/3af1283b5ebd09d239fbfa726e0f4b505ad4b0d1))
* add failing tests for OldValue/NewValue on property event args ([cf77d36](https://github.com/ZeroAlloc-Net/ZeroAlloc.Notify/commit/cf77d36e5f611e376eb5861b3edf781241e246f8))
* add InterfaceTests and EventArgsTests for moved types ([94ba427](https://github.com/ZeroAlloc-Net/ZeroAlloc.Notify/commit/94ba427a4a45ba0e38bc0aa864c047c0de85f683))
* update snapshots for OldValue/NewValue generator output ([de343f2](https://github.com/ZeroAlloc-Net/ZeroAlloc.Notify/commit/de343f276abbf6d2293ba380c0c560273a0fbf1a))

## [1.1.0](https://github.com/ZeroAlloc-Net/ZeroAlloc.Notify/compare/v1.0.1...v1.1.0) (2026-05-01)


### Features

* bundle source generator into ZeroAlloc.Notify package ([971f2cb](https://github.com/ZeroAlloc-Net/ZeroAlloc.Notify/commit/971f2cb9ecda322e9d1cae9a197ee98b1b6ecef7))
* bundle source generator into ZeroAlloc.Notify package ([97bbaa6](https://github.com/ZeroAlloc-Net/ZeroAlloc.Notify/commit/97bbaa68d54da6cc236caba4e60b57a423c2b772))
* lock public API surface (PublicApiAnalyzers + api-compat gate) ([#43](https://github.com/ZeroAlloc-Net/ZeroAlloc.Notify/issues/43)) ([64c1f74](https://github.com/ZeroAlloc-Net/ZeroAlloc.Notify/commit/64c1f7400331560e418687a6db1d48330379bf58))

## [1.0.1](https://github.com/ZeroAlloc-Net/ZeroAlloc.Notify/compare/v1.0.0...v1.0.1) (2026-04-30)


### Bug Fixes

* pack generator DLL under analyzers/dotnet/cs ([baabb5a](https://github.com/ZeroAlloc-Net/ZeroAlloc.Notify/commit/baabb5a8ae2a458e88000622404638d868c7de7d))
* pack generator DLL under analyzers/dotnet/cs ([f2dde99](https://github.com/ZeroAlloc-Net/ZeroAlloc.Notify/commit/f2dde993175b724dafd7969c25e3dc627d9a6973))

## 1.0.0 (2026-04-01)


### Features

* add notify attribute definitions ([58220bd](https://github.com/ZeroAlloc-Net/ZeroAlloc.Notify/commit/58220bd51d0614347b6aa62db4b526cf8acce046))
* add NotifyGenerator with all four async notify interfaces ([11d4c5e](https://github.com/ZeroAlloc-Net/ZeroAlloc.Notify/commit/11d4c5e8c559beb27384ad40482766ca8013578c))
* add OldValue and NewValue to AsyncPropertyChangedEventArgs ([dd6796a](https://github.com/ZeroAlloc-Net/ZeroAlloc.Notify/commit/dd6796a47ecb76e036ca6c91b6094908ad25b4bd))
* add OldValue and NewValue to AsyncPropertyChangingEventArgs ([6428def](https://github.com/ZeroAlloc-Net/ZeroAlloc.Notify/commit/6428def25b58d3d80e413f0c59cfbb1e29f0e7a8))
* pass OldValue and NewValue through generator to property event args ([97e63f2](https://github.com/ZeroAlloc-Net/ZeroAlloc.Notify/commit/97e63f217fdcbf3ee26ece91969c73badb184cd5))


### Bug Fixes

* add frontmatter with slug: / to getting-started for root URL ([2244813](https://github.com/ZeroAlloc-Net/ZeroAlloc.Notify/commit/22448135578f924357b6e6214da3833b044bc81c))
* replace out-of-docs relative link in performance.md with inline code ([3bb1531](https://github.com/ZeroAlloc-Net/ZeroAlloc.Notify/commit/3bb15310828a121c533398ccdf6f280b649d003c))
* respect field-level InvokeSequentially in SetXxxAsync; add snapshot test ([6475907](https://github.com/ZeroAlloc-Net/ZeroAlloc.Notify/commit/64759073c1d2c01cb3a82551ccdb55e7ff84c3ec))


### Performance Improvements

* add CommunityToolkitViewModel for benchmark comparison ([14338a6](https://github.com/ZeroAlloc-Net/ZeroAlloc.Notify/commit/14338a69fe6df0da85e2778feeacc440e52548a3))
* add FodyViewModel for benchmark comparison ([c4a5b7d](https://github.com/ZeroAlloc-Net/ZeroAlloc.Notify/commit/c4a5b7d3f4fa50f2dc51e0d9fa287e2d1c4c2d52))
* add INPC framework comparison benchmarks ([e5b8642](https://github.com/ZeroAlloc-Net/ZeroAlloc.Notify/commit/e5b86425418c6f7818c664acbc8742050fc8c05d))
* extend benchmark with CommunityToolkit.Mvvm and PropertyChanged.Fody comparisons ([6a8351d](https://github.com/ZeroAlloc-Net/ZeroAlloc.Notify/commit/6a8351d863025b6e155117f3d4c7bbeac9c287ca))
* update benchmark tables with real 4-way comparison results ([3fc3814](https://github.com/ZeroAlloc-Net/ZeroAlloc.Notify/commit/3fc381490c2d790588c268116f0b8c8de5b0ecb3))
