# Radzen Blazor Components examples

This is the source code of of the application behind [blazor.radzen.com](https://blazor.radzen.com)

## Commercial support

Paid support for the Radzen Blazor Components is available as part of the [Radzen Professional subscription](https://www.radzen.com/pricing/). 

Our flagship product Radzen provides tons of productivity features for Blazor developers:
- The first in the industry WYSIWYG Blazor design time canvas
- Scaffolding a complete CRUD applications from a database
- Built-in security - authentication and authorization
- Visual Studio Code and Professional support
- Deployment to IIS and Azure
- Dedicated support with 24 hour guaranteed response time
- Active community forum

## Get started with the Radzen Blazor Components

### Install

Radzen Blazor Components are distributed as the Radzen.Blazor nuget package. You can add them to your project in one of the following ways
- Install the package from command line by running dotnet add package Radzen.Blazor
- Add the project from the Visual Nuget Package Manager 
- Manually edit the .csproj file and add a project reference

### Import the namespace

Open the `_Imports.razor` file of your Blazor application and add this line `@using Radzen.Blazor`.

### Include a theme

Open the `_Host.cshtml` file (server-side Blazor) or `wwwroot/index.html` (client-side Blazor) and include a theme CSS file by adding this snippet `<link rel="stylesheet" href="_content/Radzen.Blazor/css/default.css">`

### Include Radzen.Blazor.js

Open the `_Host.cshtml` file (server-side Blazor) or `wwwroot/index.html` (client-side Blazor) and include this snippet `<script src="_content/Radzen.Blazor/Radzen.Blazor.js"></script>`

### Use a component
Use any Radzen Blazor component by typing its tag name in a Blazor page e.g. 
```html
<RadzenButton Text="Hi"></RadzenButton>
```

If you are using client-side Blazor also add the following code to your `.csproj` file (after the closing `RazorLangVersion` element):
```html
<BlazorLinkOnBuild>false</BlazorLinkOnBuild>
```
It is a workaround for a [known issue](https://github.com/mono/mono/issues/12917) when using IQueryable.

#### Data-binding a property
```razor
<RadzenButton Text="@text"></RadzenButton>
@code {
  string text = "Hi";
}
```

#### Handing events

```razor
<RadzenButton Click="@ButtonClicked" Text="Hi"></RadzenButton>
@code {
  void ButtonClicked()
  {

  }
}
```
