# Spacing

Spacing styles and utility CSS classes for margin and padding available in Radzen Blazor Components library.

Keywords: spacing, margin, padding, gutter, gap, utility, css, var

## Examples

## Blazor Spacing

Spacing utility CSS classes for margin and padding can be used in combination to control the overall layout of elements.

### Margin CSS classes

Margin refers to the space outside of an element's border. It can be used to create space between elements. Use these CSS classes to set a margin to an element without writing any additional custom styles.

#### Margin on all sides

`.rz-m-{size}`, where `{size}` is from `0` to `12`, or `auto`.

#### Margin on a specific side

`.rz-mx-{size}` for both left and right margins
`.rz-my-{size}` for both top and bottom margins
`.rz-mt-{size}` for top margin
`.rz-mr-{size}` for right margin
`.rz-mb-{size}` for bottom margin
`.rz-ml-{size}` for left margin
`.rz-ms-{size}` for logical inline start margin
`.rz-me-{size}` for logical inline end margin

### Padding CSS classes

Padding refers to the space inside of an element's border. It can be used to create space between an element's content and its border. Use these CSS classes to set a padding to an element without writing any additional custom styles.

#### Padding on all sides

`.rz-p-{size}`, where {size} is from `0` to `12`.

#### Padding on a specific side

`.rz-px-{size}` for both left and right paddings
`.rz-py-{size}` for both top and bottom paddings
`.rz-pt-{size}` for top padding
`.rz-pr-{size}` for right padding
`.rz-pb-{size}` for bottom padding
`.rz-pl-{size}` for left padding
`.rz-ps-{size}` for logical inline start padding
`.rz-pe-{size}` for logical inline end padding

### Sizes

Each one of the padding/margin sizes is mapped to a specific value in pixels with a step of `4px`. E.g. `.rz-mt-3` = 3*4px = 12px.
`0` - 0px
`05` - 2px
`1` - 4px
`2` - 8px
`3` - 12px
`4` - 16px
`5` - 20px
`6` - 24px
`7` - 28px
`8` - 32px
`9` - 36px
`10` - 40px
`11` - 44px
`12` - 48px
`auto` - A suitable margin, automatically selected by the browser

### Responsive spacing

You can set a specific spacing for different screen sizes by inserting the respective breakpoint abbreviation.
For example `.rz-m-{breakpoint}-1`, where `{breakpoint}` can be `xs`, `sm`, `md`, `lg`, `xl`, `xx`.
Learn more about [Breakpoints](/breakpoints).
