namespace Radzen.Documents.Pdf.Content;

internal static class BezierGeometry
{
    public const double Kappa = 0.5522847498307936;

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
