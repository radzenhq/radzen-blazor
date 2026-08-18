# RadzenProgressBarCircular API Reference

## Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| AriaLabel | `string?` | Gets or sets the ARIA label for accessibility support. Announced by screen readers to describe the progress bar's purpose (e.g., "File upload progress"). |
| Max | `double` | Gets or sets the maximum value of the progress range representing 100% completion. |
| Min | `double` | Gets or sets the minimum value of the progress range. Use non-zero values for custom progress scales (e.g., 0-1000 for byte counts). |
| Mode | `ProgressBarMode` | Gets or sets the progress bar mode determining the visual behavior. Determinate shows specific progress, Indeterminate shows continuous animation for unknown duration. |
| ProgressBarStyle | `ProgressBarStyle` | Gets or sets the semantic color style of the progress bar. Determines the progress bar color: Primary, Success, Info, Warning, Danger, etc. |
| ShowValue | `bool` | Gets or sets whether to display the progress value as text overlay on the progress bar. When true, shows the value with the unit (e.g., "45%"). Set to false for a cleaner look. |
| Size | `ProgressBarCircularSize` | Gets or sets the size of the circular progress indicator. Controls the diameter of the circle: ExtraSmall, Small, Medium, or Large. |
| Template | `RenderFragment?` | Gets or sets a custom template for rendering content overlaid on the progress bar. Use this to display custom progress information instead of the default value/percentage display. |
| Unit | `string` | Gets or sets the unit text displayed after the value (e.g., "%", "MB", "items"). Only shown when is true. |
| Value | `double` | Gets or sets the current progress value. Should be between and . Values outside this range are clamped. |
| ValueChanged | `Action<double>?` | Gets or sets a callback invoked when the progress value changes. Note: This is an Action, not EventCallback. For data binding, the Value property is typically bound directly. |

