using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class DataGridEnumTranslationTests
    {
        enum LeaseStatus { Active, Expired }

        enum UserStatus { [Display(Description = "Active user")] Active, Locked }

        class Row
        {
            public LeaseStatus Lease { get; set; }
            public UserStatus User { get; set; }
            public UserStatus? MaybeUser { get; set; }
        }

        static readonly List<Row> Rows = new()
        {
            new Row { Lease = LeaseStatus.Active, User = UserStatus.Active, MaybeUser = UserStatus.Active },
            new Row { Lease = LeaseStatus.Expired, User = UserStatus.Locked, MaybeUser = null },
        };

        static string Translate(Enum value) => value.GetType().Name + "." + value;

        static TestContext CreateContext()
        {
            var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");
            return ctx;
        }

        static readonly RenderFragment Columns = b =>
        {
            var seq = 0;
            foreach (var prop in new[] { nameof(Row.Lease), nameof(Row.User), nameof(Row.MaybeUser) })
            {
                b.OpenComponent<RadzenDataGridColumn<Row>>(seq++);
                b.AddAttribute(seq++, nameof(RadzenDataGridColumn<Row>.Property), prop);
                b.AddAttribute(seq++, nameof(RadzenDataGridColumn<Row>.Title), prop);
                b.CloseComponent();
            }
        };

        static IRenderedComponent<RadzenDataGrid<Row>> Render(TestContext ctx, Action<ComponentParameterCollectionBuilder<RadzenDataGrid<Row>>> extra = null)
        {
            return ctx.RenderComponent<RadzenDataGrid<Row>>(p =>
            {
                p.Add(g => g.Data, Rows);
                p.Add(g => g.Columns, Columns);
                extra?.Invoke(p);
            });
        }

        static string[] CellTexts(IRenderedComponent<RadzenDataGrid<Row>> cut) =>
            cut.FindAll("tbody td span.rz-cell-data").Select(c => c.TextContent.Trim()).ToArray();

        [Fact]
        public void TypedHookReceivesTheEnumValueSoMembersSharingANameAreToldApart()
        {
            using var ctx = CreateContext();
            var received = new List<Enum>();

            var cut = Render(ctx, p => p.Add(g => g.EnumTranslationFunc, v => { received.Add(v); return Translate(v); }));

            Assert.Equal(new[] { "LeaseStatus.Active", "UserStatus.Active", "UserStatus.Active", "LeaseStatus.Expired", "UserStatus.Locked", "" }, CellTexts(cut));
            Assert.Contains(LeaseStatus.Active, received);
            Assert.Contains(UserStatus.Active, received);
        }

        [Fact]
        public void TypedHookTakesPrecedenceOverTheTextHookAndNullFallsBackToIt()
        {
            using var ctx = CreateContext();
            var textHookInputs = new List<string>();

            var cut = Render(ctx, p =>
            {
                p.Add(g => g.EnumTranslationFunc, v => v is LeaseStatus ? "lease:" + v : null);
                p.Add(g => g.EnumFilterTranslationFunc, s => { textHookInputs.Add(s); return "text:" + s; });
            });

            Assert.Equal(new[] { "lease:Active", "text:Active user", "text:Active user", "lease:Expired", "text:Locked", "" }, CellTexts(cut));
            Assert.DoesNotContain("Active", textHookInputs);
            Assert.DoesNotContain("Expired", textHookInputs);
        }

        [Fact]
        public void TextHookAloneStillReceivesTheDisplayText()
        {
            using var ctx = CreateContext();

            var cut = Render(ctx, p => p.Add(g => g.EnumFilterTranslationFunc, s => "text:" + s));

            Assert.Equal(new[] { "text:Active", "text:Active user", "text:Active user", "text:Expired", "text:Locked", "" }, CellTexts(cut));
        }

        [Fact]
        public async Task ExportsUseTheTypedHook()
        {
            using var ctx = CreateContext();
            var cut = Render(ctx, p => p.Add(g => g.EnumTranslationFunc, Translate));

            var excel = await cut.InvokeAsync(() => cut.Instance.ToWorkbookAsync());
            var csv = await cut.InvokeAsync(() => cut.Instance.ToCsvWorkbookAsync(new DataGridCsvExportOptions()));

            foreach (var sheet in new[] { excel.Sheets[0], csv.Sheets[0] })
            {
                Assert.Equal("LeaseStatus.Active", sheet.Cells[1, 0].Value);
                Assert.Equal("UserStatus.Active", sheet.Cells[1, 1].Value);
                Assert.Equal("UserStatus.Active", sheet.Cells[1, 2].Value);
                Assert.Equal("UserStatus.Locked", sheet.Cells[2, 1].Value);
            }
        }

        [Fact]
        public void SimpleFilterOptionsUseTheTypedHook()
        {
            using var ctx = CreateContext();

            var cut = Render(ctx, p =>
            {
                p.Add(g => g.AllowFiltering, true);
                p.Add(g => g.FilterMode, FilterMode.Simple);
                p.Add(g => g.EnumTranslationFunc, Translate);
            });

            var options = cut.FindAll(".rz-grid-filter .rz-dropdown-item, .rz-cell-filter .rz-dropdown-item").Select(o => o.TextContent.Trim()).ToArray();

            Assert.Contains("LeaseStatus.Active", options);
            Assert.Contains("LeaseStatus.Expired", options);
            Assert.Contains("UserStatus.Active", options);
            Assert.Contains("UserStatus.Locked", options);
            Assert.DoesNotContain("Active user", options);
        }

        [Fact]
        public void AdvancedFilterOptionsUseTheTypedHook()
        {
            using var ctx = CreateContext();

            var cut = Render(ctx, p =>
            {
                p.Add(g => g.AllowFiltering, true);
                p.Add(g => g.FilterMode, FilterMode.Advanced);
                p.Add(g => g.EnumTranslationFunc, Translate);
            });

            var options = cut.FindAll(".rz-grid-filter .rz-dropdown-item").Select(o => o.TextContent.Trim()).ToArray();

            Assert.Contains("LeaseStatus.Active", options);
            Assert.Contains("UserStatus.Active", options);
            Assert.DoesNotContain("Active user", options);
        }

        [Fact]
        public void CheckBoxListFilterUsesTheTypedHook()
        {
            using var ctx = CreateContext();

            var cut = Render(ctx, p =>
            {
                p.Add(g => g.AllowFiltering, true);
                p.Add(g => g.FilterMode, FilterMode.CheckBoxList);
                p.Add(g => g.EnumTranslationFunc, Translate);
            });

            var expected = new[]
            {
                new[] { "LeaseStatus.Active", "LeaseStatus.Expired" },
                new[] { "UserStatus.Active", "UserStatus.Locked" },
                new[] { "UserStatus.Active", "" },
            };

            for (var column = 0; column < expected.Length; column++)
            {
                cut.FindAll("button.rz-grid-filter-icon")[column].MouseDown();

                cut.WaitForAssertion(() =>
                {
                    var options = cut.FindAll(".rz-multiselect-item").Select(o => o.TextContent.Trim()).ToArray();

                    Assert.All(expected[column], option => Assert.Contains(option, options));
                    Assert.DoesNotContain("Active user", options);
                }, TimeSpan.FromSeconds(3));
            }
        }

        [Fact]
        public void EnumAsKeyValuePairPrefersTheValueHookAndFallsBackPerMember()
        {
            var items = EnumExtensions.EnumAsKeyValuePair(typeof(UserStatus), v => Equals(v, UserStatus.Active) ? "typed" : null, s => "text:" + s)
                .Cast<DropDownItem<object>>().Select(i => i.Text).ToArray();

            Assert.Equal(new[] { "typed", "text:Locked" }, items);
        }

        [Fact]
        public void PivotTextHookStillReceivesTheDisplayText()
        {
            var seen = new List<object>();
            Func<object, string> pivotHook = o => { seen.Add(o); return "x"; };

            EnumExtensions.EnumAsKeyValuePair(typeof(LeaseStatus), pivotHook).Cast<object>().ToList();

            Assert.Equal(new object[] { "Active", "Expired" }, seen);
            Assert.Equal(typeof(Func<Enum, string>), typeof(RadzenPivotDataGrid<Row>).GetProperty(nameof(RadzenPivotDataGrid<Row>.EnumTranslationFunc)).PropertyType);
        }
    }
}
