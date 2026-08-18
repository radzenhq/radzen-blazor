# Linear Gauge

Show a value on a straight scale with a Blazor linear gauge - horizontal or vertical, with ranges and pointers.

Keywords: gauge, graph, linear, scale, bar

## Examples

## Blazor Linear Gauge

A linear gauge shows a value on a straight scale, horizontal or vertical, with ticks, ranges, and a pointer - think of a thermometer or a level meter.

### Basic Usage

A horizontal linear gauge with configurable tick placement, pointer type, and value.

```razor
<RadzenStack class="rz-p-0 rz-p-md-6 rz-p-lg-12" AlignItems="AlignItems.Center">
    <RadzenCard Variant="Variant.Outlined">
        <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Wrap="FlexWrap.Wrap">
            <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="0.5rem">
                <RadzenLabel Text="Value" Component="valueSlider" />
                <RadzenSlider Name="valueSlider" Min="0" Max="200" @bind-Value=@value Style="width: 200px;" />
            </RadzenStack>
            <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="0.5rem">
                <RadzenLabel Text="Tick position" Component="tickPositionDropDown" />
                <RadzenDropDown Name="tickPositionDropDown" @bind-Value=@tickPosition Data=@tickPositions Style="width: 160px;" />
            </RadzenStack>
            <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="0.5rem">
                <RadzenLabel Text="Pointer type" Component="pointerTypeDropDown" />
                <RadzenDropDown Name="pointerTypeDropDown" @bind-Value=@pointerType Data=@pointerTypes Style="width: 130px;" />
            </RadzenStack>
        </RadzenStack>
    </RadzenCard>

    <RadzenLinearGauge Style="width: 100%; height: 120px;">
        <RadzenLinearGaugeScale Step="40" Min="0" Max="200" MinorStep="10" TickPosition=@tickPosition>
            <RadzenLinearGaugeScalePointer Value=@value PointerType=@pointerType Fill="var(--rz-primary)" />
        </RadzenLinearGaugeScale>
    </RadzenLinearGauge>
</RadzenStack>

@code {
    double value = 80;
    GaugeTickPosition tickPosition = GaugeTickPosition.Outside;
    LinearGaugePointerType pointerType = LinearGaugePointerType.Arrow;
    IEnumerable<GaugeTickPosition> tickPositions = Enum.GetValues<GaugeTickPosition>().Cast<GaugeTickPosition>();
    IEnumerable<LinearGaugePointerType> pointerTypes = Enum.GetValues<LinearGaugePointerType>().Cast<LinearGaugePointerType>();
}
```


### Ranges and Value Display

Colored ranges highlight status zones. Use `ShowValue` and a `&lt;Template&gt;` to display a custom label next to the pointer.

```razor
<RadzenStack class="rz-p-0 rz-p-md-6 rz-p-lg-12" AlignItems="AlignItems.Center">
    <RadzenCard Variant="Variant.Outlined">
        <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Wrap="FlexWrap.Wrap">
            <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="0.5rem">
                <RadzenLabel Text="Value" Component="valueSlider" />
                <RadzenSlider Name="valueSlider" Min="0" Max="200" @bind-Value=@value Style="width: 200px;" />
            </RadzenStack>
            <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="0.5rem">
                <RadzenCheckBox @bind-Value=@showValue Name="showValueCheck" />
                <RadzenLabel Text="Show pointer value" Component="showValueCheck" />
            </RadzenStack>
        </RadzenStack>
    </RadzenCard>

    <RadzenLinearGauge Style="width: 100%; height: 140px;">
        <RadzenLinearGaugeScale Step="40" Min="0" Max="200" MinorStep="10">
            <RadzenLinearGaugeScalePointer Value=@value ShowValue=@showValue Fill="var(--rz-secondary)">
                <Template Context="pointer">
                    <RadzenStack AlignItems="AlignItems.Center" Gap="0">
                        <RadzenText TextStyle="TextStyle.H6" TagName="TagName.P" class="rz-m-0"><strong>@pointer.Value</strong></RadzenText>
                        <RadzenText TextStyle="TextStyle.Caption">km/h</RadzenText>
                    </RadzenStack>
                </Template>
            </RadzenLinearGaugeScalePointer>
            <RadzenLinearGaugeScaleRange From="0" To="80" Fill="var(--rz-success)" Height="8" />
            <RadzenLinearGaugeScaleRange From="80" To="140" Fill="var(--rz-warning)" Height="8" />
            <RadzenLinearGaugeScaleRange From="140" To="200" Fill="var(--rz-danger)" Height="8" />
        </RadzenLinearGaugeScale>
    </RadzenLinearGauge>
</RadzenStack>

@code {
    double value = 80;
    bool showValue = true;
}
```


