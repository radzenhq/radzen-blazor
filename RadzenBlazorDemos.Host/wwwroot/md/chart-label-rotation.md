# Label Rotation

Rotate crowded category labels on a Blazor chart, automatically or to an angle you set, to keep them readable.

Keywords: chart, label, rotate, rotation

## Examples

## Radzen Blazor Chart label rotation

When category labels are long or crowded, rotating them keeps the axis readable. The chart can rotate labels automatically or to an angle you set.

### Auto Rotation

The Radzen Blazor Chart component can automatically rotate the labels of the category axis when they overlap. To enable that set the LabelAutoRotation property: `LabelAutoRotation="-45"`.
Try changing the angle and the width of the chart to see the effect.

```razor
<RadzenStack class="rz-p-0 rz-p-md-6 rz-p-lg-12">
    <RadzenCard Variant="Variant.Outlined">
        <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" JustifyContent="JustifyContent.Center" Gap="2rem" Wrap="FlexWrap.Wrap">
            <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="1.5rem">
                <RadzenLabel Text="Rotation" Component="rotation" Style="width: 60px" class="rz-text-align-end" />
                <RadzenSlider @bind-Value="@rotation" Name="rotation" Min="-90" Max="90" />
                <RadzenText Style="width: 60px">@(rotation)<sup>&deg;</sup></RadzenText>
            </RadzenStack>
            <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="1.5rem">
                <RadzenLabel Text="Width" Component="width" Style="width: 60px" class="rz-text-align-end" />
                <RadzenSlider @bind-Value="@width" Name="width" Min="600" Max="1200" />
                <RadzenText Style="width: 60px">@(width)px</RadzenText>
            </RadzenStack>
        </RadzenStack>
    </RadzenCard>

    <RadzenStack Orientation="Orientation.Horizontal" JustifyContent="JustifyContent.Center">
        <RadzenChart style="@($"width: {width}px")">
            <RadzenAreaSeries FillMode="FillMode.Gradient" Smooth="true" Data="@revenue2024" CategoryProperty="Date" Title="2024" ValueProperty="Revenue">
                <RadzenSeriesDataLabels Visible="@showDataLabels" />
            </RadzenAreaSeries>
            <RadzenCategoryAxis Padding="20" LabelAutoRotation="@rotation" />
            <RadzenValueAxis Formatter="@FormatAsUSD">
                <RadzenGridLines Visible="true" />
                <RadzenAxisTitle Text="Revenue in USD" />
            </RadzenValueAxis>
        </RadzenChart>
    </RadzenStack>
</RadzenStack>

@code {
    double rotation = -45;
    bool showDataLabels = false;
    double width = 600;

    class DataItem
    {
        public string Date { get; set; }
        public double Revenue { get; set; }
    }

    string FormatAsUSD(object value)
    {
        return ((double)value).ToString("C0", CultureInfo.CreateSpecificCulture("en-US"));
    }

    DataItem[] revenue2024 = new DataItem[] {
        new DataItem { Date = "January", Revenue = 234000 },
        new DataItem { Date = "February", Revenue = 269000 },
        new DataItem { Date = "March", Revenue = 233000 },
        new DataItem { Date = "April", Revenue = 244000 },
        new DataItem { Date = "May", Revenue = 214000 },
        new DataItem { Date = "June", Revenue = 253000 },
        new DataItem { Date = "July", Revenue = 274000 },
        new DataItem { Date = "August", Revenue = 284000 },
        new DataItem { Date = "September", Revenue = 273000 },
        new DataItem { Date = "October", Revenue = 282000 },
        new DataItem { Date = "November", Revenue = 289000 },
        new DataItem { Date = "December", Revenue = 294000 }
    };
}
```


### Predefined Rotation

To always rotate the labels of the category axis set the LabelRotation property: `LabelRotation="-45"`.

```razor
<RadzenStack class="rz-p-0 rz-p-md-6 rz-p-lg-12">
    <RadzenCard Variant="Variant.Outlined">
        <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" JustifyContent="JustifyContent.Center" Gap="1.5rem" Wrap="FlexWrap.Wrap">
            <RadzenLabel Text="Rotation" Component="rotation" />
            <RadzenSlider @bind-Value="@rotation" Name="rotation" Min="-90" Max="90" />
            <RadzenText>@(rotation)<sup>&deg;</sup></RadzenText>
        </RadzenStack>
    </RadzenCard>

    <RadzenChart>
        <RadzenAreaSeries FillMode="FillMode.Gradient" Smooth="true" Data="@revenue2024" CategoryProperty="Date" Title="2024" ValueProperty="Revenue">
            <RadzenSeriesDataLabels Visible="@showDataLabels" />
        </RadzenAreaSeries>
        <RadzenCategoryAxis Padding="20" LabelRotation="@rotation" />
        <RadzenValueAxis Formatter="@FormatAsUSD">
            <RadzenGridLines Visible="true" />
            <RadzenAxisTitle Text="Revenue in USD" />
        </RadzenValueAxis>
    </RadzenChart>
</RadzenStack>

@code {
    double rotation = -45;
    bool showDataLabels = false;

    class DataItem
    {
        public string Date { get; set; }
        public double Revenue { get; set; }
    }

    string FormatAsUSD(object value)
    {
        return ((double)value).ToString("C0", CultureInfo.CreateSpecificCulture("en-US"));
    }

    DataItem[] revenue2024 = new DataItem[] {
        new DataItem { Date = "January", Revenue = 234000 },
        new DataItem { Date = "February", Revenue = 269000 },
        new DataItem { Date = "March", Revenue = 233000 },
        new DataItem { Date = "April", Revenue = 244000 },
        new DataItem { Date = "May", Revenue = 214000 },
        new DataItem { Date = "June", Revenue = 253000 },
        new DataItem { Date = "July", Revenue = 274000 },
        new DataItem { Date = "August", Revenue = 284000 },
        new DataItem { Date = "September", Revenue = 273000 },
        new DataItem { Date = "October", Revenue = 282000 },
        new DataItem { Date = "November", Revenue = 289000 },
        new DataItem { Date = "December", Revenue = 294000 }
    };
}
```
