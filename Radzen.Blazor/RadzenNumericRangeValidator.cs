using System;
using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    /// <summary>
    /// A validator component that ensures a numeric value falls within a specified minimum and maximum range.
    /// RadzenNumericRangeValidator is ideal for quantity limits, age restrictions, price ranges, or any bounded numeric input.
    /// Must be placed inside a <see cref="RadzenTemplateForm{TItem}"/>.
    /// Ensures values are within acceptable bounds by checking that the value is greater than or equal to Min and less than or equal to Max. Both bounds are inclusive.
    /// You can specify just Min to validate minimum value (e.g., age must be at least 18), just Max to validate maximum value (e.g., quantity cannot exceed 100), or both to validate range (e.g., rating must be between 1 and 5).
    /// Works with any IComparable type (int, decimal, double, DateTime, etc.). Set AllowNull = true to accept null values as valid (for optional nullable fields).
    /// </summary>
    /// <example>
    /// Age minimum validation:
    /// <code>
    /// &lt;RadzenTemplateForm TItem="Model" Data=@model&gt;
    ///     &lt;RadzenNumeric Name="Age" @bind-Value=@model.Age /&gt;
    ///     &lt;RadzenNumericRangeValidator Component="Age" Min="18" Text="Must be 18 or older" Style="position: absolute" /&gt;
    /// &lt;/RadzenTemplateForm&gt;
    /// </code>
    /// Quantity range validation:
    /// <code>
    /// &lt;RadzenTemplateForm TItem="Model" Data=@model&gt;
    ///     &lt;RadzenNumeric Name="Quantity" @bind-Value=@model.Quantity /&gt;
    ///     &lt;RadzenNumericRangeValidator Component="Quantity" Min="1" Max="100" 
    ///                                   Text="Quantity must be between 1 and 100" Style="position: absolute" /&gt;
    /// &lt;/RadzenTemplateForm&gt;
    /// </code>
    /// </example>
    public class RadzenNumericRangeValidator : ValidatorBase
    {
        /// <summary>
        /// Gets or sets the error message displayed when the value is outside the valid range.
        /// Customize to provide specific guidance (e.g., "Age must be between 18 and 65").
        /// </summary>
        /// <value>The validation error message. Default is "Not in the valid range".</value>
        [Parameter]
        public override string Text { get; set; } = "Not in the valid range";

        /// <summary>
        /// Gets or sets the minimum allowed value (inclusive).
        /// The component value must be greater than or equal to this value. Can be null to only validate maximum.
        /// Works with any IComparable type (int, decimal, DateTime, etc.).
        /// </summary>
        /// <value>The minimum value, or null for no minimum constraint.</value>
        [Parameter]
        public IComparable Min { get; set; }

        /// <summary>
        /// Gets or sets the maximum allowed value (inclusive).
        /// The component value must be less than or equal to this value. Can be null to only validate minimum.
        /// Works with any IComparable type (int, decimal, DateTime, etc.).
        /// </summary>
        /// <value>The maximum value, or null for no maximum constraint.</value>
        [Parameter]
        public IComparable Max { get; set; }

        /// <summary>
        /// Gets or sets whether null values should be considered valid.
        /// When true, null values pass validation (useful for optional nullable fields).
        /// When false (default), null values fail validation.
        /// </summary>
        /// <value><c>true</c> to allow null values; <c>false</c> to require a value. Default is <c>false</c>.</value>
        [Parameter]
        public bool AllowNull { get; set; } = false;

        /// <inheritdoc />
        protected override bool Validate(IRadzenFormComponent component)
        {
            if (Min == null && Max == null)
            {
                throw new ArgumentException("Min and Max cannot be both null");
            }

            object value = component.GetValue();

            if (value == null)
            {
                return AllowNull;
            }


            if (Min != null)
            {
                if (!TryConvertToType(value, Min.GetType(), out var convertedValue) || Min.CompareTo(convertedValue) > 0)
                {
                    return false;
                }
            }

            if (Max != null)
            {
                if (!TryConvertToType(value, Max.GetType(), out var convertedValue) || Max.CompareTo(convertedValue) < 0)
                {
                    return false;
                }
            }

            return true;
        }

        private bool TryConvertToType(object value, Type type, out object convertedValue)
        {
            try
            {
                convertedValue = Convert.ChangeType(value, type);
                return true;
            }
            catch (Exception ex) when (ex is InvalidCastException || ex is OverflowException)
            {
                convertedValue = null;
                return false;
            }
        }
    }
}