### Rounded Ranges

Use `BorderRadius` on `RadzenLinearGaugeScaleRange` for pill-shaped bands.

```razor
<RadzenStack class="rz-p-0 rz-p-md-6 rz-p-lg-12" AlignItems="AlignItems.Center">
    <RadzenCard Variant="Variant.Outlined">
        <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Wrap="FlexWrap.Wrap">
            <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="0.5rem">
                <RadzenLabel Text="Value" Component="valueSlider" />
                <RadzenSlider Name="valueSlider" Min="0" Max="200" @bind-Value=@value Style="width: 200px;" />
            </RadzenStack>
        </RadzenStack>
    </RadzenCard>

    <RadzenLinearGauge Style="width: 100%; height: 130px;">
        <RadzenLinearGaugeScale Step="40" Min="0" Max="200" MinorStep="10">
            <RadzenLinearGaugeScalePointer Value=@value Fill="var(--rz-primary)" />
            <RadzenLinearGaugeScaleRange From="0" To="80" Fill="var(--rz-success)" Height="12" BorderRadius="6" />
            <RadzenLinearGaugeScaleRange From="80" To="140" Fill="var(--rz-warning)" Height="12" BorderRadius="6" />
            <RadzenLinearGaugeScaleRange From="140" To="200" Fill="var(--rz-danger)" Height="12" BorderRadius="6" />
        </RadzenLinearGaugeScale>
    </RadzenLinearGauge>
</RadzenStack>

@code {
    double value = 80;
}
```


### Reversed Scale

Set `Reversed="true"` to place Max on the left and Min on the right.

```razor
<RadzenStack class="rz-p-0 rz-p-md-6 rz-p-lg-12" AlignItems="AlignItems.Center">
    <RadzenCard Variant="Variant.Outlined">
        <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Wrap="FlexWrap.Wrap">
            <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="0.5rem">
                <RadzenLabel Text="Value" Component="valueSlider" />
                <RadzenSlider Name="valueSlider" Min="0" Max="200" @bind-Value=@value Style="width: 200px;" />
            </RadzenStack>
        </RadzenStack>
    </RadzenCard>

    <RadzenLinearGauge Style="width: 100%; height: 120px;">
        <RadzenLinearGaugeScale Step="40" Min="0" Max="200" MinorStep="10" Reversed="true">
            <RadzenLinearGaugeScalePointer Value=@value Fill="var(--rz-info)" />
            <RadzenLinearGaugeScaleRange From="0" To="80" Fill="var(--rz-success)" Height="8" />
            <RadzenLinearGaugeScaleRange From="80" To="140" Fill="var(--rz-warning)" Height="8" />
            <RadzenLinearGaugeScaleRange From="140" To="200" Fill="var(--rz-danger)" Height="8" />
        </RadzenLinearGaugeScale>
    </RadzenLinearGauge>
</RadzenStack>

@code {
    double value = 80;
}
```


### Draggable Pointer

Set `Draggable="true"` with `@-Value`. Click anywhere on the scale or drag the pointer to update the value.

