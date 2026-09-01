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
        public void MarkdownEditor_DesignMode_RendersEditableAndHidesTextarea()
        {
            using var ctx = CreateContext();
            var component = ctx.RenderComponent<RadzenMarkdownEditor>(p => p.Add(x => x.Value, "# Hi"));

            var editable = component.Find(".rz-markdown-editor-design");
            Assert.Equal("true", editable.GetAttribute("contenteditable"));
            Assert.Empty(editable.InnerHtml.Trim()); // Blazor renders it empty; JS owns content
            Assert.True(component.Find("textarea").HasAttribute("hidden"));
        }

        [Fact]
        public void MarkdownEditor_SourceMode_ShowsTextareaAndHidesEditable()
        {
            using var ctx = CreateContext();
            var component = ctx.RenderComponent<RadzenMarkdownEditor>(p => p
                .Add(x => x.Value, "# Hi")
                .Add(x => x.Mode, MarkdownEditorMode.Source));

            Assert.False(component.Find("textarea").HasAttribute("hidden"));
            Assert.True(component.Find(".rz-markdown-editor-design").HasAttribute("hidden"));
        }

        [Fact]
        public void MarkdownEditor_DisabledRemovesContentEditable()
        {
            using var ctx = CreateContext();
            var component = ctx.RenderComponent<RadzenMarkdownEditor>(p => p.Add(x => x.Disabled, true));

            Assert.Equal("false", component.Find(".rz-markdown-editor-design").GetAttribute("contenteditable"));
        }

        [Fact]
        public void MarkdownEditor_ModeSwitcher_RaisesModeChanged()
        {
            using var ctx = CreateContext();
            MarkdownEditorMode? changed = null;
            var component = ctx.RenderComponent<RadzenMarkdownEditor>(p => p
                .Add(x => x.ModeChanged, m => changed = m));

            component.Find(".rz-markdown-editor-modes button:nth-child(2)").Click();

            Assert.Equal(MarkdownEditorMode.Source, changed);
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
            var apply = ctx.JSInterop.SetupVoid("apply", _ => true);
            apply.SetVoidResult();
            string? executed = null;

            var component = ctx.RenderComponent<RadzenMarkdownEditor>(p => p
                .Add(x => x.Value, "hello")
                .Add(x => x.Mode, MarkdownEditorMode.Source)
                .Add(x => x.Execute, args => executed = args.CommandName));

            await component.InvokeAsync(() => component.Instance.ExecuteCommandAsync(MarkdownEditorCommands.Bold));

            var invocation = Assert.Single(apply.Invocations);
            Assert.Equal(new object?[] { 0, 5, "**hello**", 2, 7 }, invocation.Arguments.ToArray());
            Assert.Equal("bold", executed);
        }

        [Fact]
        public async System.Threading.Tasks.Task MarkdownEditor_ExecuteCommand_UnknownCommand_RaisesExecuteWithoutApplying()
        {
            using var ctx = CreateContext();
            var apply = ctx.JSInterop.SetupVoid("apply", _ => true);
            apply.SetVoidResult();
            string? executed = null;

            var component = ctx.RenderComponent<RadzenMarkdownEditor>(p => p
                .Add(x => x.Mode, MarkdownEditorMode.Source)
                .Add(x => x.Execute, args => executed = args.CommandName));

            await component.InvokeAsync(() => component.Instance.ExecuteCommandAsync("insertToday"));

            Assert.Empty(apply.Invocations);
            Assert.Equal("insertToday", executed);
        }

        [Fact]
        public async System.Threading.Tasks.Task MarkdownEditor_ExecuteCommand_DesignMode_InvokesJsExecute()
        {
            using var ctx = CreateContext();
            var execute = ctx.JSInterop.SetupVoid("execute", _ => true);
            execute.SetVoidResult();
            string? executed = null;

            // Mode defaults to Design.
            var component = ctx.RenderComponent<RadzenMarkdownEditor>(p => p
                .Add(x => x.Value, "hello")
                .Add(x => x.Execute, args => executed = args.CommandName));

            await component.InvokeAsync(() => component.Instance.ExecuteCommandAsync(MarkdownEditorCommands.Bold));

            var invocation = Assert.Single(execute.Invocations);
            Assert.Equal(new object?[] { "bold", null, null }, invocation.Arguments.ToArray());
            Assert.Equal("bold", executed);
        }

        [Fact]
        public void MarkdownEditor_ExecuteCommand_Link_DesignMode_SavesSelectionBeforeDialogOpens()
        {
            using var ctx = CreateContext();
            var saveSelection = ctx.JSInterop.SetupVoid("saveSelection", _ => true);
            saveSelection.SetVoidResult();
            ctx.JSInterop.Setup<bool>("hasSelection", _ => true).SetResult(true);

            // Mode defaults to Design.
            var component = ctx.RenderComponent<RadzenMarkdownEditor>();

            // DialogService.OpenAsync never completes without a rendered <RadzenDialog> host, so this call
            // is fired without awaiting; saveSelection runs synchronously before that (never-resolving)
            // await, so it has already happened by the time this statement returns control.
            _ = component.InvokeAsync(() => component.Instance.ExecuteCommandAsync(MarkdownEditorCommands.Link));

            Assert.Single(saveSelection.Invocations);
        }

        [Fact]
        public void MarkdownEditor_RendersDefaultToolbar_WhenNoChildContent()
        {
            using var ctx = CreateContext();
            var component = ctx.RenderComponent<RadzenMarkdownEditor>();

            var icons = component.FindAll(".rz-markdown-editor-tools .rzi").Select(i => i.TextContent).ToList();
            Assert.Equal(new[] { "undo", "redo", "format_bold", "format_italic", "strikethrough_s", "title", "format_quote", "code", "code_blocks",
                                 "format_list_bulleted", "format_list_numbered", "checklist", "link", "image", "horizontal_rule" }, icons);
        }

        [Fact]
        public void MarkdownEditor_DefaultToolbar_HasUndoRedo()
        {
            using var ctx = CreateContext();
            var component = ctx.RenderComponent<RadzenMarkdownEditor>();

            Assert.Contains("undo", component.Markup);
            Assert.Contains("redo", component.Markup);
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
            var apply = ctx.JSInterop.SetupVoid("apply", _ => true);
            apply.SetVoidResult();

            var component = ctx.RenderComponent<RadzenMarkdownEditor>(p => p
                .Add(x => x.Value, "hi")
                .Add(x => x.Mode, MarkdownEditorMode.Source)
                .AddChildContent<RadzenMarkdownEditorItalic>());

            component.Find(".rz-markdown-editor-tools button").Click();

            var invocation = Assert.Single(apply.Invocations);
            Assert.Equal("*hi*", invocation.Arguments[2]);
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
        public void MarkdownEditor_ModeSwitcher_UpdatesInternalMode_WhenOneWayBound()
        {
            using var ctx = CreateContext();
            var component = ctx.RenderComponent<RadzenMarkdownEditor>(p => p
                .Add(x => x.Value, "# Hi")
                .Add(x => x.Mode, MarkdownEditorMode.Design));

            // Mode is one-way bound (no ModeChanged): the Mode parameter never changes, but the internal
            // mode field does, and rendering must follow the field, not the unchanged parameter.
            component.Find(".rz-markdown-editor-modes button:nth-child(2)").Click();

            Assert.False(component.Find("textarea").HasAttribute("hidden"));
            Assert.True(component.Find(".rz-markdown-editor-design").HasAttribute("hidden"));
        }

        [Fact]
        public async System.Threading.Tasks.Task MarkdownEditor_ExecuteShortcutAsync_AppliesRegisteredShortcut_AndIgnoresUnknownKey()
        {
            using var ctx = CreateContext();
            ctx.JSInterop.Setup<int[]?>("Radzen.getSelectionRange", _ => true).SetResult(new[] { 0, 2 });
            var apply = ctx.JSInterop.SetupVoid("apply", _ => true);
            apply.SetVoidResult();

            var component = ctx.RenderComponent<RadzenMarkdownEditor>(p => p
                .Add(x => x.Value, "hi")
                .Add(x => x.Mode, MarkdownEditorMode.Source)
                .AddChildContent<RadzenMarkdownEditorBold>());

            await component.InvokeAsync(() => component.Instance.ExecuteShortcutAsync("Ctrl+B"));

            var invocation = Assert.Single(apply.Invocations);
            Assert.Equal("**hi**", invocation.Arguments[2]);

            await component.InvokeAsync(() => component.Instance.ExecuteShortcutAsync("Ctrl+Z"));

            Assert.Single(apply.Invocations);
        }

        [Fact]
        public async System.Threading.Tasks.Task MarkdownEditor_ExecuteCommand_NormalizesCrlfSelectionOffsets()
        {
            using var ctx = CreateContext();
            ctx.JSInterop.Setup<int[]?>("Radzen.getSelectionRange", _ => true).SetResult(new[] { 6, 11 });
            var apply = ctx.JSInterop.SetupVoid("apply", _ => true);
            apply.SetVoidResult();

            var component = ctx.RenderComponent<RadzenMarkdownEditor>(p => p
                .Add(x => x.Value, "line1\r\nline2")
                .Add(x => x.Mode, MarkdownEditorMode.Source));

            await component.InvokeAsync(() => component.Instance.ExecuteCommandAsync(MarkdownEditorCommands.Bold));

            var invocation = Assert.Single(apply.Invocations);
            Assert.Equal(new object?[] { 6, 11, "**line2**", 8, 13 }, invocation.Arguments.ToArray());
        }

        [Fact]
        public void MarkdownEditor_Tools_RegisterShortcuts()
        {
            using var ctx = CreateContext();
            ctx.RenderComponent<RadzenMarkdownEditor>();

            // Loose mode records every call; no Setup so the un-awaited OnAfterRenderAsync cannot hang on a missing result.
            var invocation = Assert.Single(ctx.JSInterop.Invocations["Radzen.createMarkdownEditor"]);
            var keys = Assert.IsAssignableFrom<System.Collections.Generic.IEnumerable<string>>(invocation.Arguments[3]);
            Assert.Equal(new[] { "Ctrl+B", "Ctrl+I", "Ctrl+K" }, keys.OrderBy(k => k));
        }

        [Fact]
        public void CustomTool_Click_RaisesExecute_WithCommandName()
        {
            using var ctx = CreateContext();
            var apply = ctx.JSInterop.SetupVoid("apply", _ => true);
            apply.SetVoidResult();
            string? executed = null;

            var component = ctx.RenderComponent<RadzenMarkdownEditor>(p => p
                .Add(x => x.Mode, MarkdownEditorMode.Source)
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
            Assert.Contains("mode:Design", component.Markup);
        }

        [Fact]
        public async System.Threading.Tasks.Task MarkdownEditor_DesignInput_UpdatesValue_WithoutSetContent()
        {
            using var ctx = CreateContext();
            var plannedSetContent = ctx.JSInterop.SetupVoid("setContent", _ => true);
            string? changed = null;
            var component = ctx.RenderComponent<RadzenMarkdownEditor>(p => p
                .Add(x => x.Value, "old")
                .Add(x => x.ValueChanged, v => changed = v));
            int countAfterMount = plannedSetContent.Invocations.Count; // initial render already syncs content once; bunit's Invocations dictionary has no Clear()

            await component.InvokeAsync(() => component.Instance.OnDesignInputAsync("new **text**"));

            Assert.Equal("new **text**", changed);
            Assert.Equal(countAfterMount, plannedSetContent.Invocations.Count); // surface-originated change must not echo back
        }

        [Fact]
        public void MarkdownEditor_ProgrammaticValueChange_TriggersSetContent()
        {
            using var ctx = CreateContext();
            var plannedSetContent = ctx.JSInterop.SetupVoid("setContent", _ => true);
            var component = ctx.RenderComponent<RadzenMarkdownEditor>(p => p.Add(x => x.Value, "a"));
            int countBeforeExternalChange = plannedSetContent.Invocations.Count; // bunit's Invocations dictionary has no Clear(); compare counts instead

            component.SetParametersAndRender(p => p.Add(x => x.Value, "b"));

            Assert.True(plannedSetContent.Invocations.Count > countBeforeExternalChange);
        }

        [Fact]
        public async System.Threading.Tasks.Task MarkdownEditor_DesignChange_FlushesPendingEdit_UpdatesValueOnce_WithoutSetContent()
        {
            using var ctx = CreateContext();
            var plannedSetContent = ctx.JSInterop.SetupVoid("setContent", _ => true);
            int valueChangedCount = 0;
            string? changed = null;
            string? changeEventValue = null;
            var component = ctx.RenderComponent<RadzenMarkdownEditor>(p => p
                .Add(x => x.Value, "old")
                .Add(x => x.ValueChanged, v => { changed = v; valueChangedCount++; })
                .Add(x => x.Change, v => changeEventValue = v));
            int countAfterMount = plannedSetContent.Invocations.Count;

            // JS clears the debounce timer on blur and reports the surface's current content directly to
            // OnDesignChangeAsync, without a preceding OnDesignInputAsync — the keystroke that landed inside
            // the 250ms debounce window immediately before blur must still reach Value.
            await component.InvokeAsync(() => component.Instance.OnDesignChangeAsync("blurred **text**"));

            Assert.Equal("blurred **text**", changed);
            Assert.Equal(1, valueChangedCount); // flushed exactly once
            Assert.Equal("blurred **text**", changeEventValue);
            Assert.Equal(countAfterMount, plannedSetContent.Invocations.Count); // still no echo back to the surface
        }

        [Fact]
        public async System.Threading.Tasks.Task MarkdownEditor_DesignChange_AfterMatchingInput_DoesNotDoubleFireValueChanged()
        {
            using var ctx = CreateContext();
            ctx.JSInterop.SetupVoid("setContent", _ => true);
            int valueChangedCount = 0;
            var component = ctx.RenderComponent<RadzenMarkdownEditor>(p => p
                .Add(x => x.Value, "old")
                .Add(x => x.ValueChanged, _ => valueChangedCount++));

            // The debounced OnDesignInputAsync already flushed this markdown; blur reporting the same
            // content must not raise ValueChanged a second time.
            await component.InvokeAsync(() => component.Instance.OnDesignInputAsync("same **text**"));
            await component.InvokeAsync(() => component.Instance.OnDesignChangeAsync("same **text**"));

            Assert.Equal(1, valueChangedCount);
        }
    }
}
