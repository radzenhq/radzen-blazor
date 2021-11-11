# Chart component

This article demonstrates how to use RadzenBlazorChart.

## Series
The chart can display data as bar, column, line, area, pie and donut series. The chart series needs
data and configuration to tell it which property of the data item is the value of the series (Y axis) and which is the category (X axis).

All series have the following common properties:

- Data - specifies the data source which the series should display.
- ValueProperty - the name of the property which provides values for the Y axis of the chart. The property should be of numeric type:
`int`, `long`, `float`, `double`, `decimal`.

- CategoryProperty - the name of the property which provides value for the X axis of the chart. The property can be `string`, `Date` or numeric. If not set RadzenChart will use the index of the data item as its X axis value.

## Basic usage
Here is a very basic example that creates a column chart with minimal configuration.
```
<RadzenChart>
  <RadzenColumnSeries Data="@revenue" CategoryProperty="Quarter" ValueProperty="Revenue" />
</RadzenChart>
@code {
  class DataItem
  {
      public string Quarter { get; set; }
      public double Revenue { get; set; }
  }

  DataItem[] revenue = new DataItem[]
  {
      new DataItem { Quarter = "Q1", Revenue = 234000 },
      new DataItem { Quarter = "Q2", Revenue = 284000 },
      new DataItem { Quarter = "Q3", Revenue = 274000 },
      new DataItem { Quarter = "Q4", Revenue = 294000 }
  };
}
```

The `RadzenColumnSeries` tag is used to specify that the chart has a column series. The `Data` property specifies the
data source. The chart will render a column for every `DataItem` instance from the `revenue` array. The Y (value) axis displays
the `Revenue` property and the X (category) axis displays the `Quarter` property.

## Axis configuration

### Min, max and step
By default the Radzen Blazor Chart determines the Y axis minimum and maximum based on the range of values. For example it finds the minimum and maximum values and
uses the closes "nice" number. A nice number is usually a multiple of a power of 10:  0, 10, 100, 1000, 200000 etc.
In the previous example the chart automatically uses 200000 as the minimum and 300000 as the maximum.

To override the defaults use the `Min`, `Max` and `Step` properties of the `RadzenValueAxis` tag.

```
<RadzenChart>
  <RadzenColumnSeries Data="@revenue" CategoryProperty="Quarter" ValueProperty="Revenue" />
  <RadzenValueAxis Min="0" Max="400000" Step="100000" />
</RadzenChart>
```

### Format axis values
The value axis displays values with default formatting (`ToString()`). This can be customized in two ways - via the `FormatString` or the `Formatter` properties. `FormatString` supports the [standard .NET Number formats](https://docs.microsoft.com/en-us/dotnet/standard/base-types/standard-numeric-format-strings).
```
<RadzenChart>
  <RadzenColumnSeries Data="@revenue" CategoryProperty="Quarter" ValueProperty="Revenue" />
  <RadzenValueAxis FormatString="{0:C}"/>
</RadzenChart>
```

### Display grid lines
You can make the chart display grid lines for either the value or category axis.
Add a `RadzenGridLines` tag inside `RadzenValueAxis` or `RadzenCategoryAxis` and set its `Visible` property to `true`.
```
<RadzenChart>
  <RadzenColumnSeries Data="@revenue" CategoryProperty="Quarter" ValueProperty="Revenue" />
  <RadzenValueAxis>
      <RadzenGridLines Visible="true" />
  </RadzenValueAxis>
  <RadzenCategoryAxis>
      <RadzenGridLines Visible="true" />
  </RadzenCategoryAxis>
</RadzenChart>
```

### Set axis title
Use the `RadzenAxisTitle` tag to display text below the category axis or next to the value axis.
```
<RadzenChart>
  <RadzenColumnSeries Data="@revenue" CategoryProperty="Quarter" ValueProperty="Revenue" />
  <RadzenValueAxis>
      <RadzenAxisTitle Text="Revenue" />
  </RadzenValueAxis>
  <RadzenCategoryAxis>
      <RadzenAxisTitle Text="Quarter" />
  </RadzenCategoryAxis>
</RadzenChart>
```

## Legend configuration
The Radzen Blazor Chart displays a legend by default. It uses the `Title` property of the series (or category values for pie series) as items in the legend.
The legend is at the right side of the chart by default. You can change the position of the legend via the `Position` property.

### Legend position
```
<RadzenChart>
  <RadzenColumnSeries Data="@revenue" CategoryProperty="Quarter" ValueProperty="Revenue" />
  <RadzenLegend Position="LegendPosition.Bottom" />
</RadzenChart>
```
### Hide the legend
To hide the legend set the `Visible` property to `false`.
```
<RadzenChart>
  <RadzenColumnSeries Data="@revenue" CategoryProperty="Quarter" ValueProperty="Revenue" />
  <RadzenLegend Visible="false" />
</RadzenChart>
```
## Tooltip
The Radzen Blazor chart displays a tooltip when the user hovers series with the mouse. The tooltip by default inclused the series category, value and series name.

### Customize tooltip content
To customize the tooltip content use the `TooltipTemplate` setting of the series.
```
<RadzenChart>
  <RadzenColumnSeries Data="@revenue" CategoryProperty="Quarter" ValueProperty="Revenue">
    <TooltipTemplate Context="data">
      <div>
        Revenue for <span>@data.Quarter</span> 2020:
        <strong>@data.Revenue</strong>
      </div>
    </TooltipTemplate>
  </RadzenColumnSeries>
</RadzenChart>
```
### Disable tooltips
To disable the tooltips set the `Visible` property of the `RadzenChartTooltipOptions` tag to `false`.
```
<RadzenChart>
  <RadzenColumnSeries Data="@revenue" CategoryProperty="Quarter" ValueProperty="Revenue" />
  <RadzenChartTooltipOptions Visible="false" />
</RadzenChart>
```
