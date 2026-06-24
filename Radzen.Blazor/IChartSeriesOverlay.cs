using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    /// <summary>
    /// Interface for chart overlays
    /// </summary>
    public interface IChartSeriesOverlay
    {
        /// <summary>
        /// Render overlay
        /// </summary>
        /// <param name="categoryScale"></param>
        /// <param name="valueScale"></param>
        /// <returns>RenderFragment</returns>
        RenderFragment Render(ScaleBase categoryScale, ScaleBase valueScale);

        /// <summary>
        /// Gets overlay visibility state
        /// </summary>
        bool Visible { get; }

        /// <summary>
        /// When <c>true</c> the overlay renders in a layer above all series, so later series cannot draw over it
        /// (e.g. value labels). Default is <c>false</c> - the overlay renders with its own series.
        /// </summary>
        bool RenderOnTop => false;

        /// <summary>
        /// Hit test
        /// </summary>
        bool Contains(double mouseX, double mouseY, int tolerance);

        /// <summary>
        /// Renders tooltip
        /// </summary>
        RenderFragment RenderTooltip(double mouseX, double mouseY);

        /// <summary>
        /// Get position of the overlay tooltip.
        /// </summary>
        /// <returns>Position.</returns>
        Point GetTooltipPosition(double mouseX, double mouseY);
    }
}
