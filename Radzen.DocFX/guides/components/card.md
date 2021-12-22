# Card component
This article demonstrates how to use the Card component.

```
<RadzenCard class="m-3">
    <h3 class="h5">Contact</h3>
    <div class="d-flex flex-row">
        <RadzenImage Path="@order.Employee?.Photo" Class="rounded-circle float-left mr-3" Style="width: 100px; height: 100px;" />
        <div>
            <div>Employee</div>
            <b>@(order.Employee?.FirstName + " " + order.Employee?.LastName)</b>
            <div class="mt-3">Company</div>
            <b>@order.Customer?.CompanyName</b>
        </div>
    </div>
</RadzenCard>
```
