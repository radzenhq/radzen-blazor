// Playwright crawler for the trimmed RadzenBlazorDemos WASM app.
//
// Visits a curated set of high-risk Radzen component demo routes, waits for the
// component to render, and records console errors, unhandled page errors, and the
// Blazor error UI. Emits a per-route PASS/FAIL table so member-trimming runtime
// breakage in Radzen.Blazor can be discovered without a server-side database.
//
// Scope: the demos' own models (Northwind etc.) live in the app assembly and are
// NOT member-trimmed under the default partial trim mode, so failures here are
// Radzen-OWN reflection / JS-interop / render-path bugs, not consumer-model bugs.
//
// Usage:
//   1. Publish the demos trimmed against the LOCAL Radzen.Blazor source:
//        dotnet publish RadzenBlazorDemos/RadzenBlazorDemos.csproj -c Release \
//          -p:PublishTrimmed=true -p:RadzenLocalProject=true \
//          -p:GenerateLlmsTxt=false -p:StaticWebAssetsEnabled=false \
//          -o <out>
//   2. Serve the published wwwroot with a WASM-MIME-correct server:
//        dotnet tool install -g dotnet-serve
//        dotnet serve -d <out>/wwwroot -p 5060
//   3. Crawl:
//        cd e2e && npm install && npx playwright install chromium
//        DEMOS_URL=http://localhost:5060 node demos-crawl.mjs
//
// Exit 0 when every crawled route passes; exit 1 if any route fails.

import { chromium } from 'playwright';
import { writeFileSync } from 'node:fs';

// Note: pick a port Chromium does not treat as unsafe (it blocks 5060, 6000, etc.).
const base = (process.env.DEMOS_URL || 'http://localhost:8088').replace(/\/$/, '');
const perPageTimeout = Number(process.env.DEMOS_PAGE_TIMEOUT_MS || 45000);
const settleMs = Number(process.env.DEMOS_SETTLE_MS || 2500);
const reportPath = process.env.DEMOS_REPORT || null;

// Console/page errors that are NOT Radzen trim breakage and must be ignored:
//  - CSP img-src violations: the demos' strict Content-Security-Policy blocks local /images
//    served over http when the demo-source-viewer assets are carved out of the lean publish.
//  - "define is not defined": the lazy-loaded Monaco editor (Edit Source / Playground) AMD
//    loader, which we deliberately do not exercise. The page still renders without it.
//  - favicon / static 404 / resource-load noise.
const benign = /Content Security Policy|img-src|favicon|net::ERR_|Failed to load resource|\bdefine is not defined\b|monaco|requirejs/i;

// Curated high-risk routes. `data` tags the example's data source:
//   inline = in-memory list literals (failures here are purely Radzen-own)
//   db     = EF Core InMemory + Northwind models in the app assembly (still
//            client-side; exercises Radzen reflection-over-T against non-trimmed models)
const routes = [
  { route: '/', component: 'Home', data: 'inline' },
  { route: '/datagrid', component: 'DataGrid', data: 'db' },
  { route: '/datagrid-dynamic', component: 'DataGrid (dynamic data)', data: 'inline' },
  { route: '/datagrid-enum-filter', component: 'DataGrid (enum filter)', data: 'inline' },
  { route: '/datagrid-filter-template', component: 'DataGrid (filter template)', data: 'inline' },
  { route: '/datagrid-realtime', component: 'DataGrid (realtime)', data: 'inline' },
  { route: '/datagrid-filter-api', component: 'DataGrid (filtering)', data: 'db' },
  { route: '/datagrid-sort-api', component: 'DataGrid (sorting)', data: 'db' },
  { route: '/datagrid-grouping-api', component: 'DataGrid (grouping)', data: 'db' },
  { route: '/datagrid-multiple-selection', component: 'DataGrid (selection)', data: 'db' },
  { route: '/datagrid-column-template', component: 'DataGrid (column template)', data: 'db' },
  { route: '/pivot-data-grid', component: 'PivotDataGrid', data: 'db' },
  { route: '/dropdown', component: 'DropDown', data: 'db' },
  { route: '/dropdown-datagrid', component: 'DropDownDataGrid', data: 'db' },
  { route: '/listbox', component: 'ListBox', data: 'db' },
  { route: '/tree', component: 'Tree', data: 'db' },
  { route: '/tree-checkboxes', component: 'Tree (checkboxes)', data: 'db' },
  { route: '/charts', component: 'Chart (gallery)', data: 'inline' },
  { route: '/column-chart', component: 'Chart (column)', data: 'inline' },
  { route: '/pie-chart', component: 'Chart (pie)', data: 'inline' },
  { route: '/scheduler', component: 'Scheduler', data: 'inline' },
  { route: '/radial-gauge', component: 'RadialGauge', data: 'inline' },
  { route: '/html-editor', component: 'HtmlEditor', data: 'inline' },
  { route: '/upload', component: 'Upload', data: 'inline' },
];

