# Accordion

The Blazor Accordion shows collapsible panels with single or multiple expand modes, dynamic items, and expand/collapse events.

Keywords: panel, container

> API reference: [RadzenAccordion API](https://blazor.radzen.com/api/accordion.md)

## Examples

## Blazor Accordion

The Blazor Accordion shows collapsible panels with single or multiple expand modes, dynamic items, and expand/collapse events.

### Accordion with single expand

By default, only one accordion item can be expanded at a time, automatically collapsing others.

```razor
<div class="rz-p-sm-12">
    <RadzenAccordion>
        <Items>
            <RadzenAccordionItem Text="Orders" Icon="account_balance_wallet" CollapseTitle="Collapse orders."
                                 ExpandTitle="Expand orders." CollapseAriaLabel="Collapse the order details."
                                 ExpandAriaLabel="Expand the order details.">
                <RadzenStack Gap="1rem">
                    <RadzenFormField Text="Order ID" Variant="Variant.Outlined">
                        <RadzenTextBox @bind-Value=@order.Id />
                    </RadzenFormField>
                    <RadzenFormField Text="Quantity" Variant="Variant.Outlined">
                        <RadzenNumeric @bind-Value=@order.Quantity Min="0" />
                    </RadzenFormField>
                    <RadzenFormField Text="Status" Variant="Variant.Outlined">
                        <RadzenDropDown @bind-Value=@order.Status Data=@statuses />
                    </RadzenFormField>
                </RadzenStack>
            </RadzenAccordionItem>
            <RadzenAccordionItem Text="Employees" Icon="account_box" CollapseTitle="Collapse employees."
                                 ExpandTitle="Expand employees." CollapseAriaLabel="Collapse the employee details."
                                 ExpandAriaLabel="Expand the employee details.">
                <RadzenStack Gap="1rem">
                    <RadzenFormField Text="Name" Variant="Variant.Outlined">
                        <RadzenTextBox @bind-Value=@employee.Name />
                    </RadzenFormField>
                    <RadzenFormField Text="Department" Variant="Variant.Outlined">
                        <RadzenDropDown @bind-Value=@employee.Department Data=@departments />
                    </RadzenFormField>
                    <RadzenFormField Text="Salary" Variant="Variant.Outlined">
                        <RadzenNumeric @bind-Value=@employee.Salary Format="c" Min="0" />
                    </RadzenFormField>
                </RadzenStack>
            </RadzenAccordionItem>
            <RadzenAccordionItem Text="Customers" Icon="accessibility" CollapseTitle="Collapse customers."
                                 ExpandTitle="Expand customers." CollapseAriaLabel="Collapse the customer details."
                                 ExpandAriaLabel="Expand the customer details.">
                <RadzenStack Gap="1rem">
                    <RadzenFormField Text="Company" Variant="Variant.Outlined">
                        <RadzenTextBox @bind-Value=@customer.Company />
                    </RadzenFormField>
                    <RadzenFormField Text="Email" Variant="Variant.Outlined">
                        <RadzenTextBox @bind-Value=@customer.Email />
                    </RadzenFormField>
                    <RadzenFormField Text="Country" Variant="Variant.Outlined">
                        <RadzenDropDown @bind-Value=@customer.Country Data=@countries />
                    </RadzenFormField>
                </RadzenStack>
            </RadzenAccordionItem>
        </Items>
    </RadzenAccordion>
</div>

@code {
    class Order
    {
        public string Id { get; set; } = "ORD-1001";
        public int Quantity { get; set; } = 1;
        public string Status { get; set; } = "New";
    }

    class Employee
    {
        public string Name { get; set; } = "Nancy Davolio";
        public string Department { get; set; } = "Sales";
        public decimal Salary { get; set; } = 2500;
    }

    class Customer
    {
        public string Company { get; set; } = "Around the Horn";
        public string Email { get; set; } = "info@aroundthehorn.com";
        public string Country { get; set; } = "UK";
    }

    Order order = new Order();
    Employee employee = new Employee();
    Customer customer = new Customer();

    string[] statuses = new[] { "New", "Processing", "Shipped", "Delivered" };
    string[] departments = new[] { "Sales", "Engineering", "Support", "HR" };
    string[] countries = new[] { "USA", "UK", "Germany", "France" };
}
```


### Accordion with multiple expand

Set `Multiple` to `true` to enable multiple expand.

```razor
<div class="rz-p-sm-12">
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
</div>
```


### Dynamically create Accordion items

Use two-way binding `-Selected` to bind items selection to your model.

```razor
<RadzenCard>
    <RadzenButton Text="Collapse all" Click="@(args => items.ForEach(i => i.Selected = false) )" />
    <RadzenButton Text="Expand all" Click="@(args => items.ForEach(i => i.Selected = true) )" />
</RadzenCard>
<div class="rz-p-sm-12">
    <RadzenAccordion Multiple="true">
        <Items>
            @foreach (var item in items)
            {
                <RadzenAccordionItem Text="@item.Text" @bind-Selected=@item.Selected>
                    Details for @(item.Text)
                </RadzenAccordionItem>
            }
        </Items>
    </RadzenAccordion>
</div>
@code {
    List<MyItem> items = Enumerable.Range(0, 5).Select(i => 
        new MyItem() 
        { 
            Text =  $"Item{i}", 
            Selected = i == 0 ? true : false 
        }).ToList();

    class MyItem
    { 
        public string Text { get; set; }
        public bool Selected { get; set; }
    }
}
```


### Expand/Collapse events

Handle `Expand` and `Collapse` events to respond when accordion items are opened or closed.

```razor
<RadzenStack Gap="1rem" class="rz-p-sm-12">
    <RadzenAccordion Multiple="true"
                    Collapse=@(args => Change(args, "Accordion", "collapsed"))
                    Expand=@(args => Change(args, "Accordion", "expanded"))>
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

    <EventConsole @ref=@console />
</RadzenStack>

@code {
    EventConsole console;
    void Change(object value, string name, string action)
    {
        console.Log($"{name} item with index {value} {action}");
    }
}
```


### Client-side rendering

Set `RenderMode` to `AccordionRenderMode.Client` to handle expand/collapse with JavaScript. All items are rendered initially and toggled client-side for faster interaction.

```razor
<RadzenStack Gap="1rem" class="rz-p-sm-12">
    <RadzenAccordion RenderMode="AccordionRenderMode.Client"
                    Collapse=@(args => Change(args, "Accordion", "collapsed"))
                    Expand=@(args => Change(args, "Accordion", "expanded"))>
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

    <EventConsole @ref=@console />
</RadzenStack>

@code {
    EventConsole console;
    void Change(object value, string name, string action)
    {
        console.Log($"{name} item with index {value} {action}");
    }
}
```


### Disable expand/collapse

Use `Disabled="true"` on accordion items to prevent them from being expanded or collapsed.

```razor
<RadzenStack class="rz-p-0 rz-p-md-12">
    <RadzenCard class="rz-p-4" Variant="Variant.Outlined">
        <RadzenStack Orientation="Orientation.Vertical" Gap="4px">
            <RadzenLabel Text="Expand/Collapse enabled:" />
            <RadzenSwitch @bind-Value="canCollapse" />
        </RadzenStack>
    </RadzenCard>
    <RadzenAccordion>
        <Items>
            <RadzenAccordionItem Disabled="@(!canCollapse)" Text="Orders" Icon="account_balance_wallet">
                Details for Orders
            </RadzenAccordionItem>
            <RadzenAccordionItem Disabled="@(!canCollapse)" Text="Employees" Icon="account_box">
                Details for Employees
            </RadzenAccordionItem>
            <RadzenAccordionItem Disabled="@(!canCollapse)" Text="Customers" Icon="accessibility">
                Details for Customers
            </RadzenAccordionItem>
        </Items>
    </RadzenAccordion>
</RadzenStack>

@code {
    bool canCollapse = true;
}
```
