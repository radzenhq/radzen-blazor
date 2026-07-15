using System;
using Radzen;
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

            await base.SetParametersAsync(parameters);

            if ((textChanged || titleChanged) && Scheduler != null)
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