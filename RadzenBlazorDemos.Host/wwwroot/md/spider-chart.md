# Spider Chart

Compare a profile across many dimensions with a Blazor spider (radar) chart.

Keywords: spider, radar, chart, multivariate, radial, web

> API reference: [RadzenSpiderChart API](https://blazor.radzen.com/api/spiderchart.md)

## Examples

## Spider Chart

A spider chart, also called a radar chart, plots several measures on axes radiating from a center, so you can compare a profile across many dimensions at once - skills, product specs, or survey scores.

### Basic Usage

RadzenSpiderChart displays one or more `RadzenSpiderSeries` with `CategoryProperty` and `ValueProperty` bindings.

```razor
<RadzenSpiderChart Style="width: 100%; height: 500px;" SeriesClick="@OnSeriesClick">
    <RadzenSpiderSeries TItem="ProductData" 
                        Data="@dataA"
                        CategoryProperty="@nameof(ProductData.Feature)" 
                        ValueProperty="@nameof(ProductData.Score)"
                        Title="Product A" />
    <RadzenSpiderSeries TItem="ProductData" 
                        Data="@dataB"
                        CategoryProperty="@nameof(ProductData.Feature)" 
                        ValueProperty="@nameof(ProductData.Score)"
                        Title="Product B" />
    <RadzenSpiderSeries TItem="ProductData" 
                        Data="@dataC"
                        CategoryProperty="@nameof(ProductData.Feature)" 
                        ValueProperty="@nameof(ProductData.Score)"
                        Title="Product C" />
</RadzenSpiderChart>

<EventConsole @ref=@console />

@code {
    EventConsole console;

    void OnSeriesClick(SeriesClickEventArgs args)
    {
        console.Log($"SeriesClick: Title={args.Title}, Category={args.Category}, Value={args.Value}");
    }

    IEnumerable<ProductData> dataA;
    IEnumerable<ProductData> dataB;
    IEnumerable<ProductData> dataC;

    protected override void OnInitialized()
    {
        base.OnInitialized();

        dataA = GetProductAData();
        dataB = GetProductBData();
        dataC = GetProductCData();
    }

    class ProductData
    {
        public string Feature { get; set; }
        public double Score { get; set; }
    }

    IEnumerable<ProductData> GetProductAData()
    {
        return new ProductData[]
        {
            new ProductData { Feature = "Performance", Score = 85 },
            new ProductData { Feature = "Reliability", Score = 92 },
            new ProductData { Feature = "Usability", Score = 78 },
            new ProductData { Feature = "Features", Score = 88 },
            new ProductData { Feature = "Support", Score = 75 },
            new ProductData { Feature = "Price", Score = 82 }
        };
    }

    IEnumerable<ProductData> GetProductBData()
    {
        return new ProductData[]
        {
            new ProductData { Feature = "Performance", Score = 78 },
            new ProductData { Feature = "Reliability", Score = 88 },
            new ProductData { Feature = "Usability", Score = 92 },
            new ProductData { Feature = "Features", Score = 75 },
            new ProductData { Feature = "Support", Score = 85 },
            new ProductData { Feature = "Price", Score = 90 }
        };
    }

    IEnumerable<ProductData> GetProductCData()
    {
        return new ProductData[]
        {
            new ProductData { Feature = "Performance", Score = 92 },
            new ProductData { Feature = "Reliability", Score = 85 },
            new ProductData { Feature = "Usability", Score = 88 },
            new ProductData { Feature = "Features", Score = 82 },
            new ProductData { Feature = "Support", Score = 78 },
            new ProductData { Feature = "Price", Score = 75 }
        };
    }
}
```


### Grid Shape

Use the `GridShape` property to switch between a polygon and circular grid.

```razor
<RadzenStack class="rz-p-0 rz-p-md-6 rz-p-lg-12">
    <RadzenCard Variant="Variant.Outlined">
        <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Wrap="FlexWrap.Wrap">
            <RadzenStack Gap="0.25rem">
                <RadzenText TextStyle="TextStyle.Caption">Grid Shape</RadzenText>
                <RadzenSelectBar @bind-Value="gridShape" TValue="SpiderChartGridShape" Size="ButtonSize.Small" Change="@(() => StateHasChanged())">
                    <Items>
                        <RadzenSelectBarItem Text="Polygon" Value="SpiderChartGridShape.Polygon" />
                        <RadzenSelectBarItem Text="Circle" Value="SpiderChartGridShape.Circular" />
                    </Items>
                </RadzenSelectBar>
            </RadzenStack>
        </RadzenStack>
    </RadzenCard>

    <RadzenSpiderChart GridShape="@gridShape" Style="width: 100%; height: 500px;">
    <RadzenSpiderSeries TItem="SkillData"
                        Data="@aliceData"
                        CategoryProperty="@nameof(SkillData.Skill)"
                        ValueProperty="@nameof(SkillData.Score)"
                        Title="Alice" />
    <RadzenSpiderSeries TItem="SkillData"
                        Data="@bobData"
                        CategoryProperty="@nameof(SkillData.Skill)"
                        ValueProperty="@nameof(SkillData.Score)"
                        Title="Bob" />
    </RadzenSpiderChart>
</RadzenStack>

@code {
    SpiderChartGridShape gridShape = SpiderChartGridShape.Polygon;

    class SkillData
    {
        public string Skill { get; set; }
        public double Score { get; set; }
    }

    IEnumerable<SkillData> aliceData = new SkillData[]
    {
        new SkillData { Skill = "Communication", Score = 90 },
        new SkillData { Skill = "Problem Solving", Score = 85 },
        new SkillData { Skill = "Teamwork", Score = 92 },
        new SkillData { Skill = "Leadership", Score = 78 },
        new SkillData { Skill = "Creativity", Score = 88 },
    };

    IEnumerable<SkillData> bobData = new SkillData[]
    {
        new SkillData { Skill = "Communication", Score = 75 },
        new SkillData { Skill = "Problem Solving", Score = 92 },
        new SkillData { Skill = "Teamwork", Score = 80 },
        new SkillData { Skill = "Leadership", Score = 88 },
        new SkillData { Skill = "Creativity", Score = 70 },
    };
}
```


### Color Scheme

Set the `ColorScheme` property to apply a built-in color palette to all series.

```razor
<RadzenStack class="rz-p-0 rz-p-md-6 rz-p-lg-12">
    <RadzenCard Variant="Variant.Outlined">
        <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Wrap="FlexWrap.Wrap">
            <RadzenStack Gap="0.25rem">
                <RadzenText TextStyle="TextStyle.Caption">Color Scheme</RadzenText>
                <RadzenSelectBar @bind-Value="colorScheme" TValue="ColorScheme" Size="ButtonSize.Small" Change="@(() => StateHasChanged())">
                    <Items>
                        <RadzenSelectBarItem Text="Pastel" Value="ColorScheme.Pastel" />
                        <RadzenSelectBarItem Text="Palette" Value="ColorScheme.Palette" />
                        <RadzenSelectBarItem Text="Mono" Value="ColorScheme.Monochrome" />
                        <RadzenSelectBarItem Text="Divergent" Value="ColorScheme.Divergent" />
                    </Items>
                </RadzenSelectBar>
            </RadzenStack>
        </RadzenStack>
    </RadzenCard>

    <RadzenSpiderChart ColorScheme="@colorScheme" Style="width: 100%; height: 500px;">
    <RadzenSpiderSeries TItem="SkillData"
                        Data="@aliceData"
                        CategoryProperty="@nameof(SkillData.Skill)"
                        ValueProperty="@nameof(SkillData.Score)"
                        Title="Alice" />
    <RadzenSpiderSeries TItem="SkillData"
                        Data="@bobData"
                        CategoryProperty="@nameof(SkillData.Skill)"
                        ValueProperty="@nameof(SkillData.Score)"
                        Title="Bob" />
    </RadzenSpiderChart>
</RadzenStack>

@code {
    ColorScheme colorScheme = ColorScheme.Pastel;

    class SkillData
    {
        public string Skill { get; set; }
        public double Score { get; set; }
    }

    IEnumerable<SkillData> aliceData = new SkillData[]
    {
        new SkillData { Skill = "Communication", Score = 90 },
        new SkillData { Skill = "Problem Solving", Score = 85 },
        new SkillData { Skill = "Teamwork", Score = 92 },
        new SkillData { Skill = "Leadership", Score = 78 },
        new SkillData { Skill = "Creativity", Score = 88 },
    };

    IEnumerable<SkillData> bobData = new SkillData[]
    {
        new SkillData { Skill = "Communication", Score = 75 },
        new SkillData { Skill = "Problem Solving", Score = 92 },
        new SkillData { Skill = "Teamwork", Score = 80 },
        new SkillData { Skill = "Leadership", Score = 88 },
        new SkillData { Skill = "Creativity", Score = 70 },
    };
}
```


### Legend

Configure the legend with `RadzenSpiderLegend`. Control its `Position` and `Visible` properties.

```razor
<RadzenStack class="rz-p-0 rz-p-md-6 rz-p-lg-12">
    <RadzenCard Variant="Variant.Outlined">
        <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Wrap="FlexWrap.Wrap">
        <RadzenStack Gap="0.25rem">
            <RadzenText TextStyle="TextStyle.Caption">Legend Position</RadzenText>
            <RadzenSelectBar @bind-Value="legendPosition" TValue="LegendPosition" Size="ButtonSize.Small" Change="@(() => StateHasChanged())">
                <Items>
                    <RadzenSelectBarItem Text="Right" Value="LegendPosition.Right" />
                    <RadzenSelectBarItem Text="Left" Value="LegendPosition.Left" />
                    <RadzenSelectBarItem Text="Top" Value="LegendPosition.Top" />
                    <RadzenSelectBarItem Text="Bottom" Value="LegendPosition.Bottom" />
                </Items>
            </RadzenSelectBar>
        </RadzenStack>
        <RadzenStack Orientation="Orientation.Horizontal" Gap="0.5rem" AlignItems="AlignItems.Center">
            <RadzenSwitch @bind-Value="legendVisible" Name="legendVisible" Change="@(() => StateHasChanged())" />
            <RadzenLabel Text="Show Legend" Component="legendVisible" />
        </RadzenStack>
        </RadzenStack>
    </RadzenCard>

    <RadzenSpiderChart Style="width: 100%; height: 500px;">
    <RadzenSpiderLegend Position="@legendPosition" Visible="@legendVisible" />
    <RadzenSpiderSeries TItem="SkillData"
                        Data="@aliceData"
                        CategoryProperty="@nameof(SkillData.Skill)"
                        ValueProperty="@nameof(SkillData.Score)"
                        Title="Alice" />
    <RadzenSpiderSeries TItem="SkillData"
                        Data="@bobData"
                        CategoryProperty="@nameof(SkillData.Skill)"
                        ValueProperty="@nameof(SkillData.Score)"
                        Title="Bob" />
    </RadzenSpiderChart>
</RadzenStack>

@code {
    LegendPosition legendPosition = LegendPosition.Right;
    bool legendVisible = true;

    class SkillData
    {
        public string Skill { get; set; }
        public double Score { get; set; }
    }

    IEnumerable<SkillData> aliceData = new SkillData[]
    {
        new SkillData { Skill = "Communication", Score = 90 },
        new SkillData { Skill = "Problem Solving", Score = 85 },
        new SkillData { Skill = "Teamwork", Score = 92 },
        new SkillData { Skill = "Leadership", Score = 78 },
        new SkillData { Skill = "Creativity", Score = 88 },
    };

    IEnumerable<SkillData> bobData = new SkillData[]
    {
        new SkillData { Skill = "Communication", Score = 75 },
        new SkillData { Skill = "Problem Solving", Score = 92 },
        new SkillData { Skill = "Teamwork", Score = 80 },
        new SkillData { Skill = "Leadership", Score = 88 },
        new SkillData { Skill = "Creativity", Score = 70 },
    };
}
```


### Value Format

Use the `ValueFormatter` property on each series to customize how values are displayed in tooltips.

```razor
<RadzenStack class="rz-p-0 rz-p-md-6 rz-p-lg-12">
    <RadzenCard Variant="Variant.Outlined">
        <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Wrap="FlexWrap.Wrap">
            <RadzenStack Gap="0.25rem">
                <RadzenText TextStyle="TextStyle.Caption">Value Format</RadzenText>
                <RadzenSelectBar @bind-Value="valueFormat" TValue="string" Size="ButtonSize.Small" Change="@(() => StateHasChanged())">
                    <Items>
                        <RadzenSelectBarItem Text="Percent" Value="@("percent")" />
                        <RadzenSelectBarItem Text="Number" Value="@("number")" />
                        <RadzenSelectBarItem Text="Score" Value="@("score")" />
                        <RadzenSelectBarItem Text="Currency" Value="@("currency")" />
                    </Items>
                </RadzenSelectBar>
            </RadzenStack>
        </RadzenStack>
    </RadzenCard>

    <RadzenSpiderChart Style="width: 100%; height: 500px;">
    <RadzenSpiderSeries TItem="SkillData"
                        Data="@aliceData"
                        CategoryProperty="@nameof(SkillData.Skill)"
                        ValueProperty="@nameof(SkillData.Score)"
                        Title="Alice"
                        ValueFormatter="@GetValueFormatter()" />
    <RadzenSpiderSeries TItem="SkillData"
                        Data="@bobData"
                        CategoryProperty="@nameof(SkillData.Skill)"
                        ValueProperty="@nameof(SkillData.Score)"
                        Title="Bob"
                        ValueFormatter="@GetValueFormatter()" />
    </RadzenSpiderChart>
</RadzenStack>

@code {
    string valueFormat = "percent";

    Func<double, string> GetValueFormatter()
    {
        return valueFormat switch
        {
            "percent" => (value) => $"{value:F0}%",
            "number" => (value) => value.ToString("F1"),
            "score" => (value) => $"{value:F0}/100",
            "currency" => (value) => $"${value:F0}",
            _ => (value) => value.ToString("F1")
        };
    }

    class SkillData
    {
        public string Skill { get; set; }
        public double Score { get; set; }
    }

    IEnumerable<SkillData> aliceData = new SkillData[]
    {
        new SkillData { Skill = "Communication", Score = 90 },
        new SkillData { Skill = "Problem Solving", Score = 85 },
        new SkillData { Skill = "Teamwork", Score = 92 },
        new SkillData { Skill = "Leadership", Score = 78 },
        new SkillData { Skill = "Creativity", Score = 88 },
    };

    IEnumerable<SkillData> bobData = new SkillData[]
    {
        new SkillData { Skill = "Communication", Score = 75 },
        new SkillData { Skill = "Problem Solving", Score = 92 },
        new SkillData { Skill = "Teamwork", Score = 80 },
        new SkillData { Skill = "Leadership", Score = 88 },
        new SkillData { Skill = "Creativity", Score = 70 },
    };
}
```


### Polar Chart

Combine `GridShape="Circular"` with `ShowAxisValues="true"` and many directional categories to create a polar chart.

```razor
<RadzenStack class="rz-p-0 rz-p-md-6 rz-p-lg-12">
    <RadzenCard Variant="Variant.Outlined">
        <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Wrap="FlexWrap.Wrap">
        <RadzenStack Gap="0.25rem">
            <RadzenText TextStyle="TextStyle.Caption">Grid Shape</RadzenText>
            <RadzenSelectBar @bind-Value="gridShape" TValue="SpiderChartGridShape" Size="ButtonSize.Small">
                <Items>
                    <RadzenSelectBarItem Text="Polygon" Value="SpiderChartGridShape.Polygon" />
                    <RadzenSelectBarItem Text="Circle" Value="SpiderChartGridShape.Circular" />
                </Items>
            </RadzenSelectBar>
        </RadzenStack>
        <RadzenStack Gap="0.25rem">
            <RadzenText TextStyle="TextStyle.Caption">Angular Range</RadzenText>
            <RadzenSelectBar @bind-Value="rangePreset" TValue="string" Size="ButtonSize.Small" Change="@OnRangeChanged">
                <Items>
                    <RadzenSelectBarItem Text="Full" Value="@("full")" />
                    <RadzenSelectBarItem Text="Top Half" Value="@("top")" />
                    <RadzenSelectBarItem Text="Right Half" Value="@("right")" />
                    <RadzenSelectBarItem Text="Top-Right" Value="@("topright")" />
                    <RadzenSelectBarItem Text="Custom" Value="@("custom")" />
                </Items>
            </RadzenSelectBar>
        </RadzenStack>
        @if (rangePreset == "custom")
        {
            <RadzenStack Gap="0.25rem">
                <RadzenText TextStyle="TextStyle.Caption">Start Angle: @startAngle°</RadzenText>
                <RadzenSlider @bind-Value="startAngle" TValue="double" Min="-360" Max="360" Step="15" Style="width: 150px;" />
            </RadzenStack>
            <RadzenStack Gap="0.25rem">
                <RadzenText TextStyle="TextStyle.Caption">End Angle: @endAngle°</RadzenText>
                <RadzenSlider @bind-Value="endAngle" TValue="double" Min="-360" Max="360" Step="15" Style="width: 150px;" />
            </RadzenStack>
        }
        <RadzenStack Gap="0.25rem">
            <RadzenText TextStyle="TextStyle.Caption">Show Axis Values</RadzenText>
            <RadzenSwitch @bind-Value="showAxisValues" />
        </RadzenStack>
        </RadzenStack>
    </RadzenCard>

    <RadzenSpiderChart Style="width: 100%; height: 500px;" GridShape="@gridShape" ShowAxisValues="@showAxisValues"
                   StartAngle="@startAngle" EndAngle="@endAngle">
    <RadzenSpiderSeries TItem="PolarData"
                        Data="@windData"
                        CategoryProperty="@nameof(PolarData.Direction)"
                        ValueProperty="@nameof(PolarData.Speed)"
                        Title="Wind Speed"
                        ValueFormatter="@(v => $"{v:F0} km/h")" />
    <RadzenSpiderSeries TItem="PolarData"
                        Data="@gustData"
                        CategoryProperty="@nameof(PolarData.Direction)"
                        ValueProperty="@nameof(PolarData.Speed)"
                        Title="Gust Speed"
                        ValueFormatter="@(v => $"{v:F0} km/h")" />
        <RadzenSpiderLegend Position="LegendPosition.Bottom" />
    </RadzenSpiderChart>
</RadzenStack>

@code {
    SpiderChartGridShape gridShape = SpiderChartGridShape.Circular;
    bool showAxisValues = true;
    string rangePreset = "full";
    double startAngle = 0;
    double endAngle = 360;

    void OnRangeChanged()
    {
        switch (rangePreset)
        {
            case "full":
                startAngle = 0;
                endAngle = 360;
                break;
            case "top":
                startAngle = 270;
                endAngle = 90;
                break;
            case "right":
                startAngle = 0;
                endAngle = 180;
                break;
            case "topright":
                startAngle = 0;
                endAngle = 90;
                break;
        }
    }

    class PolarData
    {
        public string Direction { get; set; }
        public double Speed { get; set; }
    }

    IEnumerable<PolarData> windData = new PolarData[]
    {
        new PolarData { Direction = "N", Speed = 45 },
        new PolarData { Direction = "NNE", Speed = 38 },
        new PolarData { Direction = "NE", Speed = 52 },
        new PolarData { Direction = "ENE", Speed = 61 },
        new PolarData { Direction = "E", Speed = 72 },
        new PolarData { Direction = "ESE", Speed = 65 },
        new PolarData { Direction = "SE", Speed = 48 },
        new PolarData { Direction = "SSE", Speed = 35 },
        new PolarData { Direction = "S", Speed = 28 },
        new PolarData { Direction = "SSW", Speed = 32 },
        new PolarData { Direction = "SW", Speed = 40 },
        new PolarData { Direction = "WSW", Speed = 55 },
        new PolarData { Direction = "W", Speed = 68 },
        new PolarData { Direction = "WNW", Speed = 58 },
        new PolarData { Direction = "NW", Speed = 50 },
        new PolarData { Direction = "NNW", Speed = 42 },
    };

    IEnumerable<PolarData> gustData = new PolarData[]
    {
        new PolarData { Direction = "N", Speed = 62 },
        new PolarData { Direction = "NNE", Speed = 55 },
        new PolarData { Direction = "NE", Speed = 70 },
        new PolarData { Direction = "ENE", Speed = 78 },
        new PolarData { Direction = "E", Speed = 90 },
        new PolarData { Direction = "ESE", Speed = 82 },
        new PolarData { Direction = "SE", Speed = 65 },
        new PolarData { Direction = "SSE", Speed = 50 },
        new PolarData { Direction = "S", Speed = 40 },
        new PolarData { Direction = "SSW", Speed = 48 },
        new PolarData { Direction = "SW", Speed = 58 },
        new PolarData { Direction = "WSW", Speed = 72 },
        new PolarData { Direction = "W", Speed = 85 },
        new PolarData { Direction = "WNW", Speed = 75 },
        new PolarData { Direction = "NW", Speed = 68 },
        new PolarData { Direction = "NNW", Speed = 58 },
    };
}
```


### Markers

Toggle data point markers with the `ShowMarkers` property.

```razor
<RadzenStack class="rz-p-0 rz-p-md-6 rz-p-lg-12">
    <RadzenCard Variant="Variant.Outlined">
        <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Wrap="FlexWrap.Wrap">
            <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="0.5rem">
                <RadzenSwitch @bind-Value="showMarkers" Name="showMarkers" Change="@(() => StateHasChanged())" />
                <RadzenLabel Text="Show Markers" Component="showMarkers" />
            </RadzenStack>
        </RadzenStack>
    </RadzenCard>

    <RadzenSpiderChart ShowMarkers="@showMarkers" Style="width: 100%; height: 500px;">
    <RadzenSpiderSeries TItem="SkillData"
                        Data="@aliceData"
                        CategoryProperty="@nameof(SkillData.Skill)"
                        ValueProperty="@nameof(SkillData.Score)"
                        Title="Alice" />
    <RadzenSpiderSeries TItem="SkillData"
                        Data="@bobData"
                        CategoryProperty="@nameof(SkillData.Skill)"
                        ValueProperty="@nameof(SkillData.Score)"
                        Title="Bob" />
    </RadzenSpiderChart>
</RadzenStack>

@code {
    bool showMarkers = true;

    class SkillData
    {
        public string Skill { get; set; }
        public double Score { get; set; }
    }

    IEnumerable<SkillData> aliceData = new SkillData[]
    {
        new SkillData { Skill = "Communication", Score = 90 },
        new SkillData { Skill = "Problem Solving", Score = 85 },
        new SkillData { Skill = "Teamwork", Score = 92 },
        new SkillData { Skill = "Leadership", Score = 78 },
        new SkillData { Skill = "Creativity", Score = 88 },
    };

    IEnumerable<SkillData> bobData = new SkillData[]
    {
        new SkillData { Skill = "Communication", Score = 75 },
        new SkillData { Skill = "Problem Solving", Score = 92 },
        new SkillData { Skill = "Teamwork", Score = 80 },
        new SkillData { Skill = "Leadership", Score = 88 },
        new SkillData { Skill = "Creativity", Score = 70 },
    };
}
```
