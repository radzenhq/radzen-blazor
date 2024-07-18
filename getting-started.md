<style>
  .tabs {
  display: flex;
  flex-wrap: wrap;
}

.tabs-radio {
  position: absolute;
  opacity: 0;
}

.tabs-label {
  width: 80px;
}

.tabs-content {
  display: none;
  width: 100%;
  order:99;
}

.tabs-radio:checked + .tabs-label + .tabs-content {
  display: block;
}
</style>
<h3 id="interactivity"><a href="#interactivity">Interactivity and SSR</a></h3>
<p>
All interactive features of the Radzen Blazor components require interactivity for the container <code>.razor</code> file to be enabled or the <code>@rendermode</code>
attribute of the component to be set to one of the following values: <code>InteractiveServer</code>, <code>InteractiveAuto</code> or <code>@InteractiveWebAssembly</code>.
More info is available in the <a href="https://learn.microsoft.com/en-us/aspnet/core/blazor/components/render-modes?view=aspnetcore-8.0" target="_blank">rendering mode article</a> from the official Blazor documentation.
</p>
<h3 id="install"><a href="#install">Install</a></h3>
<p>
The Radzen Blazor components are distributed via the <a href="">Radzen.Blazor</a> nuget package.
</p>
<p>
  You can add the nuget package to your Blazor application in one of the following ways:
</p>
<ul>
  <li>Via Visual Studio's Nuget Package Manager <img src="https://blazor.radzen.com/images/nuget-explorer.png"></li>
  <li>Via command line <code>dotnet add package Radzen.Blazor</code></li>
  <li>By editing your application's <code>.csproj</code> file and adding a package reference <code>lt;PackageReference Include="Radzen.Blazor" Version="*" /&gt;</code></li>
</ul>
<h3 id="import"><a href="#import">Import the namespaces</a></h3>
<p>
  Import the namespaces by adding the following lines to <code>_Imports.razor</code>:
<pre>
  <code>
@using Radzen
@using Radzen.Blazor
  </code>
</pre>
</p>
<h3 id="theme"><a href="#theme">Set the theme</a></h3>
<div class="tabs">
  <input class="tabs-radio" name="tabs" checked type="radio" id="theme-net8">
  <label class="tabs-label" for="theme-net8">.NET 8</label>
  <div class="tabs-content">
    <p>Open the <code>App.razor</code> file of your application. Add this code within the <code>&lt;head&gt;</code> element:</p>
<pre>
<code>
&lt;RadzenTheme Theme="material" @rendermode="InteractiveAuto" /&gt;
</code>
</pre>
    <div class="alert">
      Use a render mode which you have enabled for your application. You can also omit the <code>@rendermode</code> attribute if you don't need interactive theme features such
      as changing the theme at runtime.
    </div>
  </div>
  
  <input class="tabs-radio" name="tabs" type="radio" id="theme-net7-server">
  <label class="tabs-label" for="theme-net7-server">.NET 7 Server</label>
  <div class="tabs-content">
    Open the <code>Pages\_Host.cshtml</code> file of your application. Add this code within the <code>&lt;head&gt;</code> element:</p>
<pre>
<code>
&lt;component type="typeof(RadzenTheme)" render-mode="ServerPrerendered" param-Theme="@("material")" /&gt;
</code>
</pre>
  </div>
<input class="tabs-radio" name="tabs" type="radio" id="theme-net7-wasm">
  <label class="tabs-label" for="theme-net7-wasm">.NET 7 WebAssembly</label>
  <div class="tabs-content">
    Open the <code>Pages\_Host.cshtml</code> file of your application. Add this code within the <code>&lt;head&gt;</code> element:</p>
<pre>
<code>
&lt;component type="typeof(RadzenTheme)" render-mode="WebAssemblyPrerendered" param-Theme="@("material")" /&gt;
</code>
</pre>
  </div>
  <input class="tabs-radio" name="tabs"  type="radio" id="theme-net6-server">
  <label class="tabs-label" for="theme-net6-server">.NET 6 Server</label>
  <div class="tabs-content">
    Open the <code>Pages\_Layout.cshtml</code> file of your application. Add this code within the <code>&lt;head&gt;</code> element:</p>
<pre>
<code>
&lt;component type="typeof(RadzenTheme)" render-mode="ServerPrerendered" param-Theme="@("material")" /&gt;
</code>
</pre>
  </div>
  <input class="tabs-radio" name="tabs"  type="radio" id="theme-net6-wasm">
  <label class="tabs-label" for="theme-net6-wasm">.NET 6 WebAssembly</label>
  <div class="tabs-content">
    Open the <code>Pages\_Layout.cshtml</code> file of your application. Add this code within the <code>&lt;head&gt;</code> element:</p>
