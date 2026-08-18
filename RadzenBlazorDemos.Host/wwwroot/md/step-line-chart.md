# Step Line Chart

Draw a staircase line for values that change in discrete steps with a Blazor step line chart.

Keywords: chart, graph, line, step, interpolation

## Examples

## Radzen Blazor Chart with step line series

A step line chart holds each value flat until the next point, then jumps - drawing a staircase that suits values which change in discrete steps rather than sliding smoothly.

```razor
<RadzenStack class="rz-p-0 rz-p-md-6 rz-p-lg-12">
    <RadzenRow>
        <RadzenColumn Size="12">
            <RadzenChart Animate="true">
                <RadzenChartTooltipOptions Shared="true" />
                <RadzenLineSeries Data="@revenue2025" CategoryProperty="Quarter" Title="2025" ValueProperty="Revenue" Interpolation="Interpolation.Step" />
                <RadzenLineSeries Data="@revenue2026" CategoryProperty="Quarter" Title="2026" ValueProperty="Revenue" Interpolation="Interpolation.Step" LineType="LineType.Dashed" />
                <RadzenCategoryAxis Padding="20">
                    <RadzenAxisCrosshair Visible="true" />
                </RadzenCategoryAxis>
                <RadzenValueAxis>
                    <RadzenGridLines Visible="true" />
                    <RadzenAxisTitle Text="Revenue" />
                </RadzenValueAxis>
            </RadzenChart>
        </RadzenColumn>
    </RadzenRow>
</RadzenStack>

@code {
    class DataItem
    {
        public string Quarter { get; set; }
        public double Revenue { get; set; }
    }

    DataItem[] revenue2025 = new DataItem[] {
        new DataItem { Quarter = "Q1", Revenue = 234000 },
        new DataItem { Quarter = "Q2", Revenue = 284000 },
        new DataItem { Quarter = "Q3", Revenue = 274000 },
        new DataItem { Quarter = "Q4", Revenue = 294000 },
    };

    DataItem[] revenue2026 = new DataItem[] {
        new DataItem { Quarter = "Q1", Revenue = 254000 },
        new DataItem { Quarter = "Q2", Revenue = 324000 },
        new DataItem { Quarter = "Q3", Revenue = 354000 },
        new DataItem { Quarter = "Q4", Revenue = 394000 },
    };
}
```
