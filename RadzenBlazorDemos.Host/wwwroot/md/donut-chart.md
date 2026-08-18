# Donut Chart

Show proportions with a Blazor donut chart - a pie with an open center for a total or label.

Keywords: chart, graph, donut

## Examples

## Radzen Blazor Chart donut series

A donut chart is a pie chart with the center removed, freeing the middle for a total or label. The slices still show each category's share of the whole.

### Showcase

A real-world revenue breakdown that combines a custom color palette, gradient fills, a total in the center, rounded corners and a subtle segment gap.

```razor
<RadzenStack class="rz-p-0 rz-p-md-6 rz-p-lg-12" AlignItems="AlignItems.Center">
    <RadzenStack Style="width: 100%; max-width: 640px;">
        <RadzenChart Animate="true" Style="height: 420px;">
            <RadzenDonutSeries Data="@revenue" CategoryProperty="Category" ValueProperty="Revenue" Title="Revenue"
                               Fills="@palette" FillMode="FillMode.Gradient" SegmentGap="2" CornerRadius="4" Radius="150">
                <ChildContent>
                    <RadzenSeriesDataLabels Visible="false" />
                </ChildContent>
                <TitleTemplate>
                    <div class="rz-donut-content">
                        <div>Total revenue</div>
                        <div>@Total</div>
                    </div>
                </TitleTemplate>
            </RadzenDonutSeries>
            <RadzenLegend Position="LegendPosition.Bottom" />
        </RadzenChart>
    </RadzenStack>
</RadzenStack>

@code {
    class DataItem
    {
        public string Category { get; set; }
        public double Revenue { get; set; }
    }

    string[] palette = new[] { "#6366f1", "#0ea5e9", "#14b8a6", "#22c55e", "#f59e0b", "#f43f5e", "#a855f7" };

    DataItem[] revenue = new DataItem[]
    {
        new DataItem { Category = "Cloud Platform", Revenue = 4820000 },
        new DataItem { Category = "Licenses", Revenue = 3260000 },
        new DataItem { Category = "Professional Services", Revenue = 2140000 },
        new DataItem { Category = "Support", Revenue = 1680000 },
        new DataItem { Category = "Training", Revenue = 920000 },
        new DataItem { Category = "Marketplace", Revenue = 760000 },
        new DataItem { Category = "Hardware", Revenue = 540000 },
    };

    string Total => (revenue.Sum(r => r.Revenue) / 1_000_000).ToString("C1", CultureInfo.CreateSpecificCulture("en-US")) + "M";
}
```


### Basic donut

Bind a collection and set `CategoryProperty` and `ValueProperty`. Each value is converted to its share of the whole automatically.

```razor
<RadzenStack class="rz-p-0 rz-p-md-6 rz-p-lg-12" AlignItems="AlignItems.Center">
    <RadzenStack Style="width: 100%; max-width: 600px;">
        <RadzenChart>
            <RadzenDonutSeries Data="@revenue" CategoryProperty="Category" ValueProperty="Value" Title="Revenue">
                <RadzenSeriesDataLabels Visible="false" />
            </RadzenDonutSeries>
            <RadzenLegend Position="LegendPosition.Bottom" />
        </RadzenChart>
    </RadzenStack>
</RadzenStack>

@code {
    class DataItem
    {
        public string Category { get; set; }
        public double Value { get; set; }
    }

    DataItem[] revenue = new DataItem[]
    {
        new DataItem { Category = "Cloud", Value = 4200 },
        new DataItem { Category = "Licenses", Value = 3100 },
        new DataItem { Category = "Services", Value = 2300 },
        new DataItem { Category = "Support", Value = 1500 },
        new DataItem { Category = "Training", Value = 900 },
    };
}
```


### Inner radius

Set `InnerRadius` (in pixels) to control the size of the hole. By default it is half of the outer radius.

