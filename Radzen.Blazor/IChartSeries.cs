using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Radzen.Blazor.Rendering;

namespace Radzen.Blazor
{
    /// <summary>
    /// Specifies the common API that <see cref="RadzenChart" /> series must implement.
    /// </summary>
    public interface IChartSeries
    {
        /// <summary>
        /// Transforms a category scale to new one.
        /// </summary>
        /// <param name="scale">The scale.</param>
        ScaleBase TransformCategoryScale(ScaleBase scale);
        /// <summary>
        /// Transforms a category scale to new one.
        /// </summary>
        /// <param name="scale">The scale.</param>
        ScaleBase TransformValueScale(ScaleBase scale);
        /// <summary>
        /// Gets or sets the series marker configuration.
        /// </summary>
        RadzenMarkers Markers { get; set; }
        /// <summary>
        /// Gets the series marker type.
        /// </summary>
        /// <value>The type of the marker.</value>
        MarkerType MarkerType { get; }
        /// <summary>
        /// Gets the size of the marker.
        /// </summary>
        /// <value>The size of the marker.</value>
        double MarkerSize { get; }
        /// <summary>
        /// Renders the series with the specified category and value scales.
        /// </summary>
        /// <param name="categoryScale">The category scale.</param>
        /// <param name="valueScale">The value scale.</param>
        /// <returns>RenderFragment.</returns>
        RenderFragment Render(ScaleBase categoryScale, ScaleBase valueScale);
        /// <summary>
        /// Renders the series overlays with the specified category and value scales.
        /// </summary>
        /// <param name="categoryScale">The category scale.</param>
        /// <param name="valueScale">The value scale.</param>
        /// <returns>RenderFragment.</returns>
        RenderFragment RenderOverlays(ScaleBase categoryScale, ScaleBase valueScale);

        /// <summary>
        /// Renders the overlays which display above all series (see <see cref="IChartSeriesOverlay.RenderOnTop" />),
        /// so later series cannot draw over them.
        /// </summary>
        /// <param name="categoryScale">The category scale.</param>
        /// <param name="valueScale">The value scale.</param>
        /// <returns>RenderFragment.</returns>
        RenderFragment RenderTopOverlays(ScaleBase categoryScale, ScaleBase valueScale) => builder => { };

        /// <summary>
        /// Renders the series tooltip.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns>RenderFragment.</returns>
        RenderFragment RenderTooltip(object data);
        /// <summary>
        /// Renders a tooltip item with the specified data to be displayed in a shared tooltip
        /// </summary>
        RenderFragment RenderSharedTooltipItem(object category);
        /// <summary>
        /// Get position of the series tooltip.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns>Position.</returns>
        Point GetTooltipPosition(object data);
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
        /// Gets a value indicating whether this series should appear in the legend.
        /// </summary>
        /// <value><c>true</c> if the series appears in the legend; otherwise, <c>false</c>.</value>
        bool ShowInLegend { get; }
        /// <summary>
        /// Gets or sets the rendering order.
        /// </summary>
        /// <value>The rendering order.</value>
        int RenderingOrder { get; set; }
        /// <summary>
        /// Determines if the series contains the specified coordinates with a given tolerance.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <returns><c>true</c> if the series contains the coordinates; otherwise, <c>false</c>.</returns>
        bool Contains(double x, double y, double tolerance);
        /// <summary>
        /// Returns the data at the specified coordinates;
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        (object, Point) DataAt(double x, double y);
        /// <summary>
        /// Returns data chart position
        /// </summary>
        IEnumerable<ChartDataLabel> GetDataLabels(double offsetX, double offsetY);

        /// <summary>
        /// Gets the data labels for the series at the specified <see cref="DataLabelPosition" />.
        /// The default implementation ignores the position and returns <see cref="GetDataLabels(double, double)" />.
        /// </summary>
        /// <param name="offsetX">The horizontal offset.</param>
        /// <param name="offsetY">The vertical offset.</param>
        /// <param name="position">The requested label position.</param>
        IEnumerable<ChartDataLabel> GetDataLabels(double offsetX, double offsetY, DataLabelPosition position) => GetDataLabels(offsetX, offsetY);
        /// <summary>
        /// Specifies whether the chart highlights the hovered data point of this series with an active point
        /// dot. Range-style series (candlestick, OHLC, high-low, box plot) opt out - a single dot cannot
        /// represent their value range. The default is <c>true</c>.
        /// </summary>
        bool ShowActivePoint => true;
        /// <summary>
        /// Returns series median
        /// </summary>
        double GetMedian();
        /// <summary>
        /// Returns series mean
        /// </summary>
        double GetMean();
        /// <summary>
        /// Returns series mode
        /// </summary>
        double GetMode();
        /// <summary>
        /// Returns series trend
        /// </summary>
        (double a, double b) GetTrend();
        /// <summary>
        /// Returns the series data as scaled (pixel) points ordered by category position.
        /// </summary>
        IList<Point> GetScaledDataPoints();
        /// <summary>
        /// Series coordinate system
        /// </summary>
        CoordinateSystem CoordinateSystem { get; }
        /// <summary>
        /// Series overlays
        /// </summary>
        IList<IChartSeriesOverlay> Overlays{ get; }
        /// <summary>
        /// Gets or sets the title of the series. The title is displayed in tooltips and the legend.
        /// </summary>
        /// <value>The title.</value>
        string Title { get; set; }
        /// <summary>
        /// Gets the title.
        /// </summary>
        string GetTitle();
        /// <summary>
        /// Measures the legend.
        /// </summary>
        /// <returns>System.Double.</returns>
        double MeasureLegend();
        /// <summary>
        /// Measures the text width of each individual legend item the series contributes. Series that
        /// render a single legend item return one value; pie-like series return one value per data item.
        /// Used to estimate how many rows a top or bottom legend wraps into.
        /// </summary>
        /// <returns>The text width of each legend item.</returns>
        System.Collections.Generic.IEnumerable<double> MeasureLegendItems();
        /// <summary>
        /// Invokes the click handler with the provided data item.
        /// </summary>
        /// <param name="handler">The handler.</param>
        /// <param name="data">The data.</param>
        Task InvokeClick(EventCallback<SeriesClickEventArgs> handler, object data);
    }
}
