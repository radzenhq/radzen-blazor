# AsyncLoader component
This article demonstrates how to use the AsyncLoader component.

# Loading data asynchronously
The AsyncLoader component enables loading data asynchronously in Blazor applications. It offers the flexibility to specify loading and loaded templates, handle errors during data retrieval, and update the UI accordingly.

```
<RadzenAsyncLoader TData="Server.Models.ApplicationUser" DataTask="@Security.GetUserById(context.Id)">
    <LoadingTemplate>
        <RadzenProgressBarCircular ProgressBarStyle="ProgressBarStyle.Primary" Value="100" ShowValue="false" Mode="ProgressBarMode.Indeterminate" Size="ProgressBarCircularSize.Small" />
    </LoadingTemplate>
    <Template Context="ctx">
        <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="0.5rem">
            @foreach (var role in ctx.Roles)
            {
                <RadzenBadge Text="@role.Name" />
            }
        </RadzenStack>
    </Template>
</RadzenAsyncLoader>
```