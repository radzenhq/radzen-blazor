using Bunit;
using Microsoft.AspNetCore.Components;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Radzen.Blazor.Tests
{
    // Covers editing and validation behavior that depends on the row cascades.
    public class DataGridRowCascadeTests
    {
        public class Person
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        static List<Person> People(int n) =>
            Enumerable.Range(1, n).Select(i => new Person { Id = i, Name = "Person " + i }).ToList();

        static IRenderedComponent<RadzenDataGrid<Person>> RenderEditableGrid(TestContext ctx, List<Person> data,
            bool withValidator)
        {
            return ctx.RenderComponent<RadzenDataGrid<Person>>(p =>
            {
                p.Add(g => g.Data, data);
                p.Add<RenderFragment>(g => g.Columns, builder =>
                {
                    builder.OpenComponent<RadzenDataGridColumn<Person>>(0);
                    builder.AddAttribute(1, nameof(RadzenDataGridColumn<Person>.Property), nameof(Person.Name));
                    builder.AddAttribute(2, nameof(RadzenDataGridColumn<Person>.EditTemplate),
                        (RenderFragment<Person>)(person => eb =>
                        {
                            eb.OpenComponent<RadzenTextBox>(0);
                            eb.AddAttribute(1, nameof(RadzenTextBox.Name), "PersonName");
                            eb.AddAttribute(2, nameof(RadzenTextBox.Value), person.Name);
                            eb.AddAttribute(3, nameof(RadzenTextBox.ValueChanged),
                                EventCallback.Factory.Create<string>(person, v => person.Name = v));
                            eb.AddAttribute(4, "ValueExpression",
                                (System.Linq.Expressions.Expression<System.Func<string>>)(() => person.Name));
                            eb.CloseComponent();

                            if (withValidator)
                            {
                                eb.OpenComponent<RadzenRequiredValidator>(5);
                                eb.AddAttribute(6, nameof(RadzenRequiredValidator.Component), "PersonName");
                                eb.AddAttribute(7, nameof(RadzenRequiredValidator.Text), "Name is required");
                                eb.CloseComponent();
                            }
                        }));
                    builder.CloseComponent();
                });
            });
        }

        [Fact]
        public void EditRow_CascadesEditContextToEditors()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var data = People(3);
            var component = RenderEditableGrid(ctx, data, withValidator: false);

            component.InvokeAsync(() => component.Instance.EditRow(data[0])).GetAwaiter().GetResult();

            Assert.True(component.Instance.IsRowInEditMode(data[0]));

            // FormComponent gets its validation-state class from the cascaded EditContext.
            var input = component.FindAll("input.rz-textbox").Single();
            var css = input.GetAttribute("class");
            Assert.True(css.Contains("valid") || css.Contains("invalid"),
                $"editor should carry an EditContext validation-state class, got '{css}'");
        }

        [Fact]
        public void EditRow_CascadesFormSoValidatorsResolveTheirComponent()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var data = People(3);
            data[0].Name = null;   // required field left empty

            var component = RenderEditableGrid(ctx, data, withValidator: true);

            component.InvokeAsync(() => component.Instance.EditRow(data[0])).GetAwaiter().GetResult();
            component.InvokeAsync(() => component.Instance.UpdateRow(data[0])).GetAwaiter().GetResult();

            // The validator resolves its target through the cascaded row form.
            Assert.True(component.Instance.IsRowInEditMode(data[0]),
                "a failing required validator should have kept the row in edit mode");
        }

        [Fact]
        public void ReadOnlyRow_WithInputInPlainTemplate_StillRenders()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var data = People(3);

            var component = ctx.RenderComponent<RadzenDataGrid<Person>>(p =>
            {
                p.Add(g => g.Data, data);
                p.Add<RenderFragment>(g => g.Columns, builder =>
                {
                    builder.OpenComponent<RadzenDataGridColumn<Person>>(0);
                    builder.AddAttribute(1, nameof(RadzenDataGridColumn<Person>.Property), nameof(Person.Name));
                    builder.AddAttribute(2, nameof(RadzenDataGridColumn<Person>.Template),
                        (RenderFragment<Person>)(person => tb =>
                        {
                            tb.OpenComponent<RadzenCheckBox<bool>>(0);
                            tb.AddAttribute(1, nameof(RadzenCheckBox<bool>.Value), true);
                            tb.CloseComponent();
                            tb.AddContent(2, person.Name);
                        }));
                    builder.CloseComponent();
                });
            });

            Assert.False(component.Instance.IsRowInEditMode(data[0]));
            Assert.Contains("Person 1", component.Markup);
            Assert.Contains("Person 3", component.Markup);
            Assert.Equal(3, component.FindAll("tbody.rz-datatable-data > tr, tbody > tr.rz-data-row").Count);
        }
    }
}