```razor
<RadzenStack class="rz-p-0 rz-p-md-6 rz-p-lg-12" AlignItems="AlignItems.Center">
    <RadzenStack Style="width: 100%; max-width: 600px;">
        <RadzenChart>
            <RadzenDonutSeries Data="@revenue" CategoryProperty="Category" ValueProperty="Value" Title="Revenue" InnerRadius="50">
                <RadzenSeriesDataLabels Visible="false" />
            </RadzenDonutSeries>
            <RadzenLegend Position="LegendPosition.Bottom" />
        </RadzenChart>
    </RadzenStack>
</RadzenStack>

@code {
    class DataItem
    {
        public string Category { get; set; }
        public double Value { get; set; }
    }

    DataItem[] revenue = new DataItem[]
    {
        new DataItem { Category = "Cloud", Value = 4200 },
        new DataItem { Category = "Licenses", Value = 3100 },
        new DataItem { Category = "Services", Value = 2300 },
        new DataItem { Category = "Support", Value = 1500 },
        new DataItem { Category = "Training", Value = 900 },
    };
}
```


### Center label

Use the `TitleTemplate` to place a total or any custom markup in the open center.

```razor
<RadzenStack class="rz-p-0 rz-p-md-6 rz-p-lg-12" AlignItems="AlignItems.Center">
    <RadzenStack Style="width: 100%; max-width: 600px;">
        <RadzenChart>
            <RadzenDonutSeries Data="@revenue" CategoryProperty="Category" ValueProperty="Value" Title="Revenue">
                <TitleTemplate>
                    <div class="rz-donut-content">
                        <div>Total</div>
                        <div>@Total</div>
                    </div>
                </TitleTemplate>
            </RadzenDonutSeries>
            <RadzenLegend Position="LegendPosition.Bottom" />
        </RadzenChart>
    </RadzenStack>
</RadzenStack>

@code {
    class DataItem
    {
        public string Category { get; set; }
        public double Value { get; set; }
    }

    DataItem[] revenue = new DataItem[]
    {
        new DataItem { Category = "Cloud", Value = 4200 },
        new DataItem { Category = "Licenses", Value = 3100 },
        new DataItem { Category = "Services", Value = 2300 },
        new DataItem { Category = "Support", Value = 1500 },
        new DataItem { Category = "Training", Value = 900 },
    };

    string Total => revenue.Sum(r => r.Value).ToString("N0");
}
```


### Various radius

Use the `RadiusProperty` to assign different outer radius values to each segment based on a data property. The inner radius stays constant while each segment extends outward proportionally.

```razor
<RadzenStack class="rz-p-0 rz-p-md-6 rz-p-lg-12" AlignItems="AlignItems.Center">
    <RadzenStack Style="width: 100%; max-width: 600px;">
        <RadzenChart>
            <RadzenDonutSeries FillMode="FillMode.Gradient" Data="@revenue" CategoryProperty="Category" ValueProperty="Value" Title="Revenue" RadiusProperty="Radius">
                <RadzenSeriesDataLabels Visible="false" />
            </RadzenDonutSeries>
            <RadzenLegend Position="LegendPosition.Bottom" />
        </RadzenChart>
    </RadzenStack>
</RadzenStack>

@code {
    class DataItem
    {
        public string Category { get; set; }
        public double Value { get; set; }
        public double Radius { get; set; }
    }

    DataItem[] revenue = new DataItem[]
    {
        new DataItem { Category = "Cloud", Value = 4200, Radius = 110 },
        new DataItem { Category = "Licenses", Value = 3100, Radius = 95 },
        new DataItem { Category = "Services", Value = 2300, Radius = 85 },
        new DataItem { Category = "Support", Value = 1500, Radius = 70 },
        new DataItem { Category = "Training", Value = 900, Radius = 60 },
    };
}
```


### Segment gap

Set `SegmentGap` to separate adjacent segments by a uniform-width space that runs from the outer rim to the inner hole, regardless of segment size.

