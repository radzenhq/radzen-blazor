# Line Chart

Show a trend over time with a Blazor line chart - the clearest way to follow how a value changes. Free and open source.

Keywords: chart, graph, line

## Examples

## Radzen Blazor Chart with line series

A line chart connects data points with straight segments, making it the clearest way to show a trend over time or any ordered sequence.

### Overview

A full year of weekly revenue across two business lines - real seasonality with a summer slowdown and a Black Friday spike - drawn with smooth lines, a shared tooltip with an axis crosshair and a currency-formatted value axis.

```razor
<RadzenStack class="rz-p-0 rz-p-md-6 rz-p-lg-12">
    <RadzenChart Animate="true" Style="height: 420px;">
        <RadzenChartTooltipOptions Shared="true" />
        <RadzenLineSeries Smooth="true" Data="@subscriptionRevenue" CategoryProperty="Week" ValueProperty="Revenue" Title="Subscriptions" StrokeWidth="3" />
        <RadzenLineSeries Smooth="true" Data="@marketplaceRevenue" CategoryProperty="Week" ValueProperty="Revenue" Title="Marketplace" StrokeWidth="3" />
        <RadzenCategoryAxis Step="@(TimeSpan.FromDays(28))" FormatString="{0:MMM d}">
            <RadzenAxisCrosshair Visible="true" Label="true" />
        </RadzenCategoryAxis>
        <RadzenValueAxis Formatter="@FormatAsUSD">
            <RadzenGridLines Visible="true" LineType="LineType.Dashed" />
            <RadzenAxisTitle Text="Weekly net revenue" />
        </RadzenValueAxis>
        <RadzenLegend Position="LegendPosition.Bottom" />
    </RadzenChart>
</RadzenStack>

@code {
    class DataItem
    {
        public DateTime Week { get; set; }
        public double Revenue { get; set; }
    }

    string FormatAsUSD(object value)
    {
        return ((double)value).ToString("C0", CultureInfo.CreateSpecificCulture("en-US"));
    }

    // First Monday of 2025; one point per week for the full year.
    static readonly DateTime seasonStart = new DateTime(2025, 1, 6);

    static IEnumerable<DataItem> Weekly(double[] values) =>
        values.Select((revenue, week) => new DataItem { Week = seasonStart.AddDays(7 * week), Revenue = revenue });

    // 52 weekly figures: post-holiday dip, spring ramp, summer slowdown,
    // autumn recovery and a Black Friday / Cyber Monday spike before year end.
    IEnumerable<DataItem> subscriptionRevenue = Weekly(new double[]
    {
        182400, 176900, 188300, 195600,                 // Jan
        201200, 208700, 199400, 214300,                 // Feb
        221800, 217500, 229100, 235400, 241900,         // Mar
        238600, 246200, 252800, 248100,                 // Apr
        257300, 263900, 259200, 268400, 274100,         // May
        269700, 261300, 255800, 248900,                 // Jun
        242100, 238600, 246300, 251700,                 // Jul
        244900, 239200, 250600, 258300, 264800,         // Aug
        273400, 281900, 289300, 284600,                 // Sep
        296200, 304700, 312300, 308900,                 // Oct
        318400, 327800, 358200, 372600,                 // Nov - Black Friday surge
        389300, 364700, 351200, 338900, 324600,         // Dec - holiday peak then wind-down
    });

    IEnumerable<DataItem> marketplaceRevenue = Weekly(new double[]
    {
        58300, 52100, 61800, 66400,                     // Jan
        71200, 68900, 74600, 82300,                     // Feb
        79100, 86700, 91200, 84800, 93500,              // Mar
        88200, 96400, 102800, 95300,                    // Apr
        104700, 111200, 98600, 113800, 121400,          // May
        109300, 102700, 96200, 89800,                   // Jun
        84600, 91300, 98700, 87400,                     // Jul
        93200, 101800, 96400, 108300, 114900,           // Aug
        107600, 119200, 126800, 118400,                 // Sep
        131300, 142700, 138200, 149600,                 // Oct
        156800, 168300, 224700, 261400,                 // Nov - Cyber Week spike
        198600, 172300, 158900, 144700, 131200,         // Dec
    });
}
```


### Basic line

