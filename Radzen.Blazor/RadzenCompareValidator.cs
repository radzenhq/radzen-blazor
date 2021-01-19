using System;
using System.Globalization;
using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    public enum CompareOperator
    {
        Equal,
        GreaterThan,
        GreaterThanEqual,
        LessThan,
        LessThanEqual,
        NotEqual,
    }

    public class RadzenCompareValidator : ValidatorBase
    {
        [Parameter]
        public override string Text { get; set; } = "Value should match";

        [Parameter]
        public object Value { get; set; }

        [Parameter]
        public CompareOperator Operator { get; set; } = CompareOperator.Equal;

        private int Compare(object componentValue)
        {
            switch (componentValue)
            {
                case String stringValue:
                    return String.Compare(stringValue, (string)Value, false, CultureInfo.CurrentCulture);
                case IComparable comparable:
                    return comparable.CompareTo(Value);
                default:
                    return 0;
            }
        }
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