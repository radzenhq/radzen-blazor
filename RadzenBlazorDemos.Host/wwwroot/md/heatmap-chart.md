# Heatmap

Show values on a labeled color-coded grid with a Blazor heatmap - calendars, matrices, and density. Free and open source.

Keywords: chart, heatmap, grid, matrix, color, intensity

> API reference: [RadzenHeatmap API](https://blazor.radzen.com/api/heatmap.md)

## Examples

## Radzen Blazor Heatmap Chart

A heatmap lays values out on a labeled grid and colors each cell by size, so patterns and outliers jump out - think a calendar of activity or a correlation matrix.

```razor
<RadzenHeatmap Data="@data" XProperty="Hour" YProperty="Day" ValueProperty="Value"
               ShowValues="true"
               Style="width: 100%; height: 400px;" />

@code {
    class HeatmapData
    {
        public string Hour { get; set; }
        public string Day { get; set; }
        public double Value { get; set; }
    }

    HeatmapData[] data;

    protected override void OnInitialized()
    {
        var days = new[] { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" };
        var hours = new[] { "9AM", "10AM", "11AM", "12PM", "1PM", "2PM", "3PM", "4PM", "5PM" };
        var random = new Random(42);

        var list = new List<HeatmapData>();
        foreach (var day in days)
        {
            foreach (var hour in hours)
            {
                var baseValue = day == "Sat" || day == "Sun" ? 5 : 20;
                var peakBonus = hour == "11AM" || hour == "2PM" ? 15 : 0;
                list.Add(new HeatmapData
                {
                    Day = day,
                    Hour = hour,
                    Value = Math.Round(baseValue + peakBonus + random.NextDouble() * 20, 0)
                });
            }
        }
        data = list.ToArray();
    }
}
```
