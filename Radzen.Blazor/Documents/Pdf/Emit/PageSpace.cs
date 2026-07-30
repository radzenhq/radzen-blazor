namespace Radzen.Documents.Pdf.Emit;

internal static class PageSpace
{
    public static double FromTop(double top, double y) => top - y;

    public static double Bottom(double top, double y, double height)
        => FromTop(top, y) - height;

    public static PdfRect Bounds(double left, double top, in Rect bounds, double delta = 0)
        => PdfRect.FromSize(
            left + bounds.X,
            Bottom(top, bounds.Y + delta, bounds.Height),
            bounds.Width,
            bounds.Height);

    public static Matrix Flip(in Matrix transform, double pageHeight)
        => Matrix.FromComponents(
            transform.A,
            -transform.B,
            -transform.C,
            transform.D,
            transform.E + (pageHeight * transform.C),
            pageHeight - transform.F - (pageHeight * transform.D));
}
