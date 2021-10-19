using System;

namespace Radzen.Blazor.Rendering
{
    /// <summary>
    /// Class HSV.
    /// </summary>
    public class HSV
    {
        /// <summary>
        /// Gets or sets the hue.
        /// </summary>
        /// <value>The hue.</value>
        public double Hue { get; set; }
        /// <summary>
        /// Gets or sets the saturation.
        /// </summary>
        /// <value>The saturation.</value>
        public double Saturation { get; set; }
        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        /// <value>The value.</value>
        public double Value { get; set; }
        /// <summary>
        /// Gets or sets the alpha.
        /// </summary>
        /// <value>The alpha.</value>
        public double Alpha { get; set; } = 1;

        /// <summary>
        /// Converts to rgb.
        /// </summary>
        /// <returns>RGB.</returns>
        public RGB ToRGB()
        {
            var hue = Hue * 6;
            var saturation = Saturation;
            var value = Value;

            double red;
            double green;
            double blue;

            if (saturation == 0) 
            {
                red = green = blue = value;
            } 
            else 
            {
                var i = Convert.ToInt32(Math.Floor(hue));
                var f = hue - i;
                var p = value * (1 - saturation);
                var q = value * (1 - saturation * f);
                var t = value * (1 - saturation * (1 - f));
                int mod = i % 6;

                red = new [] { value, q, p, p, t, value }[mod];
                green = new [] { t, value, value, q, p, p }[mod];
                blue = new [] { p, p, t, value, value, q }[mod];
            }

            return new RGB 
            {
                Red = Convert.ToInt32(red * 255), 
                Green = Convert.ToInt32(green * 255), 
                Blue = Convert.ToInt32(blue * 255), 
                Alpha = Alpha 
            };
        }
    }
}