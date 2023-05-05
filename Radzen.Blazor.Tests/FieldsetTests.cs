using Bunit;
using Microsoft.AspNetCore.Components;
using System.Linq;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class FieldsetTests
    {
        [Fact]
        public void Fieldset_Renders_CssClasses()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenFieldset>();

            component.Render();

            Assert.Contains(@$"rz-fieldset", component.Markup);
            Assert.Contains(@$"rz-fieldset-content-wrapper", component.Markup);
            Assert.Contains(@$"rz-fieldset-content", component.Markup);
        }

        [Fact]
        public void Fieldset_Renders_TextParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenFieldset>();

            var value = "Test";

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Text, value));

            Assert.Contains(@$"<span class=""rz-fieldset-legend-text"">{value}</span>", component.Markup);
        }

        [Fact]
        public void Fieldset_Renders_IconParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenFieldset>();

            var value = "Test";

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Icon, value));

            Assert.Contains(@$"<i class=""rzi"">{value}</i>", component.Markup);
        }

        [Fact]
        public void Fieldset_Renders_ChildContentParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenFieldset>();

            var value = "MyChildContent";

            component.SetParametersAndRender(parameters => parameters.Add(p => p.ChildContent, value));

            Assert.Contains(@$"{value}", component.Markup);
        }

        [Fact]
        public void Fieldset_Renders_HeaderTemplateParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenFieldset>();

            var value = "MyHeaderTemplate";

            component.SetParametersAndRender(parameters => parameters.Add(p => p.HeaderTemplate, value));

            Assert.Contains(@$"{value}", component.Markup);
        }

        [Fact]
        public void Fieldset_Renders_AllowCollapseParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenFieldset>();

            var classes = @"rz-fieldset-toggler";

            component.Render();

            Assert.DoesNotContain(classes, component.Markup);

            component.SetParametersAndRender(parameters => parameters.Add<bool>(p => p.AllowCollapse, true));

            Assert.Contains(classes, component.Markup);
        }

        [Fact]
        public void Fieldset_Renders_CollapsedParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenFieldset>();

            component.SetParametersAndRender(parameters => parameters.Add<bool>(p => p.AllowCollapse, true));

            Assert.Contains(@"<span class=""rz-fieldset-toggler rzi rzi-w rzi-minus""></span>", component.Markup);

            component.SetParametersAndRender(parameters => parameters.Add<bool>(p => p.Collapsed, true));

            Assert.Contains(@"<span class=""rz-fieldset-toggler rzi rzi-w rzi-plus""></span>", component.Markup);
        }

        [Fact]
        public void Fieldset_Renders_StyleParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenFieldset>();

            var value = "width:20px";

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Style, value));

            Assert.Contains(@$"style=""{value}""", component.Markup);
        }

        [Fact]
        public void Fieldset_Renders_UnmatchedParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenFieldset>();

            component.SetParametersAndRender(parameters => parameters.AddUnmatched("autofocus", ""));

            Assert.Contains(@$"autofocus", component.Markup);
        }

        [Fact]
        public void Fieldset_Raises_ExpandAndCollapseEvents()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenFieldset>();

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
        public void Fieldset_Renders_SummaryWhenCollapsed()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenFieldset>();

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
                "",
                component.Find(".rz-fieldset-content-summary").ParentElement.Attributes.First(attr => attr.Name == "style").Value
            );
        }

        [Fact]
        public void Fieldset_DontRenders_SummaryWhenOpen()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenFieldset>();

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
                component.Find(".rz-fieldset-content-summary").ParentElement.Attributes.First(attr => attr.Name == "style").Value
            );
        }
    }
}
