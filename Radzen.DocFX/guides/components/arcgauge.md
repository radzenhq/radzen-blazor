# ArcGauge component
This article demonstrates how to use RadzenArcGauge.
## Basic usage
Here is basic example that creates an arc gauge with minimal configuration.
```
<RadzenArcGauge Style="width: 300px; height: 300px">
  <RadzenArcGaugeScale>
    <RadzenArcGaugeScaleValue Value="50" />
  </RadzenArcGaugeScale>
</RadzenArcGauge>
```
RadzenArcGaugeScale tag is used to add a scale and configure its options - min and max value, start and end angle, tick display etc.
RadzenArcGaugeScaleValue tag adds and configures a value of the ArcGaugeScale it is a child of.

Radzen Blazor gauges can have multiple scales and every scale can have multiple pointers or values.
## Scale configuration
### Min, max and step
By default the `Min` property of both scale types (Arc and Arc) is set to `0`. `Max` is set to `100` and `Step` is set to `20`.

To override the defaults use the `Min`, `Max` and `Step` properties of the `RadzenArcGaugeScale` tag.
```
<RadzenArcGauge Style="width: 300px; height: 300px">
  <RadzenArcGaugeScale Min="100" Max="1000" Step="100">
    <RadzenArcGaugeScaleValue Value="50" />
  </RadzenArcGaugeScale>
</RadzenArcGauge>
```
### Tick configuration
By default the RadzenArcGaugeScale does not display ticks. You need to set the `TickPosition` property to `GaugeTickPosition.Outside` or `GaugeTickPosition.Inside`. To hide the ticks altogether use `GaugeTickPosition.None`.
```
<RadzenArcGauge Style="width: 300px; height: 300px">
  <RadzenArcGaugeScale TickPosition="GaugeTickPosition.Outside">
    <RadzenArcGaugeScaleValue Value="50" />
  </RadzenArcGaugeScale>
</RadzenArcGauge>
```
Minor ticks are not displayed by default. To display them set the MinorStep property to a value greater than `0`.
```
<RadzenArcGauge Style="width: 300px; height: 300px">
  <RadzenArcGaugeScale TickPosition="GaugeTickPosition.Outside" MinorStep="5">
    <RadzenArcGaugeScaleValue Value="50" />
  </RadzenArcGaugeScale>
</RadzenArcGauge>
```
### Change the start and and angles
By default the `StartAngle` property of the gauge scales is set to `-90` and `EndAngle` is set to `90`. This makes
the default shape half a circle. Here is how to create a gauge which is a full circle:
```
<RadzenArcGauge>
  <RadzenArcGaugeScale StartAngle="0" EndAngle="360">
    <RadzenArcGaugeScaleValue Value="50" />
  </RadzenArcGaugeScale>
</RadzenArcGauge>
```
### Format the values
The scale ticks labels displays values with default formatting (`ToString()`). This can be customized in two ways - via the `FormatString` or the `Formatter` properties.

`FormatString` supports the [standard .NET Number formats](https://docs.microsoft.com/en-us/dotnet/standard/base-types/standard-numeric-format-strings).

```
<RadzenArcGauge Style="width: 300px; height: 300px">
  <RadzenArcGaugeScale FormatString="{0:C}">
    <RadzenArcGaugeScaleValue Value="50" />
  </RadzenGaugeGaugeScale>
</RadzenGaugeGauge>
```

```
<RadzenArcGauge Style="width: 300px; height: 300px">
  <RadzenArcGaugeScale Formatter=@(value => value.ToString())>
    <RadzenArcGaugeScaleValue Value="50" />
  </RadzenGaugeGaugeScale>
</RadzenGaugeGauge>
```
### Change the scale position

You can use the `X` and `Y` property of the scales to change the position of their center. Both properties have a default value of `0.5` which means
that by default the center of a scale is the middle of the gauge. `X` and `Y` are a multiplier of the width and height.

For example you can move the center of the scale to the bottom of the component by setting `Y` to `1`.

```
<RadzenArcGauge Style="width: 300px; height: 300px">
  <RadzenArcGaugeScale Y="1">
    <RadzenArcGaugeScaleValue Value="50" />
  </RadzenGaugeGaugeScale>
</RadzenGaugeGauge>
```

Using `X` and `Y` is also useful when you have multiple scales - this allows you to prevent them from overlapping which they will do by default.

### Change the scale radius

By default the radius is set to be half the size of the Gauge - the smaller of its pixel width or height. You can tweak that
by setting the `Radius` property. It is also a multiplier - the value you specify is multiplied by the initial value (half the width or height depending on which is smaller).
The reason Radius is a multiplier and not an absolute value is responsiveness - users of smaller devices would expect to see a scale which is proportionally the same.

Here is how to make a scale twice as small

```
<RadzenArcGauge Style="width: 300px; height: 300px">
  <RadzenArcGaugeScale Radius="0.5">
    <RadzenArcGaugeScaleValue Value="50" />
  </RadzenGaugeGaugeScale>
</RadzenGaugeGauge>
```
## Value configuration
### Hide the value
By default the `Value` property is displayed below the scale. You can hide it by setting the `ShowValue` property to `false`.
```
<RadzenArcGauge>
  <RadzenArcGaugeScale>
    <RadzenArcGaugeScaleValue Value="50" ShowValue="false" />
  </RadzenArcGaugeScale>
</RadzenArcGauge>
```
### Customize the value display
Use the `Template` property of the pointer to tweak the default value appearance.
```
<RadzenArcGauge>
  <RadzenArcGaugeScale Min="0" Max="260">
    <RadzenArcGaugeScaleValue Value=@value>
      <Template Context="value">
        <h4>
            @value.Value <sup>km/h</sup>
        </h4>
      </Template>
    </RadzenArcGaugeScaleValue>
  </RadzenArcGaugeScale>
</RadzenArcGauge>
```