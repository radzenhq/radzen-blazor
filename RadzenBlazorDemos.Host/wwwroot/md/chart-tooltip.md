# ToolTip

Show values on hover with Blazor chart tooltips - customize content, share, split, or turn them off.

Keywords: chart, graph, legend, shared, split, tooltip

## Examples

## Radzen Blazor Chart Tooltip

Tooltips show the values behind a point on hover. You can customize their content, share one tooltip across series, split them, or turn them off.

### Customize tooltip content

To customize the tooltip content use the `TooltipTemplate` setting of the series.

```razor
<div class="rz-p-0 rz-p-md-12">
    <RadzenChart>
        <RadzenColumnSeries FillMode="FillMode.Gradient" Data="@revenue" CategoryProperty="Quarter" ValueProperty="Revenue">
            <TooltipTemplate Context="data">
                <div>
                    Revenue for <span>@data.Quarter</span> 2022:
                    <strong>@data.Revenue</strong>
                </div>
            </TooltipTemplate>
        </RadzenColumnSeries>
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


### Shared tooltip

Set `Shared="true"` on `RadzenChartTooltipOptions` to display a single tooltip listing the values of all series at the hovered category.

```razor
<div class="rz-p-0 rz-p-md-12">
    <RadzenChart>
        <RadzenChartTooltipOptions Shared="true" />
        <RadzenLineSeries Smooth="true" Data="@solar" CategoryProperty="Year" ValueProperty="Capacity" Title="Solar">
            <RadzenMarkers MarkerType="MarkerType.Circle" />
        </RadzenLineSeries>
        <RadzenLineSeries Smooth="true" Data="@wind" CategoryProperty="Year" ValueProperty="Capacity" Title="Wind">
            <RadzenMarkers MarkerType="MarkerType.Circle" />
        </RadzenLineSeries>
        <RadzenCategoryAxis Padding="20">
            <RadzenAxisTitle Text="Year" />
        </RadzenCategoryAxis>
        <RadzenValueAxis Formatter="@FormatGW">
            <RadzenGridLines Visible="true" />
            <RadzenAxisTitle Text="Installed capacity (GW)" />
        </RadzenValueAxis>
        <RadzenLegend Position="LegendPosition.Top" />
    </RadzenChart>
</div>

@code {
    class DataItem
    {
        public int Year { get; set; }
        public double Capacity { get; set; }
    }

    string FormatGW(object value) => $"{((double)value).ToString("N0", CultureInfo.InvariantCulture)} GW";

    // Approximate global installed capacity, gigawatts (GW). Source: IRENA.
    DataItem[] solar = new[]
    {
        new DataItem { Year = 2005, Capacity = 5 },     new DataItem { Year = 2007, Capacity = 9 },
        new DataItem { Year = 2009, Capacity = 23 },    new DataItem { Year = 2011, Capacity = 73 },
        new DataItem { Year = 2013, Capacity = 138 },   new DataItem { Year = 2015, Capacity = 228 },
        new DataItem { Year = 2017, Capacity = 391 },   new DataItem { Year = 2019, Capacity = 587 },
        new DataItem { Year = 2021, Capacity = 849 },   new DataItem { Year = 2023, Capacity = 1419 },
    };

    DataItem[] wind = new[]
    {
        new DataItem { Year = 2005, Capacity = 59 },    new DataItem { Year = 2007, Capacity = 94 },
        new DataItem { Year = 2009, Capacity = 159 },   new DataItem { Year = 2011, Capacity = 238 },
        new DataItem { Year = 2013, Capacity = 320 },   new DataItem { Year = 2015, Capacity = 433 },
        new DataItem { Year = 2017, Capacity = 540 },   new DataItem { Year = 2019, Capacity = 650 },
        new DataItem { Year = 2021, Capacity = 825 },   new DataItem { Year = 2023, Capacity = 1017 },
    };
}
```


### Split tooltip

Set `Split="true"` on `RadzenChartTooltipOptions` to render one small tooltip box per series, each anchored near its own data point at the hovered category.

```razor
<div class="rz-p-0 rz-p-md-12">
    <RadzenChart>
        <RadzenChartTooltipOptions Split="true" />
        <RadzenLineSeries Smooth="true" Data="@solar" CategoryProperty="Year" ValueProperty="Capacity" Title="Solar">
            <RadzenMarkers MarkerType="MarkerType.Circle" />
        </RadzenLineSeries>
        <RadzenLineSeries Smooth="true" Data="@wind" CategoryProperty="Year" ValueProperty="Capacity" Title="Wind">
            <RadzenMarkers MarkerType="MarkerType.Circle" />
        </RadzenLineSeries>
        <RadzenCategoryAxis Padding="20">
            <RadzenAxisTitle Text="Year" />
        </RadzenCategoryAxis>
        <RadzenValueAxis Formatter="@FormatGW">
            <RadzenGridLines Visible="true" />
            <RadzenAxisTitle Text="Installed capacity (GW)" />
        </RadzenValueAxis>
        <RadzenLegend Position="LegendPosition.Top" />
    </RadzenChart>
</div>

@code {
    class DataItem
    {
        public int Year { get; set; }
        public double Capacity { get; set; }
    }

    string FormatGW(object value) => $"{((double)value).ToString("N0", CultureInfo.InvariantCulture)} GW";

    // Approximate global installed capacity, gigawatts (GW). Source: IRENA.
    DataItem[] solar = new[]
    {
        new DataItem { Year = 2005, Capacity = 5 },     new DataItem { Year = 2007, Capacity = 9 },
        new DataItem { Year = 2009, Capacity = 23 },    new DataItem { Year = 2011, Capacity = 73 },
        new DataItem { Year = 2013, Capacity = 138 },   new DataItem { Year = 2015, Capacity = 228 },
        new DataItem { Year = 2017, Capacity = 391 },   new DataItem { Year = 2019, Capacity = 587 },
        new DataItem { Year = 2021, Capacity = 849 },   new DataItem { Year = 2023, Capacity = 1419 },
    };

    DataItem[] wind = new[]
    {
        new DataItem { Year = 2005, Capacity = 59 },    new DataItem { Year = 2007, Capacity = 94 },
        new DataItem { Year = 2009, Capacity = 159 },   new DataItem { Year = 2011, Capacity = 238 },
        new DataItem { Year = 2013, Capacity = 320 },   new DataItem { Year = 2015, Capacity = 433 },
        new DataItem { Year = 2017, Capacity = 540 },   new DataItem { Year = 2019, Capacity = 650 },
        new DataItem { Year = 2021, Capacity = 825 },   new DataItem { Year = 2023, Capacity = 1017 },
    };
}
```


### Disable tooltips

To disable the tooltips, set the `Visible` property of the `RadzenChartTooltipOptions` tag to false.

```razor
<div class="rz-p-0 rz-p-md-12">
    <RadzenChart>
        <RadzenColumnSeries FillMode="FillMode.Gradient" Data="@revenue" CategoryProperty="Quarter" ValueProperty="Revenue" />
        <RadzenChartTooltipOptions Visible="false" />
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
