using System;
using System.Globalization;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Radzen.Documents.Spreadsheet;

#nullable enable
/// <summary>
/// Represents the type of value contained in a cell.
/// </summary>
public enum CellDataType
{
    /// <summary>
    /// The cell contains a numeric value.
    /// </summary>
    Number,
    /// <summary>
    /// The cell contains a string value.
    /// </summary>
    String,
    /// <summary>
    /// The cell contains an error value.
    /// </summary>
    Error,
    /// <summary>
    /// The cell is empty.
    /// </summary>
    Empty,

    /// <summary>
    /// The cell contains a date value.
    /// </summary>
    Date,
    /// <summary>
    /// The cell contains a boolean value.
    /// </summary>
    Boolean
}

static class TypeExtensions
{
    // Excel serial date system: serial 1 = Jan 1, 1900.
    // Serial 60 = Feb 29, 1900 (a phantom date from the Lotus 1-2-3 bug; 1900 was not a leap year).
    // Dates from Mar 1, 1900 onward are offset by 2 to account for this phantom day.
    private static readonly DateTime Epoch = new(1900, 1, 1);
    private static readonly DateTime LotusLeapDayCutoff = new(1900, 3, 1);

    public static double ToNumber(this DateTime date)
    {
        var days = (date - Epoch).TotalDays;
        return days + (date >= LotusLeapDayCutoff ? 2 : 1);
    }

    public static DateTime ToDate(this double number)
    {
        if (number >= 61) // Mar 1, 1900 onward (after phantom Feb 29)
        {
            return Epoch.AddDays(number - 2);
        }

        if (number >= 1) // Jan 1, 1900 through Feb 28, 1900
        {
            return Epoch.AddDays(number - 1);
        }

        // Serials < 1 are pure time values; preserve fractional part
        return Epoch.AddDays(number);
    }

    public static bool IsNullable(this Type type) => Nullable.GetUnderlyingType(type) is not null;

    public static bool IsNumeric(this Type type)
    {
        if (type.IsNullable())
        {
            type = Nullable.GetUnderlyingType(type)!;
        }

        return type == typeof(int) || type == typeof(double) || type == typeof(decimal) || type == typeof(short) ||
               type == typeof(float) || type == typeof(long) || type == typeof(byte) || type == typeof(uint) ||
               type == typeof(ulong) || type == typeof(ushort) || type == typeof(sbyte);
    }
}

/// <summary>
/// Represents a value of a spreadsheet cell along with its type.
/// </summary>
[SuppressMessage("Design", "CA1036:Override methods on comparable types", Justification = "Comparison operators are intentionally omitted; use explicit comparison helpers.")]
public class CellData : IComparable, IComparable<CellData>
{
    /// <summary>
    /// Returns the data contained in the cell.
    /// </summary>
    public object? Value { get; }

    /// <summary>
    /// Returns the type of value contained in the cell.
    /// </summary>
    public CellDataType Type { get; }

    /// <summary>
    /// Creates a new instance of CellData with the specified data.
    /// </summary>
    public CellData(object? data)
    {
        if (data is null)
        {
            Value = null;
            Type = CellDataType.Empty;
            return;
        }

        var valType = data.GetType();
        var isNullable = valType.IsNullable();
        var nullableType = Nullable.GetUnderlyingType(valType);

        if (valType == typeof(string) || (isNullable && nullableType == typeof(string)))
        {
            var converted = TryConvertFromString(data.ToString(), out var convertedData, out var valueType);
            if (converted)
            {
                Value = convertedData;
                Type = valueType!.Value;
            }
            else
            {
                Value = data;
                Type = CellDataType.String;
            }
        }
        else
        {
            Type = GetValueType(data, valType, isNullable, nullableType);
            Value = (Type == CellDataType.Number) ? Convert.ToDouble(data, CultureInfo.InvariantCulture) : data;
        }
    }

