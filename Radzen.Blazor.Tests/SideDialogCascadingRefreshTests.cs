using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class SideDialogCascadingRefreshTests
    {
        class Capture : ComponentBase
        {
            public static Dialog Last;
            [CascadingParameter] public Dialog Dialog { get; set; }
            protected override void OnParametersSet() => Last = Dialog;
            protected override void BuildRenderTree(RenderTreeBuilder b) => b.AddContent(0, "content");
        }

        [Fact]
        public void SideDialog_Title_Updates_When_Cascaded_Dialog_Title_Changes()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.Services.AddScoped<DialogService>();
            var cut = ctx.RenderComponent<RadzenDialog>();
            var service = ctx.Services.GetRequiredService<DialogService>();

            cut.InvokeAsync(() => service.OpenSide("Initial", typeof(Capture), new Dictionary<string, object>(), new SideDialogOptions()));
            cut.WaitForAssertion(() => Assert.Contains("Initial", cut.Find(".rz-dialog-side-title").TextContent));

            cut.InvokeAsync(() => Capture.Last.Title = "Updated");

            cut.WaitForAssertion(() => Assert.Contains("Updated", cut.Find(".rz-dialog-side-title").TextContent));
        }

        [Fact]
        public void SideDialog_Refreshes_When_Cascaded_Dialog_Options_Change()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.Services.AddScoped<DialogService>();
            var cut = ctx.RenderComponent<RadzenDialog>();
            var service = ctx.Services.GetRequiredService<DialogService>();

            cut.InvokeAsync(() => service.OpenSide("Initial", typeof(Capture), new Dictionary<string, object>(), new SideDialogOptions { ShowClose = true }));
            cut.WaitForAssertion(() => Assert.NotNull(cut.Find(".rz-dialog-side-titlebar-close")));

            cut.InvokeAsync(() => Capture.Last.Options.ShowClose = false);

            cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".rz-dialog-side-titlebar-close")));
        }

        [Fact]
        public void SideDialog_TitleContent_ReRenders_On_Service_Refresh()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.Services.AddScoped<DialogService>();
            var cut = ctx.RenderComponent<RadzenDialog>();
            var service = ctx.Services.GetRequiredService<DialogService>();
            var counter = 0;

            cut.InvokeAsync(() => service.OpenSide("Initial", typeof(Capture), new Dictionary<string, object>(), new SideDialogOptions()));
            cut.WaitForAssertion(() => Assert.NotNull(Capture.Last));

            cut.InvokeAsync(() => Capture.Last.Options.TitleContent = _ => b => b.AddContent(0, $"Counter {counter}"));
            cut.WaitForAssertion(() => Assert.Contains("Counter 0", cut.Find(".rz-dialog-side-title").TextContent));

            counter++;
            cut.InvokeAsync(() => service.Refresh());

            cut.WaitForAssertion(() => Assert.Contains("Counter 1", cut.Find(".rz-dialog-side-title").TextContent));
        }
    }
}
