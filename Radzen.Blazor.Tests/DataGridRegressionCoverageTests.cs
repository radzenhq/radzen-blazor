using Bunit;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Radzen.Blazor.Tests
{
    /// <summary>
    /// Regression coverage for RadzenDataGrid render paths that the allocation/perf work touched:
    /// value access (nested, format, enum), cell/row styling (frozen, min/max), selection membership
    /// (with and without KeyProperty), and CellRender/RowRender attribute callbacks. These assert the
    /// user-visible output so the optimizations cannot silently change behavior.
    /// </summary>
    public class DataGridRegressionCoverageTests
    {
        public enum Status { Active, Inactive }

        public class Address
        {
            public string City { get; set; }
        }

        public class Person
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public decimal Salary { get; set; }
            public Status Status { get; set; }
            public Address Address { get; set; }
        }

        static List<Person> People() => new()
        {
            new Person { Id = 1, Name = "Alice", Salary = 5m, Status = Status.Active, Address = new Address { City = "Paris" } },
            new Person { Id = 2, Name = "Bob", Salary = 7m, Status = Status.Inactive, Address = new Address { City = "Berlin" } },
        };

        static TestContext NewCtx()
        {
            var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");
            return ctx;
        }

        // ---- value access ------------------------------------------------------------------------

        [Fact]
        public void NestedStringProperty_RendersCellValue()
        {
            using var ctx = NewCtx();
            var component = ctx.RenderComponent<RadzenDataGrid<Person>>(p =>
            {
                p.Add(g => g.Data, People());
                p.Add(g => g.Columns, b =>
                {
                    b.OpenComponent(0, typeof(RadzenDataGridColumn<Person>));
                    b.AddAttribute(1, nameof(RadzenDataGridColumn<Person>.Property), "Address.City");
                    b.CloseComponent();
                });
            });

            var cells = component.FindAll(".rz-cell-data");
            Assert.Equal("Paris", cells[0].TextContent.Trim());
            Assert.Equal("Berlin", cells[1].TextContent.Trim());
        }

        [Fact]
        public void FormatString_RendersFormattedValue()
        {
            using var ctx = NewCtx();
            var component = ctx.RenderComponent<RadzenDataGrid<Person>>(p =>
            {
                p.Add(g => g.Data, People());
                p.Add(g => g.Columns, b =>
                {
                    b.OpenComponent(0, typeof(RadzenDataGridColumn<Person>));
                    b.AddAttribute(1, nameof(RadzenDataGridColumn<Person>.Property), "Salary");
                    b.AddAttribute(2, nameof(RadzenDataGridColumn<Person>.FormatString), "{0:0.00}");
                    b.CloseComponent();
                });
            });

            var cells = component.FindAll(".rz-cell-data");
            Assert.Equal("5.00", cells[0].TextContent.Trim());
            Assert.Equal("7.00", cells[1].TextContent.Trim());
        }

        [Fact]
        public void EnumProperty_RendersDisplayValue()
        {
            using var ctx = NewCtx();
            var component = ctx.RenderComponent<RadzenDataGrid<Person>>(p =>
            {
                p.Add(g => g.Data, People());
                p.Add(g => g.Columns, b =>
                {
                    b.OpenComponent(0, typeof(RadzenDataGridColumn<Person>));
                    b.AddAttribute(1, nameof(RadzenDataGridColumn<Person>.Property), "Status");
                    b.CloseComponent();
                });
            });

            var cells = component.FindAll(".rz-cell-data");
            Assert.Equal("Active", cells[0].TextContent.Trim());
            Assert.Equal("Inactive", cells[1].TextContent.Trim());
        }

        // ---- styling -----------------------------------------------------------------------------

        [Fact]
        public void FrozenColumn_RendersFrozenCellClass()
        {
            using var ctx = NewCtx();
            var component = ctx.RenderComponent<RadzenDataGrid<Person>>(p =>
            {
                p.Add(g => g.Data, People());
                p.Add(g => g.Columns, b =>
                {
                    b.OpenComponent(0, typeof(RadzenDataGridColumn<Person>));
                    b.AddAttribute(1, nameof(RadzenDataGridColumn<Person>.Property), "Name");
                    b.AddAttribute(2, nameof(RadzenDataGridColumn<Person>.Frozen), true);
                    b.CloseComponent();

                    b.OpenComponent(10, typeof(RadzenDataGridColumn<Person>));
                    b.AddAttribute(11, nameof(RadzenDataGridColumn<Person>.Property), "Salary");
                    b.CloseComponent();
                });
            });

            Assert.Contains("rz-frozen-cell", component.Markup);
        }

        [Fact]
        public void MinMaxWidth_RendersInCellStyle()
        {
            using var ctx = NewCtx();
            var component = ctx.RenderComponent<RadzenDataGrid<Person>>(p =>
            {
                p.Add(g => g.Data, People());
                p.Add(g => g.Columns, b =>
                {
                    b.OpenComponent(0, typeof(RadzenDataGridColumn<Person>));
                    b.AddAttribute(1, nameof(RadzenDataGridColumn<Person>.Property), "Name");
                    b.AddAttribute(2, nameof(RadzenDataGridColumn<Person>.MinWidth), "80px");
                    b.AddAttribute(3, nameof(RadzenDataGridColumn<Person>.MaxWidth), "200px");
                    b.CloseComponent();
                });
            });

            Assert.Contains("min-width:80px", component.Markup);
            Assert.Contains("max-width:200px", component.Markup);
        }

        // ---- selection membership ----------------------------------------------------------------

        [Fact]
        public async Task Selection_HighlightsSelectedRow()
        {
            using var ctx = NewCtx();
            var people = People();
            var component = ctx.RenderComponent<RadzenDataGrid<Person>>(p =>
            {
                p.Add(g => g.Data, people);
                p.Add(g => g.SelectionMode, DataGridSelectionMode.Single);
                p.Add(g => g.RowSelect, EventCallback.Factory.Create<Person>(this, _ => { }));
                p.Add(g => g.Columns, b =>
                {
                    b.OpenComponent(0, typeof(RadzenDataGridColumn<Person>));
                    b.AddAttribute(1, nameof(RadzenDataGridColumn<Person>.Property), "Name");
                    b.CloseComponent();
                });
            });

            Assert.DoesNotContain("rz-state-highlight", component.Markup);

            await component.InvokeAsync(() => component.Instance.SelectRow(people[0]));

            Assert.Contains("rz-state-highlight", component.Markup);
        }

        [Fact]
        public async Task Selection_WithKeyProperty_MatchesByKeyAcrossInstances()
        {
            using var ctx = NewCtx();
            var people = People();
            var component = ctx.RenderComponent<RadzenDataGrid<Person>>(p =>
            {
                p.Add(g => g.Data, people);
                p.Add(g => g.KeyProperty, "Id");
                p.Add(g => g.SelectionMode, DataGridSelectionMode.Single);
                p.Add(g => g.RowSelect, EventCallback.Factory.Create<Person>(this, _ => { }));
                p.Add(g => g.Columns, b =>
                {
                    b.OpenComponent(0, typeof(RadzenDataGridColumn<Person>));
                    b.AddAttribute(1, nameof(RadzenDataGridColumn<Person>.Property), "Name");
                    b.CloseComponent();
                });
            });

            await component.InvokeAsync(() => component.Instance.SelectRow(people[0]));
            Assert.Contains("rz-state-highlight", component.Markup);

            var highlightedBefore = component.FindAll("tr.rz-state-highlight").Count;
            Assert.Equal(1, highlightedBefore);

            // Rebind to brand-new instances with the same keys. Selection is by KeyProperty, so the row
            // whose Id matches the previously selected item must stay highlighted (exercises the
            // keyPropertyGetter comparison path, not reference equality).
            var rebound = new List<Person>
            {
                new Person { Id = 1, Name = "Alice II", Address = new Address { City = "Paris" } },
                new Person { Id = 2, Name = "Bob II", Address = new Address { City = "Berlin" } },
            };
            component.SetParametersAndRender(p => p.Add(g => g.Data, rebound));

            var highlighted = component.FindAll("tr.rz-state-highlight");
            Assert.Single(highlighted);
            Assert.Contains("Alice II", highlighted[0].TextContent);
        }

        // ---- render callbacks --------------------------------------------------------------------

        [Fact]
        public void CellRender_AddsAttributeToCell()
        {
            using var ctx = NewCtx();
            var component = ctx.RenderComponent<RadzenDataGrid<Person>>(p =>
            {
                p.Add(g => g.Data, People());
                p.Add(g => g.CellRender, args => args.Attributes["data-city"] = "yes");
                p.Add(g => g.Columns, b =>
                {
                    b.OpenComponent(0, typeof(RadzenDataGridColumn<Person>));
                    b.AddAttribute(1, nameof(RadzenDataGridColumn<Person>.Property), "Name");
                    b.CloseComponent();
                });
            });

            Assert.Contains("data-city=\"yes\"", component.Markup);
        }

        [Fact]
        public void RowRender_AddsAttributeToRow()
        {
            using var ctx = NewCtx();
            var component = ctx.RenderComponent<RadzenDataGrid<Person>>(p =>
            {
                p.Add(g => g.Data, People());
                p.Add(g => g.RowRender, args => args.Attributes["data-row"] = "r");
                p.Add(g => g.Columns, b =>
                {
                    b.OpenComponent(0, typeof(RadzenDataGridColumn<Person>));
                    b.AddAttribute(1, nameof(RadzenDataGridColumn<Person>.Property), "Name");
                    b.CloseComponent();
                });
            });

            Assert.Contains("data-row=\"r\"", component.Markup);
        }

        // ---- in-place mutation must re-render (why the row has no reference-equality ShouldRender) -----

        // The grid's rows carry no ShouldRender override on purpose: a row renders the item's *current*
        // property values, and the common Radzen refresh pattern mutates a bound item in place (same
        // object reference) and calls Reload(). The row must re-render to show the new value even though
        // its Item reference, Index and selection are all unchanged. A reference-equality ShouldRender on
        // the row (the optimization that is safe for the immutable dropdown-item list) would skip this
        // render and display a stale cell. This test locks in that requirement.
        [Fact]
        public async Task InPlaceMutation_ThenReload_UpdatesCell()
        {
            using var ctx = NewCtx();
            var people = People();
            var component = ctx.RenderComponent<RadzenDataGrid<Person>>(p =>
            {
                p.Add(g => g.Data, people);
                p.Add(g => g.Columns, b =>
                {
                    b.OpenComponent(0, typeof(RadzenDataGridColumn<Person>));
                    b.AddAttribute(1, nameof(RadzenDataGridColumn<Person>.Property), "Name");
                    b.CloseComponent();
                });
            });

            Assert.Equal("Alice", component.FindAll(".rz-cell-data")[0].TextContent.Trim());

            // Mutate the bound item in place - same reference, no collection change.
            people[0].Name = "Alicia";
            await component.InvokeAsync(() => component.Instance.Reload());

            Assert.Equal("Alicia", component.FindAll(".rz-cell-data")[0].TextContent.Trim());
        }
    }
}
