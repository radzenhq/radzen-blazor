# Fab

The Blazor FAB (floating action button) highlights your app's primary action with a circular, elevated button.

Keywords: fab, button, floating, action

> API reference: [RadzenFab API](https://blazor.radzen.com/api/fab.md)

## Examples

## FAB - Floating Action Button

The Blazor FAB (floating action button) highlights your app's primary action with a circular, elevated button.

### Basic Usage


```razor
<RadzenLayout style="position: relative; grid-template-areas: 'rz-sidebar rz-header' 'rz-sidebar rz-body'">
    <RadzenHeader>
        <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="0">
            <RadzenSidebarToggle Click="@(() => sidebarExpanded = !sidebarExpanded)" />
            <RadzenLabel Text="Header" />
        </RadzenStack>
    </RadzenHeader>

    <!-- Place RadzenFab component first or after the Header in RadzenLayout. -->
    <RadzenFab Icon="add" Click=@(args => OnClick("FAB has just been clicked.")) />

    <RadzenSidebar Responsive="false" @bind-Expanded="@sidebarExpanded" style="position: absolute; z-index: 3">
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

    private void OnClick(string text)
    {
        NotificationService.Notify(new NotificationMessage { Severity = NotificationSeverity.Info, Summary = "RadzenFAB Clicked", Detail = text });
    }
}
```


### FAB Position

Use custom CSS properties to position the button on the screen. Feel free to change the position as you see fit e.g. `Style="position: fixed;"`.

```razor
<RadzenLayout style="position: relative; grid-template-areas: 'rz-header rz-header' 'rz-sidebar rz-body'">
    <RadzenHeader>
        <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="0">
            <RadzenSidebarToggle Click="@(() => sidebarExpanded = !sidebarExpanded)" />
            <RadzenLabel Text="Header" />
        </RadzenStack>
    </RadzenHeader>

    <!-- Place RadzenFab component first or after the Header in RadzenLayout. -->
    <RadzenFab Style="--rz-fab-inset-block-start: 2rem; --rz-fab-inset-block-end: auto;" Icon="add" ButtonStyle="ButtonStyle.Secondary" Click=@(args => OnClick("FAB has just been clicked.")) />

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


### Multiple FABs

Two FABs may be used when both actions are distinct and equally important.

```razor
<RadzenLayout style="position: relative; grid-template-areas: 'rz-header rz-header' 'rz-sidebar rz-body'">
    <RadzenHeader>
        <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="0">
            <RadzenSidebarToggle Click="@(() => sidebarExpanded = !sidebarExpanded)" />
            <RadzenLabel Text="Header" />
        </RadzenStack>
    </RadzenHeader>

    <!-- Place RadzenFab component first or after the Header in RadzenLayout. -->
    <RadzenFab Style="--rz-fab-inset-block-start: 2rem; --rz-fab-inset-block-end: auto;" Icon="add" ButtonStyle="ButtonStyle.Secondary" Click=@(args => OnClick("FAB has just been clicked.")) />

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


### Busy FAB

Use `IsBusy="true"` to show the busy indicator.

```razor
<RadzenLayout style="position: relative; grid-template-areas: 'rz-header rz-header' 'rz-sidebar rz-body'">
    <RadzenHeader>
        <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="0">
            <RadzenSidebarToggle Click="@(() => sidebarExpanded = !sidebarExpanded)" />
            <RadzenLabel Text="Header" />
        </RadzenStack>
    </RadzenHeader>

    <!-- Place RadzenFab component first or after the Header in RadzenLayout. -->
    <RadzenFab Icon="add" Click=@OnBusyClick IsBusy=@busy />

    <RadzenSidebar Responsive="false" @bind-Expanded="@sidebarExpanded">
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
    bool busy;

    async Task OnBusyClick()
    {
        busy = true;
        await Task.Delay(2000);
        busy = false;
    }
}
```
