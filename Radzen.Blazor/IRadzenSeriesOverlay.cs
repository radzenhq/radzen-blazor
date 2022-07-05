using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    /// <summary>
    /// Interface for chart overlays
    /// </summary>
    public interface IRadzenSeriesOverlay
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
    }
}
