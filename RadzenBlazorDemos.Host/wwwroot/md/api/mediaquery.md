# RadzenMediaQuery API Reference

## Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| Query | `string?` | The CSS media query this component will listen for. |

## Events

| Event | Type | Description |
|-------|------|-------------|
| Change | `EventCallback<bool>` | A callback that will be invoked when the status of the media query changes - to either match or not. |

## Methods

| Method | Returns | Description |
|--------|---------|-------------|
| OnChange(bool matches) | `Task` | Invoked by interop when media query changes. |

