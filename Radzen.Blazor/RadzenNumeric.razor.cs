using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenNumeric component.
    /// </summary>
    /// <typeparam name="TValue">The type of the t value.</typeparam>
    /// <example>
    /// <code>
    /// &lt;RadzenNumeric TValue="int" Min="1" Max="10" Change=@(args => Console.WriteLine($"Value: {args}")) /&gt;
    /// </code>
    /// </example>
    public partial class RadzenNumeric<TValue> : FormComponentWithAutoComplete<TValue>
    {
        /// <summary>
        /// Specifies additional custom attributes that will be rendered by the input.
        /// </summary>
        /// <value>The attributes.</value>
        [Parameter]
        public IReadOnlyDictionary<string, object> InputAttributes { get; set; }

        /// <summary>
        /// Gets input reference.
        /// </summary>
        protected ElementReference input;

        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            return GetClassList("rz-numeric")
                                        .Add($"rz-text-align-{Enum.GetName(typeof(TextAlign), TextAlign).ToLower()}")
                                        .ToString();
        }

        string GetInputCssClass()
        {
            return GetClassList("rz-numeric-input")
                        .Add("rz-inputtext")
                        .ToString();
        }

        private string getOnInput()
        {
            object minArg = Min;
            object maxArg = Max;
            string isNull = IsNullable.ToString().ToLower();
            return (Min != null || Max != null) ? $@"Radzen.numericOnInput(event, {minArg ?? "null"}, {maxArg ?? "null"}, {isNull})" : "";
        }

        private string getOnPaste()
        {
            object minArg = Min;
            object maxArg = Max;

            return Min != null || Max != null ? $@"Radzen.numericOnPaste(event, {minArg ?? "null"}, {maxArg ?? "null"})" : "";
        }

        bool? isNullable;
        bool IsNullable
        {
            get
            {
                if (isNullable == null)
                {
                    isNullable = typeof(TValue).IsGenericType && typeof(TValue).GetGenericTypeDefinition() == typeof(Nullable<>);
                }

                return isNullable.Value;
            }
        }

        private bool IsNumericType(object value) => value switch
        {
            sbyte => true,
            byte => true,
            short => true,
            ushort => true,
            int => true,
            uint => true,
            long => true,
            ulong => true,
            float => true,
            double => true,
            decimal => true,
            _ => false
        };

#if NET7_0_OR_GREATER
        /// <summary>
        /// Use native numeric type to process the step up/down while checking for possible overflow errors
        /// and clamping to Min/Max values
        /// </summary>
        /// <typeparam name="TNum"></typeparam>
        /// <param name="valueToUpdate"></param>
        /// <param name="stepUp"></param>
        /// <param name="decimalStep"></param>
        /// <returns></returns>
        private TNum UpdateValueWithStepNumeric<TNum>(TNum valueToUpdate, bool stepUp, decimal decimalStep) 
            where TNum : struct, System.Numerics.INumber<TNum>, System.Numerics.IMinMaxValue<TNum>
        {
            var step = TNum.CreateSaturating(decimalStep);

            if (stepUp && (TNum.MaxValue - step) < valueToUpdate)
            {
                return valueToUpdate;
            }
            if (!stepUp && (TNum.MinValue + step) > valueToUpdate)
            {
                return valueToUpdate;
            }

            var newValue = valueToUpdate + (stepUp ? step : -step);

            if (Max.HasValue && newValue > TNum.CreateSaturating(Max.Value) 
                || Min.HasValue && newValue < TNum.CreateSaturating(Min.Value) 
                || object.Equals(Value, newValue))
            {
                return valueToUpdate;
            }

            return newValue;
        }
