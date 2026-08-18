# Typography

Use the RadzenText component to format text in your applications. The TextStyle property applies a predefined text style such as H1, H2, etc.

Keywords: typo, typography, text, paragraph, header, heading, caption, overline, content

## Examples

## Blazor Text

Format and style text in your application with predefined text styles.

### Text Style

Use the `TextStyle` property to apply a predefined text style.

```razor
<div class="rz-p-12">
    <RadzenText TextStyle="TextStyle.H1" TagName="TagName.P">Radzen Heading 1</RadzenText>
    <RadzenText TextStyle="TextStyle.H2" TagName="TagName.P">Radzen Heading 2</RadzenText>
    <RadzenText TextStyle="TextStyle.H3" TagName="TagName.P">Radzen Heading 3</RadzenText>
    <RadzenText TextStyle="TextStyle.H4" TagName="TagName.P">Radzen Heading 4</RadzenText>
    <RadzenText TextStyle="TextStyle.H5" TagName="TagName.P">Radzen Heading 5</RadzenText>
    <RadzenText TextStyle="TextStyle.H6" TagName="TagName.P">Radzen Heading 6</RadzenText>
    <RadzenText TextStyle="TextStyle.Subtitle1" TagName="TagName.P">Radzen Subtitle 1</RadzenText>
    <RadzenText TextStyle="TextStyle.Subtitle2" TagName="TagName.P">Radzen Subtitle 2</RadzenText>
    <RadzenText TextStyle="TextStyle.Body1">
        <strong>Radzen Body 1</strong> Radzen Blazor Components are open source and free for commercial use. You can install them from nuget or build your own copy from source.
    </RadzenText>
    <RadzenText TextStyle="TextStyle.Body2">
        <strong>Radzen Body 2</strong> Radzen Blazor Components are open source and free for commercial use. You can install them from nuget or build your own copy from source.
    </RadzenText>
    <RadzenText TextStyle="TextStyle.Button">Radzen Button</RadzenText><br />
    <RadzenText TextStyle="TextStyle.Caption">Radzen Caption</RadzenText><br />
    <RadzenText TextStyle="TextStyle.Overline">Radzen Overline</RadzenText><br />
</div>
```


### Text Style and Tag Name

You can use `TextStyle` together with `TagName` to apply different styling while keeping the code semantically correct.
`TextStyle` controls only the visual style of the text, while `TagName` controls the HTML element that is rendered. Set `TagName` so that the document keeps a correct heading hierarchy as required by WCAG 1.3.1.

```razor
<div class="rz-p-12">
    <RadzenText TextStyle="TextStyle.H3" TagName="TagName.P">This is a paragraph styled as H3</RadzenText>
</div>
```


### Display headings

Use display headings to emphasise a text or page title. Display headings are usually larger than traditional headings.

```razor
<div class="rz-p-12">
    <RadzenText TextStyle="TextStyle.DisplayH1" TagName="TagName.P">Radzen Display 1</RadzenText>
    <RadzenText TextStyle="TextStyle.DisplayH2" TagName="TagName.P">Radzen Display 2</RadzenText>
    <RadzenText TextStyle="TextStyle.DisplayH3" TagName="TagName.P">Radzen Display 3</RadzenText>
    <RadzenText TextStyle="TextStyle.DisplayH4" TagName="TagName.P">Radzen Display 4</RadzenText>
    <RadzenText TextStyle="TextStyle.DisplayH5" TagName="TagName.P">Radzen Display 5</RadzenText>
    <RadzenText TextStyle="TextStyle.DisplayH6" TagName="TagName.P">Radzen Display 6</RadzenText>
</div>
```


### Text Align

You can use `TextAlign` to align your text.

```razor
<RadzenStack Orientation="Orientation.Vertical" Gap="3rem" class="rz-p-12">
    <RadzenText TextStyle="TextStyle.Body1" TextAlign="TextAlign.Center"><strong>TextAlign.Center</strong><br /> Radzen Blazor Components are open source and free for commercial use. You can install them from nuget or build your own copy from source.</RadzenText>
    <RadzenText TextStyle="TextStyle.Body1" TextAlign="TextAlign.Left"><strong>TextAlign.Left</strong><br />  Radzen Blazor Components are open source and free for commercial use. You can install them from nuget or build your own copy from source.</RadzenText>
    <RadzenText TextStyle="TextStyle.Body1" TextAlign="TextAlign.Right"><strong>TextAlign.Right</strong><br />  Radzen Blazor Components are open source and free for commercial use. You can install them from nuget or build your own copy from source.</RadzenText>
    <RadzenText TextStyle="TextStyle.Body1" TextAlign="TextAlign.Start"><strong>TextAlign.Start</strong><br />  Radzen Blazor Components are open source and free for commercial use. You can install them from nuget or build your own copy from source.</RadzenText>
    <RadzenText TextStyle="TextStyle.Body1" TextAlign="TextAlign.End"><strong>TextAlign.End</strong><br />  Radzen Blazor Components are open source and free for commercial use. You can install them from nuget or build your own copy from source.</RadzenText>
    <RadzenText TextStyle="TextStyle.Body1" TextAlign="TextAlign.Justify"><strong>TextAlign.Justify</strong><br />  Radzen Blazor Components are open source and free for commercial use. You can install them from nuget or build your own copy from source.</RadzenText>
</RadzenStack>
```


### Text Functional Colors

These are the theme's text color CSS variables. Each theme has its own text color values assigned. Change the theme to preview them. You can use CSS variables in styles e.g. `style="color: var(--rz-text-secondary-color);"`

```razor
<RadzenStack Orientation="Orientation.Horizontal" JustifyContent="JustifyContent.Center" AlignItems="AlignItems.Center" Gap="3rem" class="rz-p-12">
    <span style="color: var(--rz-text-color)">--rz-text-color</span>
    <span style="color: var(--rz-text-secondary-color)">--rz-text-secondary-color</span>
    <span style="color: var(--rz-text-tertiary-color)">--rz-text-tertiary-color</span>
    <span style="color: var(--rz-text-disabled-color)">--rz-text-disabled-color</span>
    <div class="rz-background-color-black rz-p-4">
        <span style="color: var(--rz-text-contrast-color)">--rz-text-contrast-color</span>
    </div>
</RadzenStack>
```


### Text Transform

Use CSS classes to capitalize text. E.g. `class="rz-text-uppercase"`.

```razor
<RadzenStack Orientation="Orientation.Horizontal" JustifyContent="JustifyContent.Center" AlignItems="AlignItems.Center" Gap="3rem" class="rz-p-12">
    <div class="rz-text-capitalize">This is capitalized</div>
    <div class="rz-text-uppercase">This is uppercase</div>
    <div class="rz-text-lowercase">This is lowercase</div>
</RadzenStack>
```


### Text Wrap

Use `rz-text-wrap`, `rz-text-nowrap`, and `rz-text-truncate` CSS classes to specify how the text content should wrap. E.g. `class="rz-text-truncate"`.

```razor
<RadzenStack Orientation="Orientation.Vertical" JustifyContent="JustifyContent.Center" AlignItems="AlignItems.Center" Gap="2rem" class="rz-p-12">
    <div class="rz-text-wrap" style="width: 100px; background-color: rgba(0,0,0,.2)">This text wraps.</div>
    <div class="rz-text-nowrap" style="width: 100px; background-color: rgba(0,0,0,.2)">This text does not wrap.</div>
    <div class="rz-text-truncate" style="width: 100px; background-color: rgba(0,0,0,.2)">This text is truncated.</div>
</RadzenStack>
```
