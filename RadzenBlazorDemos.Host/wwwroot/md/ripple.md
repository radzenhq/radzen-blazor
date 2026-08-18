# Ripple

See how to apply the ripple effect to various UI elements.

Keywords: ripple, utility, css, var

## Examples

## Blazor Ripple

Apply the ripple effect to various UI elements.

### Ripple RadzenButton

Ripple effect applied to RadzenButton via `class="rz-ripple"`. Material theme buttons have the ripple effect applied by default.

```razor
<div class="rz-p-12 rz-text-align-center">
    <RadzenButton Text="Click me!" class="rz-ripple" />
</div>
```


### Ripple RadzenLink

Ripple effect applied to RadzenLink via `class="rz-ripple"`.

```razor
<div class="rz-p-12 rz-text-align-center">
    <RadzenLink Text="Click me!" class="rz-ripple" />
</div>
```


### Ripple HTML div

Ripple effect applied to a simple `&lt;div&gt;` element via `class="rz-ripple"`. The ripple color inherits the element's text color.

```razor
<div class="rz-color-success-dark rz-ripple rz-text-align-center rz-p-12">
    Click me!
</div>
```
