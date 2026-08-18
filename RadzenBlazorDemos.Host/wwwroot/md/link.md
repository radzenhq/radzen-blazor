# Link

The Blazor Link renders a navigation link with Path and Target, integrated with Blazor routing.

> API reference: [RadzenLink API](https://blazor.radzen.com/api/link.md)

## Examples

## Blazor Link

The Blazor Link renders a navigation link with Path and Target, integrated with Blazor routing.

### Link to path in application

Create navigation links to internal application paths using the `Path` property.

```razor
<div class="rz-p-12 rz-text-align-center">
    <RadzenLink Path="buttons" Text="Go to Buttons page" />
</div>
```


### Link to path in application with icon

Add icons to links using the `Icon` property for enhanced visual cues.

```razor
<div class="rz-p-12 rz-text-align-center">
    <RadzenLink Icon="accessibility" Path="button" Text="Go to the Button component page" />
</div>
```


### Link to url

Create links to external URLs that open in the current or new browser tab/window.

```razor
<div class="rz-p-12 rz-text-align-center">
    <RadzenLink Path="https://www.radzen.com" Text="Go to url" target="_blank" />
</div>
```


### Link with child content

Define custom child content for links to include icons, badges, or other complex markup.

```razor
<div class="rz-p-12 rz-text-align-center">
    <RadzenLink Path="https://www.radzen.com" Text="Go to url" target="_blank">
        <RadzenImage Path="https://www.radzen.com/assets/radzen-logo-top-b2d6e9dcacf7d344bbab515b8748c5f4d702c6c5bfc349bd9ff9003016a3a6ee.svg" Style="width: 50%">
        </RadzenImage>
    </RadzenLink>
</div>
```


### Link disabled

Use `Disabled="true"` to disable a link and prevent navigation.

```razor
<div class="rz-p-12 rz-text-align-center">
    <RadzenLink Icon="block" Disabled="true" Path="button" Text="This link is disabled and cannot be accessed" />
</div>
```
