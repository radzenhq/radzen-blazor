using Bunit;
using Microsoft.AspNetCore.Components;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class DataGridResetTests
    {
        class Employee
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public int? ParentId { get; set; }
        }

        static IRenderedComponent<RadzenDataGrid<Employee>> Render(TestContext ctx, List<Employee> roots, List<Employee> children, List<int> loadChildDataCalls)
        {
            return ctx.RenderComponent<RadzenDataGrid<Employee>>(parameterBuilder =>
            {
                parameterBuilder.Add(p => p.Data, roots);
                parameterBuilder.Add(p => p.ExpandMode, DataGridExpandMode.Multiple);
                parameterBuilder.Add(p => p.LoadChildData, EventCallback.Factory.Create<DataGridLoadChildDataEventArgs<Employee>>(ctx.Renderer, args =>
                {
                    loadChildDataCalls.Add(args.Item.Id);
                    var data = children.Where(c => c.ParentId == args.Item.Id).ToList();
                    args.Data = data.Count > 0 ? data : null;
                }));
                parameterBuilder.Add<RenderFragment>(p => p.Columns, builder =>
                {
                    builder.OpenComponent(0, typeof(RadzenDataGridColumn<Employee>));
                    builder.AddAttribute(1, "Property", "Name");
                    builder.CloseComponent();
                });
            });
        }

        [Fact]
        public async Task DataGrid_ResetRowState_ShouldRemoveChildRows()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var roots = new List<Employee>
            {
                new Employee { Id = 1, Name = "Root1" },
                new Employee { Id = 2, Name = "Root2" }
            };
            var children = new List<Employee>
            {
                new Employee { Id = 11, Name = "Child11", ParentId = 1 },
                new Employee { Id = 12, Name = "Child12", ParentId = 1 }
            };
            var calls = new List<int>();

            var component = Render(ctx, roots, children, calls);
            var grid = component.Instance;

            await component.InvokeAsync(() => grid.ExpandRow(roots[0]));
            component.Render();

            Assert.Contains("Child11", component.Markup);
            Assert.True(grid.IsRowExpanded(roots[0]));

            await component.InvokeAsync(() => grid.Reset(resetColumnState: false, resetRowState: true));
            component.Render();

            Assert.False(grid.IsRowExpanded(roots[0]));
            Assert.DoesNotContain("Child11", component.Markup);
        }

        [Fact]
        public async Task DataGrid_ResetRowState_ShouldNotLeakChildRowsIntoNewData()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var roots = new List<Employee>
            {
                new Employee { Id = 1, Name = "Root1" }
            };
            var children = new List<Employee>
            {
                new Employee { Id = 11, Name = "OldChild", ParentId = 1 }
            };
            var calls = new List<int>();

            var component = Render(ctx, roots, children, calls);
            var grid = component.Instance;

            await component.InvokeAsync(() => grid.ExpandRow(roots[0]));
            component.Render();

            Assert.Contains("OldChild", component.Markup);

            await component.InvokeAsync(() => grid.Reset(resetColumnState: false, resetRowState: true));

            var newRoots = new List<Employee>
            {
                new Employee { Id = 3, Name = "NewRoot" }
            };
            component.SetParametersAndRender(parameters => parameters.Add(p => p.Data, newRoots));

            Assert.Contains("NewRoot", component.Markup);
            Assert.DoesNotContain("OldChild", component.Markup);
        }

        [Fact]
        public async Task DataGrid_ExpandRowsAfterReset_ShouldRestoreExpandedState()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var roots = new List<Employee>
            {
                new Employee { Id = 1, Name = "Root1" },
                new Employee { Id = 2, Name = "Root2" }
            };
            var children = new List<Employee>
            {
                new Employee { Id = 11, Name = "Child11", ParentId = 1 }
            };
            var calls = new List<int>();

            var component = Render(ctx, roots, children, calls);
            var grid = component.Instance;

            await component.InvokeAsync(() => grid.ExpandRow(roots[0]));
            component.Render();

            await component.InvokeAsync(() => grid.Reset(resetColumnState: false, resetRowState: true));
            component.Render();

            children.Clear();
            children.Add(new Employee { Id = 13, Name = "Child13", ParentId = 1 });

            await component.InvokeAsync(() => grid.ExpandRows(new[] { roots[0] }));
            component.Render();

            Assert.True(grid.IsRowExpanded(roots[0]));
            Assert.Equal(2, calls.Count);
            Assert.Contains("Child13", component.Markup);
            Assert.DoesNotContain("Child11", component.Markup);
        }
    }
}
