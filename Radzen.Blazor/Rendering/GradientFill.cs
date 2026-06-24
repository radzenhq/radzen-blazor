#nullable enable

using System.Collections.Generic;

namespace Radzen.Blazor.Rendering
{
    /// <summary>
    /// Collects and de-duplicates the linear gradient definitions used by column and bar series.
    /// Each distinct fill color (combined with the value sign, which determines the baseline side)
    /// produces a single shared gradient, referenced from every shape that uses it.
    /// </summary>
    public class GradientFill
    {
        private const string SeriesColorVariable = "var(--rz-series-color)";

        private readonly string chartId;
        private readonly int seriesIndex;
        private readonly Dictionary<(string Color, int Sign), int> ordinals = new();
        private readonly List<(string Color, int Sign)> entries = new();

        /// <summary>
        /// Initializes a new instance scoped to a single series so its gradient ids never collide
        /// with other series or other charts on the page.
        /// </summary>
        /// <param name="chartId">The unique id of the chart.</param>
        /// <param name="seriesIndex">The index of the series within the chart.</param>
        public GradientFill(string? chartId, int seriesIndex)
        {
            this.chartId = chartId ?? string.Empty;
            this.seriesIndex = seriesIndex;
        }

        /// <summary>
        /// The distinct color/sign combinations that have been referenced and therefore need a definition.
        /// </summary>
        public IReadOnlyList<(string Color, int Sign)> Entries => entries;

        private int Register(string color, int sign)
        {
            var key = (color, sign);

            if (!ordinals.TryGetValue(key, out var ordinal))
            {
                ordinal = entries.Count;
                ordinals[key] = ordinal;
                entries.Add(key);
            }

            return ordinal;
        }

        /// <summary>
        /// Returns the id of the gradient for the given color and sign, registering it on first use.
        /// An empty color falls back to the series color so series whose <c>Fill</c> defaults to an
        /// empty string still render a colored gradient.
        /// </summary>
        public string Id(string? color, int sign)
        {
            var resolved = string.IsNullOrEmpty(color) ? SeriesColorVariable : color;

            return $"rzFillGradient{chartId}-{seriesIndex}-{Register(resolved, sign)}";
        }

        /// <summary>
        /// Returns a <c>url(#id)</c> fill reference for the gradient of the given color and sign.
        /// </summary>
        public string Url(string? color, int sign)
        {
            return $"url(#{Id(color, sign)})";
        }

        /// <summary>
        /// Computes the stop opacity at offset 0 and offset 1 so the value tip is fully colored and
        /// the fill fades out toward the axis baseline, regardless of orientation or value sign.
        /// </summary>
        /// <param name="vertical"><c>true</c> for column series (vertical gradient), <c>false</c> for bar series (horizontal).</param>
        /// <param name="sign">The sign of the value: <c>1</c> for non-negative, <c>-1</c> for negative.</param>
        /// <param name="startOpacity">The opacity at the value tip.</param>
        /// <param name="endOpacity">The opacity at the axis baseline.</param>
        public static (double Offset0, double Offset1) Stops(bool vertical, int sign, double startOpacity, double endOpacity)
        {
            var tipAtOffset0 = vertical ? sign >= 0 : sign < 0;

            return tipAtOffset0 ? (startOpacity, endOpacity) : (endOpacity, startOpacity);
        }
    }
}
