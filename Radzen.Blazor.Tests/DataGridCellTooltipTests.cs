using Bunit;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class DataGridCellTooltipTests
    {
        class Person
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public decimal Salary { get; set; }
        }

        static readonly List<Person> People = new()
        {
            new Person { Id = 1, Name = "Alice", Salary = 50000m },
            new Person { Id = 2, Name = null, Salary = 61234.5m },
        };

        static IRenderedComponent<RadzenDataGrid<Person>> Render(TestContext ctx,
            Action<ComponentParameterCollectionBuilder<RadzenDataGrid<Person>>> extra = null,
            RenderFragment columns = null)
        {
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            return ctx.RenderComponent<RadzenDataGrid<Person>>(p =>
            {
                p.Add(g => g.Data, People);
                p.Add(g => g.Columns, columns ?? NameColumn);
                extra?.Invoke(p);
            });
        }

        static readonly RenderFragment NameColumn = builder =>
        {
            builder.OpenComponent<RadzenDataGridColumn<Person>>(0);
            builder.AddAttribute(1, nameof(RadzenDataGridColumn<Person>.Property), "Name");
            builder.AddAttribute(2, nameof(RadzenDataGridColumn<Person>.Title), "Name");
            builder.CloseComponent();
        };

        static IReadOnlyList<AngleSharp.Dom.IElement> Cells(IRenderedComponent<RadzenDataGrid<Person>> cut) =>
            cut.FindAll("tbody td span.rz-cell-data").ToArray();

        [Fact]
        public void TheCellCarriesItsOwnValueAsATitle()
        {
            using var ctx = new TestContext();

            var cells = Cells(Render(ctx));

            Assert.Equal("Alice", cells[0].GetAttribute("title"));
        }

        // Empty values omit the attribute rather than emitting title="".
        [Fact]
        public void ACellWithNoValueCarriesNoTitleAttributeAtAll()
        {
            using var ctx = new TestContext();

            var cells = Cells(Render(ctx));

            Assert.False(cells[1].HasAttribute("title"),
                "a null value must leave the attribute off entirely, not emit title=\"\"");
        }

        [Fact]
        public void TurningItOffLeavesNoTitleOnAnyCell()
        {
            using var ctx = new TestContext();

            var cells = Cells(Render(ctx, p => p.Add(g => g.ShowCellDataAsTooltip, false)));

            Assert.All(cells, cell => Assert.False(cell.HasAttribute("title")));
        }

        [Fact]
        public void ATemplateColumnGetsNoTitleFromTheGridWideDefault()
        {
            using var ctx = new TestContext();

            var cells = Cells(Render(ctx, columns: builder =>
            {
                builder.OpenComponent<RadzenDataGridColumn<Person>>(0);
                builder.AddAttribute(1, nameof(RadzenDataGridColumn<Person>.Property), "Name");
                builder.AddAttribute(2, nameof(RadzenDataGridColumn<Person>.Template),
                    (RenderFragment<Person>)(person => inner => inner.AddContent(0, "[" + person.Name + "]")));
                builder.CloseComponent();
            }));

            Assert.All(cells, cell => Assert.False(cell.HasAttribute("title")));
        }

        // An opted-in template column uses the underlying value, not its rendered content.
        [Fact]
        public void ATemplateColumnThatAsksForATitleGetsTheUnderlyingValue()
        {
            using var ctx = new TestContext();

            var cells = Cells(Render(ctx, columns: builder =>
            {
                builder.OpenComponent<RadzenDataGridColumn<Person>>(0);
                builder.AddAttribute(1, nameof(RadzenDataGridColumn<Person>.Property), "Name");
                builder.AddAttribute(2, nameof(RadzenDataGridColumn<Person>.ShowCellDataAsTooltip), true);
                builder.AddAttribute(3, nameof(RadzenDataGridColumn<Person>.Template),
                    (RenderFragment<Person>)(person => inner => inner.AddContent(0, "[" + person.Name + "]")));
                builder.CloseComponent();
            }));

            Assert.Equal("Alice", cells[0].GetAttribute("title"));
            Assert.Equal("[Alice]", cells[0].TextContent);
        }

        // The title and body share the formatted value.
        [Fact]
        public void TheTitleIsTheFormattedValueAndMatchesWhatTheCellShows()
        {
            using var ctx = new TestContext();

            var cells = Cells(Render(ctx, columns: builder =>
            {
                builder.OpenComponent<RadzenDataGridColumn<Person>>(0);
                builder.AddAttribute(1, nameof(RadzenDataGridColumn<Person>.Property), "Salary");
                builder.AddAttribute(2, nameof(RadzenDataGridColumn<Person>.FormatString), "{0:0.00}");
                builder.CloseComponent();
            }));

            Assert.Equal("50000.00", cells[0].GetAttribute("title"));
            Assert.Equal(cells[0].TextContent, cells[0].GetAttribute("title"));
        }

        class CountingColumn : RadzenDataGridColumn<Person>
        {
            public int Calls { get; private set; }

            public override object GetValue(Person item)
            {
                Calls++;

                return base.GetValue(item);
            }
        }

        class NullReturningColumn : CountingColumn
        {
            public override object GetValue(Person item)
            {
                base.GetValue(item);

                return null;
            }
        }

        static IRenderedComponent<RadzenDataGrid<Person>> RenderCounting(TestContext ctx,
            bool tooltip, int rows, out CountingColumn column, bool returnsNull = false)
        {
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            CountingColumn captured = null;

            var cut = ctx.RenderComponent<RadzenDataGrid<Person>>(p =>
            {
                p.Add(g => g.Data, People.Take(rows).ToList());
                p.Add(g => g.ShowCellDataAsTooltip, tooltip);
                p.Add<RenderFragment>(g => g.Columns, builder =>
                {
                    builder.OpenComponent(0, returnsNull ? typeof(NullReturningColumn) : typeof(CountingColumn));
                    builder.AddAttribute(1, nameof(RadzenDataGridColumn<Person>.Property), "Name");
                    builder.AddAttribute(2, nameof(RadzenDataGridColumn<Person>.EditTemplate),
                        (RenderFragment<Person>)(person => inner => inner.AddContent(0, "editing")));
                    builder.AddComponentReferenceCapture(3, c => captured = (CountingColumn)c);
                    builder.CloseComponent();
                });
            });

            column = captured;

            return cut;
        }

        // Call frequency is observable because GetValue is virtual; compare deltas across render passes.
        [Fact]
        public void EnteringEditModeAsksForNoValueWhenTheTooltipIsOff()
        {
            using var ctx = new TestContext();

            var cut = RenderCounting(ctx, tooltip: false, rows: 1, out var column);
            var before = column.Calls;

            cut.InvokeAsync(() => cut.Instance.EditRow(People[0])).Wait();

            Assert.Contains("editing", cut.Markup);
            Assert.Equal(before, column.Calls);
        }

        // Enabling the tooltip must not add a second lookup.
        [Fact]
        public void TheTooltipDoesNotCostASecondLookup()
        {
            using var withTooltipCtx = new TestContext();
            using var withoutTooltipCtx = new TestContext();

            RenderCounting(withTooltipCtx, tooltip: true, rows: 2, out var withTooltip);
            RenderCounting(withoutTooltipCtx, tooltip: false, rows: 2, out var withoutTooltip);

            Assert.True(withoutTooltip.Calls > 0, "the read-only body has to ask for its value");
            Assert.Equal(withoutTooltip.Calls, withTooltip.Calls);
        }

        // A legitimate null result must still count as derived.
        [Fact]
        public void AnOverrideThatReturnsNullIsStillOnlyAskedOnce()
        {
            using var nullCtx = new TestContext();
            using var valueCtx = new TestContext();

            RenderCounting(nullCtx, tooltip: true, rows: 2, out var returnsNull, returnsNull: true);
            RenderCounting(valueCtx, tooltip: true, rows: 2, out var returnsValue);

            Assert.True(returnsValue.Calls > 0, "the tooltip has to ask for its value");
            Assert.Equal(returnsValue.Calls, returnsNull.Calls);
        }

        [Fact]
        public void TheResponsiveBranchCarriesTheTitleToo()
        {
            using var ctx = new TestContext();

            var cells = Cells(Render(ctx, p => p.Add(g => g.Responsive, true)));

            Assert.Equal("Alice", cells[0].GetAttribute("title"));
            Assert.False(cells[1].HasAttribute("title"));
        }
    }
}
