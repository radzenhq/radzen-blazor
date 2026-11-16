using System;
using System.Collections.Generic;
using System.Linq;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class DataGridColumnVirtualizationLayoutTests
    {
        public class Item
        {
            public int Id { get; set; }
            public int? ParentId { get; set; }
            public string Category { get; set; }
            public string Region { get; set; }
            public int Value { get; set; }
        }

        const int ColumnCount = 60;

        static List<Item> Items(int count = 8) => Enumerable.Range(0, count).Select(i => new Item
        {
            Id = i,
            Category = $"C{i % 2}",
            Region = $"R{i % 3}",
            Value = i
        }).ToList();

        static TestContext NewCtx()
        {
            var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");
            return ctx;
        }

        static void AddLeaf(RenderTreeBuilder builder, string name, bool footers, string width = "100px")
        {
            builder.OpenComponent<RadzenDataGridColumn<Item>>(0);
            builder.SetKey(name);
            builder.AddAttribute(1, nameof(RadzenDataGridColumn<Item>.Title), name);
            builder.AddAttribute(2, nameof(RadzenDataGridColumn<Item>.UniqueID), name);

            if (width != null)
            {
                builder.AddAttribute(3, nameof(RadzenDataGridColumn<Item>.Width), width);
            }

            builder.AddAttribute(4, nameof(RadzenDataGridColumn<Item>.Template), (RenderFragment<Item>)(item => cell => cell.AddContent(0, $"{item.Id}:{name}")));
            builder.AddAttribute(5, nameof(RadzenDataGridColumn<Item>.EditTemplate), (RenderFragment<Item>)(item => cell => cell.AddMarkupContent(0, $"<input value=\"{name}\" />")));

            if (footers)
            {
                builder.AddAttribute(6, nameof(RadzenDataGridColumn<Item>.FooterTemplate), (RenderFragment)(cell => cell.AddContent(0, $"f{name}")));
                builder.AddAttribute(7, nameof(RadzenDataGridColumn<Item>.GroupFooterTemplate), (RenderFragment<Group>)(group => cell => cell.AddContent(0, $"g{name}")));
            }

            builder.CloseComponent();
        }

        static void AddProperty(RenderTreeBuilder builder, string property, bool frozen = false)
        {
            builder.OpenComponent<RadzenDataGridColumn<Item>>(0);
            builder.SetKey(property);
            builder.AddAttribute(1, nameof(RadzenDataGridColumn<Item>.Property), property);
            builder.AddAttribute(2, nameof(RadzenDataGridColumn<Item>.Title), property);
            builder.AddAttribute(3, nameof(RadzenDataGridColumn<Item>.Width), "100px");
            builder.AddAttribute(4, nameof(RadzenDataGridColumn<Item>.Frozen), frozen);
            builder.CloseComponent();
        }

        static void AddGroup(RenderTreeBuilder builder, string name, int children, bool footers)
        {
            builder.OpenComponent<RadzenDataGridColumn<Item>>(0);
            builder.SetKey(name);
            builder.AddAttribute(1, nameof(RadzenDataGridColumn<Item>.Title), name);
            builder.AddAttribute(2, nameof(RadzenDataGridColumn<Item>.Columns), (RenderFragment)(child =>
            {
                for (var index = 0; index < children; index++)
                {
                    AddLeaf(child, $"{name}-{index}", footers, null);
                }
            }));
            builder.CloseComponent();
        }

        static IRenderedComponent<RadzenDataGrid<Item>> Render(TestContext ctx, Action<ComponentParameterCollectionBuilder<RadzenDataGrid<Item>>> parameters = null,
            bool footers = false, int composite = 0, bool frozen = false, List<Item> data = null)
        {
            var component = ctx.RenderComponent<RadzenDataGrid<Item>>(p =>
            {
                p.Add(g => g.Data, data ?? Items());
                p.Add(g => g.AllowColumnVirtualization, true);
                p.Add(g => g.Columns, b =>
                {
                    AddProperty(b, nameof(Item.Category), frozen);
                    AddProperty(b, nameof(Item.Region));

                    for (var index = 0; index < ColumnCount; index++)
                    {
                        if (composite > 0)
                        {
                            AddGroup(b, index.ToString(), composite, footers);
                        }
                        else
                        {
                            AddLeaf(b, index.ToString(), footers);
                        }
                    }
                });
                parameters?.Invoke(p);
            });

            component.InvokeAsync(() => component.Instance.OnColumnScroll(3000, 800));

            return component;
        }

        static IElement Table(IRenderedComponent<RadzenDataGrid<Item>> component) => component.Find(".rz-grid-table");

        static int AssertRectangular(IRenderedComponent<RadzenDataGrid<Item>> component)
        {
            var table = Table(component);
            var columns = table.Children.First(c => c.LocalName == "colgroup").Children.Length;

            Assert.True(columns > 0);

            foreach (var section in table.Children.Where(c => c.LocalName is "thead" or "tbody" or "tfoot"))
            {
                var carried = new Dictionary<int, int>();
                var rowIndex = 0;

                foreach (var row in section.Children.Where(c => c.LocalName == "tr" && c.Children.Length > 0))
                {
                    var position = 0;
                    var next = new Dictionary<int, int>();

                    void Skip()
                    {
                        while (carried.TryGetValue(position, out var remaining))
                        {
                            if (remaining > 1)
                            {
                                next[position] = remaining - 1;
                            }

                            position++;
                        }
                    }

                    foreach (var cell in row.Children)
                    {
                        Skip();

                        var colSpan = int.Parse(cell.GetAttribute("colspan") ?? "1");
                        var rowSpan = int.Parse(cell.GetAttribute("rowspan") ?? "1");

                        for (var offset = 0; offset < colSpan; offset++)
                        {
                            if (rowSpan > 1)
                            {
                                next[position] = rowSpan - 1;
                            }

                            position++;
                        }
                    }

                    Skip();

                    foreach (var pending in carried.Where(c => c.Key >= position && c.Value > 1))
                    {
                        next[pending.Key] = pending.Value - 1;
                    }

                    var covered = position + carried.Count(c => c.Key >= position);

                    Assert.True(columns == covered, $"{section.LocalName} row {rowIndex} covers {covered} of {columns} columns: {row.OuterHtml[..Math.Min(300, row.OuterHtml.Length)]}");

                    carried = next;
                    rowIndex++;
                }
            }

            return columns;
        }

        static int Spacers(IElement row) => row.Children.Count(c => c.ClassList.Contains("rz-grid-virtual-spacer"));

        static IEnumerable<IElement> BodyRows(IRenderedComponent<RadzenDataGrid<Item>> component) =>
            Table(component).Children.First(c => c.LocalName == "tbody").Children.Where(c => c.LocalName == "tr");

        [Fact]
        public void Plain_IsRectangular()
        {
            using var ctx = NewCtx();
            var component = Render(ctx);

            Assert.Equal(16, AssertRectangular(component));
            Assert.All(BodyRows(component), row => Assert.Equal(2, Spacers(row)));
        }

        [Theory]
        [InlineData(true, false)]
        [InlineData(false, false)]
        [InlineData(true, true)]
        public void Grouping_IsRectangular(bool showGroupExpandColumn, bool twoLevels)
        {
            using var ctx = NewCtx();
            var component = Render(ctx, p =>
            {
                p.Add(g => g.AllowGrouping, true);
                p.Add(g => g.ShowGroupExpandColumn, showGroupExpandColumn);
            }, footers: true, frozen: true);

            component.InvokeAsync(() =>
            {
                component.Instance.Groups.Add(new GroupDescriptor { Property = nameof(Item.Category) });

                if (twoLevels)
                {
                    component.Instance.Groups.Add(new GroupDescriptor { Property = nameof(Item.Region) });
                }
            });
            component.Render();

            AssertRectangular(component);

            var rows = BodyRows(component).ToList();

            Assert.Contains(rows, row => row.ClassList.Contains("rz-group-row"));
            Assert.Contains(rows, row => row.TextContent.Contains("g30"));
            Assert.Contains(rows, row => row.TextContent.Contains("0:30"));
            Assert.All(rows.Where(row => !row.ClassList.Contains("rz-group-row")), row => Assert.Equal(2, Spacers(row)));
        }

        [Fact]
        public void HiddenGroupedColumn_IsRectangular()
        {
            using var ctx = NewCtx();
            var component = Render(ctx, p =>
            {
                p.Add(g => g.AllowGrouping, true);
                p.Add(g => g.HideGroupedColumn, true);
            });

            component.InvokeAsync(() => component.Instance.Groups.Add(new GroupDescriptor { Property = nameof(Item.Category) }));
            component.Render();

            AssertRectangular(component);
        }

        [Fact]
        public void ExpandedDetailTemplate_SpansAllRenderedColumns()
        {
            using var ctx = NewCtx();
            var component = Render(ctx, p => p.Add(g => g.Template, (RenderFragment<Item>)(item => b => b.AddContent(0, $"detail{item.Id}"))));

            component.InvokeAsync(() => component.Instance.ExpandRow(component.Instance.View.First()));
            component.Render();

            Assert.Equal(17, AssertRectangular(component));
            Assert.Contains(BodyRows(component), row => row.ClassList.Contains("rz-expanded-row-content") && row.TextContent.Contains("detail0"));
        }

        [Fact]
        public void SelfReferenceHierarchy_IsRectangular()
        {
            using var ctx = NewCtx();
            var all = Items(12);

            for (var index = 4; index < all.Count; index++)
            {
                all[index].ParentId = index % 4;
            }

            var component = Render(ctx, p => p.Add(g => g.LoadChildData, args => { args.Data = all.Where(i => i.ParentId == args.Item.Id); }),
                data: all.Where(i => i.ParentId == null).ToList());

            component.InvokeAsync(() => component.Instance.ExpandRow(component.Instance.View.First()));
            component.Render();

            AssertRectangular(component);

            var rows = BodyRows(component).ToList();

            Assert.Equal(6, rows.Count);
            Assert.Contains(rows, row => row.TextContent.Contains("4:30"));
            Assert.All(rows, row => Assert.Equal(2, Spacers(row)));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void CompositeColumns_AreRectangular(bool compositeDataCells)
        {
            using var ctx = NewCtx();
            var component = Render(ctx, p => p.Add(g => g.AllowCompositeDataCells, compositeDataCells), footers: true, composite: 3);

            AssertRectangular(component);

            Assert.Contains(BodyRows(component), row => row.TextContent.Contains("0:5-2"));
            Assert.DoesNotContain(BodyRows(component), row => row.TextContent.Contains("0:20-0"));
        }

        [Fact]
        public void CompositeColumnsWithGroupingAndDetail_AreRectangular()
        {
            using var ctx = NewCtx();
            var component = Render(ctx, p =>
            {
                p.Add(g => g.AllowGrouping, true);
                p.Add(g => g.Template, (RenderFragment<Item>)(item => b => b.AddContent(0, "detail")));
            }, footers: true, composite: 2, frozen: true);

            component.InvokeAsync(() => component.Instance.Groups.Add(new GroupDescriptor { Property = nameof(Item.Category) }));
            component.Render();
            component.InvokeAsync(() => component.Instance.ExpandRow(component.Instance.View.First()));
            component.Render();

            AssertRectangular(component);
        }

        [Fact]
        public void FilterRowFooterAndPaging_AreRectangular()
        {
            using var ctx = NewCtx();
            var component = Render(ctx, p =>
            {
                p.Add(g => g.AllowFiltering, true);
                p.Add(g => g.FilterMode, FilterMode.Simple);
                p.Add(g => g.AllowPaging, true);
                p.Add(g => g.PageSize, 3);
                p.Add(g => g.AllowSorting, true);
                p.Add(g => g.AllowColumnResize, true);
                p.Add(g => g.AllowColumnReorder, true);
            }, footers: true, frozen: true);

            AssertRectangular(component);

            Assert.Equal(3, BodyRows(component).Count());
        }

        [Fact]
        public void EditedRow_KeepsWindowAndRendersEditors()
        {
            using var ctx = NewCtx();
            var component = Render(ctx);

            component.InvokeAsync(() => component.Instance.EditRow(component.Instance.View.First()));
            component.Render();

            AssertRectangular(component);

            var first = BodyRows(component).First();

            Assert.Equal(14, first.QuerySelectorAll("input").Length);
        }

        [Fact]
        public void EmptyGroupedGrid_IsRectangular()
        {
            using var ctx = NewCtx();
            var component = Render(ctx, p => p.Add(g => g.AllowGrouping, true), data: new List<Item>());

            AssertRectangular(component);
        }

        [Fact]
        public void HidingColumns_RebuildsWindow()
        {
            using var ctx = NewCtx();
            var component = Render(ctx, p => p.Add(g => g.AllowColumnPicking, true));

            Assert.Equal(16, AssertRectangular(component));

            component.InvokeAsync(() =>
            {
                foreach (var column in component.Instance.ColumnsCollection.Where(c => c.UniqueID != null && int.TryParse(c.UniqueID, out var index) && index % 2 == 0))
                {
                    column.Visible = false;
                }
            });
            component.Render();

            Assert.Equal(6, AssertRectangular(component));
            Assert.DoesNotContain(BodyRows(component), row => row.TextContent.Contains("0:30"));
            Assert.Contains(BodyRows(component), row => row.TextContent.Contains("0:59"));
        }
    }
}
