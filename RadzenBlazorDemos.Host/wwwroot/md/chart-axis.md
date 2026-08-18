# Axis

Control the scale, gridlines, labels, and title of a Blazor chart's axes, or let them fit the data automatically.

Keywords: chart, graph, series

## Examples

## Radzen Blazor Chart Axis

The axes set the frame a chart is read against - the scale, the gridlines, the labels. By default they fit the data, but you can pin the range, format the values, and add a title.

### Min, max and step

By default the Radzen Blazor Chart determines the Y axis minimum and maximum based on the range of values. For example it finds the minimum and maximum values and uses the closest "nice" number. A nice number is usually a multiple of a power of 10: 0, 10, 100, 1000, 200000 etc.

```razor
<div class="rz-p-0 rz-p-md-12">
    <RadzenChart>
        <RadzenColumnSeries FillMode="FillMode.Gradient" Data="@revenue" CategoryProperty="Quarter" ValueProperty="Revenue" />
        <RadzenValueAxis Min="0" Max="400000" Step="100000" />
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


### Format axis values

The value axis displays values with default formatting (`ToString()`). This can be customized in two ways - via the `FormatString` or the `Formatter` properties. `FormatString` supports the [standard .NET Number formats](https://docs.microsoft.com/en-us/dotnet/standard/base-types/standard-numeric-format-strings).

```razor
<div class="rz-p-0 rz-p-md-12">
    <RadzenChart>
        <RadzenColumnSeries FillMode="FillMode.Gradient" Data="@revenue" CategoryProperty="Quarter" ValueProperty="Revenue" />
        <RadzenValueAxis FormatString="{0:C}" />
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


### Display grid lines

You can make the chart display grid lines for either the value or category axis. Add a `RadzenGridLines` tag inside `RadzenValueAxis` or `RadzenCategoryAxis` and set its `Visible` property to `true`.

```razor
<div class="rz-p-0 rz-p-md-12">
    <RadzenChart>
        <RadzenColumnSeries FillMode="FillMode.Gradient" Data="@revenue" CategoryProperty="Quarter" ValueProperty="Revenue" />
        <RadzenValueAxis>
            <RadzenGridLines Visible="true" />
        </RadzenValueAxis>
        <RadzenCategoryAxis>
            <RadzenGridLines Visible="true" />
        </RadzenCategoryAxis>
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


### Set axis title

Use the `RadzenAxisTitle` tag to display text below the category axis or next to the value axis.

```razor
<div class="rz-p-0 rz-p-md-12">
    <RadzenChart>
        <RadzenColumnSeries FillMode="FillMode.Gradient" Data="@revenue" CategoryProperty="Quarter" ValueProperty="Revenue" />
        <RadzenValueAxis>
            <RadzenAxisTitle Text="Revenue" />
        </RadzenValueAxis>
        <RadzenCategoryAxis>
            <RadzenAxisTitle Text="Quarter" />
        </RadzenCategoryAxis>
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
