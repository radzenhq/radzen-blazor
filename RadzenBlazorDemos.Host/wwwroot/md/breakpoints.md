# Breakpoints

Responsive breakpoints are used to adjust the layout based on the screen size of the device in use.

Keywords: breakpoints, spacing, margin, padding, gutter, gap, utility, css, responsive, layout

## Examples

## Responsive Breakpoints

Responsive breakpoints are used to adjust the layout based on the screen size of the device in use. They allow a single layout to adapt to different screen sizes, from small mobile phone screens to large desktop monitors.

### Breakpoints

A breakpoint is a specific screen width at which the layout of an app or web page will change. For example, a breakpoint with min-width of 576px means that when the screen width is 576 pixels or more, the layout will change to efficiently use the screen real estate.
Here is a list of the predefined breakpoints and their abbreviations in Radzen Blazor Components, optimized for a wide range of devices.
`xs` - Extra Small ≥ 576px
`sm` - Small ≥ 768px
`md` - Medium ≥ 1024px
`lg` - Large ≥ 1280px
`xl` - Extra Large ≥ 1920px
`xx` - Extra Extra Large ≥ 2560px

### Usage of Breakpoints


#### Breakpoints in Layouts

Breakpoints are primarily used in layouts. For example, you can define the width of a `RadzenColumn` component by setting the respective `Size` property. E.g. `SizeMD="6"`
See [Column](/column) demos to learn how to adjust your page layouts using breakpoints.

#### Breakpoints and Display Modes

Breakpoints are available with CSS utility classes for `display`. For example, you can define the display mode of an element by setting the respective `.rz-display-{breakpoint}-{value}` CSS class. E.g. `.rz-display-md-flex`
See [Display](/display) demo to learn how to use breakpoints with display.

#### Breakpoints and Content Overflow

Breakpoints are available with CSS utility classes for `overflow`. For example, you can define the overflow of content within an element by setting the respective `.rz-overflow-{breakpoint}-{value}` CSS class. E.g. `.rz-overflow-md-scroll`
See [Overflow](/overflow) demo to learn how to use breakpoints with display.

#### Breakpoints in Sizing

Breakpoints are available with CSS utility classes for `width` and `height`. For example, you can define the width of an element by setting the respective `.rz-w-{breakpoint}-{size}` CSS class. E.g. `.rz-w-md-100`
See [Sizing](/sizing) demos to learn how to use breakpoints with widths and heights.

#### Breakpoints in Spacing

Breakpoints can also be used with the Spacing CSS utility classes for `margin` and `padding`. For example, you can define the margin of an element by setting the respective `.rz-m-{breakpoint}-{size}` CSS class. E.g. `.rz-m-md-1`
See [Spacing](/spacing) demos to learn how to use breakpoints with margins and paddings.
