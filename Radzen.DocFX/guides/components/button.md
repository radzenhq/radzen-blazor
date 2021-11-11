# Button component
This article demonstrates how to use RadzenButton.

## Basic usage
The most basic configuration of RadzenButton is to set its `Text` property and handle the `Click` event.
```
<RadzenButton Click=@OnSave Text="Save" />
@code {
    void OnSave()
    {
        // Implementation
    }
}
```
## Button types
RadzenButton can also submit or reset forms. Use the `ButtonType` property for that.
# [Submit Button](#tab/submit)
```
<RadzenButton ButtonType="ButtonType.Submit" />
```
# [Reset Button](#tab/reset)
```
<RadzenButton ButtonType="ButtonType.Reset" />
```
***
## Appearance
By default RadzenButton renders as a regular button with text. You can change the appearance in various ways.
### Icons and images
You can specify an icon or a custom image which RadzenButton will display before the text.
#### [Icon](#tab/icon)
```
<RadzenButton Icon="account_circle" />
```
#### [Image](#tab/image)
```
<RadzenButton Image="images/save.png" />
```
***
### Custom content
RadzenButton can also have entirely custom child content.
```
<RadzenButton>
    Some text
    <RadzenImage Path="images/radzen-nuget.png" Style="width:20px;margin-left: 10px;" />
</RadzenButton>
```
### Styles
RadzenButton comes with a predefined set of styles (background and text colors):
#### [Primary](#tab/primary)
```
<RadzenButton ButtonStyle="ButtonStyle.Primary" />
```
#### [Secondary](#tab/secondary)
```
<RadzenButton ButtonStyle="ButtonStyle.Secondary" />
```
#### [Light](#tab/light)
```
<RadzenButton ButtonStyle="ButtonStyle.Light" />
```
#### [Success](#tab/success)
```
<RadzenButton ButtonStyle="ButtonStyle.Success" />
```
#### [Danger](#tab/danger)
```
<RadzenButton ButtonStyle="ButtonStyle.Danger" />
```
#### [Warning](#tab/warning)
```
<RadzenButton ButtonStyle="ButtonStyle.Warning" />
```
#### [Info](#tab/info)
```
<RadzenButton ButtonStyle="ButtonStyle.Info" />
```
***
## Busy indicator
A common usage scanrio is to display busy (or loading) icon to show that a task is running. RadzenButton supports it out of the box
via the `IsBusy` property. Setting it to `true` displays the loading icon.
```
<RadzenButton IsBusy=@busy Click=@OnBusyClick Text="Save" />
@code {
    bool busy;

    async Task OnBusyClick()
    {
        busy = true;
        // Use await and Task.Delay to yield execution to Blazor and refresh the UI
        await Task.Delay(2000);
        // Set the busy flag to false after the long task is done
        busy = false;
    }
}
```