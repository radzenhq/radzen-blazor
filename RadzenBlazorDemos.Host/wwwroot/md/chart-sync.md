# Synchronized Charts

Link multiple Blazor charts with a shared crosshair, active point, and tooltip using synchronized charts.

Keywords: chart, graph, sync, synchronized, crosshair, tooltip, dashboard, linked, export

## Examples

## Radzen Blazor synchronized charts

Synchronized charts share a crosshair, highlighted point, and tooltip, so hovering one chart updates the others - handy for dashboards where several charts share the same time axis.

```razor
<RadzenStack class="rz-p-0 rz-p-md-6 rz-p-lg-12">
    <RadzenCard Variant="Variant.Outlined">
        <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="1rem">
            <RadzenText TextStyle="TextStyle.Subtitle2" TagName="TagName.H3" Style="margin: 0;">Hover either chart - the other follows</RadzenText>
        </RadzenStack>
    </RadzenCard>

    <RadzenChart SyncGroup="dashboard" Style="height: 240px;">
        <RadzenAreaSeries FillMode="FillMode.Gradient" Smooth="true" Data="@revenue" CategoryProperty="Date" ValueProperty="Value" Title="Revenue" />
        <RadzenCategoryAxis FormatString="{0:MMM}">
            <RadzenAxisCrosshair Visible="true" />
        </RadzenCategoryAxis>
        <RadzenValueAxis Formatter="@FormatAsUSD">
            <RadzenGridLines Visible="true" LineType="LineType.Dashed" />
        </RadzenValueAxis>
    </RadzenChart>

    <RadzenChart SyncGroup="dashboard" Style="height: 240px;">
        <RadzenLineSeries Smooth="true" Data="@orders" CategoryProperty="Date" ValueProperty="Value" Title="Orders" />
        <RadzenCategoryAxis FormatString="{0:MMM}">
            <RadzenAxisCrosshair Visible="true" />
        </RadzenCategoryAxis>
        <RadzenValueAxis>
            <RadzenGridLines Visible="true" LineType="LineType.Dashed" />
        </RadzenValueAxis>
    </RadzenChart>
</RadzenStack>

@code {
    class DataItem
    {
        public DateTime Date { get; set; }
        public double Value { get; set; }
    }

DataItem[] revenue = Array.Empty<DataItem>();
    DataItem[] orders = Array.Empty<DataItem>();

    string FormatAsUSD(object value)
    {
        return ((double)value).ToString("C0", CultureInfo.CreateSpecificCulture("en-US"));
    }

    protected override void OnInitialized()
    {
        var random = new Random(42);
        var start = new DateTime(DateTime.Today.Year, 1, 1);

        revenue = Enumerable.Range(0, 12).Select(i => new DataItem
        {
            Date = start.AddMonths(i),
            Value = 200000 + i * 12000 + random.Next(-30000, 30000)
        }).ToArray();

        orders = Enumerable.Range(0, 12).Select(i => new DataItem
        {
            Date = start.AddMonths(i),
            Value = 1200 + i * 80 + random.Next(-200, 200)
        }).ToArray();
    }
}
```
