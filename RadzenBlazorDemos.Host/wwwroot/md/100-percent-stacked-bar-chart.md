# Full Stacked Bar Chart

Compare composition across categories with a Blazor full stacked bar chart, each bar scaled to 100%.

Keywords: chart, graph, bar, stack, percent, 100, proportional, horizontal, full

## Examples

## Radzen Blazor Chart with 100% stacked bar series

A full stacked bar chart stretches every bar to the same length and shows each segment as a share of 100%, so you compare composition across categories rather than totals.

```razor
<RadzenStack class="rz-p-0 rz-p-md-6 rz-p-lg-12">
    <RadzenRow>
        <RadzenColumn Size="12">
            <RadzenChart Animate="true" style="height: 400px">
                <RadzenFullStackedBarSeries FillMode="FillMode.Gradient" Data="@desktopShare" CategoryProperty="Quarter" Title="Desktop" ValueProperty="Share" />
                <RadzenFullStackedBarSeries FillMode="FillMode.Gradient" Data="@mobileShare" CategoryProperty="Quarter" Title="Mobile" ValueProperty="Share" />
                <RadzenFullStackedBarSeries FillMode="FillMode.Gradient" Data="@tabletShare" CategoryProperty="Quarter" Title="Tablet" ValueProperty="Share" />
                <RadzenBarOptions Radius="5" />
                <RadzenValueAxis FormatString="{0}%" Min="0" Max="100" Step="25">
                    <RadzenGridLines Visible="true" />
                </RadzenValueAxis>
            </RadzenChart>
        </RadzenColumn>
    </RadzenRow>
</RadzenStack>

@code {
    class DataItem
    {
        public string Quarter { get; set; }
        public double Share { get; set; }
    }

    DataItem[] desktopShare = new DataItem[] {
        new DataItem { Quarter = "Q1", Share = 58 },
        new DataItem { Quarter = "Q2", Share = 55 },
        new DataItem { Quarter = "Q3", Share = 50 },
        new DataItem { Quarter = "Q4", Share = 47 },
    };

    DataItem[] mobileShare = new DataItem[] {
        new DataItem { Quarter = "Q1", Share = 32 },
        new DataItem { Quarter = "Q2", Share = 35 },
        new DataItem { Quarter = "Q3", Share = 40 },
        new DataItem { Quarter = "Q4", Share = 43 },
    };

    DataItem[] tabletShare = new DataItem[] {
        new DataItem { Quarter = "Q1", Share = 10 },
        new DataItem { Quarter = "Q2", Share = 10 },
        new DataItem { Quarter = "Q3", Share = 10 },
        new DataItem { Quarter = "Q4", Share = 10 },
    };
}
```
