# ToggleButton

The Blazor ToggleButton switches between on and off states, changing its appearance when activated - ideal for toolbars and settings.

Keywords: button, switch, toggle

> API reference: [RadzenToggleButton API](https://blazor.radzen.com/api/togglebutton.md)

## Examples

## Blazor ToggleButton

The Blazor ToggleButton switches between on and off states, changing its appearance when activated - ideal for toolbars and settings.

### Bound ToggleButton

Binding Radzen ToggleButton's Value.

```razor
<RadzenStack Orientation="Orientation.Vertical" AlignItems="AlignItems.Center" JustifyContent="JustifyContent.Center" Gap="2rem" Wrap="FlexWrap.Wrap" class="rz-p-12">
    <RadzenToggleButton @bind-Value=@value Change=@OnChange Text="@(value ? "Turn off Notifications" : "Turn on Notifications" )" ButtonStyle="ButtonStyle.Light" 
        ToggleButtonStyle="ButtonStyle.Dark" InputAttributes="@(new Dictionary<string,object>(){ { "aria-label", "Turn off Notifications" }})" />
    
    <RadzenIcon Icon="south" />
    <RadzenCard style="width: 220px;">
        <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" JustifyContent="JustifyContent.SpaceBetween" Gap="1rem" Wrap="FlexWrap.Wrap">
            <RadzenText Text="Notifications" TextStyle="TextStyle.Body2" />
            <RadzenBadge BadgeStyle="@(value ? BadgeStyle.Info : BadgeStyle.Danger )" Text="@(value ? "ON" : "OFF" )" Shade="Shade.Lighter" />
        </RadzenStack>
    </RadzenCard>
</RadzenStack>

@code {
    bool value;

    private void OnChange(bool newValue)
    {        
        NotificationService.Notify(new NotificationMessage { Severity = NotificationSeverity.Info, Summary = "ToggleButton Changed", Detail = $"{newValue}" });
    }
}
```


### ToggleButton Shade

Use `ToggleButtonShade` to define the shade of the ToggleButton's toggled active state.

```razor
<RadzenStack Orientation="Orientation.Vertical" AlignItems="AlignItems.Center" JustifyContent="JustifyContent.Center" Gap="2rem" Wrap="FlexWrap.Wrap" class="rz-p-12">
    <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="1rem" Wrap="FlexWrap.Wrap">
        <RadzenToggleButton Shade="Shade.Lighter" ToggleShade="Shade.Default" Click=@(args => OnClick("Primary button")) Text="Primary" ButtonStyle="ButtonStyle.Primary" 
            InputAttributes="@(new Dictionary<string,object>(){ { "aria-label", "Lighter" }})" />
        <RadzenToggleButton Shade="Shade.Light" ToggleShade="Shade.Dark" Click=@(args => OnClick("Primary button")) Text="Primary" ButtonStyle="ButtonStyle.Primary"
                            InputAttributes="@(new Dictionary<string,object>(){ { "aria-label", "Light" }})" />
        <RadzenToggleButton ToggleShade="Shade.Darker" Click=@(args => OnClick("Primary button")) Text="Primary" ButtonStyle="ButtonStyle.Primary"
                            InputAttributes="@(new Dictionary<string,object>(){ { "aria-label", "Darker Toggle Shade" }})" />
        <RadzenToggleButton Shade="Shade.Dark" ToggleShade="Shade.Default" Click=@(args => OnClick("Primary button")) Text="Primary" ButtonStyle="ButtonStyle.Primary"
                            InputAttributes="@(new Dictionary<string,object>(){ { "aria-label", "Dark Shade" }})" />
        <RadzenToggleButton Shade="Shade.Darker" ToggleShade="Shade.Light" Click=@(args => OnClick("Primary button")) Text="Primary" ButtonStyle="ButtonStyle.Primary"
                            InputAttributes="@(new Dictionary<string,object>(){ { "aria-label", "Darker Shade" }})" />
    </RadzenStack>

    <RadzenText TextStyle="TextStyle.Subtitle1" TagName="TagName.H3">Light and Dark button styles don't have Shades</RadzenText>
    <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="1rem" Wrap="FlexWrap.Wrap">
        <RadzenToggleButton Click=@(args => OnClick("Light button")) Text="Light" ButtonStyle="ButtonStyle.Light" ToggleButtonStyle="ButtonStyle.Dark"
                            InputAttributes="@(new Dictionary<string,object>(){ { "aria-label", "Light button" }})" />
        <RadzenToggleButton Click=@(args => OnClick("Dark button")) Text="Dark" ButtonStyle="ButtonStyle.Dark" ToggleButtonStyle="ButtonStyle.Light"
                            InputAttributes="@(new Dictionary<string,object>(){ { "aria-label", "Dark button" }})" />
    </RadzenStack>
</RadzenStack>

@code {
    private void OnClick(string text)
    {
        NotificationService.Notify(new NotificationMessage { Severity = NotificationSeverity.Info, Summary = "ToggleButton Clicked", Detail = text });
    }
}
```


### ToggleButton Style

Use `ToggleButtonStyle` to define the style of the ToggleButton's toggled active state.

```razor
<RadzenStack Orientation="Orientation.Vertical" AlignItems="AlignItems.Center" JustifyContent="JustifyContent.Center" Gap="2rem" Wrap="FlexWrap.Wrap" class="rz-p-12">
    <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="1rem" Wrap="FlexWrap.Wrap">
        <RadzenToggleButton ButtonStyle="ButtonStyle.Success" ToggleButtonStyle="ButtonStyle.Danger" ToggleShade="Shade.Default" Click=@(args => OnClick("Primary button"))
                            Text="Toggle Severity" InputAttributes="@(new Dictionary<string,object>(){ { "aria-label", "Button" }})" />
    </RadzenStack>
</RadzenStack>

@code {
    private void OnClick(string text)
    {
        NotificationService.Notify(new NotificationMessage { Severity = NotificationSeverity.Info, Summary = "ToggleButton Clicked", Detail = text });
    }
}
```


### ToggleButton Variants

Use `Variant` for different button variants.

```razor
<RadzenStack Orientation="Orientation.Vertical" AlignItems="AlignItems.Center" JustifyContent="JustifyContent.Center" Gap="2rem" Wrap="FlexWrap.Wrap" class="rz-p-12">
    <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="1rem" Wrap="FlexWrap.Wrap">
        <RadzenToggleButton Variant="Variant.Filled" Shade="Shade.Default" ToggleShade="Shade.Darker" Click=@(args => OnClick("Filled button")) Text="Filled"
                            InputAttributes="@(new Dictionary<string,object>(){ { "aria-label", "Filled" }})" />
        <RadzenToggleButton Variant="Variant.Flat" Shade="Shade.Default" ToggleShade="Shade.Darker" Click=@(args => OnClick("Flat button")) Text="Flat"
                            InputAttributes="@(new Dictionary<string,object>(){ { "aria-label", "Flat" }})" />
        <RadzenToggleButton Variant="Variant.Outlined" Shade="Shade.Default" ToggleShade="Shade.Darker" Click=@(args => OnClick("Outlined button")) Text="Outlined"
                            InputAttributes="@(new Dictionary<string,object>(){ { "aria-label", "Outlined" }})" />
        <RadzenToggleButton Variant="Variant.Text" Shade="Shade.Default" ToggleShade="Shade.Darker" Click=@(args => OnClick("Text button ")) Text="Text"
                            InputAttributes="@(new Dictionary<string,object>(){ { "aria-label", "Text" }})" />
    </RadzenStack>
</RadzenStack>

@code {
    private void OnClick(string text)
    {
        NotificationService.Notify(new NotificationMessage { Severity = NotificationSeverity.Info, Summary = "ToggleButton Clicked", Detail = text });
    }
}
```


### Content in ToggleButtons

Text, icons and images can be added to a button. Use `ToggleIcon` in case you need to change the icon when the button is toggled.

```razor
<RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" JustifyContent="JustifyContent.Center" Gap="1rem" Wrap="FlexWrap.Wrap" class="rz-p-12">
    <RadzenToggleButton Click=@(args => OnClick("Primary icon button")) Icon="heart_plus" ToggleIcon="favorite" ButtonStyle="ButtonStyle.Primary"
                        Shade="Shade.Light" ToggleShade="Shade.Dark" InputAttributes="@(new Dictionary<string,object>(){ { "aria-label", "Primary icon button" }})" />
    <RadzenToggleButton Click=@(args => OnClick("Primary button with text and icon")) Text="Like" Icon="heart_plus" ToggleIcon="favorite" 
        ButtonStyle="ButtonStyle.Primary" Shade="Shade.Light" ToggleShade="Shade.Dark" InputAttributes="@(new Dictionary<string,object>(){ { "aria-label", "Primary button with text and icon" }})" />
    <RadzenToggleButton Click=@(args => OnClick("ToggleButton with image")) Image="images/radzen-nuget.png" ButtonStyle="ButtonStyle.Light" Shade="Shade.Light"
                        ToggleShade="Shade.Dark" InputAttributes="@(new Dictionary<string,object>(){ { "aria-label", "ToggleButton with image" }})" />
    <RadzenToggleButton Click=@(args => OnClick("ToggleButton with content")) Image="images/radzen-nuget.png" ButtonStyle="ButtonStyle.Light" Shade="Shade.Light"
                        ToggleShade="Shade.Dark" InputAttributes="@(new Dictionary<string,object>(){ { "aria-label", "ToggleButton with content" }})">
        <span class="rz-button-text">ToggleButton with content</span>
        <RadzenImage Path="images/radzen-nuget.png" Style="width: 20px; height: 20px;" class="rz-ms-2" AlternateText="nuget" />
    </RadzenToggleButton>
</RadzenStack>

@code {
    private void OnClick(string text)
    {
        NotificationService.Notify(new NotificationMessage { Severity = NotificationSeverity.Info, Summary = "ToggleButton Clicked", Detail = text });
    }
}
```


### ToggleButton Sizes

Use the `Size` property to set button size. Available sizes are ExtraSmall, Small, Medium (default), and Large.

```razor
<RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" JustifyContent="JustifyContent.Center" Gap="1rem" Wrap="FlexWrap.Wrap" class="rz-p-12">
    <RadzenToggleButton Click=@(args => OnClick("Large ToggleButton")) Text="Large" Size="ButtonSize.Large" Shade="Shade.Light" ToggleShade="Shade.Darker"
                        InputAttributes="@(new Dictionary<string,object>(){ { "aria-label", "Large" }})" />
    <RadzenToggleButton Click=@(args => OnClick("Medium ToggleButton")) Text="Medium" Size="ButtonSize.Medium" Shade="Shade.Light" ToggleShade="Shade.Darker"
                        InputAttributes="@(new Dictionary<string,object>(){ { "aria-label", "Medium" }})" />
    <RadzenToggleButton Click=@(args => OnClick("Small ToggleButton")) Text="Small" Size="ButtonSize.Small" Shade="Shade.Light" ToggleShade="Shade.Darker"
                        InputAttributes="@(new Dictionary<string,object>(){ { "aria-label", "Small" }})" />
    <RadzenToggleButton Click=@(args => OnClick("Extra Small ToggleButton")) Text="ExtraSmall" Size="ButtonSize.ExtraSmall" Shade="Shade.Light" ToggleShade="Shade.Darker"
                        InputAttributes="@(new Dictionary<string,object>(){ { "aria-label", "Extra Small" }})" />
</RadzenStack>

@code {
    private void OnClick(string text)
    {
        NotificationService.Notify(new NotificationMessage { Severity = NotificationSeverity.Info, Summary = "ToggleButton Clicked", Detail = text });
    }
}
```


### Disabled ToggleButton

Use `Disabled="true"` to disable a button.

```razor
<RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" JustifyContent="JustifyContent.Center" Gap="1rem" Wrap="FlexWrap.Wrap" class="rz-p-12">
    <RadzenToggleButton Disabled="true" Text="Primary" ButtonStyle="ButtonStyle.Primary" ToggleShade="Shade.Dark" InputAttributes="@(new Dictionary<string,object>(){ { "aria-label", "Primary" }})" />
    <RadzenToggleButton Disabled="true" Text="Secondary" ButtonStyle="ButtonStyle.Secondary" ToggleShade="Shade.Dark" InputAttributes="@(new Dictionary<string,object>(){ { "aria-label", "Secondary" }})" />
    <RadzenToggleButton Disabled="true" Text="Light" ButtonStyle="ButtonStyle.Light" ToggleShade="Shade.Dark" InputAttributes="@(new Dictionary<string,object>(){ { "aria-label", "Light" }})" />
    <RadzenToggleButton Disabled="true" Text="Info" ButtonStyle="ButtonStyle.Info" ToggleShade="Shade.Dark" InputAttributes="@(new Dictionary<string,object>(){ { "aria-label", "Info" }})" />
    <RadzenToggleButton Disabled="true" Text="Success" ButtonStyle="ButtonStyle.Success" ToggleShade="Shade.Dark" InputAttributes="@(new Dictionary<string,object>(){ { "aria-label", "Success" }})" />
    <RadzenToggleButton Disabled="true" Text="Warning" ButtonStyle="ButtonStyle.Warning" ToggleShade="Shade.Dark" InputAttributes="@(new Dictionary<string,object>(){ { "aria-label", "Warning" }})" />
    <RadzenToggleButton Disabled="true" Text="Danger" ButtonStyle="ButtonStyle.Danger" ToggleShade="Shade.Dark" InputAttributes="@(new Dictionary<string,object>(){ { "aria-label", "Danger" }})" />
</RadzenStack>

@code {
    private void OnClick(string text)
    {
        NotificationService.Notify(new NotificationMessage { Severity = NotificationSeverity.Info, Summary = "ToggleButton Clicked", Detail = text });
    }
}
```
