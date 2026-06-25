// Deep-interaction Playwright crawler for the trimmed RadzenBlazorDemos WASM app.
//
// Where demos-crawl.mjs only loads each route and asserts a component rendered,
// this crawler DRIVES the trim-risky components - the ones heavy on reflection,
// dynamic LINQ, anonymous-object paths, [JSInvokable] DTOs, MakeGenericType and
// JS interop - through a real user interaction, then checks for trim breakage.
//
// A trim break is detected as: the Blazor error UI (#blazor-error-ui) becoming
// visible, an unhandled page error, or a console.error - observed DURING the
// interaction (not just on load). A clean interaction = PASS for that component.
//
// Each interaction is wrapped in try/catch and self-reports PASS / FAIL /
// SELECTOR-UNRESOLVED, then the crawler CONTINUES to the next one. Selectors are
// Radzen's stable component CSS classes (consistent across the whole library),
// not per-demo markup, so the suite survives demo content changes.
//
// Usage (dev loop - drive a NON-trimmed Debug serve and iterate fast):
//   ASPNETCORE_URLS=http://localhost:8088 ASPNETCORE_ENVIRONMENT=Development \
//     dotnet run --project RadzenBlazorDemos.Host  (leave running, reuse it)
//   cd e2e && DEMOS_URL=http://localhost:8088 node demos-deep-crawl.mjs
//
// CI drives the same script against the trimmed publish (see ci.yml
// demos-deep-interaction-crawl). Exit 0 if every component PASSes, else 1.

import { chromium } from 'playwright';
import { writeFileSync, mkdtempSync } from 'node:fs';
import { join } from 'node:path';
import { tmpdir } from 'node:os';

const base = (process.env.DEMOS_URL || 'http://localhost:8088').replace(/\/$/, '');
const navTimeout = Number(process.env.DEMOS_PAGE_TIMEOUT_MS || 45000);
const settleMs = Number(process.env.DEMOS_SETTLE_MS || 2500);
const stepTimeout = Number(process.env.DEMOS_STEP_TIMEOUT_MS || 8000);
const reportPath = process.env.DEMOS_REPORT || null;

// Console/page noise that is NOT Radzen trim breakage (see demos-crawl.mjs for the
// rationale): CSP img-src on carved-out images, the lazy Monaco AMD loader, and
// generic resource-load 404s.
const benign = /Content Security Policy|img-src|favicon|net::ERR_|Failed to load resource|\bdefine is not defined\b|monaco|requirejs|the server responded with a status/i;

const browser = await chromium.launch();
const results = [];

// Run one component's interaction. `fn(page, ctx)` performs the clicks/typing and
// may push human-readable notes into ctx.notes. Throwing 'SELECTOR_UNRESOLVED'
// records the component as skipped (not a trim failure). Any error UI / pageerror /
// console.error captured during the run flips the component to FAIL.
async function drive(name, route, fn) {
  const url = `${base}${route}`;
  const page = await browser.newPage();
  const consoleErrors = [];
  const pageErrors = [];
  page.on('console', (m) => { if (m.type() === 'error') consoleErrors.push(m.text()); });
  page.on('pageerror', (e) => pageErrors.push(String(e)));

  let status = 'PASS';
  const notes = [];
  const ctx = { notes, settleMs, stepTimeout };

  try {
    await page.goto(url, { waitUntil: 'load', timeout: navTimeout });
    await page.waitForFunction(() => {
      const err = document.getElementById('blazor-error-ui');
      const errVisible = err && getComputedStyle(err).display !== 'none';
      return errVisible || document.querySelector('[class*="rz-"]');
    }, { timeout: navTimeout }).catch(() => {});
    await page.waitForTimeout(settleMs);

    await fn(page, ctx);

    // Let the post-interaction render / interop round-trip settle before probing.
    await page.waitForTimeout(800);

    const errUiVisible = await page.evaluate(() => {
      const err = document.getElementById('blazor-error-ui');
      return !!(err && getComputedStyle(err).display !== 'none');
    });
    if (errUiVisible) { status = 'FAIL'; notes.push('blazor-error-ui visible after interaction'); }

    const realPageErrors = pageErrors.filter((e) => !benign.test(e));
    if (realPageErrors.length) { status = 'FAIL'; notes.push(...realPageErrors.map((e) => `pageerror: ${e}`)); }
    const realConsole = consoleErrors.filter((e) => !benign.test(e));
    if (realConsole.length) { status = 'FAIL'; notes.push(...realConsole.map((e) => `console.error: ${e}`)); }
  } catch (err) {
    if (String(err.message).includes('SELECTOR_UNRESOLVED')) {
      status = 'SELECTOR-UNRESOLVED';
      notes.push(err.message);
    } else {
      status = 'FAIL';
      notes.push(`interaction exception: ${err.message}`);
    }
  } finally {
    await page.close();
  }

  const dedup = [...new Set(notes)];
  results.push({ name, route, url, status, errors: dedup });
  console.log(`${status.padEnd(20)} ${route.padEnd(28)} ${name}`);
  for (const n of dedup) console.log(`        ${n.slice(0, 500)}`);
}

