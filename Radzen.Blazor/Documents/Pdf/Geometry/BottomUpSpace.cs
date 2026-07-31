namespace Radzen.Documents.Pdf.Geometry;

internal static class BottomUpSpace
{
    public static double FromTop(double top, double y) => top - y;

    public static double Bottom(double top, double y, double height) => FromTop(top, y) - height;

    public static PageBox Box(double left, double top, in Rect bounds, double delta)
        => new(
            left + bounds.X,
            Bottom(top, bounds.Y + delta, bounds.Height),
            bounds.Width,
            bounds.Height);

    public static PdfRect Bounds(double left, double top, in Rect bounds, double delta = 0)
    {
        var box = Box(left, top, bounds, delta);
        return PdfRect.FromSize(box.Left, box.Bottom, box.Width, box.Height);
    }

    public static Matrix FlipVertical(in Matrix transform, double height)
        => Matrix.FromComponents(
            transform.A,
            -transform.B,
            -transform.C,
            transform.D,
            transform.E + (height * transform.C),
            height - transform.F - (height * transform.D));
}
