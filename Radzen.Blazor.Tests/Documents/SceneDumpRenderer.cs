#nullable enable
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Radzen.Documents.Core;
using Radzen.Documents.Fonts;
using Radzen.Documents.LaidOut;
using Radzen.Documents.Layout;
using Radzen.Documents.Scene;
using Radzen.Documents;

namespace Radzen.Blazor.Documents.Tests;

internal sealed class SceneDumpRenderer : ISceneVisitor
{
    private readonly StringBuilder text = new();
    private readonly Dictionary<int, SemanticArtifactKind> artifacts = [];
    private readonly Dictionary<int, SemanticIntent> intents = [];
    private int depth;
    private int item;
    private double layerTop;

    public static string Render(LaidOutDocument document)
    {
        var renderer = new SceneDumpRenderer();
        SceneWalk.Document(document, renderer);
        return renderer.text.ToString();
    }

    private static readonly FragmentPaint Unspaced = new()
    {
        Font = default,
        Opacity = 1,
        LetterSpacing = 0,
        WordSpacing = 0,
        HorizontalScale = 1,
        ScriptScale = 1,
        Rise = 0,
        IsScript = false,
        Invisible = false,
    };

    private static string N(double value)
        => (value == 0 ? 0 : value).ToString("F2", CultureInfo.InvariantCulture);

    private static string Rgba(Color color)
        => string.Create(CultureInfo.InvariantCulture, $"#{color.R:X2}{color.G:X2}{color.B:X2}{color.A:X2}");

    private void Write(string line)
    {
        text.Append(' ', depth * 2);
        text.Append(line);
        text.Append('\n');
    }

    private void Enter(string line)
    {
        Write(line);
        depth++;
    }

    private void Leave(string line)
    {
        depth--;
        Write(line);
    }

    private string Semantics(SourceId source)
    {
        if (artifacts.TryGetValue(source.Value, out var artifact))
        {
            return $" artifact={artifact}";
        }

        return intents.TryGetValue(source.Value, out var intent) ? $" role={intent}" : string.Empty;
    }

    void ISceneVisitor.BeginDocument(LaidOutDocument document)
    {
        var structure = document.Semantics.Structure;
        foreach (var artifact in structure.Artifacts)
        {
            artifacts[artifact.Source.Value] = artifact.Kind;
        }

        foreach (var association in structure.Associations)
        {
            intents[association.Source.Value] = structure.Nodes[association.Element].Intent;
        }

        Enter(string.Create(
            CultureInfo.InvariantCulture,
            $"scene pages={document.Pages.Length} language={document.Semantics.Language ?? "-"}"));

        var info = document.Info;
        Write(string.Create(
            CultureInfo.InvariantCulture,
            $"info title={info.Title ?? "-"} author={info.Author ?? "-"} subject={info.Subject ?? "-"}"
                + $" keywords={info.Keywords ?? "-"} creator={info.Creator ?? "-"}"
                + $" created={Stamp(info.CreationDate)} modified={Stamp(info.ModificationDate)}"));
    }

    private static string Stamp(System.DateTimeOffset? value)
        => value is { } moment
            ? moment.ToString("yyyy-MM-ddTHH:mm:ssK", CultureInfo.InvariantCulture)
            : "-";

    void ISceneVisitor.EndDocument(LaidOutDocument document) => Leave("/scene");

    void ISceneVisitor.BeginPage(LaidOutPage page, int index)
    {
        item = 0;
        Enter(string.Create(
            CultureInfo.InvariantCulture,
            $"page {index} size={N(page.Size.Width.Point)}x{N(page.Size.Height.Point)}"
                + $" content={N(page.ContentBox.X)},{N(page.ContentBox.Y)},{N(page.ContentBox.Width)},{N(page.ContentBox.Height)}"));
    }

    void ISceneVisitor.EndPage(LaidOutPage page, int index) => Leave("/page");

    void ISceneVisitor.EnterLayer(SceneLayerKind kind, double top)
    {
        layerTop = top;
        Enter($"layer {kind} top={N(top)}");
    }

    void ISceneVisitor.LeaveLayer(SceneLayerKind kind) => Leave($"/layer {kind}");

    void ISceneVisitor.BeginItem(int zOrder)
        => Enter(string.Create(CultureInfo.InvariantCulture, $"item {item++} z={zOrder}"));

    void ISceneVisitor.EndItem() => Leave("/item");

    void ISceneVisitor.Line(in LaidOutLine line, in SceneFrame frame)
    {
        var x = frame.Left + line.X;
        var y = layerTop + frame.Delta + line.Y;
        Line(line.Line, x, y, line.Source);
    }

