using System;

namespace Radzen.Documents;

internal static class ContentTree
{
    public static void Attach(Block block, BlockCollection owner)
    {
        ArgumentNullException.ThrowIfNull(block);
        EnsureUnowned(block.Owner, "block");
        EnsureAcyclic(block, owner, "block");
        block.Owner = owner;
    }

    public static void Attach(Inline inline, InlineCollection owner)
    {
        ArgumentNullException.ThrowIfNull(inline);
        EnsureUnowned(inline.Owner, "inline");
        inline.Owner = owner;
    }

    public static void Attach(Section section, SectionCollection owner)
    {
        ArgumentNullException.ThrowIfNull(section);
        EnsureUnowned(section.Owner, "section");
        section.Owner = owner;
    }

    public static void Attach(Row row, RowCollection owner)
    {
        ArgumentNullException.ThrowIfNull(row);
        EnsureUnowned(row.Owner, "row");
        EnsureAcyclic(row, owner, "row");
        row.Owner = owner;
    }

    public static void Attach(Column column, ColumnCollection owner)
    {
        ArgumentNullException.ThrowIfNull(column);
        EnsureUnowned(column.Owner, "column");
        column.Owner = owner;
    }

    public static void Attach(TocEntry entry, TocEntryCollection owner)
    {
        ArgumentNullException.ThrowIfNull(entry);
        EnsureUnowned(entry.Owner, "table of contents entry");
        entry.Owner = owner;
    }

    public static void Attach(ListItem item, ListItemCollection owner)
    {
        ArgumentNullException.ThrowIfNull(item);
        EnsureUnowned(item.Owner, "list item");
        EnsureAcyclic(item, owner, "list item");
        item.Owner = owner;
    }

    public static void Detach(Block block) => block.Owner = null;

    public static void Detach(Inline inline) => inline.Owner = null;

    public static void Detach(Section section) => section.Owner = null;

    public static void Detach(Row row) => row.Owner = null;

    public static void Detach(Column column) => column.Owner = null;

    public static void Detach(ListItem item) => item.Owner = null;

    public static void Detach(TocEntry entry) => entry.Owner = null;

    private static void EnsureUnowned(object? owner, string noun)
    {
        if (owner is not null)
        {
            throw new InvalidOperationException(
                $"The {noun} already belongs to a document tree. Remove it from its current parent before adding it here.");
        }
    }

    private static void EnsureAcyclic(object node, object owner, string noun)
    {
        if (Reaches(node, owner))
        {
            throw new InvalidOperationException(
                $"The {noun} cannot be added to itself or to anything it already contains; that would make the document tree cyclic.");
        }
    }

    private static bool Reaches(object? node, object target)
    {
        if (node is null)
        {
            return false;
        }

        if (ReferenceEquals(node, target))
        {
            return true;
        }

        switch (node)
        {
            case Paragraph paragraph:
                return ReferenceEquals(paragraph.Inlines, target);
            case Container container:
                return Reaches(container.Blocks, target);
            case Table table:
                return ReferenceEquals(table.Columns, target) || Reaches(table.Rows, target);
            case ListBlock list:
                return Reaches(list.Items, target);
            case Row row:
                return Reaches(row.Cells, target);
            case Cell cell:
                return Reaches(cell.Blocks, target);
            case ListItem item:
                return Reaches(item.Blocks, target);
            case Section section:
                return Reaches(section.Blocks, target)
                    || Reaches(section.Header.Blocks, target)
                    || Reaches(section.Footer.Blocks, target);
            case BlockCollection blocks:
                foreach (var block in blocks)
                {
                    if (Reaches(block, target))
                    {
                        return true;
                    }
                }

                return false;
            case RowCollection rows:
                foreach (var row in rows)
                {
                    if (Reaches(row, target))
                    {
                        return true;
                    }
                }

                return false;
            case CellCollection cells:
                foreach (var cell in cells)
                {
                    if (Reaches(cell, target))
                    {
                        return true;
                    }
                }

                return false;
            case ListItemCollection items:
                foreach (var item in items)
                {
                    if (Reaches(item, target))
                    {
                        return true;
                    }
                }

                return false;
            default:
                return false;
        }
    }
}
