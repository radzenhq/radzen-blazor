# RadzenSteps API Reference

## Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| AllowStepSelect | `bool` |  |
| Attributes | `IReadOnlyDictionary<string, object>?` | Gets or sets a dictionary of additional HTML attributes that will be applied to the component's root element. Any attributes not explicitly defined as parameters will be captured here and rendered on the element. Use this to add data-* attributes, ARIA attributes, or any custom HTML attributes. |
| Culture | `CultureInfo` | Gets or sets the culture used for formatting and parsing localizable data (numbers, dates, currency). If not set, uses the from a parent component or falls back to . |
| NextText | `string` | Gets or sets the next button text. |
| NextTitle | `string` | Gets or sets the next button title attribute. |
| PreviousText | `string` | Gets or sets the previous button text. |
| PreviousTitle | `string` | Gets or sets the previous button title attribute. |
| SelectedIndex | `int` | Gets or sets the selected index. |
| ShowStepsButtons | `bool` | Gets or sets whether to display the built-in Next and Previous navigation buttons below the step content. When false, you must provide your own navigation buttons using NextStep() and PrevStep() methods. |
| Steps | `RenderFragment?` | Gets or sets the steps. |
| Style | `string?` | Gets or sets the inline CSS style. |
| TabListAriaLabel | `string` | Gets or sets the aria-label applied to the steps tab list. |
| Transition | `StepsTransition` | Gets or sets the transition animation used when switching between steps. |
| TransitionDuration | `int` | Gets or sets the duration of the transition animation in milliseconds. |
| UICulture | `CultureInfo` | Gets or sets the culture used for localized UI strings. If not set, uses the from a parent component or falls back to . |
| Visible | `bool` | Gets or sets a value indicating whether this is visible. Invisible components are not rendered. |

## Events

| Event | Type | Description |
|-------|------|-------------|
| CanChange | `EventCallback<StepsCanChangeEventArgs>` | A callback that will be invoked when the user tries to change the step. Invoke the method to prevent this change. |
| Change | `EventCallback<int>` | Gets or sets the change callback. |
| ContextMenu | `EventCallback<Microsoft.AspNetCore.Components.Web.MouseEventArgs>` | Gets or sets the callback invoked when the user right-clicks the component. Commonly used with to display context menus. Receives mouse event arguments containing click position. |
| MouseEnter | `EventCallback<ElementReference>` | Gets or sets the callback invoked when the mouse pointer enters the component's bounds. Commonly used with to display tooltips on hover. Receives the component's ElementReference as a parameter. |
| MouseLeave | `EventCallback<ElementReference>` | Gets or sets the callback invoked when the mouse pointer leaves the component's bounds. Commonly used with to hide tooltips when hover ends. Receives the component's ElementReference as a parameter. |
| SelectedIndexChanged | `EventCallback<int>` | Gets or sets the selected index changed callback. |

## Methods

| Method | Returns | Description |
|--------|---------|-------------|
| AddStep(RadzenStepsItem step) | `void` | Adds the step. |
| NextStep() | `System.Threading.Tasks.Task` | Programmatically navigates to the next visible step in the sequence. If already at the last step, this method does nothing. Respects CanChange validation. |
| PrevStep() | `System.Threading.Tasks.Task` | Programmatically navigates to the previous visible step in the sequence. If already at the first step, this method does nothing. Respects CanChange validation. |
| RemoveStep(RadzenStepsItem item) | `void` | Removes the step. |

