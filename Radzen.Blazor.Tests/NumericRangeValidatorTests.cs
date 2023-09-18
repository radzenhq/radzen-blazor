using System;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components.Forms;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class NumericRangeValidatorTests
    {
        class FormComponentTestDouble : IRadzenFormComponent
        {
            public bool IsBound => false;

            public bool HasValue => true;

            public string Name { get; set; }

            public FieldIdentifier FieldIdentifier => throw new System.NotImplementedException();

            public object GetValue()
            {
                return Value;
            }

            public ValueTask FocusAsync()
            {
                throw new NotImplementedException();
            }

            public object Value { get; set; }
        }

        class RadzenNumericRangeValidatorTestDouble : RadzenNumericRangeValidator
        {
            public bool Validate(object value)
            {
                return base.Validate(new FormComponentTestDouble { Value = value });
            }
        }

        [Fact]
        public void Throws_Exception_If_Min_And_Max_Are_Null()
        {
            var validator = new RadzenNumericRangeValidatorTestDouble();

            Assert.Throws<System.ArgumentException>(() => validator.Validate(1));
        }

        [Fact]
        public void Returns_False_If_Value_Is_Null()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenNumericRangeValidatorTestDouble>();

            component.SetParametersAndRender(parameters =>
            {
                component.SetParametersAndRender(parameters => parameters.Add(p => p.Min, 0).Add(p => p.Max, 10));
            });

            Assert.False(component.Instance.Validate(null));
        }

        [Fact]
        public void Returns_False_If_Value_Overflows()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenNumericRangeValidatorTestDouble>();

            component.SetParametersAndRender(parameters =>
            {
                component.SetParametersAndRender(parameters => parameters.Add(p => p.Min, 0).Add(p => p.Max, 10));
            });

            Assert.False(component.Instance.Validate(long.MaxValue));
        }

        [Fact]
        public void Returns_True_If_Value_Is_Greater_Than_Min()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenNumericRangeValidatorTestDouble>();
            component.SetParametersAndRender(parameters => parameters.Add(p => p.Min, 0));

            Assert.True(component.Instance.Validate(1));
        }

        [Fact]
        public void Returns_True_If_Value_Is_Equal_To_Min()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenNumericRangeValidatorTestDouble>();
            component.SetParametersAndRender(parameters => parameters.Add(p => p.Min, 0));

            Assert.True(component.Instance.Validate(0));
        }

        [Fact]
        public void Returns_True_If_Value_Is_Less_Than_Max()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenNumericRangeValidatorTestDouble>();
            component.SetParametersAndRender(parameters => parameters.Add(p => p.Max, 10));

            Assert.True(component.Instance.Validate(9));
        }

        [Fact]
        public void Returns_True_If_Value_Is_Equal_To_Max()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenNumericRangeValidatorTestDouble>();
            component.SetParametersAndRender(parameters => parameters.Add(p => p.Max, 10));

            Assert.True(component.Instance.Validate(10));
        }

        [Fact]
        public void Returns_True_If_Value_Is_Between_Min_And_Max()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenNumericRangeValidatorTestDouble>();
            component.SetParametersAndRender(parameters => parameters.Add(p => p.Min, 0).Add(p => p.Max, 10));

            Assert.True(component.Instance.Validate(5));
        }

        [Fact]
        public void Returns_True_If_Value_Is_Between_Min_And_Max_And_They_Are_Nullable()
        {
            int? min = 0;
            int? max = 10;
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenNumericRangeValidatorTestDouble>();
            component.SetParametersAndRender(parameters => parameters.Add(p => p.Min, min).Add(p => p.Max, max));
            Assert.True(component.Instance.Validate(5));
        }

        [Fact]
        public void Returns_True_When_Value_Is_Of_DifferentType()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenNumericRangeValidatorTestDouble>();
            component.SetParametersAndRender(parameters => parameters.Add(p => p.Min, 0m).Add(p => p.Max, 10m));

            Assert.True(component.Instance.Validate(5));
        }

        [Fact]
        public void Returns_False_If_Cannot_Conert_Value()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenNumericRangeValidatorTestDouble>();
            component.SetParametersAndRender(parameters => parameters.Add(p => p.Min, 0m).Add(p => p.Max, 10m));

            Assert.False(component.Instance.Validate(DateTime.Now));
        }
    }
}