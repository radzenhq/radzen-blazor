# Bullet Chart

Show a KPI against its target in compact space with a Blazor bullet chart.

Keywords: chart, graph, bullet, gauge, target, kpi, performance

## Examples

## Radzen Blazor Chart bullet series

A bullet chart packs a measure, a target, and qualitative ranges into a compact bar - a space-saving way to show whether a KPI is on track.

```razor
<RadzenStack class="rz-p-0 rz-p-md-6 rz-p-lg-12">
    <RadzenRow>
        <RadzenColumn Size="12">
            <RadzenChart Animate="true" Style="height: 400px">
                <RadzenBulletSeries FillMode="FillMode.Gradient" Data="@data" CategoryProperty="Label"
                    ValueProperty="Value" TargetProperty="Target" MaxProperty="Max"
                    Title="Performance" TargetWidth="3"
                    RangeThresholds="@(new[] { 0.5, 0.8, 1.0 })" />
                <RadzenCategoryAxis />
                <RadzenValueAxis>
                    <RadzenGridLines Visible="true" />
                </RadzenValueAxis>
            </RadzenChart>
        </RadzenColumn>
    </RadzenRow>
</RadzenStack>

@code {
    class BulletDataItem
    {
        public string Label { get; set; } = "";
        public double Value { get; set; }
        public double Target { get; set; }
        public double Max { get; set; }
    }

    BulletDataItem[] data = new BulletDataItem[]
    {
        new BulletDataItem { Label = "Revenue", Value = 275, Target = 250, Max = 300 },
        new BulletDataItem { Label = "Profit", Value = 22, Target = 26, Max = 30 },
        new BulletDataItem { Label = "Orders", Value = 150, Target = 170, Max = 200 },
        new BulletDataItem { Label = "Satisfaction", Value = 78, Target = 85, Max = 100 },
        new BulletDataItem { Label = "Market Share", Value = 55, Target = 60, Max = 100 },
    };
}
```
