# Dialog

The Blazor Dialog opens modal dialogs and side panels from code via DialogService, with Alert and Confirm helpers, custom content, sizing, and async results.

Keywords: popup, window

> API reference: [RadzenDialog API](https://blazor.radzen.com/api/dialog.md)

## Examples

## Blazor Dialog

The Blazor Dialog opens modal dialogs and side panels from code via DialogService, with Alert and Confirm helpers, custom content, sizing, and async results.

### Open page as a dialog

Use `DialogService` to open any Blazor component or page as a modal dialog.

```razor
<div class="rz-p-12 rz-text-align-center">
    <RadzenButton Text=@($"Order {orderID} details") ButtonStyle="ButtonStyle.Secondary" Click=@OpenOrder />
</div>

@code {
    int orderID = 10248;

    public async Task OpenOrder()
    {
        await LoadStateAsync();

        await DialogService.OpenAsync<DialogCardPage>($"Order {orderID}",
               new Dictionary<string, object>() { { "OrderID", orderID } },
               new DialogOptions() 
               {
                   Resizable = true, 
                   Draggable = true,
                   Resize = OnResize,
                   Drag = OnDrag,
                   Width = Settings != null ? Settings.Width : "700px", 
                   Height = Settings != null ? Settings.Height : "512px",
                   Left = Settings != null ? Settings.Left : null, 
                   Top = Settings != null ? Settings.Top : null
                });

        await SaveStateAsync();
    }

    void OnDrag(System.Drawing.Point point)
    {
        JSRuntime.InvokeVoidAsync("console.log", $"Dialog drag. Left:{point.X}, Top:{point.Y}");

        if(Settings == null)
        {
            Settings = new DialogSettings();
        }

        Settings.Left = $"{point.X}px";
        Settings.Top = $"{point.Y}px";

        InvokeAsync(SaveStateAsync);
    }

    void OnResize(System.Drawing.Size size)
    {
        JSRuntime.InvokeVoidAsync("console.log", $"Dialog resize. Width:{size.Width}, Height:{size.Height}");

        if(Settings == null)
        {
            Settings = new DialogSettings();
        }

        Settings.Width = $"{size.Width}px";
        Settings.Height = $"{size.Height}px";

        InvokeAsync(SaveStateAsync);
    }

    DialogSettings _settings;
    public DialogSettings Settings 
    { 
        get
        {
            return _settings;
        }
        set
        {
            if (_settings != value)
            {
                _settings = value;
                InvokeAsync(SaveStateAsync);
            }
        }
    }

    private async Task LoadStateAsync()
    {
        await Task.CompletedTask;

        var result = await JSRuntime.InvokeAsync<string>("window.localStorage.getItem", "DialogSettings");
        if (!string.IsNullOrEmpty(result))
        {
            _settings = JsonSerializer.Deserialize<DialogSettings>(result);
        }
    }

    private async Task SaveStateAsync()
    {
        await Task.CompletedTask;

        await JSRuntime.InvokeVoidAsync("window.localStorage.setItem", "DialogSettings", JsonSerializer.Serialize<DialogSettings>(Settings));
    }

    public class DialogSettings
    {
        public string Left { get; set; }
        public string Top { get; set; }
        public string Width { get; set; }
        public string Height { get; set; }
    }
}
```


### Inline Dialog

Display a dialog inline within the page content without using the DialogService.

```razor
<div class="rz-p-12 rz-text-align-center">
    <RadzenButton Text="Dialog with inline Blazor content" ButtonStyle="ButtonStyle.Secondary" Click=@ShowInlineDialog />
</div>

@code {
    int orderID = 10248;

    async Task ShowInlineDialog()
    {
        var result = await DialogService.OpenAsync("Simple Dialog", ds =>
        @<RadzenStack Gap="1.5rem">
            <p>Confirm Order ID <b>@orderID</b>?</p>
            <RadzenStack Orientation="Orientation.Horizontal" Gap="0.5rem" AlignItems="AlignItems.Center" JustifyContent="JustifyContent.SpaceBetween">
                <RadzenStack Orientation="Orientation.Horizontal">
                    <RadzenButton Text="Ok" Click="() => ds.Close(true)" Style="width: 80px;" />
                    <RadzenButton Text="Cancel" Click="() => ds.Close(false)" ButtonStyle="ButtonStyle.Light" />
                </RadzenStack>
                <RadzenButton Text="Refresh" Click="(() => { orderID = 10249; ds.Refresh(); })" ButtonStyle="ButtonStyle.Light" />
            </RadzenStack>
        </RadzenStack>);
    }
}
```


### Busy Dialog

Show a busy/loading indicator dialog while processing long-running operations.

```razor
<div class="rz-p-12 rz-text-align-center">
    <RadzenButton Text="Busy Dialog with a string message" ButtonStyle="ButtonStyle.Secondary" Click=@(args => ShowBusyDialog(true)) />
    <RadzenButton Text="Busy Dialog with markup" ButtonStyle="ButtonStyle.Secondary" Click=@(args => ShowBusyDialog(false)) />
</div>

@code {
    async Task ShowBusyDialog(bool withMessageAsString)
    {
        _ = InvokeAsync(async () =>
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

    // Busy dialog from markup
    async Task BusyDialog()
    {
        await DialogService.OpenAsync("", ds =>
    @<RadzenStack AlignItems="AlignItems.Center" Gap="2rem" class="rz-p-12">
        <RadzenImage Path="images/community.svg" Style="width: 200px;" AlternateText="community" />
        <RadzenText TextStyle="TextStyle.H6" TagName="TagName.P">Loading, please wait...</RadzenText>
    </RadzenStack>, new DialogOptions() { ShowTitle = false, Style = "min-height:auto;min-width:auto;width:auto", CloseDialogOnEsc = false });
    }

    // Busy dialog from string
    async Task BusyDialog(string message)
    {
        await DialogService.OpenAsync("", ds =>
        {
            RenderFragment content = dialogContent =>
            {
                dialogContent.OpenComponent<RadzenRow>(0);
                dialogContent.AddComponentParameter(1, nameof(RadzenRow.ChildContent), (RenderFragment)(rowContent => 
                {
                    rowContent.OpenComponent<RadzenColumn>(0);
                    rowContent.AddComponentParameter(1, nameof(RadzenColumn.Size), 12);
                    rowContent.AddComponentParameter(2, nameof(RadzenRow.ChildContent), (RenderFragment)(columnContent => 
                    {
                        columnContent.AddContent(0, message);
                    }));
                    rowContent.CloseComponent();
                }));

                dialogContent.CloseComponent();
            };
            return content;
        }, new DialogOptions() { ShowTitle = false, Style = "min-height:auto;min-width:auto;width:auto", CloseDialogOnEsc = false });
    }
}
```


### Confirm Dialog

Use `DialogService.Confirm()` to display a confirmation dialog with customizable buttons and messages.

```razor
<div class="rz-p-12 rz-text-align-center">
    <RadzenButton Text="Confirm dialog" ButtonStyle="ButtonStyle.Secondary" 
        Click=@(args => DialogService.Confirm("Are you sure?", "MyTitle", new ConfirmOptions() { OkButtonText = "Yes", CancelButtonText = "No" })) />
    <RadzenButton Text="Confirm dialog with markup" ButtonStyle="ButtonStyle.Secondary"
        Click=@(args => DialogService.Confirm(GetMessage(), "MyTitle", new ConfirmOptions() { OkButtonText = "Yes", CancelButtonText = "No" })) />
</div>

@code{
    RenderFragment GetMessage()
    {
        return __builder =>
        {
            <text>
                Are <b>you</b> sure?
            </text>
        };
    }
}
```


### Alert Dialog

Use `DialogService.Alert()` to display simple alert messages to users.

```razor
<div class="rz-p-12 rz-text-align-center">
    <RadzenButton Text="Alert dialog" ButtonStyle="ButtonStyle.Secondary" 
        Click=@(args => DialogService.Alert("Some message!", "MyTitle", new AlertOptions() { OkButtonText = "Yes" })) />
    <RadzenButton Text="Alert dialog with markup" ButtonStyle="ButtonStyle.Secondary"
                  Click=@(args => DialogService.Alert(GetMessage(), "MyTitle", new AlertOptions() { OkButtonText = "Yes" })) />
</div>
@code {
    RenderFragment GetMessage()
    {
        return __builder =>
        {
            <text>
                Some <b>message</b>!
            </text>
        };
    }
}
```


### Prevent dialog from closing

Use the `CanClose` callback to prevent a dialog from closing when the user clicks the X button, overlay, or presses ESC. Return `false` to block the close. Programmatic `Close()` calls always succeed.

```razor
<div class="rz-p-12 rz-text-align-center">
    <RadzenStack Orientation="Orientation.Horizontal" Gap="1rem" JustifyContent="JustifyContent.Center" Wrap="FlexWrap.Wrap">
        <RadzenButton Text="Dialog with CanClose" ButtonStyle="ButtonStyle.Secondary" Click=@OpenDialog />
        <RadzenButton Text="Side dialog with CanClose" ButtonStyle="ButtonStyle.Light" Click=@OpenSideDialog />
    </RadzenStack>
</div>

@code {
    async Task OpenDialog()
    {
        bool hasUnsavedChanges = false;

        await DialogService.OpenAsync("Unsaved Changes Guard", ds =>
    @<div>
        <RadzenStack Gap="1rem">
            <RadzenText TextStyle="TextStyle.Body1">Try closing this dialog with the X button, ESC key, or overlay click while the checkbox is checked.</RadzenText>
            <RadzenStack Orientation="Orientation.Horizontal" Gap="0.5rem" AlignItems="AlignItems.Center">
                <RadzenCheckBox @bind-Value="hasUnsavedChanges" Name="UnsavedChanges" />
                <RadzenLabel Text="Has unsaved changes" Component="UnsavedChanges" />
            </RadzenStack>
            <RadzenButton Text="Save and Close" Click="() => ds.Close(true)" Style="width: 100%" />
        </RadzenStack>
    </div>, new DialogOptions()
            {
                CloseDialogOnOverlayClick = true,
                CanClose = async () =>
                {
                    if (!hasUnsavedChanges)
                        return true;

                    return await DialogService.Confirm("You have unsaved changes. Discard?", "Confirm",
                        new ConfirmOptions() { OkButtonText = "Discard", CancelButtonText = "Cancel" }) == true;
                }
            });
    }

    async Task OpenSideDialog()
    {
        bool hasUnsavedChanges = false;

        await DialogService.OpenSideAsync<DialogCanCloseContent>("Edit Panel",
            new Dictionary<string, object>
            {
                { nameof(DialogCanCloseContent.HasUnsavedChangesChanged), EventCallback.Factory.Create<bool>(this, (value) => hasUnsavedChanges = value) }
            },
            new SideDialogOptions()
            {
                CloseDialogOnOverlayClick = true,
                ShowMask = true,
                CanClose = async () =>
                {
                    if (!hasUnsavedChanges)
                        return true;

                    return await DialogService.Confirm("You have unsaved changes. Discard?", "Confirm",
                        new ConfirmOptions() { OkButtonText = "Discard", CancelButtonText = "Cancel" }) == true;
                }
            });
    }
}
```


### Close Dialog by clicking outside

Enable `CloseDialogOnOverlayClick` to allow users to close the dialog by clicking on the overlay backdrop.

```razor
<div class="rz-p-12 rz-text-align-center">
    <RadzenButton Text="Dialog with clickable overlay" ButtonStyle="ButtonStyle.Secondary" Click=@ShowCloseableFromOverlayDialog />
</div>

@code {
    async Task ShowCloseableFromOverlayDialog()
    {
        await DialogService.OpenAsync("Closeable from overlay Dialog", ds =>
    @<div>
        Click outside to close this Dialog
    </div>, new DialogOptions() { CloseDialogOnOverlayClick = true });
    }
}
```


### Side Dialog

Position dialogs on the side of the screen using custom CSS classes for slide-in panel effects.

```razor
<div class="rz-p-12 rz-text-align-center">
    <RadzenStack Orientation="Orientation.Horizontal" Gap="1rem" AlignItems="AlignItems.Center" class="rz-p-4 rz-mb-6 rz-border-radius-1" Style="border: var(--rz-grid-cell-border);" Wrap="FlexWrap.Wrap">
        <RadzenStack Orientation="Orientation.Horizontal" Gap="0.5rem" AlignItems="AlignItems.Center">
            <RadzenLabel Text="Position:" Component="Position" />
            <RadzenSelectBar @bind-Value="@position" TextProperty="Text" ValueProperty="Value" Name="Position"
                            Data="@(Enum.GetValues(typeof(DialogPosition)).Cast<DialogPosition>().Select(t => new { Text = $"{t}", Value = t }))" Size="ButtonSize.Small" />
        </RadzenStack>
        <RadzenStack Orientation="Orientation.Horizontal" Gap="0.5rem" AlignItems="AlignItems.Center">
            <RadzenLabel Text="Show mask:" Component="Mask" />
            <RadzenSwitch @bind-Value="@showMask" Name="Mask" />
        </RadzenStack>
        <RadzenStack Orientation="Orientation.Horizontal" Gap="0.5rem" AlignItems="AlignItems.Center">
            <RadzenLabel Text="Close on overlay click:" Component="Close" />
            <RadzenSwitch @bind-Value="@closeDialogOnOverlayClick" Disabled=@(!showMask) Name="Close" />
        </RadzenStack>
        <RadzenStack Orientation="Orientation.Horizontal" Gap="0.5rem" AlignItems="AlignItems.Center">
            <RadzenLabel Text="Resizable:" Component="Resizable" />
            <RadzenSwitch @bind-Value="@resizable" Name="Resizable" />
        </RadzenStack>
    </RadzenStack>
    <RadzenButton Text="Dialog on Side" ButtonStyle="ButtonStyle.Secondary" Click="@OpenSideDialog" />
</div>

@code {
    DialogPosition position;
    bool closeDialogOnOverlayClick;
    bool showMask;
    bool resizable;

    async Task OpenSideDialog()
    {
        await DialogService.OpenSideAsync<DialogSideContent>("Side Panel", options: new SideDialogOptions { CloseDialogOnOverlayClick = closeDialogOnOverlayClick, Resizable = resizable, Position = position, ShowMask = showMask ,MinHeight = 250.0, MinWidth = 350.0 });
    }
}
```


### Dialog with custom CSS classes

Apply custom CSS classes to dialogs for complete styling customization and theming.

```razor
<div class="rz-p-12 rz-text-align-center">
    <RadzenButton Text="Dialog with custom CSS classes" ButtonStyle="ButtonStyle.Secondary"
                  Click=@ShowDialogWithCustomCssClasses />
</div>

@code {
    async Task ShowDialogWithCustomCssClasses()
    {
        await DialogService.OpenAsync("Dialog with custom CSS classes", ds =>
        @<div>
            This dialog has custom CSS classes.
        </div>, new DialogOptions() {
                  CssClass = "custom-dialog-class",
                  WrapperCssClass = "custom-dialog-wrapper-class"
              });
    }
}
```


### Update dialog properties

Dynamically update dialog properties like title, width, and height at runtime using the cascading dialog reference.
Use the `Dialog` [Cascading Value](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/cascading-values-and-parameters#cascadingvalue-component) to update dialog properties

```razor
<div class="rz-p-12 rz-text-align-center">
	<RadzenButton Text="Update dialog properties" ButtonStyle="ButtonStyle.Secondary" Click=@OpenDialog />
</div>
@code {
	private async Task OpenDialog()
	{
		await DialogService.OpenAsync<DialogWithCascadingValueImplementation>("Original title text");
	}
}
```
