using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class DataGridDynamicColumnsDataSwitchTests
    {
        public class Setting
        {
            public string Title { get; set; }
            public string Property { get; set; }
        }

        public class Host : ComponentBase
        {
            public List<Setting> Settings { get; set; } = new();
            public IEnumerable<DataRow> Result { get; set; }
            public List<string> TemplateAccesses = new();

            public void SetDataOne()
            {
                Settings = Enumerable.Range(1, 6).Select(i => new Setting { Title = "COL" + i + " Text", Property = "COL" + i }).ToList();
                var table = new DataTable("GridDataOne");
                for (var i = 1; i <= 6; i++) table.Columns.Add("COL" + i, typeof(decimal));
                for (var r = 0; r < 8; r++) table.Rows.Add(1m, 2m, 3m, 4m, 5m, 6m);
                Result = table.AsEnumerable();
                StateHasChanged();
            }

            public void SetDataTwo()
            {
                Settings = new List<Setting> { new Setting { Title = "COL1 Text", Property = "COL1" } };
                var table = new DataTable("GridDataTwo");
                table.Columns.Add("COL1", typeof(decimal));
                for (var r = 0; r < 7; r++) table.Rows.Add(11m);
                Result = table.AsEnumerable();
                StateHasChanged();
            }

            protected override void BuildRenderTree(RenderTreeBuilder builder)
            {
                builder.OpenComponent<RadzenDataGrid<DataRow>>(0);
                builder.AddAttribute(1, "Data", Result);
                builder.AddAttribute(2, "Columns", (RenderFragment)(cb =>
                {
                    foreach (var setting in Settings)
                    {
                        cb.OpenComponent<RadzenDataGridColumn<DataRow>>(0);
                        cb.AddAttribute(1, "Title", setting.Title);
                        cb.AddAttribute(2, "Property", "(Decimal?)it[\"" + setting.Property + "\"]");
                        cb.AddAttribute(3, "Template", (RenderFragment<DataRow>)(data => tb =>
                        {
                            TemplateAccesses.Add(setting.Property + "@" + data.Table.TableName);
                            tb.OpenElement(0, "span");
                            tb.AddContent(1, data[setting.Property]);
                            tb.CloseElement();
                        }));
                        cb.CloseComponent();
                    }
                }));
                builder.CloseComponent();
            }
        }

        [Fact]
        public void Grid_Does_Not_Render_New_Data_With_Removed_Columns()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var host = ctx.RenderComponent<Host>();
            host.InvokeAsync(() => host.Instance.SetDataOne()).GetAwaiter().GetResult();
            Assert.Equal(6, host.FindAll("th[role=columnheader]").Count);
            Assert.Equal(48, host.FindAll("td[role=gridcell]").Count);

            host.InvokeAsync(() => host.Instance.SetDataTwo()).GetAwaiter().GetResult();

            var stale = host.Instance.TemplateAccesses.Where(a => a.EndsWith("GridDataTwo") && !a.StartsWith("COL1@")).ToList();
            Assert.Empty(stale);
            Assert.Equal(1, host.FindAll("th[role=columnheader]").Count);
            Assert.Equal(7, host.FindAll("td[role=gridcell]").Count);
            Assert.Equal("11", host.Find("td[role=gridcell] span").TextContent);
        }
    }
}