Bind a collection and set `CategoryProperty` and `ValueProperty`. Each point is connected with a straight segment in order.

```razor
<RadzenStack class="rz-p-0 rz-p-md-6 rz-p-lg-12">
    <RadzenChart>
        <RadzenLineSeries Data="@revenue" CategoryProperty="Month" ValueProperty="Revenue" Title="Revenue" />
    </RadzenChart>
</RadzenStack>

@code {
    class DataItem
    {
        public string Month { get; set; }
        public double Revenue { get; set; }
    }

    DataItem[] revenue = new DataItem[]
    {
        new DataItem { Month = "Jan", Revenue = 234 },
        new DataItem { Month = "Feb", Revenue = 269 },
        new DataItem { Month = "Mar", Revenue = 233 },
        new DataItem { Month = "Apr", Revenue = 244 },
        new DataItem { Month = "May", Revenue = 214 },
        new DataItem { Month = "Jun", Revenue = 253 },
        new DataItem { Month = "Jul", Revenue = 274 },
        new DataItem { Month = "Aug", Revenue = 284 },
    };
}
```


### Markers

Add `RadzenMarkers` to mark each data point. Set `MarkerType` to `Circle`, `Square`, `Triangle` or `Diamond` and tune the `Size`.

```razor
<RadzenStack class="rz-p-0 rz-p-md-6 rz-p-lg-12">
    <RadzenChart>
        <RadzenLineSeries Data="@plan" CategoryProperty="Month" ValueProperty="Value" Title="Plan">
            <RadzenMarkers MarkerType="MarkerType.Square" Size="8" />
        </RadzenLineSeries>
        <RadzenLineSeries Data="@actual" CategoryProperty="Month" ValueProperty="Value" Title="Actual">
            <RadzenMarkers MarkerType="MarkerType.Circle" Size="8" />
        </RadzenLineSeries>
        <RadzenLineSeries Data="@forecast" CategoryProperty="Month" ValueProperty="Value" Title="Forecast">
            <RadzenMarkers MarkerType="MarkerType.Triangle" Size="8" />
        </RadzenLineSeries>
        <RadzenLegend Position="LegendPosition.Bottom" />
    </RadzenChart>
</RadzenStack>

@code {
    class DataItem
    {
        public string Month { get; set; }
        public double Value { get; set; }
    }

    DataItem[] plan = new DataItem[]
    {
        new DataItem { Month = "Jan", Value = 200 },
        new DataItem { Month = "Feb", Value = 220 },
        new DataItem { Month = "Mar", Value = 240 },
        new DataItem { Month = "Apr", Value = 260 },
        new DataItem { Month = "May", Value = 280 },
    };

    DataItem[] actual = new DataItem[]
    {
        new DataItem { Month = "Jan", Value = 234 },
        new DataItem { Month = "Feb", Value = 269 },
        new DataItem { Month = "Mar", Value = 233 },
        new DataItem { Month = "Apr", Value = 244 },
        new DataItem { Month = "May", Value = 214 },
    };

    DataItem[] forecast = new DataItem[]
    {
        new DataItem { Month = "Jan", Value = 180 },
        new DataItem { Month = "Feb", Value = 210 },
        new DataItem { Month = "Mar", Value = 250 },
        new DataItem { Month = "Apr", Value = 290 },
        new DataItem { Month = "May", Value = 320 },
    };
}
```


### Multiple series

Add more than one `RadzenLineSeries` to compare trends side by side, and place a `RadzenLegend` to tell them apart.

