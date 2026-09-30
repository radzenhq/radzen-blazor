# Trimming Radzen.Blazor

Radzen.Blazor is marked `<IsTrimmable>true</IsTrimmable>`, so when you publish a Blazor
WebAssembly app the IL trimmer can remove the parts of Radzen.Blazor you don't use, shrinking your
download. This page explains what works automatically, the few cases where you need to help the
trimmer, and how.

## TL;DR

- **Most apps need to do nothing.** If your model/DTO types live in your app project or a normal
  `Shared` class library (the default), the trimmer does not strip their members, and every
  Radzen.Blazor component works as-is.
- You only need the guidance below if you **aggressively trim your own model assembly** - i.e. you
  mark it `<IsTrimmable>true</IsTrimmable>` or publish the app with `<TrimMode>full</TrimMode>`.
- **Native AOT is not supported** (Blazor itself does not run under Native AOT).

## Does this affect me?

Blazor WebAssembly trims on a `Release` publish, but in the default **partial** mode it only
member-trims assemblies that opt in with `IsTrimmable`. Your app assembly and a plain `Shared`
library are **not** opted in, so their types keep all their members. Radzen.Blazor reflects over your
model by property name (e.g. `Property="LastName"`), and because your model isn't trimmed, that
reflection always succeeds.

You enter the cases below only if **you** trim the assembly that holds your models:

```xml
<!-- Either of these trims YOUR model assembly and triggers the guidance below: -->
<IsTrimmable>true</IsTrimmable>      <!-- in the model/Shared project -->
<TrimMode>full</TrimMode>            <!-- in the WASM app project -->
```

## What works out of the box (even if your model assembly is trimmed)

Components whose **generic type parameter is the data item type** carry
`[DynamicallyAccessedMembers]`, so the trimmer keeps the members those components bind by string name.
This covers, among others:

- `RadzenDataGrid` / `RadzenDataGridColumn` (`Property`, `SortProperty`, `GroupProperty`, filtering)
- `RadzenDataList`, `RadzenPivotDataGrid`, `RadzenDataFilter`
- `RadzenScheduler` (`StartProperty`, `EndProperty`, ...)
- `RadzenGantt`, `RadzenSankeyDiagram`
- All chart series (`RadzenColumnSeries`, `RadzenLineSeries`, `RadzenPieSeries`, box-plot, OHLC,
  bubble, ... - their `CategoryProperty` / `ValueProperty` and stat properties)

DataGrid string filtering (`Contains`/`StartsWith`/`EndsWith`), sorting and grouping also work, and
Radzen.Blazor's JS-interop callbacks (Upload, GoogleMap, HtmlEditor, Spreadsheet, ...) are preserved.

## What needs action when you trim your model assembly

The trimmer can only preserve members it is told about through a type it can see. Two patterns fall
outside that and are **your** responsibility to keep:

### 1. Nested property paths

A path like `Property="Customer.City"` reads `City` off the **nested** `Customer` type. DAM is not
transitive: Radzen.Blazor preserves your top-level model's members, but not the members of types it
points to. If you bind nested paths and trim your model assembly, root the nested type (see below).

### 2. Components that bind the item type through `Data` (not a generic parameter)

For these, the bound item type is the element type of `Data` (an `IEnumerable`), which is **not** the
component's generic parameter, so Radzen.Blazor cannot annotate it:

- `RadzenDropDown`, `RadzenDropDownDataGrid`, `RadzenListBox`, `RadzenCheckBoxList`,
  `RadzenRadioButtonList`, `RadzenSelectBar`, `RadzenAutoComplete`, `RadzenPickList`
  (`TextProperty` / `ValueProperty` / `DisabledProperty`)
- `RadzenTree` (`TextProperty`, `ChildrenProperty`, ...), `RadzenTreemap`, `RadzenHeatmap`

If you bind these by property name and trim the assembly that holds the item type, root that item
type (see below).

**Symptom if you miss it:** blank cells/text, or a `System.ArgumentException: Expression must be
readable` (or a `NullReferenceException`) thrown the first time the component sorts/filters/renders -
because the trimmer removed the property getter.

## How to root your model types

Pick whichever fits:

**Option A - keep models out of a trimmed assembly (simplest).** Leave your models in the app project
or a plain `Shared` library without `IsTrimmable`. Nothing else to do.

**Option B - preserve specific types with an ILLink descriptor.** Add an XML file to your WASM app:

```xml
<!-- TrimmerRoots.xml -->
<linker>
  <assembly fullname="MyApp.Shared">
    <type fullname="MyApp.Shared.Customer" preserve="all" />
    <type fullname="MyApp.Shared.Person" preserve="all" />
  </assembly>
</linker>
```

```xml
<!-- in the WASM app .csproj -->
<ItemGroup>
  <TrimmerRootDescriptor Include="TrimmerRoots.xml" />
</ItemGroup>
```

**Option C - annotate in code.** Put
`[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]` on the type, or add a
`[DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(Customer))]` at a point
your app already references.

## Native AOT

Not supported. Radzen.Blazor relies on reflection and dynamic LINQ for its data binding, which Native
AOT (`PublishAot`) cannot precompile. This is the same position as other Blazor component libraries;
Blazor apps do not run under Native AOT in any case. (Blazor WASM AOT via `RunAOTCompilation` is a
different feature and keeps an interpreter fallback, so it is unaffected.)

## Verifying your app

Publish trimmed and exercise the data-bound screens (sort, filter, open dropdowns, etc.):

```sh
dotnet publish -c Release
```

If a component shows blank values or throws `Expression must be readable` on interaction, a bound
type was trimmed - root it per Option B/C above.

---

<sub>For contributors: Radzen.Blazor's own trim-safety is enforced by `Radzen.Blazor.Tests/TrimmingContractTests.cs`
(a discovery-based gate over every `*Property` component), a console runtime gate
(`Radzen.Blazor.TrimRuntimeTest`), and the WASM/demos crawls in `e2e/`.</sub>
