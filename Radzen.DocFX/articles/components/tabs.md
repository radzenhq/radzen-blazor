# Tabs component
This article demonstrates how to use Tabs.

## Server render mode
Only selected tab content will be rendered.
```
<RadzenTabs @bind-SelectedIndex=@selectedIndex Change=@OnChange>
    <Tabs>
        <RadzenTabsItem Text="Customers">
            Customers
        </RadzenTabsItem>
        <RadzenTabsItem Text="Orders">
            Orders
        </RadzenTabsItem>
        <RadzenTabsItem Text="Order Details">
            Order Details
        </RadzenTabsItem>
    </Tabs>
</RadzenTabs>

@code {
    int selectedIndex = 0;

    void OnChange(int index)
    {
        console.Log($"Tab with index {index} was selected.");
    }
}
```

## Client render mode
All tabs will be rendered initially and tab change will be performed completely using JavaScript
```
<RadzenTabs RenderMode="TabRenderMode.Client" @bind-SelectedIndex=@selectedIndex Change=@OnChange>
    <Tabs>
        <RadzenTabsItem Text="Customers">
            Customers
        </RadzenTabsItem>
        <RadzenTabsItem Text="Orders">
            Orders
        </RadzenTabsItem>
        <RadzenTabsItem Text="Order Details">
            Order Details
        </RadzenTabsItem>
    </Tabs>
</RadzenTabs>

@code {
    int selectedIndex = 0;

    void OnChange(int index)
    {
        console.Log($"Tab with index {index} was selected.");
    }
}
```