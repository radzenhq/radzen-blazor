# DataGrid: Filter Template

This example demonstrates how to define custom RadzenDataGrid column filter template.

Keywords: datagrid, column, filter, template

> API reference: [RadzenDataGrid API](https://blazor.radzen.com/api/datagrid.md)

## Examples

## DataGrid custom Column FilterTemplate

Build a custom Blazor DataGrid column filter with FilterTemplate - replace the default filter UI with your own controls, like a slider, date range, or dropdown.

```razor
@if (employees == null)
{
    <p><em>Loading...</em></p>
}
else
{
    <RadzenText TextStyle="TextStyle.H6" TagName="TagName.H2" class="rz-my-6">Custom filtering template with IQueryable binding</RadzenText>
        <RadzenDataGrid @ref="grid" Data=@employees FilterMode="FilterMode.Simple" AllowFiltering="true" AllowPaging="true" AllowSorting="true" ColumnWidth="200px">
        <Columns>
            <RadzenDataGridColumn Property="ID" Title="ID" FilterValue="@idValue">
                <FilterTemplate>
                    <RadzenNumeric @bind-Value=idValue ShowUpDown=false Style="width:100%" InputAttributes="@(new Dictionary<string,object>(){ { "aria-label", "filter by ID" }})" />
                </FilterTemplate>
            </RadzenDataGridColumn>
            <RadzenDataGridColumn Title="Customer" Property="CompanyName" Type="typeof(IEnumerable<string>)" 
                    FilterValue="@selectedCompanyNames" FilterOperator="FilterOperator.Contains" LogicalFilterOperator="LogicalFilterOperator.Or">
                <FilterTemplate>
                    <RadzenDropDown @bind-Value=@selectedCompanyNames Style="width:100%;" InputAttributes="@(new Dictionary<string,object>(){ { "aria-label", "filter by Company" }})"
                        Change=@OnSelectedCompanyNamesChange Data="@(companyNames)" AllowClear="true" Multiple="true" />
                </FilterTemplate>
            </RadzenDataGridColumn>
            <RadzenDataGridColumn Title="Hire Date" Property="HireDate.Date" FormatString="{0:d}"
                                  FilterValue="@hireDate?.Date">
                <FilterTemplate>
                    <RadzenDatePicker @bind-Value=@hireDate Style="width:100%;" AllowClear="true" DateFormat="d" InputAttributes="@(new Dictionary<string,object>(){ { "aria-label", "filter by Hire Date" }})" />
                </FilterTemplate>
            </RadzenDataGridColumn>
            <RadzenDataGridColumn Title="Leave Date" Property="LeaveDate" FormatString="{0:d}" />
            <RadzenDataGridColumn Title="WorkStatus" Property="WorkStatus" Type="typeof(IEnumerable<WorkStatus>)" Sortable=false
                                  FilterValue="@selectedWorkStatus" FilterOperator="FilterOperator.In" LogicalFilterOperator="LogicalFilterOperator.Or">
                <FilterTemplate>
                    <RadzenDropDown @bind-Value=@selectedWorkStatus Multiple="true" AllowSelectAll=false Style="width:100%;" InputAttributes="@(new Dictionary<string,object>(){ { "aria-label", "filter by WorkStatus" }})"
                                    Change=@OnSelectedWorkStatusChange Data=@allWorkStatuses TextProperty="Text" ValueProperty="Value" />
                </FilterTemplate>
                <Template>
                    @(string.Join(',', context.WorkStatus.Select(i => Enum.GetName((WorkStatus)i))))
                </Template>
            </RadzenDataGridColumn>
            <RadzenDataGridColumn Title="WorkStatus (as Model)" Property="WorkStatuses" Type="typeof(IEnumerable<string>)" Sortable=false
                                  FilterValue="@selectedWorkStatuses" FilterOperator="FilterOperator.In" FilterProperty="Name" LogicalFilterOperator="LogicalFilterOperator.Or">
                <FilterTemplate>
                    <RadzenDropDown @bind-Value=@selectedWorkStatuses Multiple="true" AllowSelectAll=false Style="width:100%;" InputAttributes="@(new Dictionary<string,object>(){ { "aria-label", "filter by WorkStatus from model" }})"
                                    Change=@OnSelectedWorkStatusesChange Data=@allWorkStatusesObject TextProperty="Name" ValueProperty="Name" />
                </FilterTemplate>
                <Template>
                    @(string.Join(',', context.WorkStatuses.Select(s => s.Name)))
                </Template>
            </RadzenDataGridColumn>
            <RadzenDataGridColumn Property="@nameof(Employee.TitleOfCourtesy)" Title="Title Of Courtesy" 
                FilterValue="@currentTOC">
                <FilterTemplate>
                    <RadzenDropDown @bind-Value="@currentTOC" TextProperty="Text" ValueProperty="Value" Style="width:100%;" InputAttributes="@(new Dictionary<string,object>(){ { "aria-label", "filter by title of courtesy" }})"
                                    Change=@OnSelectedTOCChange
                                    Data="@(Enum.GetValues(typeof(TitleOfCourtesy)).Cast<TitleOfCourtesy?>().Select(t => new { Text = $"{t}", Value = t == TitleOfCourtesy.All ? null : t }))" />
                </FilterTemplate>
            </RadzenDataGridColumn>
        </Columns>
    </RadzenDataGrid>
}

@code {
    RadzenDataGrid<Employee> grid;
    int? idValue;
    DateTime? hireDate;
    TitleOfCourtesy? currentTOC;
    IEnumerable<string> selectedCompanyNames;
    IEnumerable<WorkStatus> selectedWorkStatus;
    IEnumerable<object> allWorkStatuses = Enum.GetValues(typeof(WorkStatus)).Cast<WorkStatus>().Select(t => new { Text = $"{t}", Value = t });

    List<string> companyNames = new List<string> {"Vins et alcools Chevalier", "Toms Spezialitäten", "Hanari Carnes", "Richter Supermarkt", "Wellington Importadora", "Centro commercial Moctezuma" };

    public enum TitleOfCourtesy
    {
        Ms,
        Mr,
        All = -1
    }

    public enum WorkStatus
    {
        Office,
        Remote,
    }

    public class WorkStatusModel
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; }

        public WorkStatusModel(string name) =>
            Name = name;                   
    }

    public class Employee
    {
        public int ID { get; set; }
        public string CompanyName { get; set; }
        public DateTime HireDate { get; set; }
        public DateOnly LeaveDate { get; set; }
        public TitleOfCourtesy TitleOfCourtesy { get; set; }
        public IEnumerable<WorkStatus> WorkStatus { get; set; }
        public IEnumerable<WorkStatusModel> WorkStatuses { get; set; }
    }

    IEnumerable<string> selectedWorkStatuses;
    IEnumerable<WorkStatusModel> allWorkStatusesObject = Enum.GetValues(typeof(WorkStatus)).Cast<WorkStatus>().Select(t => new WorkStatusModel($"{t}"));

    void OnSelectedWorkStatusesChange(object value)
    {
        if (selectedWorkStatuses != null && !selectedWorkStatuses.Any())
        {
            selectedWorkStatuses = null;
        }
    }

    void OnSelectedCompanyNamesChange(object value)
    {
        if (selectedCompanyNames != null && !selectedCompanyNames.Any())
        {
            selectedCompanyNames = null;  
        }
    }

    void OnSelectedWorkStatusChange(object value)
    {
        if (selectedWorkStatus != null && !selectedWorkStatus.Any())
        {
            selectedWorkStatus = null;
        }
    }

    void OnSelectedTOCChange(object value)
    {
        if (currentTOC == TitleOfCourtesy.All)
        {
            currentTOC = null;
        }
    }

    IEnumerable<Employee> employees; 

    protected override async Task OnInitializedAsync()
    {
        employees = await Task.FromResult(Enumerable.Range(0, 10).Select(i =>
            new Employee
            {
                ID = i,
                CompanyName = i < 4 ? companyNames[0] : companyNames[i - 4],
                HireDate = DateTime.Now.AddMonths(-i),
                    LeaveDate = DateOnly.FromDateTime(DateTime.Now.AddMonths(+i)),
                TitleOfCourtesy = i < 5 ? TitleOfCourtesy.Mr : TitleOfCourtesy.Ms,
                    WorkStatus = i < 3 ?
                            new WorkStatus[] { WorkStatus.Office } :
                                            i < 5 ? new WorkStatus[] { WorkStatus.Remote } : new WorkStatus[] { WorkStatus.Office, WorkStatus.Remote },
                    WorkStatuses = i < 3 ?  new WorkStatusModel[] { new WorkStatusModel(Enum.GetName(WorkStatus.Office)) } :
                                                i < 5 ? new WorkStatusModel[] { new WorkStatusModel(Enum.GetName(WorkStatus.Remote)) } : new WorkStatusModel[] { new WorkStatusModel(Enum.GetName(WorkStatus.Office)), new WorkStatusModel(Enum.GetName(WorkStatus.Remote)) }
            }).AsQueryable());
    }
}
```


```razor
@if (employees == null)
{
    <p><em>Loading...</em></p>
}
else
{
    <RadzenText TextStyle="TextStyle.H6" TagName="TagName.H2" class="rz-my-6">Custom filtering template with Custom Command</RadzenText>
    <RadzenDataGrid Render="@OnRender" Style="height:400px;" AllowVirtualization="true" @ref="grid" Data=@employees FilterMode="FilterMode.Advanced"
                    AllowFiltering="true" AllowPaging="false" AllowSorting="true" ColumnWidth="200px">
        <Columns>
            <RadzenDataGridColumn Property="ID" Title="ID" FilterValue="@idValue">
            </RadzenDataGridColumn>
            <RadzenDataGridColumn Title="Customer" Property="CompanyName">
            </RadzenDataGridColumn>
            <RadzenDataGridColumn Title="Hire Date" Property="HireDate.Date" FormatString="{0:d}"
                                  FilterValue="@hireDate?.Date">
            </RadzenDataGridColumn>
            <RadzenDataGridColumn Title="Work Status" Property="WorkStatus" Type="typeof(IEnumerable<string>)" Sortable=true
                                  FilterValue="@selectedWorkStatus" FilterOperator="FilterOperator.Custom" LogicalFilterOperator="@workStatusOperator">
                <FilterTemplate Context="column">
                    <div class="rz-grid-filter">
                        <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Stretch" JustifyContent="JustifyContent.SpaceBetween">
                            <span class="rz-grid-filter-label">Filter</span>
                            <RadzenSelectBar Style="margin-top:-6px;" Change="@(()=>OnSelectedWorkStatusChange(column))" @bind-Value=@workStatusOperator TValue="LogicalFilterOperator">
                                <Items>
                                    <RadzenSelectBarItem Text="Any" Value="@(LogicalFilterOperator.Or)" />
                                    <RadzenSelectBarItem Text="All" Value="@(LogicalFilterOperator.And)" />
                                </Items>
                            </RadzenSelectBar>
                        </RadzenStack>
                        <RadzenListBox AllowClear="true" @bind-Value=@selectedWorkStatus Multiple="true" AllowSelectAll=true Style="width:100%; height:200px;"
                                       Change="@(()=>OnSelectedWorkStatusChange(column))" Data=@workStatuses />
                        <div class="rz-grid-filter-buttons">
                            <RadzenButton ButtonStyle="ButtonStyle.Secondary" Text="Clear" Click="@(() => WorkStatusFilterClear(column))" />
                            <RadzenButton ButtonStyle="ButtonStyle.Primary" Text="Close" Click="@(() => WorkStatusFilterClose(column))" />
                        </div>
                    </div>
                </FilterTemplate>
            </RadzenDataGridColumn>
            <RadzenDataGridColumn Property="@nameof(Employee.TitleOfCourtesy)" Title="Title Of Courtesy" 
                FilterValue="@currentTOC">
                <FilterTemplate>
                    <RadzenListBox @bind-Value="@currentTOC" TextProperty="Text" ValueProperty="Value" Style="width:100%;" InputAttributes="@(new Dictionary<string,object>(){ { "aria-label", "filter by title of courtesy" }})"
                                    Change=@OnSelectedTOCChange
                                    Data="@(Enum.GetValues(typeof(TitleOfCourtesy)).Cast<TitleOfCourtesy?>().Select(t => new { Text = $"{t}", Value = t == TitleOfCourtesy.All ? null : t }))" />
                </FilterTemplate>
            </RadzenDataGridColumn>
        </Columns>
    </RadzenDataGrid>
}

@code {
    RadzenDataGrid<Employee> grid;
    int? idValue = null;
    DateTime? hireDate = null;
    TitleOfCourtesy? currentTOC;
    List<string> selectedWorkStatus;
    string whereExpression;

    public LogicalFilterOperator workStatusOperator { get; set; } = LogicalFilterOperator.Or;

    List<string> companyNames = new List<string> {"Vins et alcools Chevalier", "Toms Spezialitäten", "Hanari Carnes", "Richter Supermarkt", "Wellington Importadora", "Centro commercial Moctezuma" };
    List<string> workStatuses = new List<string> { "Office", "Remote", "Temporary", "Permanent" };
    public enum TitleOfCourtesy
    {
        Ms,
        Mr,
        All = -1
    }

    public class Employee
    {
        public int ID { get; set; }
        public string CompanyName { get; set; }
        public DateTime HireDate { get; set; }
        public TitleOfCourtesy TitleOfCourtesy { get; set; }
        public string WorkStatus { get; set; }
    }

    void OnSelectedTOCChange(object value)
    {
        if (currentTOC == TitleOfCourtesy.All)
        {
            currentTOC = null;
        }
    }

    IEnumerable<Employee> employees; 

    protected override async Task OnInitializedAsync()
    {
        employees = await Task.FromResult(Enumerable.Range(0, 60).Select(i =>
            new Employee
            {
                ID = i,
                CompanyName = companyNames[i % 6],
                HireDate = DateTime.Now.AddMonths(-i),
                TitleOfCourtesy = i % 2 == 1 ? TitleOfCourtesy.Mr : TitleOfCourtesy.Ms,
                    WorkStatus = $"{(i < 20 ? "Office" : i >= 20 && i < 40 ? "Remote" : "Office, Remote")} {(i < 12 ? ", Temporary" : i >= 12 && i < 30 ? ", Permanent" : i > 30 && i < 44 ? ", Temporary" : ", Permanent")}"
            }).AsQueryable());
    }

    private async Task WorkStatusFilterClear(RadzenDataGridColumn<Employee> column)
    {
        if(selectedWorkStatus != null)
        {
            selectedWorkStatus = null;
            await OnSelectedWorkStatusChange(column);            
        }
        await column.CloseFilter();
    }

    private async Task WorkStatusFilterClose(RadzenDataGridColumn<Employee> column)
    {
        await column.CloseFilter();
    }

    async Task OnSelectedWorkStatusChange(RadzenDataGridColumn<Employee> column)
    {
        var createWhereExpression = new List<string>();

        if (selectedWorkStatus?.Any() == true)
        {
            whereExpression = string.Join($" {Enum.GetName(typeof(LogicalFilterOperator), workStatusOperator).ToLowerInvariant()} ",
                selectedWorkStatus.Select(s => $"it.WorkStatus.Contains(\"{s}\")"));
        }
        else
        {
            whereExpression = null;
            selectedWorkStatus = null;
        }

        await column.SetCustomFilterExpressionAsync(whereExpression);
    }

    void OnRender(DataGridRenderEventArgs<Employee> args)
    {
        if (args.FirstRender)
        {
            args.Grid.Sorts.Add(new SortDescriptor() { Property = "CompanyName", SortOrder = SortOrder.Ascending });
            args.Grid.Sorts.Add(new SortDescriptor() { Property = "TitleOfCourtesy", SortOrder = SortOrder.Ascending });
        }
    }
}
```
