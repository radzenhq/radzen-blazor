using System;
using System.ComponentModel;
using System.Globalization;

namespace RadzenBlazorDemos;

[TypeConverter(typeof(DollarsTypeConverter))]
public readonly record struct Dollars(decimal Amount) : IComparable<decimal>
{
    public int CompareTo(decimal other)
    {
        return Amount.CompareTo(other);
    }

    public string ToString(string format, CultureInfo culture = null) => Amount.ToString(format, culture ?? CultureInfo.CreateSpecificCulture("en-US"));
    public override string ToString() => Amount.ToString("C2", CultureInfo.CreateSpecificCulture("en-US"));
}

public class DollarsTypeConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
    {
        if (sourceType == typeof(decimal) ||
            sourceType == typeof(string)) 
        {
            return true;
        }

        return base.CanConvertFrom(context, sourceType);
    }

    public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
    {
        if (destinationType == typeof(decimal))
            return true;
        
        return base.CanConvertTo(context, destinationType);
    }

    public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
    {
        if (value is decimal d)
            return new Dollars(d);

        if (value is string s)
            return decimal.TryParse(s, out var val) ? new Dollars(val) : null;
        
        return base.ConvertFrom(context, culture, value);
    }

    public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
    {
        if (destinationType == typeof(decimal) && value is Dollars d)
            return d.Amount;
        
        return base.ConvertTo(context, culture, value, destinationType);
    }
}