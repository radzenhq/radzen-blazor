# ProfileMenu

The Blazor ProfileMenu shows a user avatar with a dropdown of account and navigation actions.

Keywords: navigation, dropdown, menu

> API reference: [RadzenProfileMenu API](https://blazor.radzen.com/api/profilemenu.md)

## Examples

## Blazor ProfileMenu

The Blazor ProfileMenu shows a user avatar with a dropdown of account and navigation actions.

```razor
<div class="rz-p-12 rz-text-align-center" style="margin-bottom: 200px;">
    <RadzenProfileMenu>
        <Template>
            <RadzenGravatar Email="user@example.com">
            </RadzenGravatar>
        </Template>
        <ChildContent>
            <RadzenProfileMenuItem Text="Buttons" Path="buttons" Icon="account_circle"></RadzenProfileMenuItem>
            <RadzenProfileMenuItem Text="Menu" Path="menu" Icon="line_weight"></RadzenProfileMenuItem>
            <RadzenProfileMenuItem Text="FileInput" Path="fileinput" Icon="attach_file"></RadzenProfileMenuItem>
            <RadzenProfileMenuItem Text="Dialog" Path="dialog" Icon="perm_media"></RadzenProfileMenuItem>
            <RadzenProfileMenuItem Text="Notification" Path="notification" Icon="announcement"></RadzenProfileMenuItem>
        </ChildContent>
    </RadzenProfileMenu>
</div>
```
