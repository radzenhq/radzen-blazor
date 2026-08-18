# Range Step Area Chart

Fill a low-to-high band that holds flat between points with a Blazor range step area chart.

Keywords: chart, graph, area, range, step, interpolation, band

## Examples

## Radzen Blazor Chart with range step area series

A range step area chart combines two ideas: it fills the band between a low and a high value, and it holds each band flat until the next point. Good for ranges that stay fixed over an interval, like a tariff window or a scheduled min/max.

```razor
<RadzenStack class="rz-p-0 rz-p-md-6 rz-p-lg-12">
    <RadzenRow>
        <RadzenColumn Size="12">
            <RadzenChart Animate="true">
                <RadzenRangeAreaSeries Data="@temperatureData" CategoryProperty="Month"
                    MinProperty="Low" MaxProperty="High" Title="Temperature Range"
                    StrokeWidth="2" Interpolation="Interpolation.Step" FillMode="FillMode.Gradient" />
                <RadzenCategoryAxis />
                <RadzenValueAxis>
                    <RadzenGridLines Visible="true" />
                    <RadzenAxisTitle Text="Temperature (°C)" />
                </RadzenValueAxis>
            </RadzenChart>
        </RadzenColumn>
    </RadzenRow>
</RadzenStack>

@code {
    class DataItem
    {
        public string Month { get; set; }
        public double Low { get; set; }
        public double High { get; set; }
    }

    DataItem[] temperatureData = new DataItem[]
    {
        new DataItem { Month = "Jan", Low = -3, High = 5 },
        new DataItem { Month = "Feb", Low = -2, High = 7 },
        new DataItem { Month = "Mar", Low = 1, High = 12 },
        new DataItem { Month = "Apr", Low = 5, High = 17 },
        new DataItem { Month = "May", Low = 9, High = 22 },
        new DataItem { Month = "Jun", Low = 13, High = 26 },
        new DataItem { Month = "Jul", Low = 15, High = 29 },
        new DataItem { Month = "Aug", Low = 14, High = 28 },
        new DataItem { Month = "Sep", Low = 11, High = 24 },
        new DataItem { Month = "Oct", Low = 7, High = 18 },
        new DataItem { Month = "Nov", Low = 2, High = 11 },
        new DataItem { Month = "Dec", Low = -1, High = 6 },
    };
}
```
