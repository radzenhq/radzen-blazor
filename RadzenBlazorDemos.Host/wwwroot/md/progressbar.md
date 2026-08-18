# ProgressBar

Demonstration and configuration of the Radzen Blazor ProgressBar component.

Keywords: progress, spinner, bar, linear

> API reference: [RadzenProgressBar API](https://blazor.radzen.com/api/progressbar.md)

## Examples

## Blazor ProgressBar

Determinate and indeterminate progress indication modes.

### ProgressBar in determinate mode, Get and Set the value

As all Radzen Blazor input components the ProgressBar has a `Value` property which gets and sets the value of the component. Use `@-Value` to get the user input. To hide the label use `ShowValue="false"`
By design Material and Fluent themes do not include a value label inside the ProgressBar. To hide the label use `ShowValue="false"`.

```razor
<div class="rz-m-12">
    <RadzenProgressBar @bind-Value="@value" AriaLabel="Progress" />
</div>

<RadzenStack Gap="1rem" class="rz-m-12">
    <RadzenText TextStyle="TextStyle.Subtitle2" TagName="TagName.H3">ProgressBar with <strong>ProgressBarStyle</strong> property and <strong>ShowValue="false"</strong></RadzenText>
    <RadzenProgressBar ProgressBarStyle="ProgressBarStyle.Primary" @bind-Value="@value" ShowValue="false" AriaLabel="Primary progress" />
    <RadzenProgressBar ProgressBarStyle="ProgressBarStyle.Secondary" @bind-Value="@value" ShowValue="false" AriaLabel="Secondary progress" />
    <RadzenProgressBar ProgressBarStyle="ProgressBarStyle.Base" @bind-Value="@value" ShowValue="false" AriaLabel="Base progress" />
    <RadzenProgressBar ProgressBarStyle="ProgressBarStyle.Light" @bind-Value="@value" ShowValue="false" AriaLabel="Light progress" />
    <RadzenProgressBar ProgressBarStyle="ProgressBarStyle.Dark" @bind-Value="@value" ShowValue="false" AriaLabel="Dark progress" />
    <RadzenProgressBar ProgressBarStyle="ProgressBarStyle.Success" @bind-Value="@value" ShowValue="false" AriaLabel="Success progress" />
    <RadzenProgressBar ProgressBarStyle="ProgressBarStyle.Danger" @bind-Value="@value" ShowValue="false" AriaLabel="Danger progress" />
    <RadzenProgressBar ProgressBarStyle="ProgressBarStyle.Warning" @bind-Value="@value" ShowValue="false" AriaLabel="Warning progress" />
    <RadzenProgressBar ProgressBarStyle="ProgressBarStyle.Info" @bind-Value="@value" ShowValue="false" AriaLabel="Info progress" />
</RadzenStack>

@code {
    double value = 55;
}
```


### ProgressBar in indeterminate mode

Use `Mode="ProgressBarMode.Indeterminate"` to display an animated progress bar when the completion time is unknown.

```razor
<div class="rz-m-12">
    <RadzenProgressBar Value="100" ShowValue="false" Mode="ProgressBarMode.Indeterminate" AriaLabel="Loading progress" />
</div>

<RadzenStack Gap="1rem" class="rz-m-12">
    <RadzenText TextStyle="TextStyle.Subtitle2" TagName="TagName.H3">ProgressBar with <strong>ProgressBarStyle</strong> property</RadzenText>
    <RadzenProgressBar ProgressBarStyle="ProgressBarStyle.Primary" Value="100" ShowValue="false" Mode="ProgressBarMode.Indeterminate" AriaLabel="Primary loading" />
    <RadzenProgressBar ProgressBarStyle="ProgressBarStyle.Secondary" Value="100" ShowValue="false" Mode="ProgressBarMode.Indeterminate" AriaLabel="Secondary loading" />
    <RadzenProgressBar ProgressBarStyle="ProgressBarStyle.Base" Value="100" ShowValue="false" Mode="ProgressBarMode.Indeterminate" AriaLabel="Base loading" />
    <RadzenProgressBar ProgressBarStyle="ProgressBarStyle.Light" Value="100" ShowValue="false" Mode="ProgressBarMode.Indeterminate" AriaLabel="Light loading" />
    <RadzenProgressBar ProgressBarStyle="ProgressBarStyle.Dark" Value="100" ShowValue="false" Mode="ProgressBarMode.Indeterminate" AriaLabel="Dark loading" />
    <RadzenProgressBar ProgressBarStyle="ProgressBarStyle.Success" Value="100" ShowValue="false" Mode="ProgressBarMode.Indeterminate" AriaLabel="Success loading" />
    <RadzenProgressBar ProgressBarStyle="ProgressBarStyle.Danger" Value="100" ShowValue="false" Mode="ProgressBarMode.Indeterminate" AriaLabel="Danger loading" />
    <RadzenProgressBar ProgressBarStyle="ProgressBarStyle.Warning" Value="100" ShowValue="false" Mode="ProgressBarMode.Indeterminate" AriaLabel="Warning loading" />
    <RadzenProgressBar ProgressBarStyle="ProgressBarStyle.Info" Value="100" ShowValue="false" Mode="ProgressBarMode.Indeterminate" AriaLabel="Info loading" />
</RadzenStack>
```


### ProgressBar Min and Max values

By default, the value range is between 0 and 100. Use the `Min` and `Max` properties to set a custom range.

```razor
<RadzenStack Gap="1rem" class="rz-m-12">
    <RadzenProgressBar Value="156" Max="200" Unit=" out of 200" AriaLabel="Upload progress" />
    <RadzenProgressBar Value="-10" Min="-100" Max="100" Unit="" AriaLabel="Temperature" />
</RadzenStack>
```


### Accessibility

Use the `AriaLabel` property to provide a descriptive label for screen readers. This is especially important when `ShowValue="false"` as screen readers need context about what the progress bar represents (e.g., "Upload progress", "Loading", "Download status").