const browser = await chromium.launch();
const results = [];

for (const entry of routes) {
  const url = `${base}${entry.route}`;
  const page = await browser.newPage();
  const consoleErrors = [];
  const pageErrors = [];
  page.on('console', (m) => { if (m.type() === 'error') consoleErrors.push(m.text()); });
  page.on('pageerror', (e) => pageErrors.push(String(e)));

  let status = 'PASS';
  const notes = [];

  try {
    await page.goto(url, { waitUntil: 'load', timeout: perPageTimeout });

    // Wait for Blazor to boot and the demo content host to appear, or the error UI.
    await page.waitForFunction(() => {
      const err = document.getElementById('blazor-error-ui');
      const errVisible = err && getComputedStyle(err).display !== 'none';
      const booted = document.querySelector('.rz-body, main, .main, #app .rz-layout, [class*="rz-"]');
      return errVisible || booted;
    }, { timeout: perPageTimeout }).catch(() => {});

    // Let the component's OnAfterRenderAsync / JS interop settle.
    await page.waitForTimeout(settleMs);

    const probe = await page.evaluate(() => {
      const err = document.getElementById('blazor-error-ui');
      return {
        errUiVisible: !!(err && getComputedStyle(err).display !== 'none'),
        // Positive render assertion: a Radzen component instance is on the page and the WASM
        // runtime hydrated (interactive markup carries rz- classes well beyond the static chrome).
        rzNodes: document.querySelectorAll('[class*="rz-"]').length,
        component: document.querySelectorAll(
          '.rz-data-grid, .rz-datatable, .rz-chart, svg, .rz-scheduler, .rz-dropdown, .rz-tree, .rz-listbox, .rz-upload, .rz-html-editor, .rz-gauge'
        ).length,
      };
    });

    if (probe.errUiVisible) {
      status = 'FAIL';
      notes.push('blazor-error-ui visible');
    }
    if (probe.component === 0) {
      status = 'FAIL';
      notes.push(`no Radzen component rendered (rz nodes=${probe.rzNodes})`);
    }
    const realPageErrors = pageErrors.filter((e) => !benign.test(e));
    if (realPageErrors.length) {
      status = 'FAIL';
      notes.push(...realPageErrors.map((e) => `pageerror: ${e}`));
    }
    const realConsole = consoleErrors.filter((e) => !benign.test(e));
    if (realConsole.length) {
      status = 'FAIL';
      notes.push(...realConsole.map((e) => `console.error: ${e}`));
    }
  } catch (err) {
    status = 'FAIL';
    notes.push(`crawl exception: ${err.message}`);
  } finally {
    await page.close();
  }

  const dedup = [...new Set(notes)];
  results.push({ ...entry, url, status, errors: dedup });
  const tag = status === 'PASS' ? 'PASS' : 'FAIL';
  console.log(`${tag}  ${entry.route}  [${entry.data}]  ${entry.component}`);
  for (const n of dedup) console.log(`        ${n.slice(0, 500)}`);
}

await browser.close();

const failures = results.filter((r) => r.status === 'FAIL');
console.log(`\n${results.length - failures.length}/${results.length} routes passed; ${failures.length} failed.`);

if (reportPath) {
  writeFileSync(reportPath, JSON.stringify(results, null, 2));
  console.log(`JSON report written to ${reportPath}`);
}

process.exitCode = failures.length ? 1 : 0;
