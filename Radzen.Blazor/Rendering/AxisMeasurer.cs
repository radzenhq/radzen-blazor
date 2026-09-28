using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Radzen.Blazor.Rendering
{
    /// <summary>
    /// Measures the sises of the chart axes.
    /// </summary>
    public static class AxisMeasurer
    {
        private const double LabelHeight = 16 * 0.875;
        private const double XAxisLabelSize = LabelHeight + 12;
        private const double LabelGap = 4;
        private const double FittedLabelAngle = -45;
        private const double MinimumLabelWidth = 40;
        private const double RenderedWidthRatio = 0.975;
        private const string Ellipsis = "\u2026";

        /// <summary>
        /// Calculates the length of the Y axis.
        /// </summary>
        /// <param name="scale">The scale.</param>
        /// <param name="axis">The axis.</param>
        /// <param name="title">The title.</param>
        /// <returns>System.Double.</returns>
        public static double YAxis(ScaleBase scale, AxisBase axis, RadzenAxisTitle title)
        {
            ArgumentNullException.ThrowIfNull(scale);
            ArgumentNullException.ThrowIfNull(axis);
            ArgumentNullException.ThrowIfNull(title);

            if (axis.Width.HasValue)
            {
                ArgumentOutOfRangeException.ThrowIfLessThan(axis.Width.Value, 24);
                return axis.Width.Value;
            }

            double length = 0;

            if (HasValidTicks(scale, scale.Ticks(axis.TickDistance)))
            {
                foreach (var y in scale.TickValues(axis.TickDistance))
                {
                    var text = axis.Format(scale, y);

                    length = Math.Max(length, TextMeasurer.TextWidth(text));
                }
            }

            return Math.Max(24, length + YAxisPadding(axis, title));
        }

        private static double YAxisPadding(AxisBase axis, RadzenAxisTitle title)
        {
            return YAxisLabelPadding(axis) + YAxisTitleSize(title);
        }

        private static double YAxisLabelPadding(AxisBase axis)
        {
            return 9 + axis.StrokeWidth;
        }

        private static double YAxisTitleSize(RadzenAxisTitle title)
        {
            return String.IsNullOrEmpty(title.Text) ? 0 : title.Size + 32;
        }

        internal static CategoryAxisLabelLayout? VerticalCategoryLabels(ScaleBase scale, AxisBase axis, RadzenAxisTitle title, double maxSize, bool readableMinimum = true)
        {
            if (!HasValidTicks(scale, scale.Ticks(axis.TickDistance)))
            {
                return null;
            }

            var labels = new List<(double Tick, string Text)>();

            foreach (var tick in scale.TickValues(axis.TickDistance))
            {
                labels.Add((tick, axis.Format(scale, scale.Value(tick))));
            }

            var padding = YAxisPadding(axis, title);
            var titleSize = YAxisTitleSize(title);
            double size;

            if (axis.Width.HasValue)
            {
                ArgumentOutOfRangeException.ThrowIfLessThan(axis.Width.Value, 24);
                size = axis.Width.Value;
            }
            else
            {
                var length = labels.Count > 0 ? labels.Max(label => TextMeasurer.TextWidth(label.Text)) : 0;
                var room = Math.Max(0, maxSize - YAxisLabelPadding(axis));

                if (readableMinimum)
                {
                    room = Math.Max(room, Math.Min(length, MinimumLabelWidth));
                }

                size = Math.Max(24, Math.Min(length, room) + padding);
            }

            var maxLength = size - padding;
            var displayed = new Dictionary<double, (string Text, string Displayed)>();

            foreach (var label in labels)
            {
                displayed[label.Tick] = (label.Text, Shorten(label.Text, maxLength));
            }

            return new CategoryAxisLabelLayout(displayed, null, size, size - titleSize);
        }

        internal static CategoryAxisLabelLayout? HorizontalCategoryLabels(ScaleBase scale, AxisBase axis, RadzenAxisTitle title, double maxSize, double plotLeft, double width = 0)
        {
            if (!HasValidTicks(scale, scale.Ticks(axis.TickDistance)))
            {
                return null;
            }

            var labels = new List<(double Tick, double X, string Text)>();

            foreach (var tick in scale.TickValues(axis.TickDistance))
            {
                labels.Add((tick, scale.Scale(tick, true), axis.Format(scale, scale.Value(tick))));
            }

            var titleSize = String.IsNullOrEmpty(title.Text) ? 0 : title.Size + 24;
            var displayed = new Dictionary<double, (string Text, string Displayed)>();

            var angle = axis.LabelRotation ?? axis.LabelAutoRotation ?? FittedLabelAngle;
            var alpha = angle * Math.PI / 180;
            var sin = Math.Abs(Math.Sin(alpha));
            var cos = Math.Abs(Math.Cos(alpha));

            if (sin < 1e-9)
            {
                sin = 0;
            }

            if (cos < 1e-9)
            {
                cos = 0;
            }

            if (axis.LabelRotation == null)
            {
                var flatStride = 1;
                var keepFlatLast = false;

                if (!HorizontalLabelsFit(labels) && sin > 0)
                {
                    bool RotatedSpacing(int first, int second) => Math.Abs(labels[second].X - labels[first].X) * sin >= LabelHeight + LabelGap;

                    flatStride = RotatedLabelStride(labels.Count, RotatedSpacing);
                    var flatLast = labels.Count - 1;
                    var flatLastKept = flatLast - flatLast % flatStride;
                    keepFlatLast = flatLastKept != flatLast && RotatedSpacing(flatLastKept, flatLast);
                }

                bool Kept(int index) => index % flatStride == 0 || (keepFlatLast && index == labels.Count - 1);

                if (HorizontalLabelsFit(labels.Where((label, index) => Kept(index)).ToList()))
                {
                    for (var index = 0; index < labels.Count; index++)
                    {
                        displayed[labels[index].Tick] = (labels[index].Text, Kept(index) ? labels[index].Text : String.Empty);
                    }

                    return new CategoryAxisLabelLayout(displayed, null, XAxisLabelSize + titleSize, XAxisLabelSize);
                }
            }

            var maxLength = sin > 0 ? Math.Max(0, (Math.Max(maxSize, XAxisLabelSize) - cos * XAxisLabelSize) / sin) : double.PositiveInfinity;
            var extendsLeft = angle < 0 ? Math.Cos(alpha) > 0 : Math.Cos(alpha) < 0;
            var texts = new string[labels.Count];
            var lengths = new double[labels.Count];

            for (var index = 0; index < labels.Count; index++)
            {
                var label = labels[index];
                var room = extendsLeft ? plotLeft + label.X : width > 0 ? width - plotLeft - label.X : double.PositiveInfinity;
                var cap = sin > 0 && cos > 0 ? Math.Min(maxLength, Math.Max(0, room) / cos) : maxLength;

                texts[index] = double.IsPositiveInfinity(cap) ? label.Text : Shorten(label.Text, cap);
                lengths[index] = TextMeasurer.TextWidth(texts[index]);
            }

            bool Separated(int first, int second)
            {
                var distance = Math.Abs(labels[second].X - labels[first].X);

                if (sin == 0)
                {
                    return distance >= (lengths[first] + lengths[second]) / 2 * RenderedWidthRatio + LabelGap;
                }

                return distance * sin >= LabelHeight + LabelGap || distance * cos >= Math.Max(lengths[first], lengths[second]) + LabelGap;
            }

            var stride = RotatedLabelStride(labels.Count, Separated);
            var last = labels.Count - 1;
            var lastKept = last - last % stride;
            var keepLast = lastKept != last && Separated(lastKept, last);
            double length = 0;

            for (var index = 0; index < labels.Count; index++)
            {
                var label = labels[index];

                if (index % stride == 0 || (index == last && keepLast))
                {
                    displayed[label.Tick] = (label.Text, texts[index]);
                    length = Math.Max(length, lengths[index]);
                }
                else
                {
                    displayed[label.Tick] = (label.Text, String.Empty);
                }
            }

            var size = Math.Max(XAxisLabelSize, sin * length + cos * XAxisLabelSize);

            return new CategoryAxisLabelLayout(displayed, angle, size + titleSize, size);
        }

        private static bool HorizontalLabelsFit(List<(double Tick, double X, string Text)> labels)
        {
            (double X, double Width)? previous = null;

            foreach (var label in labels)
            {
                if (String.IsNullOrEmpty(label.Text))
                {
                    continue;
                }

                var width = TextMeasurer.TextWidth(label.Text) * RenderedWidthRatio;

                if (previous != null && Math.Abs(label.X - previous.Value.X) < (width + previous.Value.Width) / 2 + LabelGap)
                {
                    return false;
                }

                previous = (label.X, width);
            }

            return true;
        }

        private static int RotatedLabelStride(int count, Func<int, int, bool> separated)
        {
            for (var stride = 1; stride < count; stride++)
            {
                var fits = true;

                for (var index = stride; index < count && fits; index += stride)
                {
                    fits = separated(index - stride, index);
                }

                if (fits)
                {
                    return stride;
                }
            }

            return Math.Max(1, count);
        }

        internal static string Shorten(string text, double maxWidth)
        {
            if (TextMeasurer.TextWidth(text) <= maxWidth)
            {
                return text;
            }

            var available = maxWidth - TextMeasurer.TextWidth(Ellipsis);
            var elements = StringInfo.GetTextElementEnumerator(text);
            var length = 0;
            double width = 0;

            while (elements.MoveNext())
            {
                var element = elements.GetTextElement();

                width += TextMeasurer.TextWidth(element);

                if (width > available)
                {
                    break;
                }

                length = elements.ElementIndex + element.Length;
            }

            if (length > 0 && length < text.Length && !Char.IsWhiteSpace(text[length]))
            {
                var wordEnd = length;

                while (wordEnd > 0 && !Char.IsWhiteSpace(text[wordEnd - 1]))
                {
                    wordEnd--;
                }

                if (text.Substring(0, wordEnd).Trim().Length > 0)
                {
                    length = wordEnd;
                }
            }

            return TrimBeforeEllipsis(text.Substring(0, length)) + Ellipsis;
        }

        private static string TrimBeforeEllipsis(string text)
        {
            var end = text.Length;

            while (end > 0 && (Char.IsWhiteSpace(text[end - 1]) || IsTrailingPunctuation(text[end - 1])))
            {
                end--;
            }

            return end > 0 ? text.Substring(0, end) : text.TrimEnd();
        }

        private static bool IsTrailingPunctuation(char character)
        {
            return Char.GetUnicodeCategory(character) is UnicodeCategory.DashPunctuation or UnicodeCategory.OpenPunctuation or UnicodeCategory.InitialQuotePunctuation
                || character is ',' or ';' or ':' or '.' or '/' or '&' or '\u3001' or '\u3002' or '\uFF0C' or '\uFF1B' or '\uFF1A';
        }

        /// <summary>
        /// Calculates the length of the X axis.
        /// </summary>
        /// <param name="scale">The scale.</param>
        /// <param name="axis">The axis.</param>
        /// <param name="title">The title.</param>
        /// <returns>System.Double.</returns>
        public static double XAxis(ScaleBase scale, AxisBase axis, RadzenAxisTitle title)
        {
            ArgumentNullException.ThrowIfNull(axis);
            ArgumentNullException.ThrowIfNull(scale);
            ArgumentNullException.ThrowIfNull(title);

            var size = 16 * 0.875 + 12;

            var angle = axis.LabelRotation ?? axis.LabelAutoRotation;

            if (angle != null)
            {
                var ticks = scale.Ticks(axis.TickDistance);
                var isLogX = scale.IsLogarithmic;

                double length = 0;

                if (HasValidTicks(scale, ticks))
                {
                    for (var y = ticks.Start; y <= ticks.End; y = isLogX ? y * ticks.Step : y + ticks.Step)
                    {
                        var text = axis.Format(scale, y);

                        length = Math.Max(length, TextMeasurer.TextWidth(text));
                    }
                }

                var alpha = Math.Abs(angle.Value) * Math.PI / 180;
                var rotatedWidth = Math.Abs(Math.Sin(alpha) * length) + Math.Abs(Math.Cos(alpha) * size);
                size = Math.Max(size, rotatedWidth);
            }

            if (!String.IsNullOrEmpty(title.Text))
            {
                size += title.Size + 24;
            }

            return size;
        }

        // Guards the tick enumeration loops against ticks that would never terminate: non-finite
        // bounds (e.g. a scale measured with no data), a multiplicative step that cannot grow the
        // value (log scales need step > 1 and a positive start) or a zero additive step.
        private static bool HasValidTicks(ScaleBase scale, (double Start, double End, double Step) ticks)
        {
            if (!double.IsFinite(ticks.Start) || !double.IsFinite(ticks.End) || !double.IsFinite(ticks.Step))
            {
                return false;
            }

            if (scale.IsLogarithmic)
            {
                return ticks.Step > 1 && ticks.Start > 0;
            }

            return ticks.Step > 0;
        }
    }
}