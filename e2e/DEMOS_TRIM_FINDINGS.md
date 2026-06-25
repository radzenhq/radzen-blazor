# Demos trim-safety crawl - findings

Discovery MVP: publish the Radzen Blazor demos with **member-trimming of `Radzen.Blazor`**
enabled, serve them, and crawl high-risk component pages in a real browser to find
components that break at runtime under trimming.

## Result

**No Radzen component broke under member-trimming.** 24/24 crawled routes rendered an
interactive Radzen component with no Blazor error UI and no runtime console/page error
attributable to Radzen. Driving a column-header sort on a DataGrid (`/datagrid-sort-api`)
re-ordered the rows correctly, so the reflection-over-`T` sort/filter expression path - the
path the static `TrimmingContractTests` / `trim-smoke` gate flags - works here.

### Why it passes (scope, read this)

This crawl exercises **Radzen's own** reflection / JS-interop / render code, not consumer
models. The demos' models (Northwind etc.) live in the **app assembly**, which under the
default **partial** trim mode is **not** member-trimmed - so Radzen's reflection over those
models still finds the getters/setters. That is the intended scope: it catches Radzen-OWN
trim bugs (stripped `[JSInvokable]` DTOs, render-path reflection, interop), and there are
none. It does **not** reproduce the consumer-model hazard (H1) where a trimmable consumer
assembly loses members Radzen reads by `string Property` - that is what
`Radzen.Blazor.TrimRuntimeTest` / `Radzen.Blazor.TrimTest` cover.

## What was trimmed (proof trimming actually happened)

- Published the ASP.NET Core host `RadzenBlazorDemos.Host` against the **local**
  `Radzen.Blazor` source (`<IsTrimmable>true>` on the `trim` branch), with member-trimming
  scoped to the **WASM client only** (`-p:TrimWasmClient=true`). The server is left
  untrimmed on purpose - trimming it breaks MVC application-part discovery
  (`Type.GetType` over a trimmed `ProvideApplicationPartFactoryAttribute` factory).
- The IL linker ran ("Optimizing assemblies for size"). `Radzen.Blazor.wasm` shrank from
  4,201,984 bytes (untrimmed source DLL) to 4,009,753 bytes in the published `_framework`
  (~190 KB / ~4.5% of members removed). 0 IL2xxx/IL3xxx trim warnings (the library is
  annotated/audited on this branch).
- The reduction is modest because the demos use almost the whole public surface of Radzen,
  so app-level member-trimming only removes what is genuinely unreachable.

## How to reproduce

```sh
# 1. Publish the host; trim only the WASM client, reference local Radzen source.
dotnet publish RadzenBlazorDemos.Host/RadzenBlazorDemos.Host.csproj -c Release \
  -p:RadzenLocalProject=true -p:TrimWasmClient=true \
  -p:GenerateLlmsTxt=false -p:CopyDemoPagesToWwwroot=false \
  -o <out>

# 2. The demo-source-viewer dir is carved out; create the empty dir the host expects.
mkdir -p <out>/wwwroot/demos <out>/wwwroot/images

# 3. Serve (use a port Chromium does not block - NOT 5060).
cd <out> && ASPNETCORE_URLS=http://localhost:8088 ASPNETCORE_ENVIRONMENT=Production \
  dotnet RadzenBlazorDemos.Host.dll

# 4. Crawl.
cd e2e && npm install && npx playwright install chromium
DEMOS_URL=http://localhost:8088 node demos-crawl.mjs
```

## Per-route results

`data` = the example's data source. `inline` = in-memory list literals (failures here would be
purely Radzen-own). `db` = EF Core **InMemory** provider + Northwind models in the app
assembly (still 100% client-side, no server DB; seeded in-process via `NorthwindContext.AddData`).

