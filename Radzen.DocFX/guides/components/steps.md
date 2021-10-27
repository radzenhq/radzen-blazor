# Steps component
This article demonstrates how to use Steps.

```
<RadzenSteps @bind-SelectedIndex=@selectedIndex Change=@OnChange>
    <Steps>
        <RadzenStepsItem Text="Customers">
            Customers
        </RadzenStepsItem>
        <RadzenStepsItem Text="Orders" Disabled="@(selectedCustomers == null || selectedCustomers != null && !selectedCustomers.Any())">
            Orders
        </RadzenStepsItem>
        <RadzenStepsItem Text="Order Details" Disabled="@(selectedOrders == null || selectedOrders != null && !selectedOrders.Any())">
            Order Details
        </RadzenStepsItem>
    </Steps>
</RadzenSteps>

@code {
    int selectedIndex = 0;

    void OnChange(int index)
    {
        console.Log($"Step with index {index} was selected.");
    }
}
```