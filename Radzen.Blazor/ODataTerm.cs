using System;
using System.Globalization;
using System.Text;

namespace Radzen
{
    internal sealed class ODataTerm
    {
        internal const int Or = 1;
        internal const int And = 2;
        internal const int Comparison = 3;
        internal const int Additive = 4;
        internal const int Multiplicative = 5;
        internal const int Unary = 6;
        internal const int Primary = 7;

        private readonly string text;

        private ODataTerm(string text, int precedence, bool isPath, bool isDate, bool isConstant, object? value)
        {
            this.text = text;
            Precedence = precedence;
            IsPath = isPath;
            IsDate = isDate;
            IsConstant = isConstant;
            Value = value;
        }

        internal string Text => IsConstant ? Literal(Value) : text;

        internal int Precedence { get; }

        internal bool IsPath { get; }

        internal bool IsRoot => IsPath && Text.Length == 0;

        internal bool IsDate { get; }

        internal bool IsConstant { get; }

        internal object? Value { get; }

        internal static ODataTerm Expression(string text, int precedence) => new(text, precedence, false, false, false, null);

        internal static ODataTerm Path(string text) => new(text, Primary, true, false, false, null);

        internal static ODataTerm Date(string text) => new(text, Primary, false, true, false, null);

        internal static ODataTerm Constant(object? value) => new(string.Empty, Primary, false, false, true, value);

        internal static ODataTerm? AndAlso(ODataTerm? left, ODataTerm right)
        {
            if (right.IsConstant && right.Value is true)
            {
                return left;
            }

            if (left == null || right.IsConstant && right.Value is false)
            {
                return right;
            }

            if (left.IsConstant && left.Value is false)
            {
                return left;
            }

            return Join(left, right, true);
        }

        internal static ODataTerm Join(ODataTerm left, ODataTerm right, bool and)
        {
            var nested = and ? Or : And;
            return Expression($"{Wrap(left, nested)} {(and ? "and" : "or")} {Wrap(right, nested)}", and ? And : Or);
        }

        private static string Wrap(ODataTerm term, int nested) => term.Precedence == nested ? $"({term.Text})" : term.Text;

        internal static string Literal(object? value) => value switch
        {
            null => "null",
            string text => Quote(text),
            char character => Quote(new string(character, 1)),
            bool flag => flag ? "true" : "false",
            Enum member => Enumeration(member),
            Guid guid => guid.ToString("D", CultureInfo.InvariantCulture),
            DateTimeOffset instant => instant.Offset == TimeSpan.Zero
                ? instant.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss.FFFFFFF'Z'", CultureInfo.InvariantCulture)
                : instant.ToString("yyyy-MM-dd'T'HH:mm:ss.FFFFFFFzzz", CultureInfo.InvariantCulture),
            DateTime date => (date.Kind == DateTimeKind.Local ? date.ToUniversalTime() : date).ToString("yyyy-MM-dd'T'HH:mm:ss.FFFFFFF'Z'", CultureInfo.InvariantCulture),
            DateOnly day => day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            TimeOnly time => time.ToString("HH:mm:ss.fffffff", CultureInfo.InvariantCulture),
            TimeSpan duration => $"duration'{Duration(duration)}'",
            double number => Floating(number, double.IsNaN(number), double.IsPositiveInfinity(number), double.IsNegativeInfinity(number)),
            float number => Floating(number, float.IsNaN(number), float.IsPositiveInfinity(number), float.IsNegativeInfinity(number)),
            decimal or sbyte or byte or short or ushort or int or uint or long or ulong => ((IFormattable)value).ToString(null, CultureInfo.InvariantCulture),
            _ => throw new NotSupportedException($"ODataQuery cannot write {value}, a {value.GetType().Name}, as an OData literal: it writes strings, chars, numbers, bools, Guids, enums, DateTime, DateTimeOffset, DateOnly, TimeOnly, TimeSpan and null."),
        };

        private static string Enumeration(Enum member)
        {
            var name = member.ToString();

            if (!name.Contains(',', StringComparison.Ordinal))
            {
                return Quote(name);
            }

            var type = member.GetType();

            return $"{type.Namespace}.{type.Name}{Quote(name.Replace(", ", ",", StringComparison.Ordinal))}";
        }

        internal static string Quote(string text) => $"'{text.Replace("'", "''", StringComparison.Ordinal)}'";

        private static string Floating(IFormattable number, bool nan, bool positiveInfinity, bool negativeInfinity)
        {
            if (nan)
            {
                return "NaN";
            }

            if (positiveInfinity)
            {
                return "INF";
            }

            if (negativeInfinity)
            {
                return "-INF";
            }

            return number.ToString("R", CultureInfo.InvariantCulture);
        }

        // OData ABNF durationValue: [ SIGN ] "P" [ 1*DIGIT "D" ] [ "T" [ 1*DIGIT "H" ] [ 1*DIGIT "M" ] [ 1*DIGIT [ "." 1*DIGIT ] "S" ] ]
        private static string Duration(TimeSpan duration)
        {
            var builder = new StringBuilder();

            if (duration < TimeSpan.Zero)
            {
                builder.Append('-');
                duration = duration.Negate();
            }

            builder.Append('P');

            if (duration.Days > 0)
            {
                builder.Append(duration.Days.ToString(CultureInfo.InvariantCulture)).Append('D');
            }

            var ticks = duration.Ticks % TimeSpan.TicksPerDay;

            if (ticks > 0 || duration.Days == 0)
            {
                builder.Append('T');

                if (duration.Hours > 0)
                {
                    builder.Append(duration.Hours.ToString(CultureInfo.InvariantCulture)).Append('H');
                }

                if (duration.Minutes > 0)
                {
                    builder.Append(duration.Minutes.ToString(CultureInfo.InvariantCulture)).Append('M');
                }

                var seconds = ticks % TimeSpan.TicksPerMinute;

                if (seconds > 0 || ticks == 0)
                {
                    builder.Append((seconds / (decimal)TimeSpan.TicksPerSecond).ToString("0.#######", CultureInfo.InvariantCulture)).Append('S');
                }
            }

            return builder.ToString();
        }
    }
}
