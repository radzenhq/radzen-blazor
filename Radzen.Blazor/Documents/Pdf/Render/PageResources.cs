using System.Collections.Generic;
using System;
using Radzen.Documents.LaidOut;
using Radzen.Documents.Pdf.Output;
using Radzen.Documents.Pdf.Content;

namespace Radzen.Documents.Pdf.Render;

internal sealed class PageResources
{
    public OrderedSet<EmittedFont> UsedFonts { get; } = [];

    public OrderedSet<OutputImage> UsedImages { get; } = [];

    private readonly ResourceNameAllocator<string, OutputExtGState> extGStates =
        new(ContentResourcePrefixes.Page.ExtGState, reserved: null, StringComparer.Ordinal);

    private readonly ResourceNameAllocator<(GradientPaint Gradient, Matrix Matrix), OutputPattern> patterns =
        new(ContentResourcePrefixes.Page.Pattern, reserved: null);

    private readonly Dictionary<string, OutputExtGState> extGStatesByKey = new(StringComparer.Ordinal);

    public IReadOnlyList<OutputExtGState> ExtGStates => extGStates.Values;

    public IReadOnlyList<OutputPattern> Patterns => patterns.Values;

    private readonly Dictionary<(double Width, double Height, double Radius, double Blur), ShadowMask> shadowMasks = [];

    public ShadowMask RenderShadowMask(double shapeWidthPt, double shapeHeightPt, double radiusPt, double blurPt)
    {
        var key = (shapeWidthPt, shapeHeightPt, radiusPt, blurPt);
        if (!shadowMasks.TryGetValue(key, out var mask))
        {
            mask = GaussianBlur.Render(shapeWidthPt, shapeHeightPt, radiusPt, blurPt);
            shadowMasks[key] = mask;
        }

        return mask;
    }

    public string RegisterExtGState(double fillAlpha, double strokeAlpha)
        => RegisterExtGState(fillAlpha, strokeAlpha, null);

    public string RegisterExtGState(double fillAlpha, double strokeAlpha, BlendMode? blend)
    {
        fillAlpha = Math.Clamp(fillAlpha, 0, 1);
        strokeAlpha = Math.Clamp(strokeAlpha, 0, 1);
        return ExtGStateRegistration.RegisterAlpha(extGStates, fillAlpha, strokeAlpha, blend, key => Track(new OutputExtGState(
            key,
            fillAlpha,
            strokeAlpha,
            blend,
            softMask: null)));
    }

    private OutputExtGState Track(OutputExtGState state)
    {
        extGStatesByKey[state.Key] = state;
        return state;
    }

    public OutputExtGState? FindExtGState(string key)
        => extGStatesByKey.TryGetValue(key, out var state) ? state : null;

    public string? ApplyAlpha(string? extGState, double alpha)
    {
        if (alpha >= 1)
        {
            return extGState;
        }

        if (extGState is null)
        {
            return RegisterExtGState(alpha, alpha);
        }

        if (FindExtGState(extGState) is not { SoftMask: null } state)
        {
            return extGState;
        }

        return RegisterExtGState(
            state.FillAlpha * alpha,
            state.StrokeAlpha * alpha,
            state.Blend);
    }

    public string RegisterSoftMaskExtGState(
        double fillAlpha,
        double strokeAlpha,
        OutputSoftMask softMask,
        string? contentKey)
    {
        OutputExtGState Create(string key) => Track(new OutputExtGState(
            key,
            Math.Clamp(fillAlpha, 0, 1),
            Math.Clamp(strokeAlpha, 0, 1),
            blend: null,
            softMask));

        return contentKey is { } dedup
            ? extGStates.GetOrAdd("m|" + dedup, Create)
            : extGStates.Add(Create);
    }

    public string RegisterPattern(GradientPaint gradient, Matrix matrix)
        => patterns.GetOrAdd((gradient, matrix), key => new OutputPattern(key, gradient, matrix));
}
