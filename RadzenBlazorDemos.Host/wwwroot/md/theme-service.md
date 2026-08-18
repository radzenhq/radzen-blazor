# ThemeService

The ThemeService allows to change the theme of the application at runtime.

Keywords: theme, service, change, runtime, rtl, right to left, direction, wcag, accessibility

## Examples

## Blazor ThemeService

Change the theme of your application at runtime with dynamic theme switching.

```razor
<RadzenStack class="rz-p-0 rz-p-md-6 rz-p-lg-12">
    <RadzenCard Variant="Variant.Outlined">
        <RadzenStack Orientation="Orientation.Horizontal" Wrap="FlexWrap.Wrap">
            <RadzenStack Orientation="Orientation.Vertical" Gap="0.5rem" class="rz-p-sm-2">
                <RadzenLabel Text="Change the current theme" />
                <RadzenDropDown Name="ThemeDropDown" TValue="string" Value="@ThemeService.Theme" ValueChanged="@ChangeTheme" Data="@Themes.All" TextProperty=@nameof(Theme.Text) ValueProperty=@nameof(Theme.Value)>
                </RadzenDropDown>
            </RadzenStack>
            <RadzenStack Orientation="Orientation.Vertical" Gap="0.5rem" class="rz-p-sm-2">
                <RadzenLabel Text="Right-to-left" />
                <RadzenSwitch Value=@(ThemeService.RightToLeft == true) ValueChanged=@ChangeRightToLeft />
            </RadzenStack>
            <RadzenStack Orientation="Orientation.Vertical" Gap="0.5rem" class="rz-p-sm-2">
                <RadzenLabel Text="WCAG compliant colors" />
                <RadzenSwitch Value="@(ThemeService.Wcag == true)" Name="WCAG" ValueChanged=@ChangeWcag />
            </RadzenStack>
        </RadzenStack>
    </RadzenCard>
    <EventConsole @ref=@console />
</RadzenStack>
@code {
    EventConsole console;

    void ChangeTheme(string value)
    {
        ThemeService.SetTheme(value);
    }

    void ChangeRightToLeft(bool value)
    {
        ThemeService.SetRightToLeft(value);
    }

    void ChangeWcag(bool value)
    {
        ThemeService.SetWcag(value);
    }

    protected override void OnInitialized()
    {
        ThemeService.ThemeChanged += OnThemeChanged;
    }

    public void Dispose()
    {
        ThemeService.ThemeChanged -= OnThemeChanged;
    }

    void OnThemeChanged()
    {
        console.Log($"Theme changed to {ThemeService.Theme}");
    }
}
```


### Persist the theme

The Radzen.Blazor library provides a built-in service that persists the current theme in a cookie. This means that the theme will be remembered even after the user closes the browser or navigates to a different page. The theme will be restored when the user returns to the application.

#### 3. Open the `App.razor` file of your application and add this code:
