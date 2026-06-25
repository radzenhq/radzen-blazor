I have all the verified findings. Writing the report directly as my output.

# Radzen.Blazor Trimming Audit

## 1. Executive Summary

This audit examined the shippable `Radzen.Blazor` library for trimming and AOT hazards that survive the clean trim-analyzer build because they live in the analyzer's blind spots: JS-interop serialization, reflection over the consumer's model type `T`, dynamic LINQ, and `System.Text.Json` of user types.

**Confirmed real hazards: 53.** By severity:

- **High: 14** - mostly the data-component reflection-over-`T` family (DataGrid/DropDown/Chart/Scheduler bind by string `Property`/`ValueProperty` and crash when the consumer's model lives in a trimmable assembly), plus three concrete JS-interop DTO gaps (`RadzenUpload`, `RadzenGoogleMap`) and two `System.Text.Json`-of-consumer-`T` deserialization gaps (`ReadAsync<T>`, `ODataServiceResult<T>`).
- **Medium: 32** - the bulk of the dynamic-LINQ / `RequiresUnreferencedCode`-laundered-through-suppression surface, the public `DynamicExtensions` API that suppresses instead of propagating, the `[RequiresDynamicCode]` annotation gap across the whole library (AOT-only), and several public generics missing `[DynamicallyAccessedMembers]`.
- **Low: 7** - edge cases (user-defined struct OData properties, custom `IList`/`ICollection` collection assignment, nullable-enum filter anonymous object, opt-in `RadzenComponentActivator` override path).

**Overall risk posture.** The library is *trim-clean by suppression, not by construction*. Roughly 188 `[UnconditionalSuppressMessage]` entries silence `IL2xxx` warnings with the human promise "Data item types are preserved by the application." That promise holds for the common Blazor WASM default (the consumer's app assembly is not member-trimmed), which is exactly why the existing tiny `TrimTest` passes. It breaks the moment a consumer's model lives in a separately referenced `<IsTrimmable>true</IsTrimmable>` library, or the app opts into `TrimMode=full`, or full AOT. None of those configurations are exercised by any test, so every one of these hazards is latent.

**The single most important gap:** the data-component generic parameters (`TItem` on `RadzenDataGrid`/`RadzenDataList`/`RadzenPivotDataGrid`, `T` on `PagedDataBoundComponent`/`DataBoundFormComponent`/`DropDownBase`, `TItem` on `CartesianSeries`/`RadzenScheduler`/`RadzenDataFilter`) carry **no `[DynamicallyAccessedMembers]`**. The suppressions delegate model-member preservation to the consumer but provide no mechanism to enforce it. Annotating these generic parameters with `[DynamicallyAccessedMembers(PublicProperties | PublicFields)]` is the one change that closes the largest share of the High findings, and it must start at the base classes (`PagedDataBoundComponent<T>`, `DataBoundFormComponent<T>`) or the analyzer will block downstream annotations with `IL2091`.

A secondary systemic gap: **zero `[RequiresDynamicCode]` annotations exist anywhere in the library**, and the AOT analyzer is disabled. Native AOT consumers get no warning before hitting `PlatformNotSupportedException` from `Reflection.Emit` and value-type `MakeGenericType`/`MakeGenericMethod`. This is AOT-only (standard Blazor WASM `RunAOTCompilation` keeps an interpreter fallback), hence Medium, but it is a real undocumented cliff.

---

## 2. Findings by Severity

### High

#### H1. Data-component base generics lack `[DynamicallyAccessedMembers]`; all dynamic LINQ over `T` inherits the gap

- **File:** `Radzen.Blazor/PagedDataBoundComponent.cs:22`, `Radzen.Blazor/DataBoundFormComponent.cs:27`
- **What breaks:** A consumer publishes WASM with `PublishTrimmed=true` and binds a `RadzenDataGrid`/`RadzenDataList`/`RadzenDropDown`/`RadzenDropDownDataGrid`/`RadzenPivotDataGrid` to a model in a **separate** `IsTrimmable` assembly (EF entities, a Shared/Models project). The trimmer strips the model's property getters (only setters survive, rooted by object initializers). The first sort/filter/group/`TextProperty` search throws `ArgumentException "Expression must be readable"` / `"'X' is not a member of type 'Y'"` from the dynamic-LINQ / `Expression.PropertyOrField` path. Empirically reproduced end-to-end.
- **Root cause:** `PagedDataBoundComponent<T>` and `DataBoundFormComponent<T>` declare `T` with no DAM and suppress `IL2087`/`IL2091` with `Justification = TrimMessages.DataTypePreserved`. The "preserved by the application" promise is false when the model assembly is itself trimmed; nothing flows a keep-properties requirement to the trimmer.
- **Remediation (start at the base, propagate down):**
  ```csharp
  // PagedDataBoundComponent.cs:22
  public class PagedDataBoundComponent<
      [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicFields)] T>
      : RadzenComponent

  // DataBoundFormComponent.cs:27
  public class DataBoundFormComponent<
      [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicFields)] T>
      : FormComponent<T>
  ```
  Then add the same annotation to every derived public generic so the requirement is not lost: `RadzenDataGrid<TItem>` (`RadzenDataGrid.razor.cs:99`), `RadzenDataGridColumn<TItem>` (`RadzenDataGridColumn.razor.cs:51`), `RadzenDataList<TItem>` (`RadzenDataList.razor.cs:39`), `RadzenPivotDataGrid<TItem>` (`RadzenPivotDataGrid.razor.cs:54`), `DropDownBase<T>` (`DropDownBase.cs:18`), `RadzenDropDown<TValue>` (`RadzenDropDown.razor.cs:35`), `RadzenDropDownDataGrid<TValue>` (`RadzenDropDownDataGrid.razor.cs:53`), `RadzenDataFilter<TItem>`/`RadzenDataFilterProperty<TItem>`. Use `PublicProperties | PublicFields` (matches what `PropertyAccess`/dynamic LINQ read). After this lands, delete the now-redundant `IL2087`/`IL2090`/`IL2091` suppressions (keep the `IL2026` suppressions - the `PropertyAccess`/`QueryableExtension` helpers remain `[RequiresUnreferencedCode]`). **LinkerConfig cannot fix this** - the model is a consumer type unknown to the library; DAM flow is the only mechanism.

#### H2. `DropDownBase`: `ValueProperty`/`TextProperty` reflection over the consumer item type, guarded only by suppressions

- **File:** `Radzen.Blazor/DropDownBase.cs:353` (and getters at `:498`/`:504`, single-select at `:1382`/`:1400`)
- **What breaks:** `<RadzenDropDown TValue="int" Data=@people TextProperty="FullName" ValueProperty="Id">`. Under trimming `Person.Id`/`Person.FullName` getters are stripped (`TValue=int` says nothing about `Person`; `Data` is `IEnumerable` so the element type is invisible). `OnParametersSet` -> `GetGetter` -> `PropertyAccess.Getter<object,object?>("Id", typeof(Person))` throws `ArgumentException` on render; or `GetProperty(elementType,"Id")` returns null and single-select returns the whole `Person` then fails to cast to `int` -> `InvalidCastException`. Shared by `RadzenDropDownDataGrid`, `RadzenListBox`, `RadzenCheckBoxList`, `RadzenRadioButtonList`, `RadzenSelectBar`, `RadzenAutoComplete`, `RadzenPickList`.
- **Root cause:** `elementType = PropertyAccess.GetElementType(Data.GetType())` is computed at runtime; the DAM chain is severed at the library/consumer boundary and papered over by `IL2070`/`IL2075` suppressions (`DropDownBase.cs:306-307, 475-476, 521-524, 1361-1364`).
- **Remediation:** Apply the H1 DAM annotation to `DropDownBase<T>` and forward it from the concrete dropdowns (covers the `TValue` path). Additionally make `GetElementType` flow the requirement on its return so the IL2070 suppression at `DropDownBase.cs:353/1400` becomes unnecessary:
  ```csharp
  // PropertyAccess.cs:181
  [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
  internal static Type GetElementType(
      [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces | DynamicallyAccessedMemberTypes.PublicProperties)] Type type)
  ```
  Document that consumers using `ValueProperty`/`TextProperty` under trimming must root their model (or rely on the DAM flow above).

#### H3. `QueryableExtension` filter/sort/group engine reflects over consumer `T`; RUC absorbed by class-level suppression

- **File:** `Radzen.Blazor/QueryableExtension.cs:525` (`GetNestedPropertyExpression`), reached from `RadzenDataGrid.razor.cs:258, 2116-2117, 2154`
- **What breaks:** Cell render (`PropertyAccess.Getter<TItem,object>(Property)` at `RadzenDataGridColumn.razor.cs:244/250`) and filter/sort/group (`Where<TItem>(columns)`/`OrderBy(orderBy)`/`GroupByMany`) all route to `Expression.PropertyOrField` / `Type.GetProperty(name)`. With a member-trimmed model, `GetProperty("Name")` returns null and `Expression.PropertyOrField` throws, crashing render/sort/filter/group. Also: the `In`/`NotIn`/`Contains` filter branches resolve `Enumerable.Contains/Any/Intersect/Except` by reflection, and **`System.Linq.Enumerable` is not preserved in LinkerConfig**.
- **Root cause:** Every `QueryableExtension` method is `[RequiresUnreferencedCode]`, but `RadzenDataGrid<TItem>`'s class-level `[UnconditionalSuppressMessage(IL2026)]` (`RadzenDataGrid.razor.cs:88`) swallows the obligation; no DAM on `TItem`.
- **Remediation:**
  1. Apply the H1 DAM annotation (preserves the model's accessors).
  2. Add to `LinkerConfig.xml` (the `In`/collection filter path resolves `Enumerable` by name; it is not preserved like `Queryable` at line 4):
     ```xml
     <assembly fullname="System.Linq">
       <type fullname="System.Linq.Enumerable" preserve="all" />
     </assembly>
     ```
  3. Add `[DynamicallyAccessedMembers(PublicProperties | PublicFields)]` to the `T` of the public `QueryableExtension.Where<T>`/`OrderBy<T>`/`GroupByMany<T>` overloads so direct callers also get the flow.

#### H4. `PropertyAccess` helpers require DAM on `Type`, but all callers pass unannotated `typeof(TItem)`

- **File:** `Radzen.Blazor/PropertyAccess.cs:431` (`GetPropertyType`), `:474` (`GetProperty`)
- **What breaks:** These two methods are the only `PublicProperties`-annotated sinks. Callers (`RadzenDataGridColumn.razor.cs:213/230`, `DropDownBase.cs:353/1400`, `RadzenDataFilterProperty.cs:63/355`, `CartesianSeries.cs:106/132/159`) pass `typeof(TItem)`/`Data.GetType()` with no matching source annotation -> `IL2087`, suppressed at class level. At runtime `GetProperty` returns null and the column type/filter degrades or `MakeGenericType(null)` throws.
- **Root cause:** The DAM contract is satisfied at the sink but never flowed from the source generic.
- **Remediation:** Same as H1 - annotating the component generic parameters makes these calls satisfiable and lets the `IL2087`/`IL2090` suppressions be removed. Keep the `IL2026` suppressions (`GetPropertyType`/`GetProperty`/`Getter` stay `[RequiresUnreferencedCode]`). Add `PublicFields` where dictionary/anonymous/field binding occurs (`PropertyAccess.cs:76` `GetField`).

#### H5. `CartesianSeries<TItem>` / chart series: `CategoryProperty`/`ValueProperty` reflection - safe for consumer `T`, but **crashes for the library `ChartDataPoint`** (Spreadsheet charts)

- **File:** `Radzen.Blazor/CartesianSeries.cs:50, 68`; reached via `Documents/Spreadsheet/ChartOverlay.cs:285-288`
- **What breaks:** The general consumer-chart case does **not** crash (the consumer model lives in the un-trimmed app assembly). But `RadzenSpreadsheet`'s chart feature renders `RadzenColumnSeries<ChartDataPoint>` with `CategoryProperty="Category"`/`ValueProperty="Value"` over `Radzen.Documents.Spreadsheet.ChartDataPoint` - a library type in the `IsTrimmable` `Radzen.Blazor` assembly. Its setters survive (rooted by an object initializer in `ChartDataResolver.cs:68`), but its getters (`get_Category`/`get_Value`), read only via expression-tree reflection, are stripped. Inserting/rendering a spreadsheet chart on a trimmed build throws `ArgumentException "The property 'Value' has no 'get' accessor"`.
- **Root cause:** Library type whose getters are reachable only through `PropertyAccess.Getter<ChartDataPoint,double>("Value")`; member-trimmed because the assembly is `IsTrimmable`.
- **Remediation:** Add to `LinkerConfig.xml` (note the `Radzen.Documents.Spreadsheet` namespace):
  ```xml
  <type fullname="Radzen.Documents.Spreadsheet.ChartDataPoint" preserve="all" />
  ```
  Do **not** blanket-annotate `CartesianSeries<TItem>` - the consumer path is already safe and over-annotation is noise.

#### H6. `RadzenUpload.OnChange` / `RadzenFileInput.OnChange`: `PreviewFileInfo`/`FileInfo` JSInvokable params not preserved

- **File:** `Radzen.Blazor/RadzenUpload.razor.cs:440` (and `RadzenFileInput.razor.cs:188`)
- **What breaks:** Selecting a file on a trimmed page: JS calls `invokeMethodAsync('RadzenUpload.OnChange', [{Name,Size,Url}])`; Blazor's `DotNetDispatcher` deserializes into `IEnumerable<PreviewFileInfo>` via reflection STJ. Decompilation of the trimmed assembly confirms the trimmer stripped the public parameterless ctors of `PreviewFileInfo` **and** `FileInfo`, the `PreviewFileInfo.Url` setter, `FileInfo.ContentType` setter, and `FileInfo.LastModified`. STJ throws inside `OnChange`; the JS `try/catch{}` swallows it, so selecting a file silently does nothing - no row, no preview, `Change` never fires.
- **Root cause:** `preserve="methods"` on `RadzenUpload` keeps the JSInvokable dispatch target but not the parameter DTO members. Neither DTO is in LinkerConfig.
- **Remediation:** Add to `LinkerConfig.xml` (note: namespace `Radzen`, not `Radzen.Blazor`):
  ```xml
  <type fullname="Radzen.FileInfo" preserve="all" />
  <type fullname="Radzen.PreviewFileInfo" preserve="all" />
  ```

#### H7. `RadzenUpload.OnProgress`: `FileInfo` JSInvokable param not preserved

- **File:** `Radzen.Blazor/RadzenUpload.razor.cs:465`
- **What breaks:** During upload, `invokeMethodAsync('RadzenUpload.OnProgress', ..., [{Name,Size}], ...)` deserializes into `IEnumerable<FileInfo>`. The trimmed-away parameterless `FileInfo()` ctor (never statically `new`'d - only the `IBrowserFile` ctor is used) makes STJ throw on the first progress event; the JS `catch` swallows it, so `Progress` never fires.
- **Root cause / Remediation:** Same DTO gap as H6; the `Radzen.FileInfo` LinkerConfig entry above fixes both.

#### H8. `RadzenGoogleMap.OnMapClick`: `GoogleMapClickEventArgs` JSInvokable param not preserved

- **File:** `Radzen.Blazor/RadzenGoogleMap.razor.cs:183`
- **What breaks:** Clicking the map sends `{Position:{Lat,Lng}}` to `OnMapClick(GoogleMapClickEventArgs args)`. `GoogleMapClickEventArgs.Position` is never assigned in C# (only via JSON), so its setter and parameterless ctor are trimmed. Either deserialization throws or `args.Position` is null on every `MapClick`.
- **Root cause:** LinkerConfig preserves `GoogleMapPosition` and `GoogleMapMarkerData` (lines 17-18) but missed the wrapper `GoogleMapClickEventArgs`.
- **Remediation:** Add to `LinkerConfig.xml` (namespace `Radzen`):
  ```xml
  <type fullname="Radzen.GoogleMapClickEventArgs" preserve="all" />
  ```

#### H9. `HttpResponseMessageExtensions.ReadAsync<T>`: reflection-STJ of the consumer model with no trim seam

- **File:** `Radzen.Blazor/HttpResponseMessageExtensions.cs:27` (deserialize at `:36`)
- **What breaks:** Scaffolded OData services call `await response.ReadAsync<ODataServiceResult<Category>>()`. Under trimming `Category`'s setters/ctor are stripped; reflection-based `DeserializeAsync<T>` returns rows with all-default properties (silent blank grids) or throws `NotSupportedException`/`ConstructorContainsNullParameterNames` for records/ctor-bound DTOs. The canonical Radzen-generated data-access path.
- **Root cause:** `T` has no DAM, the method is not `[RequiresUnreferencedCode]`, and the class-level `[UnconditionalSuppressMessage(IL2026)]` (`:14`) swallows the only warning. The sibling write path `ODataJsonSerializer.Serialize<TValue>` already does the right thing (DAM on `TValue`).
- **Remediation:** Annotate `T` and remove the class-level suppression:
  ```csharp
  public static async Task<T?> ReadAsync<
      [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(
      this HttpResponseMessage response)
  ```
  DAM flows through `ODataServiceResult<T>` -> `IEnumerable<T>`. `PublicConstructors` (not just parameterless) guards records/positional types.

#### H10. `ODataServiceResult<T>` preserved as wrapper only; the element type `T` is not (false comfort)

- **File:** `Radzen.Blazor/OData.cs:57`, `LinkerConfig.xml:15`
- **What breaks:** `<type fullname="Radzen.ODataServiceResult`1" preserve="all" />` keeps `Count`/`Value` on the open generic but **not** the members of the closed element type. `ReadAsync<ODataServiceResult<Category>>()` still needs `Category`'s setters/ctor, which are unprotected.
- **Root cause:** `preserve="all"` on an open generic does not recurse into the type argument; the element type lives in the consumer assembly.
- **Remediation:** Fixed by H9's DAM annotation on `ReadAsync<T>` (the requirement flows into the element type). Add a comment in `LinkerConfig.xml` noting that line 15 preserves only the wrapper and that element types are preserved via the `ReadAsync<T>` DAM flow.

#### H11. `DynamicExtensions` public dynamic-LINQ API suppresses instead of propagating `[RequiresUnreferencedCode]`

- **File:** `Radzen.Blazor/DynamicExtensions.cs:11-13`
- **What breaks:** A consumer with `using System.Linq.Dynamic.Core;` calling `query.Where("Foo == @0", val)` / `.OrderBy("Bar")` / `.Select("new { X, Y }")` gets **zero** analyzer warning, then crashes at runtime: (a) the `Select("new {...}")` projection reaches `DynamicTypeFactory.CreateType` -> `AssemblyBuilder.DefineDynamicAssembly` (AOT crash), and (b) any cast/qualified type name (`"(MyApp.Foo)x"`, enum members) reaches `ResolveTypeFromAssemblies` -> `AppDomain.GetAssemblies().GetTypes()`, which under trimming returns an incomplete set -> `"Could not resolve type"`.
- **Root cause:** The class-level `[UnconditionalSuppressMessage(IL2026)]`+`[IL2072]` silences the obligation that downstream `[RequiresUnreferencedCode]` `ExpressionParser`/`QueryableExtension` would raise, and the public `Where`/`OrderBy`/`Select` methods are not themselves `[RequiresUnreferencedCode]`, so nothing reaches the consumer.
- **Remediation:** Remove the class-level suppressions and mark each public method `[RequiresUnreferencedCode(TrimMessages.DynamicLinqReflection)]` (`Where` `:26`, `OrderBy` `:68`, `Select<T>` `:88`, `Select` `:114`). For the projection path also add `[RequiresDynamicCode(...)]` (see M-AOT findings). Add localized `[UnconditionalSuppressMessage]` only at the internal Radzen call sites where `T` is a preserved model.

#### H12. DataGrid runtime sorting never exercised by TrimTest - latent crash in separate trimmable model assembly

- **File:** `Radzen.Blazor/RadzenDataGrid.razor.cs:2079, 2173` (`view.OrderBy<TItem>(orderBy)`)
- **What breaks:** Empirically verified: with the model in an `IsTrimmable` library, `get_Name` etc. are removed; cell display via `PropertyAccess.GetValue/Getter` throws `ArgumentException "Property Get method was not found."` on first render, and clicking a sort header throws `NullReferenceException` from the compiled delegate. With the model in the rooted app assembly (as the current TrimTest has it) it does not reproduce - masking the bug.
- **Root cause / Remediation:** Same DAM gap as H1. Plus a test gap: add a `<IsTrimmable>true</IsTrimmable>` model class library to the TrimTest solution and a runtime sort/click step (see section 3).

#### H13. DataGrid runtime filtering with operators never exercised - `Where(filters)` over consumer `T` plus unpreserved `Enumerable`

- **File:** `Radzen.Blazor/RadzenDataGrid.razor.cs:2154`
- **What breaks:** Typing in a filter box (`Contains`, `GreaterThan`, `In` for enums/collections) invokes `Where<TItem>(columns)` -> `GetExpression<T>` reflecting on `T` by name and resolving `Enumerable.Contains/Any/Intersect/Except` by reflection. Stripped model member -> `ArgumentException`; stripped `Enumerable` overload (not in LinkerConfig) -> `InvalidOperationException`. The grid renders until the first filter, then crashes.
- **Remediation:** H1 DAM annotation + the `System.Linq.Enumerable` LinkerConfig entry from H3. Add a TrimTest step that applies one filter per operator family (string `Contains`, numeric `GreaterThan`, bool equals, `DateTime` range, enum `In`) at runtime.

#### H14. Nested property paths (`Customer.Name`) never exercised - per-segment reflection over nested consumer types

- **File:** `Radzen.Blazor/PropertyAccess.cs:109` (per-segment `Expression.PropertyOrField` / `GetProperty`)
- **What breaks:** Empirically reproduced under `TrimMode=full`/`link` with the model in a separate assembly: a column `Property="Customer.Name"` (or `Scheduler StartProperty`/`Chart CategoryProperty` on a nested type) resolves `Customer` on `TItem`, then `Name` on the `Customer` type. `[DAM(PublicProperties)]` on `TItem` is **not** transitive into the nested property type, so `get_Name` on `Customer` is stripped. The reflection path throws `ArgumentException "Property Get method was not found"`; the expression path throws `"Expression must be readable"`.
- **Root cause:** DAM does not recurse into nested property types; `LinkerConfig` cannot name the consumer's `Customer`.
- **Remediation (layered, no single fix):**
  1. Flow DAM through `PropertyInfo.PropertyType` so the first nested level is rooted:
     ```csharp
     // PropertyAccess.cs GetPropertyTypeIncludeInterface
     [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
     private static Type? GetPropertyTypeIncludeInterface(
         [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type? type, string? property)
     ```
  2. In `PropertyAccess.GetValue` (`:233`), when `GetProperty` returns a `PropertyInfo` with a null `GetMethod`, throw a clear diagnostic ("Property 'X.Y' accessor was trimmed; preserve nested model type via DynamicDependency or LinkerConfig") instead of the opaque BCL exception.
  3. Document that deeper nested chains require the consumer to root the intermediate type (`[DynamicDependency]` / a `TrimmerRootDescriptor` in their app).
  4. Add a TrimTest nested-path scenario (section 3).

---

### Medium

The Medium findings divide into four families. Each entry lists file:line, the runtime symptom, and the exact fix.

#### Reflection-over-`T` family (same root cause as H1-H4, narrower trigger or graceful degrade)

- **M1. `RadzenDataGridColumn.SetColumnDefaults`** - `Radzen.Blazor/RadzenDataGridColumn.razor.cs:213/230/269`. `PropertyAccess.GetPropertyType(typeof(TItem), ...)` and `typeof(TItem).GetProperty(Property)` with unannotated `TItem`; `MakeGenericType(pt!)` NREs if `pt` is null. **Fix:** H1 DAM on `RadzenDataGridColumn<TItem>`; also guard `RadzenDataGridColumn.razor.cs:215` with `typeof(IEnumerable<>).MakeGenericType(pt ?? typeof(object))`.
- **M2. `RadzenDataGridColumn.GetValue` nested paths** - `RadzenDataGridColumn.razor.cs:781`. Dotted `Property` routes to `PropertyAccess.GetValue` -> silent wrong/blank cells (not a crash). **Fix:** H1 + H14.
- **M3. `RadzenScheduler<TItem>`** - `RadzenScheduler.razor.cs:647`. `PropertyAccess.Getter<TItem,DateTime>(StartProperty)` on a model hydrated only via JSON/interop. **Fix:** add `[DynamicallyAccessedMembers(PublicProperties | PublicFields)]` to `TItem` on `RadzenScheduler.razor.cs:55`.
- **M4. `RadzenTree` hierarchical binding** - `RadzenTree.razor.cs:318`. `PropertyAccess.GetValue/Getter` on `TextProperty`/`ChildrenProperty`/`CheckableProperty`; `Data` is `IEnumerable` (no generic anchor) - structurally weaker than the grid. Crashes only under `TrimMode=full` / trimmable node-type library. **Fix:** consider a generic `RadzenTree<TItem>` with DAM; minimum, document the contract and add a TrimTest tree scenario.
- **M5. `RadzenDropDownDataGrid`** - `RadzenDropDownDataGrid.razor.cs:696`. Search-as-you-type builds multi-column `Where` over the item type via reflection. **Fix:** H1 DAM forwarding + TrimTest coverage with a filtered typed list.
- **M6. `RadzenPivotDataGrid` dynamic aggregation** - `RadzenPivotDataGrid.razor.cs:1310-1384`. `GetPropertyType(typeof(TItem), property)` then `items.Select(property).Sum(propertyType)`; on stripped property, Sum falls to `items.Count()` (wrong total) or `catch{return null}` (blank). **Fix:** H1; and prefer returning null over `items.Count()` for a misconfigured numeric property (`RadzenPivotDataGrid.razor.cs:1363-1365`).
- **M7. `RadzenDataGrid` class-level blanket suppression of 11 IL codes** - `RadzenDataGrid.razor.cs:88`. Hides grouping/sorting reflection over `TItem` and any future reflection in the 4000-line partial class. **Fix:** H1 DAM; move the 11 suppressions off the class onto the specific dynamic-LINQ members (`LoadGroups`, `GroupedPagedView`, sort helpers) so future reflection is not auto-silenced.
- **M8. `RadzenPivotDataGrid` class-level blanket suppression** - `RadzenPivotDataGrid.razor.cs:43`. Same pattern as M7; `catch{return null}` turns crashes into silent wrong totals. **Fix:** H1 + narrow suppression scope + the null-vs-Count fix in M6.
- **M9. `RadzenDataGridColumn.GetValue` reflection (engine)** - covered by H3/H14.
- **M10. `RadzenDataGrid.View` override launders RUC through non-RUC base (IL2046)** - `RadzenDataGrid.razor.cs:2126`. `OrderBy(string)`/`Cast(firstItem.GetType())` are RUC; the non-RUC base `View` forces an `IL2046` suppression. **Fix:** H1 DAM makes the suppressions guard safe code; keep the `IL2046` suppression (the base `View` cannot be RUC).
- **M11. `PagedDataBoundComponent<T>` base virtuals not RUC though every override is unsafe** - `PagedDataBoundComponent.cs:298/314/332`. Structural; do **not** add RUC to the base (it would taint trim-safe `RadzenDataList` and not propagate past the re-suppressing override). **Fix:** H1 DAM + the runtime TrimTest sort/filter step.
- **M12. `RadzenDataAnnotationValidator.Validate`** - `RadzenDataAnnotationValidator.cs:76`. `PropertyAccess.Getter<object>` + `Validator.TryValidateProperty` over the model; common attributes are safe (bound property is rooted; attribute instances are kept by default). Real residual: `[Compare("Other")]`, custom `ValidationAttribute` reflecting siblings, and resource-based messages (`ErrorMessageResourceType`). **Fix:** add `[DynamicallyAccessedMembers(All)]` to `TItem` on `RadzenTemplateForm<TItem>` (`RadzenTemplateForm.cs:33`); preserve standard `ValidationAttribute` subtypes in LinkerConfig if resource messages are used; add a TrimTest validator scenario covering `[Compare]` + a resource-message attribute.
- **M13. `RadzenComponentActivator.CreateInstance` / `Override`** - `RadzenComponentActivator.cs:42/56`. DAM on the incoming `componentType` does not flow to the registered override type; `Activator.CreateInstance`/`MakeGenericType` on the override throws `MissingMethodException` under trimming. **Fix:**
  ```csharp
  public void Override<TOriginal,
      [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TOverride>()
      => Override(typeof(TOriginal), typeof(TOverride));

  public void Override(Type original,
      [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type @override)
  ```

#### Dynamic-LINQ / suppression family

- **M14. `DynamicExtensions` class-level suppression masks RUC + assembly-scan type resolution** - `DynamicExtensions.cs:11`. Custom filter expressions / enum casts named in filter strings resolve via `ResolveTypeFromAssemblies` (`ExpressionParser.cs:936`); trimmed type -> `"Could not resolve type"`. **Fix:** same as H11 (propagate RUC). Document that types named in `CustomFilterExpression` strings must be rooted by the consumer; harden the `ResolveTypeFromAssemblies` null path with a trimming-aware message.
- **M15. `ExpressionParser.ResolveTypeFromAssemblies`** - `ExpressionParser.cs:936`. `AppDomain.GetAssemblies().GetTypes()` to resolve a type by `FullName` from a filter string; the library itself emits enum `FullName` casts (`ExpressionSerializer.cs:221`). **Fix:** change `ExpressionSerializer.FormatValue` to emit the underlying numeric value with a primitive cast (e.g. `(int)2`) instead of the enum `FullName`, removing the assembly-scan dependency for enums; add a TrimTest enum-filter round-trip.
- **M16. `ExpressionParser` `MakeGenericMethod`/`MakeGenericType` + reflection over `T`** - `ExpressionParser.cs:360, 668, 785, 842, 913`. Nested `"Customer.Name"` resolves members on the un-DAM'd `Customer` type; `MakeGeneric*` is AOT-unsafe. **Fix:** H1 DAM (roots root-model members; nested types remain a documented consumer responsibility) + `[RequiresDynamicCode]` (M-AOT).
- **M17. `QueryableExtension` `MakeGenericMethod`/`MakeGenericType`/`AsQueryable`** - `QueryableExtension.cs:296, 314, 335, 400, 421, 443, 941, ...`. Trimming is mitigated (`System.Linq.Queryable` is `preserve="all"` at LinkerConfig line 4); the gap is AOT-only (value-type closed generics). **Fix:** `[RequiresDynamicCode]` on each (M-AOT). Do **not** add an `Enumerable` entry for this finding (`ToList<T>` survives via static use) - but note H3 needs `Enumerable` for the `In`-filter reflection.
- **M18. `GroupByMany` builds `Expression<Func<T,object>>` per property** - `QueryableExtension.cs:143`. `GroupResult` is preserved (LinkerConfig line 80); the consumer's grouped property is not. **Fix:** H1 DAM; add `[DynamicallyAccessedMembers(PublicProperties | PublicFields)]` to the `T` of public `GroupByMany<T>`; add a TrimTest grouping + pivot scenario.
- **M19. `QueryableExtension.Where(columns)` / `Where(RadzenDataFilter)` invoked from suppressed DataGrid paths; nested-type preservation gap** - `QueryableExtension.cs:1429`. Nested filter paths (`Customer.Address.City`) resolve members on intermediate types not covered by root-model DAM. **Fix:** H1 + H14; add a nested-path filter scenario and a `FilterOperator.Custom` scenario (the latter exercises the `ExpressionParser` string path at `:1471`).
- **M20. `ODataJsonSerializer.Serialize<TValue>` nested complex/collection types not preserved** - `OData.cs:126`. `[DAM(PublicProperties)]` on `TValue` is shallow; nested navigation/element types are not preserved. In practice the `ComplexPropertiesConverter` *drops* class-typed complex properties from the OData body, so no observable loss for the common shape. Real residual: **user-defined struct** properties (Low, see L1). **Fix:** keep DAM; document the shallow-preservation contract; the struct case is the actionable part (L1).
- **M21. `GetExpression`/`GetNestedPropertyExpression` filter expression trees over `T`** - `QueryableExtension.cs:525`. Covered by H3/H13; string methods (`Contains`/`StartsWith`/`ToLower`) are on `System.String` and safe; the consumer-model member access is the gap. **Fix:** H1.

#### AOT (`IL3050` / `[RequiresDynamicCode]`) family - AOT-only, library-wide

The library has **zero `[RequiresDynamicCode]`** annotations and the AOT analyzer is off; enabling it surfaces ~214 `IL3050` warnings. Standard Blazor WASM `RunAOTCompilation` keeps an interpreter fallback (no crash), so these are Native-AOT-only - hence Medium. None is fixable via LinkerConfig (`IL3050` is a codegen, not a metadata, concern).

- **M22. `DynamicTypeFactory.CreateType` uses `Reflection.Emit`** - `DynamicTypeFactory.cs:18` (`AssemblyBuilder.DefineDynamicAssembly`). Hard Native-AOT failure (`PlatformNotSupportedException`), reached from `ExpressionParser.cs:560` for `new {...}` projections (Pivot aggregates, dynamic `Select` with aliases). **Fix:**
  ```csharp
  // TrimMessages
  internal const string DynamicCodeGeneration =
      "Uses Reflection.Emit to generate types at runtime, which is not supported under AOT.";
  // DynamicTypeFactory.cs:9
  [RequiresUnreferencedCode(TrimMessages.DynamicTypeGeneration)]
  [RequiresDynamicCode(TrimMessages.DynamicCodeGeneration)]
  public static Type CreateType(...)
  ```
  Propagate to `ExpressionParser` (`:17`) and the public `DynamicExtensions.Select` overloads (`:88`/`:114`). Best durable fix: replace the anonymous-type emit with a `ValueTuple`/`Dictionary` projection so the path is AOT-survivable.
- **M23. `PropertyAccess.Getter` `Expression.Compile()`** - `PropertyAccess.cs:35, 115, 265`. `[RequiresDynamicCode]` is missing; under the AOT interpreter the simple member-access lambdas Radzen builds generally survive, so no mainstream crash. **Fix:** add `[RequiresDynamicCode]` alongside the existing `[RequiresUnreferencedCode]` so the requirement is honest; add parallel `IL3050` suppressions (with justification "interpreter handles simple property-access lambdas") at the component call sites that already suppress `IL2026`.
- **M24. `RadzenPivotDataGrid` / `RadzenDataGrid` build `RadzenNumeric<T>` via `MakeGenericType`/`MakeGenericMethod` at render time** - `RadzenPivotDataGrid.razor.cs:2175/2199/2200`, `RadzenDataGrid.razor.cs:981/1005/1006`. Value-type `RadzenNumeric<int?>`/`<decimal?>` instantiations have no AOT-generated code; render-path so `[RequiresDynamicCode]` cannot be applied. **Fix:** guard with `if (RuntimeFeature.IsDynamicCodeSupported)` + a non-generic fallback, or force static instantiation of the common nullable-numeric closed generics in a preserved never-called method.
- **M25. `QueryableExtension` / `ExpressionParser` `MakeGeneric*`/`AsQueryable` (20 + 6 IL3050)** - same family as M17/M16. **Fix:** `[RequiresDynamicCode]` on the public entry points; enable `<EnableAotAnalyzer>true</EnableAotAnalyzer>` to make these visible (M28).
- **M26. `DropDownBase.GetItemType` `MakeGenericType(Nullable<>)` at render time** - `DropDownBase.cs:315` (and `:534, 1655, 1663, 1713`). Value-type `Nullable<>` instantiation, AOT-unsafe; on the normal render path. **Fix:** guard with `RuntimeFeature.IsDynamicCodeSupported` or force-instantiate common nullable value types; note the csproj advertises `IsAotCompatible=true` for net8.0 but the analyzer never actually runs - either honor it (M28) or drop the claim.
- **M27. `AIChatService` / OData JSON of consumer types under AOT** - reflection-STJ is `[RequiresDynamicCode]`. **Fix:** `[RequiresDynamicCode]` on `ReadAsync<T>`/`Serialize<TValue>`/`AIChatService` send path; long term, a source-generated `JsonSerializerContext`.
- **M28. AOT analyzer disabled; `IsAotCompatible=true` is inert** - `Radzen.Blazor.csproj:7-9`. The csproj's `IsAotCompatible` assignment does not reach the SDK's `EnableAotAnalyzer` derivation, so 214 `IL3050` are invisible. **Fix:** set `<EnableAotAnalyzer Condition="'$(TargetFramework)'=='net8.0'">true</EnableAotAnalyzer>` explicitly, triage the warnings via the annotations above, and **decide the contract**: either back `IsAotCompatible=true` with the analyzer or remove it and document "trim-compatible, not Native-AOT-compatible (Blazor WASM `RunAOTCompilation` works via interpreter fallback)."

#### JS-interop / `RequiresUnreferencedCode` family

- **M29. `RadzenHtmlEditor.GetSelectionAttributes<T>`** - `RadzenHtmlEditor.razor.cs:511` (and the class-level `IL2091` suppression at `:56`). Public generic forwards consumer `T` to `JSRuntime.InvokeAsync<T>`; the class suppression hides the `IL2091` from the consumer. Internal callers (`ImageAttributes`/`LinkAttributes`) are safe (LinkerConfig lines 36-37), but an external `T` is unprotected. **Fix:**
  ```csharp
  public ValueTask<T> GetSelectionAttributes<
      [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(
      string selector, string[] attributes)
  ```
  This satisfies `InvokeAsync<T>` at `:518` (so the misapplied class-level `IL2091` is no longer needed for this call) and flows the requirement to the consumer's `T`.
- **M30. `ODataJsonSerializer` + `ComplexPropertiesConverter<T>` nested-type STJ** - `OData.cs:126/153`. Covered by M20/L1; the converter's `T` is unannotated (`IL2091` suppressed). **Fix:**
  ```csharp
  public class ComplexPropertiesConverter<
      [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T> : JsonConverter<T>
  ```
  then remove the now-unnecessary `IL2091` suppressions at `OData.cs:125/152`.
- **M31. `DialogService.OpenSide(Type)` / `OpenSideAsync(Type)` lack DAM + RUC** (asymmetry vs `Open(Type)`) - `DialogService.cs:298/245`. The generic `OpenSide<T>` overloads are correctly `[DAM(All)]`; the dynamic-`Type` side overloads have neither, yet the same `Type` reaches `OpenComponent`. **Fix:**
  ```csharp
  [RequiresUnreferencedCode(TrimMessages.GenericMethodReflection)]
  public void OpenSide(string title,
      [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type componentType,
      Dictionary<string, object?>? parameters = null, SideDialogOptions? options = null)
  // same for OpenSideAsync(Type) at :245
  ```
- **M32. `RadzenComponentActivator` `MakeGenericType` + `Activator.CreateInstance` on override type** - duplicate root cause of M13; the override-as-`Type` (`RadzenComponentActivator.cs:42`) is the same gap viewed from the activator side. **Fix:** the `Override` parameter annotations in M13.

---

### Low

- **L1. `ODataJsonSerializer.Serialize<TValue>` - user-defined struct properties** - `OData.cs:97-98` (`IsComplex` requires `type.IsClass`). A user struct property is not classified complex, not stripped by the converter, and serializes as `{}` under trimming (silent loss in the POST body). BCL structs (Guid, DateTime, decimal) are safe. **Fix:** broaden `IsComplex` so non-`System` user value types are also excluded from the OData body, e.g. treat `baseType.IsValueType && !baseType.IsPrimitive && namespace not starting with "System"` as complex. Add a TrimTest OData scenario with a user-struct property.
- **L2. `DropDownBase` multi-select `Activator.CreateInstance<T>()` on a custom `IList` `TValue`** - `DropDownBase.cs:1640`. `List<T>`/`ObservableCollection<T>` are safe (BCL ctors rooted); a custom `class SelectedProducts : List<Product>{}` whose ctor is reflection-only throws `MissingMethodException`. **Fix:**
  ```csharp
  public class DropDownBase<
      [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T>
      : DataBoundFormComponent<T>
  ```
  (compatible with H1's `PublicProperties` - OR the two member types together), then remove the `IL2091` suppression at `:1629`. Or wrap line 1640 in try/catch falling back to a `List<>`.
- **L3. `DropDownBase.ReferenceGenericCollectionAssignment` reflective `GetMethod(Add/Remove/Clear)`** - `DropDownBase.cs:1717-1719`. Dead on all shipped paths (`PreserveCollectionOnSelection` is never set true). If a consumer subclasses and opts in with a custom `ICollection<>`, null `GetMethod` -> `NullReferenceException` from the `!.Invoke`. **Fix:** set `canHandle = (clearMethod != null && addMethod != null && removeMethod != null)` so a trimmed method degrades to the default `List<>` rebuild instead of NRE.
- **L4. `RadzenComponentActivator` override (DAM not flowed)** - covered by M13/M32; Low when viewed as the opt-in advanced feature.
- **L5. Nullable-enum filter row builds an anonymous `new { Value, Text }` bound to a DropDown via `TextProperty`/`ValueProperty`** - `RadzenDataGrid.razor:491`. The anonymous type's `get_Value`/`get_Text` are stripped (only ctor + backing fields are statically referenced), so the filter DropDown throws `ArgumentException "'Value' is not a member of type '<>f__AnonymousType...'"` when filtering a nullable-enum column. Cannot be fixed by LinkerConfig (anonymous type has an unspeakable name). **Fix:** replace the anonymous object with the preserved named type `DropDownItem<object>` (already `preserve` at LinkerConfig line 13):
  ```razor
  new object[]{ new Radzen.DropDownItem<object> {
      Value = Convert.ChangeType(-1, Enum.GetUnderlyingType(Nullable.GetUnderlyingType(column.FilterPropertyType) ?? column.FilterPropertyType)),
      Text = EnumNullFilterText } }
  ```
  Add a TrimTest nullable-enum filter column.
- **L6. `ExpressionParser.ResolveTypeFromAssemblies` enum-cast resolution (PivotDataGrid drill-down)** - `ExpressionParser.cs:936`. PivotDataGrid drill-down feeds an enum `FullName` cast into `items.Where(string)`; trimmed enum -> `"Could not resolve type"`. Standard enum column filtering uses the typed `FilterDescriptor` path and is unaffected. **Fix:** the `ExpressionSerializer` numeric-cast change from M15 closes this; or document/root the enum.
- **L7. `ODataJsonSerializer.Serialize<TValue>` data-loss nuance** - `OData.cs:140`. No crash (named reference types under-serialize gracefully; the converter drops complex props by design). Already mitigated for the crash dimension. **Fix:** document that nested complex/collection types referenced by `TValue` must be independently rooted; the actionable struct sub-case is L1.

---

## 3. Hardening the Process

The trim analyzer is clean and the existing `TrimTest` publishes clean, yet 53 real hazards remain. The gate is meaningless because (a) the app's model lives in the rooted app assembly, (b) the test only checks publish-time IL warnings and never runs the rendered components, and (c) it exercises ~4 components out of the library.

### (a) Scenarios to add to `Radzen.Blazor.TrimTest`

**Critical structural change:** move the data-item model into a **separate referenced class library** compiled with `<IsTrimmable>true</IsTrimmable>` (`Radzen.Blazor.TrimTest.Models`). This is the *only* configuration that reproduces the reflection-over-`T` family (H1-H14). Keep the model's getters unreferenced by app code (set only via object initializers / JSON).

Add to `App.razor` (binding by string `Property`/`ValueProperty`/`CategoryProperty`, and driving runtime interaction):

```razor
@* DataGrid: sort + filter (every operator family) + group + nested path *@
<RadzenDataGrid @ref="grid" TItem="Order" Data="@orders" AllowSorting="true"
                AllowFiltering="true" AllowGrouping="true" FilterMode="FilterMode.CheckBoxList"
                QueryOnlyVisibleColumns="true">
  <Columns>
    <RadzenDataGridColumn TItem="Order" Property="CustomerName" Title="Customer" />
    <RadzenDataGridColumn TItem="Order" Property="Total" Title="Total" />
    <RadzenDataGridColumn TItem="Order" Property="Status" Title="Status" />   @* enum *@
    <RadzenDataGridColumn TItem="Order" Property="Customer.City" Title="City" />  @* nested *@
    <RadzenDataGridColumn TItem="Order" Property="UnusedSortOnly" Title="Sort" UseDisplayName="true" />
  </Columns>
</RadzenDataGrid>

@* DropDown / ListBox / DropDownDataGrid by ValueProperty/TextProperty *@
<RadzenDropDown TValue="int" Data="@people" TextProperty="FullName" ValueProperty="Id" @bind-Value="@selId" AllowFiltering="true" />
<RadzenAutoComplete Data="@people" TextProperty="FullName" @bind-Value="@search" />

@* Chart by property name *@
<RadzenChart><RadzenColumnSeries TItem="Sale" Data="@sales" CategoryProperty="Date" ValueProperty="Revenue" /></RadzenChart>

@* PivotDataGrid: dynamic aggregation (Sum/Average over a value type) *@
<RadzenPivotDataGrid TItem="Sale" Data="@sales">
  <Rows><RadzenPivotRow TItem="Sale" Property="Region" /></Rows>
  <Aggregates><RadzenPivotAggregate TItem="Sale" Property="Revenue" Aggregate="AggregateFunction.Sum" /></Aggregates>
</RadzenPivotDataGrid>

@* Scheduler, Tree, Upload, FileInput, GoogleMap (roots the JSInvokable DTO graph) *@
<RadzenScheduler TItem="Appt" Data="@appts" StartProperty="Start" EndProperty="End" TextProperty="Title"><RadzenMonthView/></RadzenScheduler>
<RadzenTree Data="@nodes"><RadzenTreeLevel TextProperty="Name" ChildrenProperty="Children" /></RadzenTree>
<RadzenUpload Url="api/upload" Auto="false" Multiple="true" />
<RadzenFileInput TValue="string" @bind-Value="@fileVal" />
<RadzenGoogleMap MapClick="@(a => mapLat = a.Position.Lat)" MarkerClick="@(_ => {})">
  <Markers><RadzenGoogleMapMarker Title="x" Position="@(new GoogleMapPosition{Lat=0,Lng=0})" /></Markers>
</RadzenGoogleMap>
```

And in `OnAfterRenderAsync(firstRender)`, **actually run** the reflective paths (publish-only IL checks cannot catch the runtime throw):
```csharp
grid.ColumnsCollection[0].SetFilterValue("A"); // string Contains
// set numeric GreaterThan, bool equals, DateTime range, enum In on other columns
grid.Groups.Add(new GroupDescriptor { Property = "Status" });
await grid.OrderBy("UnusedSortOnly");
await grid.Reload();
```

Add direct exercises of the JS-interop / JSON paths that markup cannot trigger at publish time:
```csharp
// OData / ReadAsync round-trip against a stubbed HttpResponseMessage (no network)
var result = await stubbedResponse.ReadAsync<ODataServiceResult<Order>>();
// record case to catch ctor stripping:
var rec = await stubbedResponse.ReadAsync<OrderRecord>(); // record OrderRecord(int Id, string Name);
// OData serialize with nested complex + collection + user struct + DateTime?
var json = Radzen.ODataJsonSerializer.Serialize(new Parent { Id = 1, Nav = new(){Name="x"}, Items = new(){ new(){Name="y"} }, Loc = new Geo{...} });
// dynamic-LINQ projection that emits an anonymous type (AOT path)
var proj = orders.AsQueryable().Select("CustomerName as Name, Total");
```

### (b) Automated tests

Extend `LinkerConfigTests` (or add `InteropPreservationTests`) so the suppression promises are machine-checked, not hoped:

1. **JSInvokable parameter/return preservation.** Reflect over the `Radzen.Blazor` assembly, find every `[JSInvokable]` method, and for each parameter type and `InvokeAsync<T>`/`InvokeVoidAsync` argument type that is a Radzen-owned DTO, assert it (or a `<type preserve="all">` for it) is present in `LinkerConfig.xml`. This would have caught H6/H7/H8 (`FileInfo`, `PreviewFileInfo`, `GoogleMapClickEventArgs`).
2. **`InvokeAsync<T>` return-type preservation.** Scan for `InvokeAsync<T>` call sites where `T` is a Radzen DTO and assert preservation (would catch the `Rect`/HTML-editor DTO family before it regresses).
3. **No anonymous type reaches an interop boundary.** A Roslyn-analyzer-style test (or a compiled IL scan) that fails if a `new { ... }` expression flows into `JSRuntime.InvokeAsync`/`InvokeVoidAsync` or `JsonSerializer.Serialize`. This guards the exact crash class that started this effort and would catch L5.
4. **DAM-consistency test.** Assert that the public data-component generic parameters (`RadzenDataGrid<TItem>`, `DropDownBase<T>`, `PagedDataBoundComponent<T>`, etc.) carry `[DynamicallyAccessedMembers]` - fails if a future refactor drops the H1 annotation.
5. **Runtime smoke tests (bUnit)** against the trimmable-model library: render the grid/dropdown/chart, click a sort header, apply a filter, group - assert no exception and non-empty cells.

### (c) CI gate

Add a CI leg that runs `dotnet publish -c Release` on `Radzen.Blazor.TrimTest` with `PublishTrimmed=true` and `TreatWarningsAsErrors=true`, then **launches the published app and drives the runtime scenarios above** (a Playwright/Selenium pass clicking sort headers, applying filters, opening dialogs, selecting an upload file). The current `nuget.yml` only builds/tests/packs - it never publishes trimmed. Add a second leg with `<RunAOTCompilation>true</RunAOTCompilation>` (requires the `wasm-tools` workload) and a third with `<EnableAotAnalyzer>true</EnableAotAnalyzer>` on the library to gate the `IL3050` family (M22-M28).

### (d) Trimming-safety checklist for future PRs

- Does this code pass an object to `JSRuntime.InvokeAsync`/`InvokeVoidAsync`, or call `JsonSerializer.Serialize`/`Deserialize`? If yes: it must be a **named** type (never `new { }`), and that type must be in `LinkerConfig.xml` with `preserve="all"` (or the generic carries `[DynamicallyAccessedMembers]`).
- Does this code add a `[JSInvokable]` method? Its declaring type needs `preserve="methods"` and **every parameter/return DTO** needs `preserve="all"`.
- Does this code reflect over a consumer generic `T` (`typeof(T).GetProperty`, `PropertyAccess.*`, dynamic LINQ, `Expression.PropertyOrField`)? The generic parameter must carry `[DynamicallyAccessedMembers(PublicProperties | PublicFields)]` - do **not** add a new `[UnconditionalSuppressMessage]` to silence it.
- Does this code use `Reflection.Emit`, `MakeGenericType`/`MakeGenericMethod` over a runtime type, or `Expression.Compile`? Add `[RequiresDynamicCode]` next to `[RequiresUnreferencedCode]`.
- Adding a `[UnconditionalSuppressMessage]`? Prefer flowing the requirement (DAM/RUC) so the consumer is warned. Scope the suppression to the specific member, never the whole class. The justification must be *enforceable*, not a hope.
- New component that binds by string `Property`/`ValueProperty`/`CategoryProperty`? Add a TrimTest scenario over the **trimmable model library** and a runtime interaction step.

---

## 4. What Was Checked and Cleared

The following were investigated and found **not** to be hazards (builds confidence that the real findings are not noise):

- **Framework event-arg DTOs to JS** (`MouseEventArgs` -> `Radzen.resizeSplitter`, `RadzenSplitterPane`): named framework type, rooted by Blazor's own event-dispatch infrastructure, analyzer-visible. Safe.
- **`RadzenSlider` generic `TValue` to JS**: all supported `TValue` are numeric primitives / `IEnumerable<primitive>` with intrinsic STJ converters; non-`IConvertible` types are unsupported regardless of trimming. Safe.
- **All 40 `[JSInvokable]` declaring types** (including 8 declared in `.razor` files): every one matches a `preserve="methods"` entry in `LinkerConfig.xml`; no inherited-method gap. Verified complete.
- **AIChat `ChatCompletionRequest`/`ChatCompletionMessage` (`List<object> Messages`)**: only ever populated with the named, `preserve="all"` `ChatCompletionMessage`; no anonymous types; no consumer-injectable element type. Safe (maintenance footgun noted).
- **`RadzenDataGrid<TItem>` markup instantiation**: empirically verified that the Razor compiler's `OpenComponent<[DAM(All)] TComponent>` flows `All` to `Order`, preserving even an unbound property - so the *markup, app-assembly-model* case is safe. (The H1 hazard is the *separate trimmable-assembly model* case, which this does not cover.)
- **Enum reflection** (`EnumExtensions.GetDisplayDescription`, `EnumAsKeyValuePair`, `ConvertType.ChangeType` enum branch, `Enum.GetValues<T>`): ILLink does not strip enum value fields (atomic with the type) nor custom-attribute instances by default (no `RemoveAttributeInstances` config in the repo), and the attribute types are statically rooted. Degradation would be cosmetic (raw name), never a crash. Safe under trimming (AOT `Enum.GetValues(Type)` is a separate, out-of-scope `IL3050`).
- **`DataRow`/`DataTable`/`IDictionary` binding** (`Type.GetType("System."+name)`, `GetProperty("Item")`): the `(Type)it["x"]` string format makes `typeString` empty -> always falls back to `typeof(object)`; `DataRow.Item` indexers are rooted by `System.Data.Common`'s own internal callers; the path is `[RequiresUnreferencedCode]`. Safe.
- **`DialogService.Open<T>`/`OpenAsync<T>`/`OpenSide<T>`**: `[DAM(All)]` on the generic parameter roots all of `T`'s members at the call site; the downstream `Type`-laundering through `OpenComponent` does not un-root them. Safe (the dynamic-`Type` *side* overloads are the M31 gap).
- **`ComplexPropertiesConverter<T>` (write path)**: `Serialize<TValue>` is `[DAM(PublicProperties)]`; the converter is only ever parameterized by that concrete `TValue`, so STJ's requirement is satisfied; nested complex props are intentionally dropped from the body. Safe except the struct edge (L1).
- **DataGrid grouping / dynamic data binding (`TItem==object`, DataTable/ExpandoObject) / Excel-CSV export / Gauge-RangeNavigator-Sankey-Heatmap-Treemap-SpiderChart interop returns / HtmlEditor typed returns / Spreadsheet `VirtualRegion` + xlsx**: each verified either rooted by the non-trimmed consumer app assembly, preserved in `LinkerConfig`, primitive-typed, or reflection-free XML. Export byte-generation is server-side, outside the package. Safe.

Net: the dismissed set confirms the audit distinguishes "rooted by framework/consumer assembly or already in LinkerConfig" from "genuinely unprotected library-side gap." The 53 real hazards are concentrated in exactly the analyzer's documented blind spots - reflection over the consumer's model type, JS-interop DTO deserialization, dynamic LINQ, and `System.Text.Json` of user types - and the single highest-leverage fix is the H1 DAM annotation on the data-component base generics, followed by the four LinkerConfig DTO additions (H5-H8) and the `ReadAsync<T>` DAM annotation (H9).