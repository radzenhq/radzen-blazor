using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class AriaInvalidTests
    {
        class Model
        {
            public string Text { get; set; }
            public bool Flag { get; set; }
            public int Number { get; set; }
            public DateTime Date { get; set; }
            public TimeSpan Time { get; set; }
            public IEnumerable<string> Items { get; set; }
        }

        static readonly Model model = new Model();

        static void AssertFollowsValidation<TComponent>(string fieldName, string selector, Action<ComponentParameterCollectionBuilder<TComponent>> configure)
            where TComponent : IComponent
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.Services.AddScoped<DialogService>();
            ctx.Services.AddScoped<ContextMenuService>();

            var editContext = new EditContext(model);
            var messages = new ValidationMessageStore(editContext);

            var component = ctx.RenderComponent<TComponent>(parameters =>
            {
                parameters.AddCascadingValue(editContext);
                configure(parameters);
            });

            Assert.NotEmpty(component.FindAll(selector));
            Assert.All(component.FindAll(selector), element => Assert.False(element.HasAttribute("aria-invalid")));

            component.InvokeAsync(() =>
            {
                messages.Add(new FieldIdentifier(model, fieldName), "Invalid");
                editContext.NotifyValidationStateChanged();
            });

            Assert.NotEmpty(component.FindAll(selector));
            Assert.All(component.FindAll(selector), element => Assert.Equal("true", element.GetAttribute("aria-invalid")));

            component.InvokeAsync(() =>
            {
                messages.Clear();
                editContext.NotifyValidationStateChanged();
            });

            Assert.All(component.FindAll(selector), element => Assert.False(element.HasAttribute("aria-invalid")));
        }

        [Fact]
        public void TextBox_AriaInvalid_Follows_Validation() =>
            AssertFollowsValidation<RadzenTextBox>(nameof(Model.Text), "input", p => p.Add(c => c.ValueExpression, () => model.Text));

        [Fact]
        public void TextArea_AriaInvalid_Follows_Validation() =>
            AssertFollowsValidation<RadzenTextArea>(nameof(Model.Text), "textarea", p => p.Add(c => c.ValueExpression, () => model.Text));

        [Fact]
        public void Password_AriaInvalid_Follows_Validation() =>
            AssertFollowsValidation<RadzenPassword>(nameof(Model.Text), "input", p => p.Add(c => c.ValueExpression, () => model.Text));

        [Fact]
        public void Mask_AriaInvalid_Follows_Validation() =>
            AssertFollowsValidation<RadzenMask>(nameof(Model.Text), "input", p => p.Add(c => c.ValueExpression, () => model.Text));

        [Fact]
        public void AutoComplete_AriaInvalid_Follows_Validation() =>
            AssertFollowsValidation<RadzenAutoComplete>(nameof(Model.Text), "input[role=combobox]", p => p.Add(c => c.ValueExpression, () => model.Text));

        [Fact]
        public void SecurityCode_AriaInvalid_Follows_Validation() =>
            AssertFollowsValidation<RadzenSecurityCode>(nameof(Model.Text), "input.rz-security-code-input", p => p.Add(c => c.ValueExpression, () => model.Text));

        [Fact]
        public void MarkdownEditor_AriaInvalid_Follows_Validation() =>
            AssertFollowsValidation<RadzenMarkdownEditor>(nameof(Model.Text), "textarea", p => p.Add(c => c.ValueExpression, () => model.Text));

        [Fact]
        public void DropDown_AriaInvalid_Follows_Validation() =>
            AssertFollowsValidation<RadzenDropDown<string>>(nameof(Model.Text), "div.rz-dropdown[role=combobox]", p => p.Add(c => c.ValueExpression, () => model.Text));

        [Fact]
        public void DropDownDataGrid_AriaInvalid_Follows_Validation() =>
            AssertFollowsValidation<RadzenDropDownDataGrid<string>>(nameof(Model.Text), "div.rz-dropdown[role=combobox]", p => p.Add(c => c.ValueExpression, () => model.Text));

        [Fact]
        public void ListBox_AriaInvalid_Follows_Validation() =>
            AssertFollowsValidation<RadzenListBox<string>>(nameof(Model.Text), "ul[role=listbox]", p => p.Add(c => c.Data, new[] { "a", "b" }).Add(c => c.ValueExpression, () => model.Text));

        [Fact]
        public void RadioButtonList_AriaInvalid_Follows_Validation() =>
            AssertFollowsValidation<RadzenRadioButtonList<string>>(nameof(Model.Text), "[role=radiogroup]", p => p.Add(c => c.ValueExpression, () => model.Text));

        [Fact]
        public void SelectBar_AriaInvalid_Follows_Validation() =>
            AssertFollowsValidation<RadzenSelectBar<string>>(nameof(Model.Text), "[role=radiogroup]", p => p.Add(c => c.ValueExpression, () => model.Text));

        [Fact]
        public void ChipList_AriaInvalid_Follows_Validation() =>
            AssertFollowsValidation<RadzenChipList<string>>(nameof(Model.Text), "[role=listbox]", p => p.Add(c => c.ValueExpression, () => model.Text));

        [Fact]
        public void CheckBox_AriaInvalid_Follows_Validation() =>
            AssertFollowsValidation<RadzenCheckBox<bool>>(nameof(Model.Flag), "[role=checkbox]", p => p.Add(c => c.ValueExpression, () => model.Flag));

        [Fact]
        public void Switch_AriaInvalid_Follows_Validation() =>
            AssertFollowsValidation<RadzenSwitch>(nameof(Model.Flag), "[role=switch]", p => p.Add(c => c.ValueExpression, () => model.Flag));

        [Fact]
        public void Rating_AriaInvalid_Follows_Validation() =>
            AssertFollowsValidation<RadzenRating>(nameof(Model.Number), "[role=radiogroup]", p => p.Add(c => c.ValueExpression, () => model.Number));

        [Fact]
        public void Slider_AriaInvalid_Follows_Validation() =>
            AssertFollowsValidation<RadzenSlider<int>>(nameof(Model.Number), "[role=slider]", p => p.Add(c => c.ValueExpression, () => model.Number));

        [Fact]
        public void DatePicker_AriaInvalid_Follows_Validation() =>
            AssertFollowsValidation<RadzenDatePicker<DateTime>>(nameof(Model.Date), "input.rz-inputtext", p => p.Add(c => c.ValueExpression, () => model.Date));

        [Fact]
        public void TimeSpanPicker_AriaInvalid_Follows_Validation() =>
            AssertFollowsValidation<RadzenTimeSpanPicker<TimeSpan>>(nameof(Model.Time), "input[name=time]", p => p.Add(c => c.Name, "time").Add(c => c.ValueExpression, () => model.Time));

        [Fact]
        public void SelectBar_Multiple_Has_No_AriaInvalid()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var editContext = new EditContext(model);
            var messages = new ValidationMessageStore(editContext);
            messages.Add(new FieldIdentifier(model, nameof(Model.Items)), "Invalid");

            var component = ctx.RenderComponent<RadzenSelectBar<IEnumerable<string>>>(parameters => parameters
                .AddCascadingValue(editContext)
                .Add(c => c.Multiple, true)
                .Add(c => c.ValueExpression, () => model.Items));

            Assert.Contains("invalid", component.Find("[role=toolbar]").ClassName);
            Assert.False(component.Find("[role=toolbar]").HasAttribute("aria-invalid"));
        }

        [Fact]
        public void TextBox_Keeps_Consumer_AriaInvalid()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenTextBox>(parameters => parameters
                .AddUnmatched("aria-invalid", "true"));

            Assert.Equal("true", component.Find("input").GetAttribute("aria-invalid"));
        }

        [Fact]
        public void DatePicker_Keeps_Consumer_AriaInvalid_From_InputAttributes()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenDatePicker<DateTime>>(parameters => parameters
                .Add(c => c.InputAttributes, new Dictionary<string, object> { ["aria-invalid"] = "true" }));

            Assert.Equal("true", component.Find("input.rz-inputtext").GetAttribute("aria-invalid"));
        }
    }
}
