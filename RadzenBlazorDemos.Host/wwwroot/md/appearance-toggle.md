# AppearanceToggle

The AppearanceToggle button allows you to switch between two predefined themes, most commonly light and dark.

Keywords: theme, light, dark, mode, appearance, toggle, switch

> API reference: [RadzenAppearanceToggle API](https://blazor.radzen.com/api/appearancetoggle.md)

## Examples

## Blazor AppearanceToggle

A toggle button for switching application appearance between two preset themes, such as light and dark.

### Switch between light and dark mode

Use RadzenAppearanceToggle to switch between light and dark modes. Requires RadzenTheme (check the [getting started instructions](/get-started)).

```razor
<RadzenStack Orientation="Orientation.Vertical" AlignItems="AlignItems.Center" JustifyContent="JustifyContent.Center" Gap="2rem" Wrap="FlexWrap.Wrap" class="rz-p-12">
    <RadzenAppearanceToggle />
</RadzenStack>
```
