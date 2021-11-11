# Panel component
This article demonstrates how to use the Panel component.

## Templates
Use `HeaderTemplate`, `ChildContent` and `SummaryTemplate` to define custom content for Panel component parts.

```
<RadzenPanel>
    <HeaderTemplate>
        Custom header
    </HeaderTemplate>
    <ChildContent>
        Custom content
    </ChildContent>
    <SummaryTemplate>
        Custom summary
    </SummaryTemplate>
</RadzenPanel>
```

## Expand/Collapse
Use `AllowCollapse` property to allow expand/collapse and `Expand` and `Collapse` callbacks to catch if Panel component is expanded or collapsed.

```
<RadzenPanel AllowCollapse="true" Expand=@(() => Change("Panel expanded")) Collapse=@(() => Change("Panel collapsed"))>
    <HeaderTemplate>
        Custom header
    </HeaderTemplate>
    <ChildContent>
        Custom content
    </ChildContent>
    <SummaryTemplate>
        Custom summary
    </SummaryTemplate>
</RadzenPanel>

@code {
    void Change(string text)
    {
        Console.WriteLine($"{text}");
    }
}
```