using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace Radzen.Blazor
{
    /// <summary>
    /// Defines the type of comparison operation to perform when validating values.
    /// Used by <see cref="RadzenCompareValidator"/> to determine how to compare the component value against the target value.
    /// </summary>
    public enum CompareOperator
    {
        /// <summary>
        /// Validates that values are equal. Useful for password confirmation fields.
        /// </summary>
        Equal,
        /// <summary>
        /// Validates that the component value is greater than the comparison value. Useful for numeric range validation.
        /// </summary>
        GreaterThan,
        /// <summary>
        /// Validates that the component value is greater than or equal to the comparison value.
        /// </summary>
        GreaterThanEqual,
        /// <summary>
        /// Validates that the component value is less than the comparison value.
        /// </summary>
        LessThan,
        /// <summary>
        /// Validates that the component value is less than or equal to the comparison value.
        /// </summary>
        LessThanEqual,
        /// <summary>
        /// Validates that values are not equal. Useful for ensuring a value differs from a specific value.
        /// </summary>
        NotEqual,
    }

    /// <summary>
    /// A validator component that compares a form input's value against another value or another component's value using a specified comparison operator.
    /// RadzenCompareValidator is commonly used for password confirmation, numeric range validation, or ensuring field values match expected criteria.
    /// Must be placed inside a <see cref="RadzenTemplateForm{TItem}"/> and associated with a named input component.
    /// Supports various comparison operations (Equal, NotEqual, GreaterThan, LessThan, etc.) via the Operator property.
    /// For password confirmation scenarios, set the Value property to the password field and the Component to the confirmation field name.
    /// Can react to changes in the comparison value by setting ValidateOnComponentValueChange to true (default).
    /// </summary>
    /// <example>
    /// <code>
    /// &lt;RadzenTemplateForm TItem="Model" Data=@model&gt;
    ///    &lt;RadzenPassword style="display: block" Name="Password" @bind-Value=@model.Password /&gt;
    ///    &lt;RadzenPassword style="display: block" Name="RepeatPassword" @bind-Value=@model.RepeatPassword /&gt;
    ///    &lt;RadzenCompareValidator Value=@model.Password Component="RepeatPassword" Text="Passwords should be the same"  Style="position: absolute" /&gt;
    /// &lt;/RadzenTemplateForm&gt;
    /// @code {
    ///    class Model
    ///    {
    ///      public string Password { get; set; }
    ///      public double Value { get; set; }
    ///      public string RepeatPassword { get; set; }
    ///    }
    ///    Model model = new Model();
    /// }
    /// </code>
    /// </example>
    public class RadzenCompareValidator : ValidatorBase
    {
        /// <summary>
        /// Gets or sets the message displayed when the component is invalid. Set to <c>"Value should match"</c> by default.
        /// </summary>
        [Parameter]
        public override string Text { get; set; } = "Value should match";

        /// <summary>
        /// Specifies the value to compare with.
        /// </summary>
        [Parameter]
        public object Value { get; set; }

        /// <summary>
        /// Specifies the comparison operator. Set to <c>CompareOperator.Equal</c> by default.
        /// </summary>
        [Parameter]
        public CompareOperator Operator { get; set; } = CompareOperator.Equal;

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="RadzenCompareValidator"/> should be validated on value change of the specified Component.
        /// </summary>
        /// <value><c>true</c> if should be validated; otherwise, <c>false</c>.</value>
        [Parameter]
        public virtual bool ValidateOnComponentValueChange { get; set; } = true;

        private int Compare(object componentValue) => componentValue switch
        {
            string stringValue => string.Compare(stringValue, (string)Value, false, Culture),
            IComparable comparable => comparable.CompareTo(Value),
            _ => 0,
        };

        /// <inheritdoc />
        public override async Task SetParametersAsync(ParameterView parameters)
        {
            var valueChanged = parameters.DidParameterChange(nameof(Value), Value);
            var visibleChanged = parameters.DidParameterChange(nameof(Visible), Visible);

            await base.SetParametersAsync(parameters);

            if (ValidateOnComponentValueChange && (valueChanged || visibleChanged) && !firstRender && Visible)
            {
                var component = Form.FindComponent(Component);
                if (component != null && component.FieldIdentifier.FieldName != null)
                {
                    IsValid = Validate(component);

                    messages?.Clear(component.FieldIdentifier);

                    if (!IsValid)
                    {
                        messages?.Add(component.FieldIdentifier, Text);
                    }

                    EditContext?.NotifyValidationStateChanged();
                }
            }
        }

        bool firstRender = true;
        /// <inheritdoc />
        protected override void OnAfterRender(bool firstRender)
        {
            this.firstRender = firstRender;
            base.OnAfterRender(firstRender);
        }

        /// <inheritdoc />
        protected override bool Validate(IRadzenFormComponent component)
        {
            var compareResult = Compare(component.GetValue());

            return Operator switch
            {
                CompareOperator.Equal => compareResult == 0,
                CompareOperator.NotEqual => compareResult != 0,
                CompareOperator.GreaterThan => compareResult > 0,
                CompareOperator.GreaterThanEqual => compareResult >= 0,
                CompareOperator.LessThan => compareResult < 0,
                CompareOperator.LessThanEqual => compareResult <= 0,
                _ => true,
            };

        }
    }
}