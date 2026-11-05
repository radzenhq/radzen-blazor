using System.Collections.Generic;
using System.Linq;
using AngleSharp.Dom;
using Bunit;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class DataGridFrozenOffsetTests
    {
        public class Person
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string City { get; set; }
            public string Country { get; set; }
        }

        static List<Person> People() => new()
        {
            new Person { Id = 1, Name = "Alice", City = "Paris", Country = "France" },
        };

        static TestContext NewCtx()
        {
            var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");
            return ctx;
        }

        static IRenderedComponent<RadzenDataGrid<Person>> Render(TestContext ctx, params (string property, bool frozen, FrozenColumnPosition position, string width)[] columns)
        {
            return ctx.RenderComponent<RadzenDataGrid<Person>>(p =>
            {
                p.Add(g => g.Data, People());
                p.Add(g => g.Columns, b =>
                {
                    var seq = 0;
                    foreach (var column in columns)
                    {
                        b.OpenComponent(seq++, typeof(RadzenDataGridColumn<Person>));
                        b.AddAttribute(seq++, nameof(RadzenDataGridColumn<Person>.Property), column.property);
                        b.AddAttribute(seq++, nameof(RadzenDataGridColumn<Person>.Frozen), column.frozen);
                        b.AddAttribute(seq++, nameof(RadzenDataGridColumn<Person>.FrozenPosition), column.position);
                        if (column.width != null)
                        {
                            b.AddAttribute(seq++, nameof(RadzenDataGridColumn<Person>.Width), column.width);
                        }
                        b.CloseComponent();
                    }
                });
            });
        }

        static string HeaderStyle(IRenderedComponent<RadzenDataGrid<Person>> component, int index)
        {
            return component.FindAll(".rz-grid-table > thead > tr > th")[index].GetAttribute("style");
        }

        static string CellStyle(IRenderedComponent<RadzenDataGrid<Person>> component, int index)
        {
            return component.FindAll(".rz-grid-table > tbody > tr > td")[index].GetAttribute("style");
        }

        [Fact]
        public void LeftFrozenOffset_IsCustomPropertyWithDeclaredWidthsAsFallback()
        {
            using var ctx = NewCtx();
            var component = Render(ctx,
                ("Id", true, FrozenColumnPosition.Left, "80px"),
                ("Name", true, FrozenColumnPosition.Left, null),
                ("City", true, FrozenColumnPosition.Left, null),
                ("Country", false, FrozenColumnPosition.Left, null));

            Assert.Contains("inset-inline-start:var(--rz-grid-frozen-left-0,0)", HeaderStyle(component, 0));
            Assert.Contains("inset-inline-start:var(--rz-grid-frozen-left-1,80px)", HeaderStyle(component, 1));
            Assert.Contains("inset-inline-start:var(--rz-grid-frozen-left-2,calc(80px + 200px))", HeaderStyle(component, 2));
            Assert.DoesNotContain("inset-inline", HeaderStyle(component, 3) ?? string.Empty);

            Assert.Contains("inset-inline-start:var(--rz-grid-frozen-left-0,0)", CellStyle(component, 0));
            Assert.Contains("inset-inline-start:var(--rz-grid-frozen-left-1,80px)", CellStyle(component, 1));
            Assert.Contains("inset-inline-start:var(--rz-grid-frozen-left-2,calc(80px + 200px))", CellStyle(component, 2));
        }

        [Fact]
        public void RightFrozenOffset_IsCustomPropertyIndexedFromTheLeft()
        {
            using var ctx = NewCtx();
            var component = Render(ctx,
                ("Id", false, FrozenColumnPosition.Left, null),
                ("Name", false, FrozenColumnPosition.Left, null),
                ("City", true, FrozenColumnPosition.Right, null),
                ("Country", true, FrozenColumnPosition.Right, "120px"));

            Assert.Contains("inset-inline-end:var(--rz-grid-frozen-right-0,120px)", HeaderStyle(component, 2));
            Assert.Contains("inset-inline-end:var(--rz-grid-frozen-right-1,0)", HeaderStyle(component, 3));
            Assert.Contains("inset-inline-end:var(--rz-grid-frozen-right-0,120px)", CellStyle(component, 2));
            Assert.Contains("inset-inline-end:var(--rz-grid-frozen-right-1,0)", CellStyle(component, 3));
        }

        [Fact]
        public void FrozenOffsetIndex_SkipsHiddenFrozenColumns()
        {
            using var ctx = NewCtx();
            var component = ctx.RenderComponent<RadzenDataGrid<Person>>(p =>
            {
                p.Add(g => g.Data, People());
                p.Add(g => g.Columns, b =>
                {
                    b.OpenComponent(0, typeof(RadzenDataGridColumn<Person>));
                    b.AddAttribute(1, nameof(RadzenDataGridColumn<Person>.Property), "Id");
                    b.AddAttribute(2, nameof(RadzenDataGridColumn<Person>.Frozen), true);
                    b.AddAttribute(3, nameof(RadzenDataGridColumn<Person>.Width), "80px");
                    b.AddAttribute(4, nameof(RadzenDataGridColumn<Person>.Visible), false);
                    b.CloseComponent();

                    b.OpenComponent(10, typeof(RadzenDataGridColumn<Person>));
                    b.AddAttribute(11, nameof(RadzenDataGridColumn<Person>.Property), "Name");
                    b.AddAttribute(12, nameof(RadzenDataGridColumn<Person>.Frozen), true);
                    b.CloseComponent();

                    b.OpenComponent(20, typeof(RadzenDataGridColumn<Person>));
                    b.AddAttribute(21, nameof(RadzenDataGridColumn<Person>.Property), "City");
                    b.AddAttribute(22, nameof(RadzenDataGridColumn<Person>.Frozen), true);
                    b.CloseComponent();
                });
            });

            Assert.Contains("inset-inline-start:var(--rz-grid-frozen-left-0,0)", HeaderStyle(component, 0));
            Assert.Contains("inset-inline-start:var(--rz-grid-frozen-left-1,200px)", HeaderStyle(component, 1));
        }
    }
}
