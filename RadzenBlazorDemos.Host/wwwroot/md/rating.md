# Rating

The Blazor Rating captures a star rating, with a configurable number of stars and disabled or read-only modes.

Keywords: star, form, edit

> API reference: [RadzenRating API](https://blazor.radzen.com/api/rating.md)

## Examples

## Blazor Rating

The Blazor Rating captures a star rating, with a configurable number of stars and disabled or read-only modes.

### Get and Set the value of Rating

As all Radzen Blazor input components the Rating has a Value property which gets and sets the value of the component. Use `@-Value` to get the user input.

```razor
<div class="rz-p-12 rz-text-align-center">
    <RadzenRating @bind-Value=@value />
</div>

@code{
    int value;
}
```


### Get and Set the value of Rating using Value and Change event

Value property can be used to set the value of the component and `Change` event to get the user input.

```razor
<div class="rz-p-12 rz-text-align-center">
    <RadzenRating TValue="int" Value=@value Change=@(args => value = args) />
</div>

@code{
    int value;
}
```


### Set number of stars

Use the `Stars` property to configure the number of stars displayed in the rating component.

```razor
<div class="rz-p-12 rz-text-align-center">
    <RadzenRating @bind-Value=@value Stars="10" />
</div>

@code{
    int value = 4;
}
```


### Disabled Rating

Use `Disabled="true"` to disable the Rating and prevent user interaction.

```razor
<div class="rz-p-12 rz-text-align-center">
    <RadzenRating @bind-Value=@value Disabled=true />
</div>

@code{
    int value = 3;
}
```


### Read-only Rating

Use `ReadOnly="true"` to display a rating that users can view but not modify.

```razor
<div class="rz-p-12 rz-text-align-center">
    <RadzenRating @bind-Value=@value ReadOnly=true/>
</div>

@code{
    int value = 2;
}
```
