using System;
using System.Globalization;

namespace Radzen.Blazor.Spreadsheet;

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
    public static double ToNumber(this DateTime date)
    {
        var epoch = new DateTime(1900, 1, 1);
        return (date - epoch).Days;
    }

    public static DateTime ToDate(this double number)
    {
        var epoch = new DateTime(1900, 1, 1);
        return epoch.AddDays(number);
    }

    public static bool IsNullable(this Type type) => Nullable.GetUnderlyingType(type) != null;

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
        if (data == null)
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
            Value = (Type == CellDataType.Number) ? Convert.ToDouble(data) : data;
        }
    }

    internal static bool TryConvertFromString(string? value, out object? converted, out CellDataType? valueType)
    {
        if (value == null)
        {
            converted = null;
            valueType = CellDataType.Empty;
            return false;
        }

        if (double.TryParse(value, out var valNum))
        {
            converted = valNum;
            valueType = CellDataType.Number;
            return true;
        }

        if (DateTime.TryParse(value, out var valDate))
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

    private static CellDataType GetValueType(object? value, Type valType, bool isNullable, Type? nullableType)
    {
        if (value == null)
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

        return val == null ? default : (T)val;
    }

    private object? GetValue(Type type)
    {
        if (Value == null && Type == CellDataType.String)
        {
            return string.Empty;
        }

        if (Value?.GetType() == type)
        {
            return Value;
        }

        var conversionType = type;

        if (Nullable.GetUnderlyingType(type) != null)
        {
            conversionType = Nullable.GetUnderlyingType(type);
        }

        if (conversionType == typeof(string))
        {
            return Value?.ToString();
        }

        if (Value is IConvertible && conversionType != null)
        {
            try
            {
                return Convert.ChangeType(Value, conversionType, CultureInfo.InvariantCulture);
            }
            catch (Exception)
            {
                return null;
            }
        }

        return Value;
    }

    /// <inheritdoc />
    public int CompareTo(CellData? other)
    {
        if (other == null)
        {
            return 1;
        }

        switch (Value)
        {
            case null when other.Value == null:
                return 0;
            case null:
                return -1;
        }

        if (other.Value == null)
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
            return ((IComparable)Value).CompareTo((IComparable)other.Value);
        }

        return Type.CompareTo(other.Type);
    }

    /// <inheritdoc />
    public override string ToString()
    {
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

    /// <summary>
    /// Checks if this cell data is equal to another cell data.
    /// </summary>
    public bool IsEqualTo(CellData other)
    {
        // special handling of empty cell vs empty string.
        if (other.IsEmpty && string.IsNullOrEmpty(Value?.ToString()) || IsEmpty && string.IsNullOrEmpty(other.Value?.ToString()))
        {
            return true;
        }

        if (Type != other.Type)
        {
            return false;
        }

        if (Type == CellDataType.Empty || Type == CellDataType.Empty)
        {
            return other.Value == null && Value == null;
        }

        return ((IComparable)Value!).CompareTo(other.Value) == 0;
    }

    /// <summary>
    /// Checks if this cell data is less than another cell data.
    /// </summary>
    /// <param name="other"></param>
    public bool IsLessThan(CellData other)
    {
        if (Value == null || other.Value == null)
            return false;

        var compareResult = ((IComparable)Value).CompareTo(other.Value);
        return compareResult < 0;
    }

    /// <summary>
    /// Checks if this cell data is greater than another cell data.
    /// </summary>
    /// <param name="other"></param>
    public bool IsGreaterThan(CellData other)
    {
        if (Value == null || other.Value == null)
            return false;

        var compareResult = ((IComparable)Value).CompareTo(other.Value);
        return compareResult > 0;
    }

    /// <summary>
    /// Checks if this cell data is less than or equal to another cell data.
    /// </summary>
    /// <param name="other"></param>
    public bool IsLessThanOrEqualTo(CellData other)
    {
        if (Value == null || other.Value == null)
            return false;

        var compareResult = ((IComparable)Value).CompareTo(other.Value);
        return compareResult <= 0;
    }

    /// <summary>
    /// Checks if this cell data is greater than or equal to another cell data.
    /// </summary>
    /// <param name="other"></param>
    public bool IsGreaterThanOrEqualTo(CellData other)
    {
        if (Value == null || other.Value == null)
            return false;

        var compareResult = ((IComparable)Value).CompareTo(other.Value);
        return compareResult >= 0;
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return Value == null ? -1 : Value.GetHashCode();
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
}