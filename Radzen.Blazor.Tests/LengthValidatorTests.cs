using System;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components.Forms;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class LengthValidatorTests
    {
        class FormComponentTestDouble : IRadzenFormComponent
        {
            public bool IsBound => false;

            public bool HasValue => true;

            public string Name { get; set; }

            public FieldIdentifier FieldIdentifier { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

            public object GetValue()
            {
                return Value;
            }

            public ValueTask FocusAsync()
            {
                throw new NotImplementedException();
            }

            public bool Disabled { get; set; }
            public bool Visible { get; set; }
            public IFormFieldContext FormFieldContext => null;

            public object Value { get; set; }
        }

        class RadzenLengthValidatorTestDouble : RadzenLengthValidator
        {
            public bool Validate(object value)
            {
                return base.Validate(new FormComponentTestDouble { Value = value });
            }
        }

        [Fact]
        public void Returns_True_If_Value_Is_Empty_And_Min_Is_Zero()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenLengthValidatorTestDouble>();
            component.SetParametersAndRender(parameters => parameters.Add(p => p.Min, 0));

            Assert.True(component.Instance.Validate(""));
        }

        [Fact]
        public void Returns_False_If_Value_Is_Null_And_Min_Is_Set()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenLengthValidatorTestDouble>();
            component.SetParametersAndRender(parameters => parameters.Add(p => p.Min, 0));

            Assert.False(component.Instance.Validate(null));
        }

        [Fact]
        public void Returns_False_If_Value_Is_Empty_And_Min_Is_Greater_Than_Zero()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenLengthValidatorTestDouble>();
            component.SetParametersAndRender(parameters => parameters.Add(p => p.Min, 3));

            Assert.False(component.Instance.Validate(""));
        }

        [Fact]
        public void Returns_True_If_Value_Length_Equals_Min()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenLengthValidatorTestDouble>();
            component.SetParametersAndRender(parameters => parameters.Add(p => p.Min, 3));

            Assert.True(component.Instance.Validate("abc"));
        }

        [Fact]
        public void Returns_True_If_Value_Length_Greater_Than_Min()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenLengthValidatorTestDouble>();
            component.SetParametersAndRender(parameters => parameters.Add(p => p.Min, 3));

            Assert.True(component.Instance.Validate("abcd"));
        }

        [Fact]
        public void Returns_False_If_Value_Length_Less_Than_Min()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenLengthValidatorTestDouble>();
            component.SetParametersAndRender(parameters => parameters.Add(p => p.Min, 3));

            Assert.False(component.Instance.Validate("ab"));
        }

        [Fact]
        public void Returns_True_If_Value_Length_Equals_Max()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenLengthValidatorTestDouble>();
            component.SetParametersAndRender(parameters => parameters.Add(p => p.Max, 5));

            Assert.True(component.Instance.Validate("abcde"));
        }

        [Fact]
        public void Returns_True_If_Value_Length_Less_Than_Max()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenLengthValidatorTestDouble>();
            component.SetParametersAndRender(parameters => parameters.Add(p => p.Max, 5));

            Assert.True(component.Instance.Validate("abc"));
        }

        [Fact]
        public void Returns_False_If_Value_Length_Greater_Than_Max()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenLengthValidatorTestDouble>();
            component.SetParametersAndRender(parameters => parameters.Add(p => p.Max, 5));

            Assert.False(component.Instance.Validate("abcdef"));
        }

        [Fact]
        public void Returns_True_If_Value_Length_Between_Min_And_Max()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenLengthValidatorTestDouble>();
            component.SetParametersAndRender(parameters => parameters.Add(p => p.Min, 2).Add(p => p.Max, 5));

            Assert.True(component.Instance.Validate("abc"));
        }

        [Fact]
        public void Returns_True_If_Only_Max_Set_And_Value_Is_Null()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenLengthValidatorTestDouble>();
            component.SetParametersAndRender(parameters => parameters.Add(p => p.Max, 5));

            Assert.True(component.Instance.Validate(null));
        }

        [Fact]
        public void Returns_True_If_Only_Max_Set_And_Value_Is_Empty()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenLengthValidatorTestDouble>();
            component.SetParametersAndRender(parameters => parameters.Add(p => p.Max, 5));

            Assert.True(component.Instance.Validate(""));
        }
    }
}