```razor
<RadzenStack class="rz-p-0 rz-p-md-6 rz-p-lg-12">
    <RadzenChart>
        <RadzenLineSeries Data="@northData" CategoryProperty="Month" ValueProperty="Revenue" Title="North">
            <RadzenMarkers MarkerType="MarkerType.Circle" />
        </RadzenLineSeries>
        <RadzenLineSeries Data="@southData" CategoryProperty="Month" ValueProperty="Revenue" Title="South">
            <RadzenMarkers MarkerType="MarkerType.Circle" />
        </RadzenLineSeries>
        <RadzenLineSeries Data="@westData" CategoryProperty="Month" ValueProperty="Revenue" Title="West">
            <RadzenMarkers MarkerType="MarkerType.Circle" />
        </RadzenLineSeries>
        <RadzenLegend Position="LegendPosition.Bottom" />
    </RadzenChart>
</RadzenStack>

@code {
    class DataItem
    {
        public string Month { get; set; }
        public double Revenue { get; set; }
    }

    DataItem[] northData = new DataItem[]
    {
        new DataItem { Month = "Jan", Revenue = 234 },
        new DataItem { Month = "Feb", Revenue = 269 },
        new DataItem { Month = "Mar", Revenue = 233 },
        new DataItem { Month = "Apr", Revenue = 244 },
        new DataItem { Month = "May", Revenue = 214 },
        new DataItem { Month = "Jun", Revenue = 253 },
    };

    DataItem[] southData = new DataItem[]
    {
        new DataItem { Month = "Jan", Revenue = 180 },
        new DataItem { Month = "Feb", Revenue = 205 },
        new DataItem { Month = "Mar", Revenue = 198 },
        new DataItem { Month = "Apr", Revenue = 232 },
        new DataItem { Month = "May", Revenue = 254 },
        new DataItem { Month = "Jun", Revenue = 268 },
    };

    DataItem[] westData = new DataItem[]
    {
        new DataItem { Month = "Jan", Revenue = 145 },
        new DataItem { Month = "Feb", Revenue = 162 },
        new DataItem { Month = "Mar", Revenue = 184 },
        new DataItem { Month = "Apr", Revenue = 176 },
        new DataItem { Month = "May", Revenue = 199 },
        new DataItem { Month = "Jun", Revenue = 221 },
    };
}
```


### Line styles

Set `LineType` to `Solid`, `Dashed` or `Dotted` and adjust `StrokeWidth` to distinguish actual, target and forecast lines.

```razor
<RadzenStack class="rz-p-0 rz-p-md-6 rz-p-lg-12">
    <RadzenChart>
        <RadzenLineSeries Data="@actual" CategoryProperty="Month" ValueProperty="Value" Title="Actual" LineType="LineType.Solid" StrokeWidth="3" />
        <RadzenLineSeries Data="@target" CategoryProperty="Month" ValueProperty="Value" Title="Target" LineType="LineType.Dashed" StrokeWidth="2" />
        <RadzenLineSeries Data="@forecast" CategoryProperty="Month" ValueProperty="Value" Title="Forecast" LineType="LineType.Dotted" StrokeWidth="2" />
        <RadzenLegend Position="LegendPosition.Bottom" />
    </RadzenChart>
</RadzenStack>

@code {
    class DataItem
    {
        public string Month { get; set; }
        public double Value { get; set; }
    }

    DataItem[] actual = new DataItem[]
    {
        new DataItem { Month = "Jan", Value = 234 },
        new DataItem { Month = "Feb", Value = 269 },
        new DataItem { Month = "Mar", Value = 233 },
        new DataItem { Month = "Apr", Value = 244 },
        new DataItem { Month = "May", Value = 214 },
        new DataItem { Month = "Jun", Value = 253 },
    };

    DataItem[] target = new DataItem[]
    {
        new DataItem { Month = "Jan", Value = 240 },
        new DataItem { Month = "Feb", Value = 250 },
        new DataItem { Month = "Mar", Value = 260 },
        new DataItem { Month = "Apr", Value = 270 },
        new DataItem { Month = "May", Value = 280 },
        new DataItem { Month = "Jun", Value = 290 },
    };

    DataItem[] forecast = new DataItem[]
    {
        new DataItem { Month = "Jan", Value = 210 },
        new DataItem { Month = "Feb", Value = 235 },
        new DataItem { Month = "Mar", Value = 248 },
        new DataItem { Month = "Apr", Value = 262 },
        new DataItem { Month = "May", Value = 271 },
        new DataItem { Month = "Jun", Value = 285 },
    };
}
```


### Data labels

Add `RadzenSeriesDataLabels` to print values next to each point. Use a `Formatter` to format them and `Step` to label every Nth point.

