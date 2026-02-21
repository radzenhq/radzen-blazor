![Radzen Blazor Components](https://raw.githubusercontent.com/radzenhq/radzen-blazor/master/RadzenBlazorDemos/wwwroot/images/radzen-blazor-components.png)

Radzen Blazor Components
========================

The most sophisticated free UI component library for Blazor, featuring **100+ native components**. MIT licensed, used by thousands of developers at companies like Microsoft, NASA, Porsche, Dell, Siemens, and DHL.

Supports .NET 10, Blazor Server, Blazor WebAssembly, and .NET MAUI Blazor Hybrid.

[Online Demos](https://blazor.radzen.com) · [Documentation](https://blazor.radzen.com/docs) · [Get Started](https://blazor.radzen.com/get-started)

 [![License - MIT](https://img.shields.io/github/license/radzenhq/radzen-blazor?logo=github&style=for-the-badge)](https://github.com/radzenhq/radzen-blazor/blob/master/LICENSE)[![NuGet Downloads](https://img.shields.io/nuget/dt/Radzen.Blazor?color=%232694F9&label=nuget%20downloads&logo=nuget&style=for-the-badge) ](https://www.nuget.org/packages/Radzen.Blazor)![Last Commit](https://img.shields.io/github/last-commit/radzenhq/radzen-blazor?logo=github&style=for-the-badge) [![Github Contributors](https://img.shields.io/github/contributors/radzenhq/radzen-blazor?logo=github&style=for-the-badge) ](https://github.com/radzenhq/radzen-blazor/graphs/contributors)[![Radzen Blazor Components - Online Demos](https://img.shields.io/badge/demos-online-brightgreen?color=%232694F9&logo=blazor&style=for-the-badge) ](https://blazor.radzen.com)[![Radzen Blazor Components - Documentation](https://img.shields.io/badge/docs-online-brightgreen?color=%232694F9&logo=blazor&style=for-the-badge)](https://blazor.radzen.com/docs)

## Quick start

Install the NuGet package:
```bash
dotnet add package Radzen.Blazor
```

Add to `_Imports.razor`:
```razor
@using Radzen
@using Radzen.Blazor
```

Add the theme and script to `App.razor`:
```html
<!-- inside <head> -->
<RadzenTheme Theme="material" />

<!-- after the last <script> -->
<script src="_content/Radzen.Blazor/Radzen.Blazor.js"></script>
```

Register services in `Program.cs`:
```csharp
builder.Services.AddRadzenComponents();
```

Use a component:
```razor
<RadzenButton Text="Hello World" Click="@OnClick" />
```

For the full setup guide including render modes and dialog/notification configuration, see the [getting started instructions](https://blazor.radzen.com/get-started).

## Components

**Data** — DataGrid, DataList, PivotDataGrid, Pager, Tree, Scheduler, Charts, GaugeCharts

**Forms** — TextBox, TextArea, Password, Numeric, DatePicker, TimePicker, ColorPicker, Dropdown, AutoComplete, ListBox, CheckBox, RadioButtonList, Switch, Slider, Rating, FileInput, HtmlEditor

**Layout** — Card, Panel, Tabs, Accordion, Splitter, Steps, Dialog, Fieldset

**Navigation** — Menu, ContextMenu, PanelMenu, Breadcrumb, Link, TreeView

**Feedback** — Notification, Alert, ProgressBar, Badge, Tooltip, Skeleton

**Theming** — 10 built-in themes with light and dark variants. Free themes: Material, Standard, Default, Humanistic, Software. Premium themes (included with [Radzen Blazor Pro](https://www.radzen.com/pricing)): Material 3, Fluent. Full CSS variable customization and a built-in theme service for runtime switching.

[Browse all components with live demos →](https://blazor.radzen.com)

## Why choose Radzen Blazor Components?

### :sparkles: Free and open source

MIT licensed and free for commercial use. No per-developer fees, no runtime royalties. Install from [NuGet](https://www.nuget.org/packages/Radzen.Blazor) or build from source.

### :computer: 100% native Blazor

Written entirely in C#. No JavaScript framework dependencies, no wrappers. 

### :seedling: Actively maintained

Frequent releases with new components and features. Short development cycle — we ship as soon as new features are ready instead of batching into quarterly releases.

### :office: Trusted in production

Used by developers at Microsoft, NASA, Porsche, Dell, Siemens, Nokia, DHL, HSBC, Allianz, Accenture, Deloitte, and thousands of other organizations worldwide.

## Support

### :speech_balloon: Community Support
Visit the [Radzen Community forum](https://forum.radzen.com/) — 400+ active weekly users with an average response time of 2 hours.

### :dart: Radzen Blazor Pro

For dedicated support and additional productivity tools, the [Radzen Blazor Pro subscription](https://www.radzen.com/pricing) includes:
- **Radzen Blazor Studio** — standalone Blazor IDE that provides WYSIWYG design canvas, database scaffolding, CRUD wizards, app templates, and deployment to IIS/Azure
- **Radzen Blazor for Visual Studio** — Blazor tooling integrated into Visual Studio 2026
- **Premium themes** and theme customization tools
- **Dedicated support** with guaranteed 24-hour response time and priority fixes

## Run demos locally

Use **Radzen.Server.sln** to open and run demos as Blazor server application or **Radzen.WebAssembly.sln** to open and run demos as Blazor WebAssembly application. The demos require the .NET 10 SDK and should preferably be opened in VS2026.
