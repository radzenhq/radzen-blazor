using System.Collections.Generic;

namespace Radzen.Blazor.Rendering
{
    /// <summary>
    /// Class FontData.
    /// </summary>
    class FontData
    {
        /// <summary>
        /// Gets or sets the average.
        /// </summary>
        /// <value>The average.</value>
        public double Average { get; set; }
        /// <summary>
        /// Gets or sets the chars.
        /// </summary>
        /// <value>The chars.</value>
        public IDictionary<char, double> Chars { get; set; }
    }

    /// <summary>
    /// Class TextMeasurer.
    /// </summary>
    static class TextMeasurer
    {
        /// <summary>
        /// The fonts
        /// </summary>
        private static readonly IDictionary<string, FontData> fonts = new Dictionary<string, FontData> {
            {
                "Roboto",
                new FontData {
            Average = 9.566037735849056,
            Chars = new Dictionary<char, double> {
                ['0'] = 9,
                ['1'] = 9,
                ['2'] = 9,
                ['3'] = 9,
                ['4'] = 9,
                ['5'] = 9,
                ['6'] = 9,
                ['7'] = 9,
                ['8'] = 9,
                ['9'] = 9,
                ['!'] = 5,
                ['"'] = 6,
                ['#'] = 10,
                ['$'] = 9,
                ['%'] = 12,
                ['&'] = 10,
                ['\''] = 3,
                ['('] = 6,
                [')'] = 6,
                ['*'] = 7,
                ['+'] = 10,
                [','] = 4,
                ['-'] = 5,
                ['.'] = 5,
                ['/'] = 7,
                [':'] = 4,
                [';'] = 4,
                ['<'] = 9,
                ['='] = 9,
                ['>'] = 9,
                ['?'] = 8,
                ['@'] = 15,
                ['A'] = 11,
                ['B'] = 10,
                ['C'] = 11,
                ['D'] = 11,
                ['E'] = 10,
                ['F'] = 9,
                ['G'] = 11,
                ['H'] = 12,
                ['I'] = 5,
                ['J'] = 9,
                ['K'] = 11,
                ['L'] = 9,
                ['M'] = 14,
                ['N'] = 12,
                ['O'] = 11,
                ['P'] = 11,
                ['Q'] = 11,
                ['R'] = 10,
                ['S'] = 10,
                ['T'] = 10,
                ['U'] = 11,
                ['V'] = 11,
                ['W'] = 15,
                ['X'] = 11,
                ['Y'] = 10,
                ['Z'] = 10,
                ['['] = 5,
                ['\\'] = 7,
                [']'] = 5,
                ['^'] = 7,
                ['_'] = 8,
                ['`'] = 5,
                ['a'] = 9,
                ['b'] = 9,
                ['c'] = 9,
                ['d'] = 10,
                ['e'] = 9,
                ['f'] = 6,
                ['g'] = 9,
                ['h'] = 9,
                ['i'] = 4,
                ['j'] = 4,
                ['k'] = 9,
                ['l'] = 4,
                ['m'] = 15,
                ['n'] = 9,
                ['o'] = 10,
                ['p'] = 9,
                ['q'] = 10,
                ['r'] = 6,
                ['s'] = 9,
                ['t'] = 6,
                ['u'] = 9,
                ['v'] = 8,
                ['w'] = 13,
                ['x'] = 8,
                ['y'] = 8,
                ['z'] = 8,
                ['{'] = 6,
                ['|'] = 4,
                ['}'] = 6,
                ['~'] = 11,
                ['А'] = 11,
                ['Б'] = 11,
                ['В'] = 11,
                ['Г'] = 9,
                ['Д'] = 12,
                ['Е'] = 11,
                ['Ж'] = 16,
                ['З'] = 11,
                ['И'] = 12,
                ['Й'] = 12,
                ['К'] = 11,
                ['Л'] = 11,
                ['М'] = 14,
                ['Н'] = 12,
                ['О'] = 13,
                ['П'] = 12,
                ['Р'] = 11,
                ['С'] = 12,
                ['Т'] = 10,
                ['У'] = 11,
                ['Ф'] = 14,
                ['Х'] = 11,
                ['Ц'] = 12,
                ['Ч'] = 11,
                ['Ш'] = 16,
                ['Щ'] = 16,
                ['Ъ'] = 14,
                ['Ы'] = 15,
                ['Ь'] = 11,
                ['Э'] = 12,
                ['Ю'] = 16,
                ['Я'] = 12,
                ['а'] = 9,
                ['б'] = 9,
                ['в'] = 9,
                ['г'] = 7,
                ['д'] = 10,
                ['е'] = 9,
                ['ж'] = 13,
                ['з'] = 8,
                ['и'] = 10,
                ['й'] = 10,
                ['к'] = 9,
                ['л'] = 10,
                ['м'] = 11,
                ['н'] = 9,
                ['о'] = 9,
                ['п'] = 9,
                ['р'] = 9,
                ['с'] = 8,
                ['т'] = 8,
                ['у'] = 8,
                ['ф'] = 14,
                ['х'] = 8,
                ['ц'] = 10,
                ['ч'] = 9,
                ['ш'] = 13,
                ['щ'] = 13,
                ['ъ'] = 10,
                ['ы'] = 12,
                ['ь'] = 8,
                ['э'] = 9,
                ['ю'] = 13,
                ['я'] = 9,
                [' '] = 4,
            }
        }
        },
            { "Source Sans Pro", new FontData
        {
            Average = 7.957894736842105,
            Chars = new Dictionary<char, double>
            {
                ['0'] = 8,
                ['1'] = 8,
                ['2'] = 8,
                ['3'] = 8,
                ['4'] = 8,
                ['5'] = 8,
                ['6'] = 8,
                ['7'] = 8,
                ['8'] = 8,
                ['9'] = 8,
                ['!'] = 5,
                ['"'] = 7,
                ['#'] = 8,
                ['$'] = 8,
                ['%'] = 14,
                ['&'] = 10,
                ['\''] = 4,
                ['('] = 5,
                [')'] = 5,
                ['*'] = 7,
                ['+'] = 8,
                [','] = 4,
                ['-'] = 5,
                ['.'] = 4,
                ['/'] = 6,
                [':'] = 4,
                [';'] = 4,
                ['<'] = 8,
                ['='] = 8,
                ['>'] = 8,
                ['?'] = 7,
                ['@'] = 14,
                ['A'] = 9,
                ['B'] = 10,
                ['C'] = 10,
                ['D'] = 10,
                ['E'] = 9,
                ['F'] = 8,
                ['G'] = 10,
                ['H'] = 11,
                ['I'] = 5,
                ['J'] = 8,
                ['K'] = 10,
                ['L'] = 8,
                ['M'] = 12,
                ['N'] = 11,
                ['O'] = 11,
                ['P'] = 10,
                ['Q'] = 11,
                ['R'] = 10,
                ['S'] = 9,
                ['T'] = 9,
                ['U'] = 11,
                ['V'] = 9,
                ['W'] = 13,
                ['X'] = 9,
                ['Y'] = 8,
                ['Z'] = 9,
                ['['] = 5,
                ['\\'] = 6,
                [']'] = 5,
                ['^'] = 8,
                ['_'] = 8,
                ['`'] = 9,
                ['a'] = 9,
                ['b'] = 9,
                ['c'] = 8,
                ['d'] = 9,
                ['e'] = 8,
                ['f'] = 5,
                ['g'] = 9,
                ['h'] = 9,
                ['i'] = 4,
                ['j'] = 4,
                ['k'] = 8,
                ['l'] = 5,
                ['m'] = 14,
                ['n'] = 9,
                ['o'] = 9,
                ['p'] = 9,
                ['q'] = 9,
                ['r'] = 6,
                ['s'] = 7,
                ['t'] = 6,
                ['u'] = 9,
                ['v'] = 8,
                ['w'] = 12,
                ['x'] = 8,
                ['y'] = 8,
                ['z'] = 7,
                ['{'] = 5,
                ['|'] = 4,
                ['}'] = 5,
                ['~'] = 8,
                [' '] = 2,
            }
        }
            }
        };

        /// <summary>
        /// Texts the width.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="fontSize">Size of the font.</param>
        /// <param name="fontFamily">The font family.</param>
        /// <returns>System.Double.</returns>
        public static double TextWidth(string text, double fontSize = 14, string fontFamily = "Roboto")
        {
            if (!fonts.TryGetValue(fontFamily, out FontData font))
            {
                font = fonts["Roboto"];
            }

            var multiplier = fontSize / 16;
            double result = 0;

            foreach (var ch in text)
            {
                var width = font.Average;

                font.Chars.TryGetValue(ch, out width);

                result += width;
            }

            return result * multiplier;
        }
    }
}