# Steps

The Blazor Steps component guides users through a multi-step process (wizard) with numbered stages.

Keywords: step, steps, wizard, transition, animation

> API reference: [RadzenSteps API](https://blazor.radzen.com/api/steps.md)

## Examples

## Blazor Steps

The Blazor Steps component guides users through a multi-step process (wizard) with numbered stages.

```razor
@inherits DbContextPage

<RadzenSteps Change=@OnChange>
    <Steps>
        <RadzenStepsItem Text="Customers">
            <RadzenText TextStyle="TextStyle.H5" TagName="TagName.H3" class="rz-my-6">1. Select a Customer to continue</RadzenText>
            <RadzenDataGrid ColumnWidth="200px" AllowFiltering="true" AllowPaging="true" AllowSorting="true" Data="@customers" TItem="Customer" @bind-Value="@selectedCustomers">
                <Columns>
                    <RadzenDataGridColumn Property="CustomerID" Title="Customer ID" Width="140px" />
                    <RadzenDataGridColumn Property="CompanyName" Title="Company Name" />
                    <RadzenDataGridColumn Property="ContactName" Title="Contact Name" />
                    <RadzenDataGridColumn Property="ContactTitle" Title="Contact Title" />
                    <RadzenDataGridColumn Property="@nameof(Employee.Address)" Title="Address" />
                    <RadzenDataGridColumn Property="@nameof(Employee.City)" Title="City" Width="140px" />
                    <RadzenDataGridColumn Property="@nameof(Employee.Region)" Title="Region" Width="140px" />
                    <RadzenDataGridColumn Property="@nameof(Employee.PostalCode)" Title="Postal Code" Width="140px" />
                    <RadzenDataGridColumn Property="@nameof(Employee.Country)" Title="Country" Width="140px" />
                    <RadzenDataGridColumn Property="Phone" Title="Phone" Width="140px" />
                    <RadzenDataGridColumn Property="Fax" Title="Fax" Width="140px" />
                </Columns>
            </RadzenDataGrid>
        </RadzenStepsItem>
        <RadzenStepsItem Text="Orders" Disabled="@(selectedCustomers == null || selectedCustomers != null && !selectedCustomers.Any())">
            <RadzenText TextStyle="TextStyle.H5" TagName="TagName.H3" class="rz-my-6">2. Select an Order to continue</RadzenText>
            <RadzenDataGrid ColumnWidth="150px" PageSize="5" AllowFiltering="true" AllowPaging="true" AllowSorting="true" 
                        Data="@ordersByCustomers" @bind-Value="@selectedOrders">
                <Columns>
                    <RadzenDataGridColumn Width="120px" Property="OrderID" Title="Order ID" />
                    <RadzenDataGridColumn Width="200px" Property="Customer.CompanyName" Title="Customer" />
                    <RadzenDataGridColumn Property="Employee.LastName" Title="Employee">
                        <Template Context="order">
                            <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="0.5rem">
                                <RadzenImage Path="@order.Employee?.Photo" style="width: 40px; height: 40px; border-radius: 8px;" AlternateText="@(order.Employee?.FirstName + " " + order.Employee?.LastName)" />
                                <span>@order.Employee?.LastName</span>
                            </RadzenStack>
                        </Template>
                    </RadzenDataGridColumn>
                    <RadzenDataGridColumn Property="@nameof(Order.OrderDate)" Title="Order Date">
                        <Template Context="order">
                            @String.Format("{0:d}", order.OrderDate)
                        </Template>
                    </RadzenDataGridColumn>
                    <RadzenDataGridColumn Property="@nameof(Order.RequiredDate)" Title="Required Date">
                        <Template Context="order">
                            @String.Format("{0:d}", order.RequiredDate)
                        </Template>
                    </RadzenDataGridColumn>
                    <RadzenDataGridColumn Property="@nameof(Order.ShippedDate)" Title="Shipped Date">
                        <Template Context="order">
                            @String.Format("{0:d}", order.ShippedDate)
                        </Template>
                    </RadzenDataGridColumn>
                    <RadzenDataGridColumn Property="@nameof(Order.Freight)" Title="Freight">
                        <Template Context="order">
                            @String.Format(new System.Globalization.CultureInfo("en-US"), "{0:C}", order.Freight)
                        </Template>
                    </RadzenDataGridColumn>
                    <RadzenDataGridColumn Property="@nameof(Order.ShipName)" Title="Ship Name" />
                    <RadzenDataGridColumn Property="ShipAddress" Title="Address" />
                    <RadzenDataGridColumn Property="@nameof(Order.ShipCity)" Title="City" />
                    <RadzenDataGridColumn Property="ShipRegion" Title="Region" />
                    <RadzenDataGridColumn Property="ShipPostalCode" Title="Postal Code" />
                    <RadzenDataGridColumn Property="@nameof(Order.ShipCountry)" Title="Country" />
                </Columns>
            </RadzenDataGrid>
        </RadzenStepsItem>
        <RadzenStepsItem Text="Order Details" Disabled="@(selectedOrders == null || selectedOrders != null && !selectedOrders.Any())">
            <RadzenText TextStyle="TextStyle.H5" TagName="TagName.H3" class="rz-my-6">Order Details</RadzenText>
            <RadzenDataGrid AllowFiltering="true" AllowPaging="true" AllowSorting="true"
                        Data="@(orderDetailsByOrders)" ColumnWidth="200px">
                <Columns>
                    <RadzenDataGridColumn Property="Product.ProductName" Title="Product" />
                    <RadzenDataGridColumn Property="@nameof(OrderDetail.Quantity)" Title="Quantity" />
                    <RadzenDataGridColumn Property="@nameof(OrderDetail.Discount)" Title="Discount" FormatString="{0:P}" />
                </Columns>
            </RadzenDataGrid>
        </RadzenStepsItem>
    </Steps>
</RadzenSteps>

<EventConsole @ref=@console />

@code {
    EventConsole console;
    IEnumerable<Customer> customers;
    IEnumerable<Order> orders;
    IEnumerable<OrderDetail> orderDetails;

    IList<Customer> selectedCustomers;
    IList<Order> selectedOrders;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        customers = dbContext.Customers.ToList();
        orders = dbContext.Orders.Include("Customer").Include("Employee").ToList();
        orderDetails = dbContext.OrderDetails.Include("Product").ToList();
    }

    IEnumerable<Order> ordersByCustomers;
    IEnumerable<OrderDetail> orderDetailsByOrders;

    void OnChange(int index)
    {
        console.Log($"Step with index {index} was selected.");

        if (index == 1)
        {
            ordersByCustomers = selectedCustomers != null && selectedCustomers.Any() ? orders.Where(o => o.CustomerID == selectedCustomers[0].CustomerID) : Enumerable.Empty<Order>();
        }
        else if (index == 2)
        {
            orderDetailsByOrders = selectedOrders != null && selectedOrders.Any() ? orderDetails.Where(o => o.OrderID == selectedOrders[0].OrderID) : Enumerable.Empty<OrderDetail>();
        }
    }
}
```


### Transition

Use the `Transition` parameter to animate the step content when switching between steps. Set it to `StepsTransition.Fade` for a fade-in effect or `StepsTransition.Slide` for a slide-in effect. Use `TransitionDuration` to control the animation speed.

```razor
<RadzenStack Gap="2rem">
    <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="1rem">
        <RadzenText TextStyle="TextStyle.Body1">Transition:</RadzenText>
        <RadzenDropDown @bind-Value="transition" Data="transitions" TextProperty="Text" ValueProperty="Value" Style="width: 150px;" />
        <RadzenText TextStyle="TextStyle.Body1">Duration (ms):</RadzenText>
        <RadzenNumeric @bind-Value="duration" Min="100" Max="2000" Step="50" Style="width: 120px;" />
    </RadzenStack>
    <RadzenSteps Transition="@transition" TransitionDuration="@duration">
        <Steps>
            <RadzenStepsItem Text="Personal Info">
                <RadzenStack Gap="1rem" class="rz-p-4">
                    <RadzenText TextStyle="TextStyle.Subtitle1" TagName="TagName.P">Enter your personal information</RadzenText>
                    <RadzenTextBox Placeholder="First name" />
                    <RadzenTextBox Placeholder="Last name" />
                    <RadzenTextBox Placeholder="Email" />
                </RadzenStack>
            </RadzenStepsItem>
            <RadzenStepsItem Text="Address">
                <RadzenStack Gap="1rem" class="rz-p-4">
                    <RadzenText TextStyle="TextStyle.Subtitle1" TagName="TagName.P">Enter your address</RadzenText>
                    <RadzenTextBox Placeholder="Street" />
                    <RadzenTextBox Placeholder="City" />
                    <RadzenTextBox Placeholder="Zip code" />
                </RadzenStack>
            </RadzenStepsItem>
            <RadzenStepsItem Text="Confirmation">
                <RadzenStack Gap="1rem" class="rz-p-4">
                    <RadzenText TextStyle="TextStyle.Subtitle1" TagName="TagName.P">Review and confirm</RadzenText>
                    <RadzenText>Please review your information and click submit.</RadzenText>
                </RadzenStack>
            </RadzenStepsItem>
        </Steps>
    </RadzenSteps>
</RadzenStack>

@code {
    StepsTransition transition = StepsTransition.Fade;
    int duration = 1000;

    record TransitionOption(string Text, StepsTransition Value);

    List<TransitionOption> transitions = new()
    {
        new("None", StepsTransition.None),
        new("Fade", StepsTransition.Fade),
        new("Slide", StepsTransition.Slide),
    };
}
```


### CanChange event

The `CanChange` event allows you to conditionally prevent the step change. Use it to ensure your users have entered all the required information before moving to the next step.

```razor
<RadzenSteps Change="@OnChange" CanChange="@CanChange">
    <Steps>
        <RadzenStepsItem>
            <RadzenRow>
                <RadzenText Text="Enter user information"/>
            </RadzenRow>
            <RadzenTextBox Placeholder="Name" @bind-Value="name"/>
            <RadzenTextBox Placeholder="Address" @bind-Value="address"/>
            <RadzenButton Text="Save" Click="@SaveNameAndAdress" />
        </RadzenStepsItem>
        <RadzenStepsItem>
            <RadzenRow>
                <RadzenText Text="Enter about me" />
            </RadzenRow>
            <RadzenTextArea Placeholder="About me" @bind-Value="aboutMe" />
            <RadzenButton Text="Save" Click="@SaveAboutMe" />
        </RadzenStepsItem>
        <RadzenStepsItem>
            <RadzenRow>
                <RadzenText Text="Add your hobbies" />
            </RadzenRow>
            <RadzenDataGrid TItem="Hobby"
                            Data="@hobbies"
                            @bind-Value="@selectedHobbies"
                            SelectionMode="DataGridSelectionMode.Multiple"
                            AllowRowSelectOnRowClick="true" >
                <Columns>
                    <RadzenDataGridColumn Context="hobby">
                         <Template>
                            <RadzenCheckBox Value="selectedHobbies.Contains(hobby)" TValue="bool"/>
                        </Template>
                    </RadzenDataGridColumn>
                    <RadzenDataGridColumn Title="Hobby" Property="@nameof(Hobby.HobbyName)" />
                </Columns>
            </RadzenDataGrid>
            <RadzenButton Text="Save" Click="@SaveHobbies" />
        </RadzenStepsItem>
    </Steps>
</RadzenSteps>

@code {
    private string name;
    private string savedName;

    private string address;
    private string savedAddress;

    private string aboutMe;
    private string savedAboutMe;

    private List<Hobby> hobbies = new List<Hobby>() { new("Games"), new("Sport"), new("Movies"), new("Books"), new("Music") };
    private IList<Hobby> selectedHobbies = new List<Hobby>();
    private List<Hobby> savedHobbies = new List<Hobby>();

    private void OnChange()
    {
        name = savedName;
        address = savedAddress;
        aboutMe = savedAboutMe;
        selectedHobbies = savedHobbies;
    }

    private async Task CanChange(StepsCanChangeEventArgs args)
    {
        if (args.SelectedIndex == 0 && savedName == name && savedAddress == address)
        {
            return;
        }

        if (args.SelectedIndex == 1 && savedAboutMe == aboutMe)
        {
            return;
        }

        if (args.SelectedIndex == 2 && savedHobbies.SequenceEqual(selectedHobbies))
        {
            return;
        }

        var response = await DialogService.Confirm(
            "Are you sure you want to continue without saving?",
            "Confirm",
            new ConfirmOptions()
            {
                CloseDialogOnEsc = false,
                CloseDialogOnOverlayClick = false,
                ShowClose = false,
                CancelButtonText = "No",
                OkButtonText = "Yes",
            });

        if (response == false)
        {
            args.PreventDefault();
        }
    }

    private void SaveNameAndAdress()
    {
        savedName = name;
        savedAddress = address;
    }

    private void SaveAboutMe()
    {
        savedAboutMe = aboutMe;
    }

    private void SaveHobbies()
    {
        savedHobbies = selectedHobbies.ToList();
    }

    private class Hobby
    {
        public Hobby(string hobbyName)
        {
            HobbyName = hobbyName;
        }

        public string HobbyName { get; set; }

        public override bool Equals(object obj)
        {
            return obj is Hobby hobby && hobby.HobbyName == HobbyName;
        }

        public override int GetHashCode()
        {
            return HobbyName.GetHashCode();
        }
    }
}
```