```razor
<RadzenStack class="rz-p-0 rz-p-md-6 rz-p-lg-12" AlignItems="AlignItems.Center">
    <RadzenStack Style="width: 100%; max-width: 600px;">
        <RadzenChart>
            <RadzenDonutSeries FillMode="FillMode.Gradient" Data="@revenue" CategoryProperty="Category" ValueProperty="Value" Title="Revenue" SegmentGap="4">
                <RadzenSeriesDataLabels Visible="false" />
            </RadzenDonutSeries>
            <RadzenLegend Position="LegendPosition.Bottom" />
        </RadzenChart>
    </RadzenStack>
</RadzenStack>

@code {
    class DataItem
    {
        public string Category { get; set; }
        public double Value { get; set; }
    }

    DataItem[] revenue = new DataItem[]
    {
        new DataItem { Category = "Cloud", Value = 4200 },
        new DataItem { Category = "Licenses", Value = 3100 },
        new DataItem { Category = "Services", Value = 2300 },
        new DataItem { Category = "Support", Value = 1500 },
        new DataItem { Category = "Training", Value = 900 },
    };
}
```


### Rounded corners

Set `CornerRadius` to round the corners of each segment for a softer look.

```razor
<RadzenStack class="rz-p-0 rz-p-md-6 rz-p-lg-12" AlignItems="AlignItems.Center">
    <RadzenStack Style="width: 100%; max-width: 600px;">
        <RadzenChart>
            <RadzenDonutSeries Data="@revenue" CategoryProperty="Category" ValueProperty="Value" Title="Revenue" CornerRadius="10">
                <RadzenSeriesDataLabels Visible="false" />
            </RadzenDonutSeries>
            <RadzenLegend Position="LegendPosition.Bottom" />
        </RadzenChart>
    </RadzenStack>
</RadzenStack>

@code {
    class DataItem
    {
        public string Category { get; set; }
        public double Value { get; set; }
    }

    DataItem[] revenue = new DataItem[]
    {
        new DataItem { Category = "Cloud", Value = 4200 },
        new DataItem { Category = "Licenses", Value = 3100 },
        new DataItem { Category = "Services", Value = 2300 },
        new DataItem { Category = "Support", Value = 1500 },
        new DataItem { Category = "Training", Value = 900 },
    };
}
```


### Semi-circle

Combine `StartAngle` and `TotalAngle` to draw a half donut - useful for gauge-style visuals.

```razor
<RadzenStack class="rz-p-0 rz-p-md-6 rz-p-lg-12" AlignItems="AlignItems.Center">
    <RadzenStack Style="width: 100%; max-width: 600px;">
        <RadzenChart>
            <RadzenDonutSeries Data="@revenue" CategoryProperty="Category" ValueProperty="Value" Title="Revenue" StartAngle="180" TotalAngle="180">
                <RadzenSeriesDataLabels Visible="false" />
            </RadzenDonutSeries>
            <RadzenLegend Position="LegendPosition.Bottom" />
        </RadzenChart>
    </RadzenStack>
</RadzenStack>

@code {
    class DataItem
    {
        public string Category { get; set; }
        public double Value { get; set; }
    }

    DataItem[] revenue = new DataItem[]
    {
        new DataItem { Category = "Cloud", Value = 4200 },
        new DataItem { Category = "Licenses", Value = 3100 },
        new DataItem { Category = "Services", Value = 2300 },
        new DataItem { Category = "Support", Value = 1500 },
        new DataItem { Category = "Training", Value = 900 },
    };
}
```


### Explode on hover

Set `ExplodeOffset` to make segments move outward from the center when hovered, or use `ExplodedProperty` to keep a segment permanently exploded.

```razor
<RadzenStack class="rz-p-0 rz-p-md-6 rz-p-lg-12" AlignItems="AlignItems.Center">
    <RadzenStack Style="width: 100%; max-width: 600px;">
        <RadzenChart>
            <RadzenDonutSeries FillMode="FillMode.Gradient" Data="@revenue" CategoryProperty="Category" ValueProperty="Value" Title="Revenue" ExplodeOffset="15" ExplodedProperty="Highlight">
                <RadzenSeriesDataLabels Visible="false" />
            </RadzenDonutSeries>
            <RadzenLegend Position="LegendPosition.Bottom" />
        </RadzenChart>
    </RadzenStack>
</RadzenStack>

@code {
    class DataItem
    {
        public string Category { get; set; }
        public double Value { get; set; }
        public bool Highlight { get; set; }
    }

    DataItem[] revenue = new DataItem[]
    {
        new DataItem { Category = "Cloud", Value = 4200, Highlight = true },
        new DataItem { Category = "Licenses", Value = 3100 },
        new DataItem { Category = "Services", Value = 2300 },
        new DataItem { Category = "Support", Value = 1500 },
        new DataItem { Category = "Training", Value = 900 },
    };
}
```


