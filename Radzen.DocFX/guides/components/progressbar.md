# ProgressBar component
This article demonstrates how to use the ProgressBar component. 

## Get and set the value
As all Radzen Blazor input components the ProgressBar has a `Value` property which gets and sets the value of the component.
Use `@bind-Value` to get the user input. 

```
<RadzenProgressBar @bind-Value="@value"  />

@code {
    double value = 55;
}
```

## ProgressBar in indeterminate mode
```
<RadzenProgressBar Value="100" ShowValue="false" Mode="ProgressBarMode.Indeterminate" />
```

## ProgressBar in max value > 100
```
<RadzenProgressBar Value="156" Max="200" />
```

