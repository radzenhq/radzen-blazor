# Overflow

Overflow styles and utility CSS classes available in Radzen Blazor Components library.

Keywords: overflow, content, width, height, size, wrap, hide, hidden, visible, utility, css, var

## Examples

## Blazor Overflow

Overflow utility CSS classes can be used to control the overflow of content within elements.

```razor
<RadzenStack Orientation="Orientation.Horizontal" Style="height: 100px;" class="rz-text-align-center rz-border-info-light rz-m-0 rz-m-md-12" Gap="1rem">
    <RadzenCard Variant="Variant.Flat" class="rz-overflow-auto rz-h-50 rz-background-color-info-lighter rz-color-on-info-lighter">This container has <strong>.rz-overflow-auto</strong> overflow class applied.</RadzenCard>
    <RadzenCard Variant="Variant.Flat" class="rz-overflow-scroll rz-h-50 rz-background-color-info-lighter rz-color-on-info-lighter">This container has <strong>.rz-overflow-scroll</strong> overflow class applied.</RadzenCard>
    <RadzenCard Variant="Variant.Flat" class="rz-overflow-hidden rz-h-50 rz-background-color-info-lighter rz-color-on-info-lighter">This container has <strong>.rz-overflow-hidden</strong> overflow class applied.</RadzenCard>
    <RadzenCard Variant="Variant.Flat" class="rz-overflow-visible rz-h-50 rz-background-color-info-lighter rz-color-on-info-lighter">This container has <strong>.rz-overflow-visible</strong> overflow class applied.</RadzenCard>
</RadzenStack>
```

If you need to specify how the text content should wrap, use `rz-text-wrap`, `rz-text-nowrap`, and `rz-text-truncate`. See the [Text Wrap demo](/typography#text-wrap).

### Responsive overflow

You can set a specific overflow value for different screen sizes by inserting the respective breakpoint abbreviation.
For example `.rz-overflow-{breakpoint}-scroll`, where `{breakpoint}` can be `xs`, `sm`, `md`, `lg`, `xl`, `xx`.
Learn more about [Breakpoints](/breakpoints).
