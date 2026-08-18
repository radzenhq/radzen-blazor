# Legend

Show, move, restyle, or hide the legend that tells readers which series is which on a Blazor chart.

Keywords: chart, graph, legend

> API reference: [RadzenLegend API](https://blazor.radzen.com/api/legend.md)

## Examples

## Radzen Blazor Chart Legend

The legend tells readers which series is which. It shows by default using each series title; you can move it, restyle it, or hide it entirely.

### Legend position

The legend is at the right side of the chart by default. Change the position of the legend via the `Position` property.

```razor
<div class="rz-p-0 rz-p-md-12">
    <RadzenChart>
        <RadzenColumnSeries FillMode="FillMode.Gradient" Data="@revenue" CategoryProperty="Quarter" ValueProperty="Revenue" />
        <RadzenLegend Position="LegendPosition.Bottom" />
    </RadzenChart>
</div>

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


### Hide the legend

To hide the legend set the `Visible` property to `false`.

```razor
<div class="rz-p-0 rz-p-md-12">
    <RadzenChart>
        <RadzenColumnSeries FillMode="FillMode.Gradient" Data="@revenue" CategoryProperty="Quarter" ValueProperty="Revenue" />
        <RadzenLegend Visible="false" />
    </RadzenChart>
</div>

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
