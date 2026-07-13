using System;
using Radzen;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Radzen.Blazor;

namespace Radzen.Blazor
{
    /// <summary>
    /// A base class for <see cref="RadzenScheduler{TItem}" /> views.
    /// </summary>
    public abstract class SchedulerViewBase : ComponentBase, ISchedulerView, IDisposable
    {
        [Inject]
        private IServiceProvider Services { get; set; } = default!;

        private Localizer? localizer;

        internal Localizer Localizer => localizer ??=
            Services.GetService<Localizer>() ?? Localizer.Default;

        /// <summary>
        /// Gets a localized string for the specified resource key.
        /// </summary>
        public string Localize(string key) => Localizer.Get(key, Scheduler?.UICulture ?? CultureInfo.CurrentUICulture);

        /// <summary>
        /// Gets the title of the view. It is displayed in the RadzenScheduler title area.
        /// </summary>
        /// <value>The title.</value>
        public abstract string Title { get; }

        /// <summary>
        /// Gets or sets a composite format string used to format the view title. Argument <c>{0}</c> is the first date and
        /// argument <c>{1}</c> is the last date displayed by the view. The dates are formatted using the scheduler culture.
        /// For example <c>TitleFormat="{0:MMMM yyyy}"</c> displays the month and year of the first date.
        /// <see cref="TitleFormatter" /> takes precedence when both are set.
        /// </summary>
        /// <value>The title format.</value>
        [Parameter]
        public string? TitleFormat { get; set; }

        /// <summary>
        /// Gets or sets a function which returns the view title. It is invoked with the first and the last date displayed by the view.
        /// Takes precedence over <see cref="TitleFormat" />.
        /// </summary>
        /// <value>The title formatter.</value>
        [Parameter]
        public Func<DateTime, DateTime, string>? TitleFormatter { get; set; }

        /// <summary>
        /// Formats the view title using <see cref="TitleFormatter" /> or <see cref="TitleFormat" /> when set, otherwise returns the default title.
        /// </summary>
        /// <param name="start">The first date displayed by the view.</param>
        /// <param name="end">The last date displayed by the view.</param>
        /// <param name="title">The default title.</param>
        /// <returns>The formatted title.</returns>
        protected string FormatTitle(DateTime start, DateTime end, string title)
        {
            if (TitleFormatter != null)
            {
                return TitleFormatter(start, end);
            }

            if (!string.IsNullOrEmpty(TitleFormat))
            {
                return string.Format(Scheduler?.Culture ?? CultureInfo.CurrentCulture, TitleFormat, start, end);
            }

            return title;
        }

        /// <summary>
        /// Gets the icon of the view. It is displayed in the view switching UI.
        /// </summary>
        public abstract string Icon { get; }

        /// <summary>
        /// Gets the text of the view. It is displayed in the view switching UI.
        /// </summary>
        /// <value>The text.</value>
        public abstract string Text { get; set; }

