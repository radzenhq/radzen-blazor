using Bunit;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class DataGridMembershipHashDriftTests
    {
        class ValueRow : IEquatable<ValueRow>
        {
            public int Id { get; set; }
            public string Name { get; set; }

            public bool Equals(ValueRow other) => other != null && Id == other.Id && Name == other.Name;
            public override bool Equals(object obj) => Equals(obj as ValueRow);
            public override int GetHashCode() => HashCode.Combine(Id, Name);
        }

        static IRenderedComponent<RadzenDataGrid<ValueRow>> Render(TestContext ctx, List<ValueRow> data,
            Action<ComponentParameterCollectionBuilder<RadzenDataGrid<ValueRow>>> extra = null)
        {
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            return ctx.RenderComponent<RadzenDataGrid<ValueRow>>(pb =>
            {
                pb.Add(p => p.Data, data);
                pb.Add(p => p.Columns, b =>
                {
                    b.OpenComponent<RadzenDataGridColumn<ValueRow>>(0);
                    b.AddAttribute(1, nameof(RadzenDataGridColumn<ValueRow>.Property), nameof(ValueRow.Name));
                    b.CloseComponent();
                });
                extra?.Invoke(pb);
            });
        }

        [Fact]
        public async Task EditedRow_StaysInEditMode_WhenValueHashedItemIsMutatedDuringEdit()
        {
            using var ctx = new TestContext();
            var data = new List<ValueRow>
            {
                new ValueRow { Id = 1, Name = "One" },
                new ValueRow { Id = 2, Name = "Two" },
            };
            var cut = Render(ctx, data);

            await cut.InvokeAsync(() => cut.Instance.EditRow(data[0]));
            cut.Render();
            Assert.Single(cut.FindAll("tr.rz-datatable-edit"));

            data[0].Name = "Renamed";
            cut.Render();

            Assert.Single(cut.FindAll("tr.rz-datatable-edit"));
            Assert.True(cut.Instance.IsRowInEditMode(data[0]));
        }

        [Fact]
        public async Task SelectedRow_StaysHighlighted_WhenValueHashedItemIsMutated()
        {
            using var ctx = new TestContext();
            var data = new List<ValueRow>
            {
                new ValueRow { Id = 1, Name = "One" },
                new ValueRow { Id = 2, Name = "Two" },
            };
            var cut = Render(ctx, data, pb =>
            {
                pb.Add(p => p.SelectionMode, DataGridSelectionMode.Multiple);
            });

            await cut.InvokeAsync(() => cut.Instance.SelectRow(data[1]));
            cut.Render();
            Assert.Single(cut.FindAll("tr.rz-state-highlight"));

            data[1].Name = "Renamed";
            cut.Render();

            Assert.Single(cut.FindAll("tr.rz-state-highlight"));
        }

        [Fact]
        public async Task ExpandedRow_StaysExpanded_WhenValueHashedItemIsMutated()
        {
            using var ctx = new TestContext();
            var data = new List<ValueRow>
            {
                new ValueRow { Id = 1, Name = "One" },
                new ValueRow { Id = 2, Name = "Two" },
            };
            var cut = Render(ctx, data, pb =>
            {
                pb.Add(p => p.Template, (ValueRow row) => builder => builder.AddContent(0, "detail-" + row.Id));
            });

            await cut.InvokeAsync(() => cut.Instance.ExpandRow(data[0]));
            cut.Render();
            Assert.True(cut.Instance.IsRowExpanded(data[0]));

            data[0].Name = "Renamed";
            cut.Render();

            Assert.True(cut.Instance.IsRowExpanded(data[0]));
        }
    }
}