<pre>
<code>
&lt;component type="typeof(RadzenTheme)" render-mode="WebAssemblyPrerendered" param-Theme="@("material")" /&gt;
</code>
</pre>
  </div>
  <input class="tabs-radio" name="tabs" type="radio" id="theme-wasm">
  <label class="tabs-label" for="theme-wasm">WebAssembly (standalone)</label>
  <div class="tabs-content">
    If you have a standalone (not hosted) Blazor WebAssembly application open the <code>index.html</code> file and add this code within the <code>&lt;head&gt;</code> element:</p>
<pre>
<code>
&lt;link rel="stylesheet" href="_content/Radzen.Blazor/css/material-base.css"&gt;
</code>
</pre>
  </div>
</div>
<h3 id="javascript"><a href="#javascript">Include Radzen.Blazor.js</a></h3>
<div class="tabs">
  <input class="tabs-radio" name="tabs" checked type="radio" id="js-net8">
  <label class="tabs-label" for="js-net8">.NET 8</label>
  <div class="tabs-content">
    <p>Open the <code>App.razor</code> file of your application. Add this code after the last <code>&lt;script&gt;</code>:</p>
<pre>
<code>
&lt;script src="_content/Radzen.Blazor/Radzen.Blazor.js?v=@(typeof(Radzen.Colors).Assembly.GetName().Version)"&gt;&lt;/script&gt;
</code>
</pre>
  </div>
  
  <input class="tabs-radio" name="tabs" type="radio" id="js-net7">
  <label class="tabs-label" for="js-net7">.NET 7</label>
  <div class="tabs-content">
    Open the <code>Pages\_Host.cshtml</code> file of your application. Add this code after the last <code>&lt;script&gt;</code>:</p>
<pre>
<code>
&lt;script src="_content/Radzen.Blazor/Radzen.Blazor.js?v=@(typeof(Radzen.Colors).Assembly.GetName().Version)"&gt;&lt;/script&gt;
</code>
</pre>
  </div>
  <input class="tabs-radio" name="tabs" type="radio" id="js-net6">
  <label class="tabs-label" for="js-net6">.NET 6</label>
  <div class="tabs-content">
    Open the <code>Pages\_Layout.cshtml</code> file of your application. Add this code after the last <code>&lt;script&gt;</code>:</p>
<pre>
<code>
&lt;script src="_content/Radzen.Blazor/Radzen.Blazor.js?v=@(typeof(Radzen.Colors).Assembly.GetName().Version)"&gt;&lt;/script&gt;
</code>
</pre>
  </div>
  <input class="tabs-radio" name="tabs" type="radio" id="js-wasm">
  <label class="tabs-label" for="js-wasm">WebAssembly (standalone)</label>
  <div class="tabs-content">
    If you have a standalone (not hosted) Blazor WebAssembly application open the <code>index.html</code> file and add this this code after the last <code>&lt;script&gt;</code>:</p>
<pre>
<code>
&lt;script src="_content/Radzen.Blazor/Radzen.Blazor.js"&gt;&lt;/script&gt;
</code>
</pre>
  </div>
</div>
<h3 id="use"><a href="#use">Use a component</a></h3>
<p>To use a component type its tag name in your Blazor page:</p>
<pre>
<code>
&lt;RadzenButton Text="Hi" Click=@OnClick /&gt;
@code {
  void OnClick()
  {
     // Handle the click event
  }
}
</code>
</pre>
<h3 id="outlets"><a href="#outlets">Dialog, context menu, tooltip and notification</a></h3>
<p>
To use RadzenDialog, RadzenContextMenu, RadzenTooltip and RadzenNotification you need to perform a few additional steps.
</p>
<ol>
  <li>Open <code>MainLayout.razor</code> and add this code
  <div class="tabs">
    <input class="tabs-radio" name="tabs" checked type="radio" id="layout-net8">
    <label class="tabs-label" for="tlayout-net8">.NET 8</label>
    <div class="tabs-content">
<pre>
<code>
&lt;RadzenComponents @rendermode=InteractiveAuto /&gt;
</code>
</pre>
      <div class="alert">
        Use a render mode which you have enabled for your application. RadzenDialog, RadzenContextMenu, RadzenTooltip and RadzenNotification require interactivity and will not work in static render mode (SSR).
      </div>
    </div>
    <input class="tabs-radio" name="tabs" checked type="radio" id="layout-other">
    <label class="tabs-label" for="layout-other">.NET 7 &amp; .NET 6</label>
    <div class="tabs-content">
<pre>
<code>
&lt;RadzenComponents /&gt;
</code>
</pre>
    </div>
  </div>
  </li>
  <li>
  Open <code>Program.cs</code> and add this code before <code>builder.Build()</code>:
<pre>
<code>
builder.Services.AddRadzenComponents();
</code>
</pre>
  </li>
</ol>
