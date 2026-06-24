using Bunit;
using Xunit;

using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet.Tests;

#nullable enable

/// <summary>
/// The autofill-handle drag and the drawing move/resize drags repaint only their overlay via
/// dedicated Worksheet events, never the root component. These tests pin the event wiring and
/// that each overlay reacts only to its own drawing kind.
/// </summary>
public class DragRenderEventsTests
{
    [Fact]
    public void AutofillPreview_Set_RaisesAutofillPreviewChanged()
    {
        var sheet = new Worksheet(10, 10);
        var raised = 0;
        sheet.AutofillPreviewChanged += () => raised++;

        sheet.AutofillPreview = new RangeRef(new CellRef(0, 0), new CellRef(2, 0));

        Assert.Equal(1, raised);
    }

    [Fact]
    public void OnDrawingGeometryChanged_Raises_WithThatDrawing()
    {
        var sheet = new Worksheet(10, 10);
        var image = CreateImage();
        IAnchoredDrawing? received = null;
        sheet.DrawingGeometryChanged += d => received = d;

        sheet.OnDrawingGeometryChanged(image);

        Assert.Same(image, received);
    }

    private static SheetImage CreateImage(double width = 137, double height = 89)
    {
        return new SheetImage
        {
            AnchorMode = DrawingAnchorMode.OneCellAnchor,
            From = new CellAnchor { Row = 0, Column = 0 },
            Width = width,
            Height = height,
            Data = [0x89, 0x50, 0x4E, 0x47],
            ContentType = "image/png",
            Name = "test.png"
        };
    }

    public class Render : TestContext
    {
        [Fact]
        public async System.Threading.Tasks.Task AutofillOverlay_ShowsPreview_WhenSet_AndClears()
        {
            var sheet = new Worksheet(10, 10);
            sheet.Selection.Select(new CellRef(0, 0));
            var context = new ImageMockContext(sheet);

            var cut = RenderComponent<AutofillOverlay>(parameters => parameters
                .Add(p => p.Worksheet, sheet)
                .Add(p => p.Context, context));

            Assert.Empty(cut.FindAll(".rz-spreadsheet-autofill-range"));

            await cut.InvokeAsync(() => sheet.AutofillPreview = new RangeRef(new CellRef(0, 0), new CellRef(2, 0)));
            Assert.NotEmpty(cut.FindAll(".rz-spreadsheet-autofill-range"));

            await cut.InvokeAsync(() => sheet.AutofillPreview = null);
            Assert.Empty(cut.FindAll(".rz-spreadsheet-autofill-range"));
        }

        [Fact]
        public async System.Threading.Tasks.Task ImageOverlay_Repaints_OnImageGeometryChanged()
        {
            var sheet = new Worksheet(10, 10);
            var image = CreateImage();
            sheet.AddImage(image);
            var context = new ImageMockContext(sheet);

            var cut = RenderComponent<ImageOverlay>(parameters => parameters
                .Add(p => p.Worksheet, sheet)
                .Add(p => p.Context, context));

            Assert.Contains("137px", cut.Markup);
            var before = cut.RenderCount;

            await cut.InvokeAsync(() =>
            {
                image.Width = 263;
                sheet.OnDrawingGeometryChanged(image);
            });

            Assert.Equal(before + 1, cut.RenderCount);
            Assert.Contains("263px", cut.Markup);
            Assert.DoesNotContain("137px", cut.Markup);
        }

        [Fact]
        public async System.Threading.Tasks.Task ImageOverlay_DoesNotRepaint_WhenChartGeometryChanges()
        {
            var sheet = new Worksheet(10, 10);
            sheet.AddImage(CreateImage());
            var context = new ImageMockContext(sheet);

            var cut = RenderComponent<ImageOverlay>(parameters => parameters
                .Add(p => p.Worksheet, sheet)
                .Add(p => p.Context, context));

            var chart = new SheetChart { From = new CellAnchor { Row = 0, Column = 0 } };
            var before = cut.RenderCount;

            await cut.InvokeAsync(() => sheet.OnDrawingGeometryChanged(chart));

            Assert.Equal(before, cut.RenderCount);
        }
    }
}
