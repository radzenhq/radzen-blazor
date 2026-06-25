// Playwright runtime gate for the trimmed Radzen.Blazor.TrimTest WASM app.
//
// Usage:
//   1. Publish the app trimmed:
//        dotnet publish ../Radzen.Blazor.TrimTest/Radzen.Blazor.TrimTest.csproj -c Release
//   2. Serve the published wwwroot with a WASM-MIME-correct server, e.g.:
//        dotnet tool install -g dotnet-serve
//        dotnet serve -d ../Radzen.Blazor.TrimTest/bin/Release/net10.0/publish/wwwroot -p 5050
//   3. Run this harness:
//        npm install && npx playwright install chromium
//        TRIM_URL=http://localhost:5050 node trim-smoke.mjs
//
// Exit 0 when #trim-status === "done"; exit 1 on any error: marker, the global error backstop,
// a console error, or a timeout while still "running".

import { chromium } from 'playwright';

const url = process.env.TRIM_URL || 'http://localhost:5050';
const timeoutMs = Number(process.env.TRIM_TIMEOUT_MS || 120000);

const browser = await chromium.launch();
const page = await browser.newPage();

const consoleErrors = [];
page.on('console', (msg) => { if (msg.type() === 'error') consoleErrors.push(msg.text()); });
page.on('pageerror', (err) => consoleErrors.push(String(err)));

let status = 'running';
try {
  await page.goto(url, { waitUntil: 'load' });

  // Wait until the app reports a terminal status, or the global backstop fires.
  await page.waitForFunction(() => {
    const s = document.getElementById('trim-status');
    const g = document.getElementById('trim-global-error');
    const gVisible = g && g.style.display !== 'none' && g.textContent;
    return (s && s.textContent && s.textContent !== 'running') || gVisible;
  }, { timeout: timeoutMs });

  status = await page.$eval('#trim-status', (el) => el.textContent).catch(() => 'running');
  const globalError = await page
    .$eval('#trim-global-error', (el) => (el.style.display !== 'none' ? el.textContent : ''))
    .catch(() => '');

  if (globalError) {
    console.error(`FAIL: global error backstop fired: ${globalError}`);
    process.exitCode = 1;
  } else if (status !== 'done') {
    console.error(`FAIL: #trim-status = ${status}`);
    process.exitCode = 1;
  } else if (consoleErrors.length) {
    console.error(`FAIL: console errors during drive:\n  ${consoleErrors.join('\n  ')}`);
    process.exitCode = 1;
  } else {
    console.log('PASS: trimmed WASM app drove sort/filter/group over the model with no error.');
  }
} catch (err) {
  console.error(`FAIL: ${err.message} (last #trim-status = ${status})`);
  if (consoleErrors.length) console.error(`console errors:\n  ${consoleErrors.join('\n  ')}`);
  process.exitCode = 1;
} finally {
  await browser.close();
}
