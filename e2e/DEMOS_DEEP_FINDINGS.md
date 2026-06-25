# Demos deep-interaction trim crawl - findings

Upgrades the shallow demos crawl (`demos-crawl.mjs`, load + render assertion) to
**deep interaction** coverage of the trim-risky components only - the ones heavy on
reflection, dynamic LINQ, anonymous-object materialization, `[JSInvokable]` DTOs,
`MakeGenericType`, and JS interop. Every other route is intentionally ignored.

The crawler (`demos-deep-crawl.mjs`) drives each component through a real user
interaction, then checks for a trim break: the Blazor error UI (`#blazor-error-ui`)
becoming visible, an unhandled page error, or a `console.error` raised **during** the
interaction. A clean interaction = PASS.

## Scope (same as the shallow crawl - read this)

This exercises **Radzen's own** reflection / interop / render code, not consumer
models. The demos' models (Northwind etc.) live in the **app assembly**, which under
the default partial trim mode is **not** member-trimmed, so this catches Radzen-OWN
trim regressions (stripped `[JSInvokable]` DTOs, render-path reflection, dynamic-LINQ
expression building, `MakeGenericType` aggregates) - not the consumer-model hazard
(H1) covered by `Radzen.Blazor.TrimRuntimeTest` / `Radzen.Blazor.TrimTest`.

## Result

**14/14 component interactions PASS, 0 FAIL, 0 selector-unresolved** - verified against
the **actual WASM-client-trimmed publish** (not just the Debug serve). No risky
component broke under deep interaction.

