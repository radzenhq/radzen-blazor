using System;

namespace Radzen.Documents.Pdf.Emit;

// A rasterized shadow coverage buffer: an 8-bit DeviceGray image (white = fully covered)
// plus the point-space margin the blur added on every edge beyond the shape bounds.
internal readonly struct ShadowMask
{
    public required byte[] Pixels { get; init; }
    public required int Width { get; init; }
    public required int Height { get; init; }
    public required double MarginPoints { get; init; }
}

// Pure managed rounded-rectangle coverage rasterization plus a separable Gaussian blur.
// Array math only so it runs unchanged under WASM. Deterministic for identical inputs.
internal static class GaussianBlur
{
    private const double MaxShapePixels = 512;
    private const double BaseScale = 2.0;

    // Caps direct-convolution cost: kernelRadius = ceil(3*sigma) and sigma = blurPt*scale/2,
    // so an uncapped blur is O((shape+2r)^2 * (2r+1)) and can pin a WASM thread for seconds.
    // When a blur would exceed this radius the raster scale is reduced so work stays bounded;
    // small blurs are unaffected and their output is byte-identical.
    private const int MaxKernelRadius = 64;

    // Rasterizes a rounded rectangle of the given point size (analytic 1px anti-aliasing via
    // signed distance) then blurs it. The buffer is padded by the blur's kernel radius on every
    // side so the softened edge fully fades inside it; that padding is returned as MarginPoints.
    public static ShadowMask Render(double shapeWidthPt, double shapeHeightPt, double radiusPt, double blurPt)
    {
        var maxShape = Math.Max(shapeWidthPt, shapeHeightPt);
        var scale = BaseScale;
        if (maxShape > 0 && maxShape * scale > MaxShapePixels)
        {
            scale = MaxShapePixels / maxShape;
        }

        // kernelRadius = ceil(3 * blurPt * scale / 2) <= MaxKernelRadius bounds the convolution.
        if (blurPt > 0)
        {
            var kernelScale = 2.0 * MaxKernelRadius / (3.0 * blurPt);
            if (kernelScale < scale)
            {
                scale = kernelScale;
            }
        }

        var shapeW = Math.Max(1, (int)Math.Round(shapeWidthPt * scale));
        var shapeH = Math.Max(1, (int)Math.Round(shapeHeightPt * scale));

        var sigma = blurPt > 0 ? blurPt * scale / 2.0 : 0;
        var kernelRadius = sigma > 0 ? (int)Math.Ceiling(3 * sigma) : 0;

        var width = shapeW + (2 * kernelRadius);
        var height = shapeH + (2 * kernelRadius);

        var coverage = Coverage(width, height, kernelRadius, shapeW, shapeH, radiusPt * scale);
        if (kernelRadius > 0)
        {
            coverage = Blur(coverage, width, height, sigma, kernelRadius);
        }

        var pixels = new byte[width * height];
        for (var i = 0; i < pixels.Length; i++)
        {
            pixels[i] = (byte)Math.Clamp((int)Math.Round(coverage[i] * 255.0), 0, 255);
        }

        return new ShadowMask
        {
            Pixels = pixels,
            Width = width,
            Height = height,
            MarginPoints = kernelRadius / scale,
        };
    }

    private static float[] Coverage(int width, int height, int margin, int shapeW, int shapeH, double radiusPx)
    {
        var hx = shapeW / 2.0;
        var hy = shapeH / 2.0;
        var r = Math.Clamp(radiusPx, 0, Math.Min(hx, hy));
        var centerX = margin + hx;
        var centerY = margin + hy;

        var coverage = new float[width * height];
        for (var y = 0; y < height; y++)
        {
            var py = y + 0.5 - centerY;
            for (var x = 0; x < width; x++)
            {
                var px = x + 0.5 - centerX;
                var qx = Math.Abs(px) - (hx - r);
                var qy = Math.Abs(py) - (hy - r);
                var dx = Math.Max(qx, 0);
                var dy = Math.Max(qy, 0);
                var dist = Math.Sqrt((dx * dx) + (dy * dy)) + Math.Min(Math.Max(qx, qy), 0) - r;
                coverage[(y * width) + x] = (float)Math.Clamp(0.5 - dist, 0, 1);
            }
        }

        return coverage;
    }

    private static float[] Blur(float[] source, int width, int height, double sigma, int radius)
    {
        var kernel = new float[(2 * radius) + 1];
        var twoSigmaSq = 2 * sigma * sigma;
        double sum = 0;
        for (var i = -radius; i <= radius; i++)
        {
            var w = (float)Math.Exp(-(i * i) / twoSigmaSq);
            kernel[i + radius] = w;
            sum += w;
        }

        for (var i = 0; i < kernel.Length; i++)
        {
            kernel[i] = (float)(kernel[i] / sum);
        }

        var horizontal = new float[source.Length];
        for (var y = 0; y < height; y++)
        {
            var row = y * width;
            for (var x = 0; x < width; x++)
            {
                float acc = 0;
                for (var k = -radius; k <= radius; k++)
                {
                    var sx = x + k;
                    if (sx >= 0 && sx < width)
                    {
                        acc += source[row + sx] * kernel[k + radius];
                    }
                }

                horizontal[row + x] = acc;
            }
        }

        var result = new float[source.Length];
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                float acc = 0;
                for (var k = -radius; k <= radius; k++)
                {
                    var sy = y + k;
                    if (sy >= 0 && sy < height)
                    {
                        acc += horizontal[(sy * width) + x] * kernel[k + radius];
                    }
                }

                result[(y * width) + x] = acc;
            }
        }

        return result;
    }
}
