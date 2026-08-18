# Sizing

Sizing styles and utility CSS classes for width and height available in Radzen Blazor Components library.

Keywords: sizing, width, height, size, max, min, utility, css, var

## Examples

## Blazor Sizing

Sizing utility CSS classes for width and height can be used to control the overall layout of elements.

### Width percentage CSS classes

You can use the predefined utility classes for width of 25%, 50%, 75%, 100%.

```razor
<RadzenStack class="rz-text-align-center rz-border-info-light rz-m-0 rz-m-md-12" Gap="1rem">
    <RadzenCard Variant="Variant.Flat" class="rz-w-25 rz-background-color-info-lighter rz-color-on-info-lighter">.rz-w-25</RadzenCard>
    <RadzenCard Variant="Variant.Flat" class="rz-w-50 rz-background-color-info-lighter rz-color-on-info-lighter">.rz-w-50</RadzenCard>
    <RadzenCard Variant="Variant.Flat" class="rz-w-75 rz-background-color-info-lighter rz-color-on-info-lighter">.rz-w-75</RadzenCard>
    <RadzenCard Variant="Variant.Flat" class="rz-w-100 rz-background-color-info-lighter rz-color-on-info-lighter">.rz-w-100</RadzenCard>
    <RadzenCard Variant="Variant.Flat" class="rz-w-auto rz-background-color-info-lighter rz-color-on-info-lighter">.rz-w-auto</RadzenCard>
</RadzenStack>
```


### Width keyword CSS classes

There are width keyword values exposed as CSS classes such as fit-content, min-content, max-content, and stretch.

```razor
<RadzenStack class="rz-text-align-center rz-border-info-light rz-m-0 rz-m-md-12" Gap="1rem" style="overflow-x: scroll;">
    <RadzenCard Variant="Variant.Flat" class="rz-w-fit-content rz-background-color-info-lighter rz-color-on-info-lighter">This container has <strong>.rz-w-fit-content</strong> width class applied.</RadzenCard>
    <RadzenCard Variant="Variant.Flat" class="rz-w-min-content rz-background-color-info-lighter rz-color-on-info-lighter">This container has <strong>.rz-w-min-content</strong> width class applied.</RadzenCard>
    <RadzenCard Variant="Variant.Flat" class="rz-w-max-content rz-background-color-info-lighter rz-color-on-info-lighter">This container has <strong>.rz-w-max-content</strong> width class applied.</RadzenCard>
    <RadzenCard Variant="Variant.Flat" class="rz-w-stretch rz-background-color-info-lighter rz-color-on-info-lighter">This container has <strong>.rz-w-stretch</strong> width class applied.</RadzenCard>
</RadzenStack>
```


### Width viewport CSS classes

These are viewport width values exposed as CSS classes.

```razor
<RadzenStack class="rz-text-align-center rz-border-info-light rz-m-0 rz-m-md-12 rz-overflow-auto" Gap="1rem">
    <RadzenCard Variant="Variant.Flat" class="rz-vw-25 rz-background-color-info-lighter rz-color-on-info-lighter">.rz-vw-25</RadzenCard>
    <RadzenCard Variant="Variant.Flat" class="rz-vw-50 rz-background-color-info-lighter rz-color-on-info-lighter">.rz-vw-50</RadzenCard>
    <RadzenCard Variant="Variant.Flat" class="rz-vw-75 rz-background-color-info-lighter rz-color-on-info-lighter">.rz-vw-75</RadzenCard>
    <RadzenCard Variant="Variant.Flat" class="rz-vw-100 rz-background-color-info-lighter rz-color-on-info-lighter">.rz-vw-100</RadzenCard>
</RadzenStack>
```


### Max-width and min-width CSS classes

Use these CSS classes to set desired minimum and maximum width `class="rz-min-w-100"`.

### Height percentage CSS classes

You can use the predefined utility classes for height of 25%, 50%, 75%, 100%.

```razor
<RadzenStack Orientation="Orientation.Horizontal" Style="height: 200px;" class="rz-text-align-center rz-border-info-light rz-m-0 rz-m-md-12" Gap="1rem">
    <RadzenCard Variant="Variant.Flat" class="rz-h-25 rz-background-color-info-lighter rz-color-on-info-lighter">.rz-h-25</RadzenCard>
    <RadzenCard Variant="Variant.Flat" class="rz-h-50 rz-background-color-info-lighter rz-color-on-info-lighter">.rz-h-50</RadzenCard>
    <RadzenCard Variant="Variant.Flat" class="rz-h-75 rz-background-color-info-lighter rz-color-on-info-lighter">.rz-h-75</RadzenCard>
    <RadzenCard Variant="Variant.Flat" class="rz-h-100 rz-background-color-info-lighter rz-color-on-info-lighter">.rz-h-100</RadzenCard>
    <RadzenCard Variant="Variant.Flat" class="rz-h-auto rz-background-color-info-lighter rz-color-on-info-lighter">.rz-h-auto</RadzenCard>
</RadzenStack>
```


### Height viewport CSS classes

These are viewport height values exposed as CSS classes.

```razor
<RadzenStack Orientation="Orientation.Horizontal" Style="height: 400px;" class="rz-text-align-center rz-border-info-light rz-m-0 rz-m-md-12 rz-overflow-auto" Gap="1rem">
    <RadzenCard Variant="Variant.Flat" class="rz-vh-25 rz-background-color-info-lighter rz-color-on-info-lighter">.rz-vh-25</RadzenCard>
    <RadzenCard Variant="Variant.Flat" class="rz-vh-50 rz-background-color-info-lighter rz-color-on-info-lighter">.rz-vh-50</RadzenCard>
    <RadzenCard Variant="Variant.Flat" class="rz-vh-75 rz-background-color-info-lighter rz-color-on-info-lighter">.rz-vh-75</RadzenCard>
    <RadzenCard Variant="Variant.Flat" class="rz-vh-100 rz-background-color-info-lighter rz-color-on-info-lighter">.rz-vh-100</RadzenCard>
</RadzenStack>
```


### Max-height and min-height CSS classes

Use these CSS classes to set desired minimum and maximum height `class="rz-min-h-100"`.

### Responsive sizing

You can set a specific size value for different screen sizes by inserting the respective breakpoint abbreviation.
For example `.rz-w-{breakpoint}-100`, where `{breakpoint}` can be `xs`, `sm`, `md`, `lg`, `xl`, `xx`.
Learn more about [Breakpoints](/breakpoints).
