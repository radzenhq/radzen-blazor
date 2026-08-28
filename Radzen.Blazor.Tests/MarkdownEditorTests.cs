using System.Linq;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class MarkdownEditorTests
    {
        static TestContext CreateContext()
        {
            var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.Services.AddScoped<DialogService>();
            return ctx;
        }

        [Fact]
        public void MarkdownEditor_Renders_WithClassName()
        {
            using var ctx = CreateContext();
            var component = ctx.RenderComponent<RadzenMarkdownEditor>();

            Assert.Contains("rz-markdown-editor", component.Markup);
            Assert.Contains("rz-markdown-editor-toolbar", component.Markup);
            Assert.Contains("rz-markdown-editor-textarea", component.Markup);
        }

        [Fact]
        public void MarkdownEditor_HidesToolbar_WhenShowToolbarFalse()
        {
            using var ctx = CreateContext();
            var component = ctx.RenderComponent<RadzenMarkdownEditor>(p => p.Add(x => x.ShowToolbar, false));

            Assert.DoesNotContain("rz-markdown-editor-toolbar", component.Markup);
        }

        [Fact]
        public void MarkdownEditor_EditMode_RendersNoPreview()
        {
            using var ctx = CreateContext();
            var component = ctx.RenderComponent<RadzenMarkdownEditor>(p => p.Add(x => x.Value, "# Hi"));

            Assert.DoesNotContain("rz-markdown-editor-preview", component.Markup);
            Assert.DoesNotContain("hidden", component.Find("textarea").OuterHtml);
        }

        [Fact]
        public void MarkdownEditor_PreviewMode_HidesTextarea_AndRendersMarkdown()
        {
            using var ctx = CreateContext();
            var component = ctx.RenderComponent<RadzenMarkdownEditor>(p => p
                .Add(x => x.Value, "# Hi")
                .Add(x => x.Mode, MarkdownEditorMode.Preview));

            Assert.Contains("rz-markdown-editor-preview", component.Markup);
            Assert.Contains("<h1", component.Markup);
            Assert.True(component.Find("textarea").HasAttribute("hidden"));
        }

        [Fact]
        public void MarkdownEditor_SplitMode_RendersBoth()
        {
            using var ctx = CreateContext();
            var component = ctx.RenderComponent<RadzenMarkdownEditor>(p => p
                .Add(x => x.Value, "# Hi")
                .Add(x => x.Mode, MarkdownEditorMode.Split));

            Assert.Contains("rz-markdown-editor-preview", component.Markup);
            Assert.False(component.Find("textarea").HasAttribute("hidden"));
        }

        [Fact]
        public void MarkdownEditor_PreviewMode_ShowsPlaceholder_WhenEmpty()
        {
            using var ctx = CreateContext();
            var component = ctx.RenderComponent<RadzenMarkdownEditor>(p => p.Add(x => x.Mode, MarkdownEditorMode.Preview));

            Assert.Contains("Nothing to preview", component.Markup);
        }

        [Fact]
        public void MarkdownEditor_ModeSwitcher_RaisesModeChanged()
        {
            using var ctx = CreateContext();
            MarkdownEditorMode? changed = null;
            var component = ctx.RenderComponent<RadzenMarkdownEditor>(p => p
                .Add(x => x.ModeChanged, m => changed = m));

            component.Find(".rz-markdown-editor-modes button:nth-child(2)").Click();

            Assert.Equal(MarkdownEditorMode.Preview, changed);
        }

        [Fact]
        public void MarkdownEditor_Input_UpdatesValue_AndRaisesValueChanged()
        {
            using var ctx = CreateContext();
            string? value = null;
            var component = ctx.RenderComponent<RadzenMarkdownEditor>(p => p
                .Add(x => x.ValueChanged, v => value = v));

            component.Find("textarea").Input("typed");

            Assert.Equal("typed", value);
        }

        [Fact]
        public void MarkdownEditor_Input_RaisesInput_OnlyWhenImmediate()
        {
            using var ctx = CreateContext();
            var count = 0;
            var component = ctx.RenderComponent<RadzenMarkdownEditor>(p => p.Add(x => x.Input, _ => count++));
            component.Find("textarea").Input("a");
            Assert.Equal(0, count);

            component.SetParametersAndRender(p => p.Add(x => x.Immediate, true));
            component.Find("textarea").Input("b");
            Assert.Equal(1, count);
        }

        [Fact]
        public async System.Threading.Tasks.Task MarkdownEditor_ExecuteCommand_AppliesFormatterResult_AndRaisesExecute()
        {
            using var ctx = CreateContext();
            ctx.JSInterop.Setup<int[]?>("Radzen.getSelectionRange", _ => true).SetResult(new[] { 0, 5 });
            var apply = ctx.JSInterop.SetupVoid("Radzen.markdownEditorApply", _ => true);
            apply.SetVoidResult();
            string? executed = null;

            var component = ctx.RenderComponent<RadzenMarkdownEditor>(p => p
                .Add(x => x.Value, "hello")
                .Add(x => x.Execute, args => executed = args.CommandName));

            await component.InvokeAsync(() => component.Instance.ExecuteCommandAsync(MarkdownEditorCommands.Bold));

            var invocation = Assert.Single(apply.Invocations);
            Assert.Equal(new object?[] { 0, 5, "**hello**", 2, 7 }, invocation.Arguments.Skip(1).ToArray());
            Assert.Equal("bold", executed);
        }

        [Fact]
        public async System.Threading.Tasks.Task MarkdownEditor_ExecuteCommand_UnknownCommand_RaisesExecuteWithoutApplying()
        {
            using var ctx = CreateContext();
            var apply = ctx.JSInterop.SetupVoid("Radzen.markdownEditorApply", _ => true);
            apply.SetVoidResult();
            string? executed = null;

            var component = ctx.RenderComponent<RadzenMarkdownEditor>(p => p
                .Add(x => x.Execute, args => executed = args.CommandName));

            await component.InvokeAsync(() => component.Instance.ExecuteCommandAsync("insertToday"));

            Assert.Empty(apply.Invocations);
            Assert.Equal("insertToday", executed);
        }

        [Fact]
        public void MarkdownEditor_RendersDefaultToolbar_WhenNoChildContent()
        {
            using var ctx = CreateContext();
            var component = ctx.RenderComponent<RadzenMarkdownEditor>();

            var icons = component.FindAll(".rz-markdown-editor-tools .rzi").Select(i => i.TextContent).ToList();
            Assert.Equal(new[] { "format_bold", "format_italic", "title", "format_quote", "code", "code_blocks",
                                 "format_list_bulleted", "format_list_numbered", "link", "image", "horizontal_rule" }, icons);
        }

        [Fact]
        public void MarkdownEditor_RendersOnlyChildContent_WhenProvided()
        {
            using var ctx = CreateContext();
            var component = ctx.RenderComponent<RadzenMarkdownEditor>(p => p.AddChildContent<RadzenMarkdownEditorBold>());

            var icons = component.FindAll(".rz-markdown-editor-tools .rzi").Select(i => i.TextContent).ToList();
            Assert.Equal(new[] { "format_bold" }, icons);
        }

        [Fact]
        public void MarkdownEditor_ToolClick_ExecutesCommand()
        {
            using var ctx = CreateContext();
            ctx.JSInterop.Setup<int[]?>("Radzen.getSelectionRange", _ => true).SetResult(new[] { 0, 2 });
            var apply = ctx.JSInterop.SetupVoid("Radzen.markdownEditorApply", _ => true);
            apply.SetVoidResult();

            var component = ctx.RenderComponent<RadzenMarkdownEditor>(p => p
                .Add(x => x.Value, "hi")
                .AddChildContent<RadzenMarkdownEditorItalic>());

            component.Find(".rz-markdown-editor-tools button").Click();

            var invocation = Assert.Single(apply.Invocations);
            Assert.Equal("_hi_", invocation.Arguments[3]);
        }

        [Fact]
        public void MarkdownEditor_Tools_AreDisabled_InPreviewMode()
        {
            using var ctx = CreateContext();
            var component = ctx.RenderComponent<RadzenMarkdownEditor>(p => p.Add(x => x.Mode, MarkdownEditorMode.Preview));

            Assert.All(component.FindAll(".rz-markdown-editor-tools button"), b => Assert.True(b.HasAttribute("disabled")));
        }

        [Fact]
        public void MarkdownEditor_Disabled_DisablesTextarea_AndAllTools()
        {
            using var ctx = CreateContext();
            var component = ctx.RenderComponent<RadzenMarkdownEditor>(p => p.Add(x => x.Disabled, true));

            Assert.True(component.Find("textarea").HasAttribute("disabled"));
            Assert.All(component.FindAll(".rz-markdown-editor-tools button"), b => Assert.True(b.HasAttribute("disabled")));
        }

        [Fact]
        public void MarkdownEditor_Visible_False_DoesNotRender_OrCreateJsRef_ThenTrue_CreatesOnce()
        {
            using var ctx = CreateContext();
            var component = ctx.RenderComponent<RadzenMarkdownEditor>(p => p.Add(x => x.Visible, false));

            Assert.DoesNotContain("rz-markdown-editor", component.Markup);
            ctx.JSInterop.VerifyNotInvoke("Radzen.createMarkdownEditor");

            component.SetParametersAndRender(p => p.Add(x => x.Visible, true));

            ctx.JSInterop.VerifyInvoke("Radzen.createMarkdownEditor");
        }

        [Fact]
        public void MarkdownEditor_SplitMode_UpdatesPreview_OnInput()
        {
            using var ctx = CreateContext();
            var component = ctx.RenderComponent<RadzenMarkdownEditor>(p => p
                .Add(x => x.Mode, MarkdownEditorMode.Split)
                .Add(x => x.Value, "a"));

            component.Find("textarea").Input("# Hi");

            Assert.Contains("<h1", component.Markup);
        }

        [Fact]
        public void MarkdownEditor_ModeSwitcher_UpdatesInternalMode_WhenOneWayBound()
        {
            using var ctx = CreateContext();
            var component = ctx.RenderComponent<RadzenMarkdownEditor>(p => p
                .Add(x => x.Value, "# Hi")
                .Add(x => x.Mode, MarkdownEditorMode.Edit));

            // Mode is one-way bound (no ModeChanged): the Mode parameter never changes, but the internal
            // mode field does, and rendering must follow the field, not the unchanged parameter.
            component.Find(".rz-markdown-editor-modes button:nth-child(2)").Click();

            Assert.Contains("rz-markdown-editor-preview", component.Markup);
            Assert.Contains("<h1", component.Markup);
        }

        [Fact]
        public async System.Threading.Tasks.Task MarkdownEditor_ExecuteShortcutAsync_AppliesRegisteredShortcut_AndIgnoresUnknownKey()
        {
            using var ctx = CreateContext();
            ctx.JSInterop.Setup<int[]?>("Radzen.getSelectionRange", _ => true).SetResult(new[] { 0, 2 });
            var apply = ctx.JSInterop.SetupVoid("Radzen.markdownEditorApply", _ => true);
            apply.SetVoidResult();

            var component = ctx.RenderComponent<RadzenMarkdownEditor>(p => p
                .Add(x => x.Value, "hi")
                .AddChildContent<RadzenMarkdownEditorBold>());

            await component.InvokeAsync(() => component.Instance.ExecuteShortcutAsync("Ctrl+B"));

            var invocation = Assert.Single(apply.Invocations);
            Assert.Equal("**hi**", invocation.Arguments[3]);

            await component.InvokeAsync(() => component.Instance.ExecuteShortcutAsync("Ctrl+Z"));

            Assert.Single(apply.Invocations);
        }

        [Fact]
        public async System.Threading.Tasks.Task MarkdownEditor_ExecuteCommand_NormalizesCrlfSelectionOffsets()
        {
            using var ctx = CreateContext();
            ctx.JSInterop.Setup<int[]?>("Radzen.getSelectionRange", _ => true).SetResult(new[] { 6, 11 });
            var apply = ctx.JSInterop.SetupVoid("Radzen.markdownEditorApply", _ => true);
            apply.SetVoidResult();

            var component = ctx.RenderComponent<RadzenMarkdownEditor>(p => p.Add(x => x.Value, "line1\r\nline2"));

            await component.InvokeAsync(() => component.Instance.ExecuteCommandAsync(MarkdownEditorCommands.Bold));

            var invocation = Assert.Single(apply.Invocations);
            Assert.Equal(new object?[] { 6, 11, "**line2**", 8, 13 }, invocation.Arguments.Skip(1).ToArray());
        }

        [Fact]
        public void MarkdownEditor_Tools_RegisterShortcuts()
        {
            using var ctx = CreateContext();
            ctx.RenderComponent<RadzenMarkdownEditor>();

            // Loose mode records every call; no Setup so the un-awaited OnAfterRenderAsync cannot hang on a missing result.
            var invocation = Assert.Single(ctx.JSInterop.Invocations["Radzen.createMarkdownEditor"]);
            var keys = Assert.IsAssignableFrom<System.Collections.Generic.IEnumerable<string>>(invocation.Arguments[2]);
            Assert.Equal(new[] { "Ctrl+B", "Ctrl+I", "Ctrl+K" }, keys.OrderBy(k => k));
        }

        [Fact]
        public void CustomTool_Click_RaisesExecute_WithCommandName()
        {
            using var ctx = CreateContext();
            var apply = ctx.JSInterop.SetupVoid("Radzen.markdownEditorApply", _ => true);
            apply.SetVoidResult();
            string? executed = null;

            var component = ctx.RenderComponent<RadzenMarkdownEditor>(p => p
                .Add(x => x.Execute, args => executed = args.CommandName)
                .AddChildContent<RadzenMarkdownEditorCustomTool>(t => t
                    .Add(x => x.CommandName, "InsertToday")
                    .Add(x => x.Icon, "today")));

            component.Find(".rz-markdown-editor-tools button").Click();

            Assert.Equal("InsertToday", executed);
            Assert.Empty(apply.Invocations);
        }

        [Fact]
        public void CustomTool_RendersTemplate_WithEditor()
        {
            using var ctx = CreateContext();

            var component = ctx.RenderComponent<RadzenMarkdownEditor>(p => p
                .AddChildContent<RadzenMarkdownEditorCustomTool>(t => t
                    .Add(x => x.Template, editor => b => b.AddContent(0, $"mode:{editor.Mode}"))));

            Assert.Contains("rz-markdown-editor-custom-tool", component.Markup);
            Assert.Contains("mode:Edit", component.Markup);
        }

        [Theory]
        [InlineData(MarkdownEditorMode.Edit, false)]
        [InlineData(MarkdownEditorMode.Preview, false)]
        [InlineData(MarkdownEditorMode.Split, true)]
        public void MarkdownEditor_RendersSplitterBar_OnlyInSplitMode(MarkdownEditorMode mode, bool expected)
        {
            using var ctx = CreateContext();
            var component = ctx.RenderComponent<RadzenMarkdownEditor>(p => p.Add(x => x.Mode, mode));

            Assert.Equal(expected, component.FindAll(".rz-splitter-bar").Count > 0);
        }

        [Fact]
        public void MarkdownEditor_PreviewMode_HidesTextareaPane_ButKeepsTextareaInDom()
        {
            using var ctx = CreateContext();
            var component = ctx.RenderComponent<RadzenMarkdownEditor>(p => p.Add(x => x.Mode, MarkdownEditorMode.Preview));

            Assert.NotNull(component.Find("textarea"));
            Assert.Single(component.FindAll(".rz-markdown-editor-pane-hidden"));
        }

        [Theory]
        [InlineData(MarkdownEditorMode.Edit, "rz-markdown-editor-pane-full")]
        [InlineData(MarkdownEditorMode.Preview, "rz-markdown-editor-pane-hidden")]
        public void MarkdownEditor_TextareaPane_HasModeClass(MarkdownEditorMode mode, string expectedClass)
        {
            using var ctx = CreateContext();
            var component = ctx.RenderComponent<RadzenMarkdownEditor>(p => p.Add(x => x.Mode, mode));

            var pane = component.Find("textarea").ParentElement!;
            Assert.Contains(expectedClass, pane.ClassName);
        }

        [Fact]
        public void MarkdownEditor_SplitMode_TextareaPane_HasNoModeClass()
        {
            using var ctx = CreateContext();
            var component = ctx.RenderComponent<RadzenMarkdownEditor>(p => p.Add(x => x.Mode, MarkdownEditorMode.Split));

            var pane = component.Find("textarea").ParentElement!;
            Assert.DoesNotContain("rz-markdown-editor-pane-full", pane.ClassName);
            Assert.DoesNotContain("rz-markdown-editor-pane-hidden", pane.ClassName);
            Assert.Contains("rz-markdown-editor-pane-fill", component.Find(".rz-markdown-editor-preview").ParentElement!.ClassName);
        }

        [Fact]
        public void MarkdownEditor_SwitchingToSplit_AfterMount_RendersSplitterBar()
        {
            using var ctx = CreateContext();
            var component = ctx.RenderComponent<RadzenMarkdownEditor>();
            Assert.Empty(component.FindAll(".rz-splitter-bar"));

            component.SetParametersAndRender(p => p.Add(x => x.Mode, MarkdownEditorMode.Split));

            Assert.Single(component.FindAll(".rz-splitter-bar"));
            Assert.Equal(2, component.FindAll(".rz-markdown-editor-pane").Count);
        }

        [Fact]
        public void MarkdownEditor_SwitchingToPreview_AfterMount_HidesTextareaPane_AndFillsPreviewPane()
        {
            using var ctx = CreateContext();
            var component = ctx.RenderComponent<RadzenMarkdownEditor>(p => p.Add(x => x.Value, "# Hi"));

            component.SetParametersAndRender(p => p.Add(x => x.Mode, MarkdownEditorMode.Preview));

            var panes = component.FindAll(".rz-markdown-editor-pane");
            Assert.Equal(2, panes.Count);
            Assert.Contains("rz-markdown-editor-pane-hidden", panes[0].ClassName);
            Assert.Contains("rz-markdown-editor-pane-fill", panes[1].ClassName);
            Assert.Contains("rz-splitter-pane-lastresizable", panes[1].ClassName);
            Assert.Contains("<h1", component.Markup);
        }
    }
}
