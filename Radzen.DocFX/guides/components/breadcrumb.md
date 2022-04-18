# Badge component
This article demonstrates how to use the BreadCrumb component. 

The Bread Crumb offers a Menu like experience, with an optional custom Template.

## Standard Bread Crumb Menu
```
<RadzenBreadCrumb>
    <Items>
        <RadzenBreadCrumbItem Text="Layout & Navigation" />
        <RadzenBreadCrumbItem Text="Bread Crumb" />
    </Items>
</RadzenBreadCrumb>
```

## Optional Template
The optional Template can be defined using the `Template` Property of the `RadzenBreadCrumb` component.
The Context ist of Type `RadzenBreadCrumbItem`.
```
<RadzenBreadCrumb>
    <Template context="itm">
        <RadzenBadge Text="@itm.Text" />
    </Template>
    <Items>
        <RadzenBreadCrumbItem Text="Layout & Navigation" />
        <RadzenBreadCrumbItem Text="Bread Crumb" />
    </Items>
</RadzenBreadCrumb>
```
This template renders all items of the Menu in a `RadzenBadge`.