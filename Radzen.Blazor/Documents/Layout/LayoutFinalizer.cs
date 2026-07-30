using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Radzen.Documents.Fonts;
using Radzen.Documents.Geometry;

namespace Radzen.Documents.Layout;

internal static class LayoutFinalizer
{
    public static LaidOutDocument Resolve(
        LaidOutDocument document,
        FontCollection fonts,
        LoweringContext resolution,
        LayoutCaptureContext capture)
    {
        var resolver = new FieldResolver(fonts, resolution, capture);
        var count = document.Pages.Length;
        var pages = ImmutableArray.CreateBuilder<PaginatedPage>(count);
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

        return LaidOutIdentitySplitter.Split(new LaidOutDocument
        {
            Fonts = document.Fonts,
            Pages = pages.MoveToImmutable(),
            Semantics = document.Semantics,
            Info = document.Info,
        });
    }

    private sealed class PageState(
        FieldResolver resolver,
        LoweringContext resolution,
        LayoutCaptureContext capture,
        int pageNumber,
        int pageCount)
    {
        public PageLayer? Layer(PageLayer layer, double width)
        {
            var lines = Lines(layer.Lines, width);
            var boxes = Boxes(layer.Boxes);
            var tables = Tables(layer.Tables);
            if (lines is null && boxes is null && tables is null)
            {
                return null;
            }

            return layer with
            {
                Lines = lines ?? layer.Lines,
                Tables = tables ?? layer.Tables,
                Boxes = boxes ?? layer.Boxes,
            };
        }

        private ImmutableArray<PositionedBox>? Boxes(ImmutableArray<PositionedBox> boxes)
        {
            ImmutableArray<PositionedBox>.Builder? result = null;
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

        private ImmutableArray<PositionedTableFragment>? Tables(ImmutableArray<PositionedTableFragment> tables)
        {
            ImmutableArray<PositionedTableFragment>.Builder? result = null;
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
            var lines = Lines(cell.Lines, cell.ContentBox.Width);
            var tables = NestedTables(cell.Tables);
            var boxes = NestedBoxes(cell.Boxes);
            if (lines is null && tables is null && boxes is null)
            {
                return null;
            }

            return cell with
            {
                Lines = lines ?? cell.Lines,
                Tables = tables ?? cell.Tables,
                Boxes = boxes ?? cell.Boxes,
            };
        }

        private LaidOutBoxContent? Content(in LaidOutBoxContent content, double width)
        {
            var lines = Lines(content.Lines, width);
            var tables = NestedTables(content.Tables);
            var boxes = NestedBoxes(content.Boxes);
            if (lines is null && tables is null && boxes is null)
            {
                return null;
            }

            return new LaidOutBoxContent
            {
                Height = content.Height,
                Lines = lines ?? content.Lines,
                Images = content.Images,
                CodeSymbols = content.CodeSymbols,
                Tables = tables ?? content.Tables,
                Boxes = boxes ?? content.Boxes,
            };
        }

        private ImmutableArray<LaidOutNestedTable>? NestedTables(ImmutableArray<LaidOutNestedTable> tables)
        {
            ImmutableArray<LaidOutNestedTable>.Builder? result = null;
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

        private ImmutableArray<LaidOutNestedBox>? NestedBoxes(ImmutableArray<LaidOutNestedBox> boxes)
        {
            ImmutableArray<LaidOutNestedBox>.Builder? result = null;
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

        private static double InnerWidth(Container container, double boundsWidth)
            => Math.Max(0, boundsWidth - (2 * container.Padding.Point));

        private ImmutableArray<PositionedLine>? Lines(ImmutableArray<PositionedLine> lines, double width)
            => LinesCore(
                lines,
                width,
                static line => line.Source,
                static line => line.Y,
                static (current, box, y) => new PositionedLine
                {
                    Line = box,
                    Source = current.Source,
                    Y = y,
                });

        private ImmutableArray<LaidOutLine>? Lines(ImmutableArray<LaidOutLine> lines, double width)
            => LinesCore(
                lines,
                width,
                static line => line.Source,
                static line => line.Y,
                static (current, box, y) => new LaidOutLine
                {
                    Line = box,
                    Source = current.Source,
                    X = current.X,
                    Y = y,
                });

        private ImmutableArray<T>? LinesCore<T>(
            ImmutableArray<T> lines,
            double width,
            Func<T, SourceId> sourceOf,
            Func<T, double> yOf,
            Func<T, LineBox, double, T> replace)
        {
            if (!HasField(lines, sourceOf))
            {
                return null;
            }

            var result = ImmutableArray.CreateBuilder<T>(lines.Length);
            var i = 0;
            while (i < lines.Length)
            {
                var current = lines[i];
                var source = sourceOf(current);
                var paragraph = capture.Resolve<Block>(source) as Paragraph;
                if (paragraph is not null && resolver.HasField(paragraph))
                {
                    var reserved = Reserved(lines, i, sourceOf);
                    var y = yOf(current);
                    foreach (var box in Break(paragraph, width, reserved))
                    {
                        result.Add(replace(current, box, y));
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

        private static int Reserved<T>(ImmutableArray<T> lines, int start, Func<T, SourceId> sourceOf)
        {
            var source = sourceOf(lines[start]);
            var reserved = 0;
            while (start + reserved < lines.Length && sourceOf(lines[start + reserved]) == source)
            {
                reserved++;
            }

            return reserved;
        }

        private bool HasField<T>(ImmutableArray<T> lines, Func<T, SourceId> sourceOf)
        {
            foreach (var line in lines)
            {
                if (capture.Resolve<Block>(sourceOf(line)) is Paragraph paragraph
                    && resolver.HasField(paragraph))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
