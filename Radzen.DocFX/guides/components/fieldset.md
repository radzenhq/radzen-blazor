# Fieldset component
This article demonstrates how to use the Fieldset component.

## Templates
Use `HeaderTemplate`, `ChildContent` and `SummaryTemplate` to define custom content for Fieldset component parts.

```
<RadzenFieldset>
    <HeaderTemplate>
        Custom header
    </HeaderTemplate>
    <ChildContent>
        Custom content
    </ChildContent>
    <SummaryTemplate>
        Custom summary
    </SummaryTemplate>
</RadzenFieldset>
```

## Expand/Collapse
Use `AllowCollapse` property to allow expand/collapse and `Expand` and `Collapse` callbacks to catch if Fieldset component is expanded or collapsed.

```
<RadzenFieldset AllowCollapse="true" Expand=@(() => Change("Fieldset expanded")) Collapse=@(() => Change("Fieldset collapsed"))>
    <HeaderTemplate>
        Custom header
    </HeaderTemplate>
    <ChildContent>
        Custom content
    </ChildContent>
    <SummaryTemplate>
        Custom summary
    </SummaryTemplate>
</RadzenFieldset>

@code {
    void Change(string text)
    {
        Console.WriteLine($"{text}");
    }
}
```