    internal static bool TryConvertFromString(string? value, out object? converted, out CellDataType? valueType)
    {
        if (value is null)
        {
            converted = null;
            valueType = CellDataType.Empty;
            return false;
        }

        if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var valNum))
        {
            converted = valNum;
            valueType = CellDataType.Number;
            return true;
        }

        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var valDate))
        {
            valueType = CellDataType.Date;
            converted = valDate;
            return true;
        }

        if (bool.TryParse(value, out var valBool))
        {
            valueType = CellDataType.Boolean;
            converted = valBool;
            return true;
        }

        converted = null;
        valueType = null;
        return false;
    }

    internal bool TryGetInt(out int value, bool allowBooleans = true, bool nonNumericTextAsZero = false)
    {
        value = 0;
        if (!TryCoerceToNumber(out var number, allowBooleans, nonNumericTextAsZero))
        {
            return false;
        }
        value = (int)Math.Truncate(number);
        return true;
    }

    internal bool TryCoerceToNumber(out double number, bool allowBooleans, bool nonNumericTextAsZero)
    {
        number = 0d;
        switch (Type)
        {
            case CellDataType.Number:
                number = GetValueOrDefault<double>();
                return true;
            case CellDataType.String:
                if (TryConvertFromString(GetValueOrDefault<string>(), out var converted, out var valueType))
                {
                    if (valueType == CellDataType.Number)
                    {
                        number = (double)converted!;
                        return true;
                    }
                    if (valueType == CellDataType.Boolean && allowBooleans)
                    {
                        number = (bool)converted! ? 1d : 0d;
                        return true;
                    }
                }
                if (nonNumericTextAsZero)
                {
                    number = 0d;
                    return true;
                }
                return false;
            case CellDataType.Boolean:
                if (allowBooleans)
                {
                    number = GetValueOrDefault<bool>() ? 1d : 0d;
                    return true;
                }
                return false;
            default:
                return false;
        }
    }

    internal bool TryCoerceToDate(out DateTime date)
    {
        date = default;
        switch (Type)
        {
            case CellDataType.Date:
                date = GetValueOrDefault<DateTime>();
                return true;
            case CellDataType.Number:
                date = GetValueOrDefault<double>().ToDate();
                return true;
            case CellDataType.String:
                if (TryConvertFromString(GetValueOrDefault<string>(), out var converted, out var valueType))
                {
                    if (valueType == CellDataType.Date)
                    {
                        date = (DateTime)converted!;
                        return true;
                    }
                    if (valueType == CellDataType.Number)
                    {
                        date = ((double)converted!).ToDate();
                        return true;
                    }
                }
                return false;
            default:
                return false;
        }
    }

    private static CellDataType GetValueType(object? value, Type valType, bool isNullable, Type? nullableType)
    {
        if (value is null)
        {
            return CellDataType.Empty;
        }

        if (value is CellError)
        {
            return CellDataType.Error;
        }

        if (valType == typeof(string) || (isNullable && nullableType == typeof(string)))
        {
            return CellDataType.String;
        }

        if (valType.IsNumeric() || (isNullable && nullableType?.IsNumeric() == true))
        {
            return CellDataType.Number;
        }

        if (valType == typeof(bool) || (isNullable && nullableType == typeof(bool)))
        {
            return CellDataType.Boolean;
        }

        if (valType == typeof(DateTime) || (isNullable && nullableType == typeof(DateTime)))
        {
            return CellDataType.Date;
        }

        throw new NotSupportedException($"Unsupported cell value type: {valType}");
    }

    /// <summary>
    /// Returns the Cell's Value and attempts to cast it to T. If the Value is null or cannot be converted, returns the default value of T.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public T? GetValueOrDefault<T>()
    {
        var val = GetValue(typeof(T));

        return val is null ? default : (T)val;
    }

    private object? GetValue(Type type)
    {
        if (Value is null && Type == CellDataType.String)
        {
            return string.Empty;
        }

        if (Value?.GetType() == type)
        {
            return Value;
        }

        var conversionType = type;

        if (Nullable.GetUnderlyingType(type) is not null)
        {
            conversionType = Nullable.GetUnderlyingType(type);
        }

        if (conversionType == typeof(string))
        {
            return Value?.ToString();
        }

        if (Value is IConvertible && conversionType is not null)
        {
            try
            {
                return Convert.ChangeType(Value, conversionType, CultureInfo.InvariantCulture);
            }
            catch (InvalidCastException)
            {
                return null;
            }
            catch (FormatException)
            {
                return null;
            }
            catch (OverflowException)
            {
                return null;
            }
        }

        return Value;
    }

    /// <inheritdoc />
    public int CompareTo(CellData? other)
    {
        if (other is null)
        {
            return 1;
        }

        switch (Value)
        {
            case null when other.Value is null:
                return 0;
            case null:
                return -1;
        }

        if (other.Value is null)
        {
            return 1;
        }

        if (Type == CellDataType.Number && other.Type == CellDataType.Date)
        {
            return ((IComparable)Value).CompareTo(((DateTime)other.Value).ToNumber());
        }

        if (Type == CellDataType.Date && other.Type == CellDataType.Number)
        {
            return ((DateTime)Value).ToNumber().CompareTo((IComparable)other.Value);
        }

        if (Type == other.Type)
        {
            return CompareValues(Value, other.Value);
        }

        return Type.CompareTo(other.Type);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        if (Type == CellDataType.Boolean && Value is bool b)
        {
            return b ? "TRUE" : "FALSE";
        }

        return Value?.ToString() ?? string.Empty;
    }

    /// <inheritdoc />
    public int CompareTo(object? obj) => obj is CellData value ? CompareTo(value) : -1;


    /// <summary>
    /// Returns true if the cell is empty (null or empty string).
    /// </summary>
    public bool IsEmpty => Type == CellDataType.Empty;

    /// <summary>
    /// Returns true if the cell contains an error value.
    /// </summary>
    public bool IsError => Type == CellDataType.Error;

    // Excel compares text case-insensitively (=, <>, lookups, criteria all ignore case). Route every
    // string-vs-string comparison through OrdinalIgnoreCase so equality and ordering stay consistent.
    private static int CompareValues(object? left, object? right)
    {
        if (left is string ls && right is string rs)
        {
            return string.Compare(ls, rs, StringComparison.OrdinalIgnoreCase);
        }

        return ((IComparable)left!).CompareTo(right);
    }

    internal bool IsEqualTo(CellData other)
    {
        ArgumentNullException.ThrowIfNull(other);
        // special handling of empty cell vs empty string.
        if ((other.IsEmpty && string.IsNullOrEmpty(Value?.ToString())) || (IsEmpty && string.IsNullOrEmpty(other.Value?.ToString())))
        {
            return true;
        }

        if (Type != other.Type)
        {
            return false;
        }

        if (Type == CellDataType.Empty || other.Type == CellDataType.Empty)
        {
            return other.Value is null && Value is null;
        }

        return CompareValues(Value!, other.Value) == 0;
    }

    internal bool IsLessThan(CellData other)
    {
        ArgumentNullException.ThrowIfNull(other);
        if (Value is null || other.Value is null)
        {
            return false;
        }

        var compareResult = CompareValues(Value, other.Value);
        return compareResult < 0;
    }

    internal bool IsGreaterThan(CellData other)
    {
        ArgumentNullException.ThrowIfNull(other);
        if (Value is null || other.Value is null)
        {
            return false;
        }

        var compareResult = CompareValues(Value, other.Value);
        return compareResult > 0;
    }

    internal bool IsLessThanOrEqualTo(CellData other)
    {
        ArgumentNullException.ThrowIfNull(other);
        if (Value is null || other.Value is null)
        {
            return false;
        }

        var compareResult = CompareValues(Value, other.Value);
        return compareResult <= 0;
    }

    internal bool IsGreaterThanOrEqualTo(CellData other)
    {
        ArgumentNullException.ThrowIfNull(other);
        if (Value is null || other.Value is null)
        {
            return false;
        }

        var compareResult = CompareValues(Value, other.Value);
        return compareResult >= 0;
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(Type, Value);
    }

    internal CellData(object value, CellDataType type)
    {
        Value = value;
        Type = type;
    }

    /// <summary>
    /// Creates a CellData instance for a string value.
    /// </summary>
    public static CellData FromString(string value) => new(value, CellDataType.String);

    /// <summary>
    /// Creates a CellData instance for a numeric value.
    /// </summary>
    public static CellData FromNumber(double value) => new(value, CellDataType.Number);

    /// <summary>
    /// Creates a CellData instance for a boolean value.
    /// </summary>
    public static CellData FromBoolean(bool value) => new(value, CellDataType.Boolean);

    /// <summary>
    /// Creates a CellData instance for a date value.
    /// </summary>
    public static CellData FromDate(DateTime value) => new(value, CellDataType.Date);

    /// <summary>
    /// Creates a CellData instance for an error value.
    /// </summary>
    public static CellData FromError(CellError error) => new(error, CellDataType.Error);

    internal bool MatchesCriteria(CellData criteria)
    {
        ArgumentNullException.ThrowIfNull(criteria);
        if (criteria.IsError)
        {
            return false;
        }

        // Empty criteria only matches empty cells.
        if (criteria.IsEmpty)
        {
            return IsEmpty;
        }

        // String criteria carries Excel's operator/wildcard syntax; other types are direct equality.
        if (criteria.Type == CellDataType.String)
        {
            return MatchesStringCriteria(criteria.GetValueOrDefault<string>() ?? "");
        }

        return IsEqualTo(criteria);
    }

    private bool MatchesStringCriteria(string criteria)
    {
        var (op, operand) = SplitCriteriaOperator(criteria);

        // Ordering operators are numeric/date only.
        if (op is ">" or "<" or ">=" or "<=")
        {
            return EvaluateNumericComparison(op, operand);
        }

        // op is "=", "<>", or "" (default equality). "<>" is the negation of equality and is TRUE for
        // incomparable cells (text vs number, blank, etc.) - that is what "not equal" means in Excel.
        var isEqual = MatchesEquality(operand);
        return op == "<>" ? !isEqual : isEqual;
    }

    private static (string op, string operand) SplitCriteriaOperator(string criteria)
    {
        if (criteria.StartsWith(">=", StringComparison.Ordinal))
        {
            return (">=", criteria[2..].Trim());
        }

        if (criteria.StartsWith("<=", StringComparison.Ordinal))
        {
            return ("<=", criteria[2..].Trim());
        }

        if (criteria.StartsWith("<>", StringComparison.Ordinal))
        {
            return ("<>", criteria[2..]);
        }

        if (criteria.StartsWith('>'))
        {
            return (">", criteria[1..].Trim());
        }

        if (criteria.StartsWith('<'))
        {
            return ("<", criteria[1..].Trim());
        }

        if (criteria.StartsWith('='))
        {
            return ("=", criteria[1..]);
        }

        return ("", criteria);
    }

    private bool MatchesEquality(string operand)
    {
        var cellString = ToString() ?? "";

        if (operand.Contains('*', StringComparison.Ordinal) || operand.Contains('?', StringComparison.Ordinal))
        {
            return Wildcard.IsFullMatch(cellString, operand);
        }

        // A numeric operand compares numerically against numeric/date cells (so "20" matches the number 20).
        if (double.TryParse(operand, NumberStyles.Any, CultureInfo.InvariantCulture, out var numericOperand))
        {
            return Type switch
            {
                CellDataType.Number => GetValueOrDefault<double>() == numericOperand,
                CellDataType.Date => GetValueOrDefault<DateTime>().ToNumber() == numericOperand,
                _ => false
            };
        }

        if (IsEmpty)
        {
            return operand.Length == 0;
        }

        return string.Equals(cellString, operand, StringComparison.OrdinalIgnoreCase);
    }

    private bool EvaluateNumericComparison(string op, string operand)
    {
        if (!double.TryParse(operand, NumberStyles.Any, CultureInfo.InvariantCulture, out var numericValue))
        {
            // Text-ordering comparisons (e.g. ">m") are not supported.
            return false;
        }

        double cellValue;
        if (Type == CellDataType.Number)
        {
            cellValue = GetValueOrDefault<double>();
        }
        else if (Type == CellDataType.Date)
        {
            cellValue = GetValueOrDefault<DateTime>().ToNumber();
        }
        else
        {
            return false;
        }

        return op switch
        {
            ">" => cellValue > numericValue,
            "<" => cellValue < numericValue,
            ">=" => cellValue >= numericValue,
            "<=" => cellValue <= numericValue,
            _ => false
        };
    }
}