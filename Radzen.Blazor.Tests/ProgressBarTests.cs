using Bunit;
using Radzen.Blazor.Rendering;
using System;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class ProgressBarTests
    {
        [Fact]
        public void ProgressBar_Renders_ValueParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenProgressBar>();

            var value = 4.5;

            component.SetParametersAndRender(parameters => parameters.Add<double>(p => p.Value, value));


            Assert.Contains(@$"<div class=""rz-progressbar-label"">", component.Markup);
            Assert.Contains(@$"{value}%", component.Markup);
            Assert.Contains(@$"aria-valuenow=""{value}""", component.Markup);
            Assert.Contains(@$"aria-valuemin=""0""", component.Markup);
        }

        [Fact]
        public void ProgressBar_Renders_MaxParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenProgressBar>();

            component.Render();

            Assert.Contains(@$"aria-valuemax=""{100}""", component.Markup);

            var value = 500;

            component.SetParametersAndRender(parameters => parameters.Add<double>(p => p.Max, value));

            Assert.Contains(@$"aria-valuemax=""{value}""", component.Markup);
        }

        [Fact]
        public void ProgressBar_Renders_UnitParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenProgressBar>();

            component.Render();

            Assert.Contains(@$"0%", component.Markup);

            var unit = "mm";

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Unit, unit));

            Assert.Contains(@$"0{unit}", component.Markup);
        }

        [Fact]
        public void ProgressBar_Renders_StyleParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenProgressBar>();

            var value = "width:20px";

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Style, value));

            Assert.Contains(@$"style=""{value}""", component.Markup);
        }

        [Fact]
        public void ProgressBar_Renders_ModeParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenProgressBar>();

            component.Render();

            Assert.Contains(@$"rz-progressbar-determinate", component.Markup);

            component.SetParametersAndRender(parameters => parameters.Add<ProgressBarMode>(p => p.Mode, ProgressBarMode.Indeterminate));

            Assert.Contains(@$"rz-progressbar-indeterminate", component.Markup);
        }

        [Fact]
        public void ProgressBar_Renders_UnmatchedParameter()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenProgressBar>();

            component.SetParametersAndRender(parameters => parameters.AddUnmatched("autofocus", ""));

            Assert.Contains(@$"autofocus", component.Markup);
        }

        [Fact]
        public void ProgressBar_Renders_WidthFromValueAndMaxParameters()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenProgressBar>();

            var value = 4.5;
            var max = 500;

            component.SetParametersAndRender(parameters => { 
                parameters.Add<double>(p => p.Value, value);
                parameters.Add<double>(p => p.Max, max);
            });

            Assert.Contains(@$"style=""width: {Math.Min(value / max * 100, 100).ToInvariantString()}%;""", component.Markup);
        }

        [Fact]
        public void ProgressBar_Renders_ProgressBarStyle()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenProgressBar>();

            component.SetParametersAndRender(parameters=>parameters.Add(p=>p.ProgressBarStyle, ProgressBarStyle.Success));
            Assert.Contains(@$"rz-progressbar-success", component.Markup);

            component.SetParametersAndRender(parameters => parameters.Add(p => p.ProgressBarStyle, ProgressBarStyle.Info));
            Assert.Contains(@$"rz-progressbar-info", component.Markup);

            component.SetParametersAndRender(parameters => parameters.Add(p => p.ProgressBarStyle, ProgressBarStyle.Success));
            component.SetParametersAndRender(parameters => parameters.Add(p => p.Mode, ProgressBarMode.Indeterminate));
            Assert.Contains(@$"rz-progressbar-success", component.Markup);

            component.SetParametersAndRender(parameters => parameters.Add(p => p.ProgressBarStyle, ProgressBarStyle.Info));
            component.SetParametersAndRender(parameters => parameters.Add(p => p.Mode, ProgressBarMode.Indeterminate));
            Assert.Contains(@$"rz-progressbar-info", component.Markup);
        }
    }
}
