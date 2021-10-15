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
    /// Implements the <see cref="Radzen.FormComponent{TValue}" />
    /// </summary>
    /// <typeparam name="TValue">The type of the t value.</typeparam>
    /// <seealso cref="Radzen.FormComponent{TValue}" />
    /// <example>
    /// <code>
    /// &lt;RadzenNumeric TValue="int" Min="1" Max="10" Change=@(args => Console.WriteLine($"Value: {args}")) /&gt;
    /// </code>
    /// </example>
    public partial class RadzenNumeric<TValue> : FormComponent<TValue>
    {
        protected ElementReference input;

        /// <summary>
        /// Gets the component CSS class.
        /// </summary>
        /// <returns>System.String.</returns>
        protected override string GetComponentCssClass()
        {
            return GetClassList("rz-spinner").ToString();
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
                        return decimalValue.ToString(Format);
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
        public bool AutoComplete { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether up down buttons are shown.
        /// </summary>
        /// <value><c>true</c> if up down buttons are shown; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool ShowUpDown { get; set; } = true;

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
            return new string(valueStr.Where(c => char.IsDigit(c) || char.IsPunctuation(c)).ToArray());
        }

        private async System.Threading.Tasks.Task InternalValueChanged(object value)
        {
            TValue newValue;
            BindConverter.TryConvertTo<TValue>(RemoveNonNumericCharacters(value), Culture, out newValue);

            decimal? newValueAsDecimal = newValue == null ? default(decimal?) : (decimal)ConvertType.ChangeType(newValue, typeof(decimal));

            if (object.Equals(Value, newValue) && !ValueChanged.HasDelegate)
            {
                await JSRuntime.InvokeAsync<string>("Radzen.setInputValue", input, Value);
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
                await JSRuntime.InvokeAsync<string>("Radzen.setInputValue", input, Value);
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

        /// <summary>
        /// Set parameters as an asynchronous operation.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
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
    }
}