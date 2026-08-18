# Button

The Radzen Blazor Button comes in filled, flat, outlined, and text variants, with sizes, icons, shades, busy and disabled states, and click handling.

Keywords: button, form, click

> API reference: [RadzenButton API](https://blazor.radzen.com/api/button.md)

## Examples

## Blazor Button

The Radzen Blazor Button comes in filled, flat, outlined, and text variants, with sizes, icons, shades, busy and disabled states, and click handling.

### Filled Buttons

These are the default Radzen Buttons.

```razor
<RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="1rem" Wrap="FlexWrap.Wrap">
    <RadzenButton Click=@(args => OnClick("Primary button")) Text="Primary" ButtonStyle="ButtonStyle.Primary" />
    <RadzenButton Click=@(args => OnClick("Secondary button")) Text="Secondary" ButtonStyle="ButtonStyle.Secondary" />
    <RadzenButton Click=@(args => OnClick("Base button")) Text="Base" ButtonStyle="ButtonStyle.Base" />
    <RadzenButton Click=@(args => OnClick("Info button")) Text="Info" ButtonStyle="ButtonStyle.Info" />
    <RadzenButton Click=@(args => OnClick("Success button ")) Text="Success" ButtonStyle="ButtonStyle.Success" />
    <RadzenButton Click=@(args => OnClick("Warning button ")) Text="Warning" ButtonStyle="ButtonStyle.Warning" />
    <RadzenButton Click=@(args => OnClick("Danger button")) Text="Danger" ButtonStyle="ButtonStyle.Danger" />
</RadzenStack>
<RadzenText TextStyle="TextStyle.Subtitle1" TagName="TagName.H3" class="rz-mt-4">Filled Shades</RadzenText>
<RadzenStack Gap="1rem">
    <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="1rem" Wrap="FlexWrap.Wrap">
        <RadzenButton Shade="Shade.Lighter" Click=@(args => OnClick("Primary button")) Text="Primary" ButtonStyle="ButtonStyle.Primary" />
        <RadzenButton Shade="Shade.Lighter" Click=@(args => OnClick("Secondary button")) Text="Secondary" ButtonStyle="ButtonStyle.Secondary" />
        <RadzenButton Shade="Shade.Lighter" Click=@(args => OnClick("Base button")) Text="Base" ButtonStyle="ButtonStyle.Base" />
        <RadzenButton Shade="Shade.Lighter" Click=@(args => OnClick("Info button")) Text="Info" ButtonStyle="ButtonStyle.Info" />
        <RadzenButton Shade="Shade.Lighter" Click=@(args => OnClick("Success button ")) Text="Success" ButtonStyle="ButtonStyle.Success" />
        <RadzenButton Shade="Shade.Lighter" Click=@(args => OnClick("Warning button ")) Text="Warning" ButtonStyle="ButtonStyle.Warning" />
        <RadzenButton Shade="Shade.Lighter" Click=@(args => OnClick("Danger button")) Text="Danger" ButtonStyle="ButtonStyle.Danger" />
    </RadzenStack>
    <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="1rem" Wrap="FlexWrap.Wrap">
        <RadzenButton Shade="Shade.Light" Click=@(args => OnClick("Primary button")) Text="Primary" ButtonStyle="ButtonStyle.Primary" />
        <RadzenButton Shade="Shade.Light" Click=@(args => OnClick("Secondary button")) Text="Secondary" ButtonStyle="ButtonStyle.Secondary" />
        <RadzenButton Shade="Shade.Light" Click=@(args => OnClick("Base button")) Text="Base" ButtonStyle="ButtonStyle.Base" />
        <RadzenButton Shade="Shade.Light" Click=@(args => OnClick("Info button")) Text="Info" ButtonStyle="ButtonStyle.Info" />
        <RadzenButton Shade="Shade.Light" Click=@(args => OnClick("Success button ")) Text="Success" ButtonStyle="ButtonStyle.Success" />
        <RadzenButton Shade="Shade.Light" Click=@(args => OnClick("Warning button ")) Text="Warning" ButtonStyle="ButtonStyle.Warning" />
        <RadzenButton Shade="Shade.Light" Click=@(args => OnClick("Danger button")) Text="Danger" ButtonStyle="ButtonStyle.Danger" />
    </RadzenStack>
    <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="1rem" Wrap="FlexWrap.Wrap">
        <RadzenButton Shade="Shade.Dark" Click=@(args => OnClick("Primary button")) Text="Primary" ButtonStyle="ButtonStyle.Primary" />
        <RadzenButton Shade="Shade.Dark" Click=@(args => OnClick("Secondary button")) Text="Secondary" ButtonStyle="ButtonStyle.Secondary" />
        <RadzenButton Shade="Shade.Dark" Click=@(args => OnClick("Base button")) Text="Base" ButtonStyle="ButtonStyle.Base" />
        <RadzenButton Shade="Shade.Dark" Click=@(args => OnClick("Info button")) Text="Info" ButtonStyle="ButtonStyle.Info" />
        <RadzenButton Shade="Shade.Dark" Click=@(args => OnClick("Success button ")) Text="Success" ButtonStyle="ButtonStyle.Success" />
        <RadzenButton Shade="Shade.Dark" Click=@(args => OnClick("Warning button ")) Text="Warning" ButtonStyle="ButtonStyle.Warning" />
        <RadzenButton Shade="Shade.Dark" Click=@(args => OnClick("Danger button")) Text="Danger" ButtonStyle="ButtonStyle.Danger" />
    </RadzenStack>
    <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="1rem" Wrap="FlexWrap.Wrap">
        <RadzenButton Shade="Shade.Darker" Click=@(args => OnClick("Primary button")) Text="Primary" ButtonStyle="ButtonStyle.Primary" />
        <RadzenButton Shade="Shade.Darker" Click=@(args => OnClick("Secondary button")) Text="Secondary" ButtonStyle="ButtonStyle.Secondary" />
        <RadzenButton Shade="Shade.Darker" Click=@(args => OnClick("Base button")) Text="Base" ButtonStyle="ButtonStyle.Base" />
        <RadzenButton Shade="Shade.Darker" Click=@(args => OnClick("Info button")) Text="Info" ButtonStyle="ButtonStyle.Info" />
        <RadzenButton Shade="Shade.Darker" Click=@(args => OnClick("Success button ")) Text="Success" ButtonStyle="ButtonStyle.Success" />
        <RadzenButton Shade="Shade.Darker" Click=@(args => OnClick("Warning button ")) Text="Warning" ButtonStyle="ButtonStyle.Warning" />
        <RadzenButton Shade="Shade.Darker" Click=@(args => OnClick("Danger button")) Text="Danger" ButtonStyle="ButtonStyle.Danger" />
    </RadzenStack>
</RadzenStack>
        <RadzenText TextStyle="TextStyle.Subtitle1" TagName="TagName.H3" class="rz-mt-4">Filled Light and Dark</RadzenText>
        <RadzenText TextStyle="TextStyle.Body2" class="rz-mb-4">Light and Dark button styles don't have Shades</RadzenText>
<RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="1rem" Wrap="FlexWrap.Wrap">
    <RadzenButton Shade="Shade.Lighter" Click=@(args => OnClick("Light button")) Text="Light" ButtonStyle="ButtonStyle.Light" />
    <RadzenButton Shade="Shade.Lighter" Click=@(args => OnClick("Dark button")) Text="Dark" ButtonStyle="ButtonStyle.Dark" />
</RadzenStack>

@code {
    private void OnClick(string text)
    {
        NotificationService.Notify(new NotificationMessage { Severity = NotificationSeverity.Info, Summary = "Button Clicked", Detail = text });
    }
}
```


### Flat Buttons

Use `Variant="Variant.Flat"` for flat button variant.

```razor
<RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="1rem" Wrap="FlexWrap.Wrap">
    <RadzenButton Variant="Variant.Flat" Click=@(args => OnClick("Primary button")) Text="Primary" ButtonStyle="ButtonStyle.Primary" />
    <RadzenButton Variant="Variant.Flat" Click=@(args => OnClick("Secondary button")) Text="Secondary" ButtonStyle="ButtonStyle.Secondary" />
    <RadzenButton Variant="Variant.Flat" Click=@(args => OnClick("Base button")) Text="Base" ButtonStyle="ButtonStyle.Base" />
    <RadzenButton Variant="Variant.Flat" Click=@(args => OnClick("Info button")) Text="Info" ButtonStyle="ButtonStyle.Info" />
    <RadzenButton Variant="Variant.Flat" Click=@(args => OnClick("Success button ")) Text="Success" ButtonStyle="ButtonStyle.Success" />
    <RadzenButton Variant="Variant.Flat" Click=@(args => OnClick("Warning button ")) Text="Warning" ButtonStyle="ButtonStyle.Warning" />
    <RadzenButton Variant="Variant.Flat" Click=@(args => OnClick("Danger button")) Text="Danger" ButtonStyle="ButtonStyle.Danger" />
</RadzenStack>

<RadzenText TextStyle="TextStyle.Subtitle1" TagName="TagName.H3" class="rz-mt-4">Flat Shades</RadzenText>
<RadzenStack Gap="1rem">
    <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="1rem" Wrap="FlexWrap.Wrap">
        <RadzenButton Variant="Variant.Flat" Shade="Shade.Lighter" Click=@(args => OnClick("Primary button")) Text="Primary" ButtonStyle="ButtonStyle.Primary" />
        <RadzenButton Variant="Variant.Flat" Shade="Shade.Lighter" Click=@(args => OnClick("Secondary button")) Text="Secondary" ButtonStyle="ButtonStyle.Secondary" />
        <RadzenButton Variant="Variant.Flat" Shade="Shade.Lighter" Click=@(args => OnClick("Base button")) Text="Base" ButtonStyle="ButtonStyle.Base" />
        <RadzenButton Variant="Variant.Flat" Shade="Shade.Lighter" Click=@(args => OnClick("Info button")) Text="Info" ButtonStyle="ButtonStyle.Info" />
        <RadzenButton Variant="Variant.Flat" Shade="Shade.Lighter" Click=@(args => OnClick("Success button ")) Text="Success" ButtonStyle="ButtonStyle.Success" />
        <RadzenButton Variant="Variant.Flat" Shade="Shade.Lighter" Click=@(args => OnClick("Warning button ")) Text="Warning" ButtonStyle="ButtonStyle.Warning" />
        <RadzenButton Variant="Variant.Flat" Shade="Shade.Lighter" Click=@(args => OnClick("Danger button")) Text="Danger" ButtonStyle="ButtonStyle.Danger" />
    </RadzenStack>
    <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="1rem" Wrap="FlexWrap.Wrap">
        <RadzenButton Variant="Variant.Flat" Shade="Shade.Light" Click=@(args => OnClick("Primary button")) Text="Primary" ButtonStyle="ButtonStyle.Primary" />
        <RadzenButton Variant="Variant.Flat" Shade="Shade.Light" Click=@(args => OnClick("Secondary button")) Text="Secondary" ButtonStyle="ButtonStyle.Secondary" />
        <RadzenButton Variant="Variant.Flat" Shade="Shade.Light" Click=@(args => OnClick("Base button")) Text="Base" ButtonStyle="ButtonStyle.Base" />
        <RadzenButton Variant="Variant.Flat" Shade="Shade.Light" Click=@(args => OnClick("Info button")) Text="Info" ButtonStyle="ButtonStyle.Info" />
        <RadzenButton Variant="Variant.Flat" Shade="Shade.Light" Click=@(args => OnClick("Success button ")) Text="Success" ButtonStyle="ButtonStyle.Success" />
        <RadzenButton Variant="Variant.Flat" Shade="Shade.Light" Click=@(args => OnClick("Warning button ")) Text="Warning" ButtonStyle="ButtonStyle.Warning" />
        <RadzenButton Variant="Variant.Flat" Shade="Shade.Light" Click=@(args => OnClick("Danger button")) Text="Danger" ButtonStyle="ButtonStyle.Danger" />
    </RadzenStack>
    <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="1rem" Wrap="FlexWrap.Wrap">
        <RadzenButton Variant="Variant.Flat" Shade="Shade.Dark" Click=@(args => OnClick("Primary button")) Text="Primary" ButtonStyle="ButtonStyle.Primary" />
        <RadzenButton Variant="Variant.Flat" Shade="Shade.Dark" Click=@(args => OnClick("Secondary button")) Text="Secondary" ButtonStyle="ButtonStyle.Secondary" />
        <RadzenButton Variant="Variant.Flat" Shade="Shade.Dark" Click=@(args => OnClick("Base button")) Text="Base" ButtonStyle="ButtonStyle.Base" />
        <RadzenButton Variant="Variant.Flat" Shade="Shade.Dark" Click=@(args => OnClick("Info button")) Text="Info" ButtonStyle="ButtonStyle.Info" />
        <RadzenButton Variant="Variant.Flat" Shade="Shade.Dark" Click=@(args => OnClick("Success button ")) Text="Success" ButtonStyle="ButtonStyle.Success" />
        <RadzenButton Variant="Variant.Flat" Shade="Shade.Dark" Click=@(args => OnClick("Warning button ")) Text="Warning" ButtonStyle="ButtonStyle.Warning" />
        <RadzenButton Variant="Variant.Flat" Shade="Shade.Dark" Click=@(args => OnClick("Danger button")) Text="Danger" ButtonStyle="ButtonStyle.Danger" />
    </RadzenStack>
    <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="1rem" Wrap="FlexWrap.Wrap">
        <RadzenButton Variant="Variant.Flat" Shade="Shade.Darker" Click=@(args => OnClick("Primary button")) Text="Primary" ButtonStyle="ButtonStyle.Primary" />
        <RadzenButton Variant="Variant.Flat" Shade="Shade.Darker" Click=@(args => OnClick("Secondary button")) Text="Secondary" ButtonStyle="ButtonStyle.Secondary" />
        <RadzenButton Variant="Variant.Flat" Shade="Shade.Darker" Click=@(args => OnClick("Base button")) Text="Base" ButtonStyle="ButtonStyle.Base" />
        <RadzenButton Variant="Variant.Flat" Shade="Shade.Darker" Click=@(args => OnClick("Info button")) Text="Info" ButtonStyle="ButtonStyle.Info" />
        <RadzenButton Variant="Variant.Flat" Shade="Shade.Darker" Click=@(args => OnClick("Success button ")) Text="Success" ButtonStyle="ButtonStyle.Success" />
        <RadzenButton Variant="Variant.Flat" Shade="Shade.Darker" Click=@(args => OnClick("Warning button ")) Text="Warning" ButtonStyle="ButtonStyle.Warning" />
        <RadzenButton Variant="Variant.Flat" Shade="Shade.Darker" Click=@(args => OnClick("Danger button")) Text="Danger" ButtonStyle="ButtonStyle.Danger" />
    </RadzenStack>
</RadzenStack>

<RadzenText TextStyle="TextStyle.Subtitle1" TagName="TagName.H3" class="rz-mt-4">Flat Light and Dark</RadzenText>
<RadzenText TextStyle="TextStyle.Body2" class="rz-mb-4">Light and Dark button styles don't have Shades</RadzenText>
<RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="1rem" Wrap="FlexWrap.Wrap">
    <RadzenButton Variant="Variant.Flat" Click=@(args => OnClick("Light button")) Text="Light" ButtonStyle="ButtonStyle.Light" />
    <RadzenButton Variant="Variant.Flat" Click=@(args => OnClick("Dark button")) Text="Dark" ButtonStyle="ButtonStyle.Dark" />
</RadzenStack>

@code {
    private void OnClick(string text)
    {
        NotificationService.Notify(new NotificationMessage { Severity = NotificationSeverity.Info, Summary = "Button Clicked", Detail = text });
    }
}
```


### Outlined Buttons

Use `Variant="Variant.Outlined"` for outlined button variant.

```razor
<RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="1rem" Wrap="FlexWrap.Wrap">
    <RadzenButton Variant="Variant.Outlined" Click=@(args => OnClick("Primary button")) Text="Primary" ButtonStyle="ButtonStyle.Primary" />
    <RadzenButton Variant="Variant.Outlined" Click=@(args => OnClick("Secondary button")) Text="Secondary" ButtonStyle="ButtonStyle.Secondary" />
    <RadzenButton Variant="Variant.Outlined" Click=@(args => OnClick("Base button")) Text="Base" ButtonStyle="ButtonStyle.Base" />
    <RadzenButton Variant="Variant.Outlined" Click=@(args => OnClick("Info button")) Text="Info" ButtonStyle="ButtonStyle.Info" />
    <RadzenButton Variant="Variant.Outlined" Click=@(args => OnClick("Success button ")) Text="Success" ButtonStyle="ButtonStyle.Success" />
    <RadzenButton Variant="Variant.Outlined" Click=@(args => OnClick("Warning button ")) Text="Warning" ButtonStyle="ButtonStyle.Warning" />
    <RadzenButton Variant="Variant.Outlined" Click=@(args => OnClick("Danger button")) Text="Danger" ButtonStyle="ButtonStyle.Danger" />
</RadzenStack>

<RadzenText TextStyle="TextStyle.Subtitle1" TagName="TagName.H3" class="rz-mt-4">Outlined Shades</RadzenText>
<RadzenStack Gap="1rem">
    <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="1rem" Wrap="FlexWrap.Wrap">
        <RadzenButton Variant="Variant.Outlined" Shade="Shade.Lighter" Click=@(args => OnClick("Primary button")) Text="Primary" ButtonStyle="ButtonStyle.Primary" />
        <RadzenButton Variant="Variant.Outlined" Shade="Shade.Lighter" Click=@(args => OnClick("Secondary button")) Text="Secondary" ButtonStyle="ButtonStyle.Secondary" />
        <RadzenButton Variant="Variant.Outlined" Shade="Shade.Lighter" Click=@(args => OnClick("Base button")) Text="Base" ButtonStyle="ButtonStyle.Base" />
        <RadzenButton Variant="Variant.Outlined" Shade="Shade.Lighter" Click=@(args => OnClick("Info button")) Text="Info" ButtonStyle="ButtonStyle.Info" />
        <RadzenButton Variant="Variant.Outlined" Shade="Shade.Lighter" Click=@(args => OnClick("Success button ")) Text="Success" ButtonStyle="ButtonStyle.Success" />
        <RadzenButton Variant="Variant.Outlined" Shade="Shade.Lighter" Click=@(args => OnClick("Warning button ")) Text="Warning" ButtonStyle="ButtonStyle.Warning" />
        <RadzenButton Variant="Variant.Outlined" Shade="Shade.Lighter" Click=@(args => OnClick("Danger button")) Text="Danger" ButtonStyle="ButtonStyle.Danger" />
    </RadzenStack>
    <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="1rem" Wrap="FlexWrap.Wrap">
        <RadzenButton Variant="Variant.Outlined" Shade="Shade.Light" Click=@(args => OnClick("Primary button")) Text="Primary" ButtonStyle="ButtonStyle.Primary" />
        <RadzenButton Variant="Variant.Outlined" Shade="Shade.Light" Click=@(args => OnClick("Secondary button")) Text="Secondary" ButtonStyle="ButtonStyle.Secondary" />
        <RadzenButton Variant="Variant.Outlined" Shade="Shade.Light" Click=@(args => OnClick("Base button")) Text="Base" ButtonStyle="ButtonStyle.Base" />
        <RadzenButton Variant="Variant.Outlined" Shade="Shade.Light" Click=@(args => OnClick("Info button")) Text="Info" ButtonStyle="ButtonStyle.Info" />
        <RadzenButton Variant="Variant.Outlined" Shade="Shade.Light" Click=@(args => OnClick("Success button ")) Text="Success" ButtonStyle="ButtonStyle.Success" />
        <RadzenButton Variant="Variant.Outlined" Shade="Shade.Light" Click=@(args => OnClick("Warning button ")) Text="Warning" ButtonStyle="ButtonStyle.Warning" />
        <RadzenButton Variant="Variant.Outlined" Shade="Shade.Light" Click=@(args => OnClick("Danger button")) Text="Danger" ButtonStyle="ButtonStyle.Danger" />
    </RadzenStack>
    <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="1rem" Wrap="FlexWrap.Wrap">
        <RadzenButton Variant="Variant.Outlined" Shade="Shade.Dark" Click=@(args => OnClick("Primary button")) Text="Primary" ButtonStyle="ButtonStyle.Primary" />
        <RadzenButton Variant="Variant.Outlined" Shade="Shade.Dark" Click=@(args => OnClick("Secondary button")) Text="Secondary" ButtonStyle="ButtonStyle.Secondary" />
        <RadzenButton Variant="Variant.Outlined" Shade="Shade.Dark" Click=@(args => OnClick("Base button")) Text="Base" ButtonStyle="ButtonStyle.Base" />
        <RadzenButton Variant="Variant.Outlined" Shade="Shade.Dark" Click=@(args => OnClick("Info button")) Text="Info" ButtonStyle="ButtonStyle.Info" />
        <RadzenButton Variant="Variant.Outlined" Shade="Shade.Dark" Click=@(args => OnClick("Success button ")) Text="Success" ButtonStyle="ButtonStyle.Success" />
        <RadzenButton Variant="Variant.Outlined" Shade="Shade.Dark" Click=@(args => OnClick("Warning button ")) Text="Warning" ButtonStyle="ButtonStyle.Warning" />
        <RadzenButton Variant="Variant.Outlined" Shade="Shade.Dark" Click=@(args => OnClick("Danger button")) Text="Danger" ButtonStyle="ButtonStyle.Danger" />
    </RadzenStack>
    <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="1rem" Wrap="FlexWrap.Wrap">
        <RadzenButton Variant="Variant.Outlined" Shade="Shade.Darker" Click=@(args => OnClick("Primary button")) Text="Primary" ButtonStyle="ButtonStyle.Primary" />
        <RadzenButton Variant="Variant.Outlined" Shade="Shade.Darker" Click=@(args => OnClick("Secondary button")) Text="Secondary" ButtonStyle="ButtonStyle.Secondary" />
        <RadzenButton Variant="Variant.Outlined" Shade="Shade.Darker" Click=@(args => OnClick("Base button")) Text="Base" ButtonStyle="ButtonStyle.Base" />
        <RadzenButton Variant="Variant.Outlined" Shade="Shade.Darker" Click=@(args => OnClick("Info button")) Text="Info" ButtonStyle="ButtonStyle.Info" />
        <RadzenButton Variant="Variant.Outlined" Shade="Shade.Darker" Click=@(args => OnClick("Success button ")) Text="Success" ButtonStyle="ButtonStyle.Success" />
        <RadzenButton Variant="Variant.Outlined" Shade="Shade.Darker" Click=@(args => OnClick("Warning button ")) Text="Warning" ButtonStyle="ButtonStyle.Warning" />
        <RadzenButton Variant="Variant.Outlined" Shade="Shade.Darker" Click=@(args => OnClick("Danger button")) Text="Danger" ButtonStyle="ButtonStyle.Danger" />
    </RadzenStack>
</RadzenStack>

<RadzenText TextStyle="TextStyle.Subtitle1" TagName="TagName.H3" class="rz-mt-4">Outlined Light and Dark</RadzenText>
<RadzenText TextStyle="TextStyle.Body2" class="rz-mb-4">Light and Dark button styles don't have Shades</RadzenText>
<RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="1rem" Wrap="FlexWrap.Wrap" class="rz-background-color-base-500 rz-p-4">
    <RadzenButton Variant="Variant.Outlined" Click=@(args => OnClick("Light button")) Text="Light" ButtonStyle="ButtonStyle.Light" />
    <RadzenButton Variant="Variant.Outlined" Click=@(args => OnClick("Dark button")) Text="Dark" ButtonStyle="ButtonStyle.Dark" />
</RadzenStack>

@code {
    private void OnClick(string text)
    {
        NotificationService.Notify(new NotificationMessage { Severity = NotificationSeverity.Info, Summary = "Button Clicked", Detail = text });
    }
}
```


### Text Buttons

Use `Variant="Variant.Text"` for text button variant.

```razor
<RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="1rem" Wrap="FlexWrap.Wrap">
    <RadzenButton Variant="Variant.Text" Click=@(args => OnClick("Primary button")) Text="Primary" ButtonStyle="ButtonStyle.Primary" />
    <RadzenButton Variant="Variant.Text" Click=@(args => OnClick("Secondary button")) Text="Secondary" ButtonStyle="ButtonStyle.Secondary" />
    <RadzenButton Variant="Variant.Text" Click=@(args => OnClick("Base button")) Text="Base" ButtonStyle="ButtonStyle.Base" />
    <RadzenButton Variant="Variant.Text" Click=@(args => OnClick("Info button")) Text="Info" ButtonStyle="ButtonStyle.Info" />
    <RadzenButton Variant="Variant.Text" Click=@(args => OnClick("Success button ")) Text="Success" ButtonStyle="ButtonStyle.Success" />
    <RadzenButton Variant="Variant.Text" Click=@(args => OnClick("Warning button ")) Text="Warning" ButtonStyle="ButtonStyle.Warning" />
    <RadzenButton Variant="Variant.Text" Click=@(args => OnClick("Danger button")) Text="Danger" ButtonStyle="ButtonStyle.Danger" />
</RadzenStack>

<RadzenText TextStyle="TextStyle.Subtitle1" TagName="TagName.H3" class="rz-mt-4">Text Shades</RadzenText>
<RadzenStack Gap="1rem">
    <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="1rem" Wrap="FlexWrap.Wrap">
        <RadzenButton Variant="Variant.Text" Shade="Shade.Lighter" Click=@(args => OnClick("Primary button")) Text="Primary" ButtonStyle="ButtonStyle.Primary" />
        <RadzenButton Variant="Variant.Text" Shade="Shade.Lighter" Click=@(args => OnClick("Secondary button")) Text="Secondary" ButtonStyle="ButtonStyle.Secondary" />
        <RadzenButton Variant="Variant.Text" Shade="Shade.Lighter" Click=@(args => OnClick("Base button")) Text="Base" ButtonStyle="ButtonStyle.Base" />
        <RadzenButton Variant="Variant.Text" Shade="Shade.Lighter" Click=@(args => OnClick("Info button")) Text="Info" ButtonStyle="ButtonStyle.Info" />
        <RadzenButton Variant="Variant.Text" Shade="Shade.Lighter" Click=@(args => OnClick("Success button ")) Text="Success" ButtonStyle="ButtonStyle.Success" />
        <RadzenButton Variant="Variant.Text" Shade="Shade.Lighter" Click=@(args => OnClick("Warning button ")) Text="Warning" ButtonStyle="ButtonStyle.Warning" />
        <RadzenButton Variant="Variant.Text" Shade="Shade.Lighter" Click=@(args => OnClick("Danger button")) Text="Danger" ButtonStyle="ButtonStyle.Danger" />
    </RadzenStack>
    <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="1rem" Wrap="FlexWrap.Wrap">
        <RadzenButton Variant="Variant.Text" Shade="Shade.Light" Click=@(args => OnClick("Primary button")) Text="Primary" ButtonStyle="ButtonStyle.Primary" />
        <RadzenButton Variant="Variant.Text" Shade="Shade.Light" Click=@(args => OnClick("Secondary button")) Text="Secondary" ButtonStyle="ButtonStyle.Secondary" />
        <RadzenButton Variant="Variant.Text" Shade="Shade.Light" Click=@(args => OnClick("Base button")) Text="Base" ButtonStyle="ButtonStyle.Base" />
        <RadzenButton Variant="Variant.Text" Shade="Shade.Light" Click=@(args => OnClick("Info button")) Text="Info" ButtonStyle="ButtonStyle.Info" />
        <RadzenButton Variant="Variant.Text" Shade="Shade.Light" Click=@(args => OnClick("Success button ")) Text="Success" ButtonStyle="ButtonStyle.Success" />
        <RadzenButton Variant="Variant.Text" Shade="Shade.Light" Click=@(args => OnClick("Warning button ")) Text="Warning" ButtonStyle="ButtonStyle.Warning" />
        <RadzenButton Variant="Variant.Text" Shade="Shade.Light" Click=@(args => OnClick("Danger button")) Text="Danger" ButtonStyle="ButtonStyle.Danger" />
    </RadzenStack>
    <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="1rem" Wrap="FlexWrap.Wrap">
        <RadzenButton Variant="Variant.Text" Shade="Shade.Dark" Click=@(args => OnClick("Primary button")) Text="Primary" ButtonStyle="ButtonStyle.Primary" />
        <RadzenButton Variant="Variant.Text" Shade="Shade.Dark" Click=@(args => OnClick("Secondary button")) Text="Secondary" ButtonStyle="ButtonStyle.Secondary" />
        <RadzenButton Variant="Variant.Text" Shade="Shade.Dark" Click=@(args => OnClick("Base button")) Text="Base" ButtonStyle="ButtonStyle.Base" />
        <RadzenButton Variant="Variant.Text" Shade="Shade.Dark" Click=@(args => OnClick("Info button")) Text="Info" ButtonStyle="ButtonStyle.Info" />
        <RadzenButton Variant="Variant.Text" Shade="Shade.Dark" Click=@(args => OnClick("Success button ")) Text="Success" ButtonStyle="ButtonStyle.Success" />
        <RadzenButton Variant="Variant.Text" Shade="Shade.Dark" Click=@(args => OnClick("Warning button ")) Text="Warning" ButtonStyle="ButtonStyle.Warning" />
        <RadzenButton Variant="Variant.Text" Shade="Shade.Dark" Click=@(args => OnClick("Danger button")) Text="Danger" ButtonStyle="ButtonStyle.Danger" />
    </RadzenStack>
    <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="1rem" Wrap="FlexWrap.Wrap">
        <RadzenButton Variant="Variant.Text" Shade="Shade.Darker" Click=@(args => OnClick("Primary button")) Text="Primary" ButtonStyle="ButtonStyle.Primary" />
        <RadzenButton Variant="Variant.Text" Shade="Shade.Darker" Click=@(args => OnClick("Secondary button")) Text="Secondary" ButtonStyle="ButtonStyle.Secondary" />
        <RadzenButton Variant="Variant.Text" Shade="Shade.Darker" Click=@(args => OnClick("Base button")) Text="Base" ButtonStyle="ButtonStyle.Base" />
        <RadzenButton Variant="Variant.Text" Shade="Shade.Darker" Click=@(args => OnClick("Info button")) Text="Info" ButtonStyle="ButtonStyle.Info" />
        <RadzenButton Variant="Variant.Text" Shade="Shade.Darker" Click=@(args => OnClick("Success button ")) Text="Success" ButtonStyle="ButtonStyle.Success" />
        <RadzenButton Variant="Variant.Text" Shade="Shade.Darker" Click=@(args => OnClick("Warning button ")) Text="Warning" ButtonStyle="ButtonStyle.Warning" />
        <RadzenButton Variant="Variant.Text" Shade="Shade.Darker" Click=@(args => OnClick("Danger button")) Text="Danger" ButtonStyle="ButtonStyle.Danger" />
    </RadzenStack>
</RadzenStack>

<RadzenText TextStyle="TextStyle.Subtitle1" TagName="TagName.H3" class="rz-mt-4">Text Light and Dark</RadzenText>
<RadzenText TextStyle="TextStyle.Body2" class="rz-mb-4">Light and Dark button styles don't have Shades</RadzenText>
<RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="1rem" Wrap="FlexWrap.Wrap" class="rz-background-color-base-500 rz-p-4">
    <RadzenButton Variant="Variant.Text" Click=@(args => OnClick("Light button")) Text="Light" ButtonStyle="ButtonStyle.Light" />
    <RadzenButton Variant="Variant.Text" Click=@(args => OnClick("Dark button")) Text="Dark" ButtonStyle="ButtonStyle.Dark" />
</RadzenStack>

@code {
    private void OnClick(string text)
    {
        NotificationService.Notify(new NotificationMessage { Severity = NotificationSeverity.Info, Summary = "Button Clicked", Detail = text });
    }
}
```


### Content in Buttons

Text, icons and images can be added to a button.

```razor
<RadzenText TextStyle="TextStyle.Subtitle1" TagName="TagName.H3" class="rz-mt-4">Icon only button</RadzenText>
<RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="1rem" Wrap="FlexWrap.Wrap">
    <RadzenButton Click=@(args => OnClick("Primary icon button")) Icon="add_circle" ButtonStyle="ButtonStyle.Primary" />
    <RadzenButton Click=@(args => OnClick("Secondary icon button")) Icon="add_circle" ButtonStyle="ButtonStyle.Secondary" />
    <RadzenButton Click=@(args => OnClick("Base icon button")) Icon="refresh" ButtonStyle="ButtonStyle.Base" />
    <RadzenButton Click=@(args => OnClick("Info icon button")) Icon="privacy_tip" ButtonStyle="ButtonStyle.Info" />
    <RadzenButton Click=@(args => OnClick("Success icon button ")) Icon="check_circle" ButtonStyle="ButtonStyle.Success" />
    <RadzenButton Click=@(args => OnClick("Warning icon button ")) Icon="warning_amber" ButtonStyle="ButtonStyle.Warning" />
    <RadzenButton Click=@(args => OnClick("Danger icon button")) Icon="report" ButtonStyle="ButtonStyle.Danger" />
</RadzenStack>

<RadzenText TextStyle="TextStyle.Subtitle1" TagName="TagName.H3" class="rz-mt-4">Icon and text button</RadzenText>
<RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="1rem" Wrap="FlexWrap.Wrap">
    <RadzenButton Click=@(args => OnClick("Primary button with text and icon")) Text="Add New" Icon="add_circle" ButtonStyle="ButtonStyle.Primary" />
    <RadzenButton Click=@(args => OnClick("Secondary button with text and icon")) Text="Add New" Icon="add_circle" ButtonStyle="ButtonStyle.Secondary" />
    <RadzenButton Click=@(args => OnClick("Base button with text and icon")) Text="Refresh" Icon="refresh" ButtonStyle="ButtonStyle.Base" />
    <RadzenButton Click=@(args => OnClick("Info button with text and icon")) Text="Privacy tip" Icon="privacy_tip" ButtonStyle="ButtonStyle.Info" />
    <RadzenButton Click=@(args => OnClick("Success button with text and icon")) Text="Publish" Icon="check_circle" ButtonStyle="ButtonStyle.Success" />
    <RadzenButton Click=@(args => OnClick("Warning button with text and icon")) Text="Warning" Icon="warning_amber" ButtonStyle="ButtonStyle.Warning" />
    <RadzenButton Click=@(args => OnClick("Danger button with text and icon")) Text="Report" Icon="report" ButtonStyle="ButtonStyle.Danger" />
</RadzenStack>

<RadzenText TextStyle="TextStyle.Subtitle1" TagName="TagName.H3" class="rz-mt-4">Images</RadzenText>
<RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="1rem" Wrap="FlexWrap.Wrap">
    <RadzenButton Click=@(args => OnClick("Button with image")) Image="images/radzen-nuget.png" ButtonStyle="ButtonStyle.Base" />
    <RadzenButton Click=@(args => OnClick("Button with content")) Image="images/radzen-nuget.png" ButtonStyle="ButtonStyle.Base">
        <span class="rz-button-text">Button with content</span>
        <RadzenImage Path="images/radzen-nuget.png" Style="width: 20px; height: 20px; margin-inline-start: 8px;" />
    </RadzenButton>
</RadzenStack>

@code {
    private void OnClick(string text)
    {
        NotificationService.Notify(new NotificationMessage { Severity = NotificationSeverity.Info, Summary = "Button Clicked", Detail = text });
    }
}
```


### Button Sizes

Use the `Size` property to set button size. Available sizes are ExtraSmall, Small, Medium (default), and Large.

```razor
<RadzenText TextStyle="TextStyle.Subtitle1" TagName="TagName.H3">Icon</RadzenText>
<RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="1rem" Wrap="FlexWrap.Wrap">
    <RadzenButton Click=@(args => OnClick("Large icon button")) Icon="add" ButtonStyle="ButtonStyle.Primary" Size="ButtonSize.Large" />
    <RadzenButton Click=@(args => OnClick("Medium icon button")) Icon="add" ButtonStyle="ButtonStyle.Primary" Size="ButtonSize.Medium" />
    <RadzenButton Click=@(args => OnClick("Small icon button")) Icon="add" ButtonStyle="ButtonStyle.Primary" Size="ButtonSize.Small" />
    <RadzenButton Click=@(args => OnClick("Extra Small icon button")) Icon="add" ButtonStyle="ButtonStyle.Primary" Size="ButtonSize.ExtraSmall" />
</RadzenStack>

<RadzenText TextStyle="TextStyle.Subtitle1" TagName="TagName.H3" class="rz-mt-4">Text</RadzenText>
<RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="1rem" Wrap="FlexWrap.Wrap">
    <RadzenButton Click=@(args => OnClick("Large Button")) Text="Create" Size="ButtonSize.Large" />
    <RadzenButton Click=@(args => OnClick("Medium Button")) Text="Create" Size="ButtonSize.Medium" />
    <RadzenButton Click=@(args => OnClick("Small Button")) Text="Create" Size="ButtonSize.Small" />
    <RadzenButton Click=@(args => OnClick("Extra Small Button")) Text="Create" Size="ButtonSize.ExtraSmall" />
</RadzenStack>

<RadzenText TextStyle="TextStyle.Subtitle1" TagName="TagName.H3" class="rz-mt-4">Icon and Text</RadzenText>
<RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="1rem" Wrap="FlexWrap.Wrap">
    <RadzenButton Click=@(args => OnClick("Large button with text and icon")) Text="Create" Icon="add" ButtonStyle="ButtonStyle.Primary" Size="ButtonSize.Large" />
    <RadzenButton Click=@(args => OnClick("Medium button with text and icon")) Text="Create" Icon="add" ButtonStyle="ButtonStyle.Primary" Size="ButtonSize.Medium" />
    <RadzenButton Click=@(args => OnClick("Small button with text and icon")) Text="Create" Icon="add" ButtonStyle="ButtonStyle.Primary" Size="ButtonSize.Small" />
    <RadzenButton Click=@(args => OnClick("Extra Small button with text and icon")) Text="Create" Icon="add" ButtonStyle="ButtonStyle.Primary" Size="ButtonSize.ExtraSmall" />
</RadzenStack>

@code {
    private void OnClick(string text)
    {
        NotificationService.Notify(new NotificationMessage { Severity = NotificationSeverity.Info, Summary = "Button Clicked", Detail = text });
    }
}
```


### FAB-like button

See how you can achieve Floating action button look and feel. See also the standalone [RadzenFab](/fab) and [RadzenFabMenu](/fab-menu) components.

```razor
<RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" JustifyContent="JustifyContent.Center" Gap="3rem" Wrap="FlexWrap.Wrap" class="rz-p-12">
    <RadzenButton Click=@(args => OnClick("Extra Small FAB")) Icon="add" ButtonStyle="ButtonStyle.Primary" Size="ButtonSize.ExtraSmall" class="rz-border-radius-10 rz-shadow-4"/>
    <RadzenButton Click=@(args => OnClick("Small FAB")) Icon="add" ButtonStyle="ButtonStyle.Primary" Size="ButtonSize.Small" class="rz-border-radius-10 rz-shadow-6"/>
    <RadzenButton Click=@(args => OnClick("Medium FAB")) Icon="add" ButtonStyle="ButtonStyle.Primary" Size="ButtonSize.Medium" class="rz-border-radius-10 rz-shadow-8"/>
    <RadzenButton Click=@(args => OnClick("Large FAB")) Icon="add" ButtonStyle="ButtonStyle.Primary" Size="ButtonSize.Large" class="rz-border-radius-10 rz-shadow-10"/>
</RadzenStack>

@code {
    private void OnClick(string text)
    {
        NotificationService.Notify(new NotificationMessage { Severity = NotificationSeverity.Info, Summary = "Button Clicked", Detail = text });
    }
}
```


### Disabled Button

Use `Disabled="true"` to disable a button.

```razor
<RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" JustifyContent="JustifyContent.Center" Gap="1rem" Wrap="FlexWrap.Wrap" class="rz-p-12">
    <RadzenButton Disabled="true" Text="Primary" ButtonStyle="ButtonStyle.Primary" />
    <RadzenButton Disabled="true" Text="Secondary" ButtonStyle="ButtonStyle.Secondary" />
    <RadzenButton Disabled="true" Text="Base" ButtonStyle="ButtonStyle.Base" />
    <RadzenButton Disabled="true" Text="Info" ButtonStyle="ButtonStyle.Info" />
    <RadzenButton Disabled="true" Text="Success" ButtonStyle="ButtonStyle.Success" />
    <RadzenButton Disabled="true" Text="Warning" ButtonStyle="ButtonStyle.Warning" />
    <RadzenButton Disabled="true" Text="Danger" ButtonStyle="ButtonStyle.Danger" />
</RadzenStack>

@code {
    private void OnClick(string text)
    {
        NotificationService.Notify(new NotificationMessage { Severity = NotificationSeverity.Info, Summary = "Button Clicked", Detail = text });
    }
}
```


### Busy button

Use `IsBusy="true"` to show the busy indicator.

```razor
<RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" JustifyContent="JustifyContent.Center" Gap="3rem" Wrap="FlexWrap.Wrap" class="rz-p-12">
    <RadzenButton style="width: 160px" IsBusy=@busy Click=@OnBusyClick Text="Save" />
    <RadzenButton style="width: 160px" Icon="save" BusyText="Saving ..." IsBusy=@busy Click=@OnBusyClick Text="Save" />
</RadzenStack>

@code {
    bool busy;
    
    private void OnClick(string text)
    {
        NotificationService.Notify(new NotificationMessage { Severity = NotificationSeverity.Info, Summary = "Button Clicked", Detail = text });
    }

    async Task OnBusyClick()
    {
        busy = true;
        await Task.Delay(2000);
        busy = false;
    }
}
```
