# SplitButton

The Blazor SplitButton pairs a primary action with a dropdown menu of additional options.

Keywords: button, menu, dropdown, split, form

> API reference: [RadzenSplitButton API](https://blazor.radzen.com/api/splitbutton.md)

## Examples

## Blazor SplitButton

The Blazor SplitButton pairs a primary action with a dropdown menu of additional options.

### Filled SplitButton

These are the default Radzen SplitButton.

```razor
<RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="1rem" Wrap="FlexWrap.Wrap">
    <RadzenSplitButton Click=@(args => OnClick(args, "Primary split button")) Text="Primary">
        <ChildContent>
            <RadzenSplitButtonItem Text="Item1" Value="1" />
            <RadzenSplitButtonItem Text="Item2" Value="2" />
        </ChildContent>
    </RadzenSplitButton>
    <RadzenSplitButton Click=@(args => OnClick(args, "Secondary split button")) Text="Secondary" ButtonStyle="ButtonStyle.Secondary">
        <ChildContent>
            <RadzenSplitButtonItem Text="Item1" Value="1" />
            <RadzenSplitButtonItem Text="Item2" Value="2" />
        </ChildContent>
    </RadzenSplitButton>
    <RadzenSplitButton Click=@(args => OnClick(args, "Info split button")) Text="Info" ButtonStyle="ButtonStyle.Info">
        <ChildContent>
            <RadzenSplitButtonItem Text="Item1" Value="1" />
            <RadzenSplitButtonItem Text="Item2" Value="2" />
        </ChildContent>
    </RadzenSplitButton>
    <RadzenSplitButton Click=@(args => OnClick(args, "Success split button")) Text="Success" ButtonStyle="ButtonStyle.Success">
        <ChildContent>
            <RadzenSplitButtonItem Text="Item1" Value="1" />
            <RadzenSplitButtonItem Text="Item2" Value="2" />
        </ChildContent>
    </RadzenSplitButton>
    <RadzenSplitButton Click=@(args => OnClick(args, "Warning split button")) Text="Warning" ButtonStyle="ButtonStyle.Warning">
        <ChildContent>
            <RadzenSplitButtonItem Text="Item1" Value="1" />
            <RadzenSplitButtonItem Text="Item2" Value="2" />
        </ChildContent>
    </RadzenSplitButton>
    <RadzenSplitButton Click=@(args => OnClick(args, "Danger split button")) Text="Danger" ButtonStyle="ButtonStyle.Danger">
        <ChildContent>
            <RadzenSplitButtonItem Text="Item1" Value="1" />
            <RadzenSplitButtonItem Text="Item2" Value="2" />
        </ChildContent>
    </RadzenSplitButton>
</RadzenStack>

<RadzenText TextStyle="TextStyle.Subtitle1" TagName="TagName.H3" class="rz-mt-4">Filled Shades</RadzenText>
<RadzenStack Gap="1rem">
    <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="1rem" Wrap="FlexWrap.Wrap">
        <RadzenSplitButton Click=@(args => OnClick(args, "Primary split button")) Text="Primary" Shade="Shade.Lighter">
            <ChildContent>
                <RadzenSplitButtonItem Text="Item1" Value="1" />
                <RadzenSplitButtonItem Text="Item2" Value="2" />
            </ChildContent>
        </RadzenSplitButton>
        <RadzenSplitButton Click=@(args => OnClick(args, "Secondary split button")) Text="Secondary" ButtonStyle="ButtonStyle.Secondary" Shade="Shade.Lighter">
            <ChildContent>
                <RadzenSplitButtonItem Text="Item1" Value="1" />
                <RadzenSplitButtonItem Text="Item2" Value="2" />
            </ChildContent>
        </RadzenSplitButton>
        <RadzenSplitButton Click=@(args => OnClick(args, "Info split button")) Text="Info" ButtonStyle="ButtonStyle.Info" Shade="Shade.Lighter">
            <ChildContent>
                <RadzenSplitButtonItem Text="Item1" Value="1" />
                <RadzenSplitButtonItem Text="Item2" Value="2" />
            </ChildContent>
        </RadzenSplitButton>
        <RadzenSplitButton Click=@(args => OnClick(args, "Success split button")) Text="Success" ButtonStyle="ButtonStyle.Success" Shade="Shade.Lighter">
            <ChildContent>
                <RadzenSplitButtonItem Text="Item1" Value="1" />
                <RadzenSplitButtonItem Text="Item2" Value="2" />
            </ChildContent>
        </RadzenSplitButton>
        <RadzenSplitButton Click=@(args => OnClick(args, "Warning split button")) Text="Warning" ButtonStyle="ButtonStyle.Warning" Shade="Shade.Lighter">
            <ChildContent>
                <RadzenSplitButtonItem Text="Item1" Value="1" />
                <RadzenSplitButtonItem Text="Item2" Value="2" />
            </ChildContent>
        </RadzenSplitButton>
        <RadzenSplitButton Click=@(args => OnClick(args, "Danger split button")) Text="Danger" ButtonStyle="ButtonStyle.Danger" Shade="Shade.Lighter">
            <ChildContent>
                <RadzenSplitButtonItem Text="Item1" Value="1" />
                <RadzenSplitButtonItem Text="Item2" Value="2" />
            </ChildContent>
        </RadzenSplitButton>
    </RadzenStack>
    <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="1rem" Wrap="FlexWrap.Wrap">
        <RadzenSplitButton Click=@(args => OnClick(args, "Primary split button")) Text="Primary" Shade="Shade.Light">
            <ChildContent>
                <RadzenSplitButtonItem Text="Item1" Value="1" />
                <RadzenSplitButtonItem Text="Item2" Value="2" />
            </ChildContent>
        </RadzenSplitButton>
        <RadzenSplitButton Click=@(args => OnClick(args, "Secondary split button")) Text="Secondary" ButtonStyle="ButtonStyle.Secondary" Shade="Shade.Light">
            <ChildContent>
                <RadzenSplitButtonItem Text="Item1" Value="1" />
                <RadzenSplitButtonItem Text="Item2" Value="2" />
            </ChildContent>
        </RadzenSplitButton>
        <RadzenSplitButton Click=@(args => OnClick(args, "Info split button")) Text="Info" ButtonStyle="ButtonStyle.Info" Shade="Shade.Light">
            <ChildContent>
                <RadzenSplitButtonItem Text="Item1" Value="1" />
                <RadzenSplitButtonItem Text="Item2" Value="2" />
            </ChildContent>
        </RadzenSplitButton>
        <RadzenSplitButton Click=@(args => OnClick(args, "Success split button")) Text="Success" ButtonStyle="ButtonStyle.Success" Shade="Shade.Light">
            <ChildContent>
                <RadzenSplitButtonItem Text="Item1" Value="1" />
                <RadzenSplitButtonItem Text="Item2" Value="2" />
            </ChildContent>
        </RadzenSplitButton>
        <RadzenSplitButton Click=@(args => OnClick(args, "Warning split button")) Text="Warning" ButtonStyle="ButtonStyle.Warning" Shade="Shade.Light">
            <ChildContent>
                <RadzenSplitButtonItem Text="Item1" Value="1" />
                <RadzenSplitButtonItem Text="Item2" Value="2" />
            </ChildContent>
        </RadzenSplitButton>
        <RadzenSplitButton Click=@(args => OnClick(args, "Danger split button")) Text="Danger" ButtonStyle="ButtonStyle.Danger" Shade="Shade.Light">
            <ChildContent>
                <RadzenSplitButtonItem Text="Item1" Value="1" />
                <RadzenSplitButtonItem Text="Item2" Value="2" />
            </ChildContent>
        </RadzenSplitButton>
    </RadzenStack>
    <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="1rem" Wrap="FlexWrap.Wrap">
        <RadzenSplitButton Click=@(args => OnClick(args, "Primary split button")) Text="Primary" Shade="Shade.Dark">
            <ChildContent>
                <RadzenSplitButtonItem Text="Item1" Value="1" />
                <RadzenSplitButtonItem Text="Item2" Value="2" />
            </ChildContent>
        </RadzenSplitButton>
        <RadzenSplitButton Click=@(args => OnClick(args, "Secondary split button")) Text="Secondary" ButtonStyle="ButtonStyle.Secondary" Shade="Shade.Dark">
            <ChildContent>
                <RadzenSplitButtonItem Text="Item1" Value="1" />
                <RadzenSplitButtonItem Text="Item2" Value="2" />
            </ChildContent>
        </RadzenSplitButton>
        <RadzenSplitButton Click=@(args => OnClick(args, "Info split button")) Text="Info" ButtonStyle="ButtonStyle.Info" Shade="Shade.Dark">
            <ChildContent>
                <RadzenSplitButtonItem Text="Item1" Value="1" />
                <RadzenSplitButtonItem Text="Item2" Value="2" />
            </ChildContent>
        </RadzenSplitButton>
        <RadzenSplitButton Click=@(args => OnClick(args, "Success split button")) Text="Success" ButtonStyle="ButtonStyle.Success" Shade="Shade.Dark">
            <ChildContent>
                <RadzenSplitButtonItem Text="Item1" Value="1" />
                <RadzenSplitButtonItem Text="Item2" Value="2" />
            </ChildContent>
        </RadzenSplitButton>
        <RadzenSplitButton Click=@(args => OnClick(args, "Warning split button")) Text="Warning" ButtonStyle="ButtonStyle.Warning" Shade="Shade.Dark">
            <ChildContent>
                <RadzenSplitButtonItem Text="Item1" Value="1" />
                <RadzenSplitButtonItem Text="Item2" Value="2" />
            </ChildContent>
        </RadzenSplitButton>
        <RadzenSplitButton Click=@(args => OnClick(args, "Danger split button")) Text="Danger" ButtonStyle="ButtonStyle.Danger" Shade="Shade.Dark">
            <ChildContent>
                <RadzenSplitButtonItem Text="Item1" Value="1" />
                <RadzenSplitButtonItem Text="Item2" Value="2" />
            </ChildContent>
        </RadzenSplitButton>
    </RadzenStack>
    <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="1rem" Wrap="FlexWrap.Wrap">
        <RadzenSplitButton Click=@(args => OnClick(args, "Primary split button")) Text="Primary" Shade="Shade.Darker">
            <ChildContent>
                <RadzenSplitButtonItem Text="Item1" Value="1" />
                <RadzenSplitButtonItem Text="Item2" Value="2" />
            </ChildContent>
        </RadzenSplitButton>
        <RadzenSplitButton Click=@(args => OnClick(args, "Secondary split button")) Text="Secondary" ButtonStyle="ButtonStyle.Secondary" Shade="Shade.Darker">
            <ChildContent>
                <RadzenSplitButtonItem Text="Item1" Value="1" />
                <RadzenSplitButtonItem Text="Item2" Value="2" />
            </ChildContent>
        </RadzenSplitButton>
        <RadzenSplitButton Click=@(args => OnClick(args, "Info split button")) Text="Info" ButtonStyle="ButtonStyle.Info" Shade="Shade.Darker">
            <ChildContent>
                <RadzenSplitButtonItem Text="Item1" Value="1" />
                <RadzenSplitButtonItem Text="Item2" Value="2" />
            </ChildContent>
        </RadzenSplitButton>
        <RadzenSplitButton Click=@(args => OnClick(args, "Success split button")) Text="Success" ButtonStyle="ButtonStyle.Success" Shade="Shade.Darker">
            <ChildContent>
                <RadzenSplitButtonItem Text="Item1" Value="1" />
                <RadzenSplitButtonItem Text="Item2" Value="2" />
            </ChildContent>
        </RadzenSplitButton>
        <RadzenSplitButton Click=@(args => OnClick(args, "Warning split button")) Text="Warning" ButtonStyle="ButtonStyle.Warning" Shade="Shade.Darker">
            <ChildContent>
                <RadzenSplitButtonItem Text="Item1" Value="1" />
                <RadzenSplitButtonItem Text="Item2" Value="2" />
            </ChildContent>
        </RadzenSplitButton>
        <RadzenSplitButton Click=@(args => OnClick(args, "Danger split button")) Text="Danger" ButtonStyle="ButtonStyle.Danger" Shade="Shade.Darker">
            <ChildContent>
                <RadzenSplitButtonItem Text="Item1" Value="1" />
                <RadzenSplitButtonItem Text="Item2" Value="2" />
            </ChildContent>
        </RadzenSplitButton>
    </RadzenStack>
</RadzenStack>

<RadzenText TextStyle="TextStyle.Subtitle1" TagName="TagName.H3" class="rz-mt-4">Filled Light and Dark</RadzenText>
<RadzenText TextStyle="TextStyle.Body2" class="rz-mb-4">Light and Dark button styles don't have Shades</RadzenText>
<RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="1rem" Wrap="FlexWrap.Wrap">
    <RadzenSplitButton Click=@(args => OnClick(args, "Light split button")) Text="Light" ButtonStyle="ButtonStyle.Light">
        <ChildContent>
            <RadzenSplitButtonItem Text="Item1" Value="1" />
            <RadzenSplitButtonItem Text="Item2" Value="2" />
        </ChildContent>
    </RadzenSplitButton>
    <RadzenSplitButton Click=@(args => OnClick(args, "Dark split button")) Text="Dark" ButtonStyle="ButtonStyle.Dark">
        <ChildContent>
            <RadzenSplitButtonItem Text="Item1" Value="1" />
            <RadzenSplitButtonItem Text="Item2" Value="2" />
        </ChildContent>
    </RadzenSplitButton>
</RadzenStack>

@code {
    void OnClick(RadzenSplitButtonItem item, string buttonName)
    {
        if (item != null)
        {
            NotificationService.Notify(new NotificationMessage { Severity = NotificationSeverity.Info, Summary = "SplitButton Item Clicked", Detail = $"{buttonName}, item with value {item.Value} clicked" });
        }
        else
        {
            NotificationService.Notify(new NotificationMessage { Severity = NotificationSeverity.Info, Summary = "SplitButton Clicked", Detail = $"{buttonName} clicked" });
        }
    }
}
```


### Flat SplitButton

Use `Variant="Variant.Flat"` for flat split button variant.

```razor
<RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="1rem" Wrap="FlexWrap.Wrap">
    <RadzenSplitButton Variant="Variant.Flat" Click=@(args => OnClick(args, "Primary split button")) Text="Primary">
        <ChildContent>
            <RadzenSplitButtonItem Text="Item1" Value="1" />
            <RadzenSplitButtonItem Text="Item2" Value="2" />
        </ChildContent>
    </RadzenSplitButton>
    <RadzenSplitButton Variant="Variant.Flat" Click=@(args => OnClick(args, "Secondary split button")) Text="Secondary" ButtonStyle="ButtonStyle.Secondary">
        <ChildContent>
            <RadzenSplitButtonItem Text="Item1" Value="1" />
            <RadzenSplitButtonItem Text="Item2" Value="2" />
        </ChildContent>
    </RadzenSplitButton>
    <RadzenSplitButton Variant="Variant.Flat" Click=@(args => OnClick(args, "Info split button")) Text="Info" ButtonStyle="ButtonStyle.Info">
        <ChildContent>
            <RadzenSplitButtonItem Text="Item1" Value="1" />
            <RadzenSplitButtonItem Text="Item2" Value="2" />
        </ChildContent>
    </RadzenSplitButton>
    <RadzenSplitButton Variant="Variant.Flat" Click=@(args => OnClick(args, "Success split button")) Text="Success" ButtonStyle="ButtonStyle.Success">
        <ChildContent>
            <RadzenSplitButtonItem Text="Item1" Value="1" />
            <RadzenSplitButtonItem Text="Item2" Value="2" />
        </ChildContent>
    </RadzenSplitButton>
    <RadzenSplitButton Variant="Variant.Flat" Click=@(args => OnClick(args, "Warning split button")) Text="Warning" ButtonStyle="ButtonStyle.Warning">
        <ChildContent>
            <RadzenSplitButtonItem Text="Item1" Value="1" />
            <RadzenSplitButtonItem Text="Item2" Value="2" />
        </ChildContent>
    </RadzenSplitButton>
    <RadzenSplitButton Variant="Variant.Flat" Click=@(args => OnClick(args, "Danger split button")) Text="Danger" ButtonStyle="ButtonStyle.Danger">
        <ChildContent>
            <RadzenSplitButtonItem Text="Item1" Value="1" />
            <RadzenSplitButtonItem Text="Item2" Value="2" />
        </ChildContent>
    </RadzenSplitButton>
</RadzenStack>

<RadzenText TextStyle="TextStyle.Subtitle1" TagName="TagName.H3" class="rz-mt-4">Flat Shades</RadzenText>
<RadzenStack Gap="1rem">
    <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="1rem" Wrap="FlexWrap.Wrap">
        <RadzenSplitButton Variant="Variant.Flat" Click=@(args => OnClick(args, "Primary split button")) Text="Primary" Shade="Shade.Lighter">
            <ChildContent>
                <RadzenSplitButtonItem Text="Item1" Value="1" />
                <RadzenSplitButtonItem Text="Item2" Value="2" />
            </ChildContent>
        </RadzenSplitButton>
        <RadzenSplitButton Variant="Variant.Flat" Click=@(args => OnClick(args, "Secondary split button")) Text="Secondary" ButtonStyle="ButtonStyle.Secondary" Shade="Shade.Lighter">
            <ChildContent>
                <RadzenSplitButtonItem Text="Item1" Value="1" />
                <RadzenSplitButtonItem Text="Item2" Value="2" />
            </ChildContent>
        </RadzenSplitButton>
        <RadzenSplitButton Variant="Variant.Flat" Click=@(args => OnClick(args, "Info split button")) Text="Info" ButtonStyle="ButtonStyle.Info" Shade="Shade.Lighter">
            <ChildContent>
                <RadzenSplitButtonItem Text="Item1" Value="1" />
                <RadzenSplitButtonItem Text="Item2" Value="2" />
            </ChildContent>
        </RadzenSplitButton>
        <RadzenSplitButton Variant="Variant.Flat" Click=@(args => OnClick(args, "Success split button")) Text="Success" ButtonStyle="ButtonStyle.Success" Shade="Shade.Lighter">
            <ChildContent>
                <RadzenSplitButtonItem Text="Item1" Value="1" />
                <RadzenSplitButtonItem Text="Item2" Value="2" />
            </ChildContent>
        </RadzenSplitButton>
        <RadzenSplitButton Variant="Variant.Flat" Click=@(args => OnClick(args, "Warning split button")) Text="Warning" ButtonStyle="ButtonStyle.Warning" Shade="Shade.Lighter">
            <ChildContent>
                <RadzenSplitButtonItem Text="Item1" Value="1" />
                <RadzenSplitButtonItem Text="Item2" Value="2" />
            </ChildContent>
        </RadzenSplitButton>
        <RadzenSplitButton Variant="Variant.Flat" Click=@(args => OnClick(args, "Danger split button")) Text="Danger" ButtonStyle="ButtonStyle.Danger" Shade="Shade.Lighter">
            <ChildContent>
                <RadzenSplitButtonItem Text="Item1" Value="1" />
                <RadzenSplitButtonItem Text="Item2" Value="2" />
            </ChildContent>
        </RadzenSplitButton>
    </RadzenStack>
    <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="1rem" Wrap="FlexWrap.Wrap">
        <RadzenSplitButton Variant="Variant.Flat" Click=@(args => OnClick(args, "Primary split button")) Text="Primary" Shade="Shade.Light">
            <ChildContent>
                <RadzenSplitButtonItem Text="Item1" Value="1" />
                <RadzenSplitButtonItem Text="Item2" Value="2" />
            </ChildContent>
        </RadzenSplitButton>
        <RadzenSplitButton Variant="Variant.Flat" Click=@(args => OnClick(args, "Secondary split button")) Text="Secondary" ButtonStyle="ButtonStyle.Secondary" Shade="Shade.Light">
            <ChildContent>
                <RadzenSplitButtonItem Text="Item1" Value="1" />
                <RadzenSplitButtonItem Text="Item2" Value="2" />
            </ChildContent>
        </RadzenSplitButton>
        <RadzenSplitButton Variant="Variant.Flat" Click=@(args => OnClick(args, "Info split button")) Text="Info" ButtonStyle="ButtonStyle.Info" Shade="Shade.Light">
            <ChildContent>
                <RadzenSplitButtonItem Text="Item1" Value="1" />
                <RadzenSplitButtonItem Text="Item2" Value="2" />
            </ChildContent>
        </RadzenSplitButton>
        <RadzenSplitButton Variant="Variant.Flat" Click=@(args => OnClick(args, "Success split button")) Text="Success" ButtonStyle="ButtonStyle.Success" Shade="Shade.Light">
            <ChildContent>
                <RadzenSplitButtonItem Text="Item1" Value="1" />
                <RadzenSplitButtonItem Text="Item2" Value="2" />
            </ChildContent>
        </RadzenSplitButton>
        <RadzenSplitButton Variant="Variant.Flat" Click=@(args => OnClick(args, "Warning split button")) Text="Warning" ButtonStyle="ButtonStyle.Warning" Shade="Shade.Light">
            <ChildContent>
                <RadzenSplitButtonItem Text="Item1" Value="1" />
                <RadzenSplitButtonItem Text="Item2" Value="2" />
            </ChildContent>
        </RadzenSplitButton>
        <RadzenSplitButton Variant="Variant.Flat" Click=@(args => OnClick(args, "Danger split button")) Text="Danger" ButtonStyle="ButtonStyle.Danger" Shade="Shade.Light">
            <ChildContent>
                <RadzenSplitButtonItem Text="Item1" Value="1" />
                <RadzenSplitButtonItem Text="Item2" Value="2" />
            </ChildContent>
        </RadzenSplitButton>
    </RadzenStack>
    <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="1rem" Wrap="FlexWrap.Wrap">
        <RadzenSplitButton Variant="Variant.Flat" Click=@(args => OnClick(args, "Primary split button")) Text="Primary" Shade="Shade.Dark">
            <ChildContent>
                <RadzenSplitButtonItem Text="Item1" Value="1" />
                <RadzenSplitButtonItem Text="Item2" Value="2" />
            </ChildContent>
        </RadzenSplitButton>
        <RadzenSplitButton Variant="Variant.Flat" Click=@(args => OnClick(args, "Secondary split button")) Text="Secondary" ButtonStyle="ButtonStyle.Secondary" Shade="Shade.Dark">
            <ChildContent>
                <RadzenSplitButtonItem Text="Item1" Value="1" />
                <RadzenSplitButtonItem Text="Item2" Value="2" />
            </ChildContent>
        </RadzenSplitButton>
        <RadzenSplitButton Variant="Variant.Flat" Click=@(args => OnClick(args, "Info split button")) Text="Info" ButtonStyle="ButtonStyle.Info" Shade="Shade.Dark">
            <ChildContent>
                <RadzenSplitButtonItem Text="Item1" Value="1" />
                <RadzenSplitButtonItem Text="Item2" Value="2" />
            </ChildContent>
        </RadzenSplitButton>
        <RadzenSplitButton Variant="Variant.Flat" Click=@(args => OnClick(args, "Success split button")) Text="Success" ButtonStyle="ButtonStyle.Success" Shade="Shade.Dark">
            <ChildContent>
                <RadzenSplitButtonItem Text="Item1" Value="1" />
                <RadzenSplitButtonItem Text="Item2" Value="2" />
            </ChildContent>
        </RadzenSplitButton>
        <RadzenSplitButton Variant="Variant.Flat" Click=@(args => OnClick(args, "Warning split button")) Text="Warning" ButtonStyle="ButtonStyle.Warning" Shade="Shade.Dark">
            <ChildContent>
                <RadzenSplitButtonItem Text="Item1" Value="1" />
                <RadzenSplitButtonItem Text="Item2" Value="2" />
            </ChildContent>
        </RadzenSplitButton>
        <RadzenSplitButton Variant="Variant.Flat" Click=@(args => OnClick(args, "Danger split button")) Text="Danger" ButtonStyle="ButtonStyle.Danger" Shade="Shade.Dark">
            <ChildContent>
                <RadzenSplitButtonItem Text="Item1" Value="1" />
                <RadzenSplitButtonItem Text="Item2" Value="2" />
            </ChildContent>
        </RadzenSplitButton>
    </RadzenStack>
    <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="1rem" Wrap="FlexWrap.Wrap">
        <RadzenSplitButton Variant="Variant.Flat" Click=@(args => OnClick(args, "Primary split button")) Text="Primary" Shade="Shade.Darker">
            <ChildContent>
                <RadzenSplitButtonItem Text="Item1" Value="1" />
                <RadzenSplitButtonItem Text="Item2" Value="2" />
            </ChildContent>
        </RadzenSplitButton>
        <RadzenSplitButton Variant="Variant.Flat" Click=@(args => OnClick(args, "Secondary split button")) Text="Secondary" ButtonStyle="ButtonStyle.Secondary" Shade="Shade.Darker">
            <ChildContent>
                <RadzenSplitButtonItem Text="Item1" Value="1" />
                <RadzenSplitButtonItem Text="Item2" Value="2" />
            </ChildContent>
        </RadzenSplitButton>
        <RadzenSplitButton Variant="Variant.Flat" Click=@(args => OnClick(args, "Info split button")) Text="Info" ButtonStyle="ButtonStyle.Info" Shade="Shade.Darker">
            <ChildContent>
                <RadzenSplitButtonItem Text="Item1" Value="1" />
                <RadzenSplitButtonItem Text="Item2" Value="2" />
            </ChildContent>
        </RadzenSplitButton>
        <RadzenSplitButton Variant="Variant.Flat" Click=@(args => OnClick(args, "Success split button")) Text="Success" ButtonStyle="ButtonStyle.Success" Shade="Shade.Darker">
            <ChildContent>
                <RadzenSplitButtonItem Text="Item1" Value="1" />
                <RadzenSplitButtonItem Text="Item2" Value="2" />
            </ChildContent>
        </RadzenSplitButton>
        <RadzenSplitButton Variant="Variant.Flat" Click=@(args => OnClick(args, "Warning split button")) Text="Warning" ButtonStyle="ButtonStyle.Warning" Shade="Shade.Darker">
            <ChildContent>
                <RadzenSplitButtonItem Text="Item1" Value="1" />
                <RadzenSplitButtonItem Text="Item2" Value="2" />
            </ChildContent>
        </RadzenSplitButton>
        <RadzenSplitButton Variant="Variant.Flat" Click=@(args => OnClick(args, "Danger split button")) Text="Danger" ButtonStyle="ButtonStyle.Danger" Shade="Shade.Darker">
            <ChildContent>
                <RadzenSplitButtonItem Text="Item1" Value="1" />
                <RadzenSplitButtonItem Text="Item2" Value="2" />
            </ChildContent>
        </RadzenSplitButton>
    </RadzenStack>
</RadzenStack>

<RadzenText TextStyle="TextStyle.Subtitle1" TagName="TagName.H3" class="rz-mt-4">Flat Light and Dark</RadzenText>
<RadzenText TextStyle="TextStyle.Body2" class="rz-mb-4">Light and Dark button styles don't have Shades</RadzenText>
<RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="1rem" Wrap="FlexWrap.Wrap">
    <RadzenSplitButton Variant="Variant.Flat" Click=@(args => OnClick(args, "Light split button")) Text="Light" ButtonStyle="ButtonStyle.Light">
        <ChildContent>
            <RadzenSplitButtonItem Text="Item1" Value="1" />
            <RadzenSplitButtonItem Text="Item2" Value="2" />
        </ChildContent>
    </RadzenSplitButton>
    <RadzenSplitButton Variant="Variant.Flat" Click=@(args => OnClick(args, "Dark split button")) Text="Dark" ButtonStyle="ButtonStyle.Dark">
        <ChildContent>
            <RadzenSplitButtonItem Text="Item1" Value="1" />
            <RadzenSplitButtonItem Text="Item2" Value="2" />
        </ChildContent>
    </RadzenSplitButton>
</RadzenStack>

@code {
    void OnClick(RadzenSplitButtonItem item, string buttonName)
    {
        if (item != null)
        {
            NotificationService.Notify(new NotificationMessage { Severity = NotificationSeverity.Info, Summary = "SplitButton Item Clicked", Detail = $"{buttonName}, item with value {item.Value} clicked" });
        }
        else
        {
            NotificationService.Notify(new NotificationMessage { Severity = NotificationSeverity.Info, Summary = "SplitButton Clicked", Detail = $"{buttonName} clicked" });
        }
    }
}
```


### Outlined SplitButton

Use `Variant="Variant.Outlined"` for outlined split button variant.

```razor
<RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="1rem" Wrap="FlexWrap.Wrap">
    <RadzenSplitButton Click=@(args => OnClick(args, "Primary split button")) Text="Primary" Variant="Variant.Outlined">
        <ChildContent>
            <RadzenSplitButtonItem Text="Item1" Value="1" />
            <RadzenSplitButtonItem Text="Item2" Value="2" />
        </ChildContent>
    </RadzenSplitButton>
    <RadzenSplitButton Click=@(args => OnClick(args, "Secondary split button")) Text="Secondary" ButtonStyle="ButtonStyle.Secondary" Variant="Variant.Outlined">
        <ChildContent>
            <RadzenSplitButtonItem Text="Item1" Value="1" />
            <RadzenSplitButtonItem Text="Item2" Value="2" />
        </ChildContent>
    </RadzenSplitButton>
    <RadzenSplitButton Click=@(args => OnClick(args, "Info split button")) Text="Info" ButtonStyle="ButtonStyle.Info" Variant="Variant.Outlined">
        <ChildContent>
            <RadzenSplitButtonItem Text="Item1" Value="1" />
            <RadzenSplitButtonItem Text="Item2" Value="2" />
        </ChildContent>
    </RadzenSplitButton>
    <RadzenSplitButton Click=@(args => OnClick(args, "Success split button")) Text="Success" ButtonStyle="ButtonStyle.Success" Variant="Variant.Outlined">
        <ChildContent>
            <RadzenSplitButtonItem Text="Item1" Value="1" />
            <RadzenSplitButtonItem Text="Item2" Value="2" />
        </ChildContent>
    </RadzenSplitButton>
    <RadzenSplitButton Click=@(args => OnClick(args, "Warning split button")) Text="Warning" ButtonStyle="ButtonStyle.Warning" Variant="Variant.Outlined">
        <ChildContent>
            <RadzenSplitButtonItem Text="Item1" Value="1" />
            <RadzenSplitButtonItem Text="Item2" Value="2" />
        </ChildContent>
    </RadzenSplitButton>
    <RadzenSplitButton Click=@(args => OnClick(args, "Danger split button")) Text="Danger" ButtonStyle="ButtonStyle.Danger" Variant="Variant.Outlined">
        <ChildContent>
            <RadzenSplitButtonItem Text="Item1" Value="1" />
            <RadzenSplitButtonItem Text="Item2" Value="2" />
        </ChildContent>
    </RadzenSplitButton>
</RadzenStack>

<RadzenText TextStyle="TextStyle.Subtitle1" TagName="TagName.H3" class="rz-mt-4">Outlined Shades</RadzenText>
<RadzenStack Gap="1rem">
    <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="1rem" Wrap="FlexWrap.Wrap">
        <RadzenSplitButton Click=@(args => OnClick(args, "Primary split button")) Text="Primary" Shade="Shade.Lighter" Variant="Variant.Outlined">
            <ChildContent>
                <RadzenSplitButtonItem Text="Item1" Value="1" />
                <RadzenSplitButtonItem Text="Item2" Value="2" />
            </ChildContent>
        </RadzenSplitButton>
        <RadzenSplitButton Click=@(args => OnClick(args, "Secondary split button")) Text="Secondary" ButtonStyle="ButtonStyle.Secondary" Shade="Shade.Lighter" Variant="Variant.Outlined">
            <ChildContent>
                <RadzenSplitButtonItem Text="Item1" Value="1" />
                <RadzenSplitButtonItem Text="Item2" Value="2" />
            </ChildContent>
        </RadzenSplitButton>
        <RadzenSplitButton Click=@(args => OnClick(args, "Info split button")) Text="Info" ButtonStyle="ButtonStyle.Info" Shade="Shade.Lighter" Variant="Variant.Outlined">
            <ChildContent>
                <RadzenSplitButtonItem Text="Item1" Value="1" />
                <RadzenSplitButtonItem Text="Item2" Value="2" />
            </ChildContent>
        </RadzenSplitButton>
        <RadzenSplitButton Click=@(args => OnClick(args, "Success split button")) Text="Success" ButtonStyle="ButtonStyle.Success" Shade="Shade.Lighter" Variant="Variant.Outlined">
            <ChildContent>
                <RadzenSplitButtonItem Text="Item1" Value="1" />
                <RadzenSplitButtonItem Text="Item2" Value="2" />
            </ChildContent>
        </RadzenSplitButton>
        <RadzenSplitButton Click=@(args => OnClick(args, "Warning split button")) Text="Warning" ButtonStyle="ButtonStyle.Warning" Shade="Shade.Lighter" Variant="Variant.Outlined">
            <ChildContent>
                <RadzenSplitButtonItem Text="Item1" Value="1" />
                <RadzenSplitButtonItem Text="Item2" Value="2" />
            </ChildContent>
        </RadzenSplitButton>
        <RadzenSplitButton Click=@(args => OnClick(args, "Danger split button")) Text="Danger" ButtonStyle="ButtonStyle.Danger" Shade="Shade.Lighter" Variant="Variant.Outlined">
            <ChildContent>
                <RadzenSplitButtonItem Text="Item1" Value="1" />
                <RadzenSplitButtonItem Text="Item2" Value="2" />
            </ChildContent>
        </RadzenSplitButton>
    </RadzenStack>
    <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="1rem" Wrap="FlexWrap.Wrap">
        <RadzenSplitButton Click=@(args => OnClick(args, "Primary split button")) Text="Primary" Shade="Shade.Light" Variant="Variant.Outlined">
            <ChildContent>
                <RadzenSplitButtonItem Text="Item1" Value="1" />
                <RadzenSplitButtonItem Text="Item2" Value="2" />
            </ChildContent>
        </RadzenSplitButton>
        <RadzenSplitButton Click=@(args => OnClick(args, "Secondary split button")) Text="Secondary" ButtonStyle="ButtonStyle.Secondary" Shade="Shade.Light" Variant="Variant.Outlined">
            <ChildContent>
                <RadzenSplitButtonItem Text="Item1" Value="1" />
                <RadzenSplitButtonItem Text="Item2" Value="2" />
            </ChildContent>
        </RadzenSplitButton>
        <RadzenSplitButton Click=@(args => OnClick(args, "Info split button")) Text="Info" ButtonStyle="ButtonStyle.Info" Shade="Shade.Light" Variant="Variant.Outlined">
            <ChildContent>
                <RadzenSplitButtonItem Text="Item1" Value="1" />
                <RadzenSplitButtonItem Text="Item2" Value="2" />
            </ChildContent>
        </RadzenSplitButton>
        <RadzenSplitButton Click=@(args => OnClick(args, "Success split button")) Text="Success" ButtonStyle="ButtonStyle.Success" Shade="Shade.Light" Variant="Variant.Outlined">
            <ChildContent>
                <RadzenSplitButtonItem Text="Item1" Value="1" />
                <RadzenSplitButtonItem Text="Item2" Value="2" />
            </ChildContent>
        </RadzenSplitButton>
        <RadzenSplitButton Click=@(args => OnClick(args, "Warning split button")) Text="Warning" ButtonStyle="ButtonStyle.Warning" Shade="Shade.Light" Variant="Variant.Outlined">
            <ChildContent>
                <RadzenSplitButtonItem Text="Item1" Value="1" />
                <RadzenSplitButtonItem Text="Item2" Value="2" />
            </ChildContent>
        </RadzenSplitButton>
        <RadzenSplitButton Click=@(args => OnClick(args, "Danger split button")) Text="Danger" ButtonStyle="ButtonStyle.Danger" Shade="Shade.Light" Variant="Variant.Outlined">
            <ChildContent>
                <RadzenSplitButtonItem Text="Item1" Value="1" />
                <RadzenSplitButtonItem Text="Item2" Value="2" />
            </ChildContent>
        </RadzenSplitButton>
    </RadzenStack>
    <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="1rem" Wrap="FlexWrap.Wrap">
        <RadzenSplitButton Click=@(args => OnClick(args, "Primary split button")) Text="Primary" Shade="Shade.Dark" Variant="Variant.Outlined">
            <ChildContent>
                <RadzenSplitButtonItem Text="Item1" Value="1" />
                <RadzenSplitButtonItem Text="Item2" Value="2" />
            </ChildContent>
        </RadzenSplitButton>
        <RadzenSplitButton Click=@(args => OnClick(args, "Secondary split button")) Text="Secondary" ButtonStyle="ButtonStyle.Secondary" Shade="Shade.Dark" Variant="Variant.Outlined">
            <ChildContent>
                <RadzenSplitButtonItem Text="Item1" Value="1" />
                <RadzenSplitButtonItem Text="Item2" Value="2" />
            </ChildContent>
        </RadzenSplitButton>
        <RadzenSplitButton Click=@(args => OnClick(args, "Info split button")) Text="Info" ButtonStyle="ButtonStyle.Info" Shade="Shade.Dark" Variant="Variant.Outlined">
            <ChildContent>
                <RadzenSplitButtonItem Text="Item1" Value="1" />
                <RadzenSplitButtonItem Text="Item2" Value="2" />
            </ChildContent>
        </RadzenSplitButton>
        <RadzenSplitButton Click=@(args => OnClick(args, "Success split button")) Text="Success" ButtonStyle="ButtonStyle.Success" Shade="Shade.Dark" Variant="Variant.Outlined">
            <ChildContent>
                <RadzenSplitButtonItem Text="Item1" Value="1" />
                <RadzenSplitButtonItem Text="Item2" Value="2" />
            </ChildContent>
        </RadzenSplitButton>
        <RadzenSplitButton Click=@(args => OnClick(args, "Warning split button")) Text="Warning" ButtonStyle="ButtonStyle.Warning" Shade="Shade.Dark" Variant="Variant.Outlined">
            <ChildContent>
                <RadzenSplitButtonItem Text="Item1" Value="1" />
                <RadzenSplitButtonItem Text="Item2" Value="2" />
            </ChildContent>
        </RadzenSplitButton>
        <RadzenSplitButton Click=@(args => OnClick(args, "Danger split button")) Text="Danger" ButtonStyle="ButtonStyle.Danger" Shade="Shade.Dark" Variant="Variant.Outlined">
            <ChildContent>
                <RadzenSplitButtonItem Text="Item1" Value="1" />
                <RadzenSplitButtonItem Text="Item2" Value="2" />
            </ChildContent>
        </RadzenSplitButton>
    </RadzenStack>
    <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="1rem" Wrap="FlexWrap.Wrap">
        <RadzenSplitButton Click=@(args => OnClick(args, "Primary split button")) Text="Primary" Shade="Shade.Darker" Variant="Variant.Outlined">
            <ChildContent>
                <RadzenSplitButtonItem Text="Item1" Value="1" />
                <RadzenSplitButtonItem Text="Item2" Value="2" />
            </ChildContent>
        </RadzenSplitButton>
        <RadzenSplitButton Click=@(args => OnClick(args, "Secondary split button")) Text="Secondary" ButtonStyle="ButtonStyle.Secondary" Shade="Shade.Darker" Variant="Variant.Outlined">
            <ChildContent>
                <RadzenSplitButtonItem Text="Item1" Value="1" />
                <RadzenSplitButtonItem Text="Item2" Value="2" />
            </ChildContent>
        </RadzenSplitButton>
        <RadzenSplitButton Click=@(args => OnClick(args, "Info split button")) Text="Info" ButtonStyle="ButtonStyle.Info" Shade="Shade.Darker" Variant="Variant.Outlined">
            <ChildContent>
                <RadzenSplitButtonItem Text="Item1" Value="1" />
                <RadzenSplitButtonItem Text="Item2" Value="2" />
            </ChildContent>
        </RadzenSplitButton>
        <RadzenSplitButton Click=@(args => OnClick(args, "Success split button")) Text="Success" ButtonStyle="ButtonStyle.Success" Shade="Shade.Darker" Variant="Variant.Outlined">
            <ChildContent>
                <RadzenSplitButtonItem Text="Item1" Value="1" />
                <RadzenSplitButtonItem Text="Item2" Value="2" />
            </ChildContent>
        </RadzenSplitButton>
        <RadzenSplitButton Click=@(args => OnClick(args, "Warning split button")) Text="Warning" ButtonStyle="ButtonStyle.Warning" Shade="Shade.Darker" Variant="Variant.Outlined">
            <ChildContent>
                <RadzenSplitButtonItem Text="Item1" Value="1" />
                <RadzenSplitButtonItem Text="Item2" Value="2" />
            </ChildContent>
        </RadzenSplitButton>
        <RadzenSplitButton Click=@(args => OnClick(args, "Danger split button")) Text="Danger" ButtonStyle="ButtonStyle.Danger" Shade="Shade.Darker" Variant="Variant.Outlined">
            <ChildContent>
                <RadzenSplitButtonItem Text="Item1" Value="1" />
                <RadzenSplitButtonItem Text="Item2" Value="2" />
            </ChildContent>
        </RadzenSplitButton>
    </RadzenStack>
</RadzenStack>

<RadzenText TextStyle="TextStyle.Subtitle1" TagName="TagName.H3" class="rz-mt-4">Outlined Light and Dark</RadzenText>
<RadzenText TextStyle="TextStyle.Body2" class="rz-mb-4">Light and Dark button styles don't have Shades</RadzenText>
<RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="1rem" Wrap="FlexWrap.Wrap" class="rz-background-color-base-500 rz-p-4">
    <RadzenSplitButton Click=@(args => OnClick(args, "Light split button")) Text="Light" ButtonStyle="ButtonStyle.Light" Variant="Variant.Outlined">
        <ChildContent>
            <RadzenSplitButtonItem Text="Item1" Value="1" />
            <RadzenSplitButtonItem Text="Item2" Value="2" />
        </ChildContent>
    </RadzenSplitButton>
    <RadzenSplitButton Click=@(args => OnClick(args, "Dark split button")) Text="Dark" ButtonStyle="ButtonStyle.Dark" Variant="Variant.Outlined">
        <ChildContent>
            <RadzenSplitButtonItem Text="Item1" Value="1" />
            <RadzenSplitButtonItem Text="Item2" Value="2" />
        </ChildContent>
    </RadzenSplitButton>
</RadzenStack>

@code {
    void OnClick(RadzenSplitButtonItem item, string buttonName)
    {
        if (item != null)
        {
            NotificationService.Notify(new NotificationMessage { Severity = NotificationSeverity.Info, Summary = "SplitButton Item Clicked", Detail = $"{buttonName}, item with value {item.Value} clicked" });
        }
        else
        {
            NotificationService.Notify(new NotificationMessage { Severity = NotificationSeverity.Info, Summary = "SplitButton Clicked", Detail = $"{buttonName} clicked" });
        }
    }
}
```


### Text SplitButton

Use `Variant="Variant.Text"` for text split button variant.

```razor
<RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="1rem" Wrap="FlexWrap.Wrap">
    <RadzenSplitButton Click=@(args => OnClick(args, "Primary split button")) Text="Primary" Variant="Variant.Text">
        <ChildContent>
            <RadzenSplitButtonItem Text="Item1" Value="1" />
            <RadzenSplitButtonItem Text="Item2" Value="2" />
        </ChildContent>
    </RadzenSplitButton>
    <RadzenSplitButton Click=@(args => OnClick(args, "Secondary split button")) Text="Secondary" ButtonStyle="ButtonStyle.Secondary" Variant="Variant.Text">
        <ChildContent>
            <RadzenSplitButtonItem Text="Item1" Value="1" />
            <RadzenSplitButtonItem Text="Item2" Value="2" />
        </ChildContent>
    </RadzenSplitButton>
    <RadzenSplitButton Click=@(args => OnClick(args, "Info split button")) Text="Info" ButtonStyle="ButtonStyle.Info" Variant="Variant.Text">
        <ChildContent>
            <RadzenSplitButtonItem Text="Item1" Value="1" />
            <RadzenSplitButtonItem Text="Item2" Value="2" />
        </ChildContent>
    </RadzenSplitButton>
    <RadzenSplitButton Click=@(args => OnClick(args, "Success split button")) Text="Success" ButtonStyle="ButtonStyle.Success" Variant="Variant.Text">
        <ChildContent>
            <RadzenSplitButtonItem Text="Item1" Value="1" />
            <RadzenSplitButtonItem Text="Item2" Value="2" />
        </ChildContent>
    </RadzenSplitButton>
    <RadzenSplitButton Click=@(args => OnClick(args, "Warning split button")) Text="Warning" ButtonStyle="ButtonStyle.Warning" Variant="Variant.Text">
        <ChildContent>
            <RadzenSplitButtonItem Text="Item1" Value="1" />
            <RadzenSplitButtonItem Text="Item2" Value="2" />
        </ChildContent>
    </RadzenSplitButton>
    <RadzenSplitButton Click=@(args => OnClick(args, "Danger split button")) Text="Danger" ButtonStyle="ButtonStyle.Danger" Variant="Variant.Text">
        <ChildContent>
            <RadzenSplitButtonItem Text="Item1" Value="1" />
            <RadzenSplitButtonItem Text="Item2" Value="2" />
        </ChildContent>
    </RadzenSplitButton>
</RadzenStack>

<RadzenText TextStyle="TextStyle.Subtitle1" TagName="TagName.H3" class="rz-mt-4">Text Shades</RadzenText>
<RadzenStack Gap="1rem">
    <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="1rem" Wrap="FlexWrap.Wrap">
        <RadzenSplitButton Click=@(args => OnClick(args, "Primary split button")) Text="Primary" Shade="Shade.Lighter" Variant="Variant.Text">
            <ChildContent>
                <RadzenSplitButtonItem Text="Item1" Value="1" />
                <RadzenSplitButtonItem Text="Item2" Value="2" />
            </ChildContent>
        </RadzenSplitButton>
        <RadzenSplitButton Click=@(args => OnClick(args, "Secondary split button")) Text="Secondary" ButtonStyle="ButtonStyle.Secondary" Shade="Shade.Lighter" Variant="Variant.Text">
            <ChildContent>
                <RadzenSplitButtonItem Text="Item1" Value="1" />
                <RadzenSplitButtonItem Text="Item2" Value="2" />
            </ChildContent>
        </RadzenSplitButton>
        <RadzenSplitButton Click=@(args => OnClick(args, "Info split button")) Text="Info" ButtonStyle="ButtonStyle.Info" Shade="Shade.Lighter" Variant="Variant.Text">
            <ChildContent>
                <RadzenSplitButtonItem Text="Item1" Value="1" />
                <RadzenSplitButtonItem Text="Item2" Value="2" />
            </ChildContent>
        </RadzenSplitButton>
        <RadzenSplitButton Click=@(args => OnClick(args, "Success split button")) Text="Success" ButtonStyle="ButtonStyle.Success" Shade="Shade.Lighter" Variant="Variant.Text">
            <ChildContent>
                <RadzenSplitButtonItem Text="Item1" Value="1" />
                <RadzenSplitButtonItem Text="Item2" Value="2" />
            </ChildContent>
        </RadzenSplitButton>
        <RadzenSplitButton Click=@(args => OnClick(args, "Warning split button")) Text="Warning" ButtonStyle="ButtonStyle.Warning" Shade="Shade.Lighter" Variant="Variant.Text">
            <ChildContent>
                <RadzenSplitButtonItem Text="Item1" Value="1" />
                <RadzenSplitButtonItem Text="Item2" Value="2" />
            </ChildContent>
        </RadzenSplitButton>
        <RadzenSplitButton Click=@(args => OnClick(args, "Danger split button")) Text="Danger" ButtonStyle="ButtonStyle.Danger" Shade="Shade.Lighter" Variant="Variant.Text">
            <ChildContent>
                <RadzenSplitButtonItem Text="Item1" Value="1" />
                <RadzenSplitButtonItem Text="Item2" Value="2" />
            </ChildContent>
        </RadzenSplitButton>
    </RadzenStack>
    <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="1rem" Wrap="FlexWrap.Wrap">
        <RadzenSplitButton Click=@(args => OnClick(args, "Primary split button")) Text="Primary" Shade="Shade.Light" Variant="Variant.Text">
            <ChildContent>
                <RadzenSplitButtonItem Text="Item1" Value="1" />
                <RadzenSplitButtonItem Text="Item2" Value="2" />
            </ChildContent>
        </RadzenSplitButton>
        <RadzenSplitButton Click=@(args => OnClick(args, "Secondary split button")) Text="Secondary" ButtonStyle="ButtonStyle.Secondary" Shade="Shade.Light" Variant="Variant.Text">
            <ChildContent>
                <RadzenSplitButtonItem Text="Item1" Value="1" />
                <RadzenSplitButtonItem Text="Item2" Value="2" />
            </ChildContent>
        </RadzenSplitButton>
        <RadzenSplitButton Click=@(args => OnClick(args, "Info split button")) Text="Info" ButtonStyle="ButtonStyle.Info" Shade="Shade.Light" Variant="Variant.Text">
            <ChildContent>
                <RadzenSplitButtonItem Text="Item1" Value="1" />
                <RadzenSplitButtonItem Text="Item2" Value="2" />
            </ChildContent>
        </RadzenSplitButton>
        <RadzenSplitButton Click=@(args => OnClick(args, "Success split button")) Text="Success" ButtonStyle="ButtonStyle.Success" Shade="Shade.Light" Variant="Variant.Text">
            <ChildContent>
                <RadzenSplitButtonItem Text="Item1" Value="1" />
                <RadzenSplitButtonItem Text="Item2" Value="2" />
            </ChildContent>
        </RadzenSplitButton>
        <RadzenSplitButton Click=@(args => OnClick(args, "Warning split button")) Text="Warning" ButtonStyle="ButtonStyle.Warning" Shade="Shade.Light" Variant="Variant.Text">
            <ChildContent>
                <RadzenSplitButtonItem Text="Item1" Value="1" />
                <RadzenSplitButtonItem Text="Item2" Value="2" />
            </ChildContent>
        </RadzenSplitButton>
        <RadzenSplitButton Click=@(args => OnClick(args, "Danger split button")) Text="Danger" ButtonStyle="ButtonStyle.Danger" Shade="Shade.Light" Variant="Variant.Text">
            <ChildContent>
                <RadzenSplitButtonItem Text="Item1" Value="1" />
                <RadzenSplitButtonItem Text="Item2" Value="2" />
            </ChildContent>
        </RadzenSplitButton>
    </RadzenStack>
    <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="1rem" Wrap="FlexWrap.Wrap">
        <RadzenSplitButton Click=@(args => OnClick(args, "Primary split button")) Text="Primary" Shade="Shade.Dark" Variant="Variant.Text">
            <ChildContent>
                <RadzenSplitButtonItem Text="Item1" Value="1" />
                <RadzenSplitButtonItem Text="Item2" Value="2" />
            </ChildContent>
        </RadzenSplitButton>
        <RadzenSplitButton Click=@(args => OnClick(args, "Secondary split button")) Text="Secondary" ButtonStyle="ButtonStyle.Secondary" Shade="Shade.Dark" Variant="Variant.Text">
            <ChildContent>
                <RadzenSplitButtonItem Text="Item1" Value="1" />
                <RadzenSplitButtonItem Text="Item2" Value="2" />
            </ChildContent>
        </RadzenSplitButton>
        <RadzenSplitButton Click=@(args => OnClick(args, "Info split button")) Text="Info" ButtonStyle="ButtonStyle.Info" Shade="Shade.Dark" Variant="Variant.Text">
            <ChildContent>
                <RadzenSplitButtonItem Text="Item1" Value="1" />
                <RadzenSplitButtonItem Text="Item2" Value="2" />
            </ChildContent>
        </RadzenSplitButton>
        <RadzenSplitButton Click=@(args => OnClick(args, "Success split button")) Text="Success" ButtonStyle="ButtonStyle.Success" Shade="Shade.Dark" Variant="Variant.Text">
            <ChildContent>
                <RadzenSplitButtonItem Text="Item1" Value="1" />
                <RadzenSplitButtonItem Text="Item2" Value="2" />
            </ChildContent>
        </RadzenSplitButton>
        <RadzenSplitButton Click=@(args => OnClick(args, "Warning split button")) Text="Warning" ButtonStyle="ButtonStyle.Warning" Shade="Shade.Dark" Variant="Variant.Text">
            <ChildContent>
                <RadzenSplitButtonItem Text="Item1" Value="1" />
                <RadzenSplitButtonItem Text="Item2" Value="2" />
            </ChildContent>
        </RadzenSplitButton>
        <RadzenSplitButton Click=@(args => OnClick(args, "Danger split button")) Text="Danger" ButtonStyle="ButtonStyle.Danger" Shade="Shade.Dark" Variant="Variant.Text">
            <ChildContent>
                <RadzenSplitButtonItem Text="Item1" Value="1" />
                <RadzenSplitButtonItem Text="Item2" Value="2" />
            </ChildContent>
        </RadzenSplitButton>
    </RadzenStack>
    <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="1rem" Wrap="FlexWrap.Wrap">
        <RadzenSplitButton Click=@(args => OnClick(args, "Primary split button")) Text="Primary" Shade="Shade.Darker" Variant="Variant.Text">
            <ChildContent>
                <RadzenSplitButtonItem Text="Item1" Value="1" />
                <RadzenSplitButtonItem Text="Item2" Value="2" />
            </ChildContent>
        </RadzenSplitButton>
        <RadzenSplitButton Click=@(args => OnClick(args, "Secondary split button")) Text="Secondary" ButtonStyle="ButtonStyle.Secondary" Shade="Shade.Darker" Variant="Variant.Text">
            <ChildContent>
                <RadzenSplitButtonItem Text="Item1" Value="1" />
                <RadzenSplitButtonItem Text="Item2" Value="2" />
            </ChildContent>
        </RadzenSplitButton>
        <RadzenSplitButton Click=@(args => OnClick(args, "Info split button")) Text="Info" ButtonStyle="ButtonStyle.Info" Shade="Shade.Darker" Variant="Variant.Text">
            <ChildContent>
                <RadzenSplitButtonItem Text="Item1" Value="1" />
                <RadzenSplitButtonItem Text="Item2" Value="2" />
            </ChildContent>
        </RadzenSplitButton>
        <RadzenSplitButton Click=@(args => OnClick(args, "Success split button")) Text="Success" ButtonStyle="ButtonStyle.Success" Shade="Shade.Darker" Variant="Variant.Text">
            <ChildContent>
                <RadzenSplitButtonItem Text="Item1" Value="1" />
                <RadzenSplitButtonItem Text="Item2" Value="2" />
            </ChildContent>
        </RadzenSplitButton>
        <RadzenSplitButton Click=@(args => OnClick(args, "Warning split button")) Text="Warning" ButtonStyle="ButtonStyle.Warning" Shade="Shade.Darker" Variant="Variant.Text">
            <ChildContent>
                <RadzenSplitButtonItem Text="Item1" Value="1" />
                <RadzenSplitButtonItem Text="Item2" Value="2" />
            </ChildContent>
        </RadzenSplitButton>
        <RadzenSplitButton Click=@(args => OnClick(args, "Danger split button")) Text="Danger" ButtonStyle="ButtonStyle.Danger" Shade="Shade.Darker" Variant="Variant.Text">
            <ChildContent>
                <RadzenSplitButtonItem Text="Item1" Value="1" />
                <RadzenSplitButtonItem Text="Item2" Value="2" />
            </ChildContent>
        </RadzenSplitButton>
    </RadzenStack>
</RadzenStack>

<RadzenText TextStyle="TextStyle.Subtitle1" TagName="TagName.H3" class="rz-mt-4">Text Light and Dark</RadzenText>
<RadzenText TextStyle="TextStyle.Body2" class="rz-mb-4">Light and Dark button styles don't have Shades</RadzenText>
<RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="1rem" Wrap="FlexWrap.Wrap" class="rz-background-color-base-500 rz-p-4">
    <RadzenSplitButton Click=@(args => OnClick(args, "Light split button")) Text="Light" ButtonStyle="ButtonStyle.Light" Variant="Variant.Text">
        <ChildContent>
            <RadzenSplitButtonItem Text="Item1" Value="1" />
            <RadzenSplitButtonItem Text="Item2" Value="2" />
        </ChildContent>
    </RadzenSplitButton>
    <RadzenSplitButton Click=@(args => OnClick(args, "Dark split button")) Text="Dark" ButtonStyle="ButtonStyle.Dark" Variant="Variant.Text">
        <ChildContent>
            <RadzenSplitButtonItem Text="Item1" Value="1" />
            <RadzenSplitButtonItem Text="Item2" Value="2" />
        </ChildContent>
    </RadzenSplitButton>
</RadzenStack>

@code {
    void OnClick(RadzenSplitButtonItem item, string buttonName)
    {
        if (item != null)
        {
            NotificationService.Notify(new NotificationMessage { Severity = NotificationSeverity.Info, Summary = "SplitButton Item Clicked", Detail = $"{buttonName}, item with value {item.Value} clicked" });
        }
        else
        {
            NotificationService.Notify(new NotificationMessage { Severity = NotificationSeverity.Info, Summary = "SplitButton Clicked", Detail = $"{buttonName} clicked" });
        }
    }
}
```


### Content in SplitButton

Text, icons and images can be added to a split button.

```razor
<RadzenText TextStyle="TextStyle.Subtitle1" TagName="TagName.H3" class="rz-mt-4">Icon only button</RadzenText>
<RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="1rem" Wrap="FlexWrap.Wrap">
    <RadzenSplitButton Click=@(args => OnClick(args, "SplitButton with icon")) Icon="account_circle">
        <ChildContent>
            <RadzenSplitButtonItem Text="Item1" Value="1" Icon="account_box" />
            <RadzenSplitButtonItem Text="Item2" Value="2" Icon="account_balance_wallet" />
        </ChildContent>
    </RadzenSplitButton>
</RadzenStack>

<RadzenText TextStyle="TextStyle.Subtitle1" TagName="TagName.H3" class="rz-mt-4">Icon and text button</RadzenText>
<RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="1rem" Wrap="FlexWrap.Wrap">
    <RadzenSplitButton Click=@(args => OnClick(args, "SplitButton with text and icon")) Text="SplitButton" Icon="account_circle">
        <ChildContent>
            <RadzenSplitButtonItem Text="Item1" Value="1" Icon="account_box" />
            <RadzenSplitButtonItem Text="Item2" Value="2" Icon="account_balance_wallet" />
        </ChildContent>
    </RadzenSplitButton>
</RadzenStack>

<RadzenText TextStyle="TextStyle.Subtitle1" TagName="TagName.H3" class="rz-mt-4">Images</RadzenText>
<RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="1rem" Wrap="FlexWrap.Wrap">
    <RadzenSplitButton Click=@(args => OnClick(args, "SplitButton with image")) Text="Radzen" Image="images/radzen-nuget.png">
        <ChildContent>
            <RadzenSplitButtonItem Text="Item1" Value="1" Icon="account_box" />
            <RadzenSplitButtonItem Text="Item2" Value="2" Icon="account_balance_wallet" />
        </ChildContent>
    </RadzenSplitButton>
</RadzenStack>

@code {
    void OnClick(RadzenSplitButtonItem item, string buttonName)
    {
        if (item != null)
        {
            NotificationService.Notify(new NotificationMessage { Severity = NotificationSeverity.Info, Summary = "SplitButton Item Clicked", Detail = $"{buttonName}, item with value {item.Value} clicked" });
        }
        else
        {
            NotificationService.Notify(new NotificationMessage { Severity = NotificationSeverity.Info, Summary = "SplitButton Clicked", Detail = $"{buttonName} clicked" });
        }
    }
}
```


### SplitButton Sizes

Use the `Size` property to set split button size. Available sizes are ExtraSmall, Small, Medium (default), and Large.

```razor
<RadzenText TextStyle="TextStyle.Subtitle1" TagName="TagName.H3">Icon</RadzenText>
<RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="1rem" Wrap="FlexWrap.Wrap">
    <RadzenSplitButton Click=@(args => OnClick(args, "SplitButton with icon")) Icon="account_circle" Size="ButtonSize.Large">
        <ChildContent>
            <RadzenSplitButtonItem Text="Item1" Value="1" />
            <RadzenSplitButtonItem Text="Item2" Value="2" />
        </ChildContent>
    </RadzenSplitButton>
    <RadzenSplitButton Click=@(args => OnClick(args, "SplitButton with icon")) Icon="account_circle" Size="ButtonSize.Medium">
        <ChildContent>
            <RadzenSplitButtonItem Text="Item1" Value="1" />
            <RadzenSplitButtonItem Text="Item2" Value="2" />
        </ChildContent>
    </RadzenSplitButton>
    <RadzenSplitButton Click=@(args => OnClick(args, "SplitButton with icon")) Icon="account_circle" Size="ButtonSize.Small">
        <ChildContent>
            <RadzenSplitButtonItem Text="Item1" Value="1" />
            <RadzenSplitButtonItem Text="Item2" Value="2" />
        </ChildContent>
    </RadzenSplitButton>
    <RadzenSplitButton Click=@(args => OnClick(args, "SplitButton with icon")) Icon="account_circle" Size="ButtonSize.ExtraSmall">
        <ChildContent>
            <RadzenSplitButtonItem Text="Item1" Value="1" />
            <RadzenSplitButtonItem Text="Item2" Value="2" />
        </ChildContent>
    </RadzenSplitButton>
</RadzenStack>

<RadzenText TextStyle="TextStyle.Subtitle1" TagName="TagName.H3" class="rz-mt-4">Text</RadzenText>
<RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="1rem" Wrap="FlexWrap.Wrap">
    <RadzenSplitButton Click=@(args => OnClick(args, "SplitButton with text")) Text="Large" Size="ButtonSize.Large">
        <ChildContent>
            <RadzenSplitButtonItem Text="Item1" Value="1" />
            <RadzenSplitButtonItem Text="Item2" Value="2" />
        </ChildContent>
    </RadzenSplitButton>
    <RadzenSplitButton Click=@(args => OnClick(args, "SplitButton with text")) Text="Medium" Size="ButtonSize.Medium">
        <ChildContent>
            <RadzenSplitButtonItem Text="Item1" Value="1" />
            <RadzenSplitButtonItem Text="Item2" Value="2" />
        </ChildContent>
    </RadzenSplitButton>
    <RadzenSplitButton Click=@(args => OnClick(args, "SplitButton with text")) Text="Small" Size="ButtonSize.Small">
        <ChildContent>
            <RadzenSplitButtonItem Text="Item1" Value="1" />
            <RadzenSplitButtonItem Text="Item2" Value="2" />
        </ChildContent>
    </RadzenSplitButton>
    <RadzenSplitButton Click=@(args => OnClick(args, "SplitButton with text")) Text="ExtraSmall" Size="ButtonSize.ExtraSmall">
        <ChildContent>
            <RadzenSplitButtonItem Text="Item1" Value="1" />
            <RadzenSplitButtonItem Text="Item2" Value="2" />
        </ChildContent>
    </RadzenSplitButton>
</RadzenStack>

<RadzenText TextStyle="TextStyle.Subtitle1" TagName="TagName.H3" class="rz-mt-4">Icon and Text</RadzenText>
<RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="1rem" Wrap="FlexWrap.Wrap">
    <RadzenSplitButton Click=@(args => OnClick(args, "SplitButton with text and icon")) Text="SplitButton" Icon="account_circle" Size="ButtonSize.Large">
        <ChildContent>
            <RadzenSplitButtonItem Text="Item1" Value="1" />
            <RadzenSplitButtonItem Text="Item2" Value="2" />
        </ChildContent>
    </RadzenSplitButton>
    <RadzenSplitButton Click=@(args => OnClick(args, "SplitButton with text and icon")) Text="SplitButton" Icon="account_circle" Size="ButtonSize.Medium">
        <ChildContent>
            <RadzenSplitButtonItem Text="Item1" Value="1" />
            <RadzenSplitButtonItem Text="Item2" Value="2" />
        </ChildContent>
    </RadzenSplitButton>
    <RadzenSplitButton Click=@(args => OnClick(args, "SplitButton with text and icon")) Text="SplitButton" Icon="account_circle" Size="ButtonSize.Small">
        <ChildContent>
            <RadzenSplitButtonItem Text="Item1" Value="1" />
            <RadzenSplitButtonItem Text="Item2" Value="2" />
        </ChildContent>
    </RadzenSplitButton>
    <RadzenSplitButton Click=@(args => OnClick(args, "SplitButton with text and icon")) Text="SplitButton" Icon="account_circle" Size="ButtonSize.ExtraSmall">
        <ChildContent>
            <RadzenSplitButtonItem Text="Item1" Value="1" />
            <RadzenSplitButtonItem Text="Item2" Value="2" />
        </ChildContent>
    </RadzenSplitButton>
</RadzenStack>

@code {
    void OnClick(RadzenSplitButtonItem item, string buttonName)
    {
        if (item != null)
        {
            NotificationService.Notify(new NotificationMessage { Severity = NotificationSeverity.Info, Summary = "SplitButton Item Clicked", Detail = $"{buttonName}, item with value {item.Value} clicked" });
        }
        else
        {
            NotificationService.Notify(new NotificationMessage { Severity = NotificationSeverity.Info, Summary = "SplitButton Clicked", Detail = $"{buttonName} clicked" });
        }
    }
}
```


### Disabled SplitButton

Use `Disabled="true"` to disable a split button.

```razor
<RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" JustifyContent="JustifyContent.Center" Gap="1rem" Wrap="FlexWrap.Wrap" class="rz-p-12">
    <RadzenSplitButton Disabled="true" Click=@(args => OnClick(args, "Disabled split button")) Text="Primary">
        <ChildContent>
            <RadzenSplitButtonItem Text="Item1" Value="1" />
            <RadzenSplitButtonItem Text="Item2" Value="2" />
        </ChildContent>
    </RadzenSplitButton>

    <RadzenSplitButton Click=@(args => OnClick(args, "Disabled split button item")) Text="Primary">
        <ChildContent>
            <RadzenSplitButtonItem Text="Item1" Value="1" />
            <RadzenSplitButtonItem Text="Disabled Item2" Value="2" Disabled=true />
        </ChildContent>
    </RadzenSplitButton>
</RadzenStack>

@code {
    void OnClick(RadzenSplitButtonItem item, string buttonName)
    {
        if (item != null)
        {
            NotificationService.Notify(new NotificationMessage { Severity = NotificationSeverity.Info, Summary = "SplitButton Item Clicked", Detail = $"{buttonName}, item with value {item.Value} clicked" });
        }
        else
        {
            NotificationService.Notify(new NotificationMessage { Severity = NotificationSeverity.Info, Summary = "SplitButton Clicked", Detail = $"{buttonName} clicked" });
        }
    }
}
```


### Busy SplitButton

Use `IsBusy="true"` to show the busy indicator.

```razor
<RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" JustifyContent="JustifyContent.Center" Gap="3rem" Wrap="FlexWrap.Wrap" class="rz-p-12">
    <RadzenSplitButton IsBusy=@busy Click=@OnBusyClick Text="Save">
        <ChildContent>
            <RadzenSplitButtonItem Text="Item1" Value="1" />
            <RadzenSplitButtonItem Text="Item2" Value="2" />
        </ChildContent>
    </RadzenSplitButton>
    <RadzenSplitButton Icon="save" BusyText="Saving ..." IsBusy=@busy Click=@OnBusyClick Text="Save">
        <ChildContent>
            <RadzenSplitButtonItem Text="Item1" Value="1" />
            <RadzenSplitButtonItem Text="Item2" Value="2" />
        </ChildContent>
    </RadzenSplitButton>
</RadzenStack>

@code {
    bool busy;

    async Task OnBusyClick()
    {
        busy = true;
        await Task.Delay(2000);
        busy = false;
    }
}
```


### AlwaysOpenPopup SplitButton

Use `AlwaysOpenPopup="true"` to open popup with items on click.

```razor
<RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" JustifyContent="JustifyContent.Center" Gap="1rem" Wrap="FlexWrap.Wrap" class="rz-p-12">
    <RadzenSplitButton Click=@(args => OnClick(args, "Primary split button")) AlwaysOpenPopup=true Text="Primary">
        <ChildContent>
            <RadzenSplitButtonItem Text="Item1" Value="1" />
            <RadzenSplitButtonItem Text="Item2" Value="2" />
        </ChildContent>
    </RadzenSplitButton>
</RadzenStack>

@code {
    void OnClick(RadzenSplitButtonItem item, string buttonName)
    {
        if (item != null)
        {
            NotificationService.Notify(new NotificationMessage { Severity = NotificationSeverity.Info, Summary = "SplitButton Item Clicked", Detail = $"{buttonName}, item with value {item.Value} clicked" });
        }
        else
        {
            NotificationService.Notify(new NotificationMessage { Severity = NotificationSeverity.Info, Summary = "SplitButton Clicked", Detail = $"{buttonName} clicked" });
        }
    }
}
```


### DropDown icon of SplitButton

Customize the dropdown icon of SplitButton. Use `DropDownIcon` parameter to set the icon.

```razor
<RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" JustifyContent="JustifyContent.Center" Gap="1rem" Wrap="FlexWrap.Wrap" class="rz-p-12">
    <RadzenSplitButton Click=@(args => OnClick(args, "Primary split button")) Text="Primary" DropDownIcon="keyboard_double_arrow_down">
        <ChildContent>
            <RadzenSplitButtonItem Text="Item1" Value="1" />
            <RadzenSplitButtonItem Text="Item2" Value="2" />
        </ChildContent>
    </RadzenSplitButton>
    
    <RadzenSplitButton Click=@(args => OnClick(args, "Primary split button")) Text="Primary" DropDownIcon="expand_circle_down">
        <ChildContent>
            <RadzenSplitButtonItem Text="Item1" Value="1" />
            <RadzenSplitButtonItem Text="Item2" Value="2" />
        </ChildContent>
    </RadzenSplitButton>
    
    <RadzenSplitButton Click=@(args => OnClick(args, "Primary split button")) Text="Primary" DropDownIcon="keyboard_arrow_down">
        <ChildContent>
            <RadzenSplitButtonItem Text="Item1" Value="1" />
            <RadzenSplitButtonItem Text="Item2" Value="2" />
        </ChildContent>
    </RadzenSplitButton>
</RadzenStack>

@code {
    void OnClick(RadzenSplitButtonItem item, string buttonName)
    {
        if (item != null)
        {
            NotificationService.Notify(new NotificationMessage { Severity = NotificationSeverity.Info, Summary = "SplitButton Item Clicked", Detail = $"{buttonName}, item with value {item.Value} clicked" });
        }
        else
        {
            NotificationService.Notify(new NotificationMessage { Severity = NotificationSeverity.Info, Summary = "SplitButton Clicked", Detail = $"{buttonName} clicked" });
        }
    }
}
```
