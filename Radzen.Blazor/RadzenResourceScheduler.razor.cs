using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    /// <summary>
    /// Displays a collection of <see cref="AppointmentData" /> in day, week or month view.
    /// </summary>
    /// <typeparam name="TResource">The type of the value resource.</typeparam>
    /// <typeparam name="TItem">The type of the value item.</typeparam>
    /// <typeparam name="TLink">The type of the linking fields (relation).</typeparam>
    /// <example>
    /// <code>
    /// &lt;RadzenResourceScheduler ResourceData="@resourcedata" TResource="ResourceItem" AppointmentData="@appointmentdata" TItem="DataItem" TLink="int" StartProperty="Start" EndProperty="End"
    /// TextProperty="Text" ResourceTextProperty="Text" AppointmentLinkProperty="AppointmentId" ResourceLinkProperty="ResourceId" &gt;
    ///     &lt;RadzenMonthView /&gt;
    /// &lt;/RadzenResourceScheduler&gt;
    /// @code {
    ///     class DataItem
    ///     {
    ///         public int Id { get; set; }
    ///         public DateTime Start { get; set; }
    ///         public DateTime End { get; set; }
    ///         public string Text { get; set; }
    ///     }
    ///     class ResourceItem
    ///     {
    ///         public int ResourceId { get; set; }
    ///         public string Text = { get; set; }   
    ///     }
    /// 
    ///     DataItem[] appointmentdata = new DataItem[]
    ///     {
    ///         new DataItem
    ///         {
    ///             AppointmentId = 1,
    ///             Start = DateTime.Today,
    ///             End = DateTime.Today.AddDays(1),
    ///             Text = "Management Meeting"
    ///         },
    ///     };
    ///     
    ///     ResourceItem[] resourcedata = new DataItem[]
    ///     {
    ///         new ResourceItem
    ///         {
    ///             ResourceId = 1,
    ///             Text = "Meeting Room 1"
    ///         },
    ///     };
    /// 
    /// 
    /// }
    /// </code>
    /// </example>
    public partial class RadzenResourceScheduler<TResource, TItem, TLink> : RadzenComponent, IResourceScheduler
    {
        /// <summary>
        /// Gets or sets the child content of the scheduler. Use to specify what views to render.
        /// </summary>
        /// <value>The child content.</value>
        [Parameter]
        public RenderFragment ChildContent { get; set; }

        /// <summary>
        /// Gets or sets the template used to render appointments.
        /// </summary>
        /// <example>
        /// <code>
        /// &lt;RadzenResourceScheduler ResourceData="@resourcedata" TResource="ResourceItem" AppointmentData="@appointmentdata" TItem="DataItem" TLink="int" StartProperty="Start" EndProperty="End"
        /// TextProperty="Text" ResourceTextProperty="Text" AppointmentLinkProperty="AppointmentId" ResourceLinkProperty="ResourceId" &gt;
        ///     &lt;RadzenMonthView /&gt;
        /// &lt;/RadzenScheduler&gt;
        ///    &lt;Template Context="data"&gt;
        ///       &lt;strong&gt;@data.Text&lt;/strong&gt;
        ///    &lt;/Template&gt;
        ///    &lt;ChildContent&gt;
        ///       &lt;RadzenMonthView /&gt;
        ///     &lt;/ChildContent&gt;
        /// &lt;/RadzenResourceScheduler&gt;
        /// </code>
        /// </example>
        /// <value>The template.</value>
        [Parameter]
        public RenderFragment<TItem> Template { get; set; }
        
        /// <summary>
        /// Gets or sets the additional content to be rendered in place of the default navigation buttons in the resource scheduler.
        /// This property allows for complete customization of the navigation controls, replacing the native date navigation buttons (such as year, month, and day) with user-defined content or buttons.
        /// Use this to add custom controls or interactive elements that better suit your application's requirements.
        /// </summary>
        /// <value>The custom navigation template to replace default navigation buttons.</value>
        [Parameter]
        public RenderFragment NavigationTemplate { get; set; }

        /// <summary>
        /// Gets or sets the content to be rendered in place of the default navigation buttons in the resource scheduler.
        /// This property allows for complete customization of the navigation controls, replacing the native navigation buttons (such as year, month, and day, next , prev) with user-defined content or buttons.
        /// Use this to add custom controls or interactive elements that better suit your application's requirements.
        /// </summary>
        /// <value>The custom navigation template to replace default navigation buttons.</value>
        [Parameter]
        public RenderFragment HeaderTemplate { get; set; }

        /// <summary>
        /// Gets or sets the alternative content to be rendered in place of the default resource header.
        /// Use this to add custom controls or interactive elements that better suit your application's requirements.
        /// </summary>
        /// <value>The custom navigation template to replace default navigation buttons.</value>
        [Parameter]
        public RenderFragment<TResource> ResourceTemplate { get; set; }

        /// <summary>
        /// Gets or sets the appointment data of RadzenResourceScheduler. It will display an appointment for every item of the collection which is within the current view date range.
        /// </summary>
        /// <value>The appointment data.</value>
        [Parameter]
        public IEnumerable<TItem> AppointmentData { get; set; }

        /// <summary>
        /// Gets or sets the resource data of RadzenResourceScheduler. It will display a resource for every item of the resource collection as a column in the resource scheduler.
        /// </summary>
        /// <value>The resource data.</value>
        [Parameter]
        public IEnumerable<TResource> ResourceData { get; set; }

        /// <summary>
        /// Specifies the property of <typeparamref name="TItem" /> which will set <see cref="AppointmentData.Start" />.
        /// </summary>
        /// <value>The name of the property. Must be a <c>DateTime</c> property.</value>
        [Parameter]
        public string StartProperty { get; set; }

        /// <summary>
        /// Specifies the property of <typeparamref name="TItem" /> which will set <see cref="AppointmentData.End" />.
        /// </summary>
        /// <value>The name of the property. Must be a <c>DateTime</c> property.</value>
        [Parameter]
        public string EndProperty { get; set; }

        /// <summary>
        /// Specifies the property of <typeparamref name="TResource" /> which will be used as the relational link to <typeparamref name="TItem" />.
        /// </summary>
        /// <value>The name of the property. Must be of type <typeparamref name="TLink" />.</value>
        [Parameter]
        public string ResourceLinkProperty { get; set; }

        /// <summary>
        /// Specifies the property of <typeparamref name="TItem" /> which will be used as the relational link to <typeparamref name="TResource" />.
        /// </summary>
        /// <value>The name of the property. Must be of type <typeparamref name="TLink" />.</value>
        [Parameter]
        public string AppointmentLinkProperty { get; set; }

        private int selectedIndex { get; set; }

        /// <summary>
        /// Specifies the initially selected view.
        /// </summary>
        /// <value>The index of the selected.</value>
        [Parameter]
        public int SelectedIndex { get; set; }

        /// <summary>
        /// Gets or sets the text of the today button. Set to <c>Today</c> by default.
        /// </summary>
        /// <value>The today text.</value>
        [Parameter]
        public string TodayText { get; set; } = "Today";

        /// <summary>
        /// Gets or sets the text of the next button. Set to <c>Next</c> by default.
        /// </summary>
        /// <value>The next text.</value>
        [Parameter]
        public string NextText { get; set; } = "Next";

        /// <summary>
        /// Gets or sets the text of the previous button. Set to <c>Previous</c> by default.
        /// </summary>
        /// <value>The previous text.</value>
        [Parameter]
        public string PrevText { get; set; } = "Previous";

        /// <summary>
        /// Gets or sets the initial date displayed by the selected view. Set to <c>DateTime.Today</c> by default.
        /// </summary>
        /// <value>The date.</value>
        [Parameter]
        public DateTime Date { get; set; } = DateTime.Today;

        /// <summary>
        /// Gets or sets the current date displayed by the selected view. Initially set to <see cref="Date" />. Changes during navigation.
        /// </summary>
        /// <value>The current date.</value>
        public DateTime CurrentDate { get; set; }

        /// <summary>
        /// Specifies the property of <typeparamref name="TItem" /> which will set <see cref="AppointmentData.Text" />.
        /// </summary>
        /// <value>The name of the property.</value>
        [Parameter]
        public string AppointmentTextProperty { get; set; }

        /// <summary>
        /// Specifies the property of <typeparamref name="TResource" /> which will be displayed in the default resource header />.
        /// </summary>
        /// <value>The name of the property.</value>
        [Parameter]
        public string ResourceTextProperty { get; set; }

        /// <summary>
        /// Specifies whether to Show or Hide the Resource Scheduler Header. Defaults to true />.
        /// </summary>
        /// <value>Show / hide header?</value>
        [Parameter]
        public bool ShowHeader { get; set; } = true;

        /// <summary>
        /// Specifies whether the component allows dragging and dropping appointments. Defaults to true />.
        /// </summary>
        /// <value>Allow drag drop?</value>
        [Parameter]
        public bool AllowDragDrop { get; set; } = true;

        /// <summary>
        /// Specifies the initial expanded property of each resource diary column />.
        /// </summary>
        /// <value>Column Expanded?</value>
        [Parameter]
        public bool ColumnExpanded { get; set; } = true;

        /// <summary>
        /// Specifies the initial column maximum width of each resource diary column />.
        /// </summary>
        /// <value>Column Maximum Width</value>
        [Parameter]
        public string ColumnMaximumWidth { get; set; } = "none";

        /// <summary>
        /// Specifies the initial column minimum width of each resource diary column />.
        /// </summary>
        /// <value>Column Minimum Width</value>
        [Parameter]
        public string ColumnMinimumWidth { get; set; } = "350px";

        /// <summary>
        /// Specifies the initial column width of each resource diary column />.
        /// </summary>
        /// <value>Column Width</value>
        [Parameter]
        public string ColumnWidth { get; set; } = "-webkit-fill-available";

        /// <summary>
        /// Specifies the initial column visibility of each resource diary column />.
        /// </summary>
        /// <value>Column Visibility</value>
        [Parameter]
        public string ColumnVisibility { get; set; } = "visible";

        /// <summary>
        /// Specifies the initial resource maximum width of each resource diary column />.
        /// </summary>
        /// <value>Resource Maximum Width</value>
        [Parameter]
        public string ResourceMaximumWidth { get; set; } = "100%";

        /// <summary>
        /// Specifies the initial resource minimum width of each resource diary column />.
        /// </summary>
        /// <value>Resource Minimum Width</value>
        [Parameter]
        public string ResourceMinimumWidth { get; set; } = "350px";

        /// <summary>
        /// Specifies the initial resource width of each resource diary column />.
        /// </summary>
        /// <value>Resource Width</value>
        [Parameter]
        public string ResourceWidth { get; set; } = "100%";

        /// <summary>
        /// Specifies the initial resource visibility of each resource diary column />.
        /// </summary>
        /// <value>Resource Visibility</value>
        [Parameter]
        public string ResourceVisibility { get; set; } = "visible";

        /// <summary>
        /// Specifies the initial scheduler maximum width of each resource diary column />.
        /// </summary>
        /// <value>Scheduler Maximum Width</value>
        [Parameter]
        public string SchedulerMaximumWidth { get; set; } = "100%";

        /// <summary>
        /// Specifies the initial scheduler minimum width of each resource diary column />.
        /// </summary>
        /// <value>Scheduler Minimum Width</value>
        [Parameter]
        public string SchedulerMinimumWidth { get; set; } = "350px";

        /// <summary>
        /// Specifies the initial scheduler width of each resource diary column />.
        /// </summary>
        /// <value>Scheduler Minimum Width</value>
        [Parameter]
        public string SchedulerWidth { get; set; } = "100%";

        /// <summary>
        /// Specifies the initial scheduler visibility of each resource diary column />.
        /// </summary>
        /// <value>Scheduler Visibility</value>
        [Parameter]
        public string SchedulerVisibility { get; set; } = "visible";

        /// <summary>
        /// Specifies the height of the resource in a resource diary column />.
        /// </summary>
        /// <value>Resource Header Height</value>
        [Parameter]
        public string ResourceHeight { get; set; } = "100%";

        /// <summary>
        /// Specifies the height of the scheduler in a resource diary column />.
        /// </summary>
        /// <value>Scheduler Height</value>
        [Parameter]
        public string SchedulerHeight { get; set; } = "640px";

        /// <summary>
        /// A callback that will be invoked when the user clicks a slot in the current view. Commonly used to add new appointments.
        /// </summary>
        /// <example>
        /// <code>
        /// &lt;RadzenResourceScheduler Data=@appointments SlotSelect=@OnSlotSelect&gt;
        /// &lt;/RadzenResourceScheduler&gt;
        /// @code {
        ///  void OnSlotSelect(ResourceSchedulerSlotSelectEventArgs args)
        ///  {
        ///  }
        /// }
        /// </code>
        /// </example>
        [Parameter]
        public EventCallback<ResourceSchedulerSlotSelectEventArgs<TResource>> SlotSelect { get; set; }

        /// <summary>
        /// A callback that will be invoked when the user clicks the Today button.
        /// </summary>
        /// <example>
        /// <code>
        /// &lt;RadzenResourceScheduler Data=@appointments TodaySelect=@OnTodaySelect&gt;
        /// &lt;/RadzenResourceScheduler&gt;
        /// @code {
        /// void OnTodaySelect(ResourceSchedulerTodaySelectEventArgs args)
        /// {
        ///     args.Today = DateTime.Today.AddDays(1);
        /// }
        /// }
        /// </code>
        /// </example>
        [Parameter]
        public EventCallback<SchedulerTodaySelectEventArgs> TodaySelect { get; set; }

        /// <summary>
        /// A callback that will be invoked when the user clicks a month header button.
        /// </summary>
        /// <example>
        /// <code>
        /// &lt;RadzenResourceScheduler Data=@appointments MonthSelect=@OnMonthSelect&gt;
        /// &lt;/RadzenResourceScheduler&gt;
        /// @code {
        /// void OnMonthSelect(ResourceSchedulerMonthSelectEventArgs args)
        /// {
        ///     var selectedMonth = args.MonthStart.Month;
        /// }
        /// }
        /// </code>
        /// </example>
        [Parameter]
        public EventCallback<ResourceSchedulerMonthSelectEventArgs<TResource>> MonthSelect { get; set; }

        /// <summary>
        /// A callback that will be invoked when the user clicks a day header button or the day number in a MonthView.
        /// </summary>
        /// <example>
        /// <code>
        /// &lt;RadzenResourceScheduler Data=@appointments DaySelect=@OnDaySelect&gt;
        /// &lt;/RadzenResourceScheduler&gt;
        /// @code {
        /// void OnDaySelect(ResourceSchedulerDaySelectEventArgs args)
        /// {
        ///     var selectedDay = args.Day;
        /// }
        /// }
        /// </code>
        /// </example>
        [Parameter]
        public EventCallback<ResourceSchedulerDaySelectEventArgs<TResource>> DaySelect { get; set; }

        /// <summary>
        /// A callback that will be invoked when the user clicks an appointment in the current view. Commonly used to edit existing appointments.
        /// </summary>
        /// <example>
        /// <code>
        /// &lt;RadzenResourceScheduler Data=@appointments AppointmentSelect=@OnAppointmentSelect&gt;
        /// &lt;/RadzenResourceScheduler&gt;
        /// @code {
        ///  void OnAppointmentSelect(ResourceSchedulerAppointmentSelectEventArgs&lt;TItem&gt; args)
        ///  {
        ///  }
        /// }
        /// </code>
        /// </example>
        [Parameter]
        public EventCallback<ResourceSchedulerAppointmentSelectEventArgs<TResource, TItem>> AppointmentSelect { get; set; }

        /// <summary>
        /// A callback that will be invoked when the user moves the mouse over an appointment in the current view.
        /// </summary>
        [Parameter]
        public EventCallback<ResourceSchedulerAppointmentMouseEventArgs<TResource, TItem>> AppointmentMouseEnter { get; set; }

        /// <summary>
        /// A callback that will be invoked when the user moves the mouse out of an appointment in the current view.
        /// </summary>
        [Parameter]
        public EventCallback<ResourceSchedulerAppointmentMouseEventArgs<TResource, TItem>> AppointmentMouseLeave { get; set; }

        /// <summary>
        /// A callback that will be invoked when the user clicks the more text in the current view. Commonly used to view additional appointments.
        /// Invoke the <see cref="SchedulerMoreSelectEventArgs.PreventDefault"/> method to prevent the default action (showing the additional appointments).
        /// </summary>
        /// <example>
        /// <code>
        /// &lt;RadzenResourceScheduler Data=@appointments MoreSelect=@OnMoreSelect&gt;
        /// &lt;/RadzenResourceScheduler&gt;
        /// @code {
        ///  void OnMoreSelect(ResourceSchedulerMoreSelectEventArgs args)
        ///  {
        ///     args.PreventDefault();
        ///  }
        /// }
        /// </code>
        /// </example>
        [Parameter]
        public EventCallback<ResourceSchedulerMoreSelectEventArgs<TResource>> MoreSelect { get; set; }

        /// <summary>
        /// An action that will be invoked when the current view renders an appointment. Never call <c>StateHasChanged</c> when handling AppointmentRender.
        /// </summary>
        /// <example>
        /// <code>
        /// &lt;RadzenResourceScheduler Data=@appointments AppointmentRender=@OnAppointmentRendert&gt;
        /// &lt;/RadzenResourceScheduler&gt;
        /// @code {
        ///   void OnAppintmentRender(ResourceSchedulerAppointmentRenderEventArgs&lt;TItem&gt; args)
        ///   {
        ///     if (args.Data.Text == "Birthday")
        ///     {
        ///        args.Attributes["style"] = "color: red;"
        ///     }
        ///.  }
        /// }
        /// </code>
        /// </example>
        [Parameter]
        public Action<ResourceSchedulerAppointmentRenderEventArgs<TResource, TItem>> AppointmentRender { get; set; }

        /// <summary>
        /// An action that will be invoked when the current view renders an slot. Never call <c>StateHasChanged</c> when handling SlotRender.
        /// </summary>
        /// <example>
        /// <code>
        /// &lt;RadzenResourceScheduler Data=@appointments SlotRender=@OnSlotRender&gt;
        /// &lt;/RadzenResourceScheduler&gt;
        /// @code {
        ///   void OnSlotRender(ResourceSchedulerSlotRenderEventArgs args)
        ///   {
        ///     if (args.View.Text == "Month" &amp;&amp; args.Start.Date == DateTime.Today)
        ///     {
        ///        args.Attributes["style"] = "background: red;";
        ///     }
        ///   }
        /// }
        /// </code>
        /// </example>
        [Parameter]
        public Action<ResourceSchedulerSlotRenderEventArgs<TResource>> SlotRender { get; set; }

        /// <summary>
        /// A callback that will be invoked when the scheduler needs data for the current view. Commonly used to filter the
        /// data assigned to <see cref="AppointmentData" />.
        /// </summary>
        [Parameter]
        public EventCallback<ResourceSchedulerLoadDataEventArgs<TResource>> LoadData { get; set; }

        /// <summary>
        /// A callback that will be invoked when an appointment is being dragged and then dropped on a different slot.
        /// Commonly used to change it to a different / resource timeslot.
        /// </summary>
        /// <example>
        /// <code>
        /// &lt;RadzenResourceScheduler Data=@appointments AppointmentMove=@OnAppointmentMove&gt;
        /// &lt;/RadzenResourceScheduler&gt;
        /// @code {
        ///   async Task OnAppointmentMove(ResourceSchedulerAppointmentMoveEventArgs moved)
        ///   {
        ///     var draggedAppointment = appointments.SingleOrDefault(x => x == (Appointment)moved.Appointment.Data);
        ///     if (draggedAppointment != null)
        ///     {
        ///         draggedAppointment.ResourceId = args.Resource.Id;
        ///         draggedAppointment.Start = draggedAppointment.Start + moved.TimeSpan;
        ///         draggedAppointment.End = draggedAppointment.End + moved.TimeSpan;
        ///         await scheduler.Reload();
        ///     }
        ///   }
        /// }
        /// </code>
        /// </example>
        /// <value></value>
        [Parameter]
        public EventCallback<ResourceSchedulerAppointmentMoveEventArgs<TResource, TItem>> AppointmentMove { get; set; }

        private IList<ISchedulerView> Views { get; set; } = new List<ISchedulerView>();

        /// <summary>
        /// Gets the SelectedView.
        /// </summary>
        public ISchedulerView SelectedView
        {
            get
            {
                return Views.ElementAtOrDefault(selectedIndex);
            }
        }

        /// <inheritdoc />
        public async Task AddView(ISchedulerView view)
        {
            if (!Views.Where(v => v.GetType() == view.GetType()).Any())
            {
                Views.Add(view);

                if (SelectedView == view)
                {
                    await InvokeLoadData();
                }
                StateHasChanged();
            }
        }

        /// <inheritdoc />
        public bool IsSelected(ISchedulerView view)
        {
            return selectedIndex == Views.IndexOf(view);
        }

        async Task OnChangeView(ISchedulerView view)
        {
            selectedIndex = Views.IndexOf(view);

            foreach (var scheduler in Schedulers)
            {
                await scheduler.Value.SelectView(selectedIndex);
            }
            await InvokeLoadData();
        }

        async Task OnPrev()
        {
            var newDate = SelectedView.Prev();

            foreach (var scheduler in Schedulers)
            {
                scheduler.Value.CurrentDate = newDate;
            }
            await InvokeLoadData();
        }
        async Task OnToday()
        {
            var args = new SchedulerTodaySelectEventArgs { Today = DateTime.Now.Date };

            await TodaySelect.InvokeAsync(args);

            foreach (var scheduler in Schedulers)
            {
                scheduler.Value.CurrentDate = args.Today;
            }
            await InvokeLoadData();
        }

        async Task OnNext()
        {
            var newDate = SelectedView.Next();

            foreach (var scheduler in Schedulers)
            {
                scheduler.Value.CurrentDate = newDate;
            }
            await InvokeLoadData();
        }

        /// <inheritdoc />
        public void RemoveView(ISchedulerView view)
        {
            Views.Remove(view);
        }

        // Here are the major specific additions and alterations from Scheduler to ResourceScheduler

        TItem draggedItem;
        // A Dictionary of the rendered RadzenScheduler components. One for each resource
        // The ResourceLink field (ID) field of type TLink is the "Key"
        Dictionary<TLink, RadzenScheduler<TItem>> Schedulers { get; set; } = new();

        // a Dictionary of event handlers. One for each resource
        Dictionary<TLink, Action<SchedulerSlotRenderEventArgs>> OnSlotRender = [];
        Dictionary<TLink, Action<SchedulerSlotSelectEventArgs>> OnSlotSelect = [];
        Dictionary<TLink, Action<SchedulerAppointmentRenderEventArgs<TItem>>> OnAppointmentRender = [];
        Dictionary<TLink, Action<SchedulerAppointmentSelectEventArgs<TItem>>> OnAppointmentSelect = [];
        Dictionary<TLink, Action<SchedulerMonthSelectEventArgs>> OnMonthSelect = [];
        Dictionary<TLink, Action<SchedulerDaySelectEventArgs>> OnDaySelect = [];
        Dictionary<TLink, Action<SchedulerMoreSelectEventArgs>> OnMoreSelect = [];
        Dictionary<TLink, Action<SchedulerAppointmentMouseEventArgs<TItem>>> OnAppointmentMouseEnter = [];
        Dictionary<TLink, Action<SchedulerAppointmentMouseEventArgs<TItem>>> OnAppointmentMouseLeave = [];

        // Some key values for rendering the display of each column
        public Dictionary<TLink, ResourceSchedulerColumnStyles> ColumnStyles = [];


        /// <inheritdoc />
        protected override void OnInitialized()
        {
            CurrentDate = Date;
            selectedIndex = SelectedIndex;

            // here we populate the Dictionary of event handlers for the RadzenScheduler's event handlers
            // in the same loop, we also set the default column styles of each column
            foreach (var resource in ResourceData)
            {
                TLink id = (TLink)(resource.GetType().GetProperty(ResourceLinkProperty).GetValue(resource));

                OnSlotRender[id] = (args) =>
                {
                    var args_resource = new ResourceSchedulerSlotRenderEventArgs<TResource> { Resource = resource, Start = args.Start, End = args.End, Attributes = args.Attributes, View = args.View };

                    if (AllowDragDrop)
                    {
                        args.Attributes.Add("ondragover", "event.preventDefault();event.target.classList.add('highlight-slot')");
                        args.Attributes.Add("ondragleave", "event.target.classList.remove('highlight-slot')");
                        args.Attributes.Add("ondrop", EventCallback.Factory.Create<DragEventArgs>(this, async () =>
                        {
                            var app_dropped_args = new ResourceSchedulerAppointmentMoveEventArgs<TResource, TItem> { Data = draggedItem, Resource = resource, IsDefaultPrevented = false, View = SelectedView, DestinationStart = args.Start, DestinationEnd = args.End };
                            await AppointmentMove.InvokeAsync(app_dropped_args);
                            if(!app_dropped_args.IsDefaultPrevented)
                            {
                                draggedItem.GetType().GetProperty(AppointmentLinkProperty).SetValue(draggedItem, (TLink)id);
                                draggedItem.GetType().GetProperty(EndProperty).SetValue(draggedItem, (DateTime)app_dropped_args.DestinationStart.Add((DateTime)draggedItem.GetType().GetProperty(EndProperty).GetValue(draggedItem) - (DateTime)draggedItem.GetType().GetProperty(StartProperty).GetValue(draggedItem)));
                                draggedItem.GetType().GetProperty(StartProperty).SetValue(draggedItem, (DateTime)app_dropped_args.DestinationStart);
                            }
                            JSRuntime.InvokeVoidAsync("eval", $"document.querySelector('.highlight-slot').classList.remove('highlight-slot')");
                        }));
                    }
                    SlotRender?.Invoke(args_resource);
                    args.Attributes = args_resource.Attributes;
                };

                OnSlotSelect[id] = async (args) =>
                {
                    var args_resource = new ResourceSchedulerSlotSelectEventArgs<TResource> { Resource = resource, Appointments = args.Appointments, Start = args.Start, End = args.End, IsDefaultPrevented = args.IsDefaultPrevented, View = args.View };

                    await SlotSelect.InvokeAsync(args_resource);
                };

                OnAppointmentRender[id] = (args) =>
                {

                    var args_resource = new ResourceSchedulerAppointmentRenderEventArgs<TResource, TItem> { Resource = resource, Data = (TItem)args.Data, Start = args.Start, End = args.End };

                    if (AllowDragDrop)
                    {
                        args_resource.Attributes.Add("draggable", "true");
                        args_resource.Attributes.Add("ondragstart", EventCallback.Factory.Create<DragEventArgs>(this, () =>
                            {
                                TooltipService.Close();
                                draggedItem = args_resource.Data;
                            }));
                    }
                    AppointmentRender?.Invoke(args_resource);

                    string style = "cursor:grab; ";

                    if (!args_resource.Attributes.ContainsKey("style"))
                    {
                        args_resource.Attributes.Add("style", style);
                    }
                    else
                    {
                        args_resource.Attributes["style"] += $";{style}";
                    }

                    args.Attributes = args_resource.Attributes;
                };

                OnAppointmentSelect[id] = async (args) =>
                {
                    var args_resource = new ResourceSchedulerAppointmentSelectEventArgs<TResource, TItem> { Resource = resource, Start = args.Start, End = args.End, Data = args.Data };

                    await AppointmentSelect.InvokeAsync(args_resource);
                };

                OnMonthSelect[id] = async (args) =>
                {
                    var args_resource = new ResourceSchedulerMonthSelectEventArgs<TResource> { Resource = resource, Appointments = args.Appointments, MonthStart = args.MonthStart, View = args.View };

                    await MonthSelect.InvokeAsync(args_resource);
                };

                OnDaySelect[id] = async (args) =>
                {
                    var args_resource = new ResourceSchedulerDaySelectEventArgs<TResource> { Resource = resource, Appointments = args.Appointments, Day = args.Day, View = args.View };

                    await DaySelect.InvokeAsync(args_resource);
                };

                OnMoreSelect[id] = async (args) =>
                {
                    var args_resource = new ResourceSchedulerMoreSelectEventArgs<TResource> { Resource = resource, Appointments = args.Appointments, Start = args.Start, End = args.End, IsDefaultPrevented = args.IsDefaultPrevented, View = args.View };

                    await MoreSelect.InvokeAsync(args_resource);
                };

                OnAppointmentMouseEnter[id] = async (args) =>
                {
                    var args_resource = new ResourceSchedulerAppointmentMouseEventArgs<TResource, TItem> { Resource = resource, Data = args.Data, Element = args.Element };

                    await AppointmentMouseEnter.InvokeAsync(args_resource);
                };

                OnAppointmentMouseLeave[id] = async (args) =>
                {
                    var args_resource = new ResourceSchedulerAppointmentMouseEventArgs<TResource, TItem> { Resource = resource, Data = args.Data, Element = args.Element };

                    await AppointmentMouseLeave.InvokeAsync(args_resource);
                };

                ColumnStyles[id] = new();
                ColumnStyles[id].ResourceHeight = ResourceHeight;
                ColumnStyles[id].SchedulerHeight = SchedulerHeight;
                ColumnStyles[id].ColumnExpanded = ColumnExpanded;
                ColumnStyles[id].ColumnMaximumWidth = ColumnMaximumWidth;
                ColumnStyles[id].ColumnMinimumWidth = ColumnMinimumWidth;
                ColumnStyles[id].ColumnWidth = ColumnWidth;
                ColumnStyles[id].ColumnVisibility = ColumnVisibility;
                ColumnStyles[id].ResourceMaximumWidth = ResourceMaximumWidth;
                ColumnStyles[id].ResourceMinimumWidth = ResourceMinimumWidth;
                ColumnStyles[id].ResourceWidth = ResourceWidth;
                ColumnStyles[id].ResourceVisibility = ResourceVisibility;
                ColumnStyles[id].SchedulerMaximumWidth = SchedulerMaximumWidth;
                ColumnStyles[id].SchedulerMinimumWidth = SchedulerMinimumWidth;
                ColumnStyles[id].SchedulerWidth = SchedulerWidth;
                ColumnStyles[id].SchedulerVisibility = SchedulerVisibility;

            }

        }

        IEnumerable<AppointmentData> appointments;
        DateTime rangeStart;
        DateTime rangeEnd;
        Func<TItem, DateTime> startGetter;
        Func<TItem, DateTime> endGetter;
        Func<TResource, string> resourceTextGetter;
        Func<TItem, string> appointmentTextGetter;        
        Func<TItem, TLink> appointmentLinkGetter;
        Func<TResource, TLink> resourceLinkGetter;

        /// <inheritdoc />
        public override async Task SetParametersAsync(ParameterView parameters)
        {
            var needsReload = false;

            if (parameters.DidParameterChange(nameof(Date), Date))
            {
                CurrentDate = parameters.GetValueOrDefault<DateTime>(nameof(Date));
                needsReload = true;
            }

            if (parameters.DidParameterChange(nameof(SelectedIndex), SelectedIndex))
            {
                selectedIndex = parameters.GetValueOrDefault<int>(nameof(SelectedIndex));
                needsReload = true;
            }

            if (parameters.DidParameterChange(nameof(AppointmentData), AppointmentData))
            {
                appointments = null;
            }

            if (parameters.DidParameterChange(nameof(StartProperty), StartProperty))
            {
                startGetter = PropertyAccess.Getter<TItem, DateTime>(parameters.GetValueOrDefault<string>(nameof(StartProperty)));
            }

            if (parameters.DidParameterChange(nameof(EndProperty), EndProperty))
            {
                endGetter = PropertyAccess.Getter<TItem, DateTime>(parameters.GetValueOrDefault<string>(nameof(EndProperty)));
            }

            if (parameters.DidParameterChange(nameof(ResourceTextProperty), ResourceTextProperty))
            {
                resourceTextGetter = PropertyAccess.Getter<TResource, string>(parameters.GetValueOrDefault<string>(nameof(ResourceTextProperty)));
            }

            if (parameters.DidParameterChange(nameof(AppointmentTextProperty), AppointmentTextProperty))
            {
                appointmentTextGetter = PropertyAccess.Getter<TItem, string>(parameters.GetValueOrDefault<string>(nameof(AppointmentTextProperty)));
            }

            if (parameters.DidParameterChange(nameof(ResourceLinkProperty), ResourceLinkProperty))
            {
                resourceLinkGetter = PropertyAccess.Getter<TResource, TLink>(parameters.GetValueOrDefault<string>(nameof(ResourceLinkProperty)));
            }

            if (parameters.DidParameterChange(nameof(AppointmentLinkProperty), AppointmentLinkProperty))
            {
                appointmentLinkGetter = PropertyAccess.Getter<TItem, TLink>(parameters.GetValueOrDefault<string>(nameof(AppointmentLinkProperty)));
            }

            await base.SetParametersAsync(parameters);

            if (needsReload)
            {
                // do for each resource
                await InvokeLoadData();
            }
        }

        /// <summary>
        /// Invokes LoadData on a single resource column. 
        /// </summary>
        public async Task ReloadResource(TResource resource)
        {
            await LoadData.InvokeAsync(new ResourceSchedulerLoadDataEventArgs<TResource> { Resource = resource, Start = SelectedView.StartDate, End = SelectedView.EndDate, View = SelectedView });
        }

        /// <summary>
        /// Causes the current resource scheduler view to render. Enumerates the items of <see cref="AppointmentData" /> and creates instances of <see cref="AppointmentData" /> to
        /// display in the current view. Use it when <see cref="AppointmentData" /> has changed.
        /// </summary>
        public async Task Reload()
        {
            appointments = null;

            await InvokeLoadData();

            StateHasChanged();
        }
        private async Task InvokeLoadData()
        {
            if (SelectedView != null)
            {
                foreach (var resource in ResourceData)
                {
                    await LoadData.InvokeAsync(new ResourceSchedulerLoadDataEventArgs<TResource> { Resource = resource, Start = SelectedView.StartDate, End = SelectedView.EndDate, View = SelectedView });
                }
            }
        }

        /// <inheritdoc />
        public IEnumerable<TItem> GetAppointmentsForResource(TResource resource)
        {
            if (AppointmentData == null || ResourceData == null)
            {
                return Array.Empty<TItem>();
            }

            var predicate = $"{AppointmentLinkProperty} == @0";

           return AppointmentData.AsQueryable()
                               .Where(DynamicLinqCustomTypeProvider.ParsingConfig, predicate, resourceLinkGetter(resource))
                               .ToList();
        }
        protected override string GetComponentCssClass()
        {
            return $"rz-resource-scheduler";
        }
    }
}
