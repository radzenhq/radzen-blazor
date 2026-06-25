# Trimming runtime gates

Two runtime gates back the static contract tests in `Radzen.Blazor.Tests/TrimmingContractTests.cs`.
Both reference `Radzen.Blazor.TrimTest.Models`, a separate `<IsTrimmable>` assembly, so the trimmer
strips model members that Radzen reads by reflection - the real reflection-over-`T` scenario.

## 1. Console gate (required, deterministic, browser-free)

`Radzen.Blazor.TrimRuntimeTest` instantiates the item-typed generic components with the trimmable model,
then checks via reflection whether the trimmer preserved the model getters.

```
dotnet publish Radzen.Blazor.TrimRuntimeTest/Radzen.Blazor.TrimRuntimeTest.csproj \
    -c Release -r <rid> --self-contained -p:PublishTrimmed=true
./Radzen.Blazor.TrimRuntimeTest/bin/Release/net10.0/<rid>/publish/Radzen.Blazor.TrimRuntimeTest
```

Exit 1 today (getters trimmed). Exit 0 once the library annotates the item-typed component generics with
`[DynamicallyAccessedMembers(PublicProperties | PublicFields)]`.

## 2. WASM gate (faithful browser repro, optional)

`Radzen.Blazor.TrimTest` renders the components by string `Property` over the trimmable model and drives
sort/filter/group in `OnAfterRenderAsync`, reporting `done` / `error:<msg>` in `#trim-status`.

```
dotnet publish Radzen.Blazor.TrimTest/Radzen.Blazor.TrimTest.csproj -c Release
dotnet tool install -g dotnet-serve
dotnet serve -d Radzen.Blazor.TrimTest/bin/Release/net10.0/publish/wwwroot -p 5050 &
cd e2e && npm install && npx playwright install chromium
TRIM_URL=http://localhost:5050 node trim-smoke.mjs
```

Exit 1 today (the drive throws over the trimmed model). Exit 0 once the DAM fix lands (rooting
`RadzenDataGrid<Order>` preserves `Order`'s members app-wide).
