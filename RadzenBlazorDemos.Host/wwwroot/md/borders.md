# Borders

Border styles and utility CSS classes for borders available in Radzen Blazor Components library.

Keywords: border, utility, css, var

## Examples

## Blazor Borders

Border styles and utility CSS classes.

### Border radius

Use these CSS classes to set border-radius to an element e.g. `class="rz-border-radius-6"`.

```razor
<RadzenStack Orientation="Orientation.Horizontal" Wrap="FlexWrap.Wrap" JustifyContent="JustifyContent.Center" class="rz-p-12">
    <RadzenButton Text=".rz-border-radius (theme's default)" class="rz-border-radius" />
    <RadzenButton Text=".rz-border-radius-6" class="rz-border-radius-6" />
</RadzenStack>
```


### Add or remove borders arbitrarily

The following CSS classes help you add or remove borders. For example `class="rz-border-right"` adds a solid border with the theme's base color to the right side of an element. Use `rz-{border-side}-0` to remove a border.

```razor
<RadzenStack Orientation="Orientation.Vertical" class="rz-m-0 rz-m-md-12" Style="--rz-card-border-radius: var(--rz-border-radius-0)">
    <RadzenStack Orientation="Orientation.Horizontal" Wrap="FlexWrap.Wrap">
        <RadzenCard Variant="Variant.Text" class="rz-border"><strong>.rz-border</strong></RadzenCard>
        <RadzenCard Variant="Variant.Text" class="rz-border-left"><strong>.rz-border-left</strong></RadzenCard>
        <RadzenCard Variant="Variant.Text" class="rz-border-right"><strong>.rz-border-right</strong></RadzenCard>
        <RadzenCard Variant="Variant.Text" class="rz-border-start"><strong>.rz-border-start</strong></RadzenCard>
        <RadzenCard Variant="Variant.Text" class="rz-border-end"><strong>.rz-border-end</strong></RadzenCard>
        <RadzenCard Variant="Variant.Text" class="rz-border-top"><strong>.rz-border-top</strong></RadzenCard>
        <RadzenCard Variant="Variant.Text" class="rz-border-bottom"><strong>.rz-border-bottom</strong></RadzenCard>
    </RadzenStack>
    <RadzenStack Orientation="Orientation.Horizontal" Wrap="FlexWrap.Wrap">
        <RadzenCard Variant="Variant.Outlined" class="rz-border-0"><strong>.rz-border-0</strong></RadzenCard>
        <RadzenCard Variant="Variant.Outlined" class="rz-border-left-0"><strong>.rz-border-left-0</strong></RadzenCard>
        <RadzenCard Variant="Variant.Outlined" class="rz-border-right-0"><strong>.rz-border-right-0</strong></RadzenCard>
        <RadzenCard Variant="Variant.Outlined" class="rz-border-start-0"><strong>.rz-border-start-0</strong></RadzenCard>
        <RadzenCard Variant="Variant.Outlined" class="rz-border-end-0"><strong>.rz-border-end-0</strong></RadzenCard>
        <RadzenCard Variant="Variant.Outlined" class="rz-border-top-0"><strong>.rz-border-top-0</strong></RadzenCard>
        <RadzenCard Variant="Variant.Outlined" class="rz-border-bottom-0"><strong>.rz-border-bottom-0</strong></RadzenCard>
    </RadzenStack>
</RadzenStack>
```


### Border color utility CSS classes

The following CSS classes help you add color to a border. For example `class="rz-border-right rz-border-color-success"` adds a solid border with the theme's success color to the right side of an element.

