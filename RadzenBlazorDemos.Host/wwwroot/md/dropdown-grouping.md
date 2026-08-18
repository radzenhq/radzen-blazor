# DropDown: Grouping

Group Blazor DropDown items into categories with group headers bound from a property in your data.

Keywords: select, picker, form, edit, multiple, dropdown, grouping

> API reference: [RadzenDropDown API](https://blazor.radzen.com/api/dropdown.md)

## Examples

## DropDown grouping

Group Blazor DropDown items into categories with group headers bound from a property in your data.

```razor
@inherits DbContextPage

<RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" JustifyContent="JustifyContent.Center" Gap="0.5rem" class="rz-p-sm-12">
    <RadzenLabel Text="Select Value" Component="DropDownGrouping" />
    <RadzenDropDown @bind-Value=value AllowClear="true" AllowFiltering="true" FilterCaseSensitivity="FilterCaseSensitivity.CaseInsensitive" Name="DropDownGrouping"
                    Data=@groupedData ValueProperty="OrderID" TextProperty="OrderID" ItemRender="ItemRender" Style="width: 100%; max-width: 400px;">
        <Template>
            <span style="display:inline-block; font-weight: @(context.CompanyName != null ? "bold" : "normal");">
                @(context.CompanyName ?? $"OrderID: {context.OrderID}, Order date: {string.Format("{0:d}", context.OrderDate)}")</span>
        </Template>
    </RadzenDropDown>
</RadzenStack>

@code {
    int? value;
    IEnumerable<GroupData> groupedData;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        var orders = dbContext.Orders.Where(o => o.CustomerID != null).Include("Customer").ToList();

        groupedData = orders.GroupBy(g => g.Customer.CompanyName)
            .SelectMany(i => new GroupData[] { new GroupData { CompanyName = i.Key, OrderID = null } }
                .Concat(i.Select(o =>
                    new GroupData
                    {
                        OrderID = o.OrderID,
                        OrderDate = o.OrderDate
                    }))).ToList();
    }

    void ItemRender(DropDownItemRenderEventArgs<int?> args)
    {
        var data = (GroupData)args.Item;
        if (data.CompanyName != null)
        {
            // Use this code to prevent default item selection for group items.
            args.Disabled = true;
            args.Attributes.Add("style", "opacity: 1");
        }
        else
        {
            args.Attributes.Add("style", "margin-inline-start: 1rem");
        }
    }

    class GroupData
    {
        public string CompanyName { get; set; }
        public int? OrderID { get; set; }
        public DateTime? OrderDate { get; set; }
    }
}
```


### DropDown grouping with multiple selection


```razor
@inherits DbContextPage

<RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" JustifyContent="JustifyContent.Center" Gap="0.5rem" class="rz-p-sm-12">
    <RadzenLabel Text="Select Value" Component="DropDownGroupingMultiSelect" Style="margin-right: 8px; vertical-align: middle;" />
    <RadzenDropDown @ref=dd @bind-Value=@values TValue="IEnumerable<int>" AllowClear="true" Name="DropDownGroupingMultiSelect"
                    Data=@groupedData Style="width: 100%; max-width: 400px;" ItemRender="ItemRender">
        <HeaderTemplate>
            <RadzenStack Orientation="Orientation.Horizontal" class="rz-multiselect-header rz-dropdown-filter-container rz-helper-clearfix">
                <RadzenCheckBox TValue="bool?" TriState=false Value="@AreAllSelected()" Change="@SelectAll" title="@(dd.SelectAllText)" Disabled=@(!groupedData.Any()) />
                <input id="@($"{dd.SearchID}")" @oninput=@Search placeholder="Search..." class="rz-inputtext" role="textbox" type="text"
                       aria-label="@(dd.SearchAriaLabel)" style="margin-inline-start:0.5rem;width:100%;border:0;background:transparent;" />
            </RadzenStack>
        </HeaderTemplate>
        <Template>
            <RadzenCheckBox TValue="bool?" TriState=false Value="@IsGroupSelected(context)" Change="@(args => SelectGroup(args, (GroupData)context))" />
            <RadzenLabel Style=@($"margin-inline-start: 0.5rem; font-weight: {(context.CompanyName != null ? "bold" : "normal")}")
                Text="@(context.CompanyName ?? $"OrderID: {context.OrderID}, Order date: {string.Format("{0:d}", context.OrderDate)}")" 
                onclick="event.target.previousElementSibling.querySelector('.rz-chkbox-box').click()"/>
        </Template>
        <ValueTemplate>
            @string.Join(", ", groupedData.Where(g => values?.Contains(g.OrderID) == true).Take(dd.MaxSelectedLabels).Select(g => $"OrderID: {g.OrderID}, Order date: {string.Format("{0:d}", g.OrderDate)}"))
        </ValueTemplate>
    </RadzenDropDown>
</RadzenStack>

@code {
    RadzenDropDown<IEnumerable<int>> dd;
    IEnumerable<int> values;
    IEnumerable<GroupData> groupedData;
    string search;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        await Populate();
    }

    void ItemRender(DropDownItemRenderEventArgs<IEnumerable<int>> args)
    {
        // Use this code to prevent default item selection.
        args.Disabled = true;
        args.Attributes.Add("style", $"opacity:1;{(((GroupData)args.Item).CompanyName == null ? "margin-inline-start:1rem" : "")}");
    }

    async Task Populate()
    {
        groupedData = await Task.FromResult(dbContext.Orders.Include("Customer")
            .Where(o => o.CustomerID != null &&
                    (string.IsNullOrEmpty(search) ? true :
                        (o.OrderID.ToString().Contains(search)
                            || o.CustomerID.ToLower().Contains(search.ToLower())
                                || o.Customer.CompanyName.ToLower().Contains(search.ToLower())))).ToList()
            .GroupBy(g => g.Customer.CompanyName)
            .SelectMany(i => new GroupData[] { new GroupData() { CompanyName = i.Key, Items = i } }
                        .Concat(i.Select(o => new GroupData() { OrderID = o.OrderID, OrderDate = o.OrderDate }))));
    }

    async Task Search(ChangeEventArgs args)
    {
        search = $"{args.Value}";
        await Populate();
    }

    bool? AreAllSelected()
    {
        return groupedData?.All(g => values?.Contains(g.OrderID) == true) == true ? (groupedData?.Any() == true ? true : false ) :
            groupedData?.Any(g => values?.Contains(g.OrderID) == true) == true ? null : false;
    }

    bool? IsGroupSelected(GroupData group)
    {
        if (group.CompanyName != null)
        {
            return group.Items.Any() && group.Items.All(i => values?.Contains(i.OrderID) == true) ? true :
                group.Items.Any(i => values?.Contains(i.OrderID) == true) ? null : false;
        }

        return values?.Contains(group.OrderID) == true;
    }

    void SelectGroup(bool? value, GroupData group)
    {
        var newValues = values ?? Enumerable.Empty<int>();
        var items = group.CompanyName != null ? group.Items.Select(i => i.OrderID) : new int[] { group.OrderID };
        values = value == true ? newValues.Concat(items) : newValues.Except(items);
    }

    void SelectAll(bool? value)
    {
        values = value == true ? groupedData.Select(g => g.OrderID) : Enumerable.Empty<int>();
    }

    class GroupData
    { 
        public string CompanyName { get; set; }
        public int OrderID { get; set; }
        public DateTime? OrderDate { get; set; }
        public IEnumerable<Order> Items { get; set; } = Enumerable.Empty<Order>();
    }
}
```
