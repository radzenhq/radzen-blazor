# RadzenLogin API Reference

## Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| AllowRegister | `bool` | Gets or sets a value indicating whether register is allowed. |
| AllowRememberMe | `bool` | Asks the user whether to remember their credentials. Set to false by default. |
| AllowResetPassword | `bool` | Gets or sets a value indicating whether reset password is allowed. |
| Attributes | `IReadOnlyDictionary<string, object>?` | Gets or sets a dictionary of additional HTML attributes that will be applied to the component's root element. Any attributes not explicitly defined as parameters will be captured here and rendered on the element. Use this to add data-* attributes, ARIA attributes, or any custom HTML attributes. |
| AutoComplete | `bool` | Gets or sets a value indicating whether automatic complete of inputs is enabled. |
| Culture | `CultureInfo` | Gets or sets the culture used for formatting and parsing localizable data (numbers, dates, currency). If not set, uses the from a parent component or falls back to . |
| FormFieldVariant | `Variant?` | Gets or sets the design variant of the form field. |
| LoginText | `string` | Gets or sets the login text. |
| Password | `string?` | Gets or sets the password. |
| PasswordAutoCompleteType | `AutoCompleteType` | Gets or sets a value indicating the type of built-in autocomplete the browser should use. |
| PasswordRequired | `string` | Gets or sets the password required. |
| PasswordText | `string` | Gets or sets the password text. |
| RegisterMessageText | `string` | Gets or sets the register message text. |
| RegisterText | `string` | Gets or sets the register text. |
| RememberMe | `bool` | Sets the initial value of the remember me switch. |
| RememberMeText | `string` | Gets or sets the remember me text. |
| ResetPasswordText | `string` | Gets or sets the reset password text. |
| ShowLoginButton | `bool` | Gets or sets a value indicating whether default login button is shown. |
| Style | `string?` | Gets or sets the inline CSS style. |
| UICulture | `CultureInfo` | Gets or sets the culture used for localized UI strings. If not set, uses the from a parent component or falls back to . |
| UserNameAutoCompleteType | `AutoCompleteType` | Gets or sets a value indicating the type of built-in autocomplete the browser should use. |
| UserRequired | `string` | Gets or sets the user required text. |
| UserText | `string` | Gets or sets the user text. |
| Username | `string?` | Gets or sets the username. |
| Visible | `bool` | Gets or sets a value indicating whether this is visible. Invisible components are not rendered. |

## Events

| Event | Type | Description |
|-------|------|-------------|
| ContextMenu | `EventCallback<Microsoft.AspNetCore.Components.Web.MouseEventArgs>` | Gets or sets the callback invoked when the user right-clicks the component. Commonly used with to display context menus. Receives mouse event arguments containing click position. |
| Login | `EventCallback<LoginArgs>` | Gets or sets the login callback. |
| MouseEnter | `EventCallback<ElementReference>` | Gets or sets the callback invoked when the mouse pointer enters the component's bounds. Commonly used with to display tooltips on hover. Receives the component's ElementReference as a parameter. |
| MouseLeave | `EventCallback<ElementReference>` | Gets or sets the callback invoked when the mouse pointer leaves the component's bounds. Commonly used with to hide tooltips when hover ends. Receives the component's ElementReference as a parameter. |
| Register | `EventCallback` | Gets or sets the register callback. |
| ResetPassword | `EventCallback<string>` | Gets or sets the reset password callback. |