```razor
<RadzenStack Orientation="Orientation.Vertical" class="rz-m-0 rz-m-md-12" Style="--rz-card-border-radius: var(--rz-border-radius-0)">
    <RadzenStack Orientation="Orientation.Horizontal" Wrap="FlexWrap.Wrap">
        <RadzenCard Variant="Variant.Text" class="rz-border rz-border-color-success"><strong>.rz-border</strong></RadzenCard>
        <RadzenCard Variant="Variant.Text" class="rz-border-left rz-border-color-success"><strong>.rz-border-left</strong></RadzenCard>
        <RadzenCard Variant="Variant.Text" class="rz-border-right rz-border-color-success"><strong>.rz-border-right</strong></RadzenCard>
        <RadzenCard Variant="Variant.Text" class="rz-border-start rz-border-color-success"><strong>.rz-border-start</strong></RadzenCard>
        <RadzenCard Variant="Variant.Text" class="rz-border-end rz-border-color-success"><strong>.rz-border-end</strong></RadzenCard>
        <RadzenCard Variant="Variant.Text" class="rz-border-top rz-border-color-success"><strong>.rz-border-top</strong></RadzenCard>
        <RadzenCard Variant="Variant.Text" class="rz-border-bottom rz-border-color-success"><strong>.rz-border-bottom</strong></RadzenCard>
    </RadzenStack>
</RadzenStack>
```


### Border with color utility CSS classes

The following CSS classes add a border with its respective color on all sides of an element. E.g. `class="rz-border-primary"`. You can think of it as the shorthand of `class="rz-border rz-border-color-primary"`

```razor
<RadzenRow class="borders rz-m-0 rz-m-md-12">
    <RadzenColumn Size="12" SizeMD="3">
        <div class="rz-border-white"><span>.rz-border-white</span></div>
        <div class="rz-border-base-50"><span>.rz-border-base-50</span></div>
        <div class="rz-border-base-100"><span>.rz-border-base-100</span></div>
        <div class="rz-border-base-200"><span>.rz-border-base-200</span></div>
        <div class="rz-border-base-300"><span>.rz-border-base-300</span></div>
        <div class="rz-border-base-400"><span>.rz-border-base-400</span></div>
        <div class="rz-border-base-500"><span>.rz-border-base-500</span></div>
        <div class="rz-border-base-600"><span>.rz-border-base-600</span></div>
        <div class="rz-border-base-700"><span>.rz-border-base-700</span></div>
        <div class="rz-border-base-800"><span>.rz-border-base-800</span></div>
        <div class="rz-border-base-900"><span>.rz-border-base-900</span></div>
        <div class="rz-border-black"><span>.rz-border-black</span></div>
    </RadzenColumn>
    <RadzenColumn Size="12" SizeMD="3">
        <div class="rz-border-base-lighter"><span>.rz-border-primary-lighter</span></div>
        <div class="rz-border-base-light"><span>.rz-border-primary-light</span></div>
        <div class="rz-border-base"><span>.rz-border-primary</span></div>
        <div class="rz-border-base-dark"><span>.rz-border-primary-dark</span></div>
        <div class="rz-border-base-darker"><span>.rz-border-primary-darker</span></div>
    </RadzenColumn>
    <RadzenColumn Size="12" SizeMD="3">
        <div class="rz-border-primary-lighter"><span>.rz-border-primary-lighter</span></div>
        <div class="rz-border-primary-light"><span>.rz-border-primary-light</span></div>
        <div class="rz-border-primary"><span>.rz-border-primary</span></div>
        <div class="rz-border-primary-dark"><span>.rz-border-primary-dark</span></div>
        <div class="rz-border-primary-darker"><span>.rz-border-primary-darker</span></div>
    </RadzenColumn>
    <RadzenColumn Size="12" SizeMD="3">
        <div class="rz-border-secondary-lighter"><span>.rz-border-secondary-lighter</span></div>
        <div class="rz-border-secondary-light"><span>.rz-border-secondary-light</span></div>
        <div class="rz-border-secondary"><span>.rz-border-secondary</span></div>
        <div class="rz-border-secondary-dark"><span>.rz-border-secondary-dark</span></div>
        <div class="rz-border-secondary-darker"><span>.rz-border-secondary-darker</span></div>
    </RadzenColumn>
    <RadzenColumn Size="12" SizeMD="3">
        <div class="rz-border-info-lighter"><span>.rz-border-info-lighter</span></div>
        <div class="rz-border-info-light"><span>.rz-border-info-light</span></div>
        <div class="rz-border-info"><span>.rz-border-info</span></div>
        <div class="rz-border-info-dark"><span>.rz-border-info-dark</span></div>
        <div class="rz-border-info-darker"><span>.rz-border-info-darker</span></div>
    </RadzenColumn>
    <RadzenColumn Size="12" SizeMD="3">
        <div class="rz-border-success-lighter"><span>.rz-border-success-lighter</span></div>
        <div class="rz-border-success-light"><span>.rz-border-success-light</span></div>
        <div class="rz-border-success"><span>.rz-border-success</span></div>
        <div class="rz-border-success-dark"><span>.rz-border-success-dark</span></div>
        <div class="rz-border-success-darker"><span>.rz-border-success-darker</span></div>
    </RadzenColumn>
    <RadzenColumn Size="12" SizeMD="3">
        <div class="rz-border-warning-lighter"><span>.rz-border-warning-lighter</span></div>
        <div class="rz-border-warning-light"><span>.rz-border-warning-light</span></div>
        <div class="rz-border-warning"><span>.rz-border-warning</span></div>
        <div class="rz-border-warning-dark"><span>.rz-border-warning-dark</span></div>
        <div class="rz-border-warning-darker"><span>.rz-border-warning-darker</span></div>
    </RadzenColumn>
    <RadzenColumn Size="12" SizeMD="3">
        <div class="rz-border-danger-lighter"><span>.rz-border-danger-lighter</span></div>
        <div class="rz-border-danger-light"><span>.rz-border-danger-light</span></div>
        <div class="rz-border-danger"><span>.rz-border-danger</span></div>
        <div class="rz-border-danger-dark"><span>.rz-border-danger-dark</span></div>
        <div class="rz-border-danger-darker"><span>.rz-border-danger-darker</span></div>
    </RadzenColumn>
</RadzenRow>

<style>
    .borders div div {
        margin: 1rem 0;
        padding: 0.5rem;
    }
</style>
```


