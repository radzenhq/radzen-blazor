using System;
using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    /// <summary>
    /// Displays the series values as text labels.
    /// </summary>
    /// <example>
    /// <code>
    ///   &lt;RadzenChart&gt;
    ///       &lt;RadzenLineSeries Data=@revenue CategoryProperty="Quarter" ValueProperty="Revenue"&gt;
    ///          &lt;RadzenSeriesDataLabels /&gt;
    ///       &lt;/RadzenLineSeries&gt;
    ///   &lt;/RadzenChart&gt;
    ///   @code {
    ///       class DataItem
    ///       {
    ///           public string Quarter { get; set; }
    ///           public double Revenue { get; set; }
    ///       }
    ///       DataItem[] revenue = new DataItem[]
    ///       {
    ///           new DataItem { Quarter = "Q1", Revenue = 234000 },
    ///           new DataItem { Quarter = "Q2", Revenue = 284000 },
    ///           new DataItem { Quarter = "Q3", Revenue = 274000 },
    ///           new DataItem { Quarter = "Q4", Revenue = 294000 }
    ///       };
    ///   }
    /// </code>
    /// </example>
    public partial class RadzenSeriesDataLabels
    {
        /// <summary> Horizontal offset from the default position. </summary>
        [Parameter]
        public double OffsetX { get; set; }

        /// <summary> Vertical offset from the default position. </summary>
        [Parameter]
        public double OffsetY { get; set; }

        /// <summary>Determines the visibility of the data labels. Set to <c>true</c> by default.</summary>
        [Parameter]
        public bool Visible { get; set; } = true;

        /// <summary>Defines the fill color of the component.</summary>
        [Parameter]
        public string? Fill { get; set; }

        /// <summary>
        /// Data labels render above all series so no series line draws over them.
        /// </summary>
        public bool RenderOnTop => true;

        /// <summary>
        /// Gets or sets where labels render relative to their data point. <see cref="DataLabelPosition.Auto" /> (default)
        /// uses the position best suited for the series type and flips labels which would clip the plot edge.
        /// </summary>
        /// <value>The label position. Default is <see cref="DataLabelPosition.Auto" />.</value>
        [Parameter]
        public DataLabelPosition Position { get; set; } = DataLabelPosition.Auto;

        /// <summary>
        /// Gets or sets the visual treatment of the labels: a rounded background chip (default), an outlined
        /// text halo, or plain text.
        /// </summary>
        /// <value>The label appearance. Default is <see cref="DataLabelAppearance.Chip" />.</value>
        [Parameter]
        public DataLabelAppearance Appearance { get; set; } = DataLabelAppearance.Chip;

        /// <summary>
        /// Gets or sets a value indicating whether overlapping labels are allowed. When <c>false</c> (default),
        /// labels which would overlap an already rendered label - including labels of other series - are hidden.
        /// </summary>
        /// <value><c>true</c> to allow overlapping labels; otherwise, <c>false</c>. Default is <c>false</c>.</value>
        [Parameter]
        public bool AllowOverlap { get; set; }

        /// <summary>
        /// Gets or sets a format string applied to the data point value (e.g. <c>{0:C0}</c>).
        /// Overrides the value axis format. <see cref="Formatter" /> takes precedence when both are set.
        /// </summary>
        /// <value>The format string.</value>
        [Parameter]
        public string? FormatString { get; set; }

        /// <summary>
        /// Gets or sets a callback which converts the data point value to the label text.
        /// Takes precedence over <see cref="FormatString" /> and the axis format.
        /// </summary>
        /// <value>The formatter callback.</value>
        [Parameter]
        public Func<object, string>? Formatter { get; set; }

        /// <summary>
        /// Gets or sets the interval between labeled data points - <c>2</c> labels every other point, <c>3</c> every third.
        /// </summary>
        /// <value>The label step. Default is <c>1</c> (every data point).</value>
        [Parameter]
        public int Step { get; set; } = 1;

        /// <summary>
        /// Gets or sets which data points display labels.
        /// </summary>
        /// <value>The display strategy. Default is <see cref="DataLabelDisplay.All" />.</value>
        [Parameter]
        public DataLabelDisplay Display { get; set; } = DataLabelDisplay.All;

        /// <summary>
        /// Gets the CSS class for the data labels.
        /// </summary>
        /// <returns></returns>
        public string GetSeriesDataLabelClass()
        {
            var css = string.IsNullOrWhiteSpace(Fill) ? "rz-series-data-label" : "rz-series-data-label-fill";

            if (Appearance == DataLabelAppearance.Outline)
            {
                css += " rz-series-data-label-outline";
            }

            return css;
        }
    }
}