# Slider

The Blazor Slider selects a single value or a range by dragging, with step increments and horizontal or vertical orientation.

Keywords: form, slider, range

> API reference: [RadzenSlider API](https://blazor.radzen.com/api/slider.md)

## Examples

## Blazor Slider

The Blazor Slider selects a single value or a range by dragging, with step increments and horizontal or vertical orientation.

### Get and Set the value of Slider

As all Radzen Blazor input components the Slider has a Value property which gets and sets the value of the component. Use `@-Value` to get the user input.

```razor
<div class="rz-p-12 rz-text-align-center">
    <RadzenSlider @bind-Value=@value TValue="int" />
</div>

@code {
    int value = 33;
}
```


### Get and Set the value of Slider using Value and Change event

Value property can be used to set the value of the component and `Change` event to get the user input.

```razor
<div class="rz-p-12 rz-text-align-center">
    <RadzenSlider Value=@value Change="@(args => value = args)" TValue="int" />
</div>

@code {
    int value = 55;
}
```


### Slider from -100 to 100

Use the `Min` and `Max` properties to define the minimum and maximum values for the slider range.

```razor
<div class="rz-p-12 rz-text-align-center">
    <RadzenSlider @bind-Value=@value TValue="int" Min="-100" Max="100" />
</div>

@code {
    int value = -33;
}
```


### Slider with Step=10

Use the `Step` property to define the increment/decrement value when moving the slider handle.

```razor
<div class="rz-p-12 rz-text-align-center">
    <RadzenSlider @bind-Value=@value TValue="int" Step="10" />
</div>

@code {
    int value = 10;
}
```


### Range Slider

Use `Range="true"` to enable range selection mode with two handles for selecting a value range.

```razor
<div class="rz-p-12 rz-text-align-center">
    <RadzenSlider Range="true" @bind-Value=@values TValue="IEnumerable<int>" />
</div>

@code {
    IEnumerable<int> values = new int[] { 14, 78 };
}
```


### Disabled Slider

Use `Disabled="true"` to disable the Slider and prevent user interaction.

```razor
<div class="rz-p-12 rz-text-align-center">
    <RadzenSlider @bind-Value=@value TValue="int" Disabled=true />
</div>

@code {
    int value = 33;
}
```


### Vertical Slider

Use `Orientation="Orientation.Vertical"` to display the Slider in a vertical orientation.

```razor
<RadzenStack Orientation="Orientation.Horizontal" JustifyContent="JustifyContent.SpaceAround">
    <RadzenSlider @bind-Value=@value TValue="int" Orientation="Orientation.Vertical" />
    <RadzenSlider Range="true" @bind-Value=@values TValue="IEnumerable<int>" Orientation="Orientation.Vertical" />
</RadzenStack>

@code {
    int value = 33;
    IEnumerable<int> values = new int[] { 14, 78 };
}
```
