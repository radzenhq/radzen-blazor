using System.Collections.Generic;

namespace Radzen.Blazor.Rendering
{
    internal sealed class CategoryAxisLabelLayout
    {
        private readonly Dictionary<double, (string Text, string Displayed)> labels;

        internal CategoryAxisLabelLayout(Dictionary<double, (string Text, string Displayed)> labels, double? rotate, double size, double band)
        {
            this.labels = labels;
            Rotate = rotate;
            Size = size;
            Band = band;
        }

        internal double? Rotate { get; }

        internal double Size { get; }

        internal double Band { get; }

        internal string Text(double tick, string text)
        {
            return labels.TryGetValue(tick, out var label) && label.Text == text ? label.Displayed : text;
        }
    }
}
