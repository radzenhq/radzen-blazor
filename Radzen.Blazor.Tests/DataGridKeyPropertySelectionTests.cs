using System.Collections.Generic;
using System.Linq;
using Bunit;
using Microsoft.AspNetCore.Components;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class DataGridKeyPropertySelectionTests
    {
        class Order
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        static IRenderedComponent<RadzenDataGrid<Order>> Render(TestContext ctx, List<Order> data, IList<Order> selected, string keyProperty, bool keyPropertyFirst)
        {
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            return ctx.RenderComponent<RadzenDataGrid<Order>>(pb =>
            {
                if (keyPropertyFirst && keyProperty != null)
                {
                    pb.Add(p => p.KeyProperty, keyProperty);
                }

                pb.Add(p => p.Data, data);

                if (!keyPropertyFirst && keyProperty != null)
                {
                    pb.Add(p => p.KeyProperty, keyProperty);
                }

                pb.Add(p => p.SelectionMode, DataGridSelectionMode.Single);
                pb.Add(p => p.Value, selected);
                pb.Add(p => p.ValueChanged, EventCallback.Factory.Create<IList<Order>>(new object(), (IList<Order> _) => { }));
                pb.Add(p => p.Columns, b =>
                {
                    b.OpenComponent<RadzenDataGridColumn<Order>>(0);
                    b.AddAttribute(1, nameof(RadzenDataGridColumn<Order>.Property), nameof(Order.Name));
                    b.CloseComponent();
                });
            });
        }

        static List<Order> Orders() => Enumerable.Range(1, 3).Select(i => new Order { Id = i, Name = "Order " + i }).ToList();

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void KeyProperty_MatchesSelectedItem_ByKey_RegardlessOfParameterOrder(bool keyPropertyFirst)
        {
            using var ctx = new TestContext();

            var grid = Render(ctx, Orders(), new List<Order> { new Order { Id = 2, Name = "Order 2" } }, nameof(Order.Id), keyPropertyFirst);

            var highlighted = grid.FindAll("tr.rz-state-highlight");

            Assert.Single(highlighted);
            Assert.Contains("Order 2", highlighted[0].TextContent);
        }

        [Fact]
        public void NoKeyProperty_DoesNotMatch_DifferentInstance()
        {
            using var ctx = new TestContext();

            var grid = Render(ctx, Orders(), new List<Order> { new Order { Id = 2, Name = "Order 2" } }, null, false);

            Assert.Empty(grid.FindAll("tr.rz-state-highlight"));
        }

        [Fact]
        public void KeyProperty_Change_AfterRender_IsApplied()
        {
            using var ctx = new TestContext();
            var data = Orders();

            var grid = Render(ctx, data, new List<Order> { new Order { Id = 2, Name = "Changed" } }, nameof(Order.Name), false);

            Assert.Empty(grid.FindAll("tr.rz-state-highlight"));

            grid.SetParametersAndRender(pb => pb.Add(p => p.KeyProperty, nameof(Order.Id)));

            var highlighted = grid.FindAll("tr.rz-state-highlight");

            Assert.Single(highlighted);
            Assert.Contains("Order 2", highlighted[0].TextContent);
        }
    }
}