    private void Line(LineBox box, double x, double y, SourceId source)
    {
        Enter(string.Create(
            CultureInfo.InvariantCulture,
            $"line at={N(x)},{N(y)} size={N(box.Width)}x{N(box.Height)}"
                + $" baseline={N(box.Baseline)}{Semantics(source)}"));

        foreach (var run in box.ShapedRuns)
        {
            Run(run, x, y + box.Baseline);
        }

        Leave("/line");
    }

    private void Run(in ShapedTextRun run, double lineX, double baseline)
    {
        var paint = run.Paint;
        var font = paint.Font;
        var x = lineX + run.XOffset;
        var last = run.Fragments[^1];
        var advance = last.XOffset + last.Advance - run.XOffset;
        var style = new StringBuilder();
        if (font.Bold)
        {
            style.Append("bold ");
        }

        if (font.Italic)
        {
            style.Append("italic ");
        }

        if (font.Underline)
        {
            style.Append("underline ");
        }

        if (font.Strikethrough)
        {
            style.Append("strike ");
        }

        Enter(string.Create(
            CultureInfo.InvariantCulture,
            $"run \"{run.GlyphRun.Text}\" at={N(x)},{N(baseline)} advance={N(advance)}"
                + $" family={font.Family} size={N(font.Size)} style={(style.Length == 0 ? "regular" : style.ToString().TrimEnd())}"
                + $" color={Rgba(font.Color)} opacity={N(paint.Opacity)}"
                + $"{Spacing(paint)}"
                + $"{(paint.IsScript ? $" script={N(paint.ScriptScale)}@{N(paint.Rise)}" : string.Empty)}"
                + $"{(paint.Invisible ? " invisible" : string.Empty)}"
                + $"{(run.IsMarker ? " marker" : string.Empty)}"
                + $"{(paint.LinkTarget is { } uri ? $" link={uri}" : string.Empty)}"
                + $"{(paint.AnchorTarget is { } target ? $" linkAnchor={target}" : string.Empty)}"
                + $"{(paint.Anchor is { } anchor ? $" anchor={anchor}" : string.Empty)}"
                + $"{Semantics(run.Source)}"));

        if (paint.InlineImage is { } inline)
        {
            Write(string.Create(
                CultureInfo.InvariantCulture,
                $"inlineImage media={inline.Data.MediaType ?? "-"} size={N(inline.Width)}x{N(inline.Height)}"));
        }

        if (paint.FormField is { } field)
        {
            Enter(string.Create(
                CultureInfo.InvariantCulture,
                $"formField {field.Kind} name={field.Name} value=\"{field.Value}\" label={field.Label ?? "-"}"
                    + $" required={field.Required} chosen={field.Chosen}"
                    + $" size={N(field.Width)}x{N(field.Height)} ascent={N(field.Ascent)}"
                    + $" options=[{(field.Options.IsDefaultOrEmpty ? string.Empty : string.Join("|", field.Options))}]"));
            Clusters(field.ValueGlyphs, x, paint);
            Leave("/formField");
        }

        Clusters(run.GlyphRun, x, paint);
        Leave("/run");
    }

    private static string Spacing(in FragmentPaint paint)
    {
        var described = new StringBuilder();
        if (paint.LetterSpacing != 0 || paint.WordSpacing != 0)
        {
            described.Append(CultureInfo.InvariantCulture,
                $" spacing={N(paint.LetterSpacing)}/{N(paint.WordSpacing)}");
        }

        if (paint.HorizontalScale != 1)
        {
            described.Append(CultureInfo.InvariantCulture, $" scale={N(paint.HorizontalScale)}");
        }

        return described.ToString();
    }

    private void Clusters(in CapturedGlyphRun run, double originX, in FragmentPaint paint)
    {
        if (run.Spans.IsDefaultOrEmpty)
        {
            return;
        }

        foreach (var span in run.Spans)
        {
            var face = span.Face;
            Enter(string.Create(
                CultureInfo.InvariantCulture,
                $"span face={face.Family} bold={face.Bold} italic={face.Italic}"
                    + $" ascent={N(face.Metrics.Ascent)} descent={N(face.Metrics.Descent)} upem={N(face.Metrics.UnitsPerEm)}"
                    + $" x={N(originX + span.XOffset)} advance={N(span.Advance)}"));

            var pen = originX + span.XOffset;
            foreach (var glyph in span.Glyphs)
            {
                Write(string.Create(
                    CultureInfo.InvariantCulture,
                    $"cluster {glyph.Cluster} u+{glyph.Codepoint:X4} x={N(pen)}"
                        + $" advance={N(glyph.Advance)} kerning={N(glyph.Kerning)}"));
                pen += RunTextAdvance.Calculate(
                    glyph.Advance,
                    1,
                    glyph.Codepoint == ' ' ? 1 : 0,
                    paint,
                    trailingCharacterSpacing: true);
            }

            Leave("/span");
        }
    }

