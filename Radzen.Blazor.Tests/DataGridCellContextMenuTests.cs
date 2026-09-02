using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class DataGridCellContextMenuTests
    {
        class Person
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        static List<Person> People(int n) =>
            Enumerable.Range(1, n).Select(i => new Person { Id = i, Name = "Person " + i }).ToList();

        static IRenderedComponent<RadzenDataGrid<Person>> RenderGrid(TestContext ctx,
            EventCallback<DataGridCellMouseEventArgs<Person>>? contextMenu)
        {
            return ctx.RenderComponent<RadzenDataGrid<Person>>(p =>
            {
                p.Add(g => g.Data, People(3));

                if (contextMenu.HasValue)
                {
                    p.Add(g => g.CellContextMenu, contextMenu.Value);
                }

                p.Add<RenderFragment>(g => g.Columns, builder =>
                {
                    builder.OpenComponent<RadzenDataGridColumn<Person>>(0);
                    builder.AddAttribute(1, nameof(RadzenDataGridColumn<Person>.Property), nameof(Person.Id));
                    builder.CloseComponent();
                    builder.OpenComponent<RadzenDataGridColumn<Person>>(2);
                    builder.AddAttribute(3, nameof(RadzenDataGridColumn<Person>.Property), nameof(Person.Name));
                    builder.CloseComponent();
                });
            });
        }

        // Detects subtree replacement rather than an ordinary re-render.
        class StatefulCell : ComponentBase
        {
            [Parameter]
            public string Text { get; set; }

            [Parameter]
            public Action OnInit { get; set; }

            protected override void OnInitialized() => OnInit?.Invoke();

            protected override void BuildRenderTree(RenderTreeBuilder builder) => builder.AddContent(0, Text);
        }

        [Fact]
        public void DataGrid_AddingCellContextMenu_DoesNotRebuildTheCellSubtree()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var initializations = 0;

            var component = ctx.RenderComponent<RadzenDataGrid<Person>>(p =>
            {
                p.Add(g => g.Data, People(1));
                p.Add<RenderFragment>(g => g.Columns, builder =>
                {
                    builder.OpenComponent<RadzenDataGridColumn<Person>>(0);
                    builder.AddAttribute(1, nameof(RadzenDataGridColumn<Person>.Property), nameof(Person.Name));
                    builder.AddAttribute(2, nameof(RadzenDataGridColumn<Person>.Template),
                        (RenderFragment<Person>)(item => cell =>
                        {
                            cell.OpenComponent<StatefulCell>(0);
                            cell.AddAttribute(1, nameof(StatefulCell.Text), item.Name);
                            cell.AddAttribute(2, nameof(StatefulCell.OnInit), (Action)(() => initializations++));
                            cell.CloseComponent();
                        }));
                    builder.CloseComponent();
                });
            });

            Assert.Equal(1, initializations);

            component.SetParametersAndRender(p => p.Add(g => g.CellContextMenu,
                EventCallback.Factory.Create<DataGridCellMouseEventArgs<Person>>(this, _ => { })));

            Assert.Equal(1, initializations);

            DataGridCellMouseEventArgs<Person> received = null;

            component.SetParametersAndRender(p => p.Add(g => g.CellContextMenu,
                EventCallback.Factory.Create<DataGridCellMouseEventArgs<Person>>(this, args => received = args)));

            component.Find("td").ContextMenu();

            Assert.NotNull(received);
            Assert.Equal(1, initializations);

            component.SetParametersAndRender(p => p.Add(g => g.CellContextMenu,
                default(EventCallback<DataGridCellMouseEventArgs<Person>>)));

            Assert.Equal(1, initializations);
        }

        [Fact]
        public void DataGrid_CellContextMenu_FiresWithCellAndItem()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            DataGridCellMouseEventArgs<Person> received = null;
            var component = RenderGrid(ctx, EventCallback.Factory.Create<DataGridCellMouseEventArgs<Person>>(
                this, args => received = args));

            var cells = component.FindAll("td");
            Assert.NotEmpty(cells);

            cells[0].ContextMenu();

            Assert.NotNull(received);
            Assert.Equal(1, received.Data.Id);
            Assert.Equal(nameof(Person.Id), received.Column.Property);
        }

        [Fact]
        public void DataGrid_WithoutCellContextMenu_RendersTheSameCells()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var withHandler = RenderGrid(ctx, EventCallback.Factory.Create<DataGridCellMouseEventArgs<Person>>(
                this, _ => { }));
            var withoutHandler = RenderGrid(ctx, null);

            // Handler presence must not change cell content.
            string CellText(IRenderedComponent<RadzenDataGrid<Person>> c) =>
                string.Join("|", c.FindAll("td").Select(td => td.TextContent.Trim()));

            Assert.Equal(CellText(withHandler), CellText(withoutHandler));
            Assert.Contains("Person 1", withoutHandler.Markup);
            Assert.Contains("Person 3", withoutHandler.Markup);
        }

        // A misspelled modifier still lets the handler fire but no longer suppresses the browser menu.
        [Fact]
        public void TheContextMenuModifiersAreTheOnesBlazorActuallyLooksFor()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var cell = RenderGrid(ctx, EventCallback.Factory.Create<DataGridCellMouseEventArgs<Person>>(
                this, _ => { })).Find("tbody td");

            var names = cell.Attributes.Select(a => a.Name).ToArray();

            // bUnit normalizes recognized modifiers, so compare complete attribute names.
            Assert.Contains(names, n =>
                string.Equals(n, "blazor:oncontextmenu:preventDefault", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(names, n =>
                string.Equals(n, "blazor:oncontextmenu:stopPropagation", StringComparison.OrdinalIgnoreCase));

            Assert.DoesNotContain(names, n =>
                n.StartsWith("oncontextmenu:", StringComparison.OrdinalIgnoreCase) ||
                n.StartsWith("__internal", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void AGridWithNoContextMenuHandlerEmitsNoModifiersAtAll()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var cells = RenderGrid(ctx, null).FindAll("td");

            // The grid root has its own unrelated context-menu handler.
            Assert.NotEmpty(cells);
            Assert.All(cells, cell =>
                Assert.DoesNotContain("oncontextmenu", cell.OuterHtml, StringComparison.Ordinal));
        }
    }
}