```razor
<RadzenStack class="rz-p-0 rz-p-md-6 rz-p-lg-12">
    <RadzenChart>
        <RadzenLineSeries Data="@revenue" CategoryProperty="Month" ValueProperty="Revenue" Title="Revenue">
            <RadzenMarkers MarkerType="MarkerType.Circle" />
            <RadzenSeriesDataLabels Formatter="@FormatAsUSD" />
        </RadzenLineSeries>
        <RadzenValueAxis Formatter="@FormatAsUSD" />
    </RadzenChart>
</RadzenStack>

@code {
    class DataItem
    {
        public string Month { get; set; }
        public double Revenue { get; set; }
    }

    string FormatAsUSD(object value)
    {
        return ((double)value).ToString("C0", CultureInfo.CreateSpecificCulture("en-US"));
    }

    DataItem[] revenue = new DataItem[]
    {
        new DataItem { Month = "Jan", Revenue = 234000 },
        new DataItem { Month = "Feb", Revenue = 269000 },
        new DataItem { Month = "Mar", Revenue = 233000 },
        new DataItem { Month = "Apr", Revenue = 244000 },
        new DataItem { Month = "May", Revenue = 214000 },
        new DataItem { Month = "Jun", Revenue = 253000 },
    };
}
```


### Custom colors

Set the `Stroke` of each series (and the marker `Fill`) to apply your own palette instead of the theme colors.

```razor
<RadzenStack class="rz-p-0 rz-p-md-6 rz-p-lg-12">
    <RadzenChart>
        <RadzenLineSeries Data="@northData" CategoryProperty="Month" ValueProperty="Revenue" Title="North" Stroke="#6366f1" StrokeWidth="3">
            <RadzenMarkers MarkerType="MarkerType.Circle" Fill="#6366f1" />
        </RadzenLineSeries>
        <RadzenLineSeries Data="@southData" CategoryProperty="Month" ValueProperty="Revenue" Title="South" Stroke="#14b8a6" StrokeWidth="3">
            <RadzenMarkers MarkerType="MarkerType.Circle" Fill="#14b8a6" />
        </RadzenLineSeries>
        <RadzenLineSeries Data="@westData" CategoryProperty="Month" ValueProperty="Revenue" Title="West" Stroke="#f59e0b" StrokeWidth="3">
            <RadzenMarkers MarkerType="MarkerType.Circle" Fill="#f59e0b" />
        </RadzenLineSeries>
        <RadzenLegend Position="LegendPosition.Bottom" />
    </RadzenChart>
</RadzenStack>

@code {
    class DataItem
    {
        public string Month { get; set; }
        public double Revenue { get; set; }
    }

    DataItem[] northData = new DataItem[]
    {
        new DataItem { Month = "Jan", Revenue = 234 },
        new DataItem { Month = "Feb", Revenue = 269 },
        new DataItem { Month = "Mar", Revenue = 233 },
        new DataItem { Month = "Apr", Revenue = 244 },
        new DataItem { Month = "May", Revenue = 214 },
        new DataItem { Month = "Jun", Revenue = 253 },
    };

    DataItem[] southData = new DataItem[]
    {
        new DataItem { Month = "Jan", Revenue = 180 },
        new DataItem { Month = "Feb", Revenue = 205 },
        new DataItem { Month = "Mar", Revenue = 198 },
        new DataItem { Month = "Apr", Revenue = 232 },
        new DataItem { Month = "May", Revenue = 254 },
        new DataItem { Month = "Jun", Revenue = 268 },
    };

    DataItem[] westData = new DataItem[]
    {
        new DataItem { Month = "Jan", Revenue = 145 },
        new DataItem { Month = "Feb", Revenue = 162 },
        new DataItem { Month = "Mar", Revenue = 184 },
        new DataItem { Month = "Apr", Revenue = 176 },
        new DataItem { Month = "May", Revenue = 199 },
        new DataItem { Month = "Jun", Revenue = 221 },
    };
}
```


### Tooltip and crosshair

Set `RadzenChartTooltipOptions` `Shared` to show every series at the hovered category, add a `RadzenAxisCrosshair` for an aligned guide and `RadzenGridLines` for easier reading.

