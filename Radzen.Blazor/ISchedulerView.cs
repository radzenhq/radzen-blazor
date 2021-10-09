using System;
using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    /// <summary>
    /// Interface ISchedulerView
    /// </summary>
    public interface ISchedulerView
    {
        /// <summary>
        /// Gets the title.
        /// </summary>
        /// <value>The title.</value>
        string Title { get; }
        /// <summary>
        /// Gets the text.
        /// </summary>
        /// <value>The text.</value>
        string Text { get; }

        /// <summary>
        /// Nexts this instance.
        /// </summary>
        /// <returns>DateTime.</returns>
        DateTime Next();

        /// <summary>
        /// Previouses this instance.
        /// </summary>
        /// <returns>DateTime.</returns>
        DateTime Prev();

        /// <summary>
        /// Renders this instance.
        /// </summary>
        /// <returns>RenderFragment.</returns>
        RenderFragment Render();

        /// <summary>
        /// Gets the start date.
        /// </summary>
        /// <value>The start date.</value>
        DateTime StartDate { get; }
        /// <summary>
        /// Gets the end date.
        /// </summary>
        /// <value>The end date.</value>
        DateTime EndDate { get; }
    }
}