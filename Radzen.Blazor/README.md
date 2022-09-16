## Radzen Blazor is a set of 60+ free native Blazor UI controls packed with DataGrid, Scheduler, Charts and robust theming including Material design.

![Radzen Blazor Components](https://raw.githubusercontent.com/radzenhq/radzen-blazor/master/RadzenBlazorDemos/wwwroot/images/radzen-blazor-components.png)

## Why choose Radzen Blazor Components?

### :sparkles: Free

Radzen Blazor Components are open source and free for commercial use. You can install them from [nuget](https://www.nuget.org/packages/Radzen.Blazor) or build your own copy from source.

Paid support is available as part of the [Radzen Professional subscription](https://www.radzen.com/pricing/).

### :computer: Native

The components are implemented in C# and take full advantage of the Blazor framework. They do not depend on or wrap existing JavaScript frameworks or libraries.

Blazor Server and Blazor WebAssembly are fully supported.

### :seedling: Growing

We add new components and features on a regular basis.

Short development cycle. We release as soon as new stuff is available. No more quarterly releases.

## Support exceeding your expectations 

### :speech_balloon: Community Support
Everybody is welcome to visit the [Radzen Community forum](https://forum.radzen.com/). Join the growing community and participate in the discussions!

### :dart: Dedicated Support

The Radzen team monitors the forum threads, but does not guarantee a response to every question. For guaranteed responses you may consider the dedicated support option.

Dedicated support for the Radzen Blazor Components is available as part of the [Radzen Professional subscription](https://www.radzen.com/pricing/). 

Our flagship product [Radzen Studio](https://www.radzen.com/features/) provides tons of productivity features for Blazor developers:
- The first in the industry WYSIWYG Blazor design time canvas
- Scaffolding a complete CRUD applications from a database
- Built-in security - authentication and authorization
- Visual Studio Code and Professional support
- Deployment to IIS and Azure
- Dedicated support with 24 hour guaranteed response time

## Get started with Radzen Blazor Components

### 1. Install

Radzen Blazor Components are distributed as a [Radzen.Blazor nuget package](https://www.nuget.org/packages/Radzen.Blazor). You can add them to your project in one of the following ways
- Install the package from command line by running `dotnet add package Radzen.Blazor`
- Add the project from the Visual Nuget Package Manager 
- Manually edit the .csproj file and add a project reference

### 2. Import the namespace

Open the `_Imports.razor` file of your Blazor application and add this line `@using Radzen.Blazor`.

### 3. Include a theme

Open `Pages\_Layout.cshtml` (Blazor Server .NET 6+), `Pages\_Host.cshtml` (Blazor Server before .NET 6) or `wwwroot/index.html` (Blazor WebAssembly) and include a theme CSS file by adding this snippet 
```html
<link rel="stylesheet" href="_content/Radzen.Blazor/css/default.css">
```
If you either add Bootstrap manually or don't use it at all, include this instead:
```html
<link rel="stylesheet" href="_content/Radzen.Blazor/css/default-base.css">
```


### 4. Include Radzen.Blazor.js

Open `Pages\_Layout.cshtml` (Blazor Server .NET 6+), `Pages\_Host.cshtml` (Blazor Server before .NET 6) or `wwwroot/index.html` (Blazor WebAssembly) and include this snippet:

```html
<script src="_content/Radzen.Blazor/Radzen.Blazor.js"></script>
```

### 5. Use a component
Use any Radzen Blazor component by typing its tag name in a Blazor page e.g. 
```html
<RadzenButton Text="Hi"></RadzenButton>
```

#### Data-binding a property
```razor
<RadzenButton Text=@text />
<RadzenTextBox @bind-Value=@text />
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