    void ISceneVisitor.Image(in LaidOutImage image, in SceneFrame frame)
        => Write(string.Create(
            CultureInfo.InvariantCulture,
            $"image media={image.Paint.MediaType ?? "-"}"
                + $" at={N(frame.Left + image.X)},{N(layerTop + frame.Delta + image.Y)}"
                + $" size={N(image.Width)}x{N(image.Height)}"
                + $" opacity={N(image.Paint.Opacity)} interpolate={image.Paint.Interpolate}"
                + $"{Semantics(image.Source)}"));

    void ISceneVisitor.CodeSymbol(in LaidOutCodeSymbol codeSymbol, in SceneFrame frame)
    {
        var x = frame.Left + codeSymbol.X;
        var y = layerTop + frame.Delta + codeSymbol.Y;
        Enter(string.Create(
            CultureInfo.InvariantCulture,
            $"code modules={codeSymbol.Modules.Length} foreground={Rgba(codeSymbol.Foreground)}"
                + $" at={N(x)},{N(y)}"
                + $" size={N(codeSymbol.Width)}x{N(codeSymbol.Height)}"
                + $"{Semantics(codeSymbol.Source)}"));

        foreach (var module in codeSymbol.Modules)
        {
            Write(string.Create(
                CultureInfo.InvariantCulture,
                $"module at={N(x + module.X)},{N(y + module.Y)}"
                    + $" size={N(module.Width)}x{N(module.Height)}"));
        }

        if (codeSymbol.Caption is { } caption)
        {
            foreach (var line in caption)
            {
                Line(line.Line, x, y + line.Y, codeSymbol.Source);
            }
        }

        Leave("/code");
    }

    private string Style(in BoxStyle style)
    {
        var described = new StringBuilder();
        if (style.Background is { } background)
        {
            described.Append(CultureInfo.InvariantCulture, $" fill={Rgba(background)}");
        }

        if (style.BackgroundGradient is { } gradient)
        {
            described.Append(CultureInfo.InvariantCulture,
                $" gradient={gradient.Kind}({N(gradient.X0)},{N(gradient.Y0)},{N(gradient.R0)}"
                    + $"->{N(gradient.X1)},{N(gradient.Y1)},{N(gradient.R1)})[");
            for (var i = 0; i < gradient.Stops.Length; i++)
            {
                if (i > 0)
                {
                    described.Append(' ');
                }

                described.Append(CultureInfo.InvariantCulture, $"{N(gradient.Stops[i].Offset)}:{Rgba(gradient.Stops[i].Color)}");
            }

            described.Append(']');
        }

        Edge(described, "top", style.Top);
        Edge(described, "right", style.Right);
        Edge(described, "bottom", style.Bottom);
        Edge(described, "left", style.Left);

        if (style.CornerRadius != 0)
        {
            described.Append(CultureInfo.InvariantCulture, $" radius={N(style.CornerRadius)}");
        }

        if (style.Shadow is { } shadow)
        {
            described.Append(CultureInfo.InvariantCulture,
                $" shadow={Rgba(shadow.Color)}@{N(shadow.OffsetX)},{N(shadow.OffsetY)}/{N(shadow.BlurRadius)}/{N(shadow.Spread)}");
        }

        if (style.Blend is { } blend)
        {
            described.Append(CultureInfo.InvariantCulture, $" blend={blend}");
        }

        return described.ToString();
    }

    private static void Edge(StringBuilder described, string name, ResolvedEdge? edge)
    {
        if (edge is { } resolved)
        {
            described.Append(CultureInfo.InvariantCulture,
                $" {name}={Rgba(resolved.Color)}/{N(resolved.Width)}/{resolved.Style}");
        }
    }

    private string Clip(in SceneClip clip)
        => clip.ClipsLines || clip.ClipsInline
            ? string.Create(
                CultureInfo.InvariantCulture,
                $" clip={N(clip.Bounds.X)},{N(layerTop + clip.Bounds.Y)},{N(clip.Bounds.Width)},{N(clip.Bounds.Height)}"
                    + $"{(clip.ClipsLines ? "+lines" : string.Empty)}{(clip.ClipsInline ? "+inline" : string.Empty)}")
            : string.Empty;

    private static string Transform(Matrix? transform)
        => transform is { } matrix
            ? string.Create(
                CultureInfo.InvariantCulture,
                $"{N(matrix.A)},{N(matrix.B)},{N(matrix.C)},{N(matrix.D)},{N(matrix.E)},{N(matrix.F)}")
            : "-";

