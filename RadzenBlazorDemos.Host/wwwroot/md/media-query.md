# MediaQuery

Respond to browser viewport size changes using CSS media queries. Perfect for creating responsive Blazor applications.

Keywords: mediaquery, media, query, responsive, breakpoint, viewport, screen, mobile, tablet, desktop, orientation, utility

> API reference: [RadzenMediaQuery API](https://blazor.radzen.com/api/mediaquery.md)

## Examples

## Blazor MediaQuery

Respond to browser viewport size changes using CSS media queries. Perfect for responsive Blazor apps.

### Basic Usage

Use the `Query` parameter to specify a CSS media query and the `Change` event to respond when the query matches or stops matching.

```razor
<RadzenCard class="rz-p-4">
    <RadzenMediaQuery Query="(max-width: 768px)" Change=@OnChange />
    
    <RadzenText TextStyle="TextStyle.H6" TagName="TagName.P" class="rz-mb-4">
        Current viewport status:
    </RadzenText>
    
    <RadzenBadge BadgeStyle="@(isMobile ? BadgeStyle.Success : BadgeStyle.Info)" 
                 Text="@(isMobile ? "Mobile View (≤ 768px)" : "Desktop View (> 768px)")" 
                 IsPill="true" 
                 class="rz-my-2" />
    
    <RadzenText TextStyle="TextStyle.Body2" class="rz-mt-4">
        Resize your browser window to see the media query in action.
    </RadzenText>
</RadzenCard>

@code {
    bool isMobile = false;

    void OnChange(bool matches)
    {
        isMobile = matches;
        StateHasChanged();
    }
}
```


### Show/Hide Content Based on Screen Size

Conditionally display different content for mobile and desktop viewports using media queries.

```razor
<RadzenMediaQuery Query="(max-width: 768px)" Change=@OnMobileChange />

<RadzenCard class="rz-p-4">
    @if (isMobile)
    {
        <RadzenStack Orientation="Orientation.Vertical" Gap="1rem">
            <RadzenText TextStyle="TextStyle.H6" TagName="TagName.P">Mobile Navigation</RadzenText>
            <RadzenButton Text="Home" Icon="home" Style="width: 100%" />
            <RadzenButton Text="Products" Icon="shopping_cart" Style="width: 100%" />
            <RadzenButton Text="About" Icon="info" Style="width: 100%" />
            <RadzenButton Text="Contact" Icon="email" Style="width: 100%" />
        </RadzenStack>
    }
    else
    {
        <RadzenStack Orientation="Orientation.Horizontal" Gap="1rem" AlignItems="AlignItems.Center">
            <RadzenText TextStyle="TextStyle.H6" TagName="TagName.P">Desktop Navigation:</RadzenText>
            <RadzenButton Text="Home" Icon="home" />
            <RadzenButton Text="Products" Icon="shopping_cart" />
            <RadzenButton Text="About" Icon="info" />
            <RadzenButton Text="Contact" Icon="email" />
        </RadzenStack>
    }
</RadzenCard>

@code {
    bool isMobile = false;

    void OnMobileChange(bool matches)
    {
        isMobile = matches;
        StateHasChanged();
    }
}
```


### Multiple Breakpoints

Use multiple `RadzenMediaQuery` components to respond to different screen sizes and create complex responsive layouts.

```razor
<RadzenMediaQuery Query="(max-width: 576px)" Change=@(matches => OnBreakpointChange("xs", matches)) />
<RadzenMediaQuery Query="(min-width: 577px) and (max-width: 768px)" Change=@(matches => OnBreakpointChange("sm", matches)) />
<RadzenMediaQuery Query="(min-width: 769px) and (max-width: 1024px)" Change=@(matches => OnBreakpointChange("md", matches)) />
<RadzenMediaQuery Query="(min-width: 1025px)" Change=@(matches => OnBreakpointChange("lg", matches)) />

<RadzenCard class="rz-p-4">
    <RadzenText TextStyle="TextStyle.H6" TagName="TagName.P" class="rz-mb-4">
        Current Breakpoint: <strong>@currentBreakpoint</strong>
    </RadzenText>
    
    <RadzenStack Orientation="Orientation.Vertical" Gap="0.5rem">
        <RadzenText TextStyle="TextStyle.Body2">
            <RadzenBadge BadgeStyle="@(currentBreakpoint == "xs" ? BadgeStyle.Success : BadgeStyle.Light)" 
                         Text="XS" Variant="Variant.Flat" class="rz-mr-2" />
            Extra Small: ≤ 576px
        </RadzenText>
        <RadzenText TextStyle="TextStyle.Body2">
            <RadzenBadge BadgeStyle="@(currentBreakpoint == "sm" ? BadgeStyle.Success : BadgeStyle.Light)" 
                         Text="SM" Variant="Variant.Flat" class="rz-mr-2" />
            Small: 577px - 768px
        </RadzenText>
        <RadzenText TextStyle="TextStyle.Body2">
            <RadzenBadge BadgeStyle="@(currentBreakpoint == "md" ? BadgeStyle.Success : BadgeStyle.Light)" 
                         Text="MD" Variant="Variant.Flat" class="rz-mr-2" />
            Medium: 769px - 1024px
        </RadzenText>
        <RadzenText TextStyle="TextStyle.Body2">
            <RadzenBadge BadgeStyle="@(currentBreakpoint == "lg" ? BadgeStyle.Success : BadgeStyle.Light)" 
                         Text="LG" Variant="Variant.Flat" class="rz-mr-2" />
            Large: ≥ 1025px
        </RadzenText>
    </RadzenStack>
    
    <RadzenText TextStyle="TextStyle.Caption" class="rz-mt-4">
        Resize your browser window to see different breakpoints activate.
    </RadzenText>
</RadzenCard>

@code {
    string currentBreakpoint = "lg";

    void OnBreakpointChange(string breakpoint, bool matches)
    {
        if (matches)
        {
            currentBreakpoint = breakpoint;
            StateHasChanged();
        }
    }
}
```


### Device Orientation

Detect device orientation changes using `orientation: portrait` or `orientation: landscape` media queries.

```razor
<RadzenMediaQuery Query="(orientation: portrait)" Change=@OnOrientationChange />

<RadzenCard class="rz-p-4 rz-text-align-center">
    @if (isPortrait)
    {
        <RadzenIcon Icon="stay_current_portrait" Style="font-size: 4rem; color: var(--rz-primary);" />
        <RadzenText TextStyle="TextStyle.H5" TagName="TagName.P" class="rz-mt-4">
            Portrait Mode
        </RadzenText>
        <RadzenText TextStyle="TextStyle.Body2" class="rz-mt-2">
            Device is in portrait orientation (height > width)
        </RadzenText>
    }
    else
    {
        <RadzenIcon Icon="stay_current_landscape" Style="font-size: 4rem; color: var(--rz-success);" />
        <RadzenText TextStyle="TextStyle.H5" TagName="TagName.P" class="rz-mt-4">
            Landscape Mode
        </RadzenText>
        <RadzenText TextStyle="TextStyle.Body2" class="rz-mt-2">
            Device is in landscape orientation (width > height)
        </RadzenText>
    }
    
    <RadzenText TextStyle="TextStyle.Caption" class="rz-mt-4">
        Rotate your device or resize the browser window to see orientation changes.
    </RadzenText>
</RadzenCard>

@code {
    bool isPortrait = false;

    void OnOrientationChange(bool matches)
    {
        isPortrait = matches;
        StateHasChanged();
    }
}
```
