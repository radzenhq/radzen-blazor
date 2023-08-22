# RadialGauge component
This article demonstrates how to use RadzenRadialGauge. 
## Basic usage
Here is basic example that creates a radial gauge with minimal configuration.
```
<RadzenRadialGauge Style="width: 300px; height: 300px">
  <RadzenRadialGaugeScale>
    <RadzenRadialGaugeScalePointer Value="50" />
  </RadzenRadialGaugeScale>
</RadzenRadialGauge>
```
RadzenRadialGaugeScale is used to add a scale and configure its options - min and max value, start and end angle, tick display etc.
RadzenRadialGaugeScalePointer tag adds and configures a pointer of the RadialGaugeScale it is a child of. The key property here is `Value` - it specifies the value the pointer "points" to on the scale.

Radzen Blazor gauges can have multiple scales and every scale can have multiple pointers or values.
## Ranges
The RadzenRadialGaugeScale supports ranges. A range applies a color between two values of the scale. For example this is often used to specify a "dangerous" zone of the scale
which a pointer isn't supposed to go to. A RadzenRadialGaugeScale can have multiple ranges that should not overlap.
```
<RadzenRadialGauge>
  <RadzenRadialGaugeScale Min="0" Max="260">
    <RadzenRadialGaugeScalePointer Value="50" />
    <RadzenRadialGaugeScaleRange From="0" To="90" Fill="green" />
    <RadzenRadialGaugeScaleRange From="90" To="140" Fill="orange" />
    <RadzenRadialGaugeScaleRange From="140" To="260" Fill="red" />
  </RadzenRadialGaugeScale>
</RadzenRadialGauge>
```
## Scale configuration
### Min, max and step
By default the `Min` property of both scale types (Radial and Radial) is set to `0`. `Max` is set to `100` and `Step` is set to `20`.

To override the defaults use the `Min`, `Max` and `Step` properties of the `RadzenRadialGaugeScale` tag.
```
<RadzenRadialGauge Style="width: 300px; height: 300px">
  <RadzenRadialGaugeScale Min="100" Max="1000" Step="100">
    <RadzenRadialGaugeScalePointer Value="50" />
  </RadzenRadialGaugeScale>
</RadzenRadialGauge>
```
### Tick configuration
By default the RadzenRadialGaugeScale does not display ticks. You need to set the `TickPosition` property to `GaugeTickPosition.Outside` or `GaugeTickPosition.Inside`. To hide the ticks altogether use `GaugeTickPosition.None`.
```
<RadzenRadialGauge Style="width: 300px; height: 300px">
  <RadzenRadialGaugeScale TickPosition="GaugeTickPosition.Outside">
    <RadzenRadialGaugeScalePointer Value="50" />
  </RadzenRadialGaugeScale>
</RadzenRadialGauge>
```
Minor ticks are not displayed by default. To display them set the MinorStep property to a value greater than `0`.
```
<RadzenRadialGauge Style="width: 300px; height: 300px">
  <RadzenRadialGaugeScale TickPosition="GaugeTickPosition.Outside" MinorStep="5">
    <RadzenRadialGaugeScalePointer Value="50" />
  </RadzenRadialGaugeScale>
</RadzenRadialGauge>
```
### Change the start and and angles
By default the `StartAngle` property of the gauge scales is set to `-90` and `EndAngle` is set to `90`. This makes
the default shape half a circle. Here is how to create a gauge which is a full circle:
```
<RadzenRadialGauge>
  <RadzenRadialGaugeScale StartAngle="0" EndAngle="360">
    <RadzenRadialGaugeScalePointer Value="50" />
  </RadzenRadialGaugeScale>
</RadzenRadialGauge>
```
### Format the values
The scale ticks labels displays values with default formatting (`ToString()`). This can be customized in two ways - via the `FormatString` or the `Formatter` properties.

`FormatString` supports the [standard .NET Number formats](https://docs.microsoft.com/en-us/dotnet/standard/base-types/standard-numeric-format-strings).

```
<RadzenRadialGauge Style="width: 300px; height: 300px">
  <RadzenRadialGaugeScale FormatString="{0:C}">
    <RadzenGaugeGaugeScalePointer Value="50" />
  </RadzenGaugeGaugeScale>
</RadzenGaugeGauge>
```

```
<RadzenRadialGauge Style="width: 300px; height: 300px">
  <RadzenRadialGaugeScale Formatter=@(value => value.ToString())>
    <RadzenGaugeGaugeScalePointer Value="50" />
  </RadzenGaugeGaugeScale>
</RadzenGaugeGauge>
```
### Change the scale position

You can use the `X` and `Y` property of the scales to change the position of their center. Both properties have a default value of `0.5` which means
that by default the center of a scale is the middle of the gauge. `X` and `Y` are a multiplier of the width and height.

For example you can move the center of the scale to the bottom of the component by setting `Y` to `1`.

```
<RadzenRadialGauge Style="width: 300px; height: 300px">
  <RadzenRadialGaugeScale Y="1">
    <RadzenGaugeGaugeScalePointer Value="50" />
  </RadzenGaugeGaugeScale>
</RadzenGaugeGauge>
```

Using `X` and `Y` is also useful when you have multiple scales - this allows you to prevent them from overlapping which they will do by default.
## Pointer configuration
### Pointer length
By default a RadialGaugeScalePointer is as long as the radius of its scale. You can controll that via the `Length` property which is a multiplier with a default value `1`.

Here is how to make the pointer half the radius:
```
<RadzenRadialGauge>
  <RadzenRadialGaugeScale>
    <RadzenRadialGaugeScalePointer Value="50" Length="0.5" />
  </RadzenRadialGaugeScale>
</RadzenRadialGauge>
```
### Hide the pointer value
By default the `Value` property is displayed below the pointer. You can hide it by setting the `ShowValue` property to `false`.
```
<RadzenRadialGauge>
  <RadzenRadialGaugeScale>
    <RadzenRadialGaugeScalePointer Value="50" ShowValue="false" />
  </RadzenRadialGaugeScale>
</RadzenRadialGauge>
```
### Customize the value display
Use the `Template` property of the pointer to tweak the default value appearance.

```
<RadzenRadialGauge>
  <RadzenRadialGaugeScale Min="0" Max="260">
    <RadzenRadialGaugeScalePointer Value=@value>
      <Template Context="pointer">
        <h4>
            @pointer.Value <sup>km/h</sup>
        </h4>
      </Template>
    </RadzenRadialGaugeScalePointer>
  </RadzenRadialGaugeScale>
</RadzenRadialGauge>
```