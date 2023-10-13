# Installation
This article shows you how to add Radzen Blazor components to your project.

1. [Install the Nuget package](#install-the-nuget-package)
1. [Import the namespaces](#import-the-namespaces)
1. [Include the CSS and JS](#include-the-css-and-js)

> [!NOTE]
> If you want to use the Radzen.Blazor components in Blazor .NET 8 static rendering mode (SSR) you should be aware that:
> - Static rendering mode <a target="_blank" href="https://learn.microsoft.com/en-us/aspnet/core/blazor/components/render-modes?view=aspnetcore-8.0#render-modes">does not support</a> events and all component interactivity will not be available (e.g. opening a dropdown, sorting a datagrid, opening a dialog).
> - Applications created using the Blazor 8 template use static rendering mode by default and only some pages (Counter.razor) have interactivity enabled.
> - The layout is not interactive by default (MainLayout.razor). Blazor components added to it will not be interactive as a result.
> - You can <a href="https://learn.microsoft.com/en-us/aspnet/core/blazor/components/render-modes?view=aspnetcore-8.0#apply-a-render-mode-to-a-component-instance" target="_blank">enable interactivity per component basis</a> via the `@rendermode` attribute e.g. `<RadzenDialog @rendermode="@RenderMode.InteractiveServer" />`.
> - Components that have child content cannot use that approach. You will need a <a href="https://learn.microsoft.com/en-us/aspnet/core/blazor/components/render-modes?view=aspnetcore-8.0#child-component-with-a-serializable-parameter" target="_blank">wrapper component</a> with render mode set as an attribute.> - You can <a href="https://learn.microsoft.com/en-us/aspnet/core/blazor/components/render-modes?view=aspnetcore-8.0#set-the-render-mode-for-the-entire-app" target="_blank">enable interactivity</a> for the entire application.

## Install the Nuget package
Radzen Blazor Components are distributed as the [Radzen.Blazor](https://www.nuget.org/packages/Radzen.Blazor) nuget package.

You can add them to your project in one of the following ways
- Install the package from command line by running `dotnet add package Radzen.Blazor`.
- Add the project from the Visual Nuget Package Manager <img class="ml-0" src="../../../images/nuget-explorer.png">
- Manually edit the .csproj file and add a package reference `<PackageReference Include="Radzen.Blazor" Version="*" />`

## Import the namespaces
Open the `_Imports.razor` file of your Blazor application and add these two lines:

```
@using Radzen
@using Radzen.Blazor
```

## Include the CSS and JS

### Include CSS

Radzen Blazor components come with five free themes: Material, Standard, Default, Dark, Software and Humanistic.

To use a theme
1. Pick a theme. The [online demos](https://blazor.radzen.com/colors) allow you to preview the available options via the theme dropdown located in the header. The Material theme is currently selected by default.
1. Include the theme CSS file in your Blazor application. Open `Pages\_Layout.cshtml` (Blazor Server .NET 6), `Pages\_Host.cshtml` (Blazor Server .NET 7), `wwwroot/index.html` (Blazor WebAssembly) or `Components\App.razor` (Blazor .NET 8) and include the a theme CSS file by adding this snippet
   ```html
   <link rel="stylesheet" href="_content/Radzen.Blazor/css/material-base.css">
   ```

To include a different theme (i.e. Standard) just change the name of the CSS file:
```
<link rel="stylesheet" href="_content/Radzen.Blazor/css/standard-base.css">
```

### Include the JS
Open `Pages\_Layout.cshtml` (Blazor Server .NET 6), `Pages\_Host.cshtml` (Blazor Server .NET 7), `wwwroot/index.html` (Blazor WebAssembly) or
`Components\App.razor` (Blazor .NET 8) and include this snippet:
```
<script src="_content/Radzen.Blazor/Radzen.Blazor.js"></script>
```

## Next steps

You are now ready to use Radzen Blazor components! Check [Using a component](use-component.md) to see how to add a Radzen Blazor component to your page.

Some components require additional setup:

- [Context menu](context-menu.md)
- [Dialog](dialog.md)
- [Notification](context-menu.md)
- [Tooltip](tooltip.md)
