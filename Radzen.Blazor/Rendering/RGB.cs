using System;
using Radzen.Documents;

namespace Radzen.Blazor.Rendering
{
    /// <summary>
    /// Utility class used to parse and convert RGB colors.
    /// </summary>
    public class RGB
    {
        /// <summary>
        /// Gets or sets the red.
        /// </summary>
        /// <value>The red.</value>
        public double Red { get; set; }
        /// <summary>
        /// Gets or sets the green.
        /// </summary>
        /// <value>The green.</value>
        public double Green { get; set; }
        /// <summary>
        /// Gets or sets the blue.
        /// </summary>
        /// <value>The blue.</value>
        public double Blue { get; set; }
        /// <summary>
        /// Gets or sets the alpha.
        /// </summary>
        /// <value>The alpha.</value>
        public double Alpha { get; set; } = 1;

        /// <summary>
        /// Parses the specified color.
        /// </summary>
        /// <param name="color">The color.</param>
        /// <returns>RGB.</returns>
        public static RGB? Parse(string color)
        {
            if (ColorValue.Parse(color) is not { } value)
            {
                return null;
            }

            return new RGB { Red = value.Red, Green = value.Green, Blue = value.Blue, Alpha = value.Alpha };
        }

        /// <summary>
        /// Converts to hex.
        /// </summary>
        /// <returns>System.String.</returns>
        public string ToHex() => new ColorValue(Red, Green, Blue, Alpha).ToHex();

        /// <summary>
        /// Converts to css.
        /// </summary>
        /// <returns>System.String.</returns>
        public string ToCSS()
        {
            if (Alpha == 1)
            {
                return $"rgb({Red}, {Green}, {Blue})";
            }
            else
            {
                return $"rgba({Red}, {Green}, {Blue}, {Alpha.ToInvariantString()})";
            }
        }

        /// <summary>
        /// Converts to hsv.
        /// </summary>
        /// <returns>HSV.</returns>
        public HSV ToHSV()
        {
            double red = Red / 255.0;
            double green = Green / 255.0;
            double blue = Blue / 255.0;
            var min = Math.Min(red, Math.Min(green, blue));
            var max = Math.Max(red, Math.Max(green, blue));
            var delta = max - min;
            double hue;
            double saturation = 0;
            var value = max;

            if (max != 0)
            {
                saturation = delta / max;
            }

            if (delta == 0)
            {
                hue = 0;
            }
            else
            {
                if (red == max)
                {
                    hue = (green - blue) / delta + (green < blue ? 6 : 0);
                }
                else if (green == max)
                {
                    hue = 2 + (blue - red) / delta;
                }
                else
                {
                    hue = 4 + (red - green) / delta;
                }

                hue /= 6;
            }

            return new HSV { Hue = hue, Saturation = saturation, Value = value, Alpha = Alpha };
        }
    }
}