```razor
<RadzenStack class="rz-p-0 rz-p-md-6 rz-p-lg-12">
    <RadzenChart>
        <RadzenChartTooltipOptions Shared="true" />
        <RadzenLineSeries Data="@revenue2024" CategoryProperty="Date" ValueProperty="Revenue" Title="2024">
            <RadzenMarkers MarkerType="MarkerType.Circle" />
        </RadzenLineSeries>
        <RadzenLineSeries Data="@revenue2023" CategoryProperty="Date" ValueProperty="Revenue" Title="2023">
            <RadzenMarkers MarkerType="MarkerType.Circle" />
        </RadzenLineSeries>
        <RadzenCategoryAxis>
            <RadzenAxisCrosshair Visible="true" Snap="true" Label="true" />
        </RadzenCategoryAxis>
        <RadzenValueAxis Formatter="@FormatAsUSD">
            <RadzenGridLines Visible="true" LineType="LineType.Dashed" />
        </RadzenValueAxis>
        <RadzenLegend Position="LegendPosition.Bottom" />
    </RadzenChart>
</RadzenStack>

@code {
    class DataItem
    {
        public string Date { get; set; }
        public double Revenue { get; set; }
    }

    string FormatAsUSD(object value)
    {
        return ((double)value).ToString("C0", CultureInfo.CreateSpecificCulture("en-US"));
    }

    DataItem[] revenue2023 = new DataItem[] {
        new DataItem { Date = "Jan", Revenue = 234000 },
        new DataItem { Date = "Feb", Revenue = 269000 },
        new DataItem { Date = "Mar", Revenue = 233000 },
        new DataItem { Date = "Apr", Revenue = 244000 },
        new DataItem { Date = "May", Revenue = 214000 },
        new DataItem { Date = "Jun", Revenue = 253000 },
        new DataItem { Date = "Jul", Revenue = 274000 },
        new DataItem { Date = "Aug", Revenue = 284000 },
    };

    DataItem[] revenue2024 = new DataItem[] {
        new DataItem { Date = "Jan", Revenue = 334000 },
        new DataItem { Date = "Feb", Revenue = 369000 },
        new DataItem { Date = "Mar", Revenue = 333000 },
        new DataItem { Date = "Apr", Revenue = 344000 },
        new DataItem { Date = "May", Revenue = 314000 },
        new DataItem { Date = "Jun", Revenue = 353000 },
        new DataItem { Date = "Jul", Revenue = 374000 },
        new DataItem { Date = "Aug", Revenue = 384000 },
    };
}
```


### Interactive configuration

Combine the settings above and see how they interact - smooth lines, markers, data labels, shared tooltip and tick placement.

