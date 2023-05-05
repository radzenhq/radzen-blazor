# Installation
This article shows you how to add Radzen Blazor components to your project.

1. [Install the Nuget package](#install-the-nuget-package)
1. [Import the namespaces](#import-the-namespaces)
1. [Include the CSS and JS](#include-the-css-and-js)

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
1. Include the theme CSS file in your Blazor application. Open `Pages\_Layout.cshtml` (Blazor Server .NET 6), `Pages\_Host.cshtml` (Blazor Server .NET 7) or `wwwroot/index.html` (Blazor WebAssembly) and include the a theme CSS file by adding this snippet
   ```html
   <link rel="stylesheet" href="_content/Radzen.Blazor/css/material-base.css">
   ```

To include a different theme (i.e. Standard) just change the name of the CSS file:
```
<link rel="stylesheet" href="_content/Radzen.Blazor/css/standard-base.css">
```

### Include the JS
Open `Pages\_Layout.cshtml` (Blazor Server .NET 6), `Pages\_Host.cshtml` (Blazor Server .NET 7) or `wwwroot/index.html` (Blazor WebAssembly) and include this snippet:
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
