# Icons

Display Material icons in Blazor with the RadzenIcon component - control size and color, and use custom icon fonts.

Keywords: icon, content

## Examples

## Blazor Icon

The Blazor Icon component renders Material icons with control over size and color, plus support for custom icon fonts.

### Material Icons

By default, the `RadzenIcon` component uses the embedded in Radzen Blazor Components `MaterialSymbolsOutlined.woff2` font containing more than 2,500 glyphs. [See all Material Symbols ↗](https://fonts.google.com/icons?icon.set=Material+Symbols)

```razor
<RadzenStack Orientation="Orientation.Horizontal" Wrap="FlexWrap.Wrap" class="icon-preview rz-p-12">
    <div><RadzenIcon Icon="accessibility" /><span>accessibility</span></div>
    <div><RadzenIcon Icon="3d_rotation" /><span>3d_rotation</span></div>
    <div><RadzenIcon Icon="accessible" /><span>accessible</span></div>
    <div><RadzenIcon Icon="account_balance" /><span>account_balance</span></div>
    <div><RadzenIcon Icon="account_balance_wallet" /><span>account_balance_wallet</span></div>
    <div><RadzenIcon Icon="account_box" /><span>account_box</span></div>
    <div><RadzenIcon Icon="account_circle" /><span>account_circle</span></div>
    <div><RadzenIcon Icon="add_shopping_cart" /><span>add_shopping_cart</span></div>
    <div><RadzenIcon Icon="alarm" /><span>alarm</span></div>
    <div><RadzenIcon Icon="alarm_add" /><span>alarm_add</span></div>
    <div><RadzenIcon Icon="alarm_off" /><span>alarm_off</span></div>
    <div><RadzenIcon Icon="alarm_on" /><span>alarm_on</span></div>
    <div><RadzenIcon Icon="all_out" /><span>all_out</span></div>
    <div><RadzenIcon Icon="android" /><span>android</span></div>
    <div><RadzenIcon Icon="announcement" /><span>announcement</span></div>
    <div><RadzenIcon Icon="aspect_ratio" /><span>aspect_ratio</span></div>
    <div><RadzenIcon Icon="assessment" /><span>assessment</span></div>
    <div><RadzenIcon Icon="assignment" /><span>assignment</span></div>
    <div><RadzenIcon Icon="assignment_ind" /><span>assignment_ind</span></div>
    <div><RadzenIcon Icon="assignment_late" /><span>assignment_late</span></div>
    <div><RadzenIcon Icon="assignment_return" /><span>assignment_return</span></div>
    <div><RadzenIcon Icon="assignment_returned" /><span>assignment_returned</span></div>
    <div><RadzenIcon Icon="assignment_turned_in" /><span>assignment_turned_in</span></div>
    <div><RadzenIcon Icon="autorenew" /><span>autorenew</span></div>
    <div><RadzenIcon Icon="backup" /><span>backup</span></div>
    <div><RadzenIcon Icon="book" /><span>book</span></div>
    <div><RadzenIcon Icon="bookmark" /><span>bookmark</span></div>
    <div><RadzenIcon Icon="bookmark_border" /><span>bookmark_border</span></div>
    <div><RadzenIcon Icon="bug_report" /><span>bug_report</span></div>
    <div><RadzenIcon Icon="build" /><span>build</span></div>
    <div><RadzenIcon Icon="cached" /><span>cached</span></div>
    <div><RadzenIcon Icon="camera_enhance" /><span>camera_enhance</span></div>
    <div><RadzenIcon Icon="card_giftcard" /><span>card_giftcard</span></div>
    <div><RadzenIcon Icon="card_membership" /><span>card_membership</span></div>
    <div><RadzenIcon Icon="card_travel" /><span>card_travel</span></div>
    <div><RadzenIcon Icon="change_history" /><span>change_history</span></div>
    <div><RadzenIcon Icon="check_circle" /><span>check_circle</span></div>
    <div><RadzenIcon Icon="chrome_reader_mode" /><span>chrome_reader_mode</span></div>
    <div><RadzenIcon Icon="class" /><span>class</span></div>
    <div><RadzenIcon Icon="code" /><span>code</span></div>
    <div><RadzenIcon Icon="compare_arrows" /><span>compare_arrows</span></div>
    <div><RadzenIcon Icon="copyright" /><span>copyright</span></div>
    <div><RadzenIcon Icon="credit_card" /><span>credit_card</span></div>
    <div><RadzenIcon Icon="dashboard" /><span>dashboard</span></div>
    <div><RadzenIcon Icon="date_range" /><span>date_range</span></div>
    <div><RadzenIcon Icon="delete" /><span>delete</span></div>
    <div><RadzenIcon Icon="delete_forever" /><span>delete_forever</span></div>
    <div><RadzenIcon Icon="description" /><span>description</span></div>
    <div><RadzenIcon Icon="dns" /><span>dns</span></div>
    <div><RadzenIcon Icon="done" /><span>done</span></div>
    <div><RadzenIcon Icon="done_all" /><span>done_all</span></div>
    <div><RadzenIcon Icon="donut_large" /><span>donut_large</span></div>
    <div><RadzenIcon Icon="donut_small" /><span>donut_small</span></div>
    <div><RadzenIcon Icon="eject" /><span>eject</span></div>
    <div><RadzenIcon Icon="euro_symbol" /><span>euro_symbol</span></div>
    <div><RadzenIcon Icon="event" /><span>event</span></div>
    <div><RadzenIcon Icon="event_seat" /><span>event_seat</span></div>
    <div><RadzenIcon Icon="exit_to_app" /><span>exit_to_app</span></div>
    <div><RadzenIcon Icon="explore" /><span>explore</span></div>
    <div><RadzenIcon Icon="extension" /><span>extension</span></div>
    <div><RadzenIcon Icon="face" /><span>face</span></div>
    <div><RadzenIcon Icon="favorite" /><span>favorite</span></div>
    <div><RadzenIcon Icon="favorite_border" /><span>favorite_border</span></div>
    <div><RadzenIcon Icon="feedback" /><span>feedback</span></div>
    <div><RadzenIcon Icon="find_in_page" /><span>find_in_page</span></div>
    <div><RadzenIcon Icon="find_replace" /><span>find_replace</span></div>
    <div><RadzenIcon Icon="fingerprint" /><span>fingerprint</span></div>
    <div><RadzenIcon Icon="flight_land" /><span>flight_land</span></div>
    <div><RadzenIcon Icon="flight_takeoff" /><span>flight_takeoff</span></div>
    <div><RadzenIcon Icon="flip_to_back" /><span>flip_to_back</span></div>
    <div><RadzenIcon Icon="flip_to_front" /><span>flip_to_front</span></div>
    <div><RadzenIcon Icon="g_translate" /><span>g_translate</span></div>
    <div><RadzenIcon Icon="gavel" /><span>gavel</span></div>
    <div><RadzenIcon Icon="get_app" /><span>get_app</span></div>
    <div><RadzenIcon Icon="gif" /><span>gif</span></div>
    <div><RadzenIcon Icon="grade" /><span>grade</span></div>
    <div><RadzenIcon Icon="group_work" /><span>group_work</span></div>
    <div><RadzenIcon Icon="help" /><span>help</span></div>
    <div><RadzenIcon Icon="highlight_off" /><span>highlight_off</span></div>
    <div><RadzenIcon Icon="history" /><span>history</span></div>
    <div><RadzenIcon Icon="home" /><span>home</span></div>
    <div><RadzenIcon Icon="hourglass_empty" /><span>hourglass_empty</span></div>
    <div><RadzenIcon Icon="hourglass_full" /><span>hourglass_full</span></div>
    <div><RadzenIcon Icon="http" /><span>http</span></div>
    <div><RadzenIcon Icon="https" /><span>https</span></div>
    <div><RadzenIcon Icon="input" /><span>input</span></div>
    <div><RadzenIcon Icon="invert_colors" /><span>invert_colors</span></div>
    <div><RadzenIcon Icon="label" /><span>label</span></div>
    <div><RadzenIcon Icon="language" /><span>language</span></div>
    <div><RadzenIcon Icon="launch" /><span>launch</span></div>
    <div><RadzenIcon Icon="lightbulb" /><span>lightbulb</span></div>
    <div><RadzenIcon Icon="line_style" /><span>line_style</span></div>
    <div><RadzenIcon Icon="line_weight" /><span>line_weight</span></div>
    <div><RadzenIcon Icon="list" /><span>list</span></div>
    <div><RadzenIcon Icon="lock" /><span>lock</span></div>
    <div><RadzenIcon Icon="lock_open" /><span>lock_open</span></div>
    <div><RadzenIcon Icon="loyalty" /><span>loyalty</span></div>
    <div><RadzenIcon Icon="markunread_mailbox" /><span>markunread_mailbox</span></div>
    <div><RadzenIcon Icon="motorcycle" /><span>motorcycle</span></div>
    <div><RadzenIcon Icon="note_add" /><span>note_add</span></div>
    <div><RadzenIcon Icon="offline_pin" /><span>offline_pin</span></div>
    <div><RadzenIcon Icon="opacity" /><span>opacity</span></div>
    <div><RadzenIcon Icon="open_in_browser" /><span>open_in_browser</span></div>
    <div><RadzenIcon Icon="open_in_new" /><span>open_in_new</span></div>
    <div><RadzenIcon Icon="open_with" /><span>open_with</span></div>
    <div><RadzenIcon Icon="pageview" /><span>pageview</span></div>
</RadzenStack>
<style>
    .icon-preview div {
        display: flex;
        flex-direction: column;
        align-items: center;
        gap: 0.5rem;
        width: 120px;
    }
    .icon-preview span {
        color: var(--rz-text-disabled-color);
        font-size: 0.75rem;
    }
</style>
```

[See all Material Symbols](https://fonts.google.com/icons?icon.set=Material+Symbols)

### Icon color

Use `IconColor` property to set custom icon foreground color.

```razor
<RadzenStack Orientation="Orientation.Horizontal" JustifyContent="JustifyContent.Center" class="rz-p-0 rz-p-md-6 rz-p-lg-12" >
    <RadzenIcon Icon="info" IconColor="@Colors.Info" />
    <RadzenIcon Icon="warning" IconColor="@Colors.Warning" />
    <RadzenIcon Icon="dangerous" IconColor="@Colors.Danger" />
    <RadzenIcon Icon="done" IconColor="@Colors.Success" />
    <RadzenIcon Icon="smart_button" IconColor="@Colors.Primary" />
    <RadzenIcon Icon="smart_button" IconColor="@Colors.Secondary" />
    <RadzenIcon Icon="dialpad" IconColor="pink" />
</RadzenStack>
```


### Filled icons

Use `font-variation-settings` CSS property for filled icons with the Material Symbols font. Note that some icons cannot be filled because they lack elements that allow for filling.

```razor
<RadzenStack Orientation="Orientation.Horizontal" JustifyContent="JustifyContent.Center" class="rz-p-0 rz-p-md-6 rz-p-lg-12" >
    <RadzenIcon class="filled-icon" Icon="info" IconColor="@Colors.Info" />
    <RadzenIcon class="filled-icon" Icon="warning" IconColor="@Colors.Warning" />
    <RadzenIcon class="filled-icon" Icon="dangerous" IconColor="@Colors.Danger" />
    <RadzenIcon class="filled-icon" Icon="done" IconColor="@Colors.Success" />
    <RadzenIcon class="filled-icon" Icon="smart_button" IconColor="@Colors.Primary" />
    <RadzenIcon class="filled-icon" Icon="smart_button" IconColor="@Colors.Secondary" />
    <RadzenIcon class="filled-icon" Icon="dialpad" IconColor="pink" />
</RadzenStack>

<style>
.filled-icon {
    font-variation-settings: 'FILL' 1;
}
</style>
```


### Styled icons

Use `IconStyle` property to modify the icons foreground color. It offers the standard styles defined by the theme.

```razor
<RadzenStack Orientation="Orientation.Horizontal" JustifyContent="JustifyContent.Center" class="rz-p-0 rz-p-md-6 rz-p-lg-12" >
    <RadzenCard Variant="Variant.Flat">
        <RadzenIcon Icon="dashboard" IconStyle="IconStyle.Primary" />
    </RadzenCard>
    <RadzenCard Variant="Variant.Flat">
        <RadzenIcon Icon="dashboard" IconStyle="IconStyle.Secondary" />
    </RadzenCard>
    <RadzenCard Variant="Variant.Flat">
        <RadzenIcon Icon="dashboard" IconStyle="IconStyle.Info" />
    </RadzenCard>
    <RadzenCard Variant="Variant.Flat">
        <RadzenIcon Icon="dashboard" IconStyle="IconStyle.Success" />
    </RadzenCard>
    <RadzenCard Variant="Variant.Flat">
        <RadzenIcon Icon="dashboard" IconStyle="IconStyle.Warning" />
    </RadzenCard>
    <RadzenCard Variant="Variant.Flat">
        <RadzenIcon Icon="dashboard" IconStyle="IconStyle.Danger" />
    </RadzenCard>
    <RadzenCard Variant="Variant.Flat" class="rz-background-color-base-700">
        <RadzenIcon Icon="dashboard" IconStyle="IconStyle.Light" />
    </RadzenCard>
    <RadzenCard Variant="Variant.Flat" class="rz-background-color-on-base">
        <RadzenIcon Icon="dashboard" IconStyle="IconStyle.Base" />
    </RadzenCard>
    <RadzenCard Variant="Variant.Flat" class="rz-background-color-base-200">
        <RadzenIcon Icon="dashboard" IconStyle="IconStyle.Dark" />
    </RadzenCard>
</RadzenStack>

<style>
    .rz-card {
        line-height: 0;
    }
</style>
```


### Using RadzenIcon with other icon fonts

You can use any icon font supporting ligatures with the `RadzenIcon` component. To do so, you need to load the font file using the CSS `@-face` at-rule and set the corresponding font-family name to the `--rz-icon-font-family` CSS variable. The example below uses Material Symbols Rounded font.

#### RadzenIcon with Material Symbols Rounded font

Material Symbols and Material Symbols Rounded are variable fonts containing multiple stylistic variations e.g. you can control the boldness of the icon using the `font-weight:` CSS property.

```razor
<style>
    /* START Material Symbols font CSS */

    @@font-face {
        font-family: 'Material Symbols Rounded';
        font-style: normal;
        font-weight: 100 700;
        src: url('fonts/MaterialSymbolsRounded.woff2') format('woff2');
    }

    .material-symbols-rounded {
        --rz-icon-font-family: 'Material Symbols Rounded';
    }

    /* END Material Symbols font CSS */
</style>
<RadzenStack Orientation="Orientation.Horizontal" Wrap="FlexWrap.Wrap" JustifyContent="JustifyContent.Center" AlignItems="AlignItems.Center" class="rz-m-12" Style="--rz-icon-size: 2rem;">
    <div style="width: 200px">Material Symbols (default)</div>
    <RadzenIcon Icon="home" style="font-weight: 100;"/>
    <RadzenIcon Icon="home" style="font-weight: 200;"/>
    <RadzenIcon Icon="home" style="font-weight: 300;"/>
    <RadzenIcon Icon="home" style="font-weight: 400;"/>
    <RadzenIcon Icon="home" style="font-weight: 500;"/>
    <RadzenIcon Icon="home" style="font-weight: 600;"/>
    <RadzenIcon Icon="home" style="font-weight: 700;"/>
</RadzenStack>

<RadzenStack Orientation="Orientation.Horizontal" Wrap="FlexWrap.Wrap" JustifyContent="JustifyContent.Center" AlignItems="AlignItems.Center" class="material-symbols-rounded rz-m-12" Style="--rz-icon-size: 2rem;">
    <div style="width: 200px">Material Symbols Rounded</div>
    <RadzenIcon Icon="home" style="font-weight: 100;"/>
    <RadzenIcon Icon="home" style="font-weight: 200;"/>
    <RadzenIcon Icon="home" style="font-weight: 300;"/>
    <RadzenIcon Icon="home" style="font-weight: 400;"/>
    <RadzenIcon Icon="home" style="font-weight: 500;"/>
    <RadzenIcon Icon="home" style="font-weight: 600;"/>
    <RadzenIcon Icon="home" style="font-weight: 700;"/>
</RadzenStack>
```

Read more about [variable fonts](https://fonts.google.com/knowledge/glossary/variable_fonts).

#### RadzenIcon with Material Symbols font loaded from Google Fonts.

For full control over the icon font content and styles, you can load the Material Symbols font from Google Fonts and set the `--rz-icon-font-family` CSS variable accordingly.

```razor
<!-- Include the Material Symbols font from Google Fonts -->
<link href="https://fonts.googleapis.com/css2?family=Material+Symbols+Outlined" rel="stylesheet" />
<style>
    /* Set the --rz-icon-font-family CSS variable to use Material Symbols Outlined font */

    .material-symbols-outlined {
        --rz-icon-font-family: 'Material Symbols Outlined';
    }

    /* To do that globally, you can add the following CSS to your site.css file:

    :root {
        --rz-icon-font-family: 'Material Symbols Outlined';
    }
    */

</style>

<RadzenStack Orientation="Orientation.Horizontal" Wrap="FlexWrap.Wrap" JustifyContent="JustifyContent.Center" AlignItems="AlignItems.Center" class="material-symbols-outlined rz-m-12" Style="--rz-icon-size: 2rem;">
    <RadzenIcon Icon="home" style="font-weight: 100;"/>
    <RadzenIcon Icon="home" style="font-weight: 200;"/>
    <RadzenIcon Icon="home" style="font-weight: 300;"/>
    <RadzenIcon Icon="home" style="font-weight: 400;"/>
    <RadzenIcon Icon="home" style="font-weight: 500;"/>
    <RadzenIcon Icon="home" style="font-weight: 600;"/>
    <RadzenIcon Icon="home" style="font-weight: 700;"/>
</RadzenStack>
```


#### RadzenIcon with FontAwesome font


```razor
<style>
    /* Font Awesome 6 Free font CSS */

    @@font-face {
        font-family: 'Font Awesome 6 Free';
        font-style: normal;
        font-weight: 900;
        src: url('https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.7.2/webfonts/fa-solid-900.woff2') format('woff2');
    }

    .font-awesome {
      --rz-icon-font-family: 'Font Awesome 6 Free'
    }

    /* Font Awesome 6 Free font CSS */
</style>
<RadzenStack Orientation="Orientation.Horizontal" Wrap="FlexWrap.Wrap" JustifyContent="JustifyContent.Center" AlignItems="AlignItems.Center" class="rz-m-12" Style="--rz-icon-size: 2rem;">
    <div style="width: 200px">Material Symbols (default)</div>
    <RadzenIcon Icon="home"/>
    <RadzenIcon Icon="help"/>
    <RadzenIcon Icon="delete"/>
    <RadzenIcon Icon="folder"/>
</RadzenStack>

<RadzenStack Orientation="Orientation.Horizontal" Wrap="FlexWrap.Wrap" JustifyContent="JustifyContent.Center" AlignItems="AlignItems.Center" class="font-awesome rz-m-12" Style="--rz-icon-size: 2rem;">
    <div style="width: 200px">Font Awesome 6 Free</div>
    <RadzenIcon Icon="@("\uf015")"/>
    <RadzenIcon Icon="@("\uf059")"/>
    <RadzenIcon Icon="@("\uf2ed")"/>
    <RadzenIcon Icon="@("\uf07b")"/>
</RadzenStack>
```