| Route | Component | Data | Result | Notes |
|---|---|---|---|---|
| `/` | Home | inline | PASS | |
| `/datagrid` | DataGrid | db | PASS | |
| `/datagrid-dynamic` | DataGrid (dynamic data) | inline | PASS | 10 data rows |
| `/datagrid-enum-filter` | DataGrid (enum filter) | inline | PASS | |
| `/datagrid-filter-template` | DataGrid (filter template) | inline | PASS | |
| `/datagrid-realtime` | DataGrid (realtime) | inline | PASS | |
| `/datagrid-filter-api` | DataGrid (filtering) | db | PASS | rows populated |
| `/datagrid-sort-api` | DataGrid (sorting) | db | PASS | header-click sort re-ordered rows |
| `/datagrid-grouping-api` | DataGrid (grouping) | db | PASS | |
| `/datagrid-multiple-selection` | DataGrid (selection) | db | PASS | |
| `/datagrid-column-template` | DataGrid (column template) | db | PASS | |
| `/pivot-data-grid` | PivotDataGrid | db | PASS | aggregates rendered |
| `/dropdown` | DropDown | db | PASS | |
| `/dropdown-datagrid` | DropDownDataGrid | db | PASS | |
| `/listbox` | ListBox | db | PASS | |
| `/tree` | Tree | db | PASS | |
| `/tree-checkboxes` | Tree (checkboxes) | db | PASS | |
| `/charts` | Chart (gallery) | inline | PASS | SVG rendered |
| `/column-chart` | Chart (column) | inline | PASS | |
| `/pie-chart` | Chart (pie) | inline | PASS | |
| `/scheduler` | Scheduler | inline | PASS | |
| `/radial-gauge` | RadialGauge | inline | PASS | |
| `/html-editor` | HtmlEditor | inline | PASS | |
| `/upload` | Upload | inline | PASS | |

## Ignored (non-trim) noise

The detector filters two error classes that are NOT trim breakage:

- **CSP `img-src` violations** for `/images/*.svg|png`: the demos ship a strict
  `Content-Security-Policy: img-src data: https:`, which blocks local images served over
  `http` once the demo-source-viewer / image assets are carved out of the lean publish.
- **`Uncaught ReferenceError: define is not defined`**: the lazy-loaded Monaco editor
  (Edit Source / Playground) AMD loader, which is deliberately not exercised. Pages render
  fully without it.

## Carve-outs and coverage gaps (honest)

- **Roslyn live-code editor / Playground** and **`/docs/api`** (lazy `Radzen.Blazor.Api.dll`,
  `Microsoft.CodeAnalysis.*`): not crawled (trim/AOT-hostile, irrelevant to component trim-safety).
- **GoogleMap (`/googlemap`)**: not crawled - needs an external Google Maps API key/network.
- **Server-side prerender vs WASM hydration**: the demos are a Blazor Web App
  (`InteractiveWebAssembly`, `blazor.web.js`). Each page is prerendered by the **untrimmed**
  server and then hydrated by the **trimmed** WASM client. Trim breakage would surface during
  or after hydration as a page/console error or a stuck `#blazor-error-ui` - none did.
- Interaction depth is shallow (load + one sort/select on a subset). Deeper edit/filter-popup
  flows were not automated.

## Build-infra blocker found (pre-existing, unrelated to trimming)

A clean `dotnet build/publish -c Release` of the demos against the **local project**
(`RadzenLocalProject=true`) crashes in the SDK 10.0.301 task
`GenerateStaticWebAssetsDevelopmentManifest` with `InvalidOperationException: Sequence
contains more than one element`. Root cause: `Radzen.Blazor.Api` referenced the
`Radzen.Blazor` **NuGet package** in Release while the demos referenced the **project**, so two
`_content/Radzen.Blazor` static-asset sets (package + project, each fingerprinted) collided and
tripped the SDK dev-manifest tree builder. Reproduces with the package ref too once both
sources are present. Fixed for the crawl by routing the whole graph through the local project
(see csproj changes). This is a demos/SDK build quirk, **not** a `Radzen.Blazor` runtime trim
issue.

## Reusable scaffolding committed

- `e2e/demos-crawl.mjs` - the Playwright crawler (curated routes, noise filter, positive
  render assertion, JSON report via `DEMOS_REPORT`).
- `.github/workflows` - a `workflow_dispatch` CI job (manual) that publishes the host
  trimmed and runs the crawl.
- Publish flags added to `RadzenBlazorDemos.csproj` / `Radzen.Blazor.Api.csproj`:
  `RadzenLocalProject`, `TrimWasmClient`, and skip switches for `GenerateLlmsTxt` /
  `CopyDemoPagesToWwwroot`.
