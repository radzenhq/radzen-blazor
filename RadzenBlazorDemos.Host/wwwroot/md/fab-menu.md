# FabMenu

The Blazor FAB Menu expands a floating action button into a menu of quick actions.

Keywords: fab, menu, button, floating, action

> API reference: [RadzenFabMenu API](https://blazor.radzen.com/api/fabmenu.md)

## Examples

## FAB Menu

The Blazor FAB Menu expands a floating action button into a menu of quick actions.

### Basic Usage

A FAB menu can contain 2–6 items, which should be closely related and grouped under a single primary action (e.g., Add).

```razor
<RadzenLayout style="position: relative; grid-template-areas: 'rz-header rz-header' 'rz-sidebar rz-body'">
    <RadzenHeader>
        <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="0">
            <RadzenSidebarToggle Click="@(() => sidebarExpanded = !sidebarExpanded)" />
            <RadzenLabel Text="Header" />
        </RadzenStack>
    </RadzenHeader>

    <!-- Place RadzenFabMenu component first or after the Header in RadzenLayout. -->
    <RadzenFabMenu Direction="Radzen.FabMenuDirection.Top">
        <RadzenFabMenuItem Text="Folder" Icon="folder" Click=@(args => OnClick("New Folder.")) />
        <RadzenFabMenuItem Text="Message" Icon="chat" Click=@(args => OnClick("New Message.")) />
        <RadzenFabMenuItem Text="Article" Icon="article" Click=@(args => OnClick("New Article.")) />
    </RadzenFabMenu>

    <RadzenSidebar Responsive="false" @bind-Expanded="@sidebarExpanded" style="position: absolute; z-index: 3">
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

    private void OnClick(string text)
    {
        NotificationService.Notify(new NotificationMessage { Severity = NotificationSeverity.Info, Summary = "RadzenFAB Clicked", Detail = text });
    }
}
```


### FAB menu with icon only buttons


```razor
<RadzenLayout style="position: relative; grid-template-areas: 'rz-header rz-header' 'rz-sidebar rz-body'">
    <RadzenHeader>
        <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="0">
            <RadzenSidebarToggle Click="@(() => sidebarExpanded = !sidebarExpanded)"/>
            <RadzenLabel Text="Header" />
        </RadzenStack>
    </RadzenHeader>

    <!-- Place RadzenFabMenu component first or after the Header in RadzenLayout. -->
    <RadzenFabMenu Direction="Radzen.FabMenuDirection.Top" ButtonStyle="ButtonStyle.Light" ToggleButtonStyle="ButtonStyle.Dark">
        <RadzenFabMenuItem ButtonStyle="ButtonStyle.Light" Icon="folder" Click=@(args => OnClick("Add Folder.")) />
        <RadzenFabMenuItem ButtonStyle="ButtonStyle.Light" Icon="chat" Click=@(args => OnClick("Add Message.")) />
        <RadzenFabMenuItem ButtonStyle="ButtonStyle.Light" Icon="article" Click=@(args => OnClick("Add Article.")) />
    </RadzenFabMenu>

    <RadzenSidebar Responsive="false" @bind-Expanded="@sidebarExpanded" style="position: absolute; z-index: 3">
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

    private void OnClick(string text)
    {
        NotificationService.Notify(new NotificationMessage { Severity = NotificationSeverity.Info, Summary = "FAB clicked", Detail = text });
    }
}
```


### Expand Direction

The `Direction` property of the RadzenFabMenu component controls the direction in which the menu items expand from the FAB. This property accepts values from the `FabMenuDirection` enum and determines both the visual layout and positioning of the menu items. By default, the menu items expand upward arranged vertically (bottom to top) and right-aligned with the FAB.
Choose appropriate direction: Top - Most common, good for primary actions Bottom - Good when FAB is at the top of the screen Left/Right - Good for horizontal layouts or when space is limited vertically Start/End - Use for international applications with RTL support

```razor
<RadzenLayout style="position: relative; grid-template-areas: 'rz-header rz-header' 'rz-sidebar rz-body'">
    <RadzenHeader>
        <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="0">
            <RadzenSidebarToggle Click="@(() => sidebarExpanded = !sidebarExpanded)" />
            <RadzenLabel Text="Header" />
        </RadzenStack>
    </RadzenHeader>

    <!-- Place RadzenFabMenu component first or after the Header in RadzenLayout. -->
    <RadzenFabMenu Direction="Radzen.FabMenuDirection.Start">
        <RadzenFabMenuItem Icon="folder" Click=@(args => OnClick("Add Folder.")) />
        <RadzenFabMenuItem Icon="chat" Click=@(args => OnClick("Add Message.")) />
        <RadzenFabMenuItem Icon="article" Click=@(args => OnClick("Add Article.")) />
    </RadzenFabMenu>

    <RadzenSidebar Responsive="false" @bind-Expanded="@sidebarExpanded" style="position: absolute; z-index: 3">
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

    private void OnClick(string text)
    {
        NotificationService.Notify(new NotificationMessage { Severity = NotificationSeverity.Info, Summary = "FAB clicked", Detail = text });
    }
}
```


### Accessibility

You can use the `AriaLabel` parameter in RadzenFabMenu components:

```razor
<RadzenLayout style="position: relative; grid-template-areas: 'rz-header rz-header' 'rz-sidebar rz-body'">
    <RadzenHeader>
        <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="0">
            <RadzenSidebarToggle Click="@(() => sidebarExpanded = !sidebarExpanded)" />
            <RadzenLabel Text="Header" />
        </RadzenStack>
    </RadzenHeader>

    <!-- Place RadzenFabMenu component first or after the Header in RadzenLayout. -->
    <RadzenFabMenu IsOpen="@menuOpen" 
                   IsOpenChanged="@OnMenuOpenChanged"
                   Icon="add" 
                   ToggleIcon="close"
                   AriaLabel="@(menuOpen ? "Close menu" : "Open menu")">
        <RadzenFabMenuItem Text="File" Icon="add" Click="@(args => OnMenuItemClick("New File"))" />
        <RadzenFabMenuItem Text="Upload" Icon="upload" Click="@(args => OnMenuItemClick("Upload"))" />
        <RadzenFabMenuItem Text="Settings" Icon="settings" Click="@(args => OnMenuItemClick("Settings"))" />
    </RadzenFabMenu>

    <RadzenSidebar Responsive="false" @bind-Expanded="@sidebarExpanded" style="position: absolute; z-index: 3">
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
    
    private bool menuOpen = false;

    private void ToggleMenu() => menuOpen = !menuOpen;

    private void OnMenuOpenChanged(bool isOpen)
    {
        menuOpen = isOpen;
        NotificationService.Notify(NotificationSeverity.Info, "Menu", 
            $"Menu is now {(isOpen ? "open" : "closed")}. Aria-label: \"{(isOpen ? "Close menu" : "Open menu")}\"");
    }

    private void OnMenuItemClick(string itemName)
    {
        NotificationService.Notify(NotificationSeverity.Success, "Menu Item Clicked", $"You clicked: {itemName}");
    }

}
```