        /// <summary>
        /// Gets or sets the scheduler instance.
        /// </summary>
        /// <value>The scheduler.</value>
        [CascadingParameter]
        public IScheduler? Scheduler { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the view groups appointments by the resource types of the scheduler
        /// (declared as <see cref="RadzenSchedulerResource" /> child content). Set to <c>false</c> by default.
        /// </summary>
        /// <value><c>true</c> if the view groups appointments by resource; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool GroupByResource { get; set; }

        /// <summary>
        /// Gets or sets a comma-separated list of resource type names (<see cref="RadzenSchedulerResource.Name" />) to group by, in nesting order.
        /// All declared resource types in order of declaration are used when not set.
        /// </summary>
        /// <value>The resource type names to group by.</value>
        [Parameter]
        public string? GroupBy { get; set; }

        /// <summary>
        /// Gets or sets the orientation of the resource groups. Horizontal renders the groups side by side, vertical renders them one below the other.
        /// Set to <c>Vertical</c> by default. The day view uses <c>Horizontal</c> by default and renders the resources as columns which share the time ruler.
        /// </summary>
        /// <value>The group orientation.</value>
        [Parameter]
        public Orientation GroupOrientation { get; set; } = Orientation.Vertical;

        /// <summary>
        /// Gets the resource types the view groups by - the declared resource types filtered and ordered by <see cref="GroupBy" />.
        /// </summary>
        protected IList<RadzenSchedulerResource> ResourceTypes
        {
            get
            {
                var all = Scheduler?.Resources ?? Array.Empty<RadzenSchedulerResource>();

                if (string.IsNullOrEmpty(GroupBy))
                {
                    return all;
                }

                return GroupBy.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                              .Select(name => all.FirstOrDefault(resource => resource.Name == name))
                              .Where(resource => resource != null)
                              .Cast<RadzenSchedulerResource>()
                              .ToList();
            }
        }

        /// <summary>
        /// Gets every combination of resource items the view groups by - the cartesian product of the items of <see cref="ResourceTypes" />,
        /// each keyed by resource type <see cref="RadzenSchedulerResource.Name" />.
        /// </summary>
        protected IList<IDictionary<string, object>> ResourcePaths
        {
            get
            {
                var types = ResourceTypes.Where(type => type.Items.Count > 0).ToList();

                if (types.Count == 0)
                {
                    return new List<IDictionary<string, object>>();
                }

                IEnumerable<IDictionary<string, object>> paths = new List<IDictionary<string, object>> { new Dictionary<string, object>() };

                foreach (var type in types)
                {
                    paths = paths.SelectMany(path => type.Items.Select(item =>
                    {
                        IDictionary<string, object> next = new Dictionary<string, object>(path)
                        {
                            [type.Name] = item
                        };
                        return next;
                    }));
                }

                return paths.ToList();
            }
        }

        /// <summary>
        /// Gets a value indicating whether the view should render resource groups - <see cref="GroupByResource" /> is set and the scheduler has resources with items.
        /// </summary>
        protected bool HasResourceGroups => GroupByResource && ResourcePaths.Count > 0;

        /// <summary>
        /// Determines whether the specified combination is the first combination of <see cref="ResourcePaths" />. Used to render
        /// shared chrome such as the time ruler only once when the groups are rendered side by side.
        /// </summary>
        /// <param name="resources">The resource items keyed by resource type name.</param>
        /// <returns><c>true</c> if the combination consists of the first item of every resource type; otherwise, <c>false</c>.</returns>
        protected bool IsFirstResourcePath(IDictionary<string, object> resources)
        {
            var types = ResourceTypes;

            return resources.All(entry => ReferenceEquals(types.FirstOrDefault(type => type.Name == entry.Key)?.Items.FirstOrDefault(), entry.Value));
        }

        /// <summary>
        /// Returns the appointments which belong to every resource item of the specified combination.
        /// </summary>
        /// <param name="appointments">The appointments to filter.</param>
        /// <param name="resources">The resource items keyed by resource type name.</param>
        /// <returns>The appointments of the specified resource combination.</returns>
        protected IList<AppointmentData> AppointmentsForResources(IEnumerable<AppointmentData> appointments, IDictionary<string, object> resources)
        {
            var types = ResourceTypes;

            return appointments.Where(item => resources.All(entry =>
                types.FirstOrDefault(type => type.Name == entry.Key)?.IsAppointmentInResource(item, entry.Value) == true)).ToList();
        }


        /// <summary>
        /// Disposes this instance.
        /// </summary>
        public void Dispose()
        {
            Scheduler?.RemoveView(this);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Called by the Blazor runtime when parameters are set.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        public override async Task SetParametersAsync(ParameterView parameters)
        {
            var textChanged = parameters.DidParameterChange(nameof(Text), Text);
            var titleChanged = parameters.DidParameterChange(nameof(TitleFormat), TitleFormat);
            var groupChanged = parameters.DidParameterChange(nameof(GroupByResource), GroupByResource) ||
                parameters.DidParameterChange(nameof(GroupBy), GroupBy) ||
                parameters.DidParameterChange(nameof(GroupOrientation), GroupOrientation);

            await base.SetParametersAsync(parameters);

            if ((textChanged || titleChanged || groupChanged) && Scheduler != null)
            {
                await Scheduler.Reload();
            }

            if (Scheduler != null)
            {
                await Scheduler.AddView(this);
            }
        }

        /// <summary>
        /// Returns a new date when the user clicks the next button of RadzenScheduler.
        /// </summary>
        /// <returns>The next date. For example a day view will return the next day, a week view will return the next week.</returns>
        public abstract DateTime Next();

        /// <summary>
        /// Returns a new date when the user clicks the previous button of RadzenScheduler.
        /// </summary>
        /// <returns>The previous date. For example a day view will return the previous day, a week view will return the previous week.</returns>
        public abstract DateTime Prev();

        /// <summary>
        /// Renders this instance.
        /// </summary>
        public abstract RenderFragment Render();

        /// <summary>
        /// Gets the start date.
        /// </summary>
        /// <value>The start date.</value>
        public abstract DateTime StartDate { get; }
        /// <summary>
        /// Gets the end date.
        /// </summary>
        /// <value>The end date.</value>
        public abstract DateTime EndDate { get; }

        /// <summary>
        /// Handles appointent move event.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task OnAppointmentMove(SchedulerAppointmentMoveEventArgs data)
        {
            if (Scheduler != null)
            {
                await Scheduler.AppointmentMove.InvokeAsync(data);
            }
        }
    }
}