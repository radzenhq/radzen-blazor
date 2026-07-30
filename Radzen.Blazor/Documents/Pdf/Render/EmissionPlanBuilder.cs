using System.Collections.Generic;
using System.Collections.Immutable;
using Radzen.Documents.Pdf.Emission;

namespace Radzen.Documents.Pdf.Render;

internal sealed class EmissionPlanBuilder
{
    private readonly Dictionary<StructureElement, int> structureIds = [];
    private readonly Dictionary<StructureElement, StructureElementSnapshot> structures = [];
    private readonly Dictionary<GeneratedFont, EmissionFont> fonts = [];
    private readonly Dictionary<GeneratedImage, EmissionImage> images = [];

    private ImmutableArray<PageEmissionPlan> renderedPages;

    public DocumentEmissionPlan Build(
        IReadOnlyList<GeneratedPage> pages,
        StructureElement structure,
        IReadOnlyDictionary<string, GeneratedAnchor> anchors,
        RoleMap roleMap)
    {
        AssignStructureIds(structure);

        var pageSnapshots = ImmutableArray.CreateBuilder<PageEmissionPlan>(pages.Count);
        foreach (var page in pages)
        {
            pageSnapshots.Add(CapturePage(page));
        }

        renderedPages = pageSnapshots.MoveToImmutable();
        var structureSnapshot = CaptureStructure(structure);

        var anchorSnapshots = ImmutableDictionary.CreateBuilder<string, EmissionAnchor>(System.StringComparer.Ordinal);
        foreach (var anchor in anchors)
        {
            anchorSnapshots.Add(anchor.Key, new EmissionAnchor(renderedPages[anchor.Value.PageIndex], anchor.Value.Top));
        }

        return new DocumentEmissionPlan(
            renderedPages,
            structureSnapshot,
            anchorSnapshots.ToImmutable(),
            [.. roleMap.Entries]);
    }

    private void AssignStructureIds(StructureElement element)
    {
        if (structureIds.ContainsKey(element))
        {
            return;
        }

        structureIds.Add(element, structureIds.Count);
        foreach (var child in element.Children)
        {
            AssignStructureIds(child);
        }
    }

    private StructureElementSnapshot CaptureStructure(StructureElement element)
    {
        if (structures.TryGetValue(element, out var existing))
        {
            return existing;
        }

        var children = ImmutableArray.CreateBuilder<StructureElementSnapshot>(element.Children.Count);
        foreach (var child in element.Children)
        {
            children.Add(CaptureStructure(child));
        }

        var kids = ImmutableArray.CreateBuilder<StructureKidSnapshot>(element.Kids.Count);
        foreach (var kid in element.Kids)
        {
            kids.Add(new StructureKidSnapshot(
                kid.Child is { } child ? CaptureStructure(child) : null,
                kid.Child is null ? renderedPages[kid.PageIndex] : null,
                kid.Mcid));
        }

        var marks = ImmutableArray.CreateBuilder<(PageEmissionPlan Page, int Mcid)>(element.Marks.Count);
        foreach (var (pageIndex, mcid) in element.Marks)
        {
            marks.Add((renderedPages[pageIndex], mcid));
        }

        var snapshot = new StructureElementSnapshot(
            structureIds[element],
            element.Type,
            element.Alt,
            element.ActualText,
            element.HeaderScope,
            element.RowSpan,
            element.ColumnSpan,
            children.MoveToImmutable(),
            marks.MoveToImmutable(),
            kids.MoveToImmutable());
        structures.Add(element, snapshot);
        return snapshot;
    }

    private PageEmissionPlan CapturePage(GeneratedPage page)
    {
        var pageFonts = ImmutableArray.CreateBuilder<EmissionFont>(page.Fonts.Count);
        foreach (var font in page.Fonts)
        {
            pageFonts.Add(CaptureFont(font));
        }

        var pageImages = ImmutableArray.CreateBuilder<EmissionImage>(page.Images.Count);
        foreach (var image in page.Images)
        {
            pageImages.Add(CaptureImage(image));
        }

        var links = ImmutableArray.CreateBuilder<EmissionLink>(page.Links.Count);
        foreach (var link in page.Links)
        {
            links.Add(new EmissionLink(
                link.X1,
                link.Y1,
                link.X2,
                link.Y2,
                link.Uri,
                link.Destination,
                link.Element is { } element ? structureIds[element] : null));
        }

        var states = ImmutableArray.CreateBuilder<EmissionExtGState>(page.ExtGStates.Count);
        foreach (var state in page.ExtGStates)
        {
            states.Add(new EmissionExtGState(
                state.Key,
                state.FillAlpha,
                state.StrokeAlpha,
                state.Blend,
                state.SoftMask is { } mask ? CaptureSoftMask(mask) : null,
                state.ClearSoftMask));
        }

        var patterns = ImmutableArray.CreateBuilder<EmissionPattern>(page.Patterns.Count);
        foreach (var pattern in page.Patterns)
        {
            patterns.Add(new EmissionPattern(
                pattern.Key,
                EmissionDictionarySnapshot.Capture(pattern.Pattern)));
        }

        return new PageEmissionPlan(
            page.Content,
            pageFonts.MoveToImmutable(),
            pageImages.MoveToImmutable(),
            links.MoveToImmutable(),
            states.MoveToImmutable(),
            patterns.MoveToImmutable());
    }

    private EmissionFont CaptureFont(GeneratedFont font)
    {
        if (fonts.TryGetValue(font, out var existing))
        {
            return existing;
        }

        var snapshot = new EmissionFont(
            font.Key,
            font.Base14,
            font.Sfnt,
            font.GidToUnicode.ToImmutableDictionary(),
            font.CompactGidMap?.ToImmutableDictionary(),
            font.Extraction
                ?? throw new System.InvalidOperationException("The extraction font must be built before the emission plan is captured."));
        fonts.Add(font, snapshot);
        return snapshot;
    }

    private EmissionImage CaptureImage(GeneratedImage image)
    {
        if (images.TryGetValue(image, out var existing))
        {
            return existing;
        }

        var snapshot = new EmissionImage(
            image.Key,
            EmissionStreamSnapshot.Capture(image.Image.Image),
            image.Image.SoftMask is { } mask ? EmissionStreamSnapshot.Capture(mask) : null);
        images.Add(image, snapshot);
        return snapshot;
    }

    private static EmissionSoftMask CaptureSoftMask(GeneratedSoftMask mask)
    {
        var xObjects = ImmutableArray.CreateBuilder<KeyValuePair<string, EmissionStreamSnapshot>>(mask.Group.XObjects.Count);
        foreach (var xObject in mask.Group.XObjects)
        {
            xObjects.Add(new KeyValuePair<string, EmissionStreamSnapshot>(
                xObject.Key,
                EmissionStreamSnapshot.Capture(xObject.Value)));
        }

        return new EmissionSoftMask(
            mask.Type == SoftMaskType.Luminosity
                ? EmissionSoftMaskType.Luminosity
                : EmissionSoftMaskType.Alpha,
            new EmissionTransparencyGroup(
                mask.Group.Content,
                [.. mask.Group.BBox],
                mask.Group.ColorSpace,
                mask.Group.Isolated,
                mask.Group.Knockout,
                xObjects.MoveToImmutable()),
            mask.Backdrop is { } backdrop ? [.. backdrop] : null);
    }
}
