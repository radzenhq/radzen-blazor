using Bunit;
using Microsoft.AspNetCore.Components;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class DataGridAccessibleNameTests
    {
        class Item
        {
            public int Id { get; set; }
        }

        static IRenderedComponent<RadzenDataGrid<Item>> Render(TestContext ctx,
            System.Action<ComponentParameterCollectionBuilder<RadzenDataGrid<Item>>>? extra = null)
        {
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            return ctx.RenderComponent<RadzenDataGrid<Item>>(pb =>
            {
                pb.Add(p => p.Data, new List<Item> { new() { Id = 1 }, new() { Id = 2 } });
                pb.Add(p => p.Columns, b =>
                {
                    b.OpenComponent<RadzenDataGridColumn<Item>>(0);
                    b.AddAttribute(1, nameof(RadzenDataGridColumn<Item>.Property), "Id");
                    b.CloseComponent();
                });
                extra?.Invoke(pb);
            });
        }

        [Fact]
        public void Grid_without_name_renders_no_name_attributes()
        {
            using var ctx = new TestContext();
            var grid = Render(ctx);

            var role = grid.Find("[role=grid]");
            Assert.False(role.HasAttribute("aria-label"));
            Assert.False(role.HasAttribute("aria-labelledby"));
        }

        [Fact]
        public void AriaLabel_parameter_renders_on_role_grid_element()
        {
            using var ctx = new TestContext();
            var grid = Render(ctx, pb => pb.Add(p => p.AriaLabel, "All requests"));

            Assert.Equal("All requests", grid.Find("[role=grid]").GetAttribute("aria-label"));
            Assert.False(grid.Find(".rz-data-grid").HasAttribute("aria-label"));
        }

        [Fact]
        public void AriaLabelledBy_parameter_renders_on_role_treegrid_element()
        {
            using var ctx = new TestContext();
            var grid = Render(ctx, pb =>
            {
                pb.Add(p => p.AriaLabelledBy, "requests-title");
                pb.Add(p => p.LoadChildData, EventCallback.Factory.Create<DataGridLoadChildDataEventArgs<Item>>(ctx.Renderer, _ => { }));
            });

            Assert.Equal("requests-title", grid.Find("[role=treegrid]").GetAttribute("aria-labelledby"));
        }

        [Fact]
        public void Splatted_name_attributes_move_to_role_grid_element()
        {
            using var ctx = new TestContext();
            var grid = Render(ctx, pb =>
            {
                pb.AddUnmatched("aria-labelledby", "requests-title");
                pb.AddUnmatched("aria-label", "All requests");
                pb.AddUnmatched("data-test", "kept");
            });

            var role = grid.Find("[role=grid]");
            Assert.Equal("requests-title", role.GetAttribute("aria-labelledby"));
            Assert.Equal("All requests", role.GetAttribute("aria-label"));

            var wrapper = grid.Find(".rz-data-grid");
            Assert.False(wrapper.HasAttribute("aria-labelledby"));
            Assert.False(wrapper.HasAttribute("aria-label"));
            Assert.Equal("kept", wrapper.GetAttribute("data-test"));
        }

        [Fact]
        public void Parameters_take_precedence_over_splatted_attributes()
        {
            using var ctx = new TestContext();
            var grid = Render(ctx, pb =>
            {
                pb.Add(p => p.AriaLabel, "From parameter");
                pb.AddUnmatched("aria-label", "From attribute");
            });

            Assert.Equal("From parameter", grid.Find("[role=grid]").GetAttribute("aria-label"));
        }

        [Fact]
        public async Task Splatted_name_attribute_change_is_reflected()
        {
            using var ctx = new TestContext();
            var grid = Render(ctx, pb => pb.AddUnmatched("aria-labelledby", "first"));

            Assert.Equal("first", grid.Find("[role=grid]").GetAttribute("aria-labelledby"));

            await grid.InvokeAsync(() => grid.SetParametersAndRender(pb => pb.AddUnmatched("aria-labelledby", "second")));

            Assert.Equal("second", grid.Find("[role=grid]").GetAttribute("aria-labelledby"));
            Assert.False(grid.Find(".rz-data-grid").HasAttribute("aria-labelledby"));
        }
    }
}
