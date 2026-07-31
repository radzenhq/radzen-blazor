using System.Collections.Generic;
using System.Collections.Immutable;
using System;
using Radzen.Documents.Fonts;
using Radzen.Documents.LaidOut;

namespace Radzen.Documents.Layout;

internal static class LayoutFinalizer
{
    public static LaidOutDocument Resolve(
        LaidOutDocument document,
        FontCollection fonts,
        LoweringResult resolution,
        LayoutCaptureContext capture)
    {
        var resolver = new FieldResolver(fonts, resolution, capture);
        var count = document.Pages.Length;
        var pages = ImmutableArray.CreateBuilder<LaidOutPage>(count);
        for (var index = 0; index < count; index++)
        {
            var page = document.Pages[index];
            var state = new PageState(resolver, resolution, capture, index + 1, count);
            var width = page.ContentBox.Width;

            var body = state.Layer(page.Body, width);
            var header = state.Layer(page.HeaderLayer, width);
            var footer = state.Layer(page.FooterLayer, width);

            if (body is not null || header is not null || footer is not null)
            {
                page = page with
                {
                    Body = body ?? page.Body,
                    HeaderLayer = header ?? page.HeaderLayer,
                    FooterLayer = footer ?? page.FooterLayer,
                };
            }

            pages.Add(PageNavigationCollector.Collect(page));
        }

        return new LaidOutDocument
        {
            Id = document.Id,
            Fonts = document.Fonts,
            Pages = pages.MoveToImmutable(),
            Semantics = document.Semantics,
            Info = document.Info,
        };
    }

