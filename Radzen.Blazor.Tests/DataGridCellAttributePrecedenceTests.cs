using Bunit;
using Microsoft.AspNetCore.Components;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Radzen.Blazor.Tests
{
    // Preserve CellRender class/style precedence while allocating cell attributes lazily.
    public class DataGridCellAttributePrecedenceTests
    {
        public class Person
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        static List<Person> People(int n) =>
            Enumerable.Range(1, n).Select(i => new Person { Id = i, Name = "Person " + i }).ToList();

        static IRenderedComponent<RadzenDataGrid<Person>> Render(TestContext ctx,
            System.Action<DataGridCellRenderEventArgs<Person>> cellRender, TextAlign align = TextAlign.Right)
        {
            return ctx.RenderComponent<RadzenDataGrid<Person>>(p =>
            {
                p.Add(g => g.Data, People(2));

                if (cellRender != null)
                {
                    p.Add(g => g.CellRender, cellRender);
                }

                p.Add<RenderFragment>(g => g.Columns, builder =>
                {
                    builder.OpenComponent<RadzenDataGridColumn<Person>>(0);
                    builder.AddAttribute(1, nameof(RadzenDataGridColumn<Person>.Property), nameof(Person.Name));
                    builder.AddAttribute(2, nameof(RadzenDataGridColumn<Person>.TextAlign), align);
                    builder.CloseComponent();
                });
            });
        }

        [Fact]
        public void ColumnStyle_WinsOverCellRenderStyle()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = Render(ctx, args => args.Attributes["style"] = "text-align:left");

            var style = component.FindAll("td").First().GetAttribute("style");
            Assert.Contains("text-align:left", style);
            Assert.Contains("text-align:right", style);
            Assert.True(style.IndexOf("text-align:right") > style.IndexOf("text-align:left"),
                $"the column style should land last and win, got '{style}'");
        }

        [Fact]
        public void CellRenderClass_IsMergedWithTheColumnClass()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = Render(ctx, args => args.Attributes["class"] = "my-cell");

            var css = component.FindAll("td").First().GetAttribute("class");
            Assert.NotNull(css);
            Assert.Contains("my-cell", css);
        }

        [Fact]
        public void WithoutCellRender_ClassAndStyleStillLandOnTheCell()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = Render(ctx, cellRender: null);

            var cell = component.FindAll("td").First();
            Assert.Contains("text-align:right", cell.GetAttribute("style"));
            Assert.Contains("Person 1", component.Markup);
        }
    }
}
