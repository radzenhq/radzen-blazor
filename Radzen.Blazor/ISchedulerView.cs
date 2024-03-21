using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    /// <summary>
    /// Interface that has to be implemented by a view in order to by supported by <see cref="RadzenScheduler{TItem}" />.
    /// </summary>
    public interface ISchedulerView
    {
        /// <summary>
        /// Gets the icon of the view. It is displayed in the view switching UI.
        /// </summary>
        string Icon { get; }
        /// <summary>
        /// Gets the title of the view. It is displayed in the RadzenScheduler title area.
        /// </summary>
        /// <value>The title.</value>
        string Title { get; }
        /// <summary>
        /// Gets the text of the view. It is displayed in the view switching UI.
        /// </summary>
        /// <value>The text.</value>
        string Text { get; }

        /// <summary>
        /// Returns a new date when the user clicks the next button of RadzenScheduler.
        /// </summary>
        /// <returns>The next date. For example a day view will return the next day, a week view will return the next week.</returns>
        DateTime Next();

        /// <summary>
        /// Returns a new date when the user clicks the previous button of RadzenScheduler.
        /// </summary>
        /// <returns>The previous date. For example a day view will return the previous day, a week view will return the previous week.</returns>
        DateTime Prev();

        /// <summary>
        /// Renders this instance.
        /// </summary>
        /// <returns>RenderFragment.</returns>
        RenderFragment Render();

        /// <summary>
        /// Gets the start date.
        /// </summary>
        DateTime StartDate { get; }
        /// <summary>
        /// Gets the end date.
        /// </summary>
        DateTime EndDate { get; }
        /// <summary>
        /// Handles appointent move event.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        Task OnAppointmentMove(SchedulerAppointmentMoveEventArgs data);
    }
}