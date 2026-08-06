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

            Assert.Contains(@$"style=""--rz-progressbar-value: 0%;--rz-progressbar-buffer-value: 0%;{value}""", component.Markup);
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

            component.SetParametersAndRender(parameters =>
            {
                parameters.Add<double>(p => p.Value, value);
                parameters.Add<double>(p => p.Max, max);
            });

            Assert.Contains(@$"style=""--rz-progressbar-value: {Math.Min(value / max * 100, 100).ToInvariantString()}%;--rz-progressbar-buffer-value: 0%;""", component.Markup);
        }

        [Fact]
        public void ProgressBar_Renders_ProgressBarStyle()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenProgressBar>();

            component.SetParametersAndRender(parameters => parameters.Add(p => p.ProgressBarStyle, ProgressBarStyle.Success));
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

        [Fact]
        public void ProgressBar_Renders_ShowValue_True()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenProgressBar>(parameters =>
            {
                parameters.Add(p => p.ShowValue, true);
                parameters.Add(p => p.Value, 50);
            });

            Assert.Contains("rz-progressbar-label", component.Markup);
            Assert.Contains("50%", component.Markup);
        }

        [Fact]
        public void ProgressBar_Renders_ShowValue_False()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenProgressBar>(parameters =>
            {
                parameters.Add(p => p.ShowValue, false);
                parameters.Add(p => p.Value, 50);
            });

            Assert.DoesNotContain("rz-progressbar-label", component.Markup);
        }

        [Fact]
        public void ProgressBar_Renders_Template()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenProgressBar>(parameters =>
            {
                parameters.Add(p => p.Value, 75);
                parameters.Add(p => p.Template, builder => builder.AddContent(0, "Custom: 75%"));
            });

            Assert.Contains("Custom: 75%", component.Markup);
        }

        [Fact]
        public void ProgressBar_Renders_BufferValueParameter()
        {
            using var ctx = new TestContext();
            var bufferValue = 70;

            var component = ctx.RenderComponent<RadzenProgressBar>(parameters =>
            {
                parameters.Add(p => p.BufferValue, bufferValue);
            });

            Assert.Contains(@$"--rz-progressbar-buffer-value: {bufferValue}%;", component.Markup);
            Assert.Contains(@"class=""rz-progressbar-buffer-value""", component.Markup);
        }

        [Fact]
        public void ProgressBar_Renders_WidthFromBufferValueAndMaxParameters()
        {
            using var ctx = new TestContext();

            var bufferValue = 250d;
            var max = 500d;

            var component = ctx.RenderComponent<RadzenProgressBar>(parameters =>
            {
                parameters.Add(p => p.BufferValue, bufferValue);
                parameters.Add(p => p.Max, max);
            });

            Assert.Contains(@$"--rz-progressbar-buffer-value: {Math.Min(bufferValue / max * 100, 100).ToInvariantString()}%;", component.Markup);
        }

        [Fact]
        public void ProgressBar_Renders_BufferValueWithCustomRange()
        {
            using var ctx = new TestContext();

            var bufferValue = 300d;
            var min = 100d;
            var max = 500d;

            var component = ctx.RenderComponent<RadzenProgressBar>(parameters =>
            {
                parameters.Add(p => p.BufferValue, bufferValue);
                parameters.Add(p => p.Min, min);
                parameters.Add(p => p.Max, max);
            });

            var normalizedBufferValue = Math.Min(Math.Max((bufferValue - min) / (max - min), 0), 1) * 100;

            Assert.Contains(@$"--rz-progressbar-buffer-value: {normalizedBufferValue.ToInvariantString()}%;", component.Markup);
        }

        [Fact]
        public void ProgressBar_Clamps_BufferValueAboveMax()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenProgressBar>(parameters =>
            {
                parameters.Add(p => p.BufferValue, 150);
                parameters.Add(p => p.Max, 100);
            });

            Assert.Contains(@"--rz-progressbar-buffer-value: 100%;", component.Markup);
        }

        [Fact]
        public void ProgressBar_Clamps_BufferValueBelowMin()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenProgressBar>(parameters =>
            {
                parameters.Add(p => p.BufferValue, -50);
                parameters.Add(p => p.Min, 0);
                parameters.Add(p => p.Max, 100);
            });

            Assert.Contains(@"--rz-progressbar-buffer-value: 0%;", component.Markup);
        }

        [Fact]
        public void ProgressBar_DoesNotRender_BufferInIndeterminateMode()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenProgressBar>(parameters =>
            {
                parameters.Add(p => p.BufferValue, 70);
                parameters.Add(p => p.Mode, ProgressBarMode.Indeterminate);
            });

            Assert.DoesNotContain(@"class=""rz-progressbar-buffer-value""", component.Markup);
        }

        [Fact]
        public void ProgressBar_DoesNotRenderBuffer_WhenBufferValueIsNotSet()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenProgressBar>(parameters =>
            {
                parameters.Add(p => p.Value, -10);
                parameters.Add(p => p.Min, -100);
                parameters.Add(p => p.Max, 100);
            });
            Assert.DoesNotContain(@"class=""rz-progressbar-buffer-value""", component.Markup);
        }

        [Fact]
        public void ProgressBar_Renders_BufferBehindValue()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenProgressBar>(parameters =>
            {
                parameters.Add(p => p.Value, 30);
                parameters.Add(p => p.BufferValue, 70);
            });

            Assert.Contains(@"--rz-progressbar-value: 30%;", component.Markup);
            Assert.Contains(@"--rz-progressbar-buffer-value: 70%;", component.Markup);

            var bufferIndex = component.Markup.IndexOf(@"class=""rz-progressbar-buffer-value""", StringComparison.Ordinal);
            var valueIndex = component.Markup.IndexOf(@"class=""rz-progressbar-value""", StringComparison.Ordinal);

            Assert.True(bufferIndex >= 0);
            Assert.True(valueIndex > bufferIndex);
        }

        [Fact]
        public void ProgressBar_RendersBuffer_WhenBufferIsZero()
        {
            using var ctx = new TestContext();

            var component = ctx.RenderComponent<RadzenProgressBar>(parameters =>
            {
                parameters.Add(p => p.Value, -20);
                parameters.Add(p => p.BufferValue, 0);
                parameters.Add(p => p.Min, -100);
                parameters.Add(p => p.Max, 100);
            });

            Assert.Contains(@"--rz-progressbar-buffer-value: 50%;", component.Markup);
        }
    }
}
