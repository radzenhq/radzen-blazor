# Dialog component
This article demonstrates how to use the Dialog component. Use `DialogService` to open and close dialogs. 

# Show inline dialog with custom content

```
@inject DialogService DialogService

<RadzenButton Text="Show dialog with inline Blazor content" Click=@ShowInlineDialog />

@code {
    async Task ShowInlineDialog()
    {
     var result = await DialogService.OpenAsync("Simple Dialog", ds =>
        @<div>
            <p Style="margin-bottom: 1rem">Confirm?</p>
            <div class="row">
                <div class="col-md-12">
                    <RadzenButton Text="Ok" Click="() => ds.Close(true)" Style="margin-bottom: 10px; width: 150px" />
                    <RadzenButton Text="Cancel" Click="() => ds.Close(false)" ButtonStyle="ButtonStyle.Secondary"  Style="margin-bottom: 10px; width: 150px"/>
                    <RadzenButton Text="Refresh" Click="(() => { orderID = 10249; ds.Refresh(); })" ButtonStyle="ButtonStyle.Info"  Style="margin-bottom: 10px; width: 150px"/>
                    Order ID: @orderID
                </div>
            </div>
        </div>);
    
      Console.WriteLine($"Dialog result: {result}");
    }
}
```

# Show component/page as dialog
Use `DialogOptions` to specify various dialog properties and provide parameters as `Dictionary<string, object>`.

```
@inject DialogService DialogService

<RadzenButton Text=@($"Show OrderID: {orderID} details") Click=@OpenOrder />

public async Task OpenOrder()
{
    await DialogService.OpenAsync<DialogCardPage>($"Order {orderID}",
            new Dictionary<string, object>() { { "OrderID", orderID } },
            new DialogOptions() { Width = "700px", Height = "530px", Resizable = true, Draggable = true });
}
```

# Show confirm dialog

```
@inject DialogService DialogService

<RadzenButton Text="Show confirm dialog" Click=@(args => DialogService.Confirm("Are you sure?", "MyTitle", new ConfirmOptions() { OkButtonText = "Yes", CancelButtonText = "No" })) />
```

# Show busy dialog

```
@inject DialogService DialogService

<RadzenButton Text="Show busy dialog with string" Click=@(args => ShowBusyDialog(true)) />
<RadzenButton Text="Show busy dialog with markup" Click=@(args => ShowBusyDialog(false)) />

async Task ShowBusyDialog(bool withMessageAsString)
{
    InvokeAsync(async () =>
    {
        // Simulate background task
        await Task.Delay(2000);

        // Close the dialog
        DialogService.Close();
    });

    if (withMessageAsString)
    {
        await BusyDialog("Busy ...");
    }
    else
    {
        await BusyDialog();
    }
}

async Task BusyDialog()
{
    await DialogService.OpenAsync("", ds =>
        @<div>
            <div class="row">
                <div class="col-md-12">
                    Loading...
                </div>
            </div>
    </div>, new DialogOptions() { ShowTitle = false, Style = "min-height:auto;min-width:auto;width:auto" });
}
```

# Show side dialog

The `DialogService` offers the possibility to open a dialog on the side, instead of the screen center.

```
@inject DialogService DialogService

<RadzenButton Text="Show side dialog" Click=@ShowDialog />

@code {
    async Task ShowInlineDialog()
    {
     var result = await DialogService.OpenSideAsync<DialogCardPage>("Side Dialog", new SideDialogOptions
     {
        Position = DialogPosition.Right
     });
    
      Console.WriteLine($"Dialog result: {result}");
    }
}
```

The `SideDialogOptions` class can be used to modify the dialog behavior, by e.g. settings the render position to the left or right side of the screen. The Sie Dialog can be positioned on each side of the screen by setting the `SideDialogOptions.Position` property when opening.

In opposite to the default dialog there can only be one side dialog open at the same time. Opening a second side dialog will close the first opened dialog with a null result!