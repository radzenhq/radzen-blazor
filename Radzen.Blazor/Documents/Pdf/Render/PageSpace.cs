using Radzen.Documents.Geometry;

namespace Radzen.Documents.Pdf.Render;

internal static class PageSpace
{
    public static double FromTop(double top, double y) => BottomUpSpace.FromTop(top, y);

    public static double Bottom(double top, double y, double height)
        => BottomUpSpace.Bottom(top, y, height);

    public static PdfRect Bounds(double left, double top, in Rect bounds, double delta = 0)
    {
        var box = BottomUpSpace.Box(left, top, bounds, delta);
        return PdfRect.FromSize(box.Left, box.Bottom, box.Width, box.Height);
    }

    public static Matrix Flip(in Matrix transform, double pageHeight)
        => BottomUpSpace.FlipVertical(transform, pageHeight);
}