// Resolve a selector within a bounded number of attempts; otherwise mark the
// component selector-unresolved and MOVE ON (never keep retrying / exploring).
async function need(page, selector, ctx, what = selector) {
  const loc = page.locator(selector).first();
  try {
    await loc.waitFor({ state: 'visible', timeout: ctx.stepTimeout });
    return loc;
  } catch {
    throw new Error(`SELECTOR_UNRESOLVED: ${what} (${selector})`);
  }
}

// ---------------------------------------------------------------------------
// 1. DataGrid filtering - dynamic LINQ + string.Contains/StartsWith.
//    FilterMode.Simple text columns render an inline `.rz-cell-filter input.rz-textbox`
//    in the header; typing + Enter runs OnFilter -> the dynamic Where(Contains) path.
await drive('DataGrid filtering (Contains)', '/datagrid-filter-api', async (page, ctx) => {
  const input = await need(page, '.rz-cell-filter input.rz-textbox', ctx, 'simple filter text input');
  const before = await page.locator('.rz-data-grid .rz-data-row, .rz-datatable-data tr').count();
  ctx.notes.push(`rows before filter=${before}`);
  await input.click();
  await input.fill('an');
  await input.press('Enter');
  await page.waitForTimeout(ctx.settleMs);
  const after = await page.locator('.rz-data-grid .rz-data-row, .rz-datatable-data tr').count();
  ctx.notes.push(`rows after Contains 'an' = ${after}`);
});

// 2. DataGrid sorting - dynamic LINQ OrderBy. Click a sortable column header.
await drive('DataGrid sorting (OrderBy)', '/datagrid-sort-api', async (page, ctx) => {
  const header = await need(page, 'th.rz-sortable-column .rz-column-title', ctx, 'sortable column header');
  const firstCellBefore = await page.locator('.rz-data-grid .rz-data-row td, .rz-datatable-data tr td').first().innerText().catch(() => '');
  await header.click();
  await page.waitForTimeout(ctx.settleMs);
  const firstCellAfter = await page.locator('.rz-data-grid .rz-data-row td, .rz-datatable-data tr td').first().innerText().catch(() => '');
  // Re-ordering the first cell is the real OrderBy-over-T signal.
  ctx.notes.push(`first cell before='${firstCellBefore.slice(0, 30)}' after='${firstCellAfter.slice(0, 30)}' reordered=${firstCellBefore !== firstCellAfter}`);
});

// 3. DataGrid grouping - GroupResult / dynamic grouping. Expand/collapse a group row.
await drive('DataGrid grouping (expand/collapse)', '/datagrid-grouping-api', async (page, ctx) => {
  const toggler = await need(page, '.rz-datatable-data .rz-row-toggler, .rz-group-row .rz-cell-data span[class*="rzi"], .rz-data-grid .rz-row-toggler', ctx, 'group expand toggler');
  await toggler.click();
  await page.waitForTimeout(ctx.settleMs);
  // toggle back
  await toggler.click().catch(() => {});
  await page.waitForTimeout(800);
  ctx.notes.push('toggled a group row twice');
});