Proof trimming was in effect for the verification run: the IL linker ran ("Optimizing
assemblies for size"), `Radzen.Blazor.wasm` was emitted at ~4.0 MB (the trimmed size),
and the publish produced **0 IL2xxx/IL3xxx** warnings. The interaction signals were
byte-for-byte identical between the trimmed and the non-trimmed Debug serve (same row
counts, same 60 enum options, same 96/185 filtered items, same 314 pivot aggregate
cells, Bold still mutated the markup, the chart tooltip still rendered, dialogs still
opened) - so every reflection / dynamic-LINQ / anonymous-object / `[JSInvokable]` /
`MakeGenericType` / interop path the interactions touch survives trimming.

CI runs the identical script against the same trimmed publish on every build - see
ci.yml `demos-deep-interaction-crawl` (non-blocking).

## Components driven, the hazard each exercises, and the interaction signal

| # | Route | Component / hazard | Interaction | Signal observed |
|---|---|---|---|---|
| 1 | `/datagrid-filter-api` | DataGrid filtering - dynamic LINQ `Where` + `string.Contains` | type "an" in the inline Simple filter, Enter | row count changed on filter |
| 2 | `/datagrid-sort-api` | DataGrid sorting - dynamic LINQ `OrderBy` over T | click a `.rz-sortable-column` header | first cell re-ordered (Steven -> Nancy) |
| 3 | `/datagrid-grouping-api` | DataGrid grouping - `GroupResult` / dynamic grouping | expand then collapse a group row toggler | toggled twice, no error |
| 4 | `/datagrid-enum-filter` | DataGrid enum / nullable-enum filter - anonymous `{Text,Value}` objects | open the `.rz-grid-filter-icon` popup, open the enum value dropdown, pick a value, Apply | 60 enum options materialized + filter applied |
| 5 | `/dropdown-datagrid` | DropDownDataGrid - `TextProperty` reflection | open, type "a" in the embedded grid's `.rz-lookup-search-input` | embedded grid filtered to 15 rows |
| 6 | `/dropdown-filtering` | filterable DropDown - `TextProperty` reflection | open, type "a" in `.rz-dropdown-filter` | 96 filtered items |
| 7 | `/autocomplete` | AutoComplete - `TextProperty` reflection | type "a" | 185 filtered suggestions |
| 8 | `/pivot-data-grid` | PivotDataGrid - `MakeGenericType` aggregates over T | assert aggregate cells; click a sortable pivot header | 314 aggregate cells materialized |
| 9 | `/upload` | Upload - `[JSInvokable]` PreviewFileInfo/FileInfo DTO | `setInputFiles` a temp .txt | 1 file row rendered (preview DTO round-trip) |
| 10 | `/html-editor` | HtmlEditor - interop DTO `RadzenHtmlEditorCommandState` | type, select-all (DOM range), click Bold | Bold command mutated the markup |
| 11 | `/spreadsheet` | Spreadsheet - `[JSInvokable]` CellEventArgs (the original production crash) | click a cell, type a value, Enter, select a 2x2 range | cell edit + range selection, no interop error |
| 12 | `/column-chart` | Chart - JS interop (tooltip / measure-text) | hover the column bars | tooltip element (`.rz-chart-tooltip`) rendered |
| 13 | `/dialog` | Dialog - `DialogService` dynamic open | click the demo open button, capture + close the modal | 2 dialog containers, opened and closed |
| 14 | `/scheduler` | Scheduler - `StartProperty`/`EndProperty` reflection + JS interop | click an appointment | edit dialog opened (2 dialog containers) |

## Selectors

Driven by Radzen's **stable component CSS classes**, consistent across the library, so
the suite survives demo content/markup changes rather than binding to per-demo markup:

- DataGrid: `.rz-cell-filter input.rz-textbox` (Simple text filter), `th.rz-sortable-column`,
  `.rz-grid-filter-icon` + `.rz-overlaypanel .rz-grid-filter` (Advanced filter popup),
  `.rz-row-toggler` (group expand).
- DropDown / AutoComplete: `.rz-dropdown`, `.rz-dropdown-trigger`, `.rz-dropdown-filter`,
  `.rz-lookup-search-input`, `.rz-autocomplete-input`, `.rz-dropdown-item`.
- Pivot: `.rz-pivot-table`, `.rz-pivot-drill-down-header .rz-tree-toggler`.
- Upload: `input[type=file]`, `.rz-fileupload-row`.
- HtmlEditor: `.rz-html-editor-content`, `.rz-html-editor-toolbar button[title*="Bold"]`.
- Spreadsheet: `.rz-spreadsheet`, `[data-row][data-column]`.
- Chart: `.rz-chart svg`, `.rz-chart-tooltip`.
- Dialog: `.rz-dialog-wrapper`, `.rz-dialog`, `.rz-dialog-titlebar-close`.
- Scheduler: `.rz-scheduler`, `.rz-event`, `.rz-slot`.

## How to reproduce

```sh
# Dev loop - drive a NON-trimmed Debug serve and iterate fast (reuse one server):
ASPNETCORE_ENVIRONMENT=Development \
  dotnet run --project RadzenBlazorDemos.Host --no-build --urls http://localhost:8088
# (launchSettings overrides ASPNETCORE_URLS; pass --urls so Chromium can reach the port)

cd e2e && npm install && npx playwright install chromium
DEMOS_URL=http://localhost:8088 node demos-deep-crawl.mjs
```

The trimmed publish + serve is exactly the shallow crawl's (see `DEMOS_TRIM_FINDINGS.md`);
CI reuses those flags for the deep job. Exit 0 if all PASS, else 1.

## Knobs (env vars)

- `DEMOS_URL` (default `http://localhost:8088`)
- `DEMOS_PAGE_TIMEOUT_MS` (nav timeout, default 45000)
- `DEMOS_SETTLE_MS` (post-render / post-interaction settle, default 2500)
- `DEMOS_STEP_TIMEOUT_MS` (per-selector resolve budget before selector-unresolved, default 8000)
- `DEMOS_REPORT` (path to write a JSON result report)

## Known gaps (honest)

- **GoogleMap (`/googlemap`)**: not crawled - needs an external Google Maps API key/network.
- The CI deep-interaction job is **non-blocking** (`continue-on-error: true`) initially:
  DOM interactions can be flaky on a cold CI runner. Promote to blocking once it has
  proven stable across a few runs.
- Pivot drill-down: the default `/pivot-data-grid` layout is flat (no collapsible groups),
  so the drill-down toggle is exercised only opportunistically; the `MakeGenericType`
  aggregate hazard is still fully exercised because the aggregate cells render on load.
