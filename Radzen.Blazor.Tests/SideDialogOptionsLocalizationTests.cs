using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Globalization;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class SideDialogOptionsLocalizationTests
    {
        class Content : ComponentBase
        {
            protected override void BuildRenderTree(RenderTreeBuilder b) => b.AddContent(0, "content");
        }

        class BgLocalizer : ILocalizer
        {
            public string Get(string key, CultureInfo culture) => key switch
            {
                "Dialog_ResizeBarTitle" => "Промени големина",
                "Dialog_ResizeBarAriaLabel" => "Плъзнете за преоразмеряване",
                _ => null
            };
        }

        static (IRenderedComponent<RadzenDialog> cut, SideDialogOptions options) OpenResizableSideDialog(TestContext ctx)
        {
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.Services.AddScoped<ILocalizer, BgLocalizer>();
            ctx.Services.AddRadzenComponents();
            var cut = ctx.RenderComponent<RadzenDialog>();
            var service = ctx.Services.GetRequiredService<DialogService>();
            var options = new SideDialogOptions { Resizable = true };

            cut.InvokeAsync(() => service.OpenSide("Title", typeof(Content), new Dictionary<string, object>(), options));
            cut.WaitForAssertion(() => Assert.NotNull(cut.Find(".rz-dialog-resize-bar")));

            return (cut, options);
        }

        [Fact]
        public void SideDialog_ResizeBar_Renders_Custom_ILocalizer_Strings()
        {
            using var ctx = new TestContext();
            var (cut, _) = OpenResizableSideDialog(ctx);

            var bar = cut.Find(".rz-dialog-resize-bar");
            Assert.Equal("Промени големина", bar.GetAttribute("title"));
            Assert.Equal("Плъзнете за преоразмеряване", bar.GetAttribute("aria-label"));
        }

        [Fact]
        public void SideDialogOptions_ResizeBar_Getters_Use_Custom_ILocalizer()
        {
            using var ctx = new TestContext();
            var (_, options) = OpenResizableSideDialog(ctx);

            Assert.Equal("Промени големина", options.ResizeBarTitle);
            Assert.Equal("Плъзнете за преоразмеряване", options.ResizeBarAriaLabel);
        }

        [Fact]
        public void SideDialogOptions_ResizeBar_Getters_Fall_Back_To_Builtin_Strings_Before_Open()
        {
            var options = new SideDialogOptions();

            Assert.Equal("Drag to resize", options.ResizeBarTitle);
            Assert.Equal("Resize side dialog", options.ResizeBarAriaLabel);
        }
    }
}
