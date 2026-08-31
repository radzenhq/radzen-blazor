# Radzen.Blazor.EntityFrameworkAdapter

Execute Entity Framework Core queries from Radzen data-bound components asynchronously.

Radzen components compose filtering, sorting, paging, and virtualization over a bound `IQueryable`. This adapter lets supported Entity Framework queries use `CountAsync` and `ToListAsync` without requiring a custom `LoadData` handler. `Radzen.Blazor` remains provider-agnostic; this package supplies the EF Core implementation of `IAsyncQueryExecutor`.

## Install

Install the adapter version that matches your `Radzen.Blazor` version:

```shell
dotnet add package Radzen.Blazor.EntityFrameworkAdapter
```

The package targets .NET 8, .NET 9, and .NET 10.

## Configure

Register the adapter once in `Program.cs`:

```csharp
builder.Services.AddRadzenQueryableEntityFrameworkAdapter();
```

## Use

Bind an EF Core `IQueryable` or `DbSet` normally:

```razor
<RadzenDataGrid Data="@db.Orders"
                TItem="Order"
                AllowPaging="true"
                AllowSorting="true"
                AllowFiltering="true">
    <Columns>
        <RadzenDataGridColumn TItem="Order" Property="Number" Title="Number" />
        <RadzenDataGridColumn TItem="Order" Property="Customer.Name" Title="Customer" />
    </Columns>
</RadzenDataGrid>
```

The component continues to compose the query. Count and materialization operations are awaited through EF Core when the query provider supports asynchronous execution.

## Supported components

- `RadzenDataGrid`, including paging, grouping, and virtualization
- `RadzenDataList`, including virtualization
- `RadzenPivotDataGrid`
- `RadzenDropDown` and `RadzenListBox` virtualization
- `RadzenDropDownDataGrid` search and paging

Existing synchronous behavior is preserved when the adapter is not registered or a query provider does not support asynchronous execution. Application-provided `LoadData` handlers continue to own their query execution.

`RadzenGantt` retains its existing synchronous internal loader because it performs hierarchy-wide traversal in addition to paging. Materialize its data asynchronously before binding when the source requires asynchronous database access.

## Cancellation and concurrency

Each component serializes its provider operations. A newer load requests cancellation of a superseded load and waits for that load to exit before using the same provider again. Virtualized requests link the component lifetime with the request cancellation token.

Coordination is per component. If multiple components or application services share one `DbContext`, they can still issue concurrent operations against that context. Follow EF Core lifetime guidance: avoid concurrent use of a `DbContext`, scope it to the appropriate unit of work, or create contexts with `IDbContextFactory<TContext>`.

Provider-originated failures and cancellations continue to propagate. The adapter permits synchronous fallback only for query shapes EF Core cannot execute asynchronously; it does not retry against a busy `DbContext`.
