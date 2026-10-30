using Bunit;
using Microsoft.AspNetCore.Components;
using System.Collections.ObjectModel;
using System.Linq;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class DataGridGroupExpandAllTests
    {
        class Item
        {
            public string City { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
        }

        const string Button = "th.rz-col-icon button.rz-group-expand-all";

        static IRenderedComponent<RadzenDataGrid<Item>> Render(TestContext ctx, bool showGroupExpandAll = true, bool grouped = true,
            bool showGroupExpandColumn = true, bool? allGroupsExpanded = null, EventCallback<bool?> allGroupsExpandedChanged = default,
            string expandTitle = null, string collapseTitle = null, System.Action<GroupRowRenderEventArgs> groupRowRender = null)
        {
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var groups = new ObservableCollection<GroupDescriptor>();

            if (grouped)
            {
                groups.Add(new GroupDescriptor { Property = nameof(Item.City) });
            }

            return ctx.RenderComponent<RadzenDataGrid<Item>>(pb =>
            {
                pb.Add(p => p.Data, new[]
                {
                    new Item { City = "Sofia", Name = "Ivan" },
                    new Item { City = "Sofia", Name = "Maria" },
                    new Item { City = "Berlin", Name = "Anna" }
                });
                pb.Add(p => p.AllowGrouping, true);
                pb.Add(p => p.Groups, groups);
                pb.Add(p => p.ShowGroupExpandAll, showGroupExpandAll);
                pb.Add(p => p.ShowGroupExpandColumn, showGroupExpandColumn);
                pb.Add(p => p.AllGroupsExpanded, allGroupsExpanded);
                pb.Add(p => p.AllGroupsExpandedChanged, allGroupsExpandedChanged);

                if (expandTitle != null)
                {
                    pb.Add(p => p.ExpandAllGroupsTitle, expandTitle);
                }

                if (collapseTitle != null)
                {
                    pb.Add(p => p.CollapseAllGroupsTitle, collapseTitle);
                }

                if (groupRowRender != null)
                {
                    pb.Add(p => p.GroupRowRender, groupRowRender);
                }

                pb.Add(p => p.Columns, builder =>
                {
                    builder.OpenComponent<RadzenDataGridColumn<Item>>(0);
                    builder.AddAttribute(1, nameof(RadzenDataGridColumn<Item>.Property), nameof(Item.City));
                    builder.CloseComponent();

                    builder.OpenComponent<RadzenDataGridColumn<Item>>(2);
                    builder.AddAttribute(3, nameof(RadzenDataGridColumn<Item>.Property), nameof(Item.Name));
                    builder.CloseComponent();
                });
            });
        }

        static int DataRowCount(IRenderedComponent<RadzenDataGrid<Item>> component) => component.FindAll("tr.rz-data-row").Count;

        [Fact]
        public void Button_IsNotRendered_ByDefault()
        {
            using var ctx = new TestContext();
            var component = Render(ctx, showGroupExpandAll: false);

            Assert.Empty(component.FindAll(Button));
            Assert.Equal(2, component.FindAll(".rz-group-row").Count);
        }

        [Fact]
        public void Button_IsRendered_OnlyInFirstGroupHeaderCell()
        {
            using var ctx = new TestContext();
            var component = Render(ctx);

            var buttons = component.FindAll(Button);
            Assert.Single(buttons);
            Assert.Equal("Collapse all groups", buttons[0].GetAttribute("title"));
            Assert.Equal("Collapse all groups", buttons[0].GetAttribute("aria-label"));
            Assert.Equal("true", buttons[0].GetAttribute("aria-expanded"));
            Assert.Contains("rzi-chevron-circle-down", buttons[0].FirstElementChild.ClassName);
            Assert.Equal(3, DataRowCount(component));

            var headerCells = component.FindAll("thead tr:first-child th");
            Assert.Equal("rz-col-icon rz-unselectable-text", headerCells[0].ClassName);
            Assert.NotNull(headerCells[0].QuerySelector("button.rz-group-expand-all"));
        }

        [Fact]
        public void Button_IsNotRendered_WhenNotGrouped()
        {
            using var ctx = new TestContext();
            var component = Render(ctx, grouped: false);

            Assert.Empty(component.FindAll(Button));
            Assert.Empty(component.FindAll("th.rz-col-icon"));
        }

        [Fact]
        public void Button_IsNotRendered_WhenGroupExpandColumnIsHidden()
        {
            using var ctx = new TestContext();
            var component = Render(ctx, showGroupExpandColumn: false);

            Assert.Empty(component.FindAll(Button));
            Assert.Empty(component.FindAll("th.rz-col-icon"));
        }

        [Fact]
        public void Click_ExpandsAllGroups_AndReportsTrue()
        {
            using var ctx = new TestContext();
            bool? reported = null;
            var changed = EventCallback.Factory.Create<bool?>(new object(), (bool? v) => reported = v);
            var component = Render(ctx, allGroupsExpanded: false, allGroupsExpandedChanged: changed);

            Assert.Equal(0, DataRowCount(component));
            Assert.Equal("false", component.Find(Button).GetAttribute("aria-expanded"));

            component.Find(Button).Click();

            Assert.True(reported);
            Assert.Equal(3, DataRowCount(component));
            var button = component.Find(Button);
            Assert.Equal("true", button.GetAttribute("aria-expanded"));
            Assert.Equal("Collapse all groups", button.GetAttribute("title"));
            Assert.Contains("rzi-chevron-circle-down", button.FirstElementChild.ClassName);
            Assert.All(component.FindAll(".rz-group-row .rz-row-toggler"), toggler => Assert.Contains("rzi-chevron-circle-down", toggler.ClassName));
        }

        [Fact]
        public void Click_CollapsesAllGroups_WhenAllAreExpanded()
        {
            using var ctx = new TestContext();
            bool? reported = null;
            var changed = EventCallback.Factory.Create<bool?>(new object(), (bool? v) => reported = v);
            var component = Render(ctx, allGroupsExpanded: true, allGroupsExpandedChanged: changed);

            Assert.Equal(3, DataRowCount(component));

            component.Find(Button).Click();

            Assert.False(reported);
            Assert.Equal(0, DataRowCount(component));
            var button = component.Find(Button);
            Assert.Equal("false", button.GetAttribute("aria-expanded"));
            Assert.Equal("Expand all groups", button.GetAttribute("title"));
            Assert.All(component.FindAll(".rz-group-row .rz-row-toggler"), toggler => Assert.Contains("rzi-chevron-circle-right", toggler.ClassName));
        }

        [Fact]
        public void Click_ExpandsAll_WhenSomeGroupsAreCollapsed()
        {
            using var ctx = new TestContext();
            bool? reported = null;
            var changed = EventCallback.Factory.Create<bool?>(new object(), (bool? v) => reported = v);
            var component = Render(ctx, allGroupsExpanded: true, allGroupsExpandedChanged: changed);

            component.FindAll(".rz-group-row button")[0].Click();

            Assert.Null(reported);
            Assert.Equal(1, DataRowCount(component));
            Assert.Equal("false", component.Find(Button).GetAttribute("aria-expanded"));

            component.Find(Button).Click();

            Assert.True(reported);
            Assert.Equal(3, DataRowCount(component));
            Assert.Equal("true", component.Find(Button).GetAttribute("aria-expanded"));
        }

        [Fact]
        public void Click_CollapsesAll_WhenAllGroupsAreExpandedByDefault()
        {
            using var ctx = new TestContext();
            bool? reported = null;
            var changed = EventCallback.Factory.Create<bool?>(new object(), (bool? v) => reported = v);
            var component = Render(ctx, allGroupsExpandedChanged: changed);

            Assert.Equal(3, DataRowCount(component));

            component.Find(Button).Click();

            Assert.False(reported);
            Assert.Equal(0, DataRowCount(component));
            Assert.Equal("false", component.Find(Button).GetAttribute("aria-expanded"));
        }

        [Fact]
        public void Click_ExpandsAll_WhenGroupRowRenderCollapsedGroups()
        {
            using var ctx = new TestContext();
            bool? reported = null;
            var changed = EventCallback.Factory.Create<bool?>(new object(), (bool? v) => reported = v);
            var component = Render(ctx, allGroupsExpandedChanged: changed, groupRowRender: args => args.Expanded = args.FirstRender ? false : null);

            Assert.Equal(0, DataRowCount(component));

            component.Find(Button).Click();

            Assert.True(reported);
            Assert.Equal(3, DataRowCount(component));
            Assert.Equal("true", component.Find(Button).GetAttribute("aria-expanded"));
            Assert.Equal("Collapse all groups", component.Find(Button).GetAttribute("title"));
        }

        [Fact]
        public void ToggleAllGroupsExpand_IsPublicApi()
        {
            using var ctx = new TestContext();
            var component = Render(ctx, showGroupExpandAll: false);

            Assert.Equal(3, DataRowCount(component));

            component.InvokeAsync(() => component.Instance.ToggleAllGroupsExpand()).GetAwaiter().GetResult();
            component.Render();

            Assert.Equal(0, DataRowCount(component));

            component.InvokeAsync(() => component.Instance.ToggleAllGroupsExpand()).GetAwaiter().GetResult();
            component.Render();

            Assert.Equal(3, DataRowCount(component));
        }

        [Fact]
        public void Titles_CanBeOverridden()
        {
            using var ctx = new TestContext();
            var component = Render(ctx, allGroupsExpanded: false, expandTitle: "Open", collapseTitle: "Close");

            Assert.Equal("Open", component.Find(Button).GetAttribute("title"));

            component.Find(Button).Click();

            Assert.Equal("Close", component.Find(Button).GetAttribute("title"));
        }
    }
}