### Custom colors

Provide your own palette with `Fills` (and `Strokes`). Combine with `FillMode` to switch between solid and gradient fills.

```razor
<RadzenStack class="rz-p-0 rz-p-md-6 rz-p-lg-12" AlignItems="AlignItems.Center">
    <RadzenStack Style="width: 100%; max-width: 600px;">
        <RadzenChart>
            <RadzenDonutSeries Data="@revenue" CategoryProperty="Category" ValueProperty="Value" Title="Revenue" Fills="@palette" FillMode="FillMode.Solid">
                <RadzenSeriesDataLabels Visible="false" />
            </RadzenDonutSeries>
            <RadzenLegend Position="LegendPosition.Bottom" />
        </RadzenChart>
    </RadzenStack>
</RadzenStack>

@code {
    class DataItem
    {
        public string Category { get; set; }
        public double Value { get; set; }
    }

    string[] palette = new[] { "#6366f1", "#0ea5e9", "#14b8a6", "#22c55e", "#f59e0b" };

    DataItem[] revenue = new DataItem[]
    {
        new DataItem { Category = "Cloud", Value = 4200 },
        new DataItem { Category = "Licenses", Value = 3100 },
        new DataItem { Category = "Services", Value = 2300 },
        new DataItem { Category = "Support", Value = 1500 },
        new DataItem { Category = "Training", Value = 900 },
    };
}
```


### Labels and legend

Add `RadzenSeriesDataLabels` to print values on the segments and position the `RadzenLegend`. Set `ShowTooltipOnLegend` to surface tooltips from the legend too.

```razor
<RadzenStack class="rz-p-0 rz-p-md-6 rz-p-lg-12" AlignItems="AlignItems.Center">
    <RadzenStack Style="width: 100%; max-width: 600px;">
        <RadzenChart>
            <RadzenDonutSeries Data="@revenue" CategoryProperty="Category" ValueProperty="Value" Title="Revenue" ShowTooltipOnLegend="true">
                <RadzenSeriesDataLabels Visible="true" Position="DataLabelPosition.Inside" />
            </RadzenDonutSeries>
            <RadzenLegend Position="LegendPosition.Right" />
        </RadzenChart>
    </RadzenStack>
</RadzenStack>

@code {
    class DataItem
    {
        public string Category { get; set; }
        public double Value { get; set; }
    }

    DataItem[] revenue = new DataItem[]
    {
        new DataItem { Category = "Cloud", Value = 4200 },
        new DataItem { Category = "Licenses", Value = 3100 },
        new DataItem { Category = "Services", Value = 2300 },
        new DataItem { Category = "Support", Value = 1500 },
        new DataItem { Category = "Training", Value = 900 },
    };
}
```


### Playground

Combine the settings above and see how they interact - fill mode, inner radius, segment gap, corner radius, semi-circle, data labels and legend.

