using Bunit;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class DataGridODataFilterTests
    {
        public class Holiday
        {
            public DateOnly Day { get; set; }
        }

        [Theory]
        [InlineData(999, "Day eq 0999-01-01")]
        [InlineData(2026, "Day eq 2026-01-01")]
        public void DataGrid_ODataFilter_WritesDateOnlyWithFourDigitYear(int year, string expected)
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenDataGrid<Holiday>>(parameterBuilder =>
            {
                parameterBuilder.Add<IEnumerable<Holiday>>(p => p.Data, new List<Holiday>().AsODataEnumerable());
                parameterBuilder.Add<RenderFragment>(p => p.Columns, builder =>
                {
                    builder.OpenComponent(0, typeof(RadzenDataGridColumn<Holiday>));
                    builder.AddAttribute(1, "Property", nameof(Holiday.Day));
                    builder.AddAttribute(2, "FilterValue", new DateOnly(year, 1, 1));
                    builder.AddAttribute(3, "FilterOperator", FilterOperator.Equals);
                    builder.CloseComponent();
                });
                parameterBuilder.Add<bool>(p => p.AllowFiltering, true);
            });

            var column = component.FindComponent<RadzenDataGridColumn<Holiday>>().Instance;

            Assert.Equal(expected, column.GetColumnODataFilter());
        }
    }
}