#endif

        async System.Threading.Tasks.Task UpdateValueWithStep(bool stepUp)
        {
            if (Disabled || ReadOnly)
            {
                return;
            }

            var step = string.IsNullOrEmpty(Step) || Step == "any" ? 1 : decimal.Parse(Step.Replace(",", "."), System.Globalization.CultureInfo.InvariantCulture);
            TValue newValue;

#if NET7_0_OR_GREATER
            if (IsNumericType(Value))
            {
                // cannot call UpdateValueWithStepNumeric directly because TValue is not value type constrained
                Func<dynamic, bool, decimal, dynamic> dynamicWrapper = (dynamic value, bool stepUp, decimal step) 
                    => UpdateValueWithStepNumeric(value, stepUp, step);

                newValue = dynamicWrapper(Value, stepUp, step);
            }
            else
#endif
            {
                var valueToUpdate = ConvertToDecimal(Value);

                var newValueToUpdate = valueToUpdate + (stepUp ? step : -step);

                if (Max.HasValue && newValueToUpdate > Max.Value || Min.HasValue && newValueToUpdate < Min.Value || object.Equals(Value, newValueToUpdate))
                {
                    return;
                }

                if ((typeof(TValue) == typeof(byte) || typeof(TValue) == typeof(byte?)) && (newValueToUpdate < 0 || newValueToUpdate > 255))
                {
                    return;
                }

                newValue = ConvertFromDecimal(newValueToUpdate);
            }

            if(object.Equals(newValue, Value))
                return;

            Value = newValue;

            await ValueChanged.InvokeAsync(Value);
            if (FieldIdentifier.FieldName != null) { EditContext?.NotifyFieldChanged(FieldIdentifier); }
            await Change.InvokeAsync(Value);

            StateHasChanged();
        }

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        /// <value>The value.</value>
        [Parameter]
        public override TValue Value
        {
            get
            {
                return _value;
            }
            set
            {
                if (!EqualityComparer<TValue>.Default.Equals(value, _value))
                {
                    _value = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets the formatted value.
        /// </summary>
        /// <value>The formatted value.</value>
        protected string FormattedValue
        {
            get
            {
                if (Value != null)
                {
                    if (Format != null)
                    {
                        if (Value is IFormattable formattable)
                        {
                            return formattable.ToString(Format, Culture);
                        }
                        decimal decimalValue = ConvertToDecimal(Value);
                        return decimalValue.ToString(Format, Culture);
                    }
                    return Value.ToString();
                }
                else
                {
                    return "";
                }
            }
            set
            {
                _ = InternalValueChanged(value);
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance has value.
        /// </summary>
        /// <value><c>true</c> if this instance has value; otherwise, <c>false</c>.</value>
        public override bool HasValue
        {
            get
            {
                return Value != null;
            }
        }

        /// <summary>
        /// Gets or sets the format.
        /// </summary>
        /// <value>The format.</value>
        [Parameter]
        public string Format { get; set; }

        /// <summary>
        /// Gets or sets the step.
        /// </summary>
        /// <value>The step.</value>
        [Parameter]
        public string Step { get; set; }

        private bool IsInteger()
        {
            var type = typeof(TValue).IsGenericType ? typeof(TValue).GetGenericArguments()[0] : typeof(TValue);

            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether is read only.
        /// </summary>
        /// <value><c>true</c> if is read only; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool ReadOnly { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether input automatic complete is enabled.
        /// </summary>
        /// <value><c>true</c> if input automatic complete is enabled; otherwise, <c>false</c>.</value>
        [Parameter]
        public override bool AutoComplete { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether up down buttons are shown.
        /// </summary>
        /// <value><c>true</c> if up down buttons are shown; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool ShowUpDown { get; set; } = true;

        /// <summary>
        /// Gets or sets the text align.
        /// </summary>
        /// <value>The text align.</value>
        [Parameter]
        public TextAlign TextAlign { get; set; } = TextAlign.Left;

        /// <summary>
        /// Handles the <see cref="E:Change" /> event.
        /// </summary>
        /// <param name="args">The <see cref="ChangeEventArgs"/> instance containing the event data.</param>
        protected async System.Threading.Tasks.Task OnChange(ChangeEventArgs args)
        {
            await InternalValueChanged(args.Value);
        }

        private string RemoveNonNumericCharacters(object value)
        {
            string valueStr = value as string;
            if (valueStr == null)
            {
                valueStr = value.ToString();
            }

            if (!string.IsNullOrEmpty(Format))
            {
                string formattedStringWithoutPlaceholder = Format.Replace("#", "").Trim();
                
                if (valueStr.Contains(Format))
                {
                    string currencyDecimalSeparator = Culture.NumberFormat.CurrencyDecimalSeparator;

                    string[] splitFormatString = formattedStringWithoutPlaceholder.Split(currencyDecimalSeparator);
                    string[] splitValueString = valueStr.Split(currencyDecimalSeparator);
                    int lengthDifference = splitValueString[0].Length - splitFormatString[0].Length;
                    formattedStringWithoutPlaceholder = formattedStringWithoutPlaceholder.PadLeft(formattedStringWithoutPlaceholder.Length + lengthDifference, '0');
                }
                
                valueStr = valueStr.Replace(formattedStringWithoutPlaceholder, "");
            }

            return new string(valueStr.Where(c => char.IsDigit(c) || char.IsPunctuation(c)).ToArray()).Replace("%", "");
        }

        /// <summary>
        /// Gets or sets the function which returns TValue from string.
        /// </summary>
        [Parameter]
        public Func<string, TValue> ConvertValue { get; set; }

        private async System.Threading.Tasks.Task InternalValueChanged(object value)
        {
            TValue newValue;
            try
            {
                if (value is TValue typedValue)
                {
                    newValue = typedValue;
                }
                else if (ConvertValue != null)
                {
                    newValue = ConvertValue($"{value}");
                }
                else
                {
                    BindConverter.TryConvertTo<TValue>(RemoveNonNumericCharacters(value), Culture, out newValue);
                }
            }
            catch
            {
                newValue = default(TValue);
            }

            newValue = ApplyMinMax(newValue);

            if (EqualityComparer<TValue>.Default.Equals(Value, newValue))
            {
                await JSRuntime.InvokeAsync<string>("Radzen.setInputValue", input, FormattedValue);
                return;
            }

            Value = newValue;
            if (!ValueChanged.HasDelegate)
            {
                await JSRuntime.InvokeAsync<string>("Radzen.setInputValue", input, FormattedValue);
            }

            await ValueChanged.InvokeAsync(Value);
            if (FieldIdentifier.FieldName != null) { EditContext?.NotifyFieldChanged(FieldIdentifier); }
            await Change.InvokeAsync(Value);
        }
        
        private TValue ApplyMinMax(TValue newValue)
        {
            if (Max == null && Min == null || newValue == null)
            {
                return newValue;
            }

            if (newValue is IComparable<decimal> c)
            {
                if (Max.HasValue && c.CompareTo(Max.Value) > 0)
                    return ConvertFromDecimal(Max.Value);
                if (Min.HasValue && c.CompareTo(Min.Value) < 0)
                    return ConvertFromDecimal(Min.Value);
                return newValue;
            }

            decimal? newValueAsDecimal;
            try
            {
                newValueAsDecimal = ConvertToDecimal(newValue);
            }
            catch
            {
                newValueAsDecimal = default;
            }

            if (newValueAsDecimal > Max)
            {
                newValueAsDecimal = Max.Value;
            }

            if (newValueAsDecimal < Min)
            {
                newValueAsDecimal = Min.Value;
            }
            return ConvertFromDecimal(newValueAsDecimal);
        }

        private decimal ConvertToDecimal(TValue input)
        {
            if (input == null)
                return default;

            var converter = TypeDescriptor.GetConverter(typeof(TValue));
            if (converter.CanConvertTo(typeof(decimal)))
                return (decimal)converter.ConvertTo(null, Culture, input, typeof(decimal));
            
            return (decimal)ConvertType.ChangeType(input, typeof(decimal));
        }

        private TValue ConvertFromDecimal(decimal? input)
        {
            if (input == null)
                return default;

            var converter = TypeDescriptor.GetConverter(typeof(TValue));
            if (converter.CanConvertFrom(typeof(decimal)))
            {
                return (TValue)converter.ConvertFrom(null, Culture, input);
            }
            
            return (TValue)ConvertType.ChangeType(input, typeof(TValue));
        }

        /// <summary>
        /// Determines the minimum value.
        /// </summary>
        /// <value>The minimum value.</value>
        [Parameter]
        public decimal? Min { get; set; }

        /// <summary>
        /// Determines the maximum value.
        /// </summary>
        /// <value>The maximum value.</value>
        [Parameter]
        public decimal? Max { get; set; }

        /// <inheritdoc />
        public override async Task SetParametersAsync(ParameterView parameters)
        {
            bool minChanged = parameters.DidParameterChange(nameof(Min), Min);
            bool maxChanged = parameters.DidParameterChange(nameof(Max), Max);

            await base.SetParametersAsync(parameters);

            if (minChanged && IsJSRuntimeAvailable)
            {
                await InternalValueChanged(Value);
            }

            if (maxChanged && IsJSRuntimeAvailable)
            {
                await InternalValueChanged(Value);
            }
        }

        bool preventKeyPress = false;
        async Task OnKeyPress(KeyboardEventArgs args)
        {
            var key = args.Code != null ? args.Code : args.Key;

            if (key == "ArrowUp" || key == "ArrowDown")
            {
                preventKeyPress = true;

                if (key == "ArrowUp")
                {
                    await UpdateValueWithStep(true);
                }
                else
                {
                    await UpdateValueWithStep(false);
                }
            }
            else
            {
                preventKeyPress = false;
            }
        }

        /// <summary>
        /// Gets or sets the up button aria-label attribute.
        /// </summary>
        [Parameter]
        public string UpAriaLabel { get; set; } = "Up";

        /// <summary>
        /// Gets or sets the down button aria-label attribute.
        /// </summary>
        [Parameter]
        public string DownAriaLabel { get; set; } = "Down";

#if NET5_0_OR_GREATER
        /// <summary>
        /// Sets the focus on the input element.
        /// </summary>
        public override async ValueTask FocusAsync()
        {
            await input.FocusAsync();
        }
#endif
    }
}
