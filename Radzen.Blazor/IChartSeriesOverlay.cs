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
        /// Hit test
        /// </summary>
        bool Contains(double mouseX, double mouseY, int tolerance);

        /// <summary>
        /// Renders tooltip
        /// </summary>
        RenderFragment RenderTooltip(double mouseX, double mouseY, double marginLeft, double marginTop);
    }
}
