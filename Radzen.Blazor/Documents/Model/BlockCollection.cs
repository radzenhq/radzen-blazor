using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Radzen.Documents.Codes;

namespace Radzen.Documents;


/// <summary>
/// An ordered collection of block-level content with typed helpers for appending blocks.
/// A block belongs to exactly one collection: adding a block that already has a parent throws,
/// as does adding a block to itself or to anything it contains.
/// </summary>
public sealed class BlockCollection : IReadOnlyList<Block>
{
    private readonly TrackedList<Block> items = [];
    private readonly HashSet<Block> membership = new(ReferenceEqualityComparer.Instance);

    /// <inheritdoc/>
    public int Count => items.Count;

    /// <inheritdoc/>
    public Block this[int index] => items[index];

    internal bool StructureChanged => items.StructureChanged;

    internal void AcceptStructure() => items.AcceptStructure();

    /// <summary>
    /// Appends an existing block.
    /// </summary>
    /// <typeparam name="T">The block type.</typeparam>
    /// <param name="block">The block to append.</param>
    /// <returns>The same <paramref name="block"/> instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="block"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">
    /// <paramref name="block"/> already belongs to a collection, or appending it would make
    /// the document tree cyclic.
    /// </exception>
    public T Add<T>(T block)
        where T : Block
    {
        Own(block);
        membership.Add(block);
        items.Add(block);
        return block;
    }

    /// <summary>
    /// Inserts an existing block at the specified position.
    /// </summary>
    /// <typeparam name="T">The block type.</typeparam>
    /// <param name="index">The zero-based position to insert at, from 0 to <see cref="Count"/>.</param>
    /// <param name="block">The block to insert.</param>
    /// <returns>The same <paramref name="block"/> instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="block"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is out of range.</exception>
    /// <exception cref="InvalidOperationException">
    /// <paramref name="block"/> already belongs to a collection, or inserting it would make
    /// the document tree cyclic.
    /// </exception>
    public T Insert<T>(int index, T block)
        where T : Block
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(index, items.Count);
        Own(block);
        membership.Add(block);
        items.Insert(index, block);
        return block;
    }

    /// <summary>
    /// Removes the specified block, detaching it so it may be added elsewhere.
    /// </summary>
    /// <param name="block">The block to remove.</param>
    /// <returns><see langword="true"/> if the block was in the collection; otherwise <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="block"/> is <see langword="null"/>.</exception>
    public bool Remove(Block block)
    {
        ArgumentNullException.ThrowIfNull(block);

        if (!membership.Remove(block))
        {
            return false;
        }

        items.Remove(block);
        ContentTree.Detach(block);
        return true;
    }

    /// <summary>
    /// Removes the block at the specified position, detaching it so it may be added elsewhere.
    /// </summary>
    /// <param name="index">The zero-based index of the block to remove.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is out of range.</exception>
    public void RemoveAt(int index)
    {
        var block = items[index];
        items.RemoveAt(index);
        membership.Remove(block);
        ContentTree.Detach(block);
    }

    /// <summary>
    /// Moves the block at <paramref name="fromIndex"/> to <paramref name="toIndex"/>, shifting the
    /// blocks in between.
    /// </summary>
    /// <param name="fromIndex">The zero-based index of the block to move.</param>
    /// <param name="toIndex">The zero-based index the block ends up at.</param>
    /// <exception cref="ArgumentOutOfRangeException">Either index is out of range.</exception>
    public void Move(int fromIndex, int toIndex) => items.Move(fromIndex, toIndex);

    internal static BlockCollection Borrowing(Block block)
    {
        var collection = new BlockCollection();
        collection.membership.Add(block);
        collection.items.Add(block);
        return collection;
    }

    private void Own<T>(T block)
        where T : Block
    {
        ArgumentNullException.ThrowIfNull(block);

        if (membership.Contains(block))
        {
            throw new InvalidOperationException(
                "The block already belongs to a document tree. Remove it from its current parent before adding it here.");
        }

        ContentTree.Attach(block, this);
    }

    /// <summary>
    /// Appends a paragraph containing the specified text.
    /// </summary>
    /// <param name="text">The paragraph text.</param>
    /// <returns>The newly created paragraph.</returns>
    public Paragraph Add(string text) => AddParagraph(text);

    /// <summary>
    /// Appends an empty paragraph.
    /// </summary>
    /// <returns>The newly created paragraph.</returns>
    public Paragraph AddParagraph() => Add(new Paragraph());

    /// <summary>
    /// Appends a paragraph containing the specified text.
    /// </summary>
    /// <param name="text">The paragraph text.</param>
    /// <returns>The newly created paragraph.</returns>
    public Paragraph AddParagraph(string text)
    {
        var paragraph = new Paragraph { Text = text };
        return Add(paragraph);
    }

    /// <summary>
    /// Appends an empty table.
    /// </summary>
    /// <returns>The newly created table.</returns>
    public Table AddTable() => Add(new Table());

    /// <summary>
    /// Appends an empty table of contents.
    /// </summary>
    /// <returns>The newly created table of contents.</returns>
    public TableOfContents AddTableOfContents() => Add(new TableOfContents());

    /// <summary>
    /// Appends a page break.
    /// </summary>
    /// <returns>The newly created page break.</returns>
    public PageBreak AddPageBreak() => Add(new PageBreak());

    /// <summary>
    /// Appends an empty list.
    /// </summary>
    /// <param name="style">The marker style. Defaults to <see cref="ListStyle.Bullet"/>.</param>
    /// <returns>The newly created list.</returns>
    public List AddList(ListStyle style = ListStyle.Bullet) => Add(new List { Style = style });

    /// <summary>
    /// Appends an image, buffering the bytes from the specified stream.
    /// </summary>
    /// <param name="stream">The source image stream.</param>
    /// <returns>The newly created image.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="stream"/> is <see langword="null"/>.</exception>
    public Image AddImage(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        return Add(Image.FromStream(stream));
    }

    /// <summary>
    /// Appends a QR code rendered as vector squares.
    /// </summary>
    /// <param name="value">The text to encode.</param>
    /// <param name="size">The rendered width and height of the code, quiet zone included.</param>
    /// <param name="errorCorrection">The error-correction level. Defaults to <see cref="QrErrorCorrection.Medium"/>.</param>
    /// <returns>The newly created QR code.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    public QrCode AddQrCode(string value, Unit size, QrErrorCorrection errorCorrection = QrErrorCorrection.Medium)
    {
        ArgumentNullException.ThrowIfNull(value);
        return Add(new QrCode(value, size) { ErrorCorrection = errorCorrection });
    }

    /// <summary>
    /// Appends a 1D barcode rendered as vector bars.
    /// </summary>
    /// <param name="type">The barcode symbology.</param>
    /// <param name="value">The value to encode.</param>
    /// <param name="width">The rendered width of the bars.</param>
    /// <param name="height">The rendered height of the bars.</param>
    /// <param name="showText">Whether the human-readable value is drawn centered below the bars.</param>
    /// <returns>The newly created barcode.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    public Barcode AddBarcode(BarcodeType type, string value, Unit width, Unit height, bool showText = false)
    {
        ArgumentNullException.ThrowIfNull(value);
        return Add(new Barcode(type, value, width, height) { ShowText = showText });
    }

    /// <summary>
    /// Removes every block, detaching each one so it may be added elsewhere.
    /// </summary>
    public void Clear()
    {
        foreach (var block in items)
        {
            ContentTree.Detach(block);
        }

        items.Clear();
        membership.Clear();
    }

    /// <inheritdoc/>
    public IEnumerator<Block> GetEnumerator() => items.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
