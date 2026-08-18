# Shadows

Shadow styles and utility CSS classes for shadows available in Radzen Blazor Components library.

Keywords: shadow, utility, css, var

## Examples

## Blazor Shadows

Shadow styles and utility CSS classes.

### Utility CSS classes

Use these CSS classes to set box-shadow to an element e.g. `class="rz-shadow-2"`. Each theme has its own shadows assigned. Change the theme to preview them.

```razor
<RadzenStack Orientation="Orientation.Horizontal" Wrap="FlexWrap.Wrap" JustifyContent="JustifyContent.Center" class="shadows rz-p-12" Gap="3rem">
    <div class="rz-shadow-0"><span>.rz-shadow-0</span></div>
    <div class="rz-shadow-1"><span>.rz-shadow-1</span></div>
    <div class="rz-shadow-2"><span>.rz-shadow-2</span></div>
    <div class="rz-shadow-3"><span>.rz-shadow-3</span></div>
    <div class="rz-shadow-4"><span>.rz-shadow-4</span></div>
    <div class="rz-shadow-5"><span>.rz-shadow-5</span></div>
    <div class="rz-shadow-6"><span>.rz-shadow-6</span></div>
    <div class="rz-shadow-7"><span>.rz-shadow-7</span></div>
    <div class="rz-shadow-8"><span>.rz-shadow-8</span></div>
    <div class="rz-shadow-9"><span>.rz-shadow-9</span></div>
    <div class="rz-shadow-10"><span>.rz-shadow-10</span></div>
</RadzenStack>
<style>
    .shadows div {
        padding: 1rem;
        width: 10rem;
        text-align: center;
    }
</style>
```


### Custom CSS properties (CSS Variables)

These are the root theme shadow CSS variables.
You can use CSS variables in styles e.g. `style="box-shadow: var(--rz-shadow-2);"`

```razor
<RadzenStack Orientation="Orientation.Horizontal" Wrap="FlexWrap.Wrap" JustifyContent="JustifyContent.Center" class="shadows rz-p-12" Gap="3rem">
    <div style="box-shadow: var(--rz-shadow-0);"><span>--rz-shadow-0</span></div>
    <div style="box-shadow: var(--rz-shadow-1);"><span>--rz-shadow-1</span></div>
    <div style="box-shadow: var(--rz-shadow-2);"><span>--rz-shadow-2</span></div>
    <div style="box-shadow: var(--rz-shadow-3);"><span>--rz-shadow-3</span></div>
    <div style="box-shadow: var(--rz-shadow-4);"><span>--rz-shadow-4</span></div>
    <div style="box-shadow: var(--rz-shadow-5);"><span>--rz-shadow-5</span></div>
    <div style="box-shadow: var(--rz-shadow-6);"><span>--rz-shadow-6</span></div>
    <div style="box-shadow: var(--rz-shadow-7);"><span>--rz-shadow-7</span></div>
    <div style="box-shadow: var(--rz-shadow-8);"><span>--rz-shadow-8</span></div>
    <div style="box-shadow: var(--rz-shadow-9);"><span>--rz-shadow-9</span></div>
    <div style="box-shadow: var(--rz-shadow-10);"><span>--rz-shadow-10</span></div>
</RadzenStack>
<style>
    .shadows div {
        padding: 1rem;
        width: 10rem;
        text-align: center;
    }
</style>
```
