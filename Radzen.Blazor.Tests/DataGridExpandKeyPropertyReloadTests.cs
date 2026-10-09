using Bunit;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class DataGridExpandKeyPropertyReloadTests
    {
        class Row
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        static IRenderedComponent<RadzenDataGrid<Row>> Render(TestContext ctx, Func<IEnumerable<Row>> source, bool keyProperty)
        {
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            return ctx.RenderComponent<RadzenDataGrid<Row>>(pb =>
            {
                if (keyProperty)
                {
                    pb.Add(p => p.KeyProperty, nameof(Row.Id));
                }
                pb.Add(p => p.LoadData, EventCallback.Factory.Create<LoadDataArgs>(new object(), (LoadDataArgs _) => { }));
                pb.Add(p => p.Data, source());
                pb.Add(p => p.Template, (Row r) => b => b.AddContent(0, "detail-" + r.Id));
                pb.Add(p => p.Columns, b =>
                {
                    b.OpenComponent<RadzenDataGridColumn<Row>>(0);
                    b.AddAttribute(1, nameof(RadzenDataGridColumn<Row>.Property), nameof(Row.Name));
                    b.CloseComponent();
                });
            });
        }

        static List<Row> Fresh() => new List<Row>
        {
            new Row { Id = 1, Name = "One" },
            new Row { Id = 2, Name = "Two" },
        };

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ExpandedRow_IconAndDetailAgree_AfterReloadWithFreshObjects(bool keyProperty)
        {
            using var ctx = new TestContext();
            var cut = Render(ctx, Fresh, keyProperty);

            await cut.InvokeAsync(() => cut.Instance.ExpandRow(cut.Instance.Data.First()));
            cut.Render();
            Assert.Single(cut.FindAll("tr.rz-expanded-row-content"));
            Assert.Single(cut.FindAll(".rzi-chevron-circle-down"));

            cut.SetParametersAndRender(pb => pb.Add(p => p.Data, Fresh()));
            await cut.InvokeAsync(() => cut.Instance.Reload());
            cut.Render();

            var expected = keyProperty ? 1 : 0;
            Assert.Equal(expected, cut.FindAll(".rzi-chevron-circle-down").Count);
            Assert.Equal(expected, cut.FindAll("tr.rz-expanded-row-content").Count);
            Assert.Equal(keyProperty, cut.Instance.IsRowExpanded(cut.Instance.Data.First()));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ExpandedRow_ToggleStillWorks_AfterReloadWithFreshObjects(bool keyProperty)
        {
            using var ctx = new TestContext();
            var cut = Render(ctx, Fresh, keyProperty);

            await cut.InvokeAsync(() => cut.Instance.ExpandRow(cut.Instance.Data.First()));
            cut.Render();

            cut.SetParametersAndRender(pb => pb.Add(p => p.Data, Fresh()));
            await cut.InvokeAsync(() => cut.Instance.Reload());
            cut.Render();

            var before = cut.Markup;
            await cut.InvokeAsync(() => cut.Instance.ExpandRow(cut.Instance.Data.First()));
            cut.Render();
            var afterFirstClick = cut.Markup;
            Assert.True(before != afterFirstClick, $"KeyProperty={keyProperty}: first toggle after reload changed nothing");

            await cut.InvokeAsync(() => cut.Instance.ExpandRow(cut.Instance.Data.First()));
            cut.Render();
            Assert.True(afterFirstClick != cut.Markup, $"KeyProperty={keyProperty}: second toggle after reload changed nothing");
        }

        class Node
        {
            public int Id { get; set; }
            public int? ParentId { get; set; }
            public string Name { get; set; }
        }

        static List<Node> FreshNodes() => new List<Node>
        {
            new Node { Id = 1, Name = "Root 1" },
            new Node { Id = 2, Name = "Root 2" },
            new Node { Id = 3, ParentId = 1, Name = "Child 1.1" },
            new Node { Id = 4, ParentId = 1, Name = "Child 1.2" },
        };

        static IRenderedComponent<RadzenDataGrid<Node>> RenderHierarchy(TestContext ctx, Func<List<Node>> source, List<Node> loadChildDataCalls)
        {
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            return ctx.RenderComponent<RadzenDataGrid<Node>>(pb =>
            {
                pb.Add(p => p.KeyProperty, nameof(Node.Id));
                pb.Add(p => p.LoadData, EventCallback.Factory.Create<LoadDataArgs>(new object(), (LoadDataArgs _) => { }));
                pb.Add(p => p.Data, source().Where(n => n.ParentId == null).ToList());
                pb.Add(p => p.LoadChildData, EventCallback.Factory.Create<DataGridLoadChildDataEventArgs<Node>>(new object(), args =>
                {
                    loadChildDataCalls.Add(args.Item);
                    args.Data = source().Where(n => n.ParentId == args.Item.Id).ToList();
                }));
                pb.Add(p => p.Columns, b =>
                {
                    b.OpenComponent<RadzenDataGridColumn<Node>>(0);
                    b.AddAttribute(1, nameof(RadzenDataGridColumn<Node>.Property), nameof(Node.Name));
                    b.CloseComponent();
                });
            });
        }

        [Fact]
        public async Task HierarchyExpandedRow_KeepsChildrenAndToggle_AfterReloadWithFreshObjects()
        {
            using var ctx = new TestContext();
            var calls = new List<Node>();
            var cut = RenderHierarchy(ctx, FreshNodes, calls);

            await cut.InvokeAsync(() => cut.Instance.ExpandRow(cut.Instance.Data.First()));
            cut.Render();
            Assert.Equal(4, cut.FindAll("tr.rz-data-row").Count);
            Assert.Single(cut.FindAll(".rzi-chevron-circle-down"));

            var fresh = FreshNodes().Where(n => n.ParentId == null).ToList();
            cut.SetParametersAndRender(pb => pb.Add(p => p.Data, fresh));
            await cut.InvokeAsync(() => cut.Instance.Reload());
            cut.Render();

            Assert.Equal(4, cut.FindAll("tr.rz-data-row").Count);
            Assert.Single(cut.FindAll(".rzi-chevron-circle-down"));

            await cut.InvokeAsync(() => cut.Instance.ExpandRow(fresh[0]));
            cut.Render();
            Assert.Equal(2, cut.FindAll("tr.rz-data-row").Count);
            Assert.Empty(cut.FindAll(".rzi-chevron-circle-down"));

            await cut.InvokeAsync(() => cut.Instance.ExpandRow(fresh[0]));
            cut.Render();
            Assert.Equal(4, cut.FindAll("tr.rz-data-row").Count);
            Assert.Same(fresh[0], calls.Last());
        }
    }
}
