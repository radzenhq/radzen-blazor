# Accordion component
This article demonstrates how to use the Accordion component.

## Single item expand

```
<RadzenAccordion>
    <Items>
        <RadzenAccordionItem Text="Orders" Icon="account_balance_wallet">
            Details for Orders
        </RadzenAccordionItem>
        <RadzenAccordionItem Text="Employees" Icon="account_box">
            Details for Employees
        </RadzenAccordionItem>
        <RadzenAccordionItem Text="Customers" Icon="accessibility">
            Details for Customers
        </RadzenAccordionItem>
    </Items>
</RadzenAccordion>
```

## Multiple items expand

```
<RadzenAccordion Multiple="true">
    <Items>
        <RadzenAccordionItem Text="Orders" Icon="account_balance_wallet">
            Details for Orders
        </RadzenAccordionItem>
        <RadzenAccordionItem Text="Employees" Icon="account_box">
            Details for Employees
        </RadzenAccordionItem>
        <RadzenAccordionItem Text="Customers" Icon="accessibility">
            Details for Customers
        </RadzenAccordionItem>
    </Items>
</RadzenAccordion>
```

## Expand/Collapse events

```
<RadzenAccordion Collapse=@(args => Change(args, "collapsed"))
                 Expand=@(args => Change(args, "expanded"))>
    <Items>
        <RadzenAccordionItem Text="Orders" Icon="account_balance_wallet">
            Details for Orders
        </RadzenAccordionItem>
        <RadzenAccordionItem Text="Employees" Icon="account_box">
            Details for Employees
        </RadzenAccordionItem>
        <RadzenAccordionItem Text="Customers" Icon="accessibility">
            Details for Customers
        </RadzenAccordionItem>
    </Items>
</RadzenAccordion>

@code {
    void Change(object value, string action)
    {
        Console.WriteLine($"Item with index {value} {action}");
    }
}
```