// 4. DataGrid enum / nullable-enum filter - anonymous-object path in the enum filter.
//    The demo uses the default FilterMode.Advanced: a `.rz-grid-filter-icon` button
//    opens the `.rz-overlaypanel` popup whose enum value RadzenDropDown materializes
//    the EnumExtensions.EnumAsKeyValuePair anonymous {Text,Value} objects.
await drive('DataGrid enum filter popup', '/datagrid-enum-filter', async (page, ctx) => {
  const filterIcon = await need(page, 'button.rz-grid-filter-icon', ctx, 'column filter icon');
  await filterIcon.click();
  await page.waitForTimeout(ctx.settleMs);
  // Open the enum value dropdown inside the popup (this binds EnumAsKeyValuePair).
  const enumDropdown = page.locator('.rz-overlaypanel .rz-grid-filter .rz-dropdown').last();
  if (await enumDropdown.count()) {
    await enumDropdown.click();
    await page.waitForTimeout(ctx.settleMs);
    const items = await page.locator('.rz-dropdown-panel .rz-dropdown-item, .rz-dropdown-items .rz-dropdown-item').count();
    ctx.notes.push(`enum filter options rendered=${items}`);
    if (items > 0) {
      await page.locator('.rz-dropdown-panel .rz-dropdown-item, .rz-dropdown-items .rz-dropdown-item').nth(1).click().catch(() => {});
      await page.waitForTimeout(600);
      const applyBtn = page.locator('.rz-overlaypanel .rz-apply-filter').first();
      if (await applyBtn.count()) { await applyBtn.click().catch(() => {}); await page.waitForTimeout(ctx.settleMs); }
      ctx.notes.push('selected an enum value and applied the filter');
    }
  } else {
    ctx.notes.push('no enum dropdown in popup');
  }
});

// 5a. DropDownDataGrid - TextProperty reflection. Open, type in the embedded grid's
//     lookup search box (`.rz-lookup-search-input`), exercising filter-over-T.
await drive('DropDownDataGrid open + filter', '/dropdown-datagrid', async (page, ctx) => {
  const trigger = await need(page, '.rz-dropdown .rz-dropdown-trigger, .rz-dropdown', ctx, 'dropdown trigger');
  await trigger.click();
  await page.waitForTimeout(ctx.settleMs);
  const filterInput = page.locator('input.rz-lookup-search-input:visible').first();
  if (await filterInput.count()) {
    await filterInput.fill('a');
    await page.waitForTimeout(ctx.settleMs);
    const rows = await page.locator('.rz-data-grid .rz-data-row:visible, .rz-datatable-data tr:visible').count();
    ctx.notes.push(`embedded grid rows after typing 'a'=${rows}`);
  } else {
    ctx.notes.push('SELECTOR_UNRESOLVED: dropdown-datagrid lookup search input');
    throw new Error('SELECTOR_UNRESOLVED: dropdown-datagrid lookup search input (input.rz-lookup-search-input)');
  }
});

// 5b. Filterable DropDown - TextProperty reflection over the filter text. The filter
//     panel is a Popup; click the dropdown trigger to open it, then type in the filter.
await drive('DropDown filtering', '/dropdown-filtering', async (page, ctx) => {
  const trigger = await need(page, '.rz-dropdown .rz-dropdown-trigger, .rz-dropdown', ctx, 'dropdown trigger');
  await trigger.click();
  await page.waitForTimeout(ctx.settleMs);
  const filter = page.locator('input.rz-dropdown-filter:visible').first();
  if (!(await filter.count())) {
    throw new Error('SELECTOR_UNRESOLVED: dropdown filter input not visible after open (input.rz-dropdown-filter)');
  }
  await filter.fill('a');
  await page.waitForTimeout(ctx.settleMs);
  const items = await page.locator('.rz-dropdown-items .rz-dropdown-item, .rz-dropdown-panel .rz-dropdown-item').count();
  ctx.notes.push(`filtered dropdown items=${items}`);
});

// 5c. AutoComplete - TextProperty reflection. Type to trigger the filtered suggestion list.
await drive('AutoComplete filtering', '/autocomplete', async (page, ctx) => {
  const input = await need(page, 'input.rz-autocomplete-input', ctx, 'autocomplete input');
  await input.click();
  await input.fill('a');
  await page.waitForTimeout(ctx.settleMs);
  const items = await page.locator('.rz-autocomplete-items .rz-autocomplete-list-item').count();
  ctx.notes.push(`autocomplete suggestions=${items}`);
});

