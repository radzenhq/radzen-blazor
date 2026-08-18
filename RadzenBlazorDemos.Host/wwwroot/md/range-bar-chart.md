# Range Bar Chart

Show spans from a start to an end value with a Blazor range bar chart - schedules, date ranges, and low-to-high bands.

Keywords: chart, graph, bar, range, gantt, timeline, min, max

## Examples

## Radzen Blazor Chart range bar series

A range bar chart draws each bar from a start value to an end value rather than from zero - a simple way to show spans like date ranges, schedules, or low-to-high bands laid out horizontally.

```razor
<RadzenStack class="rz-p-0 rz-p-md-6 rz-p-lg-12">
    <RadzenRow>
        <RadzenColumn Size="12">
            <RadzenChart Animate="true">
                <RadzenRangeBarSeries FillMode="FillMode.Gradient" Data="@projectData" CategoryProperty="Task"
                    MinProperty="Start" MaxProperty="End" Title="Project Timeline" />
                <RadzenCategoryAxis />
                <RadzenValueAxis>
                    <RadzenGridLines Visible="true" />
                    <RadzenAxisTitle Text="Week" />
                </RadzenValueAxis>
            </RadzenChart>
        </RadzenColumn>
    </RadzenRow>
</RadzenStack>

@code {
    class DataItem
    {
        public string Task { get; set; }
        public double Start { get; set; }
        public double End { get; set; }
    }

    DataItem[] projectData = new DataItem[]
    {
        new DataItem { Task = "Research", Start = 1, End = 3 },
        new DataItem { Task = "Design", Start = 2, End = 5 },
        new DataItem { Task = "Development", Start = 4, End = 10 },
        new DataItem { Task = "Testing", Start = 8, End = 12 },
        new DataItem { Task = "Deployment", Start = 11, End = 13 },
        new DataItem { Task = "Review", Start = 12, End = 14 },
    };
}
```
