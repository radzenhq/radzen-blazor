# Range Navigator

Frame the visible window of a chart with a draggable Blazor range navigator overview strip.

Keywords: chart, range, navigator, selector, zoom, pan, interactive

> API reference: [RadzenRangeNavigator API](https://blazor.radzen.com/api/rangenavigator.md)

## Examples

## Radzen Blazor Range Navigator

A range navigator is a small overview chart with a draggable selection that controls the visible range of a larger chart - the familiar strip you drag to frame a window of time.

### With Series

Use `RadzenRangeNavigatorLineSeries` as child content to display a mini-chart preview of the full data range inside the navigator.

```razor
<RadzenStack class="rz-p-0 rz-p-md-6 rz-p-lg-12">
    <RadzenCard Variant="Variant.Outlined">
        <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="1rem">
            <RadzenText TextStyle="TextStyle.Subtitle2" TagName="TagName.H3" Style="margin: 0;">ACME Corp (ACME) — Daily Stock Price</RadzenText>
            <RadzenButton Text="Reset Zoom" Click="@ResetRange" Variant="Variant.Outlined" Size="ButtonSize.Small" />
            <RadzenButton Text="Last 3 Months" Click="@LastThreeMonths" Variant="Variant.Outlined" Size="ButtonSize.Small" />
            <RadzenButton Text="Last 6 Months" Click="@LastSixMonths" Variant="Variant.Outlined" Size="ButtonSize.Small" />
        </RadzenStack>
    </RadzenCard>

    <RadzenChart AllowZoom="true" AllowPan="true" @bind-ViewStart="@start" @bind-ViewEnd="@end" Style="height: 400px;">
        <RadzenCandlestickSeries Data="@stockData" CategoryProperty="Date"
            OpenProperty="Open" HighProperty="High" LowProperty="Low" CloseProperty="Close"
            Title="ACME Corp" BullFill="#26A69A" BearFill="#EF5350" />
        <RadzenLineSeries Data="@stockData" CategoryProperty="Date" ValueProperty="SMA20" Title="SMA 20" Stroke="#FF9800" StrokeWidth="1.5">
            <RadzenMarkers Visible="false" />
        </RadzenLineSeries>
        <RadzenCategoryAxis Padding="20" FormatString="{0:MMM dd}" />
        <RadzenValueAxis Formatter="@FormatAsUSD">
            <RadzenGridLines Visible="true" />
            <RadzenAxisTitle Text="Price (USD)" />
        </RadzenValueAxis>
        <RadzenLegend Position="LegendPosition.Bottom" />
        <RadzenChartRangeNavigator Height="80" ShowHandleLabels="true" HandleLabelFormatString="{0:MMM dd, yyyy}">
            <RadzenRangeNavigatorLineSeries FillMode="FillMode.Gradient" Data="@stockData" CategoryProperty="Date" ValueProperty="Close" Stroke="#1E88E5" />
        </RadzenChartRangeNavigator>
    </RadzenChart>
</RadzenStack>

@code {
    class StockDataItem
    {
        public DateTime Date { get; set; }
        public double Open { get; set; }
        public double High { get; set; }
        public double Low { get; set; }
        public double Close { get; set; }
        public double Volume { get; set; }
        public double SMA20 { get; set; }
    }

    double start = 0;
    double end = 1;
    List<StockDataItem> stockData = new();

    string FormatAsUSD(object value)
    {
        return ((double)value).ToString("C0", CultureInfo.CreateSpecificCulture("en-US"));
    }

    void ResetRange()
    {
        start = 0;
        end = 1;
    }

    void LastThreeMonths()
    {
        start = 0.75;
        end = 1;
    }

    void LastSixMonths()
    {
        start = 0.5;
        end = 1;
    }

    protected override void OnInitialized()
    {
        var random = new Random(123);
        var startDate = new DateTime(2024, 1, 2);
        double price = 150.0;

        // Generate ~1 year of daily stock data (trading days only)
        for (int i = 0; i < 252; i++)
        {
            var date = startDate.AddDays(i);

            // Skip weekends
            if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
            {
                continue;
            }

            // Random walk with slight upward drift
            var dailyReturn = (random.NextDouble() - 0.48) * 4.0;
            var open = price;
            var close = Math.Round(open + dailyReturn, 2);
            var high = Math.Round(Math.Max(open, close) + random.NextDouble() * 3.0, 2);
            var low = Math.Round(Math.Min(open, close) - random.NextDouble() * 3.0, 2);
            var volume = Math.Round(2_000_000 + random.NextDouble() * 8_000_000);

            stockData.Add(new StockDataItem
            {
                Date = date,
                Open = open,
                High = high,
                Low = low,
                Close = close,
                Volume = volume
            });

            price = close;
        }

        // Calculate 20-day Simple Moving Average
        for (int i = 0; i < stockData.Count; i++)
        {
            if (i >= 19)
            {
                var sum = 0.0;
                for (int j = i - 19; j <= i; j++)
                {
                    sum += stockData[j].Close;
                }
                stockData[i].SMA20 = Math.Round(sum / 20, 2);
            }
            else
            {
                stockData[i].SMA20 = stockData[i].Close;
            }
        }
    }
}
```


### Compact

Use the `RadzenRangeNavigator` without child content for a compact range selector. The component automatically renders as a compact track bar.

```razor
<RadzenStack class="rz-p-0 rz-p-md-6 rz-p-lg-12">
    <RadzenCard Variant="Variant.Outlined">
        <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="1rem" Wrap="FlexWrap.Wrap">
            <RadzenText TextStyle="TextStyle.Subtitle2" TagName="TagName.P" Style="margin: 0;">Selected: @GetDateLabel(start) — @GetDateLabel(end)</RadzenText>
            <RadzenButton Text="Full Year" Click="@(() => { start = 0; end = 1; })" Variant="Variant.Outlined" Size="ButtonSize.Small" />
            <RadzenButton Text="Q1" Click="@(() => { start = 0; end = 0.25; })" Variant="Variant.Outlined" Size="ButtonSize.Small" />
            <RadzenButton Text="Q2" Click="@(() => { start = 0.25; end = 0.5; })" Variant="Variant.Outlined" Size="ButtonSize.Small" />
            <RadzenButton Text="H2" Click="@(() => { start = 0.5; end = 1; })" Variant="Variant.Outlined" Size="ButtonSize.Small" />
        </RadzenStack>
    </RadzenCard>

    <RadzenChart AllowZoom="true" AllowPan="true" @bind-ViewStart="@start" @bind-ViewEnd="@end">
        <RadzenLineSeries Data="@data" CategoryProperty="Date" ValueProperty="Revenue" Title="Revenue" Smooth="true" Stroke="#1E88E5">
            <RadzenMarkers Visible="false" />
        </RadzenLineSeries>
        <RadzenLineSeries Data="@data" CategoryProperty="Date" ValueProperty="Expenses" Title="Expenses" Smooth="true" Stroke="#EF5350">
            <RadzenMarkers Visible="false" />
        </RadzenLineSeries>
        <RadzenCategoryAxis Padding="20" FormatString="{0:MMM dd}" />
        <RadzenValueAxis Formatter="@FormatAsUSD">
            <RadzenGridLines Visible="true" />
        </RadzenValueAxis>
        <RadzenLegend Position="LegendPosition.Bottom" />
    </RadzenChart>

    <RadzenRangeNavigator @bind-Start="@start" @bind-End="@end" Style="margin-top: 16px;"
        ShowAxis="true" Min="@rangeStart" Max="@rangeEnd" AxisFormatString="{0:MMM}" />
</RadzenStack>

@code {
    class DataItem
    {
        public DateTime Date { get; set; }
        public double Revenue { get; set; }
        public double Expenses { get; set; }
    }

    double start = 0;
    double end = 1;
    DateTime rangeStart = new(2025, 1, 1);
    DateTime rangeEnd = new(2025, 12, 31);
    List<DataItem> data = new();

    string FormatAsUSD(object value)
    {
        return ((double)value).ToString("C0", CultureInfo.CreateSpecificCulture("en-US"));
    }

    string GetDateLabel(double fraction)
    {
        var totalDays = (rangeEnd - rangeStart).TotalDays;
        var date = rangeStart.AddDays(fraction * totalDays);
        return date.ToString("MMM dd, yyyy");
    }

    protected override void OnInitialized()
    {
        var random = new Random(77);

        for (int i = 0; i < 365; i++)
        {
            var date = rangeStart.AddDays(i);
            var seasonalRevenue = 50000 + 15000 * Math.Sin((i - 60) * Math.PI / 182.5);
            var seasonalExpenses = 35000 + 8000 * Math.Sin((i - 90) * Math.PI / 182.5);

            data.Add(new DataItem
            {
                Date = date,
                Revenue = Math.Round(seasonalRevenue + random.NextDouble() * 10000 - 5000),
                Expenses = Math.Round(seasonalExpenses + random.NextDouble() * 6000 - 3000)
            });
        }
    }
}
```
