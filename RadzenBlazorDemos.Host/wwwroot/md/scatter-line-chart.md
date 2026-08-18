# Scatter Line Chart

Plot X/Y points joined by a line with a Blazor scatter line chart - markers plus trend on a numeric axis.

Keywords: chart, graph, scatter, line, marker, point

## Examples

## Radzen Blazor Chart scatter line series

A scatter line chart plots X/Y points and joins them with a line, combining the raw markers of a scatter with the trend of a line - useful when the order of points carries meaning.

```razor
<RadzenStack class="rz-p-0 rz-p-md-6 rz-p-lg-12">
    <RadzenRow>
        <RadzenColumn Size="12">
            <RadzenChart Animate="true">
                <RadzenLineSeries Data="@temperatureData" CategoryProperty="Month" ValueProperty="AvgHigh" Title="Avg High" Stroke="rgb(239,83,80)" StrokeWidth="2">
                    <RadzenMarkers MarkerType="MarkerType.Circle" Size="6" Stroke="rgb(239,83,80)" Fill="white" StrokeWidth="2" />
                </RadzenLineSeries>
                <RadzenLineSeries Data="@temperatureData" CategoryProperty="Month" ValueProperty="AvgLow" Title="Avg Low" Stroke="rgb(38,166,154)" StrokeWidth="2">
                    <RadzenMarkers MarkerType="MarkerType.Square" Size="6" Stroke="rgb(38,166,154)" Fill="white" StrokeWidth="2" />
                </RadzenLineSeries>
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
        public double AvgHigh { get; set; }
        public double AvgLow { get; set; }
    }

    DataItem[] temperatureData = new DataItem[]
    {
        new DataItem { Month = "Jan", AvgHigh = 6, AvgLow = -1 },
        new DataItem { Month = "Feb", AvgHigh = 8, AvgLow = 0 },
        new DataItem { Month = "Mar", AvgHigh = 12, AvgLow = 3 },
        new DataItem { Month = "Apr", AvgHigh = 16, AvgLow = 6 },
        new DataItem { Month = "May", AvgHigh = 21, AvgLow = 10 },
        new DataItem { Month = "Jun", AvgHigh = 25, AvgLow = 14 },
        new DataItem { Month = "Jul", AvgHigh = 28, AvgLow = 16 },
        new DataItem { Month = "Aug", AvgHigh = 27, AvgLow = 15 },
        new DataItem { Month = "Sep", AvgHigh = 23, AvgLow = 12 },
        new DataItem { Month = "Oct", AvgHigh = 17, AvgLow = 8 },
        new DataItem { Month = "Nov", AvgHigh = 11, AvgLow = 3 },
        new DataItem { Month = "Dec", AvgHigh = 7, AvgLow = 0 },
    };
}
```
