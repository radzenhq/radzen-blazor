using System;
using System.Collections.Generic;
using System.Linq;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class DataGridColumnVirtualizationTests
    {
        public class Row
        {
            public int Id { get; set; }
        }

        class ColumnOptions
        {
            public string Width { get; set; } = "100px";
            public bool Frozen { get; set; }
            public FrozenColumnPosition FrozenPosition { get; set; }
            public int Children { get; set; }
        }

        static List<Row> Rows(int count = 3) => Enumerable.Range(0, count).Select(i => new Row { Id = i }).ToList();

        static TestContext NewCtx()
        {
            var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");
            return ctx;
        }

        static void AddColumn(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder, string name, ColumnOptions options)
        {
            builder.OpenComponent<RadzenDataGridColumn<Row>>(0);
            builder.SetKey(name);
            builder.AddAttribute(1, nameof(RadzenDataGridColumn<Row>.Title), $"T{name}");
            builder.AddAttribute(2, nameof(RadzenDataGridColumn<Row>.UniqueID), name);

            if (options.Width != null)
            {
                builder.AddAttribute(3, nameof(RadzenDataGridColumn<Row>.Width), options.Width);
            }

            builder.AddAttribute(4, nameof(RadzenDataGridColumn<Row>.Frozen), options.Frozen);
            builder.AddAttribute(5, nameof(RadzenDataGridColumn<Row>.FrozenPosition), options.FrozenPosition);

            if (options.Children > 0)
            {
                builder.AddAttribute(6, nameof(RadzenDataGridColumn<Row>.Columns), (RenderFragment)(child =>
                {
                    for (var index = 0; index < options.Children; index++)
                    {
                        AddColumn(child, $"{name}-{index}", new ColumnOptions { Width = null });
                    }
                }));
            }
            else
            {
                builder.AddAttribute(7, nameof(RadzenDataGridColumn<Row>.Template), (RenderFragment<Row>)(item => cell => cell.AddContent(0, $"r{item.Id}c{name}")));
            }

            builder.CloseComponent();
        }

        static IRenderedComponent<RadzenDataGrid<Row>> Render(TestContext ctx, int columnCount, bool virtualize = true,
            Action<int, ColumnOptions> configure = null, Action<ComponentParameterCollectionBuilder<RadzenDataGrid<Row>>> parameters = null, int rows = 3)
        {
            return ctx.RenderComponent<RadzenDataGrid<Row>>(p =>
            {
                p.Add(g => g.Data, Rows(rows));
                p.Add(g => g.AllowColumnVirtualization, virtualize);
                p.Add(g => g.Columns, b =>
                {
                    for (var index = 0; index < columnCount; index++)
                    {
                        var options = new ColumnOptions();
                        configure?.Invoke(index, options);
                        AddColumn(b, index.ToString(), options);
                    }
                });
                parameters?.Invoke(p);
            });
        }

        static void Scroll(IRenderedComponent<RadzenDataGrid<Row>> component, double left, double width)
        {
            component.InvokeAsync(() => component.Instance.OnColumnScroll(left, width));
        }

        static IReadOnlyList<IElement> BodyCells(IRenderedComponent<RadzenDataGrid<Row>> component, int row = 0)
        {
            return component.FindAll(".rz-grid-table > tbody > tr")[row].Children.ToList();
        }

        static IReadOnlyList<IElement> HeaderCells(IRenderedComponent<RadzenDataGrid<Row>> component, int row = 0)
        {
            return component.FindAll(".rz-grid-table > thead > tr")[row].Children.ToList();
        }

        static IReadOnlyList<IElement> Cols(IRenderedComponent<RadzenDataGrid<Row>> component)
        {
            return component.FindAll(".rz-grid-table > colgroup > col").ToList();
        }

        static bool IsSpacer(IElement element) => element.ClassList.Contains("rz-grid-virtual-spacer");

        static string[] Texts(IEnumerable<IElement> cells) => cells.Where(c => !IsSpacer(c)).Select(c => c.TextContent.Trim()).ToArray();

        [Fact]
        public void Disabled_RendersEveryColumnWithoutSpacersOrVirtualAttributes()
        {
            using var ctx = NewCtx();
            var component = Render(ctx, 100, virtualize: false);

            Assert.Equal(100, BodyCells(component).Count);
            Assert.Equal(100, HeaderCells(component).Count);
            Assert.Empty(component.FindAll(".rz-grid-virtual-spacer"));
            Assert.Empty(component.FindAll("[data-virtual-column-index]"));
            Assert.Empty(component.FindAll("[aria-colindex]"));
            Assert.Null(component.Find(".rz-data-grid-data").GetAttribute("aria-colcount"));
            Assert.All(Cols(component), col => Assert.Equal("width:100px", col.GetAttribute("style")));
        }

        [Fact]
        public void Enabled_RendersFirstColumnsAndTrailingSpacerBeforeMeasurement()
        {
            using var ctx = NewCtx();
            var component = Render(ctx, 100);

            var cells = BodyCells(component);

            Assert.Equal(24, cells.Count);
            Assert.True(IsSpacer(cells[^1]));
            Assert.Equal(Enumerable.Range(0, 23).Select(i => $"r0c{i}"), Texts(cells));

            var cols = Cols(component);

            Assert.Equal(24, cols.Count);
            Assert.Equal("width:7700px;min-width:7700px;max-width:7700px", cols[^1].GetAttribute("style"));
            Assert.Equal("100", component.Find(".rz-data-grid-data").GetAttribute("aria-colcount"));
        }

        [Fact]
        public void Scroll_RendersWindowBetweenLeadingAndTrailingSpacers()
        {
            using var ctx = NewCtx();
            var component = Render(ctx, 100);

            Scroll(component, 5000, 800);

            var cells = BodyCells(component);

            Assert.True(IsSpacer(cells[0]));
            Assert.True(IsSpacer(cells[^1]));
            Assert.Equal(Enumerable.Range(47, 14).Select(i => $"r0c{i}"), Texts(cells));

            var cols = Cols(component);

            Assert.Equal("width:4700px;min-width:4700px;max-width:4700px", cols[0].GetAttribute("style"));
            Assert.Equal("width:3900px;min-width:3900px;max-width:3900px", cols[^1].GetAttribute("style"));

            var headers = HeaderCells(component);

            Assert.Equal(16, headers.Count);
            Assert.Equal("47", headers[1].GetAttribute("data-column-index"));
            Assert.Equal("47", headers[1].GetAttribute("data-virtual-column-index"));
            Assert.Equal("48", headers[1].GetAttribute("aria-colindex"));
            Assert.Equal("47", cells[1].GetAttribute("data-virtual-column-index"));
            Assert.Equal("48", cells[1].GetAttribute("aria-colindex"));
            Assert.EndsWith("47-col", cols[1].Id);
        }

        [Fact]
        public void Scroll_WithinSameWindow_DoesNotRender()
        {
            using var ctx = NewCtx();
            var component = Render(ctx, 100);

            Scroll(component, 5000, 750);

            var renders = component.RenderCount;

            Scroll(component, 5040, 750);

            Assert.Equal(renders, component.RenderCount);
        }

        [Fact]
        public void FrozenColumns_StayRenderedOnBothSides()
        {
            using var ctx = NewCtx();
            var component = Render(ctx, 100, configure: (index, options) =>
            {
                options.Frozen = index == 0 || index == 99;
                options.FrozenPosition = index == 99 ? FrozenColumnPosition.Right : FrozenColumnPosition.Left;
            });

            Scroll(component, 5000, 800);

            var cells = BodyCells(component);
            var texts = Texts(cells);

            Assert.Equal("r0c0", texts[0]);
            Assert.Equal("r0c99", texts[^1]);
            Assert.Equal(16, texts.Length);
            Assert.Equal(2, cells.Count(IsSpacer));
            Assert.True(IsSpacer(cells[1]));
            Assert.True(IsSpacer(cells[^2]));
        }

        [Fact]
        public void Widths_ResolvePercentGridSettingAndDefault()
        {
            using var ctx = NewCtx();
            var component = Render(ctx, 3, configure: (index, options) =>
            {
                options.Width = index == 0 ? "10%" : index == 1 ? null : "5rem";
            });

            Scroll(component, 0, 1000);

            Assert.Equal(new[] { "width:100px", "width:200px", "width:80px" }, Cols(component).Select(c => c.GetAttribute("style")));

            component.SetParametersAndRender(p => p.Add(g => g.ColumnWidth, "150px"));

            Assert.Equal("width:150px", Cols(component)[1].GetAttribute("style"));
        }

        [Fact]
        public void Responsive_DisablesColumnVirtualization()
        {
            using var ctx = NewCtx();
            var component = Render(ctx, 100, parameters: p => p.Add(g => g.Responsive, true));

            Assert.Equal(100, BodyCells(component).Count);
            Assert.Empty(component.FindAll(".rz-grid-virtual-spacer"));
        }

        [Fact]
        public void ColSpan_StartingBeforeWindow_IsClampedToRenderedColumns()
        {
            using var ctx = NewCtx();
            var component = Render(ctx, 100, parameters: p => p.Add(g => g.CellRender, args =>
            {
                if (args.Column.UniqueID == "45")
                {
                    args.Attributes["colspan"] = 5;
                }
            }));

            Scroll(component, 5000, 800);

            var cells = BodyCells(component).Where(c => !IsSpacer(c)).ToList();

            Assert.Equal("r0c45", cells[0].TextContent.Trim());
            Assert.Equal("3", cells[0].GetAttribute("colspan"));
            Assert.Equal(new[] { "r0c45" }.Concat(Enumerable.Range(50, 11).Select(i => $"r0c{i}")), Texts(cells));
        }

        [Fact]
        public void ColSpan_EndingAfterWindow_IsClampedToRenderedColumns()
        {
            using var ctx = NewCtx();
            var component = Render(ctx, 100, parameters: p => p.Add(g => g.CellRender, args =>
            {
                if (args.Column.UniqueID == "59")
                {
                    args.Attributes["colspan"] = 10;
                }
            }));

            Scroll(component, 5000, 800);

            var cells = BodyCells(component);
            var last = cells[^2];

            Assert.True(IsSpacer(cells[^1]));
            Assert.Equal("r0c59", last.TextContent.Trim());
            Assert.Equal("2", last.GetAttribute("colspan"));
            Assert.Equal(13, cells.Count(c => !IsSpacer(c)));
        }

        [Fact]
        public void RowSpan_OmitsCoveredCellInFollowingRow()
        {
            using var ctx = NewCtx();
            var component = Render(ctx, 100, parameters: p => p.Add(g => g.CellRender, args =>
            {
                if (args.Column.UniqueID == "50" && args.Data.Id == 0)
                {
                    args.Attributes["rowspan"] = 2;
                }
            }));

            Scroll(component, 5000, 800);

            Assert.Equal(14, BodyCells(component, 0).Count(c => !IsSpacer(c)));
            Assert.Equal(Enumerable.Range(47, 14).Where(i => i != 50).Select(i => $"r1c{i}"), Texts(BodyCells(component, 1)));
            Assert.Equal(14, BodyCells(component, 2).Count(c => !IsSpacer(c)));
        }

        [Fact]
        public void CompositeColumns_RenderWholeGroupsWithLeafColumnDefinitions()
        {
            using var ctx = NewCtx();
            var component = Render(ctx, 60, configure: (index, options) =>
            {
                options.Children = 2;
                options.Width = "300px";
            });

            Scroll(component, 6000, 600);

            var cells = BodyCells(component);

            Assert.True(IsSpacer(cells[0]));
            Assert.True(IsSpacer(cells[^1]));
            Assert.Equal(Enumerable.Range(17, 8).SelectMany(i => new[] { $"r0c{i}-0", $"r0c{i}-1" }), Texts(cells));

            var cols = Cols(component);

            Assert.Equal(18, cols.Count);
            Assert.Equal("width:150px", cols[1].GetAttribute("style"));

            var headers = HeaderCells(component);

            Assert.True(IsSpacer(headers[0]));
            Assert.Equal("2", headers[0].GetAttribute("rowspan"));
            Assert.Empty(HeaderCells(component, 1).Where(IsSpacer));
        }

        [Fact]
        public void FocusedColumn_StaysRenderedAfterScrollingAway()
        {
            using var ctx = NewCtx();
            var component = Render(ctx, 100);

            component.InvokeAsync(() => component.Instance.OnColumnFocus(2));

            Scroll(component, 5000, 800);

            var cells = BodyCells(component);

            Assert.Equal("r0c2", Texts(cells)[0]);
            Assert.Equal(3, cells.Count(IsSpacer));

            component.InvokeAsync(() => component.Instance.OnColumnFocus(-1));

            Assert.Equal(2, BodyCells(component).Count(IsSpacer));
        }

        [Fact]
        public void EmptyAndGroupRows_SpanRenderedColumnsAndSpacers()
        {
            using var ctx = NewCtx();
            var component = Render(ctx, 100, rows: 0);

            Scroll(component, 5000, 800);

            Assert.Equal("16", component.Find(".rz-datatable-emptymessage").GetAttribute("colspan"));
        }

        [Fact]
        public void FilterAndFooterRows_UseTheSameWindow()
        {
            using var ctx = NewCtx();
            var component = ctx.RenderComponent<RadzenDataGrid<Row>>(p =>
            {
                p.Add(g => g.Data, Rows());
                p.Add(g => g.AllowColumnVirtualization, true);
                p.Add(g => g.AllowFiltering, true);
                p.Add(g => g.FilterMode, FilterMode.Simple);
                p.Add(g => g.Columns, b =>
                {
                    for (var index = 0; index < 100; index++)
                    {
                        b.OpenComponent<RadzenDataGridColumn<Row>>(0);
                        b.SetKey(index);
                        b.AddAttribute(1, nameof(RadzenDataGridColumn<Row>.Property), nameof(Row.Id));
                        b.AddAttribute(2, nameof(RadzenDataGridColumn<Row>.UniqueID), index.ToString());
                        b.AddAttribute(3, nameof(RadzenDataGridColumn<Row>.Width), "100px");
                        b.AddAttribute(4, nameof(RadzenDataGridColumn<Row>.FooterTemplate), (RenderFragment)(f => f.AddContent(0, "f")));
                        b.CloseComponent();
                    }
                });
            });

            Scroll(component, 5000, 800);

            Assert.Equal(16, HeaderCells(component, 1).Count);
            Assert.Equal(2, HeaderCells(component, 1).Count(IsSpacer));

            var footer = component.FindAll(".rz-grid-table > tfoot > tr")[0].Children.ToList();

            Assert.Equal(16, footer.Count);
            Assert.Equal(2, footer.Count(IsSpacer));
        }
    }
}
