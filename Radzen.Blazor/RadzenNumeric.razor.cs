using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
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
    public partial class RadzenNumeric<TValue> : FormComponent<TValue>
    {
        /// <summary>
        /// Specifies additional custom attributes that will be rendered by the input.
        /// </summary>
        /// <value>The attributes.</value>
        public IReadOnlyDictionary<string, object> InputAttributes { get; set; }

        /// <summary>
        /// Gets input reference.
        /// </summary>
        protected ElementReference input;

        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            return GetClassList("rz-spinner")
                                        .Add($"rz-text-align-{Enum.GetName(typeof(TextAlign), TextAlign).ToLower()}")
                                        .ToString();
        }

        string GetInputCssClass()
        {
            return GetClassList("rz-spinner-input")
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

        async System.Threading.Tasks.Task UpdateValueWithStep(bool stepUp)
        {
            if (Disabled || ReadOnly)
            {
                return;
            }

            var step = string.IsNullOrEmpty(Step) || Step == "any" ? 1 : double.Parse(Step.Replace(",", "."), System.Globalization.CultureInfo.InvariantCulture);

            var valueToUpdate = Value != null ? Convert.ChangeType(Value, typeof(decimal)) : (decimal)Convert.ChangeType(default(decimal), typeof(decimal));

            var newValue = ((decimal)Convert.ChangeType(valueToUpdate, typeof(decimal))) + (decimal)Convert.ChangeType(stepUp ? step : -step, typeof(decimal));

            if (Max.HasValue && newValue > Max.Value || Min.HasValue && newValue < Min.Value || object.Equals(Value, newValue))
            {
                return;
            }

            Value = (TValue)ConvertType.ChangeType(newValue, typeof(TValue));

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
                        decimal decimalValue = (decimal)Convert.ChangeType(Value, typeof(decimal));
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
        public bool AutoComplete { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating the type of built-in autocomplete
        /// the browser should use.
        /// <see cref="Blazor.AutoCompleteType" />
        /// </summary>
        /// <value>
        /// The type of built-in autocomplete.
        /// </value>
        [Parameter]
        public AutoCompleteType AutoCompleteType { get; set; } = AutoCompleteType.On;

        /// <summary>
        /// Gets the autocomplete attribute's string value.
        /// </summary>
        /// <value>
        /// <c>off</c> if the AutoComplete parameter is false or the
        /// AutoCompleteType parameter is "off". When the AutoComplete
        /// parameter is true, the value is <c>on</c> or, if set, the value of
        /// AutoCompleteType.</value>
        public string AutoCompleteAttribute
        {
            get => !AutoComplete ? "off" : AutoCompleteType.GetAutoCompleteValue();
        }

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
                valueStr = valueStr.Replace(Format.Replace("#", "").Trim(), "");
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
                if (ConvertValue != null)
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

            decimal? newValueAsDecimal;
            try
            {
                newValueAsDecimal = newValue == null ? default(decimal?) : (decimal)ConvertType.ChangeType(newValue, typeof(decimal));
            }
            catch
            {
                newValueAsDecimal = default(TValue) == null ? default(decimal?) : (decimal)ConvertType.ChangeType(default(TValue), typeof(decimal));
            }

            if (object.Equals(Value, newValue) && (!ValueChanged.HasDelegate || !string.IsNullOrEmpty(Format)))
            {
                await JSRuntime.InvokeAsync<string>("Radzen.setInputValue", input, FormattedValue);
                return;
            }

            if (Max.HasValue && newValueAsDecimal > Max.Value)
            {
                newValueAsDecimal = Max.Value;
            }

            if (Min.HasValue && newValueAsDecimal < Min.Value)
            {
                newValueAsDecimal = Min.Value;
            }

            Value = (TValue)ConvertType.ChangeType(newValueAsDecimal, typeof(TValue));
            if (!ValueChanged.HasDelegate)
            {
                await JSRuntime.InvokeAsync<string>("Radzen.setInputValue", input, FormattedValue);
            }

            await ValueChanged.InvokeAsync(Value);
            if (FieldIdentifier.FieldName != null) { EditContext?.NotifyFieldChanged(FieldIdentifier); }
            await Change.InvokeAsync(Value);
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

            if (minChanged && Min.HasValue && Value != null && IsJSRuntimeAvailable)
            {
                decimal decimalValue = (decimal)Convert.ChangeType(Value, typeof(decimal));
                if (decimalValue < Min.Value)
                {
                    await InternalValueChanged(Min.Value);
                }
            }

            if (maxChanged && Max.HasValue && Value != null && IsJSRuntimeAvailable)
            {
                decimal decimalValue = (decimal)Convert.ChangeType(Value, typeof(decimal));
                if (decimalValue > Max.Value)
                {
                    await InternalValueChanged(Max.Value);
                }
            }
        }

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
