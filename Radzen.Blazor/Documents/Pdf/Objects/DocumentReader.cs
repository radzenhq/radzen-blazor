using Radzen.Documents.Pdf.Objects.Encryption;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Radzen.Documents.Pdf.Objects;

/// <summary>
/// Reads a PDF file (ISO 32000-1 section 7.5): it locates the last
/// cross-reference section via <c>startxref</c>, follows any <c>/Prev</c> chain of
/// incremental updates - classic tables, cross-reference streams, or a mix - and
/// parses indirect objects (including objects compressed inside object streams) on
/// demand. When the cross-reference machinery is unusable the reader falls back to
/// scanning the file for <c>N G obj</c> headers and reconstructing the trailer.
/// </summary>
public sealed class DocumentReader
{
    private readonly ReaderLimits limits;

    // The resource limits this reader enforces, so read-path helpers (font/ToUnicode
    // reconstruction) honor a caller-tightened budget instead of ReaderLimits.Default.
    internal ReaderLimits Limits => limits;
    private DictionaryObject trailer = new();
    private readonly StreamDecoder decoder;
    private readonly XrefLoader xrefLoader;
    private readonly IndirectObjectStore store;
    private readonly DocumentRepairer repairer;

    private DocumentReader(byte[] data, ReaderLimits limits)
    {
        this.limits = limits;
        decoder = new StreamDecoder(limits, Resolve);
        repairer = new DocumentRepairer(data, limits);
        xrefLoader = new XrefLoader(data, limits, decoder);
        store = new IndirectObjectStore(data, limits, xrefLoader.Entries, decoder, repairer);
    }

    /// <summary>
    /// Gets the trailer dictionary of the most recent cross-reference section. For
    /// a cross-reference stream this is the stream's own dictionary.
    /// </summary>
    public DictionaryObject Trailer => trailer;

    /// <summary>
    /// Gets the number of in-use (non-free) objects across the merged
    /// cross-reference sections, including objects stored in object streams.
    /// </summary>
    public int ObjectCount
    {
        get
        {
            var count = 0;
            foreach (var entry in xrefLoader.Entries.Values)
            {
                if (entry.InUse)
                {
                    count++;
                }
            }

            return count;
        }
    }

    /// <summary>
    /// Gets a value indicating whether the document is encrypted (its trailer
    /// carries an <c>/Encrypt</c> entry and a security handler was constructed).
    /// </summary>
    public bool IsEncrypted => store.IsEncrypted;

    internal int GenerationOf(int number) => store.GenerationOf(number);

    /// <summary>
    /// Parses a PDF document from a byte array.
    /// </summary>
    /// <param name="data">The complete document bytes.</param>
    /// <returns>A reader positioned over the parsed cross-reference tables.</returns>
    public static DocumentReader Parse(byte[] data) => Parse(data, null);

    /// <summary>
    /// Parses a PDF document from a byte array, supplying a password for an
    /// encrypted document. Opening an encrypted document whose user and owner
    /// passwords both reject the supplied password throws
    /// <see cref="InvalidPasswordException"/>.
    /// </summary>
    /// <param name="data">The complete document bytes.</param>
    /// <param name="password">The user or owner password, or <c>null</c>/empty for none.</param>
    /// <returns>A reader positioned over the parsed cross-reference tables.</returns>
    public static DocumentReader Parse(byte[] data, string? password) => Parse(data, password, ReaderLimits.Default);

    /// <summary>
    /// Parses a PDF document from a byte array, supplying a password for an
    /// encrypted document and the resource limits to enforce while reading.
    /// </summary>
    /// <param name="data">The complete document bytes.</param>
    /// <param name="password">The user or owner password, or <c>null</c>/empty for none.</param>
    /// <param name="limits">The resource limits to enforce while reading.</param>
    /// <returns>A reader positioned over the parsed cross-reference tables.</returns>
    public static DocumentReader Parse(byte[] data, string? password, ReaderLimits limits)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(limits);
        var snapshot = limits.Snapshot();
        if (data.LongLength > snapshot.MaxFileBytes)
        {
            throw new DocumentParseException("Maximum file size exceeded.", -1);
        }

