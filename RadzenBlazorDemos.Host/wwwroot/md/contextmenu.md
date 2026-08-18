# ContextMenu

The Blazor ContextMenu opens a right-click menu of actions anywhere in your app via ContextMenuService.

Keywords: popup, dropdown, menu

> API reference: [RadzenContextMenu API](https://blazor.radzen.com/api/contextmenu.md)

## Examples

## Blazor ContextMenu

The Blazor ContextMenu opens a right-click menu of actions anywhere in your app via ContextMenuService.
Use `ContextMenuService` to open and close context menus.

### Show ContextMenu with items

Create a context menu with menu items that appear when right-clicking on an element.

```razor
<RadzenStack Gap="1rem" class="rz-p-sm-12">
    <RadzenButton Text="Right click me" ContextMenu=@(args => ShowContextMenuWithItems(args)) ButtonStyle="ButtonStyle.Secondary" Size="ButtonSize.Large" />
    <EventConsole @ref=@console />
</RadzenStack>

@code {
    EventConsole console;

    void ShowContextMenuWithItems(MouseEventArgs args)
    {
        ContextMenuService.Open(args,
            new List<ContextMenuItem> {
                new ContextMenuItem(){ Text = "Context menu item 1", Value = 1, Icon = "home" },
                new ContextMenuItem(){ Text = "Context menu item 2", Value = 2, Icon = "search", Disabled = true },
                new ContextMenuItem(){ Text = "Context menu item 3", Value = 3, Icon = "info" },
         }, OnMenuItemClick);
    }

    void OnMenuItemClick(MenuItemEventArgs args)
    {
        console.Log($"Menu item with Value={args.Value} clicked");
        if(!args.Value.Equals(3) && !args.Value.Equals(4))
        {
            ContextMenuService.Close();
        }
    }
}
```


### Show ContextMenu with custom content and separator

Use custom templates and separators to create rich context menu experiences with icons, badges, and dividers.

```razor
<RadzenStack Gap="1rem" class="rz-p-sm-12">
    <RadzenButton Text="Right click me" ContextMenu=@(args => ShowContextMenuWithContent(args)) ButtonStyle="ButtonStyle.Secondary" Size="ButtonSize.Large" />
    <EventConsole @ref=@console />
</RadzenStack>

@code {
    EventConsole console;

    void ShowContextMenuWithContent(MouseEventArgs args) => ContextMenuService.Open(args, ds =>
        @<RadzenMenu Click="OnMenuItemClick">
            <RadzenMenuItem Text="Item1" Value="1"></RadzenMenuItem>
            <RadzenMenuItem Text="Item2" Value="2"></RadzenMenuItem>
            <hr />
            <RadzenMenuItem Text="More items" Value="3">
                <RadzenMenuItem Text="More sub items" Value="4">
                    <RadzenMenuItem Text="Item1" Value="5"></RadzenMenuItem>
                    <RadzenMenuItem Text="Item2" Value="6"></RadzenMenuItem>
                </RadzenMenuItem>
            </RadzenMenuItem>
        </RadzenMenu>);

    void OnMenuItemClick(MenuItemEventArgs args)
    {
        console.Log($"Menu item with Value={args.Value} clicked");
        if(!args.Value.Equals(3) && !args.Value.Equals(4))
        {
            ContextMenuService.Close();
        }
    }
}
```


### Show ContextMenu for HTML element

Attach a context menu to any HTML element using the `ContextMenuService` and element reference.

```razor
<RadzenStack Gap="1rem" class="rz-p-sm-12">
    <button @oncontextmenu=@(args => ShowContextMenuWithContent(args)) @oncontextmenu:preventDefault="true">
        Right click me
    </button>
    <EventConsole @ref=@console />
</RadzenStack>

@code {
    EventConsole console;

    void ShowContextMenuWithContent(MouseEventArgs args) => ContextMenuService.Open(args, ds =>
        @<RadzenMenu Click="OnMenuItemClick">
            <RadzenMenuItem Text="Item1" Value="1"></RadzenMenuItem>
            <RadzenMenuItem Text="Item2" Value="2"></RadzenMenuItem>
            <RadzenMenuItem Text="More items" Value="3">
                <RadzenMenuItem Text="More sub items" Value="4">
                    <RadzenMenuItem Text="Item1" Value="5"></RadzenMenuItem>
                    <RadzenMenuItem Text="Item2" Value="6"></RadzenMenuItem>
                </RadzenMenuItem>
            </RadzenMenuItem>
        </RadzenMenu>);

    void OnMenuItemClick(MenuItemEventArgs args)
    {
        console.Log($"Menu item with Value={args.Value} clicked");
        if(!args.Value.Equals(3) && !args.Value.Equals(4))
        {
            ContextMenuService.Close();
        }
    }
}
```
