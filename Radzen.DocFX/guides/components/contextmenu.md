# ContextMenu component
This article demonstrates how to use the ContextMenu component. Use `ContextMenuService` to open and close context menus.

## Show ContextMenu with items

```
@inject ContextMenuService ContextMenuService

<RadzenButton Text="Show context menu" ContextMenu=@(args => ShowContextMenuWithItems(args)) />

@code {
    void ShowContextMenuWithItems(MouseEventArgs args)
    {
        ContextMenuService.Open(args,
            new List<ContextMenuItem> {
                new ContextMenuItem(){ Text = "Context menu item 1", Value = 1 },
                new ContextMenuItem(){ Text = "Context menu item 2", Value = 2 },
                new ContextMenuItem(){ Text = "Context menu item 3", Value = 3 },
         }, OnMenuItemClick);
    }

    void OnMenuItemClick(MenuItemEventArgs args)
    {
        Console.WriteLine($"Menu item with Value={args.Value} clicked");
    }
}
```

## Show ContextMenu with custom content

```
@inject ContextMenuService ContextMenuService

<RadzenButton Text="Show context menu" ContextMenu=@(args => ShowContextMenuWithContent(args)) />

@code {
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
        Console.WriteLine($"Menu item with Value={args.Value} clicked");
    }
}
```

## Show ContextMenu for HTML element

```
@inject ContextMenuService ContextMenuService

<button @oncontextmenu=@(args => ShowContextMenuWithContent(args)) @oncontextmenu:preventDefault="true">
  Show context menu
</button>

@code {
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
        Console.WriteLine($"Menu item with Value={args.Value} clicked");
    }
}
```