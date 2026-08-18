# Radar Column Chart

Arrange columns around a circle for cyclical categories with a Blazor radar column chart.

Keywords: spider, radar, column, chart, radial, bar

## Examples

## Radar Column Chart

A radar column chart arranges columns around a circle instead of along a straight axis, which gives cyclical categories - months, hours, compass directions - a natural round layout.

```razor
<RadzenStack class="rz-p-0 rz-p-md-6 rz-p-lg-12">
    <RadzenRow>
        <RadzenColumn Size="12">
            <RadzenSpiderChart Style="width: 100%; height: 500px" GridShape="SpiderChartGridShape.Circular" ShowAxisValues="true">
                <RadzenSpiderColumnSeries Data="@data" CategoryProperty="Nutrient" ValueProperty="Amount" Title="Daily Intake" TItem="NutrientData" />
                <RadzenSpiderLegend Position="LegendPosition.Bottom" />
            </RadzenSpiderChart>
        </RadzenColumn>
    </RadzenRow>
</RadzenStack>

@code {
    class NutrientData
    {
        public string Nutrient { get; set; }
        public double Amount { get; set; }
    }

    NutrientData[] data = new[]
    {
        new NutrientData { Nutrient = "Protein", Amount = 65 },
        new NutrientData { Nutrient = "Fiber", Amount = 40 },
        new NutrientData { Nutrient = "Vitamin A", Amount = 85 },
        new NutrientData { Nutrient = "Vitamin C", Amount = 72 },
        new NutrientData { Nutrient = "Calcium", Amount = 55 },
        new NutrientData { Nutrient = "Iron", Amount = 48 },
        new NutrientData { Nutrient = "Potassium", Amount = 62 },
        new NutrientData { Nutrient = "Magnesium", Amount = 45 },
    };
}
```
