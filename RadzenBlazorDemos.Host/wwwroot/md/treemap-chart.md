# Treemap

Show hierarchy and proportion as nested rectangles with a Blazor treemap.

Keywords: chart, treemap, hierarchy, rectangle, proportion, area

> API reference: [RadzenTreemap API](https://blazor.radzen.com/api/treemap.md)

## Examples

## Radzen Blazor Treemap Chart

A treemap fills space with nested rectangles sized by value, so it can show a whole hierarchy and the relative weight of every part in one compact view - disk usage or budget breakdowns are classic cases.

```razor
<RadzenTreemap Data="@sectors" ValueProperty="MarketCap" TextProperty="Name"
               ShowLabels="true" ShowValues="true" Padding="3"
               Style="width: 100%; height: 500px;" />

@code {
    class SectorData
    {
        public string Name { get; set; }
        public double MarketCap { get; set; }
    }

    SectorData[] sectors = new[]
    {
        new SectorData { Name = "Technology", MarketCap = 12500 },
        new SectorData { Name = "Healthcare", MarketCap = 7200 },
        new SectorData { Name = "Financials", MarketCap = 6800 },
        new SectorData { Name = "Consumer", MarketCap = 5100 },
        new SectorData { Name = "Energy", MarketCap = 4300 },
        new SectorData { Name = "Industrials", MarketCap = 3900 },
        new SectorData { Name = "Telecom", MarketCap = 2800 },
        new SectorData { Name = "Materials", MarketCap = 2200 },
        new SectorData { Name = "Utilities", MarketCap = 1800 },
        new SectorData { Name = "Real Estate", MarketCap = 1500 },
    };
}
```
