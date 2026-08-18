# ProgressBarCircular

Demonstration and configuration of the Radzen Blazor circular progress bar component.

Keywords: progress, spinner, circle, circular

> API reference: [RadzenProgressBarCircular API](https://blazor.radzen.com/api/progressbarcircular.md)

## Examples

## Blazor ProgressBarCircular

Circular progress bar with determinate and indeterminate modes and size options.

### ProgressBarCircular in determinate mode, Get and Set the value

As all Radzen Blazor input components the ProgressBarCircular has a `Value` property which gets and sets the value of the component. Use `@-Value` to get the user input.

```razor
<RadzenStack AlignItems="AlignItems.Center" class="rz-m-12" Gap="2rem">
    <RadzenProgressBarCircular @bind-Value="@value" AriaLabel="Progress" />
    <RadzenText TextStyle="TextStyle.Subtitle2" TagName="TagName.H3">ProgressBarCircular with <strong>ProgressBarStyle</strong> property</RadzenText>
    <RadzenStack Orientation="Orientation.Horizontal" JustifyContent="JustifyContent.Center" Wrap="FlexWrap.Wrap">
        <RadzenProgressBarCircular ProgressBarStyle="ProgressBarStyle.Primary" @bind-Value="@value" AriaLabel="Primary progress" />
        <RadzenProgressBarCircular ProgressBarStyle="ProgressBarStyle.Secondary" @bind-Value="@value" AriaLabel="Secondary progress" />
        <RadzenProgressBarCircular ProgressBarStyle="ProgressBarStyle.Base" @bind-Value="@value" AriaLabel="Base progress" />
        <RadzenProgressBarCircular ProgressBarStyle="ProgressBarStyle.Light" @bind-Value="@value" AriaLabel="Light progress" />
        <RadzenProgressBarCircular ProgressBarStyle="ProgressBarStyle.Dark" @bind-Value="@value" AriaLabel="Dark progress" />
        <RadzenProgressBarCircular ProgressBarStyle="ProgressBarStyle.Success" @bind-Value="@value" AriaLabel="Success progress" />
        <RadzenProgressBarCircular ProgressBarStyle="ProgressBarStyle.Danger" @bind-Value="@value" AriaLabel="Danger progress" />
        <RadzenProgressBarCircular ProgressBarStyle="ProgressBarStyle.Warning" @bind-Value="@value" AriaLabel="Warning progress" />
        <RadzenProgressBarCircular ProgressBarStyle="ProgressBarStyle.Info" @bind-Value="@value" AriaLabel="Info progress" />
    </RadzenStack>
</RadzenStack>

@code {
    double value = 55;
}
```


### ProgressBarCircular in indeterminate mode

Use `Mode="ProgressBarMode.Indeterminate"` to display an animated circular spinner when the completion time is unknown.

```razor
<RadzenStack AlignItems="AlignItems.Center" class="rz-m-12" Gap="2rem">
    <RadzenProgressBarCircular Value="100" ShowValue="false" Mode="ProgressBarMode.Indeterminate" AriaLabel="Loading progress" />
    <RadzenText TextStyle="TextStyle.Subtitle2" TagName="TagName.H3">ProgressBarCircular with <strong>ProgressBarStyle</strong> property</RadzenText>
    <RadzenStack Orientation="Orientation.Horizontal" JustifyContent="JustifyContent.Center" Wrap="FlexWrap.Wrap">
        <RadzenProgressBarCircular ProgressBarStyle="ProgressBarStyle.Primary" Value="100" ShowValue="false" Mode="ProgressBarMode.Indeterminate" AriaLabel="Primary loading" />
        <RadzenProgressBarCircular ProgressBarStyle="ProgressBarStyle.Secondary" Value="100" ShowValue="false" Mode="ProgressBarMode.Indeterminate" AriaLabel="Secondary loading" />
        <RadzenProgressBarCircular ProgressBarStyle="ProgressBarStyle.Base" Value="100" ShowValue="false" Mode="ProgressBarMode.Indeterminate" AriaLabel="Base loading" />
        <RadzenProgressBarCircular ProgressBarStyle="ProgressBarStyle.Light" Value="100" ShowValue="false" Mode="ProgressBarMode.Indeterminate" AriaLabel="Light loading" />
        <RadzenProgressBarCircular ProgressBarStyle="ProgressBarStyle.Dark" Value="100" ShowValue="false" Mode="ProgressBarMode.Indeterminate" AriaLabel="Dark loading" />
        <RadzenProgressBarCircular ProgressBarStyle="ProgressBarStyle.Success" Value="100" ShowValue="false" Mode="ProgressBarMode.Indeterminate" AriaLabel="Success loading" />
        <RadzenProgressBarCircular ProgressBarStyle="ProgressBarStyle.Danger" Value="100" ShowValue="false" Mode="ProgressBarMode.Indeterminate" AriaLabel="Danger loading" />
        <RadzenProgressBarCircular ProgressBarStyle="ProgressBarStyle.Warning" Value="100" ShowValue="false" Mode="ProgressBarMode.Indeterminate" AriaLabel="Warning loading" />
        <RadzenProgressBarCircular ProgressBarStyle="ProgressBarStyle.Info" Value="100" ShowValue="false" Mode="ProgressBarMode.Indeterminate" AriaLabel="Info loading" />
    </RadzenStack>
</RadzenStack>
```


### ProgressBarCircular sizes

Use the `Size` property to set sizes of the progress circle and the text inside it. Available sizes are ExtraSmall, Small, Medium (default), and Large.

```razor
<RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" JustifyContent="JustifyContent.Center" Wrap="FlexWrap.Wrap" class="rz-m-12" Gap="2rem">
    <RadzenProgressBarCircular ShowValue="true" Mode="ProgressBarMode.Indeterminate" Size="ProgressBarCircularSize.ExtraSmall" AriaLabel="Loading, extra small">
        <Template>Wait</Template>
    </RadzenProgressBarCircular>
    <RadzenProgressBarCircular ShowValue="true" Mode="ProgressBarMode.Indeterminate" Size="ProgressBarCircularSize.Small" AriaLabel="Loading, small">
        <Template>Wait</Template>
    </RadzenProgressBarCircular>
    <RadzenProgressBarCircular ShowValue="true" Mode="ProgressBarMode.Indeterminate" Size="ProgressBarCircularSize.Medium" AriaLabel="Loading, medium">
        <Template>Wait</Template>
    </RadzenProgressBarCircular>
    <RadzenProgressBarCircular ShowValue="true" Mode="ProgressBarMode.Indeterminate" Size="ProgressBarCircularSize.Large" AriaLabel="Loading, large">
        <Template>Wait</Template>
    </RadzenProgressBarCircular>
</RadzenStack>
```


### ProgressBarCircular Min and Max values

By default, the value range is between 0 and 100. Use the `Min` and `Max` properties to set a custom range.

```razor
<RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" JustifyContent="JustifyContent.Center" Wrap="FlexWrap.Wrap" class="rz-m-12" Gap="2rem">
    <RadzenProgressBarCircular Value="-10" Min="-100" Max="100" Unit="" AriaLabel="Temperature" />
    <RadzenProgressBarCircular Value="255" Max="360" Unit="°" AriaLabel="Angle" />
</RadzenStack>
```


### Accessibility

Use the `AriaLabel` property to provide a descriptive label for screen readers. This is especially important when `ShowValue="false"` as screen readers need context about what the progress bar represents (e.g., "Upload progress", "Loading", "Download status").