```razor
<RadzenStack class="rz-p-0 rz-p-md-6 rz-p-lg-12">
    <RadzenCard Variant="Variant.Outlined">
        <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Wrap="FlexWrap.Wrap">
            <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="0.5rem">
                <RadzenCheckBox @bind-Value="@smooth" Name="smooth"></RadzenCheckBox>
                <RadzenLabel Text="Smooth" Component="smooth" />
            </RadzenStack>
            <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="0.5rem">
                <RadzenCheckBox @bind-Value="@showDataLabels" Name="dataLabels"></RadzenCheckBox>
                <RadzenLabel Text="Show Data Labels" Component="dataLabels" />
            </RadzenStack>
            @if (showDataLabels)
            {
                <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="0.5rem">
                    <RadzenLabel Text="Label Step" Component="labelStep" />
                    <RadzenNumeric @bind-Value="@labelStep" Min="1" Max="4" Style="width: 70px" Name="labelStep" />
                </RadzenStack>
            }
            <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="0.5rem">
                <RadzenCheckBox @bind-Value="@showMarkers" Name="markers"></RadzenCheckBox>
                <RadzenLabel Text="Show Markers" Component="markers" />
            </RadzenStack>
            <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="0.5rem">
                <RadzenCheckBox @bind-Value="@sharedTooltip" Name="sharedToltip"></RadzenCheckBox>
                <RadzenLabel Text="Shared Tooltip" Component="sharedTooltip" />
            </RadzenStack>
            <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="0.5rem">
                <RadzenLabel Text="Tick Placement" Component="tickPlacement" />
                <RadzenSelectBar @bind-Value="@tickPlacement" TValue="TickPlacement" Size="ButtonSize.Small" Name="tickPlacement">
                    <Items>
                        <RadzenSelectBarItem Value="TickPlacement.Between" Text="Between" />
                        <RadzenSelectBarItem Value="TickPlacement.On" Text="On" />
                    </Items>
                </RadzenSelectBar>
            </RadzenStack>
        </RadzenStack>
    </RadzenCard>

   <RadzenChart Animate="true">
        <RadzenChartTooltipOptions Shared="@sharedTooltip" />
        <RadzenLineSeries Smooth="@smooth" Data="@revenue2023" CategoryProperty="Date" Title="2023" LineType="LineType.Dashed" ValueProperty="Revenue">
            <RadzenMarkers Visible="@showMarkers" MarkerType="MarkerType.Square" />
            <RadzenSeriesDataLabels Visible="@showDataLabels" Step="@labelStep" />
        </RadzenLineSeries>
        <RadzenLineSeries Smooth="@smooth" Data="@revenue2024" CategoryProperty="Date" Title="2024" ValueProperty="Revenue">
            <RadzenMarkers Visible="@showMarkers" MarkerType="MarkerType.Circle" />
            <RadzenSeriesDataLabels Visible="@showDataLabels" Step="@labelStep" />
        </RadzenLineSeries>
        <RadzenCategoryAxis TickPlacement="@tickPlacement">
            <RadzenAxisCrosshair Visible="true" />
        </RadzenCategoryAxis>
        <RadzenValueAxis Formatter="@FormatAsUSD">
            <RadzenGridLines Visible="true" LineType="LineType.Dashed" />
            <RadzenAxisTitle Text="Revenue in USD" />
        </RadzenValueAxis>
    </RadzenChart>
</RadzenStack>

@code {
    bool smooth = false;
    bool sharedTooltip = true;
    bool showDataLabels = false;
    int labelStep = 1;
    bool showMarkers = true;
    TickPlacement tickPlacement = TickPlacement.Between;

    class DataItem
    {
        public string Date { get; set; }
        public double Revenue { get; set; }
    }

    string FormatAsUSD(object value)
    {
        return ((double)value).ToString("C0", CultureInfo.CreateSpecificCulture("en-US"));
    }

    DataItem[] revenue2023 = new DataItem[] {
        new DataItem
        {
            Date = "Jan",
            Revenue = 234000
        },
        new DataItem
        {
            Date = "Feb",
            Revenue = 269000
        },
        new DataItem
        {
            Date = "Mar",
            Revenue = 233000
        },
        new DataItem
        {
            Date = "Apr",
            Revenue = 244000
        },
        new DataItem
        {
            Date = "May",
            Revenue = 214000
        },
        new DataItem
        {
            Date = "Jun",
            Revenue = 253000
        },
        new DataItem
        {
            Date = "Jul",
            Revenue = 274000
        },
        new DataItem
        {
            Date = "Aug",
            Revenue = 284000
        },
        new DataItem
        {
            Date = "Sept",
            Revenue = 273000
        },
        new DataItem
        {
            Date = "Oct",
            Revenue = 282000
        },
        new DataItem
        {
            Date = "Nov",
            Revenue = 289000
        },
        new DataItem
        {
            Date = "Dec",
            Revenue = 294000
        }
    };

    DataItem[] revenue2024 = new DataItem[] {
        new DataItem
        {
            Date = "Jan",
            Revenue = 334000
        },
        new DataItem
        {
            Date = "Feb",
            Revenue = 369000
        },
        new DataItem
        {
            Date = "Mar",
            Revenue = 333000
        },
        new DataItem
        {
            Date = "Apr",
            Revenue = 344000
        },
        new DataItem
        {
            Date = "May",
            Revenue = 314000
        },
        new DataItem
        {
            Date = "Jun",
            Revenue = 353000
        },
        new DataItem
        {
            Date = "Jul",
            Revenue = 374000
        },
        new DataItem
        {
            Date = "Aug",
            Revenue = 384000
        },
        new DataItem
        {
            Date = "Sept",
            Revenue = 373000
        },
        new DataItem
        {
            Date = "Oct",
            Revenue = 382000
        },
        new DataItem
        {
            Date = "Nov",
            Revenue = 389000
        },
        new DataItem
        {
            Date = "Dec",
            Revenue = 394000
        }
    };
}
```