```razor
<RadzenLinearGauge Style="width: 100%; height: 130px;">
    <RadzenLinearGaugeScale Step="40" Min="0" Max="200" MinorStep="10"
                            Click="@(v => value = v)">
        <RadzenLinearGaugeScalePointer @bind-Value=@value ShowValue="true"
                                       Draggable="true" Fill="var(--rz-secondary)" />
        <RadzenLinearGaugeScaleRange From="0" To="80" Fill="var(--rz-success)" Height="8" />
        <RadzenLinearGaugeScaleRange From="80" To="140" Fill="var(--rz-warning)" Height="8" />
        <RadzenLinearGaugeScaleRange From="140" To="200" Fill="var(--rz-danger)" Height="8" />
    </RadzenLinearGaugeScale>
</RadzenLinearGauge>

@code {
    double value = 60;
}
```


### Vertical Orientation

Set `Orientation="Orientation.Vertical"` for compact dashboard layouts. All pointer types and ranges are supported.

```razor
<RadzenStack class="rz-p-0 rz-p-md-6 rz-p-lg-12" AlignItems="AlignItems.Center">
    <RadzenCard Variant="Variant.Outlined">
        <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Wrap="FlexWrap.Wrap">
            <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="0.5rem">
                <RadzenLabel Text="Value" Component="valueSlider" />
                <RadzenSlider Name="valueSlider" Min="0" Max="200" @bind-Value=@value Style="width: 200px;" />
            </RadzenStack>
            <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="0.5rem">
                <RadzenCheckBox @bind-Value=@showValue Name="showValueCheck" />
                <RadzenLabel Text="Show pointer value" Component="showValueCheck" />
            </RadzenStack>
        </RadzenStack>
    </RadzenCard>

    <RadzenLinearGauge Style="width: 180px; height: 300px;">
        <RadzenLinearGaugeScale Step="40" Min="0" Max="200" MinorStep="10"
                                Orientation="Orientation.Vertical">
            <RadzenLinearGaugeScalePointer Value=@value ShowValue=@showValue Fill="var(--rz-info)" />
            <RadzenLinearGaugeScaleRange From="0" To="80" Fill="var(--rz-success)" Height="8" />
            <RadzenLinearGaugeScaleRange From="80" To="140" Fill="var(--rz-warning)" Height="8" />
            <RadzenLinearGaugeScaleRange From="140" To="200" Fill="var(--rz-danger)" Height="8" />
        </RadzenLinearGaugeScale>
    </RadzenLinearGauge>
</RadzenStack>

@code {
    double value = 80;
    bool showValue = true;
}
```


### Multiple Scales

Use `LinePosition` to stack independent scales inside the same gauge container.

```razor
<RadzenStack class="rz-p-0 rz-p-md-6 rz-p-lg-12" AlignItems="AlignItems.Center">
    <RadzenCard Variant="Variant.Outlined">
        <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Wrap="FlexWrap.Wrap">
            <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="0.5rem">
                <RadzenLabel Text="Value" Component="valueSlider" />
                <RadzenSlider Name="valueSlider" Min="0" Max="200" @bind-Value=@value Style="width: 200px;" />
            </RadzenStack>
        </RadzenStack>
    </RadzenCard>

    <RadzenLinearGauge Style="width: 100%; height: 200px;">
        <RadzenLinearGaugeScale Step="40" Min="0" Max="200" MinorStep="0"
                                TickPosition="GaugeTickPosition.Outside" LinePosition="0.3">
            <RadzenLinearGaugeScalePointer Value=@value Fill="var(--rz-primary)" />
        </RadzenLinearGaugeScale>
        <RadzenLinearGaugeScale Step="40" Min="0" Max="200" MinorStep="0"
                                TickPosition="GaugeTickPosition.Inside" LinePosition="0.7" ShowTickLabels="false">
            <RadzenLinearGaugeScalePointer Value=@(200 - value) Fill="var(--rz-secondary)" />
        </RadzenLinearGaugeScale>
    </RadzenLinearGauge>
</RadzenStack>

@code {
    double value = 80;
}
```