### Set border width via CSS variable

Use `--rz-border-width` CSS variable to set the width of a border.

```razor
<RadzenStack Orientation="Orientation.Vertical" class="rz-m-0 rz-m-md-12" Gap="0">
    <RadzenText TextStyle="TextStyle.Body1" class="rz-mb-8">
        Apply to a single element:
    </RadzenText>
    <RadzenStack Orientation="Orientation.Horizontal" Wrap="FlexWrap.Wrap" Gap="1rem">
        <RadzenCard Variant="Variant.Flat" class="rz-border rz-border-color-warning" Style="--rz-border-width: 10px;"><strong>.rz-border</strong></RadzenCard>
    </RadzenStack>
    <RadzenText TextStyle="TextStyle.Body1" class="rz-mt-12 rz-mb-8">
        Apply to a group of elements:
    </RadzenText>
    <RadzenStack Style="--rz-border-width: 10px;" Orientation="Orientation.Horizontal" Wrap="FlexWrap.Wrap" Gap="1rem">
        <RadzenCard Variant="Variant.Flat" class="rz-border rz-border-color-info"><strong>.rz-border</strong></RadzenCard>
        <RadzenCard Variant="Variant.Flat" class="rz-border-left rz-border-color-info"><strong>.rz-border-left</strong></RadzenCard>
        <RadzenCard Variant="Variant.Flat" class="rz-border-right rz-border-color-info"><strong>.rz-border-right</strong></RadzenCard>
        <RadzenCard Variant="Variant.Flat" class="rz-border-start rz-border-color-info"><strong>.rz-border-start</strong></RadzenCard>
        <RadzenCard Variant="Variant.Flat" class="rz-border-end rz-border-color-info"><strong>.rz-border-end</strong></RadzenCard>
        <RadzenCard Variant="Variant.Flat" class="rz-border-top rz-border-color-info"><strong>.rz-border-top</strong></RadzenCard>
        <RadzenCard Variant="Variant.Flat" class="rz-border-bottom rz-border-color-info"><strong>.rz-border-bottom</strong></RadzenCard>
    </RadzenStack>
</RadzenStack>
```


### Borders with CSS variables

Use theme color variables when defining borders. [See theme colors](/colors)
