# Chip

The Blazor Chip is a compact element for tags, statuses, and filters, with optional remove and selection.

Keywords: chip, tag, label, status

> API reference: [RadzenChip API](https://blazor.radzen.com/api/chip.md)

## Examples

## Chip

The Blazor Chip is a compact element for tags, statuses, and filters, with optional remove and selection.

### Chip Style

Use the `ChipStyle` property to set a predefined chip style, e.g. `ChipStyle="BadgeStyle.Primary"`.

```razor
<RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Wrap="FlexWrap.Wrap" JustifyContent="JustifyContent.Center" class="rz-p-12" Gap="0.75rem">
    <RadzenChip Text="Primary" ChipStyle="BadgeStyle.Primary" />
    <RadzenChip Text="Secondary" ChipStyle="BadgeStyle.Secondary" />
    <RadzenChip Text="Base" ChipStyle="BadgeStyle.Base" />
    <RadzenChip Text="Info" ChipStyle="BadgeStyle.Info" />
    <RadzenChip Text="Success" ChipStyle="BadgeStyle.Success" />
    <RadzenChip Text="Warning" ChipStyle="BadgeStyle.Warning" />
    <RadzenChip Text="Danger" ChipStyle="BadgeStyle.Danger" />
    <RadzenChip Text="Light" ChipStyle="BadgeStyle.Light" />
    <RadzenChip Text="Dark" ChipStyle="BadgeStyle.Dark" />
</RadzenStack>
```


### Variant

Use the `Variant` property to control the chip design variant, e.g. `Variant="Variant.Outlined"`.

```razor
<RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Wrap="FlexWrap.Wrap" JustifyContent="JustifyContent.Center" class="rz-p-12" Gap="0.75rem">
    <RadzenChip Text="Filled" Variant="Variant.Filled" ChipStyle="BadgeStyle.Primary" />
    <RadzenChip Text="Flat" Variant="Variant.Flat" ChipStyle="BadgeStyle.Primary" />
    <RadzenChip Text="Text" Variant="Variant.Text" ChipStyle="BadgeStyle.Primary" />
    <RadzenChip Text="Outlined" Variant="Variant.Outlined" ChipStyle="BadgeStyle.Primary" />
</RadzenStack>
```


### Sizes

Use the `Size` property to set the chip size, e.g. `Size="ChipSize.Small"`.

```razor
<RadzenText TextStyle="TextStyle.Subtitle1" TagName="TagName.H3">Text</RadzenText>
<RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Wrap="FlexWrap.Wrap" class="rz-p-12" Gap="0.75rem">
    <RadzenChip Text="Medium" Size="ChipSize.Medium" ChipStyle="BadgeStyle.Primary" />
    <RadzenChip Text="Small" Size="ChipSize.Small" ChipStyle="BadgeStyle.Primary" />
    <RadzenChip Text="Extra small" Size="ChipSize.ExtraSmall" ChipStyle="BadgeStyle.Primary" />
</RadzenStack>

<RadzenText TextStyle="TextStyle.Subtitle1" TagName="TagName.H3" class="rz-mt-4">Icon and Text</RadzenText>
<RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Wrap="FlexWrap.Wrap" class="rz-p-12" Gap="0.75rem">
    <RadzenChip Text="Medium" Size="ChipSize.Medium" Icon="settings" ChipStyle="BadgeStyle.Primary" />
    <RadzenChip Text="Small" Size="ChipSize.Small" Icon="settings" ChipStyle="BadgeStyle.Primary" />
    <RadzenChip Text="Extra small" Size="ChipSize.ExtraSmall" Icon="settings" ChipStyle="BadgeStyle.Primary" />
</RadzenStack>
```


### Icons

Use the `Icon` property to display a material icon before the chip text.

```razor
<RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Wrap="FlexWrap.Wrap" JustifyContent="JustifyContent.Center" class="rz-p-12" Gap="0.75rem">
    <RadzenChip Text="Home" Icon="home" ChipStyle="BadgeStyle.Primary" />
    <RadzenChip Text="Settings" Icon="settings" ChipStyle="BadgeStyle.Secondary" />
    <RadzenChip Text="Favorite" Icon="star" ChipStyle="BadgeStyle.Warning" />
    <RadzenChip Text="Info" Icon="info" ChipStyle="BadgeStyle.Info" />
    <RadzenChip Text="Done" Icon="check_circle" ChipStyle="BadgeStyle.Success" />
</RadzenStack>
```


### Selected

Use the `Selected` property to highlight a chip as selected.

```razor
<RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Wrap="FlexWrap.Wrap" JustifyContent="JustifyContent.Center" class="rz-p-12" Gap="0.75rem">
    <RadzenChip Text="Default" ChipStyle="BadgeStyle.Primary" />
    <RadzenChip Text="Selected" ChipStyle="BadgeStyle.Primary" Selected="true" />
    <RadzenChip Text="Default" ChipStyle="BadgeStyle.Secondary" />
    <RadzenChip Text="Selected" ChipStyle="BadgeStyle.Secondary" Selected="true" />
    <RadzenChip Text="Default" ChipStyle="BadgeStyle.Info" />
    <RadzenChip Text="Selected" ChipStyle="BadgeStyle.Info" Selected="true" />
</RadzenStack>
```


### Disabled

Use the `Disabled` property to prevent user interaction with the chip.

```razor
<RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Wrap="FlexWrap.Wrap" JustifyContent="JustifyContent.Center" class="rz-p-12" Gap="0.75rem">
    <RadzenChip Text="Primary" ChipStyle="BadgeStyle.Primary" Disabled="true" />
    <RadzenChip Text="Secondary" ChipStyle="BadgeStyle.Secondary" Disabled="true" />
    <RadzenChip Text="Info" ChipStyle="BadgeStyle.Info" Disabled="true" />
    <RadzenChip Text="Success" ChipStyle="BadgeStyle.Success" Disabled="true" />
    <RadzenChip Text="Warning" ChipStyle="BadgeStyle.Warning" Disabled="true" />
    <RadzenChip Text="Danger" ChipStyle="BadgeStyle.Danger" Disabled="true" />
    <RadzenChip Text="With Icon" Icon="block" Disabled="true" />
</RadzenStack>
```


### Events

Handle `Click` and `Close` events to respond to user interactions.

```razor
<RadzenStack class="rz-p-12" Gap="0.5rem">
    <RadzenText TextStyle="TextStyle.Body2" class="rz-text-secondary-color">Click a chip to select it, or remove it with the close icon.</RadzenText>
    <RadzenStack Orientation="Orientation.Horizontal" Wrap="FlexWrap.Wrap" Gap="0.75rem">
        @foreach (var tag in tags)
        {
            <RadzenChip Text="@tag"
                        ChipStyle="BadgeStyle.Secondary"
                        Close="@(args => RemoveTag(tag))"
                        Click="@(args => SelectTag(tag))"
                        IsSelected="@(selectedTag == tag)" />
        }
    </RadzenStack>
    <RadzenText TextStyle="TextStyle.Body2"><strong>Last event:</strong> @lastEvent</RadzenText>
</RadzenStack>

@code {
    string selectedTag = string.Empty;
    string lastEvent = "none";
    List<string> tags = new() { "Blazor", "Radzen", "Chip", "Component" };

    void SelectTag(string tag)
    {
        selectedTag = tag;
        lastEvent = $"Selected: {tag}";
    }

    void RemoveTag(string tag)
    {
        tags.Remove(tag);
        if (selectedTag == tag)
        {
            selectedTag = string.Empty;
        }
        lastEvent = $"Removed: {tag}";
    }
}
```


### Add / Remove

Combine removable chips with an input to build add/remove patterns like email recipient lists.

```razor
<RadzenStack class="rz-p-12" Gap="0.5rem">
    <RadzenText TextStyle="TextStyle.Body2" class="rz-text-secondary-color">Use the close button to remove a chip, or type a value and click Add.</RadzenText>
    <RadzenCard Variant="Variant.Outlined" class="rz-p-4">
        <RadzenStack Orientation="Orientation.Horizontal" Wrap="FlexWrap.Wrap" Gap="0.5rem" AlignItems="AlignItems.Center">
            <RadzenText TextStyle="TextStyle.Body2" Style="white-space: nowrap;" class="rz-text-secondary-color"><strong>To:</strong></RadzenText>
            @foreach (var recipient in recipients)
            {
                <RadzenChip Text="@recipient"
                            Icon="person"
                            ChipStyle="BadgeStyle.Info"
                            Close="@(args => RemoveRecipient(recipient))" />
            }
            <RadzenStack Orientation="Orientation.Horizontal" Gap="0.25rem" AlignItems="AlignItems.Center" Style="flex: 1; min-width: 12rem;">
                <RadzenTextBox @bind-Value="@recipientInput" Placeholder="Add recipient email" Style="flex: 1;" @onkeydown="@OnRecipientKeyDown" />
                <RadzenButton Text="Add" Icon="add" Click="@AddRecipient" Size="ButtonSize.Small" />
            </RadzenStack>
        </RadzenStack>
    </RadzenCard>
</RadzenStack>

@code {
    string recipientInput = string.Empty;
    List<string> recipients = new() { "pedro@example.com", "john@example.com" };

    void AddRecipient()
    {
        var candidate = recipientInput.Trim();
        if (!string.IsNullOrWhiteSpace(candidate) && !recipients.Contains(candidate))
        {
            recipients.Add(candidate);
            recipientInput = string.Empty;
        }
    }

    void OnRecipientKeyDown(KeyboardEventArgs args)
    {
        if (args.Key == "Enter")
        {
            AddRecipient();
        }
    }

    void RemoveRecipient(string recipient)
    {
        recipients.Remove(recipient);
    }
}
```
