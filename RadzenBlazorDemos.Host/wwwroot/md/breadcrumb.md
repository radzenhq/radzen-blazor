# BreadCrumb

The Blazor BreadCrumb shows a navigation trail so users can see and jump back to their location in the app.

Keywords: breadcrumb, navigation, menu

> API reference: [RadzenBreadCrumb API](https://blazor.radzen.com/api/breadcrumb.md)

## Examples

## Blazor BreadCrumb

The Blazor BreadCrumb shows a navigation trail so users can see and jump back to their location in the app.

### Default Radzen BreadCrumb

Display a hierarchical navigation trail with clickable breadcrumb items showing the user's location.

```razor
<div class="rz-m-12">
    <RadzenBreadCrumb>
        <RadzenBreadCrumbItem Path="/" Text="Components" />
        <RadzenBreadCrumbItem Path="/badge" Text="Badge" />
        <RadzenBreadCrumbItem Icon="add" Text="Add" />
    </RadzenBreadCrumb>
</div>
```


### BreadCrumb with template

The optional Template can be defined using the `Template` Property of the `RadzenBreadCrumb` component. The Context is of Type `RadzenBreadCrumbItem`.

```razor
<div class="rz-m-12">
    <RadzenBreadCrumb>
        <Template Context="item">
            <RadzenBadge Text="@item.Text" IsPill="true" />
        </Template>
        <ChildContent>
            <RadzenBreadCrumbItem Path="/" Text="Components" />
            <RadzenBreadCrumbItem Path="/badge" Text="Badge" />
            <RadzenBreadCrumbItem Icon="add" Text="Add" />
        </ChildContent>
    </RadzenBreadCrumb>
</div>
```


### BreadCrumb with child content

Define custom child content for breadcrumb items to include icons, badges, or other elements.

```razor
<div class="rz-m-12">
    <RadzenBreadCrumb>
        <Template Context="item">
            <RadzenLink Path="@item.Path" Icon="@item.Icon" Text="@item.Text" />
            (Template)
        </Template>
        <ChildContent>
            <RadzenBreadCrumbItem Path="/" Text="Components" />
            <RadzenBreadCrumbItem>
                <RadzenLink Path="/badge" Icon="bolt" Text="Badge" />
                (Child Content)
            </RadzenBreadCrumbItem>
            <RadzenBreadCrumbItem Text="Add" />
        </ChildContent>
    </RadzenBreadCrumb>
</div>
```
