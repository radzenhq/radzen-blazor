using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Radzen.Blazor.Rendering
{
    /// <summary>
    /// Class RGB.
    /// </summary>
    public class RGB
    {
        /// <summary>
        /// The known colors
        /// </summary>
        private static Dictionary<string, string> knownColors = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase)
        {
            ["black"] = "000000",
            ["silver"] = "c0c0c0",
            ["gray"] = "808080",
            ["white"] = "ffffff",
            ["maroon"] = "800000",
            ["red"] = "ff0000",
            ["purple"] = "800080",
            ["fuchsia"] = "ff00ff",
            ["green"] = "008000",
            ["lime"] = "00ff00",
            ["olive"] = "808000",
            ["yellow"] = "ffff00",
            ["navy"] = "000080",
            ["blue"] = "0000ff",
            ["teal"] = "008080",
            ["aqua"] = "00ffff",
            ["orange"] = "ffa500",
            ["aliceblue"] = "f0f8ff",
            ["antiquewhite"] = "faebd7",
            ["aquamarine"] = "7fffd4",
            ["azure"] = "f0ffff",
            ["beige"] = "f5f5dc",
            ["bisque"] = "ffe4c4",
            ["blanchedalmond"] = "ffebcd",
            ["blueviolet"] = "8a2be2",
            ["brown"] = "a52a2a",
            ["burlywood"] = "deb887",
            ["cadetblue"] = "5f9ea0",
            ["chartreuse"] = "7fff00",
            ["chocolate"] = "d2691e",
            ["coral"] = "ff7f50",
            ["cornflowerblue"] = "6495ed",
            ["cornsilk"] = "fff8dc",
            ["crimson"] = "dc143c",
            ["cyan"] = "00ffff",
            ["darkblue"] = "00008b",
            ["darkcyan"] = "008b8b",
            ["darkgoldenrod"] = "b8860b",
            ["darkgray"] = "a9a9a9",
            ["darkgreen"] = "006400",
            ["darkgrey"] = "a9a9a9",
            ["darkkhaki"] = "bdb76b",
            ["darkmagenta"] = "8b008b",
            ["darkolivegreen"] = "556b2f",
            ["darkorange"] = "ff8c00",
            ["darkorchid"] = "9932cc",
            ["darkred"] = "8b0000",
            ["darksalmon"] = "e9967a",
            ["darkseagreen"] = "8fbc8f",
            ["darkslateblue"] = "483d8b",
            ["darkslategray"] = "2f4f4f",
            ["darkslategrey"] = "2f4f4f",
            ["darkturquoise"] = "00ced1",
            ["darkviolet"] = "9400d3",
            ["deeppink"] = "ff1493",
            ["deepskyblue"] = "00bfff",
            ["dimgray"] = "696969",
            ["dimgrey"] = "696969",
            ["dodgerblue"] = "1e90ff",
            ["firebrick"] = "b22222",
            ["floralwhite"] = "fffaf0",
            ["forestgreen"] = "228b22",
            ["gainsboro"] = "dcdcdc",
            ["ghostwhite"] = "f8f8ff",
            ["gold"] = "ffd700",
            ["goldenrod"] = "daa520",
            ["greenyellow"] = "adff2f",
            ["grey"] = "808080",
            ["honeydew"] = "f0fff0",
            ["hotpink"] = "ff69b4",
            ["indianred"] = "cd5c5c",
            ["indigo"] = "4b0082",
            ["ivory"] = "fffff0",
            ["khaki"] = "f0e68c",
            ["lavender"] = "e6e6fa",
            ["lavenderblush"] = "fff0f5",
            ["lawngreen"] = "7cfc00",
            ["lemonchiffon"] = "fffacd",
            ["lightblue"] = "add8e6",
            ["lightcoral"] = "f08080",
            ["lightcyan"] = "e0ffff",
            ["lightgoldenrodyellow"] = "fafad2",
            ["lightgray"] = "d3d3d3",
            ["lightgreen"] = "90ee90",
            ["lightgrey"] = "d3d3d3",
            ["lightpink"] = "ffb6c1",
            ["lightsalmon"] = "ffa07a",
            ["lightseagreen"] = "20b2aa",
            ["lightskyblue"] = "87cefa",
            ["lightslategray"] = "778899",
            ["lightslategrey"] = "778899",
            ["lightsteelblue"] = "b0c4de",
            ["lightyellow"] = "ffffe0",
            ["limegreen"] = "32cd32",
            ["linen"] = "faf0e6",
            ["magenta"] = "ff00ff",
            ["mediumaquamarine"] = "66cdaa",
            ["mediumblue"] = "0000cd",
            ["mediumorchid"] = "ba55d3",
            ["mediumpurple"] = "9370db",
            ["mediumseagreen"] = "3cb371",
            ["mediumslateblue"] = "7b68ee",
            ["mediumspringgreen"] = "00fa9a",
            ["mediumturquoise"] = "48d1cc",
            ["mediumvioletred"] = "c71585",
            ["midnightblue"] = "191970",
            ["mintcream"] = "f5fffa",
            ["mistyrose"] = "ffe4e1",
            ["moccasin"] = "ffe4b5",
            ["navajowhite"] = "ffdead",
            ["oldlace"] = "fdf5e6",
            ["olivedrab"] = "6b8e23",
            ["orangered"] = "ff4500",
            ["orchid"] = "da70d6",
            ["palegoldenrod"] = "eee8aa",
            ["palegreen"] = "98fb98",
            ["paleturquoise"] = "afeeee",
            ["palevioletred"] = "db7093",
            ["papayawhip"] = "ffefd5",
            ["peachpuff"] = "ffdab9",
            ["peru"] = "cd853f",
            ["pink"] = "ffc0cb",
            ["plum"] = "dda0dd",
            ["powderblue"] = "b0e0e6",
            ["rosybrown"] = "bc8f8f",
            ["royalblue"] = "4169e1",
            ["saddlebrown"] = "8b4513",
            ["salmon"] = "fa8072",
            ["sandybrown"] = "f4a460",
            ["seagreen"] = "2e8b57",
            ["seashell"] = "fff5ee",
            ["sienna"] = "a0522d",
            ["skyblue"] = "87ceeb",
            ["slateblue"] = "6a5acd",
            ["slategray"] = "708090",
            ["slategrey"] = "708090",
            ["snow"] = "fffafa",
            ["springgreen"] = "00ff7f",
            ["steelblue"] = "4682b4",
            ["tan"] = "d2b48c",
            ["thistle"] = "d8bfd8",
            ["tomato"] = "ff6347",
            ["turquoise"] = "40e0d0",
            ["violet"] = "ee82ee",
            ["wheat"] = "f5deb3",
            ["whitesmoke"] = "f5f5f5",
            ["yellowgreen"] = "9acd32",
            ["rebeccapurple"] = "663399"
        };

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
        public static RGB Parse(string color)
        {
            if (string.IsNullOrEmpty(color))
            {
                return null;
            }

            var rgb = new Regex(@"rgb\((?<r>\d{1,3}),\s*(?<g>\d{1,3}),\s*(?<b>\d{1,3})\)");

            var match = rgb.Match(color);

            if (match.Success)
            {
                var red = int.Parse(match.Groups["r"].Value);
                var green = int.Parse(match.Groups["g"].Value);
                var blue = int.Parse(match.Groups["b"].Value);

                return new RGB { Red = red, Green = green, Blue = blue };
            }

            var rgba = new Regex(@"rgba\((?<r>\d{1,3}),\s*(?<g>\d{1,3}),\s*(?<b>\d{1,3}),\s*(?<a>\d?\.?\d+)\)");

            match = rgba.Match(color);

            if (match.Success)
            {
                var red = int.Parse(match.Groups["r"].Value);
                var green = int.Parse(match.Groups["g"].Value);
                var blue = int.Parse(match.Groups["b"].Value);
                var alpha = double.Parse(match.Groups["a"].Value, CultureInfo.InvariantCulture);

                return new RGB { Red = red, Green = green, Blue = blue, Alpha = alpha };
            }

            var hex = new Regex("(?<r>[0-9a-fA-F]{2})(?<g>[0-9a-fA-F]{2})(?<b>[0-9a-fA-F]{2})");

            match = hex.Match(color);

            if (match.Success)
            {
                var red = int.Parse($"{match.Groups["r"].Value}", NumberStyles.HexNumber);
                var green = int.Parse($"{match.Groups["g"].Value}", NumberStyles.HexNumber);
                var blue = int.Parse($"{match.Groups["b"].Value}", NumberStyles.HexNumber);

                return new RGB { Red = red, Green = green, Blue = blue };
            }

            if (knownColors.TryGetValue(color, out var knownColor))
            {
                return Parse(knownColor);
            }

            return null;
        }

        /// <summary>
        /// Converts to hex.
        /// </summary>
        /// <returns>System.String.</returns>
        public string ToHex()
        {
            var red = Convert.ToInt32(Red);
            var green = Convert.ToInt32(Green);
            var blue = Convert.ToInt32(Blue);
            return $"{red.ToString("X2")}{green.ToString("X2")}{blue.ToString("X2")}";
        }

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