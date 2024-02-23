# Card component
This article demonstrates how to use the Card component.

```
<RadzenCard class="rz-m-3">
    <h3 class="h5">Contact</h3>
    <RadzenStack Orientation="Orientation.Horizontal">
        <RadzenImage Path="@order.Employee?.Photo" Class="rounded-circle float-left rz-mr-4" Style="width: 100px; height: 100px;" />
        <div>
            <div>Employee</div>
            <b>@(order.Employee?.FirstName + " " + order.Employee?.LastName)</b>
            <div class="rz-mt-4">Company</div>
            <b>@order.Customer?.CompanyName</b>
        </div>
    </RadzenStack>
</RadzenCard>
```
