# Badge

Blazor Badge component for displaying counts, labels, and status indicators with multiple styles and variants.

Keywords: badge, link

> API reference: [RadzenBadge API](https://blazor.radzen.com/api/badge.md)

## Examples

## Blazor Badge

A small graphic for displaying counts, labels, and status indicators with multiple styles and variants.

```razor
<RadzenStack Orientation="Orientation.Horizontal" Wrap="FlexWrap.Wrap" JustifyContent="JustifyContent.Center" class="rz-p-12" Gap="2rem">
    <RadzenButton ButtonStyle="ButtonStyle.Primary">
        Notifications
        <RadzenBadge Variant="Variant.Outlined" BadgeStyle="BadgeStyle.Light" Text="15" class="rz-ms-2"/>
    </RadzenButton>

    <RadzenButton ButtonStyle="ButtonStyle.Secondary" Shade="Shade.Lighter" class="rz-shadow-0">
        Messages
        <RadzenBadge BadgeStyle="BadgeStyle.Secondary" IsPill="@true" Text="15" class="rz-ms-2" />
    </RadzenButton>

    <RadzenButton ButtonStyle="ButtonStyle.Dark" class="rz-border-radius-6 rz-shadow-6">
        Events
        <RadzenBadge BadgeStyle="BadgeStyle.Warning" Shade="Shade.Lighter" IsPill="@true" Text="15" class="rz-ms-2" />
    </RadzenButton>
</RadzenStack>
```


### Badge Style

To set a predefined badge style, use the `BadgeStyle` property, e.g. `BadgeStyle="BadgeStyle.Primary"`.

```razor
<RadzenStack Orientation="Orientation.Horizontal" Wrap="FlexWrap.Wrap" JustifyContent="JustifyContent.Center" class="rz-p-12" Gap="2rem">
    <RadzenBadge BadgeStyle="BadgeStyle.Primary" Text="Primary" />
    <RadzenBadge BadgeStyle="BadgeStyle.Secondary" Text="Secondary" />
    <RadzenBadge BadgeStyle="BadgeStyle.Base" Text="Base" />
    <RadzenBadge BadgeStyle="BadgeStyle.Info" Text="Info" />
    <RadzenBadge BadgeStyle="BadgeStyle.Success" Text="Success" />
    <RadzenBadge BadgeStyle="BadgeStyle.Warning" Text="Warning" />
    <RadzenBadge BadgeStyle="BadgeStyle.Danger" Text="Danger" />
    <RadzenBadge BadgeStyle="BadgeStyle.Light" Text="Light" />
    <RadzenBadge BadgeStyle="BadgeStyle.Dark" Text="Dark" />
</RadzenStack>
```


### Badge Shade

Each badge style, except Light and Dark, comes with a set of shades. Use the `Shade` property, e.g. `Shade="Shade.Lighter"`.

```razor
<RadzenStack Orientation="Orientation.Vertical" class="rz-p-12" Gap="1rem">
    <RadzenStack Orientation="Orientation.Horizontal" Wrap="FlexWrap.Wrap" JustifyContent="JustifyContent.Center" Gap="2rem">
        <RadzenBadge Shade="Shade.Lighter" BadgeStyle="BadgeStyle.Primary" Text="Primary" />
        <RadzenBadge Shade="Shade.Lighter" BadgeStyle="BadgeStyle.Secondary" Text="Secondary" />
        <RadzenBadge Shade="Shade.Lighter" BadgeStyle="BadgeStyle.Base" Text="Base" />
        <RadzenBadge Shade="Shade.Lighter" BadgeStyle="BadgeStyle.Info" Text="Info" />
        <RadzenBadge Shade="Shade.Lighter" BadgeStyle="BadgeStyle.Success" Text="Success" />
        <RadzenBadge Shade="Shade.Lighter" BadgeStyle="BadgeStyle.Warning" Text="Warning" />
        <RadzenBadge Shade="Shade.Lighter" BadgeStyle="BadgeStyle.Danger" Text="Danger" />
    </RadzenStack>
    <RadzenStack Orientation="Orientation.Horizontal" Wrap="FlexWrap.Wrap" JustifyContent="JustifyContent.Center" Gap="2rem">
        <RadzenBadge Shade="Shade.Light" BadgeStyle="BadgeStyle.Primary" Text="Primary" />
        <RadzenBadge Shade="Shade.Light" BadgeStyle="BadgeStyle.Secondary" Text="Secondary" />
        <RadzenBadge Shade="Shade.Light" BadgeStyle="BadgeStyle.Base" Text="Base" />
        <RadzenBadge Shade="Shade.Light" BadgeStyle="BadgeStyle.Info" Text="Info" />
        <RadzenBadge Shade="Shade.Light" BadgeStyle="BadgeStyle.Success" Text="Success" />
        <RadzenBadge Shade="Shade.Light" BadgeStyle="BadgeStyle.Warning" Text="Warning" />
        <RadzenBadge Shade="Shade.Light" BadgeStyle="BadgeStyle.Danger" Text="Danger" />
    </RadzenStack>
    <RadzenStack Orientation="Orientation.Horizontal" Wrap="FlexWrap.Wrap" JustifyContent="JustifyContent.Center" Gap="2rem">
        <RadzenBadge Shade="Shade.Dark" BadgeStyle="BadgeStyle.Primary" Text="Primary" />
        <RadzenBadge Shade="Shade.Dark" BadgeStyle="BadgeStyle.Secondary" Text="Secondary" />
        <RadzenBadge Shade="Shade.Dark" BadgeStyle="BadgeStyle.Base" Text="Base" />
        <RadzenBadge Shade="Shade.Dark" BadgeStyle="BadgeStyle.Info" Text="Info" />
        <RadzenBadge Shade="Shade.Dark" BadgeStyle="BadgeStyle.Success" Text="Success" />
        <RadzenBadge Shade="Shade.Dark" BadgeStyle="BadgeStyle.Warning" Text="Warning" />
        <RadzenBadge Shade="Shade.Dark" BadgeStyle="BadgeStyle.Danger" Text="Danger" />
    </RadzenStack>
    <RadzenStack Orientation="Orientation.Horizontal" Wrap="FlexWrap.Wrap" JustifyContent="JustifyContent.Center" Gap="2rem">
        <RadzenBadge Shade="Shade.Darker" BadgeStyle="BadgeStyle.Primary" Text="Primary" />
        <RadzenBadge Shade="Shade.Darker" BadgeStyle="BadgeStyle.Secondary" Text="Secondary" />
        <RadzenBadge Shade="Shade.Darker" BadgeStyle="BadgeStyle.Base" Text="Base" />
        <RadzenBadge Shade="Shade.Darker" BadgeStyle="BadgeStyle.Info" Text="Info" />
        <RadzenBadge Shade="Shade.Darker" BadgeStyle="BadgeStyle.Success" Text="Success" />
        <RadzenBadge Shade="Shade.Darker" BadgeStyle="BadgeStyle.Warning" Text="Warning" />
        <RadzenBadge Shade="Shade.Darker" BadgeStyle="BadgeStyle.Danger" Text="Danger" />
    </RadzenStack>
</RadzenStack>
```


### Badge Variant

Each badge style and shade can be used with different badge variants. Use the `Variant` property, e.g. `Variant="Variant.Outlined"`.

```razor
<RadzenStack Orientation="Orientation.Horizontal" Wrap="FlexWrap.Wrap" JustifyContent="JustifyContent.Center" class="rz-p-12" Gap="2rem">
    <RadzenBadge Text="Flat" />
    <RadzenBadge Variant="Variant.Outlined" Text="Outlined" />
    <RadzenBadge Variant="Variant.Text" Text="Text" />
</RadzenStack>

<RadzenStack Orientation="Orientation.Vertical" class="rz-pb-12" Gap="1rem">
    <RadzenStack Orientation="Orientation.Horizontal" Wrap="FlexWrap.Wrap" JustifyContent="JustifyContent.Center" Gap="2rem">
        <RadzenBadge Shade="Shade.Lighter" Text="Flat" />
        <RadzenBadge Shade="Shade.Lighter" Variant="Variant.Outlined"  Text="Outlined" />
        <RadzenBadge Shade="Shade.Lighter" Variant="Variant.Text" Text="Text" />
    </RadzenStack>
    <RadzenStack Orientation="Orientation.Horizontal" Wrap="FlexWrap.Wrap" JustifyContent="JustifyContent.Center" Gap="2rem">
        <RadzenBadge Shade="Shade.Light" Text="Flat" />
        <RadzenBadge Shade="Shade.Light" Variant="Variant.Outlined"  Text="Outlined" />
        <RadzenBadge Shade="Shade.Light" Variant="Variant.Text" Text="Text" />
    </RadzenStack>
    <RadzenStack Orientation="Orientation.Horizontal" Wrap="FlexWrap.Wrap" JustifyContent="JustifyContent.Center" Gap="2rem">
        <RadzenBadge Shade="Shade.Dark" Text="Flat" />
        <RadzenBadge Shade="Shade.Dark" Variant="Variant.Outlined"  Text="Outlined" />
        <RadzenBadge Shade="Shade.Dark" Variant="Variant.Text" Text="Text" />
    </RadzenStack>
    <RadzenStack Orientation="Orientation.Horizontal" Wrap="FlexWrap.Wrap" JustifyContent="JustifyContent.Center" Gap="2rem">
        <RadzenBadge Shade="Shade.Darker" Text="Flat" />
        <RadzenBadge Shade="Shade.Darker" Variant="Variant.Outlined" Text="Outlined" />
        <RadzenBadge Shade="Shade.Darker" Variant="Variant.Text" Text="Text" />
    </RadzenStack>
</RadzenStack>
```


### Pill

Use `IsPill="true"` for pill-shaped badges.

```razor
<RadzenStack Orientation="Orientation.Horizontal" Wrap="FlexWrap.Wrap" JustifyContent="JustifyContent.Center" class="rz-p-12" Gap="2rem">
    <RadzenBadge IsPill="true" BadgeStyle="BadgeStyle.Secondary" Text="Flat" />
    <RadzenBadge IsPill="true" Variant="Variant.Outlined" BadgeStyle="BadgeStyle.Secondary" Text="Outlined" />
</RadzenStack>
```


### Child Content

Define custom content with ease.

```razor
<RadzenStack Orientation="Orientation.Horizontal" Wrap="FlexWrap.Wrap" JustifyContent="JustifyContent.Center" class="rz-p-12" Gap="2rem">

    <RadzenBadge BadgeStyle="BadgeStyle.Info" Shade="Shade.Dark">
        <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="0.25rem" Style="padding: 0 0.25rem 0 0; text-transform: none;">
            <RadzenIcon Icon="bug_report" /> No Bugs Found
        </RadzenStack>
    </RadzenBadge>
    
</RadzenStack>
```
