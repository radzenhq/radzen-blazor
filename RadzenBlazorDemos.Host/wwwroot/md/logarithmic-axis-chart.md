# Logarithmic Axis

Use a logarithmic axis on a Blazor chart to keep values spanning several orders of magnitude readable.

Keywords: chart, graph, logarithmic, log, axis, scale

## Examples

## Radzen Blazor Chart with logarithmic axis

A logarithmic axis compresses a huge range onto a readable scale, so values spanning several orders of magnitude - from tens to millions - all stay visible on one chart.

```razor
<RadzenStack class="rz-p-0 rz-p-md-6 rz-p-lg-12">
    <RadzenRow>
        <RadzenColumn Size="12">
            <RadzenChart Style="height: 480px">
                <RadzenColumnSeries FillMode="FillMode.Gradient" Data="@data" CategoryProperty="Country" ValueProperty="Population" Title="Population">
                    <RadzenSeriesDataLabels Visible="true" />
                </RadzenColumnSeries>
                <RadzenCategoryAxis />
                <RadzenValueAxis Logarithmic="true" FormatString="{0:N0}">
                    <RadzenGridLines Visible="true" />
                    <RadzenAxisTitle Text="Population (log scale)" />
                </RadzenValueAxis>
                <RadzenChartTooltipOptions Visible="true" />
                <RadzenLegend Position="LegendPosition.Bottom" />
            </RadzenChart>
        </RadzenColumn>
    </RadzenRow>
</RadzenStack>

@code {
    class DataItem
    {
        public string Country { get; set; }
        public double Population { get; set; }
    }

    DataItem[] data = new DataItem[]
    {
        new DataItem { Country = "Iceland", Population = 376000 },
        new DataItem { Country = "Luxembourg", Population = 660000 },
        new DataItem { Country = "Singapore", Population = 5900000 },
        new DataItem { Country = "Sweden", Population = 10500000 },
        new DataItem { Country = "Germany", Population = 84000000 },
        new DataItem { Country = "USA", Population = 332000000 },
        new DataItem { Country = "India", Population = 1400000000 },
    };
}
```
