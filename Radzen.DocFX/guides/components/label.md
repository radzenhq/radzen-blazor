# Label component
This article demonstrates how to use the Label component. Use `Component` property to associate Label with input component with specific Name.

## Label with plain-text content

```
<RadzenLabel Text="Some text" Component="CheckBox1" />
<RadzenCheckBox Name="CheckBox1" />
```

## Label with HTML content

```
<RadzenLabel Component="CheckBox1">Some <strong>text</strong></RadzenLabel>
<RadzenCheckBox Name="CheckBox1" />
```
