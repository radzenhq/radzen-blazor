# Full Stacked Line Chart

Trace each series' share of 100% over time with a Blazor full stacked line chart.

Keywords: chart, graph, line, stack, percent, 100, proportional, full

## Examples

## Radzen Blazor Chart with 100% stacked line series

A full stacked line chart normalizes every point to 100%, so the lines trace how each series' share of the total shifts over time rather than its absolute value.

```razor
<RadzenStack class="rz-p-0 rz-p-md-6 rz-p-lg-12">
    <RadzenRow>
        <RadzenColumn Size="12">
            <RadzenChart Animate="true">
                <RadzenChartTooltipOptions Shared="true" />
                <RadzenFullStackedLineSeries Data="@desktopShare" CategoryProperty="Month" Title="Desktop" ValueProperty="Share" />
                <RadzenFullStackedLineSeries Data="@mobileShare" CategoryProperty="Month" Title="Mobile" ValueProperty="Share" />
                <RadzenFullStackedLineSeries Data="@tabletShare" CategoryProperty="Month" Title="Tablet" ValueProperty="Share" LineType="LineType.Dashed" />
                <RadzenCategoryAxis Padding="20">
                    <RadzenAxisCrosshair Visible="true" />
                </RadzenCategoryAxis>
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
        public string Month { get; set; }
        public double Share { get; set; }
    }

    DataItem[] desktopShare = new DataItem[] {
        new DataItem { Month = "Jan", Share = 60 },
        new DataItem { Month = "Feb", Share = 58 },
        new DataItem { Month = "Mar", Share = 55 },
        new DataItem { Month = "Apr", Share = 52 },
        new DataItem { Month = "May", Share = 50 },
        new DataItem { Month = "Jun", Share = 47 },
    };

    DataItem[] mobileShare = new DataItem[] {
        new DataItem { Month = "Jan", Share = 30 },
        new DataItem { Month = "Feb", Share = 32 },
        new DataItem { Month = "Mar", Share = 35 },
        new DataItem { Month = "Apr", Share = 38 },
        new DataItem { Month = "May", Share = 40 },
        new DataItem { Month = "Jun", Share = 43 },
    };

    DataItem[] tabletShare = new DataItem[] {
        new DataItem { Month = "Jan", Share = 10 },
        new DataItem { Month = "Feb", Share = 10 },
        new DataItem { Month = "Mar", Share = 10 },
        new DataItem { Month = "Apr", Share = 10 },
        new DataItem { Month = "May", Share = 10 },
        new DataItem { Month = "Jun", Share = 10 },
    };
}
```
