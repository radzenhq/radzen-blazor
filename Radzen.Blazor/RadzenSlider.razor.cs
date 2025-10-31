﻿using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    /// <summary>
    /// A slider component for selecting numeric values by dragging a handle along a track.
    /// RadzenSlider supports single value selection or range selection (min/max) with horizontal or vertical orientation.
    /// Provides an intuitive way to select numeric values within a range, especially useful when the exact value is less important than the approximate position,
    /// you want to show the valid range visually, or the input should be constrained to specific increments.
    /// Features single value or min/max range selection, Horizontal (default) or Vertical layout, Min/Max values defining the selectable range,
    /// Step property controlling value granularity, colored track showing selected portion for visual feedback, Arrow key support for precise adjustment, and drag gestures on mobile devices.
    /// For range selection, set Range=true and bind to IEnumerable&lt;TValue&gt; (e.g., IEnumerable&lt;int&gt;) for min/max values.
    /// Common uses include price filters, volume controls, zoom levels, or any bounded numeric input.
    /// </summary>
    /// <typeparam name="TValue">The type of the slider value. Supports numeric types (int, decimal, double) or IEnumerable for range selection.</typeparam>
    /// <example>
    /// Basic slider:
    /// <code>
    /// &lt;RadzenSlider @bind-Value=@volume TValue="int" Min="0" Max="100" /&gt;
    /// @code {
    ///     int volume = 50;
    /// }
    /// </code>
    /// Range slider for price filter:
    /// <code>
    /// &lt;RadzenSlider @bind-Value=@priceRange TValue="IEnumerable&lt;decimal&gt;" Range="true" 
    ///               Min="0" Max="1000" Step="10" /&gt;
    /// @code {
    ///     IEnumerable&lt;decimal&gt; priceRange = new decimal[] { 100, 500 };
    /// }
    /// </code>
    /// Vertical slider with step:
    /// <code>
    /// &lt;RadzenSlider @bind-Value=@value TValue="double" Min="0" Max="1" Step="0.1" 
    ///               Orientation="Orientation.Vertical" Style="height: 200px;" /&gt;
    /// </code>
    /// </example>
    public partial class RadzenSlider<TValue> : FormComponent<TValue>
    {
        ElementReference handle;
        ElementReference minHandle;
        ElementReference maxHandle;

        bool visibleChanged = false;
        bool disabledChanged = false;
        bool maxChanged = false;
        bool minChanged = false;
        bool rangeChanged = false;
        bool stepChanged = false;
        bool firstRender = true;

        decimal Left => ((MinValue() - Min) * 100) / (Max - Min);
        decimal SecondLeft => ((MaxValue() - Min) * 100) / (Max - Min);

        /// <inheritdoc />
        public override async Task SetParametersAsync(ParameterView parameters)
        {
            visibleChanged = parameters.DidParameterChange(nameof(Visible), Visible);
            disabledChanged = parameters.DidParameterChange(nameof(Disabled), Disabled);
            maxChanged = parameters.DidParameterChange(nameof(Max), Max);
            minChanged = parameters.DidParameterChange(nameof(Min), Min);
            rangeChanged = parameters.DidParameterChange(nameof(Range), Range);
            stepChanged = parameters.DidParameterChange(nameof(Step), Step);

            await base.SetParametersAsync(parameters);

            if ((visibleChanged || disabledChanged) && !firstRender)
            {
                if (Visible == false || Disabled == true)
                {
                    Dispose();
                }
            }
        }

        /// <inheritdoc />
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);

            this.firstRender = firstRender;

            if (firstRender || visibleChanged || disabledChanged || maxChanged || minChanged || rangeChanged || stepChanged)
            {
                visibleChanged = false;
                disabledChanged = false;

                if (maxChanged)
                {
                    maxChanged = false;
                }

                if (minChanged)
                {
                    minChanged = false;
                }

                if (rangeChanged)
                {
                    rangeChanged = false;
                }

                if (stepChanged)
                {
                    stepChanged = false;
                }

                if (Visible && !Disabled)
                {
                    await JSRuntime.InvokeVoidAsync("Radzen.createSlider", UniqueID, Reference, Element, Range, Range ? minHandle : handle, maxHandle, Min, Max, Value, Step, Orientation == Orientation.Vertical);

                    StateHasChanged();
                }
            }
        }

        /// <inheritdoc />
        public override void Dispose()
        {
            base.Dispose();

            if (IsJSRuntimeAvailable)
            {
                JSRuntime.InvokeVoid("Radzen.destroySlider", UniqueID, Element);
            }
        }

        /// <summary>
        /// Called when value changed.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="isMin">if set to <c>true</c> [is minimum].</param>
        [JSInvokable("RadzenSlider.OnValueChange")]
        public async System.Threading.Tasks.Task OnValueChange(decimal value, bool isMin)
        {
            var step = string.IsNullOrEmpty(Step) || Step == "any" ? 1 : decimal.Parse(Step.Replace(",", "."), System.Globalization.CultureInfo.InvariantCulture);

            var newValue = Math.Round((value - MinValue()) / step) * step + MinValue();

            if (Range)
            {
                var oldMinValue = ((IEnumerable)Value).OfType<object>().FirstOrDefault();
                var oldMaxValue = ((IEnumerable)Value).OfType<object>().LastOrDefault();

                var type = typeof(TValue).IsGenericType ? typeof(TValue).GetGenericArguments()[0] : typeof(TValue);
                var convertedNewValue = ConvertType.ChangeType(newValue, type);

                var newValueAsDecimal = (decimal)ConvertType.ChangeType(newValue, typeof(decimal));
                var oldMaxValueAsDecimal = (decimal)ConvertType.ChangeType(oldMaxValue, typeof(decimal));
                var oldMinValueAsDecimal = (decimal)ConvertType.ChangeType(oldMinValue, typeof(decimal));

                var values = Enumerable.Range(0, 2).Select(i =>
                {
                    if (i == 0)
                    {
                        return isMin &&
                            !object.Equals(oldMinValue, convertedNewValue) &&
                            newValueAsDecimal >= Min && newValueAsDecimal <= Max &&
                            newValueAsDecimal < oldMaxValueAsDecimal
                                ? convertedNewValue : oldMinValue;
                    }
                    else
                    {
                        return !isMin &&
                            !object.Equals(oldMaxValue, convertedNewValue) &&
                            newValueAsDecimal >= Min && newValueAsDecimal <= Max &&
                            newValueAsDecimal > oldMinValueAsDecimal
                                ? convertedNewValue : oldMaxValue;
                    }
                }).AsQueryable().Cast(type);

                if (!object.Equals(Value, values))
                {
                    Value = (TValue)values;

                    await ValueChanged.InvokeAsync(Value);

                    EditContext?.NotifyFieldChanged(FieldIdentifier);

                    await Change.InvokeAsync(Value);

                    StateHasChanged();
                }
            }
            else
            {
                var valueAsDecimal = Value == null ? 0 : (decimal)ConvertType.ChangeType(Value, typeof(decimal));

                if (!object.Equals(valueAsDecimal, newValue) && newValue >= Min && newValue <= Max)
                {
                    Value = (TValue)ConvertType.ChangeType(newValue, typeof(TValue));

                    await ValueChanged.InvokeAsync(Value);

                    EditContext?.NotifyFieldChanged(FieldIdentifier);

                    await Change.InvokeAsync(Value);

                    StateHasChanged();
                }
            }
        }

        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            return $"rz-slider {(Disabled ? "rz-state-disabled " : "")}{(Orientation == Orientation.Vertical ? "rz-slider-vertical" : "rz-slider-horizontal")}";
        }

        /// <summary>
        /// Specifies the orientation. Set to <c>Orientation.Horizontal</c> by default.
        /// </summary>
        [Parameter]
        public Orientation Orientation { get; set; } = Orientation.Horizontal;

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        /// <value>The value.</value>
        [Parameter]
        public override TValue Value
        {
            get
            {
                if (_value == null)
                {
                    if (Range)
                    {
                        var type = typeof(TValue).IsGenericType ? typeof(TValue).GetGenericArguments()[0] : typeof(TValue);

                        _value = (TValue)Enumerable.Range(0, 2).Select(i =>
                        {
                            if (i == 0)
                            {
                                return ConvertType.ChangeType(Min, type);
                            }
                            else
                            {
                                return ConvertType.ChangeType(Max, type);
                            }
                        }).AsQueryable().Cast(type);
                    }
                    else
                    {
                        _value = (TValue)ConvertType.ChangeType(Min, typeof(TValue));
                    }
                }

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

        decimal MinValue()
        {
            if (Range)
            {
                var values = Value as IEnumerable;
                if (values != null && values.OfType<object>().Any())
                {
                    var v = values.OfType<object>().FirstOrDefault();
                    return (decimal)Convert.ChangeType(v != null ? v : Min, typeof(decimal));
                }
            }

            return HasValue ? (decimal)Convert.ChangeType(Value, typeof(decimal)) : Min;
        }

        decimal MaxValue()
        {
            if (Range)
            {
                var values = Value as IEnumerable;
                if (values != null && values.OfType<object>().Any())
                {
                    var v = values.OfType<object>().LastOrDefault();
                    return (decimal)Convert.ChangeType(v != null ? v : Max, typeof(decimal));
                }
            }

            return HasValue ? (decimal)Convert.ChangeType(Value, typeof(decimal)) : Min;
        }

        /// <summary>
        /// Gets or sets the step.
        /// </summary>
        /// <value>The step.</value>
        [Parameter]
        public string Step { get; set; } = "1";

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="RadzenSlider{TValue}"/> is range.
        /// </summary>
        /// <value><c>true</c> if range; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool Range { get; set; } = false;

        /// <summary>
        /// Determines the minimum value.
        /// </summary>
        /// <value>The minimum value.</value>
        [Parameter]
        public decimal Min { get; set; } = 0;

        /// <summary>
        /// Determines the maximum value.
        /// </summary>
        /// <value>The maximum value.</value>
        [Parameter]
        public decimal Max { get; set; } = 100;

        bool preventKeyPress = false;
        async Task OnKeyPress(KeyboardEventArgs args, bool isMin)
        {
            var key = args.Code != null ? args.Code : args.Key;

            if (Orientation == Orientation.Horizontal ? key == "ArrowLeft" || key == "ArrowRight" : key == "ArrowUp" || key == "ArrowDown")
            {
                preventKeyPress = true;

                var step = string.IsNullOrEmpty(Step) || Step == "any" ? 1 : decimal.Parse(Step.Replace(",", "."), System.Globalization.CultureInfo.InvariantCulture);

                if (Range)
                {
                    var oldMinValue = ((IEnumerable)Value).OfType<object>().FirstOrDefault();
                    var oldMaxValue = ((IEnumerable)Value).OfType<object>().LastOrDefault();
                    var oldMinValueAsDecimal = (decimal)ConvertType.ChangeType(oldMinValue, typeof(decimal));
                    var oldMaxValueAsDecimal = (decimal)ConvertType.ChangeType(oldMaxValue, typeof(decimal));

                    await OnValueChange((isMin ? oldMinValueAsDecimal : oldMaxValueAsDecimal) + (key == "ArrowLeft" || key == "ArrowDown" ? -step : step), isMin);
                }
                else
                {
                    var valueAsDecimal = Value == null ? 0 : (decimal)ConvertType.ChangeType(Value, typeof(decimal));

                    await OnValueChange(valueAsDecimal + (key == "ArrowLeft" || key == "ArrowDown" ? -step : step), isMin);
                }
            }
            else
            {
                preventKeyPress = false;
            }
        }
    }
}
