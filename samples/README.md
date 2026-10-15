# Samples

| Sample | Description |
|--------|-------------|
| [ZeroAlloc.Notify.AotSmoke](ZeroAlloc.Notify.AotSmoke/) | Console app that drives the generated `SetXxxAsync` setters and the async `PropertyChanging` / `PropertyChanged` events over reference, primitive, struct, nullable and enum properties. CI publishes it with `PublishAot=true` and runs it. |

Run it from the repository root:

```shell
dotnet run --project samples/ZeroAlloc.Notify.AotSmoke
```

For collection changes, validation and view-model patterns, see the guides in [docs/](../docs/):
[Getting Started](../docs/getting-started.md), [Observable Properties](../docs/observable-properties.md),
[Collection Changes](../docs/collection-changes.md) and [Validation](../docs/validation.md).
