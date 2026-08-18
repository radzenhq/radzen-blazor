# Carousel

The Blazor Carousel cycles through content - images or any markup - with navigation arrows and paging.

Keywords: carousel, gallery, slide, deck, container

> API reference: [RadzenCarousel API](https://blazor.radzen.com/api/carousel.md)

## Examples

## Blazor Carousel

The Blazor Carousel cycles through content - images or any markup - with navigation arrows and paging.

```razor
@inherits DbContextPage

<RadzenStack class="rz-p-0 rz-p-md-12">
    <RadzenCard class="rz-p-4" Variant="Variant.Outlined">
        <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Start" Wrap="FlexWrap.Wrap">
            <RadzenStack Orientation="Orientation.Vertical" Gap="4px">
                Selected index
                <RadzenNumeric @bind-Value=@selectedIndex Min="0" Max="@(orderIDs.Count() - 1)" aria-label="selected index" />
            </RadzenStack>
            <RadzenStack Orientation="Orientation.Vertical" Gap="4px">
                Interval
                <RadzenNumeric @bind-Value=@interval aria-label="interval" />
            </RadzenStack>
            <RadzenStack Orientation="Orientation.Vertical" Gap="4px">
                Animation duration
                <RadzenNumeric @bind-Value=@animationDuration TValue="double?" aria-label="animation duration" />
            </RadzenStack>
            <RadzenStack Orientation="Orientation.Vertical" Gap="4px">
                Items per page
                <RadzenNumeric @bind-Value=@itemsPerPage Min="1" Max="6" aria-label="items per page" />
            </RadzenStack>
            <RadzenStack Orientation="Orientation.Vertical" Gap="8px">
                Auto-cycle
                <RadzenSwitch @bind-Value="@auto" InputAttributes="@(new Dictionary<string,object>(){ { "aria-label", "auto-cycle" }})" />
            </RadzenStack>
            <RadzenStack Orientation="Orientation.Vertical" Gap="8px">
                Navigate
                <RadzenStack Orientation="Orientation.Horizontal" Gap="16px">
                    <RadzenToggleButton Text="@toggleText" Click="@Toggle" ButtonStyle="ButtonStyle.Base" Variant="Variant.Flat" Size="ButtonSize.Small" />
                    <RadzenButton Text="Go to first" Click="@(args => carousel.Navigate(0))" ButtonStyle="ButtonStyle.Base" Variant="Variant.Flat" Size="ButtonSize.Small" Disabled="@(selectedIndex == 0)" />
                    <RadzenButton Text="Go to last" Click="@(args => carousel.Navigate(orderIDs.Count() - 1))" ButtonStyle="ButtonStyle.Base" Variant="Variant.Flat" Size="ButtonSize.Small"  Disabled="@(selectedIndex == orderIDs.Count() - 1)" />
                </RadzenStack>
            </RadzenStack>
        </RadzenStack>
    </RadzenCard>
    <RadzenCard class="rz-p-4" Variant="Variant.Outlined">
        <RadzenCarousel @ref=carousel @bind-SelectedIndex="@selectedIndex" Auto="@auto" Interval="@interval" AnimationDuration="@animationDuration" ItemsPerPage="@itemsPerPage" PagerOverlay="false" ButtonShade="Shade.Default" Style="height:500px"
                Change="@(args => console.Log($"SelectedIndex changed to {args}"))">
            <Items>
                @foreach (var orderID in orderIDs)
                {
                <RadzenCarouselItem>
                    <RadzenCard class="rz-w-75">
                        <DialogCardPage OrderID=@orderID />
                    </RadzenCard>
                </RadzenCarouselItem>
                }
            </Items>
        </RadzenCarousel>
    </RadzenCard>
</RadzenStack>

<EventConsole @ref=@console Style="min-height: 230px;" />

@code {
    RadzenCarousel carousel;

    bool auto = true;
    double interval = 4000;
    double? animationDuration;
    int itemsPerPage = 1;
    string toggleText = "Stop";

    bool started = true;
    void Toggle()
    {
        if (started)
        {
            carousel.Stop();
            toggleText = "Start";
        }
        else
        {
            carousel.Start();
            toggleText = "Stop";
        }

        started = !started;
    }

    EventConsole console;

    int selectedIndex;

    IQueryable<int> orderIDs;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        orderIDs = dbContext.Orders.Select(o => o.OrderID).Take(10);
    }
}
```


### Multiple items per page

Use `ItemsPerPage` to display multiple items at the same time.

```razor
<RadzenStack class="rz-p-0 rz-p-md-12">
    <RadzenCard class="rz-p-4" Variant="Variant.Outlined">
        <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Start" Wrap="FlexWrap.Wrap">
            <RadzenStack Orientation="Orientation.Vertical" Gap="4px">
                Items per page
                <RadzenNumeric @bind-Value=@itemsPerPage Min="1" Max="6" aria-label="items per page" />
            </RadzenStack>
            <RadzenStack Orientation="Orientation.Vertical" Gap="4px">
                Auto-cycle
                <RadzenSwitch @bind-Value="@auto" InputAttributes="@(new Dictionary<string,object>(){ { "aria-label", "auto-cycle" }})" />
            </RadzenStack>
        </RadzenStack>
    </RadzenCard>
    <RadzenCarousel @bind-SelectedIndex="@selectedIndex" Auto="@auto" Interval="4000"
        ItemsPerPage="@itemsPerPage" PagerOverlay="false" Style="height: 400px;">
        <Items>
            <RadzenCarouselItem>
                <RadzenImage Path="images/gallery/1.jpg" Style="width: 100%; height: 100%; object-fit: cover; padding: 0.5rem;" />
            </RadzenCarouselItem>
            <RadzenCarouselItem>
                <RadzenImage Path="images/gallery/2.jpg" Style="width: 100%; height: 100%; object-fit: cover; padding: 0.5rem;" />
            </RadzenCarouselItem>
            <RadzenCarouselItem>
                <RadzenImage Path="images/gallery/3.jpg" Style="width: 100%; height: 100%; object-fit: cover; padding: 0.5rem;" />
            </RadzenCarouselItem>
            <RadzenCarouselItem>
                <RadzenImage Path="images/gallery/4.jpg" Style="width: 100%; height: 100%; object-fit: cover; padding: 0.5rem;" />
            </RadzenCarouselItem>
            <RadzenCarouselItem>
                <RadzenImage Path="images/gallery/5.jpg" Style="width: 100%; height: 100%; object-fit: cover; padding: 0.5rem;" />
            </RadzenCarouselItem>
            <RadzenCarouselItem>
                <RadzenImage Path="images/gallery/6.jpg" Style="width: 100%; height: 100%; object-fit: cover; padding: 0.5rem;" />
            </RadzenCarouselItem>
        </Items>
    </RadzenCarousel>
</RadzenStack>

@code {
    int itemsPerPage = 3;
    bool auto = false;
    int selectedIndex;
}
```


### Navigation button styles

Easily change the look and feel of next/prev navigation buttons via `ButtonStyle`, `Shade`, `Variant`, and `Size`.

```razor
<RadzenStack class="rz-p-0 rz-p-md-12">
    <RadzenCard class="rz-p-4" Variant="Variant.Outlined">
        <RadzenStack Orientation="Orientation.Vertical">
            <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Start" Wrap="FlexWrap.Wrap">
                <RadzenStack Orientation="Orientation.Vertical" Gap="4px">
                    Allow navigation
                    <RadzenSwitch @bind-Value="@allowNavigation" Style="margin-top: 4px;" InputAttributes="@(new Dictionary<string,object>(){ { "aria-label", "allow navigation" }})" />
                </RadzenStack>
                <RadzenStack Orientation="Orientation.Vertical" Gap="4px">
                    Buttons Style
                    <RadzenSelectBar @bind-Value="@style" TextProperty="Text" ValueProperty="Value" 
                            Data="@(Enum.GetValues(typeof(ButtonStyle)).Cast<ButtonStyle>().Select(t => new { Text = $"{t}", Value = t }))" Size="ButtonSize.Small" class="rz-display-none rz-display-xl-flex" />
                    <RadzenDropDown @bind-Value="@style" TextProperty="Text" ValueProperty="Value" InputAttributes="@(new Dictionary<string,object>(){ { "aria-label", "buttons style" }})"
                                Data="@(Enum.GetValues(typeof(ButtonStyle)).Cast<ButtonStyle>().Select(t => new { Text = $"{t}", Value = t }))" class="rz-display-flex rz-display-xl-none" Style="width: 200px;" />
                </RadzenStack>
            </RadzenStack>
            <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Start" Wrap="FlexWrap.Wrap">
                <RadzenStack Orientation="Orientation.Vertical" Gap="4px">
                    Buttons Variant
                    <RadzenSelectBar @bind-Value="@variant" TextProperty="Text" ValueProperty="Value" 
                            Data="@(Enum.GetValues(typeof(Variant)).Cast<Variant>().Select(t => new { Text = $"{t}", Value = t }))" Size="ButtonSize.Small" class="rz-display-none rz-display-xl-flex" />
                    <RadzenDropDown @bind-Value="@variant" TextProperty="Text" ValueProperty="Value" InputAttributes="@(new Dictionary<string,object>(){ { "aria-label", "buttons variant" }})"
                                Data="@(Enum.GetValues(typeof(Variant)).Cast<Variant>().Select(t => new { Text = $"{t}", Value = t }))" class="rz-display-flex rz-display-xl-none" Style="width: 200px;" />
                </RadzenStack>
                <RadzenStack Orientation="Orientation.Vertical" Gap="4px">
                    Buttons Shade
                    <RadzenSelectBar @bind-Value="@shade" TextProperty="Text" ValueProperty="Value" 
                            Data="@(Enum.GetValues(typeof(Shade)).Cast<Shade>().Select(t => new { Text = $"{t}", Value = t }))" Size="ButtonSize.Small" class="rz-display-none rz-display-xl-flex" />
                    <RadzenDropDown @bind-Value="@shade" TextProperty="Text" ValueProperty="Value" InputAttributes="@(new Dictionary<string,object>(){ { "aria-label", "buttons shade" }})"
                                Data="@(Enum.GetValues(typeof(Shade)).Cast<Shade>().Select(t => new { Text = $"{t}", Value = t }))" class="rz-display-flex rz-display-xl-none" Style="width: 200px;" />
                </RadzenStack>
                <RadzenStack Orientation="Orientation.Vertical" Gap="4px">
                    Buttons Size
                    <RadzenSelectBar @bind-Value="@size" TextProperty="Text" ValueProperty="Value" 
                            Data="@(Enum.GetValues(typeof(ButtonSize)).Cast<ButtonSize>().Select(t => new { Text = $"{t}", Value = t }))" Size="ButtonSize.Small" class="rz-display-none rz-display-xl-flex" />
                    <RadzenDropDown @bind-Value="@size" TextProperty="Text" ValueProperty="Value" InputAttributes="@(new Dictionary<string,object>(){ { "aria-label", "buttons size" }})"
                                Data="@(Enum.GetValues(typeof(ButtonSize)).Cast<ButtonSize>().Select(t => new { Text = $"{t}", Value = t }))" class="rz-display-flex rz-display-xl-none" Style="width: 200px;" />
                </RadzenStack>
            </RadzenStack>
        </RadzenStack>
    </RadzenCard>
    <RadzenCarousel @ref=carousel Auto="false" AllowNavigation="@allowNavigation"
        Style="height: 400px; max-width: 600px;" class="rz-mx-auto"
        ButtonStyle="@style" ButtonSize="@size" ButtonShade="@shade" ButtonVariant="@variant">
        <Items>
            <RadzenCarouselItem>
                <RadzenImage Path="images/gallery/3.jpg" class="rz-h-100" />
            </RadzenCarouselItem>
            <RadzenCarouselItem>
                <RadzenImage Path="images/gallery/2.jpg" class="rz-h-100" />
            </RadzenCarouselItem>
            <RadzenCarouselItem>
                <RadzenImage Path="images/gallery/7.jpg" class="rz-h-100" />
            </RadzenCarouselItem>
            <RadzenCarouselItem>
                <RadzenImage Path="images/gallery/4.jpg" class="rz-h-100" />
            </RadzenCarouselItem>
            <RadzenCarouselItem>
                <RadzenImage Path="images/gallery/5.jpg" class="rz-h-100" />
            </RadzenCarouselItem>
            <RadzenCarouselItem>
                <RadzenImage Path="images/gallery/6.jpg" class="rz-h-100" />
            </RadzenCarouselItem>
            <RadzenCarouselItem>
                <RadzenImage Path="images/gallery/1.jpg" class="rz-h-100" />
            </RadzenCarouselItem>
            <RadzenCarouselItem>
                <RadzenImage Path="images/gallery/8.jpg" class="rz-h-100" />
            </RadzenCarouselItem>
        </Items>
    </RadzenCarousel>
</RadzenStack>

@code {
    RadzenCarousel carousel;
    bool allowNavigation = true;
    Variant variant = Variant.Text;
    ButtonStyle style = ButtonStyle.Base;
    Shade shade = Shade.Lighter;
    ButtonSize size = ButtonSize.Large;
}
```


### Navigation button content

Use `NextText=""` and `PrevText=""` to add text to the next/prev navigation buttons. To change the icons, use `NextIcon=""` and `PrevIcon=""`.

```razor
<RadzenStack class="rz-p-0 rz-p-md-12">
    <RadzenCard class="rz-p-4" Variant="Variant.Outlined">
        <RadzenStack Orientation="Orientation.Vertical" Gap="1rem">
            <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Start" Wrap="FlexWrap.Wrap">
                <RadzenStack Orientation="Orientation.Vertical" Gap="4px">
                    PrevText
                    <RadzenTextBox @bind-Value="@prevText" Style="width: 200px;" />
                </RadzenStack>
                <RadzenStack Orientation="Orientation.Vertical" Gap="4px">
                    PrevIcon
                    <RadzenTextBox @bind-Value="@prevIcon" Style="width: 200px;" />
                </RadzenStack>
                <RadzenStack Orientation="Orientation.Vertical" Gap="4px">
                    NextText
                    <RadzenTextBox @bind-Value="@nextText" Style="width: 200px;" />
                </RadzenStack>
                <RadzenStack Orientation="Orientation.Vertical" Gap="4px">
                    NextIcon
                    <RadzenTextBox @bind-Value="@nextIcon" Style="width: 200px;" />
                </RadzenStack>
            </RadzenStack>
        </RadzenStack>
    </RadzenCard>
    <RadzenCarousel @ref=carousel AllowNavigation="false" Auto="false" Style="height: 400px; max-width: 600px;" class="rz-mx-auto"
        NextText="@nextText" NextIcon="@nextIcon" PrevText="@prevText" PrevIcon="@prevIcon">
        <Items>
            <RadzenCarouselItem>
                <RadzenImage Path="images/gallery/3.jpg" class="rz-h-100" />
            </RadzenCarouselItem>
            <RadzenCarouselItem>
                <RadzenImage Path="images/gallery/2.jpg" class="rz-h-100" />
            </RadzenCarouselItem>
            <RadzenCarouselItem>
                <RadzenImage Path="images/gallery/7.jpg" class="rz-h-100" />
            </RadzenCarouselItem>
            <RadzenCarouselItem>
                <RadzenImage Path="images/gallery/4.jpg" class="rz-h-100" />
            </RadzenCarouselItem>
            <RadzenCarouselItem>
                <RadzenImage Path="images/gallery/5.jpg" class="rz-h-100" />
            </RadzenCarouselItem>
            <RadzenCarouselItem>
                <RadzenImage Path="images/gallery/6.jpg" class="rz-h-100" />
            </RadzenCarouselItem>
            <RadzenCarouselItem>
                <RadzenImage Path="images/gallery/1.jpg" class="rz-h-100" />
            </RadzenCarouselItem>
            <RadzenCarouselItem>
                <RadzenImage Path="images/gallery/8.jpg" class="rz-h-100" />
            </RadzenCarouselItem>
        </Items>
    </RadzenCarousel>
</RadzenStack>

@code {
    RadzenCarousel carousel;

    string prevText = "Prev";
    string prevIcon = "arrow_circle_left";
    string nextText = "Next";
    string nextIcon = "arrow_circle_right";
}
```


### Paging

You can disable the built-in paging via `AllowPaging="false"`. `PagerOverlay` and `PagerPosition` help to position the pager according to your needs.

```razor
<RadzenStack class="rz-p-0 rz-p-md-12">
    <RadzenCard class="rz-p-4" Variant="Variant.Outlined">
        <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Start" Wrap="FlexWrap.Wrap">
            <RadzenStack Orientation="Orientation.Vertical" Gap="4px">
                Allow paging
                <RadzenSwitch @bind-Value="@allowPaging" Style="margin-top: 4px;" InputAttributes="@(new Dictionary<string,object>(){ { "aria-label", "allow paging" }})" />
            </RadzenStack>
            <RadzenStack Orientation="Orientation.Vertical" Gap="4px">
                Pager Overlay
                <RadzenSwitch @bind-Value="@pagerOverlay" Style="margin-top: 4px;" InputAttributes="@(new Dictionary<string,object>(){ { "aria-label", "pager overlay" }})" />
            </RadzenStack>
            <RadzenStack Orientation="Orientation.Vertical" Gap="4px">
                Pager Position
                <RadzenSelectBar @bind-Value="@pagerPosition" TextProperty="Text" ValueProperty="Value" 
                        Data="@(Enum.GetValues(typeof(PagerPosition)).Cast<PagerPosition>().Select(t => new { Text = $"{t}", Value = t }))" class="rz-display-none rz-display-xl-flex" />
                <RadzenDropDown @bind-Value="@pagerPosition" TextProperty="Text" Name="PagerPosition" ValueProperty="Value"
                                Data="@(Enum.GetValues(typeof(PagerPosition)).Cast<PagerPosition>().Select(t => new { Text = $"{t}", Value = t }))" class="rz-display-flex rz-display-xl-none" />
            </RadzenStack>
        </RadzenStack>
    </RadzenCard>
    <RadzenCarousel @ref=carousel @bind-SelectedIndex="@selectedIndex" Auto="false" AllowPaging="@allowPaging"
        Style="height: 400px;" PagerPosition="@pagerPosition" PagerOverlay="@pagerOverlay">
        <Items>
            <RadzenCarouselItem>
                <RadzenImage Path="images/gallery/2.jpg" class="rz-w-100" />
            </RadzenCarouselItem>
            <RadzenCarouselItem>
                <RadzenImage Path="images/gallery/1.jpg" class="rz-w-100" />
            </RadzenCarouselItem>
            <RadzenCarouselItem>
                <RadzenImage Path="images/gallery/3.jpg" class="rz-w-100" />
            </RadzenCarouselItem>
            <RadzenCarouselItem>
                <RadzenImage Path="images/gallery/4.jpg" class="rz-w-100" />
            </RadzenCarouselItem>
            <RadzenCarouselItem>
                <RadzenImage Path="images/gallery/5.jpg" class="rz-w-100" />
            </RadzenCarouselItem>
            <RadzenCarouselItem>
                <RadzenImage Path="images/gallery/6.jpg" class="rz-w-100" />
            </RadzenCarouselItem>
            <RadzenCarouselItem>
                <RadzenImage Path="images/gallery/7.jpg" class="rz-w-100" />
            </RadzenCarouselItem>
            <RadzenCarouselItem>
                <RadzenImage Path="images/gallery/8.jpg" class="rz-w-100" />
            </RadzenCarouselItem>
        </Items>
    </RadzenCarousel>
</RadzenStack>

@code {
    RadzenCarousel carousel;
    bool allowPaging = true;
    bool pagerOverlay = true;
    PagerPosition pagerPosition = PagerPosition.Bottom;

    int selectedIndex;
}
```


### Data-binding


```razor
@inherits DbContextPage

<RadzenStack class="rz-p-0 rz-p-md-12">
    <RadzenCard class="rz-p-4" Variant="Variant.Outlined">
        <RadzenCarousel @ref=carousel PagerPosition="PagerPosition.Bottom" PagerOverlay="false" ButtonShade="Shade.Default" Style="height:500px">
            <Items>
                @foreach (var orderID in orderIDs)
                {
                <RadzenCarouselItem>
                    <RadzenCard class="rz-w-75">
                        <DialogCardPage OrderID=@orderID />
                    </RadzenCard>
                </RadzenCarouselItem>
                }
            </Items>
        </RadzenCarousel>
    </RadzenCard>
</RadzenStack>

@code {
    RadzenCarousel carousel;

    IQueryable<int> orderIDs;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        orderIDs = dbContext.Orders.Select(o => o.OrderID).Take(10);
    }
}
```


### Carousel with RadzenPager

You can use the Carousel with [RadzenPager](/pager) component.

```razor
@inherits DbContextPage

<RadzenStack class="rz-p-0 rz-p-md-12">
    <RadzenCarousel Auto="false" AllowPaging="false" AllowNavigation="false" Style="height:500px">
        <Items>
            @if(orderID != default(int))
            {
            <RadzenCarouselItem>
                <RadzenCard class="rz-h-100">
                    <DialogCardPage OrderID=@orderID ShowClose=false />
                </RadzenCard>
            </RadzenCarouselItem>
            }
        </Items>
    </RadzenCarousel>
    <RadzenCard class="rz-p-4" Variant="Variant.Outlined">
        <RadzenPager Count="@dbContext.Orders.Count()" PageSize="1" PageChanged="@OnPageChanged" />
    </RadzenCard>
</RadzenStack>

@code {
    int selectedIndex;

    int orderID;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        orderID = await GetOrderID(selectedIndex);
    }

    async Task<int> GetOrderID(int index)
    {
        return await Task.FromResult(dbContext.Orders
            .Select(o => o.OrderID)
            .Skip(selectedIndex)
            .Take(1)
            .FirstOrDefault());
    }

    async Task OnPageChanged(PagerEventArgs args)
    {
        selectedIndex = args.PageIndex;

        orderID = await GetOrderID(selectedIndex);
    }
}
```
