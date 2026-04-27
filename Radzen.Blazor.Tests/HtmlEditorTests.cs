using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using System.Threading.Tasks;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class HtmlEditorTests
    {
        static TestContext CreateContext()
        {
            var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.Services.AddScoped<DialogService>();
            ctx.Services.AddScoped<ContextMenuService>();
            ctx.Services.AddScoped<NotificationService>();
            return ctx;
        }

        [Fact]
        public void HtmlEditor_Renders_WithClassName()
        {
            using var ctx = CreateContext();
            var component = ctx.RenderComponent<RadzenHtmlEditor>();

            Assert.Contains(@"rz-html-editor", component.Markup);
        }

        [Fact]
        public void HtmlEditor_Renders_ShowToolbar_True()
        {
            using var ctx = CreateContext();
            var component = ctx.RenderComponent<RadzenHtmlEditor>(parameters =>
            {
                parameters.Add(p => p.ShowToolbar, true);
            });

            Assert.Contains("rz-html-editor-toolbar", component.Markup);
        }

        [Fact]
        public void HtmlEditor_Renders_ShowToolbar_False()
        {
            using var ctx = CreateContext();
            var component = ctx.RenderComponent<RadzenHtmlEditor>(parameters =>
            {
                parameters.Add(p => p.ShowToolbar, false);
            });

            Assert.DoesNotContain("rz-html-editor-toolbar", component.Markup);
        }

        [Fact]
        public void HtmlEditor_Renders_Mode_Design()
        {
            using var ctx = CreateContext();
            var component = ctx.RenderComponent<RadzenHtmlEditor>(parameters =>
            {
                parameters.Add(p => p.Mode, HtmlEditorMode.Design);
            });

            // Design mode shows the content editable div
            Assert.Contains("contenteditable", component.Markup);
        }

        [Fact]
        public void HtmlEditor_Renders_Mode_Source()
        {
            using var ctx = CreateContext();
            var component = ctx.RenderComponent<RadzenHtmlEditor>(parameters =>
            {
                parameters.Add(p => p.Mode, HtmlEditorMode.Source);
            });

            // Source mode shows the textarea for HTML editing
            Assert.Contains("rz-html-editor-source", component.Markup);
        }

        [Fact]
        public void HtmlEditor_Renders_Disabled_Attribute()
        {
            using var ctx = CreateContext();
            var component = ctx.RenderComponent<RadzenHtmlEditor>(parameters =>
            {
                parameters.Add(p => p.Disabled, true);
            });

            Assert.Contains("disabled", component.Markup);
        }

        [Fact]
        public void HtmlEditor_Renders_ContentArea()
        {
            using var ctx = CreateContext();
            var component = ctx.RenderComponent<RadzenHtmlEditor>();

            Assert.Contains("rz-html-editor-content", component.Markup);
        }

        [Fact]
        public void HtmlEditor_DefaultToolbar_Renders_TableTools()
        {
            using var ctx = CreateContext();

            var component = ctx.RenderComponent<RadzenHtmlEditor>();

            Assert.Contains("table_chart", component.Markup);
            Assert.Contains("merge_type", component.Markup);
            Assert.Contains("call_split", component.Markup);
        }

        [Fact]
        public void HtmlEditorTable_Renders_PropertyPanelLabels()
        {
            using var ctx = CreateContext();

            var component = ctx.RenderComponent<RadzenHtmlEditorTable>();

            Assert.Equal("Insert table", component.Instance.Title);
            Assert.Equal("Column width (px)", component.Instance.ColumnWidthPxText);
            Assert.Equal("Border color", component.Instance.BorderColorText);
            Assert.Equal("Top", component.Instance.BorderTopText);
        }

        [Fact]
        public void HtmlEditorTable_Renders_BorderSideDefaults()
        {
            using var ctx = CreateContext();

            var component = ctx.RenderComponent<RadzenHtmlEditorTable>();

            Assert.Equal("Right", component.Instance.BorderRightText);
            Assert.Equal("Bottom", component.Instance.BorderBottomText);
            Assert.Equal("Left", component.Instance.BorderLeftText);
            Assert.Equal("Border style", component.Instance.BorderStyleText);
        }

        [Fact]
        public void HtmlEditorTableCommandButton_Disabled_WithoutTableSelection()
        {
            using var ctx = CreateContext();

            var component = ctx.RenderComponent<RadzenHtmlEditor>(parameters => parameters
                .AddChildContent<RadzenHtmlEditorTableCommandButton>(childParameters => childParameters
                    .Add(p => p.TableCommand, "mergeCellRight")
                    .Add(p => p.Icon, "merge_type")
                    .Add(p => p.Title, "Merge right")));

            var button = component.Find("button");

            Assert.True(button.HasAttribute("disabled"));
        }

        [Fact]
        public void HtmlEditorTableTools_Renders_TableButtons()
        {
            using var ctx = CreateContext();

            var component = ctx.RenderComponent<RadzenHtmlEditor>(parameters => parameters
                .AddChildContent<RadzenHtmlEditorTableTools>());

            Assert.Contains("table_chart", component.Markup);
            Assert.Contains("merge_type", component.Markup);
            Assert.Contains("call_split", component.Markup);
        }

        [Fact]
        public void HtmlEditorCommandState_Defaults_ToSuccessfulResult()
        {
            var state = new RadzenHtmlEditorCommandState();

            Assert.True(state.Success);
            Assert.Null(state.Message);
        }

        [Fact]
        public async Task HtmlEditor_ExecuteTableCommandAsync_UsesLocalizedBlockedMessage()
        {
            using var ctx = CreateContext();

            ctx.JSInterop.Setup<RadzenHtmlEditorCommandState>("Radzen.execTableCommand", _ => true)
                .SetResult(new RadzenHtmlEditorCommandState
                {
                    Success = false,
                    Message = "table.paste.blocked"
                });

            ctx.JSInterop.Setup<HtmlEditorTableSelection>("Radzen.getTableSelection", _ => true)
                .SetResult(new HtmlEditorTableSelection());

            var component = ctx.RenderComponent<RadzenHtmlEditor>(parameters => parameters
                .Add(p => p.TableStrings, new HtmlEditorTableStrings
                {
                    ActionBlocked = "Aktion blockiert",
                    PasteBlocked = "Einfügen ist für diese Zellstruktur nicht möglich."
                }));

            await component.InvokeAsync(() => component.Instance.ExecuteTableCommandAsync("pasteCells"));

            var notificationService = ctx.Services.GetRequiredService<NotificationService>();
            var message = Assert.Single(notificationService.Messages);
            Assert.Equal(NotificationSeverity.Warning, message.Severity);
            Assert.Equal("Aktion blockiert", message.Summary);
            Assert.Equal("Einfügen ist für diese Zellstruktur nicht möglich.", message.Detail);
        }

        [Theory]
        [InlineData("insertTable")]
        [InlineData("updateTable")]
        [InlineData("addRowBefore")]
        [InlineData("addRowAfter")]
        [InlineData("addColumnBefore")]
        [InlineData("addColumnAfter")]
        [InlineData("deleteRow")]
        [InlineData("deleteColumn")]
        [InlineData("mergeCellRight")]
        [InlineData("mergeCellDown")]
        [InlineData("splitCell")]
        [InlineData("deleteTable")]
        public async Task HtmlEditor_ExecuteTableCommandAsync_RoundTrips_CommandState(string command)
        {
            using var ctx = CreateContext();

            ctx.JSInterop.Setup<RadzenHtmlEditorCommandState>("Radzen.execTableCommand", invocation =>
                invocation.Arguments.Count >= 2 && invocation.Arguments[1]?.ToString() == command)
                .SetResult(new RadzenHtmlEditorCommandState
                {
                    Success = true,
                    Html = $"<p>{command}</p>"
                });

            var component = ctx.RenderComponent<RadzenHtmlEditor>(parameters => parameters
                .Add(p => p.Value, "<p>initial</p>"));

            await component.InvokeAsync(() => component.Instance.ExecuteTableCommandAsync(command));

            Assert.True(component.Instance.State.Success);
            Assert.Equal($"<p>{command}</p>", component.Instance.State.Html);
        }

        [Fact]
        public async Task HtmlEditor_ContextMenuTableCommand_DoesNotShowBlockedNotification_WhenCommandSucceeds()
        {
            using var ctx = CreateContext();

            ctx.JSInterop.Setup<RadzenHtmlEditorCommandState>("Radzen.execTableCommand", invocation =>
                invocation.Arguments.Count >= 2 && invocation.Arguments[1]?.ToString() == "addRowAfter")
                .SetResult(new RadzenHtmlEditorCommandState
                {
                    Success = true,
                    Html = "<table><tbody><tr><td>Cell</td></tr><tr><td>&nbsp;</td></tr></tbody></table>"
                });

            var component = ctx.RenderComponent<RadzenHtmlEditor>();

            await component.InvokeAsync(() => component.Instance.ExecuteTableCommandAsync("addRowAfter"));

            var notificationService = ctx.Services.GetRequiredService<NotificationService>();
            Assert.Empty(notificationService.Messages);
            Assert.True(component.Instance.State.Success);
        }

        [Fact]
        public async Task HtmlEditor_GetTableSelectionAsync_RoundTrips_StyleFields()
        {
            using var ctx = CreateContext();

            ctx.JSInterop.Setup<HtmlEditorTableSelection>("Radzen.getTableSelection", _ => true)
                .SetResult(new HtmlEditorTableSelection
                {
                    InTable = true,
                    ColumnWidthPx = 120,
                    CellPaddingPx = 8,
                    CellBorder = "1px solid red",
                    BorderStyle = "dashed",
                    BorderWidthPx = 2,
                    BorderColor = "red",
                    BorderTop = true,
                    BorderRight = true,
                    BorderBottom = false,
                    BorderLeft = true
                });

            var component = ctx.RenderComponent<RadzenHtmlEditor>();

            var selection = await component.InvokeAsync(() => component.Instance.GetTableSelectionAsync().AsTask());

            Assert.True(selection.InTable);
            Assert.Equal(120, selection.ColumnWidthPx);
            Assert.Equal(8, selection.CellPaddingPx);
            Assert.Equal("1px solid red", selection.CellBorder);
            Assert.Equal("dashed", selection.BorderStyle);
            Assert.Equal(2, selection.BorderWidthPx);
            Assert.Equal("red", selection.BorderColor);
            Assert.True(selection.BorderTop);
            Assert.True(selection.BorderRight);
            Assert.False(selection.BorderBottom);
            Assert.True(selection.BorderLeft);
        }

        [Fact]
        public void HtmlEditorTable_UsesEditorTableTextFallbacks()
        {
            var editor = new RadzenHtmlEditor
            {
                TableStrings = new HtmlEditorTableStrings
                {
                    DialogTitle = "Tabelle einfügen",
                    Rows = "Zeilen",
                    Columns = "Spalten",
                    Width = "Breite",
                    Border = "Rahmen",
                    HeaderRow = "Kopfzeile einschließen",
                    Edit = "Tabelle bearbeiten",
                    OK = "Einfügen",
                    Update = "Aktualisieren",
                    Cancel = "Abbrechen",
                    InsertRowAbove = "Zeile oben einfügen",
                    InsertColumnLeft = "Spalte links einfügen",
                    ColumnWidth = "Spaltenbreite",
                    CellBackground = "Zellhintergrund",
                    CellPadding = "Zellenabstand",
                    CellTextAlign = "Horizontal",
                    CellVerticalAlign = "Vertikal",
                    CellBorder = "Zellrahmen",
                    ColumnWidthPx = "Spaltenbreite (px)",
                    CellPaddingPx = "Zellenabstand (px)",
                    BorderStyle = "Rahmenstil",
                    BorderWidthPx = "Rahmenbreite (px)",
                    BorderColor = "Rahmenfarbe",
                    BorderTop = "Oben",
                    BorderRight = "Rechts",
                    BorderBottom = "Unten",
                    BorderLeft = "Links",
                    DeleteTable = "Tabelle entfernen",
                    MergeRight = "Nach rechts verbinden"
                }
            };

            var component = new RadzenHtmlEditorTable
            {
                Editor = editor
            };

            Assert.Equal("Tabelle einfügen", component.Title);
            Assert.Equal("Zeilen", component.RowsText);
            Assert.Equal("Spalten", component.ColumnsText);
            Assert.Equal("Breite", component.WidthText);
            Assert.Equal("Rahmen", component.BorderText);
            Assert.Equal("Kopfzeile einschließen", component.HeaderRowText);
            Assert.Equal("Tabelle bearbeiten", component.EditText);
            Assert.Equal("Einfügen", component.OkText);
            Assert.Equal("Aktualisieren", component.UpdateText);
            Assert.Equal("Abbrechen", component.CancelText);
            Assert.Equal("Zeile oben einfügen", component.InsertRowAboveText);
            Assert.Equal("Spalte links einfügen", component.InsertColumnLeftText);
            Assert.Equal("Spaltenbreite", component.ColumnWidthText);
            Assert.Equal("Zellhintergrund", component.CellBackgroundText);
            Assert.Equal("Zellenabstand", component.CellPaddingText);
            Assert.Equal("Horizontal", component.CellTextAlignText);
            Assert.Equal("Vertikal", component.CellVerticalAlignText);
            Assert.Equal("Zellrahmen", component.CellBorderText);
            Assert.Equal("Spaltenbreite (px)", component.ColumnWidthPxText);
            Assert.Equal("Zellenabstand (px)", component.CellPaddingPxText);
            Assert.Equal("Rahmenstil", component.BorderStyleText);
            Assert.Equal("Rahmenbreite (px)", component.BorderWidthPxText);
            Assert.Equal("Rahmenfarbe", component.BorderColorText);
            Assert.Equal("Oben", component.BorderTopText);
            Assert.Equal("Rechts", component.BorderRightText);
            Assert.Equal("Unten", component.BorderBottomText);
            Assert.Equal("Links", component.BorderLeftText);
            Assert.Equal("Tabelle entfernen", component.DeleteTableText);
            Assert.Equal("Nach rechts verbinden", component.MergeRightText);
        }
    }
}

