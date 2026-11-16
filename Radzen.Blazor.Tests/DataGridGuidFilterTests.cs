using Bunit;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class DataGridGuidFilterTests
    {
        public class Item
        {
            public Guid Id { get; set; }
            public Guid? ParentId { get; set; }
        }

        static readonly Guid KnownId = Guid.Parse("3f2504e0-4f89-11d3-9a0c-0305e82c3301");

        static TestContext CreateContext()
        {
            var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");
            return ctx;
        }

        static RadzenDataGridColumn<Item> RenderColumn(TestContext ctx, string property)
        {
            var cut = ctx.RenderComponent<RadzenDataGrid<Item>>(parameters =>
            {
                parameters.Add(p => p.Data, new List<Item> { new() { Id = KnownId, ParentId = KnownId } });
                parameters.Add(p => p.AllowFiltering, true);
                parameters.Add<RenderFragment>(p => p.Columns, builder =>
                {
                    builder.OpenComponent<RadzenDataGridColumn<Item>>(0);
                    builder.AddAttribute(1, "Property", property);
                    builder.CloseComponent();
                });
            });

            return cut.Instance.ColumnsCollection.First();
        }

        [Theory]
        [InlineData(nameof(Item.Id), "3f2504e0")]
        [InlineData(nameof(Item.Id), "not a guid")]
        [InlineData(nameof(Item.Id), "")]
        [InlineData(nameof(Item.ParentId), "3f2504e0")]
        [InlineData(nameof(Item.ParentId), "")]
        public void SetFilterValue_ClearsFilterValue_WhenTextIsNotAGuid(string property, string text)
        {
            using var ctx = CreateContext();
            var column = RenderColumn(ctx, property);

            column.SetFilterValue(text);
            column.SetFilterValue(text, false);

            Assert.Null(column.GetFilterValue());
            Assert.Null(column.GetSecondFilterValue());
        }

        [Theory]
        [InlineData(nameof(Item.Id))]
        [InlineData(nameof(Item.ParentId))]
        public void SetFilterValue_ParsesGuidText(string property)
        {
            using var ctx = CreateContext();
            var column = RenderColumn(ctx, property);

            column.SetFilterValue(KnownId.ToString());

            Assert.Equal(KnownId, column.GetFilterValue());
        }
    }
}