        var reader = new DocumentReader(data, snapshot);
        reader.Load();
        reader.InitializeSecurity(password);
        return reader;
    }

    /// <summary>
    /// Parses a PDF document from a stream. The stream is fully read into memory.
    /// </summary>
    /// <param name="stream">The source stream.</param>
    /// <returns>A reader positioned over the parsed cross-reference tables.</returns>
    public static DocumentReader Parse(Stream stream) => Parse(stream, null);

    /// <summary>
    /// Parses a PDF document from a stream, supplying a password for an encrypted
    /// document. The stream is fully read into memory.
    /// </summary>
    /// <param name="stream">The source stream.</param>
    /// <param name="password">The user or owner password, or <c>null</c>/empty for none.</param>
    /// <returns>A reader positioned over the parsed cross-reference tables.</returns>
    public static DocumentReader Parse(Stream stream, string? password)
        => Parse(stream, password, ReaderLimits.Default);

    /// <summary>
    /// Parses a PDF document from a stream with resource limits.
    /// </summary>
    /// <param name="stream">The source stream.</param>
    /// <param name="password">The user or owner password, or <c>null</c>/empty for none.</param>
    /// <param name="limits">The resource limits to enforce while reading.</param>
    /// <returns>A reader positioned over the parsed cross-reference tables.</returns>
    public static DocumentReader Parse(Stream stream, string? password, ReaderLimits limits)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(limits);
        var snapshot = limits.Snapshot();
        return Parse(ReadFully(stream, snapshot.MaxFileBytes), password, snapshot);
    }

    // A seekable stream is read straight into an exact-size buffer (one copy); a
    // non-seekable stream grows a pooled buffer and is copied out once. Either way the
    // document is never held in two full-size buffers at the same time, which matters
    // for large files under a constrained (WASM) heap. A stream that delivers fewer bytes
    // than its Length promised throws rather than parsing a truncated prefix, which a
    // repair pass would happily turn into a different document.
    internal static byte[] ReadFully(Stream stream, long maxFileBytes)
    {
        if (stream.CanSeek)
        {
            var remaining = stream.Length - stream.Position;
            if (remaining < 0)
            {
                remaining = 0;
            }

            if (remaining > maxFileBytes || remaining > int.MaxValue)
            {
                throw new DocumentParseException("Maximum file size exceeded.", -1);
            }

            var buffer = new byte[remaining];
            stream.ReadExactly(buffer, 0, buffer.Length);
            return buffer;
        }

        using var pooled = new PooledBufferStream();
        var chunk = new byte[81920];
        long total = 0;
        int read;
        while ((read = stream.Read(chunk, 0, chunk.Length)) > 0)
        {
            try
            {
                total = checked(total + read);
            }
            catch (OverflowException)
            {
                throw new DocumentParseException("Maximum file size exceeded.", -1);
            }

            if (total > maxFileBytes || total > int.MaxValue)
            {
                throw new DocumentParseException("Maximum file size exceeded.", -1);
            }

            pooled.Write(chunk, 0, read);
        }

        return pooled.ToArray();
    }

    internal DictionaryObject? ReconstructCatalogWithPages()
    {
        var catalog = FindCatalogWithPages();
        if (catalog is not null)
        {
            return catalog;
        }

        trailer = repairer.Repair(store);
        return FindCatalogWithPages();
    }

    private DictionaryObject? FindCatalogWithPages()
    {
        var numbers = new List<int>(xrefLoader.Entries.Keys);
        numbers.Sort();
        numbers.Reverse();
        foreach (var number in numbers)
        {
            DictionaryObject? candidate;
            try
            {
                candidate = GetObject(number) as DictionaryObject;
            }
            catch (DocumentParseException)
            {
                continue;
            }

            if (candidate is not null
                && candidate.TryGetValue("Type", out var type)
                && type is NameObject name
                && string.Equals(name.Value, "Catalog", StringComparison.Ordinal)
                && candidate.TryGetValue("Pages", out var pages)
                && pages is not null
                && Resolve(pages) is DictionaryObject)
            {
                return candidate;
            }
        }

        return null;
    }

    /// <summary>
    /// Parses and returns the indirect object with the given object number.
    /// </summary>
    /// <param name="number">The object number.</param>
    /// <returns>The parsed object.</returns>
    public DocumentObject GetObject(int number) => store.GetObject(number);

    // A reverse map from each already-resolved (cached) object instance to its
    // indirect object number. The incremental save path uses it to Override in
    // place the loaded objects a caller mutated (a filled form field, the page
    // tree root); only objects this reader has resolved appear, which is exactly
    // the set the model exposes for editing.
    internal IReadOnlyDictionary<DocumentObject, int> BuildObjectNumberIndex()
    {
        return store.BuildObjectNumberIndex();
    }

    /// <summary>
    /// Resolves an indirect reference to the object it points at. Non-reference
    /// objects are returned unchanged.
    /// </summary>
    /// <param name="value">The object to resolve.</param>
    /// <returns>The referenced object, or <paramref name="value"/> itself.</returns>
    public DocumentObject Resolve(DocumentObject value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return store.Resolve(value);
    }

    private void InitializeSecurity(string? password)
    {
        if (!trailer.TryGetValue("Encrypt", out var encryptObject) || encryptObject is null)
        {
            return;
        }

        var encryptObjectNumber = encryptObject is ReferenceObject reference ? reference.ObjectNumber : -1;

        // A present /Encrypt that does not resolve to a dictionary (an array, a dangling
        // reference resolving to null, a corrupt value) cannot be honored; processing the
        // document as plaintext would feed ciphertext into the filters and emit garbage.
        if (Resolve(encryptObject) is not DictionaryObject encrypt)
        {
            throw new DocumentParseException("The /Encrypt entry is not a dictionary.", -1);
        }

        var handler = new StandardSecurityHandler(encrypt, ReadDocumentId(), password ?? "");
        if (!handler.IsUserPassword && !handler.IsOwnerPassword)
        {
            throw new InvalidPasswordException();
        }

        store.SetSecurity(handler, encryptObjectNumber);
    }

    private byte[] ReadDocumentId()
    {
        if (trailer.TryGetValue("ID", out var id) && id is ArrayObject array
            && array.Count > 0 && array[0] is StringObject first)
        {
            return Encoding.Latin1.GetBytes(first.Value);
        }

        return [];
    }

    private void Load()
    {
        try
        {
            LoadFromXref();
        }
        catch (Exception exception) when (IsRecoverableParseFailure(exception))
        {
            trailer = repairer.Repair(store);
        }
    }

    // Malformed cross-reference data throws plain BCL exceptions (a /Type /XRef
    // stream missing /W, mistyped dictionary values, truncated entries, corrupt
    // Flate payloads); all of them mean the xref is unusable and the repair scan
    // should run.
    private static bool IsRecoverableParseFailure(Exception exception)
        => exception is DocumentParseException
            or KeyNotFoundException
            or InvalidCastException
            or ArgumentException
            or OverflowException
            or IndexOutOfRangeException
            or FormatException
            or InvalidOperationException
            or EndOfStreamException
            or InvalidDataException;

    private void LoadFromXref()
    {
        trailer = xrefLoader.Load(store);
    }

    /// <summary>
    /// Decodes the data of a stream object by applying its full <c>/Filter</c>
    /// chain (with <c>/DecodeParms</c> predictors) in order. A stream without a
    /// filter returns its data unchanged.
    /// </summary>
    /// <param name="stream">The stream object to decode.</param>
    /// <returns>The decoded stream bytes.</returns>
    /// <exception cref="DocumentParseException">The chain contains an unsupported filter.</exception>
    public byte[] DecodeStream(StreamObject stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        return decoder.Decode(stream.Dictionary, stream.Data);
    }

}
