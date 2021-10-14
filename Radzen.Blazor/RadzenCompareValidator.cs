using System;
using System.Globalization;
using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    /// <summary>
    /// Enum CompareOperator
    /// </summary>
    public enum CompareOperator
    {
        /// <summary>
        /// The equal
        /// </summary>
        Equal,
        /// <summary>
        /// The greater than
        /// </summary>
        GreaterThan,
        /// <summary>
        /// The greater than equal
        /// </summary>
        GreaterThanEqual,
        /// <summary>
        /// The less than
        /// </summary>
        LessThan,
        /// <summary>
        /// The less than equal
        /// </summary>
        LessThanEqual,
        /// <summary>
        /// The not equal
        /// </summary>
        NotEqual,
    }

    /// <summary>
    /// RadzenCompareValidator component.
    /// Implements the <see cref="Radzen.Blazor.ValidatorBase" />
    /// </summary>
    /// <seealso cref="Radzen.Blazor.ValidatorBase" />
    public class RadzenCompareValidator : ValidatorBase
    {
        /// <summary>
        /// Gets or sets the text.
        /// </summary>
        /// <value>The text.</value>
        [Parameter]
        public override string Text { get; set; } = "Value should match";

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        /// <value>The value.</value>
        [Parameter]
        public object Value { get; set; }

        /// <summary>
        /// Gets or sets the operator.
        /// </summary>
        /// <value>The operator.</value>
        [Parameter]
        public CompareOperator Operator { get; set; } = CompareOperator.Equal;

        /// <summary>
        /// Compares the specified component value.
        /// </summary>
        /// <param name="componentValue">The component value.</param>
        /// <returns>System.Int32.</returns>
        private int Compare(object componentValue)
        {
            switch (componentValue)
            {
                case String stringValue:
                    return String.Compare(stringValue, (string)Value, false, Culture);
                case IComparable comparable:
                    return comparable.CompareTo(Value);
                default:
                    return 0;
            }
        }
        /// <summary>
        /// Validates the specified component.
        /// </summary>
        /// <param name="component">The component.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        protected override bool Validate(IRadzenFormComponent component)
        {
            var compareResult = Compare(component.GetValue());

            switch (Operator)
            {
                case CompareOperator.Equal:
                    return compareResult == 0;
                case CompareOperator.NotEqual:
                    return compareResult != 0;
                case CompareOperator.GreaterThan:
                    return compareResult > 0;
                case CompareOperator.GreaterThanEqual:
                    return compareResult >= 0;
                case CompareOperator.LessThan:
                    return compareResult < 0;
                case CompareOperator.LessThanEqual:
                    return compareResult <= 0;
                default:
                    return true;
            }
        }
    }
}