```razor
<RadzenStack class="rz-p-0 rz-p-md-6 rz-p-lg-12">
    <RadzenCard Variant="Variant.Outlined">
        <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Wrap="FlexWrap.Wrap" Gap="2rem">
            <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="0.5rem">
                <RadzenLabel Text="Fill" Component="fillMode" />
                <RadzenSelectBar @bind-Value="@fillMode" TValue="FillMode" Size="ButtonSize.Small" Name="fillMode">
                    <Items>
                        <RadzenSelectBarItem Value="FillMode.Gradient" Text="Gradient" />
                        <RadzenSelectBarItem Value="FillMode.Solid" Text="Solid" />
                        <RadzenSelectBarItem Value="FillMode.None" Text="None" />
                    </Items>
                </RadzenSelectBar>
            </RadzenStack>
            <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="0.5rem">
                <RadzenLabel Text="Inner radius" Component="innerRadius" />
                <RadzenNumeric @bind-Value="@innerRadius" TValue="double" Min="0" Max="120" Step="5" Name="innerRadius" InputSize="InputSize.Small" Style="width: 6rem;" />
            </RadzenStack>
            <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="0.5rem">
                <RadzenLabel Text="Segment gap" Component="segmentGap" />
                <RadzenNumeric @bind-Value="@segmentGap" TValue="double" Min="0" Max="20" Step="1" Name="segmentGap" InputSize="InputSize.Small" Style="width: 6rem;" />
            </RadzenStack>
            <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="0.5rem">
                <RadzenLabel Text="Corner radius" Component="cornerRadius" />
                <RadzenNumeric @bind-Value="@cornerRadius" TValue="double" Min="0" Max="20" Step="1" Name="cornerRadius" InputSize="InputSize.Small" Style="width: 6rem;" />
            </RadzenStack>
            <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="0.5rem">
                <RadzenCheckBox @bind-Value="@semiCircle" Name="semiCircle"></RadzenCheckBox>
                <RadzenLabel Text="Semi-circle" Component="semiCircle" />
            </RadzenStack>
            <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="0.5rem">
                <RadzenCheckBox @bind-Value="@showDataLabels" Name="dataLabels"></RadzenCheckBox>
                <RadzenLabel Text="Data labels" Component="dataLabels" />
            </RadzenStack>
            @if (showDataLabels)
            {
                <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="0.5rem">
                    <RadzenLabel Text="Label position" Component="labelPosition" />
                    <RadzenSelectBar @bind-Value="@labelPosition" TValue="DataLabelPosition" Size="ButtonSize.Small" Name="labelPosition">
                        <Items>
                            <RadzenSelectBarItem Value="DataLabelPosition.Auto" Text="Auto" />
                            <RadzenSelectBarItem Value="DataLabelPosition.Inside" Text="Inside" />
                            <RadzenSelectBarItem Value="DataLabelPosition.Center" Text="Center" />
                        </Items>
                    </RadzenSelectBar>
                </RadzenStack>
            }
            <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="0.5rem">
                <RadzenCheckBox @bind-Value="@showLegend" Name="legend"></RadzenCheckBox>
                <RadzenLabel Text="Legend" Component="legend" />
            </RadzenStack>
        </RadzenStack>
    </RadzenCard>

    <RadzenStack AlignItems="AlignItems.Center">
        <RadzenStack Style="width: 100%; max-width: 600px;">
            <RadzenChart Animate="true">
                <RadzenDonutSeries Data="@revenue" CategoryProperty="Category" ValueProperty="Value" Title="Revenue"
                                   FillMode="@fillMode" InnerRadius="@innerRadius" SegmentGap="@segmentGap" CornerRadius="@cornerRadius"
                                   StartAngle="@(semiCircle ? 180d : 90d)" TotalAngle="@(semiCircle ? 180d : 360d)">
                    <RadzenSeriesDataLabels Visible="@showDataLabels" Position="@labelPosition" />
                </RadzenDonutSeries>
                <RadzenLegend Visible="@showLegend" Position="LegendPosition.Bottom" />
            </RadzenChart>
        </RadzenStack>
    </RadzenStack>
</RadzenStack>

@code {
    FillMode fillMode = FillMode.Gradient;
    double innerRadius = 50;
    double segmentGap = 2;
    double cornerRadius = 4;
    bool semiCircle = false;
    bool showDataLabels = true;
    DataLabelPosition labelPosition = DataLabelPosition.Auto;
    bool showLegend = true;

    class DataItem
    {
        public string Category { get; set; }
        public double Value { get; set; }
    }

    DataItem[] revenue = new DataItem[]
    {
        new DataItem { Category = "Cloud", Value = 4200 },
        new DataItem { Category = "Licenses", Value = 3100 },
        new DataItem { Category = "Services", Value = 2300 },
        new DataItem { Category = "Support", Value = 1500 },
        new DataItem { Category = "Training", Value = 900 },
    };
}
```
