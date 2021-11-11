# Tooltip component
This article demonstrates how to use the Tooltip component. Use `TooltipService` to open and close tooltips. 

## Show tooltip with string message
```
@inject TooltipService tooltipService

<RadzenButton Text="Show tooltip" MouseEnter="@(args => ShowTooltip(args) )" />

@code {
    void ShowTooltip(ElementReference elementReference, TooltipOptions options = null) => tooltipService.Open(elementReference, "Some content", options);
}
```

## Show tooltip with HTML content
```
@inject TooltipService tooltipService

<RadzenButton Text="Show tooltip" MouseEnter="@(args => ShowTooltipWithHtml(args))" />

@code {
    void ShowTooltipWithHtml(ElementReference elementReference, TooltipOptions options = null) => tooltipService.Open(elementReference, ds =>
    @<div>
        Some <b>HTML</b> content
    </div>, options);
    }
}
```

## Show tooltip on mouse over Radzen component and close it after 10 sec
```
@inject TooltipService tooltipService

<RadzenButton Text="Show tooltip" MouseEnter="@(args => ShowTooltip(args, new TooltipOptions(){ Duration = 10000 }))" />

@code {
    void ShowTooltip(ElementReference elementReference, TooltipOptions options = null) => tooltipService.Open(elementReference, "Some content", options);
}
```

## Show tooltip on mouse over HTML element
```
@inject TooltipService tooltipService

<button @ref="htmlButton" @onmouseover="@(args => ShowTooltip(htmlButton))">
    Show tooltip
</button>

@code {
    ElementReference htmlButton;
    
    void ShowTooltip(ElementReference elementReference, TooltipOptions options = null) => tooltipService.Open(elementReference, "Some content", options);
}
```