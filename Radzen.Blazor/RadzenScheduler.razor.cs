using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    /// <summary>
    /// Class RadzenScheduler.
    /// Implements the <see cref="Radzen.RadzenComponent" />
    /// Implements the <see cref="Radzen.Blazor.IScheduler" />
    /// </summary>
    /// <typeparam name="TItem">The type of the t item.</typeparam>
    /// <seealso cref="Radzen.RadzenComponent" />
    /// <seealso cref="Radzen.Blazor.IScheduler" />
    public partial class RadzenScheduler<TItem> : RadzenComponent, IScheduler
    {
        /// <summary>
        /// Gets or sets the child content.
        /// </summary>
        /// <value>The child content.</value>
        [Parameter]
        public RenderFragment ChildContent { get; set; }

        /// <summary>
        /// Gets or sets the template.
        /// </summary>
        /// <value>The template.</value>
        [Parameter]
        public RenderFragment<TItem> Template { get; set; }

        /// <summary>
        /// Gets or sets the data.
        /// </summary>
        /// <value>The data.</value>
        [Parameter]
        public IEnumerable<TItem> Data { get; set; }

        /// <summary>
        /// Gets or sets the start property.
        /// </summary>
        /// <value>The start property.</value>
        [Parameter]
        public string StartProperty { get; set; }

        /// <summary>
        /// Gets or sets the end property.
        /// </summary>
        /// <value>The end property.</value>
        [Parameter]
        public string EndProperty { get; set; }

        /// <summary>
        /// Gets or sets the index of the selected.
        /// </summary>
        /// <value>The index of the selected.</value>
        private int selectedIndex { get; set; }
        /// <summary>
        /// Gets or sets the index of the selected.
        /// </summary>
        /// <value>The index of the selected.</value>
        [Parameter]
        public int SelectedIndex { get; set; }

        /// <summary>
        /// Gets or sets the today text.
        /// </summary>
        /// <value>The today text.</value>
        [Parameter]
        public string TodayText { get; set; } = "Today";

        /// <summary>
        /// Gets or sets the date.
        /// </summary>
        /// <value>The date.</value>
        [Parameter]
        public DateTime Date { get; set; } = DateTime.Today;

        /// <summary>
        /// Gets or sets the current date.
        /// </summary>
        /// <value>The current date.</value>
        public DateTime CurrentDate { get; set; }

        /// <summary>
        /// Gets or sets the text property.
        /// </summary>
        /// <value>The text property.</value>
        [Parameter]
        public string TextProperty { get; set; }

        /// <summary>
        /// Gets or sets the slot select.
        /// </summary>
        /// <value>The slot select.</value>
        [Parameter]
        public EventCallback<SchedulerSlotSelectEventArgs> SlotSelect { get; set; }

        /// <summary>
        /// Gets or sets the appointment select.
        /// </summary>
        /// <value>The appointment select.</value>
        [Parameter]
        public EventCallback<SchedulerAppointmentSelectEventArgs<TItem>> AppointmentSelect { get; set; }

        /// <summary>
        /// Gets or sets the appointment render.
        /// </summary>
        /// <value>The appointment render.</value>
        [Parameter]
        public Action<SchedulerAppointmentRenderEventArgs<TItem>> AppointmentRender { get; set; }

        /// <summary>
        /// Gets or sets the load data.
        /// </summary>
        /// <value>The load data.</value>
        [Parameter]
        public EventCallback<SchedulerLoadDataEventArgs> LoadData { get; set; }

        /// <summary>
        /// Gets or sets the views.
        /// </summary>
        /// <value>The views.</value>
        IList<ISchedulerView> Views { get; set; } = new List<ISchedulerView>();

        /// <summary>
        /// Gets the selected view.
        /// </summary>
        /// <value>The selected view.</value>
        ISchedulerView SelectedView
        {
            get
            {
                return Views.ElementAtOrDefault(selectedIndex);
            }
        }

        /// <summary>
        /// Gets the appointment attributes.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>IDictionary&lt;System.String, System.Object&gt;.</returns>
        public IDictionary<string, object> GetAppointmentAttributes(AppointmentData item)
        {
            var args = new SchedulerAppointmentRenderEventArgs<TItem> { Data = (TItem)item.Data, Start = item.Start, End = item.End };

            if (AppointmentRender != null)
            {
                AppointmentRender(args);
            }

            return args.Attributes;
        }

        /// <summary>
        /// Renders the appointment.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>RenderFragment.</returns>
        public RenderFragment RenderAppointment(AppointmentData item)
        {
            if (Template != null)
            {
                TItem context = (TItem)item.Data;
                return Template(context);
            }

            return builder => builder.AddContent(0, item.Text);
        }

        /// <summary>
        /// Selects the slot.
        /// </summary>
        /// <param name="start">The start.</param>
        /// <param name="end">The end.</param>
        /// <returns>Task.</returns>
        public async Task SelectSlot(DateTime start, DateTime end)
        {
            await SlotSelect.InvokeAsync(new SchedulerSlotSelectEventArgs { Start = start, End = end });
        }

        /// <summary>
        /// Selects the appointment.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns>Task.</returns>
        public async Task SelectAppointment(AppointmentData data)
        {
            await AppointmentSelect.InvokeAsync(new SchedulerAppointmentSelectEventArgs<TItem> { Start = data.Start, End = data.End, Data = (TItem)data.Data });
        }

        /// <summary>
        /// Adds the view.
        /// </summary>
        /// <param name="view">The view.</param>
        /// <returns>Task.</returns>
        public async Task AddView(ISchedulerView view)
        {
            if (!Views.Contains(view))
            {
                Views.Add(view);

                if (SelectedView == view)
                {
                    await InvokeLoadData();
                }

                StateHasChanged();
            }
        }

        /// <summary>
        /// Reloads this instance.
        /// </summary>
        /// <returns>Task.</returns>
        public async Task Reload()
        {
            appointments = null;

            await InvokeLoadData();

            StateHasChanged();
        }

        /// <summary>
        /// Determines whether the specified view is selected.
        /// </summary>
        /// <param name="view">The view.</param>
        /// <returns><c>true</c> if the specified view is selected; otherwise, <c>false</c>.</returns>
        public bool IsSelected(ISchedulerView view)
        {
            return selectedIndex == Views.IndexOf(view);
        }

        /// <summary>
        /// Called when [change view].
        /// </summary>
        /// <param name="view">The view.</param>
        async Task OnChangeView(ISchedulerView view)
        {
            selectedIndex = Views.IndexOf(view);

            await InvokeLoadData();
        }

        /// <summary>
        /// Called when [previous].
        /// </summary>
        async Task OnPrev()
        {
            CurrentDate = SelectedView.Prev();

            await InvokeLoadData();
        }

        /// <summary>
        /// Called when [today].
        /// </summary>
        async Task OnToday()
        {
            CurrentDate = DateTime.Now.Date;

            await InvokeLoadData();
        }

        /// <summary>
        /// Called when [next].
        /// </summary>
        async Task OnNext()
        {
            CurrentDate = SelectedView.Next();

            await InvokeLoadData();
        }

        /// <summary>
        /// Removes the view.
        /// </summary>
        /// <param name="view">The view.</param>
        public void RemoveView(ISchedulerView view)
        {
            Views.Remove(view);
        }

        /// <summary>
        /// Called when [initialized].
        /// </summary>
        protected override void OnInitialized()
        {
            CurrentDate = Date;
            selectedIndex = SelectedIndex;

            double height = 0;

            var style = CurrentStyle;

            if (style.ContainsKey("height"))
            {
                var pixelHeight = style["height"];

                if (pixelHeight.EndsWith("px"))
                {
                    height = Convert.ToDouble(pixelHeight.TrimEnd("px".ToCharArray()));
                }
            }

            if (height > 0)
            {
                heightIsSet = true;

                Height = height;
            }
        }

        /// <summary>
        /// The appointments
        /// </summary>
        IEnumerable<AppointmentData> appointments;
        /// <summary>
        /// The range start
        /// </summary>
        DateTime rangeStart;
        /// <summary>
        /// The range end
        /// </summary>
        DateTime rangeEnd;

        /// <summary>
        /// The start getter
        /// </summary>
        Func<TItem, DateTime> startGetter;
        /// <summary>
        /// The end getter
        /// </summary>
        Func<TItem, DateTime> endGetter;
        /// <summary>
        /// The text getter
        /// </summary>
        Func<TItem, string> textGetter;

        /// <summary>
        /// Set parameters as an asynchronous operation.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
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

            if (parameters.DidParameterChange(nameof(Data), Data))
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

            if (parameters.DidParameterChange(nameof(TextProperty), TextProperty))
            {
                textGetter = PropertyAccess.Getter<TItem, string>(parameters.GetValueOrDefault<string>(nameof(TextProperty)));
            }

            await base.SetParametersAsync(parameters);

            if (needsReload)
            {
                await InvokeLoadData();
            }
        }

        /// <summary>
        /// Invokes the load data.
        /// </summary>
        private async Task InvokeLoadData()
        {
            if (SelectedView != null)
            {
                await LoadData.InvokeAsync(new SchedulerLoadDataEventArgs { Start = SelectedView.StartDate, End = SelectedView.EndDate });
            }
        }

        /// <summary>
        /// Determines whether [is appointment in range] [the specified item].
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="start">The start.</param>
        /// <param name="end">The end.</param>
        /// <returns><c>true</c> if [is appointment in range] [the specified item]; otherwise, <c>false</c>.</returns>
        public bool IsAppointmentInRange(AppointmentData item, DateTime start, DateTime end)
        {
            if (item.Start == item.End && item.Start >= start && item.End < end)
            {
                return true;
            }

            return item.End > start && item.Start < end;
        }

        /// <summary>
        /// Gets the appointments in range.
        /// </summary>
        /// <param name="start">The start.</param>
        /// <param name="end">The end.</param>
        /// <returns>IEnumerable&lt;AppointmentData&gt;.</returns>
        public IEnumerable<AppointmentData> GetAppointmentsInRange(DateTime start, DateTime end)
        {
            if (Data == null)
            {
                return Array.Empty<AppointmentData>();
            }

            if (start == rangeStart && end == rangeEnd && appointments != null)
            {
                return appointments;
            }

            rangeStart = start;
            rangeEnd = end;

            var predicate = $"{EndProperty} >= @0 && {StartProperty} < @1";

            appointments = Data.AsQueryable()
                               .Where(predicate, start, end)
                               .ToList()
                               .Select(item => new AppointmentData { Start = startGetter(item), End = endGetter(item), Text = textGetter(item), Data = item });

            return appointments;
        }

        /// <summary>
        /// Class Rect.
        /// </summary>
        class Rect
        {
            /// <summary>
            /// Gets or sets the width.
            /// </summary>
            /// <value>The width.</value>
            public double Width { get; set; }
            /// <summary>
            /// Gets or sets the height.
            /// </summary>
            /// <value>The height.</value>
            public double Height { get; set; }
        }

        /// <summary>
        /// On after render as an asynchronous operation.
        /// </summary>
        /// <param name="firstRender">if set to <c>true</c> [first render].</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);

            if (firstRender)
            {
                var rect = await JSRuntime.InvokeAsync<Rect>("Radzen.createScheduler", Element, Reference);

                if (!heightIsSet)
                {
                    heightIsSet = true;
                    Resize(rect.Width, rect.Height);
                }
            }
        }

        /// <summary>
        /// Resizes the specified width.
        /// </summary>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        [JSInvokable]
        public void Resize(double width, double height)
        {
            var stateHasChanged = false;

            if (height != Height)
            {
                Height = height;
                stateHasChanged = true;
            }

            if (stateHasChanged)
            {
                StateHasChanged();
            }
        }

        /// <summary>
        /// Disposes this instance.
        /// </summary>
        public override void Dispose()
        {
            base.Dispose();

            if (IsJSRuntimeAvailable)
            {
                JSRuntime.InvokeVoidAsync("Radzen.destroyScheduler", Element);
            }
        }

        /// <summary>
        /// The height is set
        /// </summary>
        private bool heightIsSet = false;
        /// <summary>
        /// Gets the height.
        /// </summary>
        /// <value>The height.</value>
        private double Height { get; set; } = 400; // Default height set from theme.

        /// <summary>
        /// Gets the height.
        /// </summary>
        /// <value>The height.</value>
        double IScheduler.Height
        {
            get
            {
                return Height;
            }
        }
        /// <summary>
        /// Gets the component CSS class.
        /// </summary>
        /// <returns>System.String.</returns>
        protected override string GetComponentCssClass()
        {
            return $"rz-scheduler";
        }
    }
}