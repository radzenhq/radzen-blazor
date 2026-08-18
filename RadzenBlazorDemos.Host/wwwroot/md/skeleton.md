# Skeleton

RadzenSkeleton component displays loading placeholders with various shapes and animations.

Keywords: skeleton, load, loading, placeholder, animation, wave, pulse, text, circular, rectangular, rounded

> API reference: [RadzenSkeleton API](https://blazor.radzen.com/api/skeleton.md)

## Examples

## Blazor Skeleton

Display loading placeholders with various shapes and animations while content loads.

### Basic Usage

Use the `Variant` property to specify the skeleton shape and `Style` to control dimensions. The component supports three shape variants - Text, Circular, and Rectangular.

```razor
<RadzenStack Orientation="Orientation.Horizontal" JustifyContent="JustifyContent.SpaceBetween" Gap="3rem" Wrap="FlexWrap.Wrap" class="rz-py-12 rz-px-6 rz-mx-auto" Style="max-width: 800px;">
    <RadzenStack Orientation="Orientation.Vertical" AlignItems="AlignItems.Center" Gap="1rem">
        <RadzenText TextStyle="TextStyle.H6" TagName="TagName.P">Text (Default)</RadzenText>
        <RadzenSkeleton /> <!-- Default Skeleton. Equivalent to Variant="SkeletonVariant.Text" -->
        <RadzenSkeleton Style="height: 2rem;" />
    </RadzenStack>
    <RadzenStack Orientation="Orientation.Vertical" AlignItems="AlignItems.Center" Gap="1rem">
        <RadzenText TextStyle="TextStyle.H6" TagName="TagName.P">Circular</RadzenText>
        <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" >
            <RadzenSkeleton Variant="SkeletonVariant.Circular" Style="width: 1rem; height: 1rem" />
            <RadzenSkeleton Variant="SkeletonVariant.Circular" />
            <RadzenSkeleton Variant="SkeletonVariant.Circular" Style="width: 4rem; height: 4rem" />
        </RadzenStack>
    </RadzenStack>
    <RadzenStack Orientation="Orientation.Vertical" AlignItems="AlignItems.Center" Gap="1rem">
        <RadzenText TextStyle="TextStyle.H6" TagName="TagName.P">Rectangular</RadzenText>
        
        <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center">
            <RadzenSkeleton Variant="SkeletonVariant.Rectangular" />
            <RadzenSkeleton Variant="SkeletonVariant.Rectangular" Style="width: 4rem; height: 4rem" />
            <RadzenSkeleton Variant="SkeletonVariant.Rectangular" Style="width: 2rem; height: 4rem" />
        </RadzenStack>
    </RadzenStack>
</RadzenStack>
```


### Text size

The height value of the default Text variant is a function of the parent's element font-size.

```razor
<RadzenStack Orientation="Orientation.Vertical" Gap="1rem" Wrap="FlexWrap.Wrap" class="rz-p-12">
    <RadzenText TextStyle="TextStyle.H1" TagName="TagName.P">
        <RadzenSkeleton Animation="SkeletonAnimation.Wave" /> Heading 1 
    </RadzenText>
    <RadzenText TextStyle="TextStyle.H3" TagName="TagName.P">
        <RadzenSkeleton Animation="SkeletonAnimation.Wave" /> Heading 3 
    </RadzenText>
    <RadzenText TextStyle="TextStyle.H5" TagName="TagName.P">
        <RadzenSkeleton Animation="SkeletonAnimation.Wave" /> Heading 5 
    </RadzenText>
    <RadzenText TextStyle="TextStyle.Body1">
        <RadzenSkeleton Animation="SkeletonAnimation.Wave" /> Body 1 
    </RadzenText>
    <RadzenText TextStyle="TextStyle.Caption">
        <RadzenSkeleton Animation="SkeletonAnimation.Wave" /> Caption 
    </RadzenText>
</RadzenStack>
```


### Animations

The component has three animation types (None, Wave, Pulse).

```razor
<RadzenRow Gap="2rem" class="rz-p-12">
    <RadzenColumn Size="12" SizeMD="4"> 
        <RadzenStack Orientation="Orientation.Vertical" Gap="1rem">
            <RadzenText TextStyle="TextStyle.H6" TagName="TagName.P">None (Default)</RadzenText>
            <RadzenSkeleton Style="width: 100%;" />
            <RadzenSkeleton Style="width: 100%;" />
            <RadzenSkeleton Style="width: 60%;" />
        </RadzenStack>
    </RadzenColumn>
    <RadzenColumn Size="12" SizeMD="4"> 
        <RadzenStack Orientation="Orientation.Vertical" Gap="1rem">
            <RadzenText TextStyle="TextStyle.H6" TagName="TagName.P">Wave Animation</RadzenText>
            <RadzenSkeleton Animation="SkeletonAnimation.Wave" Style="width: 100%;" />
            <RadzenSkeleton Animation="SkeletonAnimation.Wave" Style="width: 100%;" />
            <RadzenSkeleton Animation="SkeletonAnimation.Wave" Style="width: 60%;" />
        </RadzenStack>
    </RadzenColumn>
    <RadzenColumn Size="6" SizeMD="4">
        <RadzenStack Orientation="Orientation.Vertical" Gap="1rem">
            <RadzenText TextStyle="TextStyle.H6" TagName="TagName.P">Pulse Animation</RadzenText>
            <RadzenSkeleton Animation="SkeletonAnimation.Pulse" Style="width: 100%;" />
            <RadzenSkeleton Animation="SkeletonAnimation.Pulse" Style="width: 100%;" />
            <RadzenSkeleton Animation="SkeletonAnimation.Pulse" Style="width: 60%;" />
        </RadzenStack>
    </RadzenColumn>
</RadzenRow>
```


### Complex Example

Here's how you might use skeletons in a card layout to show loading states.

```razor
<div class="rz-p-12">
    <RadzenRow Gap="2rem">
        <RadzenColumn Size="4">
            <RadzenCard Variant="Variant.Outlined" class="rz-p-8">
                <RadzenStack Orientation="Orientation.Vertical" Gap="1rem">
                    <RadzenStack Orientation="Orientation.Horizontal" Gap="1rem" AlignItems="AlignItems.Center">
                        <RadzenSkeleton Variant="SkeletonVariant.Circular" Style="width: 50px; height: 50px;" />
                        <RadzenStack Orientation="Orientation.Vertical" Gap="0.5rem">
                            <RadzenSkeleton Style="width: 120px; height: 16px;" />
                            <RadzenSkeleton Style="width: 80px; height: 14px;" />
                        </RadzenStack>
                    </RadzenStack>
                    
                    <RadzenSkeleton Style="width: 100%; height: 14px;" />
                    <RadzenSkeleton Style="width: 90%; height: 14px;" />
                    <RadzenSkeleton Style="width: 70%; height: 14px;" />
                    
                    <RadzenStack Orientation="Orientation.Horizontal" Gap="1rem" class="rz-mt-6">
                        <RadzenSkeleton Style="width: 80px; height: 32px;" />
                        <RadzenSkeleton Style="width: 80px; height: 32px;" />
                    </RadzenStack>
                </RadzenStack>
            </RadzenCard>
        </RadzenColumn>
        
        <RadzenColumn Size="4">
            <RadzenCard Variant="Variant.Outlined" class="rz-p-8">
                <RadzenStack Orientation="Orientation.Vertical" Gap="1rem">
                    <RadzenSkeleton Variant="SkeletonVariant.Rectangular" Style="width: 100%; height: 80px;" />
                    
                    <RadzenSkeleton Style="width: 100%; height: 16px;" />
                    <RadzenSkeleton Style="width: 80%; height: 14px;" />
                    <RadzenSkeleton Style="width: 60%; height: 14px;" />
                    
                    <RadzenStack Orientation="Orientation.Horizontal" Gap="1rem" class="rz-mt-6">
                        <RadzenSkeleton Variant="SkeletonVariant.Circular" Style="width: 24px; height: 24px;" />
                        <RadzenSkeleton Variant="SkeletonVariant.Circular" Style="width: 24px; height: 24px;" />
                        <RadzenSkeleton Variant="SkeletonVariant.Circular" Style="width: 24px; height: 24px;" />
                    </RadzenStack>
                </RadzenStack>
            </RadzenCard>
        </RadzenColumn>
        
        <RadzenColumn Size="4">
            <RadzenCard Variant="Variant.Outlined" class="rz-p-8">
                <RadzenStack Orientation="Orientation.Vertical" Gap="1rem">
                    <RadzenStack Orientation="Orientation.Horizontal" Gap="1rem" AlignItems="AlignItems.Center">
                        <RadzenSkeleton Style="width: 60px; height: 60px;" />
                        <RadzenStack Orientation="Orientation.Vertical" Gap="0.5rem">
                            <RadzenSkeleton Style="width: 100px; height: 16px;" />
                            <RadzenSkeleton Style="width: 60px; height: 14px;" />
                        </RadzenStack>
                    </RadzenStack>
                    
                    <RadzenSkeleton Style="width: 100%; height: 14px;" />
                    <RadzenSkeleton Style="width: 85%; height: 14px;" />
                    <RadzenSkeleton Style="width: 75%; height: 14px;" />
                    
                    <RadzenStack Orientation="Orientation.Horizontal" Gap="1rem" class="rz-mt-6">
                        <RadzenSkeleton Variant="SkeletonVariant.Rectangular" Style="width: 100px; height: 32px;" />
                        <RadzenSkeleton Variant="SkeletonVariant.Rectangular" Style="width: 60px; height: 32px;" />
                    </RadzenStack>
                </RadzenStack>
            </RadzenCard>
        </RadzenColumn>
    </RadzenRow>
</div>
```


### DataGrid Loading Example

This example demonstrates how to use skeletons to show loading states while a DataGrid is loading data. The skeleton mimics the actual DataGrid structure including headers, rows, and pagination.

```razor
@inherits DbContextPage

<style>
    .rz-datatable-loading-content {
        position: initial !important;
        left: initial !important;
        top: initial !important;
        transform: initial !important;
    }
</style>

<div class="rz-p-12">
    <RadzenStack Orientation="Orientation.Vertical" Gap="1rem">
        <RadzenText TextStyle="TextStyle.H6" TagName="TagName.P">DataGrid with Skeleton Loading</RadzenText>
        
        <RadzenButton Text="Toggle Loading" Click="@ToggleLoading" Style="margin-bottom: 20px;" />
        
        @if (isLoading)
        {
            @RenderLoading()
        }
        else
        {
            <RadzenDataGrid Style="height: 360px" @ref="grid" Count="@count" Data="@employees" LoadData="@LoadData"
                            AllowSorting="true" AllowFiltering="true" AllowPaging="true" PageSize="5" IsLoading="@isLoadingGrid"
                           PagerHorizontalAlign="HorizontalAlign.Center" ColumnWidth="200px">
                <Columns>
                    <RadzenDataGridColumn Property="@nameof(Employee.EmployeeID)" Filterable="false" Title="ID" Frozen="true" Width="80px" TextAlign="TextAlign.Center" />
                    <RadzenDataGridColumn Title="Photo" Frozen="true" Sortable="false" Filterable="false" Width="80px" TextAlign="TextAlign.Center" >
                        <Template Context="data">
                            <RadzenImage Path="@data.Photo" class="rz-gravatar" AlternateText="@(data.FirstName + " " + data.LastName)" />
                        </Template>
                    </RadzenDataGridColumn>
                    <RadzenDataGridColumn Property="@nameof(Employee.FirstName)" Title="First Name" Frozen="true" Width="160px"/>
                    <RadzenDataGridColumn Property="@nameof(Employee.LastName)" Title="Last Name" Width="160px"/>
                    <RadzenDataGridColumn Property="@nameof(Employee.Title)" Title="Job Title" Width="200px"/>
                    <RadzenDataGridColumn Property="@nameof(Employee.TitleOfCourtesy)" Title="Title" Width="120px" />
                    <RadzenDataGridColumn Property="@nameof(Employee.BirthDate)" Title="Birth Date" FormatString="{0:d}" Width="160px" />
                    <RadzenDataGridColumn Property="@nameof(Employee.HireDate)" Title="Hire Date" FormatString="{0:d}" Width="160px" />
                    <RadzenDataGridColumn Property="@nameof(Employee.Address)" Title="Address" Width="200px" />
                    <RadzenDataGridColumn Property="@nameof(Employee.City)" Title="City" Width="160px" />
                    <RadzenDataGridColumn Property="@nameof(Employee.Region)" Title="Region" Width="160px" />
                    <RadzenDataGridColumn Property="@nameof(Employee.PostalCode)" Title="Postal Code" Width="160px" />
                    <RadzenDataGridColumn Property="@nameof(Employee.Country)" Title="Country" Width="160px" />
                    <RadzenDataGridColumn Property="@nameof(Employee.HomePhone)" Title="Home Phone" Width="160px" />
                    <RadzenDataGridColumn Property="@nameof(Employee.Extension)" Title="Extension" Width="160px" />
                    <RadzenDataGridColumn Property="@nameof(Employee.Notes)" Title="Notes" Width="300px" />
                </Columns>
                <LoadingTemplate>
                    @RenderLoading()
                </LoadingTemplate>
            </RadzenDataGrid>
        }
    </RadzenStack>
</div>

@code {
    RadzenDataGrid<Employee> grid;
    int count;
    IEnumerable<Employee> employees;
    bool isLoading = false;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        // Start with loading state
        isLoading = true;
        await Task.Delay(2000); // Simulate loading delay
        await LoadData(new LoadDataArgs { Skip = 0, Top = 5 });
        isLoading = false;
    }

    async Task ToggleLoading()
    {
        isLoading = !isLoading;
        if (isLoading)
        {
            await Task.Delay(2000); // Simulate loading delay
            isLoading = false;
            StateHasChanged();
        }
    }

    bool isLoadingGrid = false;

    async Task LoadData(LoadDataArgs args)
    {
        isLoadingGrid = true;

        employees = Enumerable.Empty<Employee>();

        await Task.Yield();

        var query = dbContext.Employees.AsQueryable();

        if (!string.IsNullOrEmpty(args.Filter))
        {
            query = query.Where(grid.ColumnsCollection);
        }

        if (!string.IsNullOrEmpty(args.OrderBy))
        {
            query = query.OrderBy(args.OrderBy);
        }

        await Task.Delay(2000); // Simulate loading delay

        count = await Task.FromResult(query.Count());
        employees = await Task.FromResult(query.Skip(args.Skip.Value).Take(args.Top.Value).ToList());
    
        isLoadingGrid = false;
    }

    internal RenderFragment RenderLoading()
    {
        return __builder =>
        {
            <text>
                <!-- Skeleton loading state -->
                <RadzenTable Style="height: 360px">
                    <RadzenTableHeader>
                        <RadzenTableHeaderRow>
                            <RadzenTableHeaderCell Style="width: 300px;">
                                <RadzenStack Orientation="Orientation.Horizontal" JustifyContent="JustifyContent.SpaceBetween" AlignItems="AlignItems.Center" class="rz-p-0">
                                    <RadzenStack Orientation="Orientation.Horizontal" JustifyContent="JustifyContent.Start" AlignItems="AlignItems.Center" Gap="1.5rem">
                                        <RadzenSkeleton Style="width: 1rem;" />
                                        <RadzenSkeleton Style="width: var(--rz-gravatar-width);" />
                                        <RadzenSkeleton Style="width: 2rem;" />
                                    </RadzenStack>
                                    <RadzenSkeleton Variant="SkeletonVariant.Circular" Style="width: 1.25rem; height: 1.25rem" />
                                </RadzenStack>
                            </RadzenTableHeaderCell>
                        @for (int i = 0; i < 6; i++)
                        {
                            <RadzenTableHeaderCell Style="width: 160px;">
                                <RadzenStack Orientation="Orientation.Horizontal" JustifyContent="JustifyContent.SpaceBetween" AlignItems="AlignItems.Center" class="rz-p-0">
                                    <RadzenSkeleton Style="width: 50%;" />
                                    <RadzenSkeleton Variant="SkeletonVariant.Circular" Style="width: 1.25rem; height: 1.25rem" />
                                </RadzenStack>
                            </RadzenTableHeaderCell>
                        }
                        </RadzenTableHeaderRow>
                    </RadzenTableHeader>
                    <RadzenTableBody>
                    <!-- Table rows skeleton -->
                    @for (int i = 0; i < 5; i++)
                    {
                        <RadzenTableRow>
                            <RadzenTableCell>
                                <RadzenStack Orientation="Orientation.Horizontal" JustifyContent="JustifyContent.Start" AlignItems="AlignItems.Center" Gap="1.5rem">
                                    <RadzenSkeleton Style="width: 1rem;" />
                                    <RadzenSkeleton Variant="SkeletonVariant.Circular" Style="width: var(--rz-gravatar-width); height: var(--rz-gravatar-height);" />
                                    <RadzenSkeleton Animation="SkeletonAnimation.Wave" Style="width: 1.5rem; flex: 1;" />
                                </RadzenStack>
                            </RadzenTableCell>
                            <RadzenTableCell>
                                <RadzenSkeleton Animation="SkeletonAnimation.Wave" Style="width: 50%;" />
                            </RadzenTableCell>
                            <RadzenTableCell>
                                <RadzenSkeleton Animation="SkeletonAnimation.Wave" Style="width: 90%;" />
                            </RadzenTableCell>
                            <RadzenTableCell>
                                <RadzenSkeleton Animation="SkeletonAnimation.Wave" Style="width: 20%;" />
                            </RadzenTableCell>
                            <RadzenTableCell>
                                <RadzenSkeleton Animation="SkeletonAnimation.Wave" Style="width: 70%;" />
                            </RadzenTableCell>
                            <RadzenTableCell>
                                <RadzenSkeleton Animation="SkeletonAnimation.Wave" Style="width: 80%;" />
                            </RadzenTableCell>
                            <RadzenTableCell>
                                <RadzenSkeleton Animation="SkeletonAnimation.Wave" Style="width: 80%;" />
                            </RadzenTableCell>
                        </RadzenTableRow>
                    }
                    </RadzenTableBody>
                    <RadzenTableBody>
                    <!-- Pager -->
                        <RadzenTableRow>
                            <RadzenTableCell colspan="7">
                                <RadzenStack Orientation="Orientation.Horizontal" JustifyContent="JustifyContent.Center" class="rz-p-5">
                                    <RadzenSkeleton Style="width: 1.5rem; height: 1.5rem" />
                                    <RadzenSkeleton Style="width: 1.5rem; height: 1.5rem" />
                                    <RadzenSkeleton Style="width: 1.5rem; height: 1.5rem" />
                                    <RadzenSkeleton Style="width: 1.5rem; height: 1.5rem" />
                                    <RadzenSkeleton Style="width: 1.5rem; height: 1.5rem" />
                                </RadzenStack>
                            </RadzenTableCell>
                        </RadzenTableRow>
                    </RadzenTableBody>
                </RadzenTable>
            </text>
        };
    }
}
```
