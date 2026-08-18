# Layout

The Blazor Layout arranges a page into header, sidebar, body, and footer regions, with a collapsible sidebar.

Keywords: layout, sidebar, drawer, header, body, footer

> API reference: [RadzenLayout API](https://blazor.radzen.com/api/layout.md)

## Examples

## Blazor Layout

The Blazor Layout arranges a page into header, sidebar, body, and footer regions, with a collapsible sidebar.

### Sidebar, Header and Footer

Create a standard layout with sidebar navigation, header, and footer sections using `RadzenLayout` components.

```razor
<RadzenLayout>
    <RadzenHeader>
        <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="0">
            <RadzenSidebarToggle Click="@(() => sidebar1Expanded = !sidebar1Expanded)" />
            <RadzenLabel Text="Header" />
        </RadzenStack>
    </RadzenHeader>
    <RadzenSidebar @bind-Expanded="@sidebar1Expanded">
        <RadzenPanelMenu>
            <RadzenPanelMenuItem Text="Home" Icon="home" />
            <RadzenPanelMenuItem Text="Users" Icon="account_box" />
        </RadzenPanelMenu>
        <div class="rz-p-4">
            Sidebar
        </div>
    </RadzenSidebar>
    <RadzenBody>
        <div class="rz-p-4">
            Body
        </div>
    </RadzenBody>
    <RadzenFooter>
        Footer
    </RadzenFooter>
</RadzenLayout>

@code {
    bool sidebar1Expanded = true;
}
```


### Full height Sidebar

Create a sidebar that spans the full height of the viewport, sitting alongside the header and content.

```razor
<RadzenLayout>
    <RadzenHeader>
        <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="0">
            <RadzenSidebarToggle Click="@(() => sidebarExpanded = !sidebarExpanded)" />
            <RadzenLabel Text="Header" />
        </RadzenStack>
    </RadzenHeader>
    <RadzenSidebar FullHeight="true" @bind-Expanded="@sidebarExpanded">
        <RadzenPanelMenu>
            <RadzenPanelMenuItem Text="Home" Icon="home" />
            <RadzenPanelMenuItem Text="Users" Icon="account_box" />
        </RadzenPanelMenu>
        <div class="rz-p-4">
            Sidebar
        </div>
    </RadzenSidebar>
    <RadzenBody>
        <div class="rz-p-4">
            Body
        </div>
    </RadzenBody>
</RadzenLayout>

@code {
    bool sidebarExpanded = true;
}
```


### Overlay Sidebar

Create a sidebar that overlays the content when opened, perfect for responsive mobile layouts.

```razor
<RadzenLayout style="position: relative">
    <RadzenHeader>
        <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="0">
            <RadzenSidebarToggle Click="@(() => sidebarExpanded = !sidebarExpanded)" />
            <RadzenLabel Text="Header" />
        </RadzenStack>
    </RadzenHeader>
    <RadzenSidebar Responsive="false" @bind-Expanded="@sidebarExpanded" style="position: absolute">
        <RadzenPanelMenu>
            <RadzenPanelMenuItem Text="Home" Icon="home" />
            <RadzenPanelMenuItem Text="Users" Icon="account_box" />
        </RadzenPanelMenu>
        <div class="rz-p-4">
            Sidebar
        </div>
    </RadzenSidebar>
    <RadzenBody>
        <div class="rz-p-4">
            Body
        </div>
    </RadzenBody>
    <RadzenFooter>
        Footer
    </RadzenFooter>
</RadzenLayout>

@code {
    bool sidebarExpanded = false;
}
```


### Full height overlay Sidebar

Create a full-height overlay sidebar that covers the entire viewport height when opened.

```razor
<RadzenLayout style="position: relative;">
    <RadzenHeader>
        <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="0">
            <RadzenSidebarToggle Click="@(() => sidebarExpanded = !sidebarExpanded)" />
            <RadzenLabel Text="Header" />
        </RadzenStack>
    </RadzenHeader>
    <RadzenSidebar FullHeight="true" Responsive="false" @bind-Expanded="@sidebarExpanded" style="position: absolute; z-index: 3">
        <RadzenStack AlignItems="AlignItems.End" class="rz-p-2">
            <RadzenButton Icon="west" Variant="Variant.Text" ButtonStyle="ButtonStyle.Secondary" Click="@(() => sidebarExpanded = false)" />
        </RadzenStack>
        <RadzenPanelMenu>
            <RadzenPanelMenuItem Text="Home" Icon="home" />
            <RadzenPanelMenuItem Text="Users" Icon="account_box" />
        </RadzenPanelMenu>
        <div class="rz-p-4">
            Sidebar
        </div>
    </RadzenSidebar>
    <RadzenBody>
        <div class="rz-p-4">
            Body
        </div>
    </RadzenBody>
    @if (sidebarExpanded)
    {
    <div @onclick="@(() => sidebarExpanded = false)" class="rz-dialog-mask" style="position: absolute; z-index: 2"></div>
    }
</RadzenLayout>

@code {
    bool sidebarExpanded = false;
}
```


### Right Sidebar

Use `Position="SidebarPosition.Right"` if you need to explicitly position the sidebar to the right, or `Position="SidebarPosition.End"` if you need to take into account RTL support.

```razor
<RadzenLayout>
    <RadzenHeader>
        <RadzenStack Orientation="Orientation.Horizontal" JustifyContent="JustifyContent.End" AlignItems="AlignItems.Center" Gap="1rem">
            <RadzenLabel Text="Header" />
            <RadzenSidebarToggle Click="@(() => sidebarExpanded = !sidebarExpanded)" class="rz-m-0" />
        </RadzenStack>
    </RadzenHeader>
    <RadzenSidebar Position="SidebarPosition.End" @bind-Expanded="@sidebarExpanded">
        <RadzenPanelMenu>
            <RadzenPanelMenuItem Text="Home" Icon="home" />
            <RadzenPanelMenuItem Text="Users" Icon="account_box" />
        </RadzenPanelMenu>
        <div class="rz-p-4">
            Sidebar
        </div>
    </RadzenSidebar>
    <RadzenBody>
        <div class="rz-p-4">
            Body
        </div>
    </RadzenBody>
</RadzenLayout>

@code {
    bool sidebarExpanded = true;
}
```


### Right full height Sidebar

Position a full-height sidebar on the right side of the layout.

```razor
<RadzenLayout>
    <RadzenHeader>
        <RadzenStack Orientation="Orientation.Horizontal" JustifyContent="JustifyContent.End" AlignItems="AlignItems.Center" Gap="1rem">
            <RadzenLabel Text="Header" />
            <RadzenSidebarToggle Click="@(() => sidebarExpanded = !sidebarExpanded)" class="rz-m-0" />
        </RadzenStack>
    </RadzenHeader>
    <RadzenSidebar Position="SidebarPosition.End" FullHeight="true" @bind-Expanded="@sidebarExpanded">
        <RadzenPanelMenu>
            <RadzenPanelMenuItem Text="Home" Icon="home" />
            <RadzenPanelMenuItem Text="Users" Icon="account_box" />
        </RadzenPanelMenu>
        <div class="rz-p-4">
            Sidebar
        </div>
    </RadzenSidebar>
    <RadzenBody>
        <div class="rz-p-4">
            Body
        </div>
    </RadzenBody>
</RadzenLayout>

@code {
    bool sidebarExpanded = true;
}
```


### Right and Left Sidebar

Use `Position="SidebarPosition.Right"` and `Position="SidebarPosition.Left"` if you need to explicitly position the sidebar to the right or to the left, without taking into account LTR or RTL direction.

```razor
<RadzenLayout>
    <RadzenHeader>
        <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="0">
            <RadzenSidebarToggle Click="@(() => leftSidebarExpanded = !leftSidebarExpanded)" />
            <RadzenLabel Text="Header" />
        </RadzenStack>
    </RadzenHeader>
    <RadzenSidebar Position="SidebarPosition.Left" @bind-Expanded="@leftSidebarExpanded">
        <RadzenPanelMenu>
            <RadzenPanelMenuItem Text="Home" Icon="home" />
            <RadzenPanelMenuItem Text="Users" Icon="account_box" />
        </RadzenPanelMenu>
        <div class="rz-p-4">
            Left Sidebar
        </div>
    </RadzenSidebar>
    <RadzenBody>
        <div class="rz-p-4">
            Body
        </div>
        <div class="rz-p-4">
            <RadzenButton Text="Toggle left Sidebar" Click="@(() => leftSidebarExpanded = !leftSidebarExpanded)" />
            <RadzenButton Text="Toggle right Sidebar" Click="@(() => rightSidebarExpanded = !rightSidebarExpanded)" />
        </div>
    </RadzenBody>
    <RadzenSidebar Position="SidebarPosition.Right" @bind-Expanded="@rightSidebarExpanded" Style="width: 300px;">
        <div class="rz-p-4">
            Right Sidebar
        </div>
    </RadzenSidebar>
</RadzenLayout>

@code {
    bool leftSidebarExpanded = true;
    bool rightSidebarExpanded = true;
}
```


### Start and End Sidebar

Use `Position="SidebarPosition.Start"` or `Position="SidebarPosition.End"` for international applications with RTL support.

```razor
<RadzenLayout>
    <RadzenHeader>
        <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="0">
            <RadzenSidebarToggle Click="@(() => startSidebarExpanded = !startSidebarExpanded)" />
            <RadzenLabel Text="Header" />
        </RadzenStack>
    </RadzenHeader>
    <RadzenSidebar Position="SidebarPosition.Start" @bind-Expanded="@startSidebarExpanded">
        <RadzenPanelMenu>
            <RadzenPanelMenuItem Text="Home" Icon="home" />
            <RadzenPanelMenuItem Text="Users" Icon="account_box" />
        </RadzenPanelMenu>
        <div class="rz-p-4">
            Start Sidebar
        </div>
    </RadzenSidebar>
    <RadzenBody>
        <div class="rz-p-4">
            Body
        </div>
        <div class="rz-p-4">
            <RadzenButton Text="Toggle start Sidebar" Click="@(() => startSidebarExpanded = !startSidebarExpanded)" />
            <RadzenButton Text="Toggle end Sidebar" Click="@(() => endSidebarExpanded = !endSidebarExpanded)" />
        </div>
    </RadzenBody>
    <RadzenSidebar Position="SidebarPosition.End" @bind-Expanded="@endSidebarExpanded" Style="width: 300px;">
        <div class="rz-p-4">
            End Sidebar
        </div>
    </RadzenSidebar>
</RadzenLayout>

@code {
    bool startSidebarExpanded = true;
    bool endSidebarExpanded = true;
}
```


### Icon Sidebar

Create a compact icon-only sidebar that expands to show labels on hover or click.

```razor
<RadzenLayout>
    <RadzenHeader>
        <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="0">
            <RadzenSidebarToggle Click="@(() => sidebarExpanded = !sidebarExpanded)" />
            <RadzenLabel Text="Header" />
        </RadzenStack>
    </RadzenHeader>
    <RadzenSidebar Responsive="false" Style="width: max-content">
        <RadzenPanelMenu DisplayStyle="@(sidebarExpanded ? MenuItemDisplayStyle.IconAndText : MenuItemDisplayStyle.Icon)" ShowArrow="false">
            <RadzenPanelMenuItem Text="Overview" Icon="home" />
            <RadzenPanelMenuItem Text="Dashboard" Icon="dashboard" />
            <RadzenPanelMenuItem Text="UI Fundamentals" Icon="auto_awesome">
                <RadzenPanelMenuItem Text="Themes" Icon="color_lens" />
                <RadzenPanelMenuItem Text="Colors" Icon="invert_colors" />
            </RadzenPanelMenuItem>
        </RadzenPanelMenu>
    </RadzenSidebar>
    <RadzenBody>
        <div class="rz-p-4">
            Body
        </div>
    </RadzenBody>
    <RadzenFooter>
        Footer
    </RadzenFooter>
</RadzenLayout>

@code {
    bool sidebarExpanded = true;
}
```
