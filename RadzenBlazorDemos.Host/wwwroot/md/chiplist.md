# ChipList

The Blazor ChipList shows a set of selectable, removable chips bound to data.

Keywords: chip, chiplist, tag, form, edit, selection

> API reference: [RadzenChipList API](https://blazor.radzen.com/api/chiplist.md)

## Examples

## ChipList

The Blazor ChipList shows a set of selectable, removable chips bound to data.

### Single selection

Use `RadzenChipList` to let users pick a single value from a set of chips.

```razor
<RadzenStack class="rz-p-12" Gap="0.5rem">
    <RadzenChipList @bind-Value="@selectedCategory"
                    TValue="string"
                    Data="@categories"
                    TextProperty="Name"
                    ValueProperty="Name"
                    ChipStyle="BadgeStyle.Info" />
    <RadzenText TextStyle="TextStyle.Body2">Selected category: <strong>@selectedCategory</strong></RadzenText>
</RadzenStack>

@code {
    string selectedCategory = "Backend";

    List<NamedItem> categories = new()
    {
        new NamedItem("Frontend"),
        new NamedItem("Backend"),
        new NamedItem("Design")
    };

    public record NamedItem(string Name);
}
```


### Multiple selection

Set `Multiple="true"` to allow selecting more than one chip. Use `AllowDelete="true"` to enable chip removal.

```razor
<RadzenStack class="rz-p-12" Gap="0.5rem">
    <RadzenChipList @bind-Value="@selectedTagIds"
                    TValue="IEnumerable<int>"
                    Data="@tags"
                    TextProperty="Name"
                    ValueProperty="Id"
                    Multiple="true"
                    AllowDelete="true"
                    ChipStyle="BadgeStyle.Secondary"
                    ChipRemoved="@OnChipRemoved" />
    <RadzenText TextStyle="TextStyle.Body2">Selected tags: <strong>@string.Join(", ", selectedTagIds)</strong></RadzenText>
</RadzenStack>

@code {
    IEnumerable<int> selectedTagIds = new[] { 1, 3 };

    List<TagItem> tags = new()
    {
        new TagItem(1, "Bug"),
        new TagItem(2, "Feature"),
        new TagItem(3, "Docs"),
        new TagItem(4, "Refactor")
    };

    void OnChipRemoved(object value)
    {
        if (value is int id)
        {
            tags = tags.Where(t => t.Id != id).ToList();
            selectedTagIds = selectedTagIds.Where(v => v != id).ToArray();
        }
    }

    public record TagItem(int Id, string Name);
}
```


### Events

Handle `Change` and `ChipRemoved` callbacks to respond to selection changes and chip removals.

```razor
<RadzenStack class="rz-p-12" Gap="0.5rem">
    <RadzenText TextStyle="TextStyle.Body2" class="rz-text-secondary-color">Select or remove chips to see events logged below.</RadzenText>
    <RadzenChipList @bind-Value="@selectedIds"
                    TValue="IEnumerable<int>"
                    Data="@items"
                    TextProperty="Name"
                    ValueProperty="Id"
                    Multiple="true"
                    AllowDelete="true"
                    ChipStyle="BadgeStyle.Info"
                    Change="@OnChange"
                    ChipRemoved="@OnChipRemoved" />
    <RadzenCard Variant="Variant.Outlined" class="rz-p-4">
        <RadzenText TextStyle="TextStyle.Subtitle2" TagName="TagName.P" class="rz-mb-2">Event log</RadzenText>
        @if (events.Count == 0)
        {
            <RadzenText TextStyle="TextStyle.Body2" class="rz-text-secondary-color">No events yet.</RadzenText>
        }
        else
        {
            @foreach (var entry in events)
            {
                <RadzenText TextStyle="TextStyle.Body2">@entry</RadzenText>
            }
        }
    </RadzenCard>
</RadzenStack>

@code {
    IEnumerable<int> selectedIds = new[] { 1 };
    List<string> events = new();

    List<EventItem> items = new()
    {
        new EventItem(1, "Alpha"),
        new EventItem(2, "Beta"),
        new EventItem(3, "Gamma"),
        new EventItem(4, "Delta")
    };

    void OnChange(object value)
    {
        events.Insert(0, $"Change — selection: [{string.Join(", ", selectedIds)}] at {DateTime.Now:T}");
    }

    void OnChipRemoved(object value)
    {
        if (value is int id)
        {
            var name = items.FirstOrDefault(i => i.Id == id)?.Name ?? id.ToString();
            items = items.Where(i => i.Id != id).ToList();
            selectedIds = selectedIds.Where(v => v != id).ToArray();
            events.Insert(0, $"ChipRemoved — \"{name}\" (id: {id}) at {DateTime.Now:T}");
        }
    }

    public record EventItem(int Id, string Name);
}
```


### Templates

Use the `Template` render fragment to customize chip content. Combine with style, variant, and size properties for full control.

```razor
<RadzenStack class="rz-p-12" Gap="1rem">
    <RadzenStack Orientation="Orientation.Horizontal" Gap="1rem" Wrap="FlexWrap.Wrap">
        <RadzenStack Gap="0.25rem">
            <RadzenText TextStyle="TextStyle.Caption">Style</RadzenText>
            <RadzenSelectBar @bind-Value="@selectedStyle" TValue="BadgeStyle" Size="ButtonSize.Small">
                <Items>
                    <RadzenSelectBarItem Text="Base" Value="BadgeStyle.Base" />
                    <RadzenSelectBarItem Text="Primary" Value="BadgeStyle.Primary" />
                    <RadzenSelectBarItem Text="Info" Value="BadgeStyle.Info" />
                </Items>
            </RadzenSelectBar>
        </RadzenStack>
        <RadzenStack Gap="0.25rem">
            <RadzenText TextStyle="TextStyle.Caption">Variant</RadzenText>
            <RadzenSelectBar @bind-Value="@selectedVariant" TValue="Variant" Size="ButtonSize.Small">
                <Items>
                    <RadzenSelectBarItem Text="Filled" Value="Variant.Filled" />
                    <RadzenSelectBarItem Text="Outlined" Value="Variant.Outlined" />
                    <RadzenSelectBarItem Text="Text" Value="Variant.Text" />
                </Items>
            </RadzenSelectBar>
        </RadzenStack>
        <RadzenStack Gap="0.25rem">
            <RadzenText TextStyle="TextStyle.Caption">Size</RadzenText>
            <RadzenSelectBar @bind-Value="@selectedSize" TValue="ChipSize" Size="ButtonSize.Small">
                <Items>
                    <RadzenSelectBarItem Text="Medium" Value="ChipSize.Medium" />
                    <RadzenSelectBarItem Text="Small" Value="ChipSize.Small" />
                    <RadzenSelectBarItem Text="XS" Value="ChipSize.ExtraSmall" />
                </Items>
            </RadzenSelectBar>
        </RadzenStack>
    </RadzenStack>
    <RadzenChipList @bind-Value="@selectedPriority"
                    TValue="string"
                    Data="@priorities"
                    TextProperty="Name"
                    ValueProperty="Name"
                    ChipStyle="@selectedStyle"
                    Variant="@selectedVariant"
                    Size="@selectedSize">
        <Template Context="chip">
            <RadzenBadge Text="@GetInitial(chip.Text)" IsPill="true" BadgeStyle="BadgeStyle.Primary" class="rz-me-1" />
            <strong>@chip.Text</strong>
        </Template>
    </RadzenChipList>
</RadzenStack>

@code {
    string selectedPriority = "Medium";
    BadgeStyle selectedStyle = BadgeStyle.Base;
    Variant selectedVariant = Variant.Filled;
    ChipSize selectedSize = ChipSize.Medium;

    List<NamedItem> priorities = new()
    {
        new NamedItem("Low"),
        new NamedItem("Medium"),
        new NamedItem("High")
    };

    string GetInitial(string text)
    {
        return string.IsNullOrWhiteSpace(text) ? "?" : text.Trim()[0].ToString().ToUpperInvariant();
    }

    public record NamedItem(string Name);
}
```
