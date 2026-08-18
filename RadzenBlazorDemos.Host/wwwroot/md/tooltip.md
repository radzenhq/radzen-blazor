# Tooltip

Blazor Tooltip component with configurable positions, HTML content, delay, and duration settings.

Keywords: popup, tooltip

> API reference: [RadzenTooltip API](https://blazor.radzen.com/api/tooltip.md)

## Examples

## Blazor Tooltip

A small pop-up box that appears when the user hovers over or clicks on a UI element.

### Show tooltip with string message

Show string message tooltip when the user hovers a Radzen component.

```razor
<div class="rz-p-12 rz-text-align-center">
    <RadzenButton Text="Show tooltip" MouseEnter="@(args => ShowTooltip(args) )" />
</div>

@code {
    void ShowTooltip(ElementReference elementReference, TooltipOptions options = null) => tooltipService.Open(elementReference, "Hello!", options);
}
```


### Tooltip positions

Place the Tooltip to the left, right, top, or bottom of a component.

```razor
<RadzenStack Orientation="Orientation.Horizontal" JustifyContent="JustifyContent.Center" Wrap="FlexWrap.Wrap" class="rz-p-12">
    <RadzenButton Text="Left" MouseEnter="@(args => ShowTooltip(args, new TooltipOptions(){ Position = TooltipPosition.Left }))" />
    <RadzenButton Text="Top" MouseEnter="@(args => ShowTooltip(args, new TooltipOptions(){ Position = TooltipPosition.Top }))" />
    <RadzenButton Text="Bottom" MouseEnter="@(args => ShowTooltip(args, new TooltipOptions(){ Position = TooltipPosition.Bottom }))" />
    <RadzenButton Text="Right" MouseEnter="@(args => ShowTooltip(args, new TooltipOptions(){ Position = TooltipPosition.Right }))" />
</RadzenStack>

@code {
    void ShowTooltip(ElementReference elementReference, TooltipOptions options = null) => tooltipService.Open(elementReference, "Some content", options);
}
```


### Show tooltip with HTML content

Show HTML content tooltip when the user hovers a Radzen component.

```razor
<div class="rz-p-12 rz-text-align-center">
    <RadzenButton Text="Show tooltip" MouseEnter="@(args => ShowTooltipWithHtml(args, new TooltipOptions(){ Style = "background: var(--rz-warning-light); color: var(--rz-text-color)", Duration = null }))" />
</div>

@code {
    void ShowTooltipWithHtml(ElementReference elementReference, TooltipOptions options = null) => tooltipService.Open(elementReference, ds =>
@<div>
    Some <b>HTML</b> content
</div>, options);
}
```


### Tooltip delay and duration

Show tooltip with delay when the user hovers a component and close it after 5 seconds.

```razor
<div class="rz-p-12 rz-text-align-center">
    <RadzenButton Text="Show tooltip" MouseEnter="@(args => ShowTooltip(args, new TooltipOptions(){ Delay = 500, Duration = 5000 }))" />
</div>

@code {
    void ShowTooltip(ElementReference elementReference, TooltipOptions options = null) => tooltipService.Open(elementReference, "Now wait 5 seconds and I will disappear.", options);
}
```


### Close Tooltip on page click

Show styled tooltip on button click and close it on page click.

```razor
<div class="rz-p-12 rz-text-align-center">
    <RadzenButton @ref="radzenButton" Text="Click to show tooltip" Click="@(args => ShowTooltip(radzenButton.Element, new TooltipOptions(){ Style = "background-color: var(--rz-secondary); color: var(--rz-text-contrast-color)", Duration = null }))" />
</div>

@code {
    RadzenButton radzenButton;

    void ShowTooltip(ElementReference elementReference, TooltipOptions options = null) => tooltipService.Open(elementReference, "Click anywhere on the page to close me.", options);
}
```


### Tooltip on HTML element

Show string message tooltip when the user hovers an HTML element.

```razor
<div class="rz-p-12 rz-text-align-center">
    <button @ref="htmlButton" @onmouseenter="@(args => ShowTooltip(htmlButton))">
        Show tooltip
    </button>
</div>

@code {
    ElementReference htmlButton;

    void ShowTooltip(ElementReference elementReference, TooltipOptions options = null) => tooltipService.Open(elementReference, "Some content", options);
}
```