    void ISceneVisitor.EnterBox(LaidOutBox box, in SceneFrame frame, in SceneClip clip)
        => Enter(string.Create(
            CultureInfo.InvariantCulture,
            $"box at={N(frame.Left + box.Bounds.X)},{N(layerTop + frame.Delta + box.Bounds.Y)}"
                + $" size={N(box.Bounds.Width)}x{N(box.Bounds.Height)} opacity={N(box.Opacity)}"
                + $" transform={Transform(box.Transform)}"
                + $"{Style(box.Style)}{Clip(clip)}{Semantics(box.Source)}"));

    void ISceneVisitor.LeaveBox(LaidOutBox box, in SceneFrame frame) => Leave("/box");

    void ISceneVisitor.EnterFragment(in LaidOutTableFragment fragment, in SceneFrame frame)
        => Enter(string.Create(
            CultureInfo.InvariantCulture,
            $"tableFragment at={N(frame.Left + fragment.Bounds.X)},{N(layerTop + frame.Delta + fragment.Bounds.Y)}"
                + $" size={N(fragment.Bounds.Width)}x{N(fragment.Bounds.Height)} rows={fragment.Rows.Length}"
                + $"{Semantics(fragment.Source)}"));

    void ISceneVisitor.LeaveFragment(in LaidOutTableFragment fragment, in SceneFrame frame) => Leave("/tableFragment");

    void ISceneVisitor.EnterTable(in LaidOutTablePlacement table, in SceneFrame frame)
        => Enter(string.Create(
            CultureInfo.InvariantCulture,
            $"table at={N(frame.Left)},{N(layerTop + frame.Delta)} width={N(table.Layout.Width)}"));

    void ISceneVisitor.LeaveTable(in LaidOutTablePlacement table, in SceneFrame frame) => Leave("/table");

    void ISceneVisitor.EnterRow(in LaidOutRow row, in SceneFrame frame)
        => Enter(string.Create(
            CultureInfo.InvariantCulture,
            $"row {row.SourceRow} header={row.IsHeader} y={N(layerTop + frame.Delta + row.Y)} height={N(row.Height)}"
                + $" background={(row.Background is { } background ? Rgba(background) : "-")} cells={row.Cells.Length}"));

    void ISceneVisitor.LeaveRow(in LaidOutRow row, in SceneFrame frame) => Leave("/row");

    void ISceneVisitor.EnterCell(LaidOutCell cell, in SceneFrame frame, in SceneClip clip)
        => Enter(string.Create(
            CultureInfo.InvariantCulture,
            $"cell {cell.Row},{cell.Column} span={cell.RowSpan}x{cell.ColumnSpan}"
                + $" at={N(frame.Left + cell.Bounds.X)},{N(layerTop + frame.Delta + cell.Bounds.Y)}"
                + $" size={N(cell.Bounds.Width)}x{N(cell.Bounds.Height)} opacity={N(cell.Opacity)}"
                + $"{Style(cell.Decoration)}{Clip(clip)}{Semantics(cell.Source)}"));

    void ISceneVisitor.LeaveCell(LaidOutCell cell, in SceneFrame frame) => Leave("/cell");

    void ISceneVisitor.Link(in LaidOutLink link)
        => Write(string.Create(
            CultureInfo.InvariantCulture,
            $"link rect={N(link.Left)},{N(link.Top)},{N(link.Right)},{N(link.Bottom)}"
                + $" uri={link.Uri ?? "-"} anchor={link.Anchor ?? "-"}"
                + $"{Semantics(link.Source)}"));

    void ISceneVisitor.Anchor(in LaidOutAnchor anchor)
        => Write(string.Create(CultureInfo.InvariantCulture, $"anchor {anchor.Name} top={N(anchor.Top)}"));

    void ISceneVisitor.Watermark(LaidOutWatermark watermark)
    {
        Enter(string.Create(
            CultureInfo.InvariantCulture,
            $"watermark center={N(watermark.CenterX)},{N(watermark.CenterY)}"
                + $" rotation={N(watermark.Rotation)} opacity={N(watermark.Opacity)}"));

        if (watermark.Text is { } text)
        {
            Enter(string.Create(
                CultureInfo.InvariantCulture,
                $"watermarkText \"{text.Text}\" family={text.Font.Family} size={N(text.Size)}"
                    + $" color={Rgba(text.Color)} at={N(text.X)},{N(text.Baseline)}"
                    + $" alpha={(text.AlphaOverride is { } alpha ? N(alpha) : "-")}"));
            Clusters(text.GlyphRun, text.X, Unspaced);
            Leave("/watermarkText");
        }

        if (watermark.Image is { } image)
        {
            Write(string.Create(
                CultureInfo.InvariantCulture,
                $"watermarkImage media={image.Paint.MediaType ?? "-"} at={N(image.X)},{N(image.Y)}"
                    + $" size={N(image.Width)}x{N(image.Height)} alpha={N(image.Alpha)}"));
        }

        Leave("/watermark");
    }
}
