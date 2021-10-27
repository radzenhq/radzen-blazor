# Pager component
This article demonstrates how to use the Pager component.

```
 <RadzenPager Count="count" PageSize="@pageSize" PageNumbersCount="10" PageChanged="@PageChanged" />

 @code {
    int pageSize = 6;
    int count;
    IEnumerable<Order> orders;

    protected override void OnInitialized()
    {
        count = dbContext.Orders.Count();
        orders = GetOrders(0, pageSize);
    }

    void PageChanged(PagerEventArgs args)
    {
        orders = GetOrders(args.Skip, args.Top);
    }

    IEnumerable<Order> GetOrders(int skip, int take)
    {
        return dbContext.Orders.Include("Customer").Include("Employee").Skip(skip).Take(take).ToList();
    }
}
```