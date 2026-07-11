using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace Radzen.Documents.Pdf;

#nullable enable

/// <summary>
/// An ordered, read-only view of block-level content with typed helpers for appending blocks.
/// </summary>
public class BlockCollection : IReadOnlyList<Block>
{
    private readonly List<Block> items = [];

    /// <inheritdoc/>
    public int Count => items.Count;

    /// <inheritdoc/>
    public Block this[int index] => items[index];

    /// <summary>
    /// Appends an existing block.
    /// </summary>
    /// <typeparam name="T">The block type.</typeparam>
    /// <param name="block">The block to append.</param>
    /// <returns>The same <paramref name="block"/> instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="block"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="block"/> is already in the collection.</exception>
    public T Add<T>(T block)
        where T : Block
    {
        ArgumentNullException.ThrowIfNull(block);

        if (Contains(block))
        {
            throw new ArgumentException("The block is already in the collection.", nameof(block));
        }

        items.Add(block);
        return block;
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

    private bool Contains(Block block)
    {
        foreach (var item in items)
        {
            if (ReferenceEquals(item, block))
            {
                return true;
            }
        }

        return false;
    }

    internal void Clear() => items.Clear();

    /// <inheritdoc/>
    public IEnumerator<Block> GetEnumerator() => items.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
