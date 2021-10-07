using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    /// <summary>
    /// Interface IChartSeries
    /// </summary>
    public interface IChartSeries
    {
        /// <summary>
        /// Transforms the category scale.
        /// </summary>
        /// <param name="scale">The scale.</param>
        /// <returns>ScaleBase.</returns>
        ScaleBase TransformCategoryScale(ScaleBase scale);
        /// <summary>
        /// Transforms the value scale.
        /// </summary>
        /// <param name="scale">The scale.</param>
        /// <returns>ScaleBase.</returns>
        ScaleBase TransformValueScale(ScaleBase scale);
        /// <summary>
        /// Gets or sets the markers.
        /// </summary>
        /// <value>The markers.</value>
        RadzenMarkers Markers { get; set; }
        /// <summary>
        /// Gets the type of the marker.
        /// </summary>
        /// <value>The type of the marker.</value>
        MarkerType MarkerType { get; }
        /// <summary>
        /// Gets the size of the marker.
        /// </summary>
        /// <value>The size of the marker.</value>
        double MarkerSize { get; }
        /// <summary>
        /// Renders the specified category scale.
        /// </summary>
        /// <param name="categoryScale">The category scale.</param>
        /// <param name="valueScale">The value scale.</param>
        /// <returns>RenderFragment.</returns>
        RenderFragment Render(ScaleBase categoryScale, ScaleBase valueScale);
        /// <summary>
        /// Renders the tooltip.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="marginLeft">The margin left.</param>
        /// <param name="marginTop">The margin top.</param>
        /// <returns>RenderFragment.</returns>
        RenderFragment RenderTooltip(object data, double marginLeft, double marginTop);
        /// <summary>
        /// Renders the legend item.
        /// </summary>
        /// <returns>RenderFragment.</returns>
        RenderFragment RenderLegendItem();
        /// <summary>
        /// Gets the color.
        /// </summary>
        /// <value>The color.</value>
        string Color { get; }
        /// <summary>
        /// Gets a value indicating whether this <see cref="IChartSeries"/> is visible.
        /// </summary>
        /// <value><c>true</c> if visible; otherwise, <c>false</c>.</value>
        bool Visible { get; }
        /// <summary>
        /// Gets a value indicating whether [show in legend].
        /// </summary>
        /// <value><c>true</c> if [show in legend]; otherwise, <c>false</c>.</value>
        bool ShowInLegend { get; }
        /// <summary>
        /// Gets or sets the rendering order.
        /// </summary>
        /// <value>The rendering order.</value>
        int RenderingOrder { get; set; }
        /// <summary>
        /// Determines whether this instance contains the object.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <returns><c>true</c> if [contains] [the specified x]; otherwise, <c>false</c>.</returns>
        bool Contains(double x, double y, double tolerance);
        /// <summary>
        /// Datas at.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <returns>System.Object.</returns>
        object DataAt(double x, double y);
        /// <summary>
        /// Gets or sets the title.
        /// </summary>
        /// <value>The title.</value>
        string Title { get; set; }
        /// <summary>
        /// Gets the title.
        /// </summary>
        /// <returns>System.String.</returns>
        string GetTitle();
        /// <summary>
        /// Measures the legend.
        /// </summary>
        /// <returns>System.Double.</returns>
        double MeasureLegend();
        /// <summary>
        /// Invokes the click.
        /// </summary>
        /// <param name="handler">The handler.</param>
        /// <param name="data">The data.</param>
        /// <returns>Task.</returns>
        Task InvokeClick(EventCallback<SeriesClickEventArgs> handler, object data);
    }
}
