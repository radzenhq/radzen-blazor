# Sankey Diagram

Trace flow between stages as proportional bands with a Blazor Sankey diagram.

Keywords: sankey, flow, diagram, visualization, relationships

> API reference: [RadzenSankeyDiagram API](https://blazor.radzen.com/api/sankeydiagram.md)

## Examples

## Radzen Blazor Sankey Diagram

A Sankey diagram shows flow between stages as bands whose width is proportional to the amount - ideal for tracing where quantities move and split, like budgets, energy, or user journeys.

### Basic Usage

RadzenSankeyDiagram requires `Data`, `SourceProperty`, `TargetProperty`, and `ValueProperty` to display flow data.

```razor
<div class="rz-p-0 rz-p-md-12">
    <RadzenSankeyDiagram TItem="FlowData"
                       Data="@flows" 
                       SourceProperty="Source"
                       TargetProperty="Target"
                       ValueProperty="Value">
    </RadzenSankeyDiagram>
</div>

@code {
    public class FlowData
    {
        public string Source { get; set; }
        public string Target { get; set; }
        public double Value { get; set; }
    }
    
    private List<FlowData> flows = new List<FlowData>
    {
        new FlowData { Source = "A", Target = "C", Value = 10 },
        new FlowData { Source = "A", Target = "D", Value = 15 },
        new FlowData { Source = "B", Target = "C", Value = 20 },
        new FlowData { Source = "B", Target = "D", Value = 25 },
        new FlowData { Source = "C", Target = "E", Value = 15 },
        new FlowData { Source = "C", Target = "F", Value = 10 },
        new FlowData { Source = "C", Target = "G", Value = 5 },
        new FlowData { Source = "D", Target = "E", Value = 20 },
        new FlowData { Source = "D", Target = "F", Value = 15 },
        new FlowData { Source = "D", Target = "G", Value = 5 }
    };
}
```


### Color Scheme

Use the `ColorScheme` property to choose from built-in color palettes that automatically style nodes and links.

```razor
<div class="rz-p-0 rz-p-md-12">
    <RadzenCard Variant="Variant.Outlined" class="rz-mb-12">
        <RadzenStack Orientation="Orientation.Vertical" Gap="0.25rem">
            <RadzenLabel Text="Color scheme:" Component="ColorScheme" />
            <RadzenDropDown @bind-Value="@colorScheme" Data="@colorSchemes" Name="ColorScheme">
                <Template Context="scheme">
                    @Enum.GetName(typeof(ColorScheme), scheme)
                </Template>
            </RadzenDropDown>
        </RadzenStack>
    </RadzenCard>
    <RadzenSankeyDiagram TItem="FlowData"
                         Data="@flows"
                         SourceProperty="Source"
                         TargetProperty="Target"
                         ValueProperty="Value"
                         SourceLabelProperty="SourceLabel"
                         TargetLabelProperty="TargetLabel"
                         ColorScheme="@colorScheme">
    </RadzenSankeyDiagram>
</div>

@code {
    public class FlowData
    {
        public string Source { get; set; }
        public string Target { get; set; }
        public double Value { get; set; }
        public string SourceLabel { get; set; }
        public string TargetLabel { get; set; }
    }

    private ColorScheme colorScheme = ColorScheme.Pastel;
    private IEnumerable<ColorScheme> colorSchemes = Enum.GetValues(typeof(ColorScheme)).Cast<ColorScheme>();

    private List<FlowData> flows = new List<FlowData>
    {
        new FlowData { Source = "products", SourceLabel = "Product Sales", Target = "marketing", TargetLabel = "Marketing", Value = 150 },
        new FlowData { Source = "products", SourceLabel = "Product Sales", Target = "engineering", TargetLabel = "Engineering", Value = 280 },
        new FlowData { Source = "products", SourceLabel = "Product Sales", Target = "operations", TargetLabel = "Operations", Value = 70 },
        new FlowData { Source = "services", SourceLabel = "Consulting Services", Target = "marketing", TargetLabel = "Marketing", Value = 100 },
        new FlowData { Source = "services", SourceLabel = "Consulting Services", Target = "engineering", TargetLabel = "Engineering", Value = 160 },
        new FlowData { Source = "licensing", SourceLabel = "Licensing Fees", Target = "engineering", TargetLabel = "Engineering", Value = 80 },
        new FlowData { Source = "licensing", SourceLabel = "Licensing Fees", Target = "operations", TargetLabel = "Operations", Value = 50 },
        new FlowData { Source = "licensing", SourceLabel = "Licensing Fees", Target = "marketing", TargetLabel = "Marketing", Value = 30 },
    };
}
```


### Node Properties

Control node layout with `NodeAlignment` (Justify, Left, Right, Center), `NodeWidth`, and `NodePadding`.

```razor
<div class="rz-p-0 rz-p-md-12">
    <RadzenCard Variant="Variant.Outlined" class="rz-mb-12">
        <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Start" Wrap="FlexWrap.Wrap" Gap="1rem">
            <RadzenStack Orientation="Orientation.Vertical" Gap="0.25rem">
                <RadzenLabel Text="Node alignment:" Component="NodeAlignment" />
                <RadzenDropDown @bind-Value="@nodeAlignment" Data="@nodeAlignments" Name="NodeAlignment">
                    <Template Context="alignment">
                        @Enum.GetName(typeof(SankeyAlignment), alignment)
                    </Template>
                </RadzenDropDown>
            </RadzenStack>
            <RadzenStack Orientation="Orientation.Vertical" Gap="0.25rem">
                <RadzenLabel Text="Node width:" Component="NodeWidth" />
                <RadzenNumeric @bind-Value="@nodeWidth" Min="10" Max="50" Name="NodeWidth" />
            </RadzenStack>
            <RadzenStack Orientation="Orientation.Vertical" Gap="0.25rem">
                <RadzenLabel Text="Node padding:" Component="NodePadding" />
                <RadzenNumeric @bind-Value="@nodePadding" Min="2" Max="20" Name="NodePadding" />
            </RadzenStack>
        </RadzenStack>
    </RadzenCard>
    <RadzenSankeyDiagram TItem="FlowData"
                         Data="@flows"
                         SourceProperty="Source"
                         TargetProperty="Target"
                         ValueProperty="Value"
                         SourceLabelProperty="SourceLabel"
                         TargetLabelProperty="TargetLabel"
                         NodeAlignment="@nodeAlignment"
                         NodeWidth="@nodeWidth"
                         NodePadding="@nodePadding">
    </RadzenSankeyDiagram>
</div>

@code {
    public class FlowData
    {
        public string Source { get; set; }
        public string Target { get; set; }
        public double Value { get; set; }
        public string SourceLabel { get; set; }
        public string TargetLabel { get; set; }
    }

    private SankeyAlignment nodeAlignment = SankeyAlignment.Justify;
    private double nodeWidth = 24;
    private double nodePadding = 8;
    private IEnumerable<SankeyAlignment> nodeAlignments = Enum.GetValues(typeof(SankeyAlignment)).Cast<SankeyAlignment>();

    private List<FlowData> flows = new List<FlowData>
    {
        new FlowData { Source = "products", SourceLabel = "Product Sales", Target = "marketing", TargetLabel = "Marketing", Value = 150 },
        new FlowData { Source = "products", SourceLabel = "Product Sales", Target = "engineering", TargetLabel = "Engineering", Value = 280 },
        new FlowData { Source = "products", SourceLabel = "Product Sales", Target = "operations", TargetLabel = "Operations", Value = 70 },
        new FlowData { Source = "services", SourceLabel = "Consulting Services", Target = "marketing", TargetLabel = "Marketing", Value = 100 },
        new FlowData { Source = "services", SourceLabel = "Consulting Services", Target = "engineering", TargetLabel = "Engineering", Value = 160 },
        new FlowData { Source = "licensing", SourceLabel = "Licensing Fees", Target = "engineering", TargetLabel = "Engineering", Value = 80 },
        new FlowData { Source = "licensing", SourceLabel = "Licensing Fees", Target = "operations", TargetLabel = "Operations", Value = 50 },
        new FlowData { Source = "licensing", SourceLabel = "Licensing Fees", Target = "marketing", TargetLabel = "Marketing", Value = 30 },
    };
}
```


### Custom Colors

Define custom colors for individual nodes and links using the `NodeFills` and `LinkFills` properties.

```razor
<div class="rz-p-0 rz-p-md-12">
    <RadzenSankeyDiagram TItem="FlowData"
                         Data="@flows"
                         SourceProperty="Source"
                         TargetProperty="Target"
                         ValueProperty="Value"
                         SourceLabelProperty="SourceLabel"
                         TargetLabelProperty="TargetLabel"
                         NodeFills="@nodeColors"
                         LinkFills="@linkColors">
    </RadzenSankeyDiagram>
</div>

@code {
    public class FlowData
    {
        public string Source { get; set; }
        public string Target { get; set; }
        public double Value { get; set; }
        public string SourceLabel { get; set; }
        public string TargetLabel { get; set; }
    }

    private List<FlowData> flows = new List<FlowData>
    {
        new FlowData { Source = "products", SourceLabel = "Product Sales", Target = "marketing", TargetLabel = "Marketing", Value = 150 },
        new FlowData { Source = "products", SourceLabel = "Product Sales", Target = "engineering", TargetLabel = "Engineering", Value = 280 },
        new FlowData { Source = "products", SourceLabel = "Product Sales", Target = "operations", TargetLabel = "Operations", Value = 70 },
        new FlowData { Source = "services", SourceLabel = "Consulting Services", Target = "marketing", TargetLabel = "Marketing", Value = 100 },
        new FlowData { Source = "services", SourceLabel = "Consulting Services", Target = "engineering", TargetLabel = "Engineering", Value = 160 },
        new FlowData { Source = "licensing", SourceLabel = "Licensing Fees", Target = "engineering", TargetLabel = "Engineering", Value = 80 },
        new FlowData { Source = "licensing", SourceLabel = "Licensing Fees", Target = "operations", TargetLabel = "Operations", Value = 50 },
        new FlowData { Source = "licensing", SourceLabel = "Licensing Fees", Target = "marketing", TargetLabel = "Marketing", Value = 30 },
    };

    private List<string> nodeColors = new List<string>
    {
        "#3B82F6", // Product Sales
        "#10B981", // Consulting Services
        "#F59E0B", // Licensing Fees
        "#EC4899", // Marketing
        "#8B5CF6", // Engineering
        "#06B6D4", // Operations
    };

    private List<string> linkColors = new List<string>
    {
        "#3B82F680", // Product Sales → Marketing
        "#3B82F680", // Product Sales → Engineering
        "#3B82F680", // Product Sales → Operations
        "#10B98180", // Consulting Services → Marketing
        "#10B98180", // Consulting Services → Engineering
        "#F59E0B80", // Licensing Fees → Engineering
        "#F59E0B80", // Licensing Fees → Operations
        "#F59E0B80", // Licensing Fees → Marketing
    };
}
```


### Custom Tooltips

Customize tooltip appearance and content with `ValueFormatter`, `ValueText`, `IncomingText`, `OutgoingText`, `FlowText`, and `TooltipStyle`.

```razor
<div class="rz-p-0 rz-p-md-12">
    <RadzenSankeyDiagram TItem="FlowData"
                         Data="@flows"
                         SourceProperty="Source"
                         TargetProperty="Target"
                         ValueProperty="Value"
                         SourceLabelProperty="SourceLabel"
                         TargetLabelProperty="TargetLabel"
                         ValueFormatter="@(value => $"${value:N0}K")"
                         ValueText="Total Budget"
                         IncomingText="Budget Received"
                         OutgoingText="Budget Allocated"
                         FlowText="Allocation"
                         TooltipStyle="background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; border-radius: 12px; padding: 12px 16px; box-shadow: 0 10px 25px rgba(0,0,0,0.2); font-weight: 500;">
    </RadzenSankeyDiagram>
</div>

@code {
    public class FlowData
    {
        public string Source { get; set; }
        public string Target { get; set; }
        public double Value { get; set; }
        public string SourceLabel { get; set; }
        public string TargetLabel { get; set; }
    }

    private List<FlowData> flows = new List<FlowData>
    {
        new FlowData { Source = "products", SourceLabel = "Product Sales", Target = "marketing", TargetLabel = "Marketing", Value = 150 },
        new FlowData { Source = "products", SourceLabel = "Product Sales", Target = "engineering", TargetLabel = "Engineering", Value = 280 },
        new FlowData { Source = "products", SourceLabel = "Product Sales", Target = "operations", TargetLabel = "Operations", Value = 70 },
        new FlowData { Source = "services", SourceLabel = "Consulting Services", Target = "marketing", TargetLabel = "Marketing", Value = 100 },
        new FlowData { Source = "services", SourceLabel = "Consulting Services", Target = "engineering", TargetLabel = "Engineering", Value = 160 },
        new FlowData { Source = "licensing", SourceLabel = "Licensing Fees", Target = "engineering", TargetLabel = "Engineering", Value = 80 },
        new FlowData { Source = "licensing", SourceLabel = "Licensing Fees", Target = "operations", TargetLabel = "Operations", Value = 50 },
        new FlowData { Source = "licensing", SourceLabel = "Licensing Fees", Target = "marketing", TargetLabel = "Marketing", Value = 30 },
    };
}
```


### Animation

Toggle flow animations with the `Animated` property to visualize data movement through the diagram.

```razor
<div class="rz-p-0 rz-p-md-12">
    <RadzenCard Variant="Variant.Outlined" class="rz-mb-12">
        <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="0.5rem">
            <RadzenCheckBox @bind-Value="@animated" Name="Animated" />
            <RadzenLabel Text="Animate flow" Component="Animated" />
        </RadzenStack>
    </RadzenCard>
    <RadzenSankeyDiagram TItem="FlowData"
                         Data="@flows"
                         SourceProperty="Source"
                         TargetProperty="Target"
                         ValueProperty="Value"
                         SourceLabelProperty="SourceLabel"
                         TargetLabelProperty="TargetLabel"
                         Animated="@animated">
    </RadzenSankeyDiagram>
</div>

@code {
    public class FlowData
    {
        public string Source { get; set; }
        public string Target { get; set; }
        public double Value { get; set; }
        public string SourceLabel { get; set; }
        public string TargetLabel { get; set; }
    }

    private bool animated = true;

    private List<FlowData> flows = new List<FlowData>
    {
        new FlowData { Source = "products", SourceLabel = "Product Sales", Target = "marketing", TargetLabel = "Marketing", Value = 150 },
        new FlowData { Source = "products", SourceLabel = "Product Sales", Target = "engineering", TargetLabel = "Engineering", Value = 280 },
        new FlowData { Source = "products", SourceLabel = "Product Sales", Target = "operations", TargetLabel = "Operations", Value = 70 },
        new FlowData { Source = "services", SourceLabel = "Consulting Services", Target = "marketing", TargetLabel = "Marketing", Value = 100 },
        new FlowData { Source = "services", SourceLabel = "Consulting Services", Target = "engineering", TargetLabel = "Engineering", Value = 160 },
        new FlowData { Source = "licensing", SourceLabel = "Licensing Fees", Target = "engineering", TargetLabel = "Engineering", Value = 80 },
        new FlowData { Source = "licensing", SourceLabel = "Licensing Fees", Target = "operations", TargetLabel = "Operations", Value = 50 },
        new FlowData { Source = "licensing", SourceLabel = "Licensing Fees", Target = "marketing", TargetLabel = "Marketing", Value = 30 },
    };
}
```
