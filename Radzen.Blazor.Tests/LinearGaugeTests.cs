using Bunit;
using Radzen.Blazor.Rendering;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class LinearGaugeTests
    {
        [Fact]
        public void LinearGauge_Renders_CssClass()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenLinearGauge>(parameters =>
                parameters.Add(p => p.Style, "width:300px;height:150px"));

            Assert.Contains("rz-linear-gauge", component.Markup);
        }

        [Fact]
        public void LinearGauge_Renders_SvgWhenDimensionsSet()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenLinearGauge>(parameters =>
                parameters.Add(p => p.Style, "width:300px;height:150px"));

            Assert.Contains("<svg", component.Markup);
        }

        [Fact]
        public void LinearGauge_Renders_ScaleLine()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenLinearGauge>(parameters =>
                parameters.Add(p => p.Style, "width:300px;height:150px")
                    .AddChildContent<RadzenLinearGaugeScale>());

            Assert.Contains("rz-line", component.Markup);
        }

        [Fact]
        public void LinearGauge_Renders_Ticks()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenLinearGauge>(parameters =>
                parameters.Add(p => p.Style, "width:300px;height:150px")
                    .AddChildContent<RadzenLinearGaugeScale>());

            Assert.Contains("rz-tick", component.Markup);
            Assert.Contains("rz-tick-text", component.Markup);
        }

        [Fact]
        public void LinearGauge_Renders_TickLabels()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            // Default scale: Min=0, Max=100, Step=20
            var component = ctx.RenderComponent<RadzenLinearGauge>(parameters =>
                parameters.Add(p => p.Style, "width:300px;height:150px")
                    .AddChildContent<RadzenLinearGaugeScale>());

            Assert.Contains(">0<", component.Markup);
            Assert.Contains(">20<", component.Markup);
            Assert.Contains(">40<", component.Markup);
            Assert.Contains(">60<", component.Markup);
            Assert.Contains(">80<", component.Markup);
            Assert.Contains(">100<", component.Markup);
        }

        [Fact]
        public void LinearGauge_Renders_CustomMinMax()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenLinearGauge>(parameters =>
                parameters.Add(p => p.Style, "width:300px;height:150px")
                    .AddChildContent<RadzenLinearGaugeScale>(scale =>
                        scale.Add(p => p.Min, 0)
                            .Add(p => p.Max, 50)
                            .Add(p => p.Step, 10)));

            Assert.Contains(">0<", component.Markup);
            Assert.Contains(">10<", component.Markup);
            Assert.Contains(">50<", component.Markup);
            Assert.DoesNotContain(">100<", component.Markup);
        }

        [Fact]
        public void LinearGauge_Renders_HideTickLabels()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenLinearGauge>(parameters =>
                parameters.Add(p => p.Style, "width:300px;height:150px")
                    .AddChildContent<RadzenLinearGaugeScale>(scale =>
                        scale.Add(p => p.ShowTickLabels, false)));

            Assert.Contains("rz-tick", component.Markup);
            Assert.DoesNotContain("rz-tick-text", component.Markup);
        }

        [Fact]
        public void LinearGauge_Renders_NoTicksWhenPositionNone()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenLinearGauge>(parameters =>
                parameters.Add(p => p.Style, "width:300px;height:150px")
                    .AddChildContent<RadzenLinearGaugeScale>(scale =>
                        scale.Add(p => p.TickPosition, GaugeTickPosition.None)));

            Assert.DoesNotContain("rz-tick", component.Markup);
        }

        [Fact]
        public void LinearGauge_Renders_FormatString()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenLinearGauge>(parameters =>
                parameters.Add(p => p.Style, "width:300px;height:150px")
                    .AddChildContent<RadzenLinearGaugeScale>(scale =>
                        scale.Add(p => p.FormatString, "{0:N1}")
                            .Add(p => p.Min, 0)
                            .Add(p => p.Max, 100)
                            .Add(p => p.Step, 50)));

            Assert.Contains(">0.0<", component.Markup);
            Assert.Contains(">50.0<", component.Markup);
            Assert.Contains(">100.0<", component.Markup);
        }

        [Fact]
        public void LinearGauge_Renders_StyleParameter()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenLinearGauge>(parameters =>
                parameters.Add(p => p.Style, "width:300px;height:150px"));

            Assert.Contains(@"style=""width:300px;height:150px""", component.Markup);
        }

        [Fact]
        public void LinearGauge_Renders_VisibleFalse()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenLinearGauge>(parameters =>
                parameters.Add(p => p.Style, "width:300px;height:150px")
                    .Add(p => p.Visible, false));

            Assert.DoesNotContain("rz-linear-gauge", component.Markup);
        }

        [Fact]
        public void LinearGauge_Renders_Pointer()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenLinearGauge>(parameters =>
                parameters.Add(p => p.Style, "width:300px;height:150px")
                    .AddChildContent<RadzenLinearGaugeScale>(scale =>
                        scale.AddChildContent<RadzenLinearGaugeScalePointer>(pointer =>
                            pointer.Add(p => p.Value, 50))));

            Assert.Contains("rz-linear-gauge-pointer", component.Markup);
        }

        [Fact]
        public void LinearGauge_Renders_PointerArrow()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenLinearGauge>(parameters =>
                parameters.Add(p => p.Style, "width:300px;height:150px")
                    .AddChildContent<RadzenLinearGaugeScale>(scale =>
                        scale.AddChildContent<RadzenLinearGaugeScalePointer>(pointer =>
                            pointer.Add(p => p.Value, 50)
                                .Add(p => p.PointerType, LinearGaugePointerType.Arrow))));

            Assert.Contains("rz-linear-gauge-pointer-arrow", component.Markup);
        }

        [Fact]
        public void LinearGauge_Renders_PointerBar()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenLinearGauge>(parameters =>
                parameters.Add(p => p.Style, "width:300px;height:150px")
                    .AddChildContent<RadzenLinearGaugeScale>(scale =>
                        scale.AddChildContent<RadzenLinearGaugeScalePointer>(pointer =>
                            pointer.Add(p => p.Value, 50)
                                .Add(p => p.PointerType, LinearGaugePointerType.Bar))));

            Assert.Contains("rz-linear-gauge-pointer-bar", component.Markup);
        }

        [Fact]
        public void LinearGauge_Renders_PointerLine()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenLinearGauge>(parameters =>
                parameters.Add(p => p.Style, "width:300px;height:150px")
                    .AddChildContent<RadzenLinearGaugeScale>(scale =>
                        scale.AddChildContent<RadzenLinearGaugeScalePointer>(pointer =>
                            pointer.Add(p => p.Value, 50)
                                .Add(p => p.PointerType, LinearGaugePointerType.Line))));

            Assert.Contains("rz-linear-gauge-pointer-line", component.Markup);
        }

        [Fact]
        public void LinearGauge_Renders_DraggablePointerCursor()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenLinearGauge>(parameters =>
                parameters.Add(p => p.Style, "width:300px;height:150px")
                    .AddChildContent<RadzenLinearGaugeScale>(scale =>
                        scale.AddChildContent<RadzenLinearGaugeScalePointer>(pointer =>
                            pointer.Add(p => p.Value, 50)
                                .Add(p => p.Draggable, true))));

            Assert.Contains("rz-linear-gauge-pointer-draggable", component.Markup);
        }

        [Fact]
        public void LinearGauge_DoesNotRender_DraggableCursorWhenNotDraggable()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenLinearGauge>(parameters =>
                parameters.Add(p => p.Style, "width:300px;height:150px")
                    .AddChildContent<RadzenLinearGaugeScale>(scale =>
                        scale.AddChildContent<RadzenLinearGaugeScalePointer>(pointer =>
                            pointer.Add(p => p.Value, 50))));

            Assert.DoesNotContain("rz-linear-gauge-pointer-draggable", component.Markup);
        }

        [Fact]
        public void LinearGauge_Renders_ShowValue()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenLinearGauge>(parameters =>
                parameters.Add(p => p.Style, "width:300px;height:150px")
                    .AddChildContent<RadzenLinearGaugeScale>(scale =>
                        scale.AddChildContent<RadzenLinearGaugeScalePointer>(pointer =>
                            pointer.Add(p => p.Value, 75)
                                .Add(p => p.ShowValue, true))));

            Assert.Contains("rz-linear-gauge-value", component.Markup);
            Assert.Contains("75", component.Markup);
        }

        [Fact]
        public void LinearGauge_DoesNotRender_ValueWhenShowValueFalse()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenLinearGauge>(parameters =>
                parameters.Add(p => p.Style, "width:300px;height:150px")
                    .AddChildContent<RadzenLinearGaugeScale>(scale =>
                        scale.AddChildContent<RadzenLinearGaugeScalePointer>(pointer =>
                            pointer.Add(p => p.Value, 75))));

            // The value div is still rendered (for positioning) but should not contain the value text
            Assert.DoesNotContain(">75<", component.Markup);
        }

        [Fact]
        public void LinearGauge_Renders_ValueFormatString()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenLinearGauge>(parameters =>
                parameters.Add(p => p.Style, "width:300px;height:150px")
                    .AddChildContent<RadzenLinearGaugeScale>(scale =>
                        scale.AddChildContent<RadzenLinearGaugeScalePointer>(pointer =>
                            pointer.Add(p => p.Value, 75.5)
                                .Add(p => p.ShowValue, true)
                                .Add(p => p.FormatString, "{0:N1}"))));

            Assert.Contains("75.5", component.Markup);
        }

        [Fact]
        public void LinearGauge_Renders_Range()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenLinearGauge>(parameters =>
                parameters.Add(p => p.Style, "width:300px;height:150px")
                    .AddChildContent<RadzenLinearGaugeScale>(scale =>
                        scale.AddChildContent<RadzenLinearGaugeScaleRange>(range =>
                            range.Add(p => p.From, 0)
                                .Add(p => p.To, 50)
                                .Add(p => p.Fill, "green"))));

            Assert.Contains("rz-linear-gauge-range", component.Markup);
            Assert.Contains("green", component.Markup);
        }

        [Fact]
        public void LinearGauge_Renders_VerticalOrientation()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenLinearGauge>(parameters =>
                parameters.Add(p => p.Style, "width:150px;height:300px")
                    .AddChildContent<RadzenLinearGaugeScale>(scale =>
                        scale.Add(p => p.Orientation, Orientation.Vertical)));

            // Vertical scale renders ticks and labels
            Assert.Contains("rz-tick", component.Markup);
            Assert.Contains("rz-tick-text", component.Markup);
        }

        [Fact]
        public void LinearGauge_Renders_ClickOverlay()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            double clickedValue = -1;

            var component = ctx.RenderComponent<RadzenLinearGauge>(parameters =>
                parameters.Add(p => p.Style, "width:300px;height:150px")
                    .AddChildContent<RadzenLinearGaugeScale>(scale =>
                        scale.Add(p => p.Click, (double v) => clickedValue = v)));

            Assert.Contains("rz-linear-gauge-scale-overlay", component.Markup);
        }

        [Fact]
        public void LinearGauge_DoesNotRender_ClickOverlayWithoutHandler()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenLinearGauge>(parameters =>
                parameters.Add(p => p.Style, "width:300px;height:150px")
                    .AddChildContent<RadzenLinearGaugeScale>());

            Assert.DoesNotContain("rz-linear-gauge-scale-overlay", component.Markup);
        }

        [Fact]
        public void LinearGauge_Renders_UnmatchedParameter()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenLinearGauge>(parameters =>
                parameters.Add(p => p.Style, "width:300px;height:150px")
                    .AddUnmatched("data-testid", "my-gauge"));

            Assert.Contains(@"data-testid=""my-gauge""", component.Markup);
        }

        [Fact]
        public void LinearGauge_Renders_PointerFill()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenLinearGauge>(parameters =>
                parameters.Add(p => p.Style, "width:300px;height:150px")
                    .AddChildContent<RadzenLinearGaugeScale>(scale =>
                        scale.AddChildContent<RadzenLinearGaugeScalePointer>(pointer =>
                            pointer.Add(p => p.Value, 50)
                                .Add(p => p.Fill, "red"))));

            Assert.Contains("red", component.Markup);
        }

        [Fact]
        public void LinearGauge_Renders_ScaleStroke()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenLinearGauge>(parameters =>
                parameters.Add(p => p.Style, "width:300px;height:150px")
                    .AddChildContent<RadzenLinearGaugeScale>(scale =>
                        scale.Add(p => p.Stroke, "blue")));

            Assert.Contains("blue", component.Markup);
        }

        [Fact]
        public void LinearGauge_Renders_ValueTemplate()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenLinearGauge>(parameters =>
                parameters.Add(p => p.Style, "width:300px;height:150px")
                    .AddChildContent<RadzenLinearGaugeScale>(scale =>
                        scale.AddChildContent<RadzenLinearGaugeScalePointer>(pointer =>
                            pointer.Add(p => p.Value, 42)
                                .Add(p => p.ShowValue, true)
                                .Add<Microsoft.AspNetCore.Components.RenderFragment<RadzenLinearGaugeScalePointer>>(
                                    p => p.Template,
                                    p => builder => builder.AddContent(0, $"Custom: {p.Value}")))));

            Assert.Contains("Custom: 42", component.Markup);
        }

        [Fact]
        public void LinearGauge_Renders_RangeBorderRadius()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenLinearGauge>(parameters =>
                parameters.Add(p => p.Style, "width:300px;height:150px")
                    .AddChildContent<RadzenLinearGaugeScale>(scale =>
                        scale.AddChildContent<RadzenLinearGaugeScaleRange>(range =>
                            range.Add(p => p.From, 0)
                                .Add(p => p.To, 50)
                                .Add(p => p.Fill, "green")
                                .Add(p => p.BorderRadius, 6))));

            Assert.Contains("rx=\"6\"", component.Markup);
        }
    }
}
