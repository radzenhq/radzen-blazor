namespace Radzen.Documents.Pdf.Content;

// Bezier approximation of circular/elliptical arcs, shared by every emitter that needs one.
internal static class BezierGeometry
{
    // Control-point offset, as a fraction of the radius, for a quarter arc drawn as one cubic
    // Bezier: 4/3 * (sqrt(2) - 1).
    public const double Kappa = 0.5522847498307936;

    // Appends a closed ellipse centred on (cx, cy) as four cubic Beziers, starting at the
    // rightmost point and running counterclockwise.
    public static void AppendEllipse(PathContent path, double cx, double cy, double rx, double ry)
    {
        var kx = rx * Kappa;
        var ky = ry * Kappa;
        path.MoveTo(cx + rx, cy);
        path.CurveTo(cx + rx, cy + ky, cx + kx, cy + ry, cx, cy + ry);
        path.CurveTo(cx - kx, cy + ry, cx - rx, cy + ky, cx - rx, cy);
        path.CurveTo(cx - rx, cy - ky, cx - kx, cy - ry, cx, cy - ry);
        path.CurveTo(cx + kx, cy - ry, cx + rx, cy - ky, cx + rx, cy);
        path.Close();
    }

    public static void AppendCircle(PathContent path, double cx, double cy, double r) =>
        AppendEllipse(path, cx, cy, r, r);
}