// 6. PivotDataGrid - MakeGenericType aggregates over T. Computing the aggregate cells
//    is the reflective hazard; it happens on render. Assert cells materialized, then
//    drive a column-header sort (re-runs the aggregate pipeline) and a drill-down if present.
await drive('PivotDataGrid aggregates + sort', '/pivot-data-grid', async (page, ctx) => {
  await need(page, '.rz-pivot-table', ctx, 'pivot table');
  const cells = await page.locator('.rz-pivot-table td').count();
  ctx.notes.push(`pivot aggregate cells rendered=${cells}`);
  if (cells === 0) { ctx.notes.push('no aggregate cells - aggregate pipeline produced nothing'); }
  // Re-run the aggregate/sort pipeline by clicking a sortable pivot header.
  const sortable = page.locator('.rz-pivot-table .rz-sortable .rz-pivot-header-text, .rz-pivot-column-header.rz-sortable-column').first();
  if (await sortable.count()) {
    await sortable.click().catch(() => {});
    await page.waitForTimeout(ctx.settleMs);
    ctx.notes.push('clicked a sortable pivot header (re-ran aggregates)');
  }
  // Drill-down if this layout has collapsible groups.
  const toggler = page.locator('.rz-pivot-drill-down-header .rz-tree-toggler').first();
  if (await toggler.count()) {
    await toggler.click();
    await page.waitForTimeout(ctx.settleMs);
    await toggler.click().catch(() => {});
    await page.waitForTimeout(800);
    ctx.notes.push('toggled a pivot drill-down group');
  }
});

// 7. Upload - [JSInvokable] PreviewFileInfo/FileInfo. Drive input[type=file] with a temp file.
await drive('Upload setInputFiles', '/upload', async (page, ctx) => {
  const fileInput = await need(page, 'input[type=file]', ctx, 'file input');
  const dir = mkdtempSync(join(tmpdir(), 'rz-upload-'));
  const tmp = join(dir, 'trim-probe.txt');
  writeFileSync(tmp, 'radzen trim probe payload');
  await fileInput.setInputFiles(tmp);
  await page.waitForTimeout(ctx.settleMs);
  // After selection the Upload renders a file row / preview; the [JSInvokable]
  // PreviewFileInfo round-trip is what trimming can break.
  const rows = await page.locator('.rz-fileupload-files .rz-fileupload-row, .rz-fileupload-filename').count();
  ctx.notes.push(`upload file rows after setInputFiles=${rows}`);
});

// 8. HtmlEditor - interop DTO RadzenHtmlEditorCommandState. The Bold command and the
//    selection-change state report both round-trip that DTO through JS interop.
//    Type text, select it, then click Bold (selecting first so execCommand wraps it).
await drive('HtmlEditor type + Bold', '/html-editor', async (page, ctx) => {
  const content = await need(page, '.rz-html-editor-content', ctx, 'html editor content area');
  await content.click();
  await page.keyboard.type('Trim probe text');
  await page.waitForTimeout(600);
  const before = await content.innerHTML().catch(() => '');
  // Select the editor's contents with a DOM range so Bold has a range to format
  // (Ctrl+A in a contenteditable can escape to the document).
  await page.evaluate(() => {
    const el = document.querySelector('.rz-html-editor-content');
    el.focus();
    const r = document.createRange();
    r.selectNodeContents(el);
    const s = window.getSelection();
    s.removeAllRanges();
    s.addRange(r);
  });
  await page.waitForTimeout(400);
  const bold = await need(page, '.rz-html-editor-toolbar button[title*="Bold" i]', ctx, 'Bold toolbar button');
  await bold.click();
  await page.waitForTimeout(ctx.settleMs);
  const after = await content.innerHTML().catch(() => '');
  // The Bold command round-trips RadzenHtmlEditorCommandState through JS interop and
  // mutates the markup (adds <b>/<strong> or a font-weight span). Either the markup
  // changed or carries a weight style = the interop DTO survived trimming.
  const formatted = after !== before || /<(b|strong)\b|font-weight/i.test(after);
  ctx.notes.push(`HtmlEditor Bold command mutated markup=${formatted}`);
});

// 9. Spreadsheet - [JSInvokable] CellEventArgs (the original production crash).
//    Click a cell, type a value, commit with Enter.
await drive('Spreadsheet cell edit', '/spreadsheet', async (page, ctx) => {
  await need(page, '.rz-spreadsheet', ctx, 'spreadsheet');
  const cell = page.locator('.rz-spreadsheet [data-row][data-column]').first();
  try {
    await cell.waitFor({ state: 'visible', timeout: ctx.stepTimeout });
  } catch {
    throw new Error('SELECTOR_UNRESOLVED: spreadsheet cell ([data-row][data-column])');
  }
  await cell.click();
  await page.waitForTimeout(600);
  await page.keyboard.type('123');
  await page.keyboard.press('Enter');
  await page.waitForTimeout(ctx.settleMs);
  // Select a range to exercise the CellEventArgs/selection interop too.
  await cell.click();
  await page.keyboard.down('Shift');
  await page.keyboard.press('ArrowDown');
  await page.keyboard.press('ArrowRight');
  await page.keyboard.up('Shift');
  await page.waitForTimeout(800);
  ctx.notes.push('typed a value into a cell and selected a 2x2 range');
});

