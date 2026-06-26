using Bunit;
using Radzen.Blazor.Rendering;
using System;
using System.Collections;
using System.Collections.Generic;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class SliderTests
    {
        [Fact]
        public void Slider_Renders_CssClass()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenSlider<int>>();

            Assert.Contains(@$"rz-slider", component.Markup);
            Assert.Contains(@$"rz-slider-horizontal", component.Markup);
            Assert.Contains(@$"rz-slider-handle", component.Markup);
            Assert.Contains(@$"rz-slider-range rz-slider-range-min", component.Markup);
        }

        [Fact]
        public void Slider_Renders_ValueParameter()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenSlider<int>>();

            decimal max = 100;
            var value = 4;

            component.SetParametersAndRender(parameters => parameters.Add<int>(p => p.Value, value));

            Assert.Contains(@$"style=""width: {Math.Round((value / max * 100)).ToInvariantString()}%;""", component.Markup);
            Assert.Contains(@$"style=""inset-inline-start: {Math.Round((value / max * 100)).ToInvariantString()}%;""", component.Markup);
        }

        [Fact]
        public void Slider_Renders_ValueParameterWithRange()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenSlider<IEnumerable<int>>>(parameterBuilder => parameterBuilder.Add<bool>(p => p.Range, true));

            component.SetParametersAndRender(parameters => {
                parameters.Add<IEnumerable<int>>(p => p.Value, new int[] { 4, 30 });
            });

            Assert.Contains(@$"inset-inline-start: 4%", component.Markup);
            Assert.Contains(@$"inset-inline-start: 30%", component.Markup);
            Assert.Contains(@$"inset-inline-start: 4%; width: 26%;", component.Markup);
        }

        [Fact]
        public void Slider_Renders_StyleParameter()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenSlider<int>>();

            var value = "width:20px";

            component.SetParametersAndRender(parameters => parameters.Add(p => p.Style, value));

            Assert.Contains(@$"style=""{value}""", component.Markup);
        }

        [Fact]
        public void Slider_Renders_UnmatchedParameter()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenSlider<int>>();

            component.SetParametersAndRender(parameters => parameters.AddUnmatched("autofocus", ""));

            Assert.Contains(@$"autofocus", component.Markup);
        }

        [Fact]
        public void Slider_Renders_Orientation_Vertical()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenSlider<int>>(parameters =>
            {
                parameters.Add(p => p.Orientation, Orientation.Vertical);
            });

            Assert.Contains("rz-slider-vertical", component.Markup);
        }

        [Fact]
        public void Slider_Renders_Disabled()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenSlider<int>>(parameters =>
            {
                parameters.Add(p => p.Disabled, true);
            });

            Assert.Contains("rz-state-disabled", component.Markup);
        }

        [Fact]
        public void Slider_Renders_SliderHandle()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenSlider<int>>();

            Assert.Contains("rz-slider-handle", component.Markup);
        }

        [Fact]
        public void Slider_Renders_SliderRange()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenSlider<int>>();

            Assert.Contains("rz-slider-range", component.Markup);
        }

        [Fact]
        public void Slider_Renders_TabIndex()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenSlider<int>>();

            Assert.Contains("tabindex=\"0\"", component.Markup);
        }

        [Fact]
        public void Slider_Renders_SliderRole_And_AriaValues()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenSlider<int>>(parameters =>
            {
                parameters.Add(p => p.Min, 0);
                parameters.Add(p => p.Max, 100);
                parameters.Add(p => p.Value, 42);
            });

            var handle = component.Find(".rz-slider-handle");

            Assert.Equal("slider", handle.GetAttribute("role"));
            Assert.Equal("42", handle.GetAttribute("aria-valuenow"));
            Assert.Equal("0", handle.GetAttribute("aria-valuemin"));
            Assert.Equal("100", handle.GetAttribute("aria-valuemax"));
            Assert.Equal("horizontal", handle.GetAttribute("aria-orientation"));
            Assert.Equal("Value", handle.GetAttribute("aria-label"));
        }

        [Fact]
        public void Slider_Renders_AriaOrientation_Vertical()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenSlider<int>>(parameters =>
            {
                parameters.Add(p => p.Orientation, Orientation.Vertical);
            });

            var handle = component.Find(".rz-slider-handle");

            Assert.Equal("vertical", handle.GetAttribute("aria-orientation"));
        }

        [Fact]
        public void Slider_Renders_AriaDisabled()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenSlider<int>>(parameters =>
            {
                parameters.Add(p => p.Disabled, true);
            });

            var handle = component.Find(".rz-slider-handle");

            Assert.Equal("true", handle.GetAttribute("aria-disabled"));
        }

        [Fact]
        public void Slider_Renders_CustomHandleLabel()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenSlider<int>>(parameters =>
            {
                parameters.Add(p => p.HandleLabel, "Volume");
            });

            var handle = component.Find(".rz-slider-handle");

            Assert.Equal("Volume", handle.GetAttribute("aria-label"));
        }

        [Fact]
        public void Slider_Renders_SliderRole_Range_BothHandles()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenSlider<IEnumerable<int>>>(parameters =>
            {
                parameters.Add(p => p.Range, true);
                parameters.Add(p => p.Min, 0);
                parameters.Add(p => p.Max, 100);
                parameters.Add(p => p.Value, new int[] { 20, 80 });
            });

            var handles = component.FindAll(".rz-slider-handle");

            Assert.Equal(2, handles.Count);

            var minHandle = handles[0];
            var maxHandle = handles[1];

            Assert.Equal("slider", minHandle.GetAttribute("role"));
            Assert.Equal("slider", maxHandle.GetAttribute("role"));

            Assert.Equal("20", minHandle.GetAttribute("aria-valuenow"));
            Assert.Equal("0", minHandle.GetAttribute("aria-valuemin"));
            Assert.Equal("80", minHandle.GetAttribute("aria-valuemax"));
            Assert.Equal("Minimum", minHandle.GetAttribute("aria-label"));

            Assert.Equal("80", maxHandle.GetAttribute("aria-valuenow"));
            Assert.Equal("20", maxHandle.GetAttribute("aria-valuemin"));
            Assert.Equal("100", maxHandle.GetAttribute("aria-valuemax"));
            Assert.Equal("Maximum", maxHandle.GetAttribute("aria-label"));
        }

        [Fact]
        public void Slider_Home_Sets_Value_To_Min()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenSlider<int>>(parameters =>
            {
                parameters.Add(p => p.Min, 0);
                parameters.Add(p => p.Max, 100);
                parameters.Add(p => p.Value, 50);
            });

            component.Find(".rz-slider-handle").KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Code = "Home" });

            Assert.Equal(0, component.Instance.Value);
        }

        [Fact]
        public void Slider_End_Sets_Value_To_Max()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenSlider<int>>(parameters =>
            {
                parameters.Add(p => p.Min, 0);
                parameters.Add(p => p.Max, 100);
                parameters.Add(p => p.Value, 50);
            });

            component.Find(".rz-slider-handle").KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Code = "End" });

            Assert.Equal(100, component.Instance.Value);
        }

        [Fact]
        public void Slider_Arrow_Steps_Value()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");

            var component = ctx.RenderComponent<RadzenSlider<int>>(parameters =>
            {
                parameters.Add(p => p.Min, 0);
                parameters.Add(p => p.Max, 100);
                parameters.Add(p => p.Step, "1");
                parameters.Add(p => p.Value, 50);
            });

            component.Find(".rz-slider-handle").KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Code = "ArrowRight" });
            Assert.Equal(51, component.Instance.Value);

            component.Find(".rz-slider-handle").KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Code = "ArrowLeft" });
            Assert.Equal(50, component.Instance.Value);
        }
    }
}
