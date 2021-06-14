using Bunit;
using Microsoft.AspNetCore.Components;
using System.Linq;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class PanelTests
    {
        [Fact]
        public void Panel_Renders_CssClasses()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenPanel>();

            component.Render();

            Assert.Contains(@$"rz-panel", component.Markup);
            Assert.Contains(@$"rz-panel-titlebar", component.Markup);
            Assert.Contains(@$"rz-panel-content-wrapper", component.Markup);
            Assert.Contains(@$"rz-panel-content", component.Markup);
        }

        [Fact]
        public void Panel_Renders_TextParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenPanel>();

            var value = "Test";

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Text, value));

            Assert.Contains(@$"<span>{value}</span>", component.Markup);
        }

        [Fact]
        public void Panel_Renders_IconParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenPanel>();

            var value = "Test";

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Icon, value));

            Assert.Contains(@$"<i class=""rzi"">{value}</i>", component.Markup);
        }

        [Fact]
        public void Panel_Renders_ChildContentParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenPanel>();

            var value = "MyChildContent";

            component.SetParametersAndRender(parameters => parameters.Add(p => p.ChildContent, value));

            Assert.Contains(@$"{value}", component.Markup);
        }

        [Fact]
        public void Panel_Renders_HeaderTemplateParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenPanel>();

            var value = "MyHeaderTemplate";

            component.SetParametersAndRender(parameters => parameters.Add(p => p.HeaderTemplate, value));

            Assert.Contains(@$"{value}", component.Markup);
        }

        [Fact]
        public void Panel_Renders_FooterTemplateParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenPanel>();

            var value = "MyFooterTemplate";

            component.SetParametersAndRender(parameters => parameters.Add(p => p.FooterTemplate, value));

            Assert.Contains(@$"{value}", component.Markup);
        }

        [Fact]
        public void Panel_Renders_AllowCollapseParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenPanel>();

            var classes = @"class=""rz-panel-titlebar-icon rz-panel-titlebar-toggler""";

            component.Render();

            Assert.DoesNotContain(classes, component.Markup);

            component.SetParametersAndRender(parameters => parameters.Add<bool>(p => p.AllowCollapse, true));

            Assert.Contains(classes, component.Markup);
        }

        [Fact]
        public void Panel_Renders_CollapsedParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenPanel>();

            component.SetParametersAndRender(parameters => parameters.Add<bool>(p => p.AllowCollapse, true));

            Assert.Contains(@"<span class=""rzi rzi-minus""></span>", component.Markup);

            component.SetParametersAndRender(parameters => parameters.Add<bool>(p => p.Collapsed, true));

            Assert.Contains(@"<span class=""rzi rzi-plus""></span>", component.Markup);
        }

        [Fact]
        public void Panel_Renders_StyleParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenPanel>();

            var value = "width:20px";

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Style, value));

            Assert.Contains(@$"style=""{value}""", component.Markup);
        }

        [Fact]
        public void Panel_Renders_UnmatchedParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenPanel>();

            component.SetParametersAndRender(parameters => parameters.AddUnmatched("autofocus", ""));

            Assert.Contains(@$"autofocus", component.Markup);
        }

        [Fact]
        public void Panel_Raises_ExpandAndCollapseEvents()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenPanel>();

            var raised = false;

            component.SetParametersAndRender(parameters =>
            {
                parameters.Add<bool>(p => p.AllowCollapse, true);
                parameters.Add(p => p.Collapse, args => { raised = true; });
            });

            component.Find("a").Click();

            Assert.True(raised);

            raised = false;

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Expand, args => { raised = true; }));

            component.Find("a").Click();
        }

        [Fact]
        public void Panel_Renders_SummaryWhenCollapsed()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenPanel>();

            component.SetParametersAndRender(parameters =>
            {
                parameters.Add<bool>(p => p.AllowCollapse, true);
                parameters.Add<bool>(p => p.Collapsed, true);
                parameters.Add<RenderFragment>(p => p.SummaryTemplate, builder =>
                {
                    builder.OpenElement(0, "p");
                    builder.AddContent(0, "SummaryContent");
                    builder.CloseElement();
                });

            });

            Assert.Contains("SummaryContent", component.Markup);
            Assert.Equal(
                "display: block",
                component.Find(".rz-panel-content-summary").ParentElement.Attributes.First(attr => attr.Name == "style").Value
            );
        }

        [Fact]
        public void Panel_DontRenders_SummaryWhenOpen()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenPanel>();

            component.SetParametersAndRender(parameters =>
            {
                parameters.Add<bool>(p => p.AllowCollapse, true);
                parameters.Add<bool>(p => p.Collapsed, false);
                parameters.Add<RenderFragment>(p => p.SummaryTemplate, builder =>
                {
                    builder.OpenElement(0, "p");
                    builder.AddContent(0, "SummaryContent");
                    builder.CloseElement();
                });

            });

            Assert.Contains("SummaryContent", component.Markup);
            Assert.Equal(
                "display: none",
                component.Find(".rz-panel-content-summary").ParentElement.Attributes.First(attr => attr.Name == "style").Value
            );
        }
    }
}