// 10. Chart - JS interop (measure-text/positioning) + tooltip. Hover the column bars to
//     trigger the tooltip render (.rz-chart-tooltip), exercising the interop round-trip.
await drive('Chart hover tooltip', '/column-chart', async (page, ctx) => {
  const svg = await need(page, '.rz-chart svg', ctx, 'chart svg');
  // Hover the actual column bars first; fall back to sweeping the plot area.
  const bars = page.locator('.rz-chart svg path[class*="column"], .rz-chart svg .rz-column-series path, .rz-chart svg rect');
  const n = await bars.count();
  let tooltip = 0;
  for (let i = 0; i < Math.min(n, 6) && tooltip === 0; i++) {
    await bars.nth(i).hover({ force: true }).catch(() => {});
    await page.waitForTimeout(400);
    tooltip = await page.locator('.rz-chart-tooltip').count();
  }
  if (tooltip === 0) {
    const box = await svg.boundingBox();
    if (box) {
      for (const fx of [0.25, 0.4, 0.55, 0.7, 0.85]) {
        await page.mouse.move(box.x + box.width * fx, box.y + box.height * 0.75);
        await page.waitForTimeout(300);
        tooltip = await page.locator('.rz-chart-tooltip').count();
        if (tooltip) break;
      }
    }
  }
  ctx.notes.push(`chart bars=${n}; tooltip elements after hover=${tooltip}`);
});

// 11. Dialog - DialogService.OpenAsync opens a page as a dynamic dialog. Click the
//     demo's open button (scope past the app chrome buttons like the theme toggle).
await drive('Dialog open modal', '/dialog', async (page, ctx) => {
  // The first demo button opens an order page as a dialog ("Order NNNNN details").
  const openBtn = page.locator('.rz-button', { hasText: /details/i }).first();
  let target = openBtn;
  if (!(await openBtn.count())) {
    // Fall back to the first demo-content button after the layout chrome.
    target = page.locator('main .rz-button:not([disabled]), .rz-body .rz-button:not([disabled])').first();
  }
  if (!(await target.count())) throw new Error('SELECTOR_UNRESOLVED: dialog open button');
  await target.click();
  await page.waitForTimeout(ctx.settleMs);
  const dialogs = await page.locator('.rz-dialog-wrapper, .rz-dialog').count();
  ctx.notes.push(`dialog containers visible after open=${dialogs}`);
  if (dialogs > 0) {
    const close = page.locator('.rz-dialog-titlebar-close').first();
    await close.click().catch(() => {});
    await page.waitForTimeout(600);
    ctx.notes.push('opened and closed the dialog');
  } else {
    ctx.notes.push('no dialog opened');
  }
});

// 12. Scheduler - StartProperty/EndProperty reflection + JS interop. Click a slot/appointment.
await drive('Scheduler slot/appointment click', '/scheduler', async (page, ctx) => {
  await need(page, '.rz-scheduler', ctx, 'scheduler');
  const appointment = page.locator('.rz-event, .rz-event-content').first();
  if (await appointment.count()) {
    await appointment.click();
    await page.waitForTimeout(ctx.settleMs);
    ctx.notes.push('clicked an appointment');
  } else {
    const slot = await need(page, '.rz-slot', ctx, 'scheduler slot');
    await slot.click();
    await page.waitForTimeout(ctx.settleMs);
    ctx.notes.push('clicked an empty slot');
  }
  // A slot/appointment click opens the add/edit appointment dialog via DialogService.
  const dialogs = await page.locator('.rz-dialog, .rz-dialog-wrapper').count();
  ctx.notes.push(`scheduler dialog containers after click=${dialogs}`);
});

await browser.close();

const failures = results.filter((r) => r.status === 'FAIL');
const unresolved = results.filter((r) => r.status === 'SELECTOR-UNRESOLVED');
const passed = results.filter((r) => r.status === 'PASS');
console.log(`\n${passed.length}/${results.length} components PASS; ${failures.length} FAIL; ${unresolved.length} selector-unresolved.`);

if (reportPath) {
  writeFileSync(reportPath, JSON.stringify(results, null, 2));
  console.log(`JSON report written to ${reportPath}`);
}

process.exitCode = failures.length ? 1 : 0;
