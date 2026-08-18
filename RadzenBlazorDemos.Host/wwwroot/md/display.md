# Display

Display styles and utility CSS classes available in Radzen Blazor Components library.

Keywords: display, hide, show, flex, block, inline, utility, css, var

## Examples

## Blazor Display

Display utility CSS classes can be used to control the display value of UI elements and components.
`.rz-display-{value}`, where `{value}` can be:
`none` for hiding an element
`block` to treat an element as a block box
`inline` to treat an element as an inline box
`inline-block` to have a block box surrounded by elements as if it were an inline box
`flex` to have a block box that arranges its content according to the flexbox model
`inline-flex` to have an inline box that arranges its content according to the flexbox model
`grid` to have a block box that arranges its content according to the grid model
`inline-grid` to have an inline box that arranges its content according to the grid model

### Responsive display

You can set a specific display value for different screen sizes by inserting the respective breakpoint abbreviation.
For example `.rz-display-{breakpoint}-block`, where `{breakpoint}` can be `xs`, `sm`, `md`, `lg`, `xl`, `xx`.
Learn more about [Breakpoints](/breakpoints).
