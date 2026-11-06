using System.Collections.Generic;
using System.Linq;
using Bunit;
using Microsoft.AspNetCore.Components;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class DataGridFilterOperatorTextTests
    {
        public class Order
        {
            public int Id { get; set; }
            public IEnumerable<string> Tags { get; set; }
        }

        static IRenderedComponent<RadzenDataGrid<Order>> Render(TestContext ctx, FilterOperator op, FilterMode mode = FilterMode.SimpleWithMenu)
        {
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var data = new List<Order>
            {
                new Order { Id = 1, Tags = new[] { "a", "b" } },
                new Order { Id = 2, Tags = new[] { "c" } },
            };

            return ctx.RenderComponent<RadzenDataGrid<Order>>(p => p
                .Add(g => g.Data, data)
                .Add(g => g.AllowFiltering, true)
                .Add(g => g.FilterMode, mode)
                .Add(g => g.InText, "custom in")
                .Add(g => g.NotInText, "custom not in")
                .Add(g => g.Columns, builder =>
                {
                    builder.OpenComponent<RadzenDataGridColumn<Order>>(0);
                    builder.AddAttribute(1, nameof(RadzenDataGridColumn<Order>.Property), nameof(Order.Tags));
                    builder.AddAttribute(2, nameof(RadzenDataGridColumn<Order>.Type), typeof(IEnumerable<string>));
                    builder.AddAttribute(3, nameof(RadzenDataGridColumn<Order>.FilterOperator), op);
                    builder.AddAttribute(4, nameof(RadzenDataGridColumn<Order>.Title), "Tags");
                    if (mode == FilterMode.Advanced)
                    {
                        builder.AddAttribute(5, nameof(RadzenDataGridColumn<Order>.FilterOperators), new[] { FilterOperator.In, FilterOperator.NotIn });
                    }
                    builder.CloseComponent();
                }));
        }

        [Theory]
        [InlineData(FilterOperator.In, "custom in")]
        [InlineData(FilterOperator.NotIn, "custom not in")]
        public void GetFilterOperatorText_UsesGridInAndNotInText(FilterOperator op, string expected)
        {
            using var ctx = new TestContext();
            var grid = Render(ctx, op);

            var column = grid.Instance.ColumnsCollection.Single();

            Assert.Equal(expected, column.GetFilterOperatorText(op));
        }

        [Theory]
        [InlineData(FilterOperator.In, "custom in")]
        [InlineData(FilterOperator.NotIn, "custom not in")]
        public void FilterIcon_Title_UsesGridInAndNotInText(FilterOperator op, string expected)
        {
            using var ctx = new TestContext();
            var grid = Render(ctx, op);

            var icon = grid.Find(".rz-filter-button");

            Assert.Equal(expected, icon.GetAttribute("title"));
        }

        [Theory]
        [InlineData(FilterOperator.In, "custom in")]
        [InlineData(FilterOperator.NotIn, "custom not in")]
        public void AdvancedFilterOperatorDropDown_UsesGridInAndNotInText(FilterOperator op, string expected)
        {
            using var ctx = new TestContext();
            var grid = Render(ctx, op, FilterMode.Advanced);

            var items = grid.FindAll(".rz-grid-filter .rz-dropdown-item").Select(i => i.TextContent.Trim()).ToList();

            Assert.Contains(expected, items);
            Assert.DoesNotContain(op.ToString(), items);
        }
    }
}
