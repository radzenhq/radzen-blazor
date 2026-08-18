# Series

Bind data to a line, bar, pie, or other series - the building block of every Blazor chart.

Keywords: chart, graph, series

## Examples

## Radzen Blazor Chart

The series is the heart of a chart - each one binds a set of data to a visual form like a line, bar, or pie. Most chart questions start here: how to bind data, combine series, and handle interaction.

### Chart Series

The chart can display [area](/area-chart), [bar](/bar-chart), [column](/column-chart), [donut](/donut-chart), [line](/line-chart), and [pie](/pie-chart) data series. The chart series needs data and configuration to tell it which property of the data item is the value of the series (Y axis) and which is the category (X axis).
All series have the following common properties:
`Data` - specifies the data source which the series should display.
`ValueProperty` - the name of the property which provides values for the Y axis of the chart. The property should be of numeric type: `int`, `long`, `float`, `double`, `decimal`.
`CategoryProperty` - the name of the property which provides value for the X axis of the chart. The property can be `string`, `Date` or `numeric`. If not set RadzenChart will use the index of the data item as its X axis value.

### Basic usage

Here is a very basic example that creates a column chart with minimal configuration.
The `RadzenColumnSeries` tag is used to specify that the chart has a column series. The `Data` property specifies the data source. The chart will render a column for every `DataItem` instance from the `revenue` array. The Y (value) axis displays the `Revenue` property and the X (category) axis displays the `Quarter` property.

```razor
<div class="rz-p-0 rz-p-md-12">
    <RadzenChart>
        <RadzenColumnSeries FillMode="FillMode.Gradient" Data="@revenue1" CategoryProperty="Quarter" ValueProperty="Revenue" />
        <RadzenColumnSeries FillMode="FillMode.Gradient" Data="@revenue2" CategoryProperty="Quarter" ValueProperty="Revenue" />
    </RadzenChart>
</div>

@code {
  class DataItem
  {
      public string Quarter { get; set; }
      public double Revenue { get; set; }
  }

  DataItem[] revenue1 = new DataItem[]
  {
      new DataItem { Quarter = "Q1", Revenue = 234000 },
      new DataItem { Quarter = "Q2", Revenue = 284000 },
      new DataItem { Quarter = "Q3", Revenue = 274000 },
      new DataItem { Quarter = "Q4", Revenue = 294000 }
  };

  DataItem[] revenue2 = new DataItem[]
  {
      new DataItem { Quarter = "Q1", Revenue = 324000 },
      new DataItem { Quarter = "Q2", Revenue = 224000 },
      new DataItem { Quarter = "Q3", Revenue = 444000 },
      new DataItem { Quarter = "Q4", Revenue = 564000 }
  };
}
```