    private sealed class PageState(
        FieldResolver resolver,
        LoweringResult resolution,
        LayoutCaptureContext capture,
        int pageNumber,
        int pageCount)
    {
        public LaidOutLayer? Layer(LaidOutLayer layer, double width)
        {
            if (ResolveContent(layer, width, Tables) is not { } resolved)
            {
                return null;
            }

            return layer with
            {
                Lines = resolved.Lines ?? layer.Lines,
                Tables = resolved.Tables ?? layer.Tables,
                Boxes = resolved.Boxes ?? layer.Boxes,
            };
        }

        private ImmutableArray<LaidOutBox>? Boxes(ImmutableArray<LaidOutBox> boxes)
        {
            ImmutableArray<LaidOutBox>.Builder? result = null;
            for (var i = 0; i < boxes.Length; i++)
            {
                if (Content(
                    boxes[i].Content,
                    InnerWidth(capture.Resolve<Container>(boxes[i].Source), boxes[i].Bounds.Width))
                    is not { } content)
                {
                    continue;
                }

                result ??= boxes.ToBuilder();
                result[i] = boxes[i] with { Content = content };
            }

            return result?.ToImmutable();
        }

        private ImmutableArray<LaidOutTableFragment>? Tables(ImmutableArray<LaidOutTableFragment> tables)
        {
            ImmutableArray<LaidOutTableFragment>.Builder? result = null;
            for (var i = 0; i < tables.Length; i++)
            {
                if (Table(tables[i].Layout) is not { } layout)
                {
                    continue;
                }

                result ??= tables.ToBuilder();
                result[i] = TableFragmentJoin.Rejoin(tables[i], layout);
            }

            return result?.ToImmutable();
        }

        private LaidOutTable? Table(LaidOutTable table)
        {
            ImmutableArray<LaidOutCell>.Builder? cells = null;
            for (var i = 0; i < table.Cells.Length; i++)
            {
                if (Cell(table.Cells[i]) is not { } cell)
                {
                    continue;
                }

                cells ??= table.Cells.ToBuilder();
                cells[i] = cell;
            }

            return cells is null
                ? null
                : new LaidOutTable
                {
                    Id = table.Id,
                    ColumnWidths = table.ColumnWidths,
                    RowHeights = table.RowHeights,
                    Width = table.Width,
                    Height = table.Height,
                    Cells = cells.ToImmutable(),
                    Decoration = table.Decoration,
                    Source = table.Source,
                };
        }

        private LaidOutCell? Cell(LaidOutCell cell)
        {
            if (ResolveContent(cell, cell.ContentBox.Width, NestedTables) is not { } resolved)
            {
                return null;
            }

            return cell with
            {
                Lines = resolved.Lines ?? cell.Lines,
                Tables = resolved.Tables ?? cell.Tables,
                Boxes = resolved.Boxes ?? cell.Boxes,
            };
        }

        private LaidOutBoxContent? Content(in LaidOutBoxContent content, double width)
        {
            if (ResolveContent(content, width, NestedTables) is not { } resolved)
            {
                return null;
            }

            return new LaidOutBoxContent
            {
                Height = content.Height,
                Lines = resolved.Lines ?? content.Lines,
                Images = content.Images,
                CodeSymbols = content.CodeSymbols,
                Tables = resolved.Tables ?? content.Tables,
                Boxes = resolved.Boxes ?? content.Boxes,
            };
        }

        private ContentResolution<TTable>? ResolveContent<TTable>(
            ILaidOutContent<TTable> content,
            double width,
            Func<ImmutableArray<TTable>, ImmutableArray<TTable>?> resolveTables)
        {
            var lines = Lines(content.Lines, width);
            var tables = resolveTables(content.Tables);
            var boxes = Boxes(content.Boxes);
            return lines is null && tables is null && boxes is null
                ? null
                : new ContentResolution<TTable>(lines, tables, boxes);
        }

        private readonly record struct ContentResolution<TTable>(
            ImmutableArray<LaidOutLine>? Lines,
            ImmutableArray<TTable>? Tables,
            ImmutableArray<LaidOutBox>? Boxes);

        private ImmutableArray<LaidOutTablePlacement>? NestedTables(ImmutableArray<LaidOutTablePlacement> tables)
        {
            ImmutableArray<LaidOutTablePlacement>.Builder? result = null;
            for (var i = 0; i < tables.Length; i++)
            {
                if (Table(tables[i].Layout) is not { } layout)
                {
                    continue;
                }

                result ??= tables.ToBuilder();
                result[i] = tables[i] with { Layout = layout };
            }

            return result?.ToImmutable();
        }

        private static double InnerWidth(Container container, double boundsWidth)
            => Math.Max(0, boundsWidth - container.EffectivePadding.Horizontal);

        private ImmutableArray<LaidOutLine>? Lines(ImmutableArray<LaidOutLine> lines, double width)
        {
            if (!HasField(lines))
            {
                return null;
            }

            var result = ImmutableArray.CreateBuilder<LaidOutLine>(lines.Length);
            var i = 0;
            while (i < lines.Length)
            {
                var current = lines[i];
                if (FieldParagraph(current) is { } paragraph)
                {
                    var reserved = Reserved(lines, i);
                    var y = current.Y;
                    foreach (var box in Break(paragraph, width, reserved))
                    {
                        result.Add(current with { Line = box, Y = y });
                        y += box.Height;
                    }

                    i += reserved;
                }
                else
                {
                    result.Add(current);
                    i++;
                }
            }

            return result.ToImmutable();
        }

        private IReadOnlyList<LineBox> Break(Paragraph paragraph, double width, int reserved)
            => resolver.ResolveFields(paragraph, width, pageNumber, pageCount, resolution.Alignment(paragraph), reserved);

        private static int Reserved(ImmutableArray<LaidOutLine> lines, int start)
        {
            var source = lines[start].Source;
            var reserved = 0;
            while (start + reserved < lines.Length && lines[start + reserved].Source == source)
            {
                reserved++;
            }

            return reserved;
        }

        private Paragraph? FieldParagraph(in LaidOutLine line)
            => resolver.ParagraphWithFields(capture.Resolve<Block>(line.Source));

        private bool HasField(ImmutableArray<LaidOutLine> lines)
        {
            foreach (var line in lines)
            {
                if (FieldParagraph(line) is not null)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
