using Bunit;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class DataGridSelfRefHierarchyTests
    {
        class Employee
        {
            public int Id { get; set; }
            public int? ParentId { get; set; }
            public string Name { get; set; }
        }

        static List<Employee> CreateData(int rootCount, int childrenPerRoot, int grandChildrenPerChild = 0)
        {
            var data = new List<Employee>();
            var id = 1;

            for (var r = 0; r < rootCount; r++)
            {
                var root = new Employee { Id = id++, Name = $"Root {r}" };
                data.Add(root);

                for (var c = 0; c < childrenPerRoot; c++)
                {
                    var child = new Employee { Id = id++, ParentId = root.Id, Name = $"Child {r}.{c}" };
                    data.Add(child);

                    for (var g = 0; g < grandChildrenPerChild; g++)
                    {
                        data.Add(new Employee { Id = id++, ParentId = child.Id, Name = $"GrandChild {r}.{c}.{g}" });
                    }
                }
            }

            return data;
        }

        static IRenderedComponent<RadzenDataGrid<Employee>> CreateGrid(TestContext ctx, List<Employee> data, Action<ComponentParameterCollectionBuilder<RadzenDataGrid<Employee>>> configure = null)
        {
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            return ctx.RenderComponent<RadzenDataGrid<Employee>>(pb =>
            {
                pb.Add(p => p.Data, data.Where(e => e.ParentId == null).ToList());
                pb.Add(p => p.LoadChildData, EventCallback.Factory.Create<DataGridLoadChildDataEventArgs<Employee>>(data,
                    args => args.Data = data.Where(e => e.ParentId == args.Item.Id).ToList()));
                pb.Add<RenderFragment>(p => p.Columns, builder =>
                {
                    builder.OpenComponent(0, typeof(RadzenDataGridColumn<Employee>));
                    builder.AddAttribute(1, "Property", "Name");
                    builder.CloseComponent();
                });
                configure?.Invoke(pb);
            });
        }

        [Fact]
        public async Task DataGrid_SelfRefHierarchy_ExpandedChildren_AppearAfterParent()
        {
            var data = CreateData(3, 2);
            using var ctx = new TestContext();
            var component = CreateGrid(ctx, data);
            var grid = component.Instance;
            var roots = data.Where(e => e.ParentId == null).ToList();

            await component.InvokeAsync(() => grid.ExpandRow(roots[1]));

            List<Employee> view = null;
            await component.InvokeAsync(() => view = grid.View.ToList());

            Assert.Equal(new[] { "Root 0", "Root 1", "Child 1.0", "Child 1.1", "Root 2" }, view.Select(e => e.Name));
        }

        [Fact]
        public async Task DataGrid_SelfRefHierarchy_NestedExpand_AppearsInOrder()
        {
            var data = CreateData(2, 2, 2);
            using var ctx = new TestContext();
            var component = CreateGrid(ctx, data);
            var grid = component.Instance;
            var roots = data.Where(e => e.ParentId == null).ToList();
            var child = data.First(e => e.Name == "Child 0.1");

            await component.InvokeAsync(() => grid.ExpandRow(roots[0]));
            await component.InvokeAsync(() => grid.ExpandRow(child));

            List<Employee> view = null;
            await component.InvokeAsync(() => view = grid.View.ToList());

            Assert.Equal(new[] { "Root 0", "Child 0.0", "Child 0.1", "GrandChild 0.1.0", "GrandChild 0.1.1", "Root 1" }, view.Select(e => e.Name));
        }

        [Fact]
        public async Task DataGrid_SelfRefHierarchy_Collapse_RestoresRootView()
        {
            var data = CreateData(3, 2);
            using var ctx = new TestContext();
            var component = CreateGrid(ctx, data);
            var grid = component.Instance;
            var roots = data.Where(e => e.ParentId == null).ToList();

            await component.InvokeAsync(() => grid.ExpandRow(roots[0]));
            await component.InvokeAsync(() => grid.CollapseRows(new[] { roots[0] }));

            List<Employee> view = null;
            await component.InvokeAsync(() => view = grid.View.ToList());

            Assert.Equal(new[] { "Root 0", "Root 1", "Root 2" }, view.Select(e => e.Name));
        }

        [Fact]
        public async Task DataGrid_SelfRefHierarchy_Filter_KeepsParentWithMatchingChild()
        {
            var data = CreateData(3, 2);
            using var ctx = new TestContext();
            var component = CreateGrid(ctx, data, pb => pb.Add(p => p.AllowFiltering, true));
            var grid = component.Instance;
            var roots = data.Where(e => e.ParentId == null).ToList();

            await component.InvokeAsync(() => grid.ExpandRow(roots[1]));

            var column = grid.ColumnsCollection.First();
            await component.InvokeAsync(() => column.SetFilterValue("Child 1.0"));

            List<Employee> view = null;
            await component.InvokeAsync(() => view = grid.View.ToList());

            Assert.Equal(new[] { "Root 1", "Child 1.0" }, view.Select(e => e.Name));
        }

        [Fact]
        public async Task DataGrid_SelfRefHierarchy_Filter_RemovesParentWithoutMatchingChild()
        {
            var data = CreateData(3, 2);
            using var ctx = new TestContext();
            var component = CreateGrid(ctx, data, pb => pb.Add(p => p.AllowFiltering, true));
            var grid = component.Instance;
            var roots = data.Where(e => e.ParentId == null).ToList();

            await component.InvokeAsync(() => grid.ExpandRow(roots[0]));

            var column = grid.ColumnsCollection.First();
            await component.InvokeAsync(() => column.SetFilterValue("Root 2"));

            List<Employee> view = null;
            await component.InvokeAsync(() => view = grid.View.ToList());

            Assert.Equal(new[] { "Root 2" }, view.Select(e => e.Name));
        }

        [Fact]
        public async Task DataGrid_SelfRefHierarchy_Sort_OrdersChildrenWithinParent()
        {
            var data = CreateData(2, 3);
            using var ctx = new TestContext();
            var component = CreateGrid(ctx, data, pb => pb.Add(p => p.AllowSorting, true));
            var grid = component.Instance;
            var roots = data.Where(e => e.ParentId == null).ToList();

            await component.InvokeAsync(() => grid.ExpandRow(roots[0]));
            await component.InvokeAsync(() => grid.OrderByDescending("Name"));

            List<Employee> view = null;
            await component.InvokeAsync(() => view = grid.View.ToList());

            Assert.Equal(new[] { "Root 1", "Root 0", "Child 0.2", "Child 0.1", "Child 0.0" }, view.Select(e => e.Name));
        }

        [Fact]
        public async Task DataGrid_SelfRefHierarchy_ViewCount_ScalesLinearly()
        {
            var data = CreateData(20000, 2);
            using var ctx = new TestContext();
            var component = CreateGrid(ctx, data, pb => pb.Add(p => p.AllowVirtualization, true));
            var grid = component.Instance;
            var roots = data.Where(e => e.ParentId == null).ToList();

            await component.InvokeAsync(() => grid.ExpandRow(roots[0]));

            var sw = Stopwatch.StartNew();
            var count = grid.View.Count();
            var page = grid.View.Skip(10000).Take(50).ToList();
            sw.Stop();

            Assert.Equal(20002, count);
            Assert.Equal(50, page.Count);
            Assert.True(sw.ElapsedMilliseconds < 5000, $"View enumeration took {sw.ElapsedMilliseconds} ms");
        }
    }
}
