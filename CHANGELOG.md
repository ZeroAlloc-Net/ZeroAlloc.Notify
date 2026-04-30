# Changelog

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
