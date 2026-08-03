using Radzen.Documents.Pdf.Crypto;
using Radzen.Documents.Pdf.Objects.Encryption;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Radzen.Documents.Pdf.Objects;

// ISO 32000-1 7.5: the last cross-reference section is located via startxref and any
// /Prev chain of incremental updates is followed.
internal sealed class DocumentReader
{
    private readonly ReaderLimits limits;
    internal ReaderLimits Limits => limits;
    private DictionaryObject trailer = new();
    private readonly StreamDecoder decoder;
    private readonly XrefLoader xrefLoader;
    private readonly IndirectObjectStore store;
    private readonly DocumentRepairer repairer;
    private readonly DocumentObjectGraph? graph;

    private DocumentReader(byte[] data, ReaderLimits limits)
    {
        this.limits = limits;
        decoder = new StreamDecoder(limits, Resolve);
        repairer = new DocumentRepairer(data, limits);
        xrefLoader = new XrefLoader(data, limits, decoder);
        store = new IndirectObjectStore(data, limits, xrefLoader.Entries, decoder, repairer);
    }

    internal DocumentReader(DocumentObjectGraph graph)
    {
        this.graph = graph;
        limits = ReaderLimits.Default.Snapshot();
        trailer = graph.Trailer;
        decoder = new StreamDecoder(limits, Resolve);
        xrefLoader = null!;
        store = null!;
        repairer = null!;
    }

    public DictionaryObject Trailer => trailer;

    public int ObjectCount
    {
        get
        {
            if (graph is not null)
            {
                return graph.Objects.Count;
            }

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

    public bool IsEncrypted => graph is null && store.IsEncrypted;

    internal int GenerationOf(int number) => graph is null ? store.GenerationOf(number) : 0;

    public static DocumentReader Parse(byte[] data) => Parse(data, null);

    public static DocumentReader Parse(byte[] data, string? password) => Parse(data, password, ReaderLimits.Default);

    public static DocumentReader Parse(byte[] data, string? password, ReaderLimits limits)
        => Parse(data, password, null, limits);

    public static DocumentReader Parse(
        byte[] data, string? password, IAesCbcProvider? aesProvider, ReaderLimits limits)
    {
        var reader = Prepare(data, limits);
        reader.InitializeSecurity(password, aesProvider);
        return reader;
    }

    public static async ValueTask<DocumentReader> ParseAsync(
        byte[] data, string? password, IAesCbcProvider? aesProvider, ReaderLimits limits)
    {
        var reader = Prepare(data, limits);
        var pump = new AesCbcPump(aesProvider);
        await reader.InitializeSecurityAsync(password, pump).ConfigureAwait(false);
        if (!reader.store.IsEncrypted)
        {
            return reader;
        }

        for (var attempt = 0; attempt < 64; attempt++)
        {
            pump.Mode = AesCbcPump.Stage.Collect;
            reader.store.ResetCache();
            reader.store.WarmDecryption();
            if (pump.PendingCount == 0)
            {
                pump.Mode = AesCbcPump.Stage.PassThrough;
                return reader;
            }

            await pump.ResolvePendingAsync().ConfigureAwait(false);
        }

        throw new InvalidOperationException(
            "The supplied IAesCbcProvider did not settle after 64 decryption passes.");
    }

    private static DocumentReader Prepare(byte[] data, ReaderLimits limits)
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
        return reader;
    }

    public static DocumentReader Parse(Stream stream) => Parse(stream, null);

    public static DocumentReader Parse(Stream stream, string? password)
        => Parse(stream, password, ReaderLimits.Default);

    public static DocumentReader Parse(Stream stream, string? password, ReaderLimits limits)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(limits);
        var snapshot = limits.Snapshot();
        return Parse(PdfSourceBytes.ReadFully(stream, snapshot.MaxFileBytes), password, snapshot);
    }


    internal DictionaryObject? ReconstructCatalogWithPages()
    {
        var catalog = FindCatalogWithPages(out var number);
        if (catalog is null)
        {
            trailer = repairer.Repair(store);
            catalog = FindCatalogWithPages(out number);
        }

        if (catalog is not null)
        {
            trailer["Root"] = new ReferenceObject(number, 0);
        }

        return catalog;
    }

    private DictionaryObject? FindCatalogWithPages(out int number)
    {
        var numbers = new List<int>(xrefLoader.Entries.Keys);
        numbers.Sort();
        numbers.Reverse();
        foreach (var candidateNumber in numbers)
        {
            DictionaryObject? candidate;
            try
            {
                candidate = GetObject(candidateNumber) as DictionaryObject;
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
                number = candidateNumber;
                return candidate;
            }
        }

        number = 0;
        return null;
    }

    public DocumentObject GetObject(int number)
        => graph is null
            ? store.GetObject(number)
            : number >= 1 && number <= graph.Objects.Count
                ? graph.Objects[number - 1]
                : throw new KeyNotFoundException($"Object {number} is not present.");

    internal IReadOnlyDictionary<DocumentObject, int> BuildObjectNumberIndex()
    {
        return graph is null ? store.BuildObjectNumberIndex() : graph.BuildObjectNumberIndex();
    }

    public DocumentObject Resolve(DocumentObject value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (graph is null)
        {
            return store.Resolve(value);
        }

        return value is ReferenceObject reference
            ? graph.Resolve(reference) ?? throw new KeyNotFoundException($"Object {reference.ObjectNumber} is not present.")
            : value;
    }

    private void InitializeSecurity(string? password, IAesCbcProvider? aesProvider)
    {
        if (!TryReadEncryptDictionary(out var encrypt, out var encryptObjectNumber))
        {
            return;
        }

        Adopt(new StandardSecurityHandler(encrypt!, ReadDocumentId(), password ?? "", aesProvider), encryptObjectNumber);
    }

    private async ValueTask InitializeSecurityAsync(string? password, IAesCbcProvider aesProvider)
    {
        if (!TryReadEncryptDictionary(out var encrypt, out var encryptObjectNumber))
        {
            return;
        }

        var handler = await StandardSecurityHandler
            .CreateAsync(encrypt!, ReadDocumentId(), password ?? "", aesProvider).ConfigureAwait(false);
        Adopt(handler, encryptObjectNumber);
    }

    private void Adopt(StandardSecurityHandler handler, int encryptObjectNumber)
    {
        if (!handler.IsUserPassword && !handler.IsOwnerPassword)
        {
            throw new InvalidPasswordException();
        }

        store.SetSecurity(handler, encryptObjectNumber);
    }

    private bool TryReadEncryptDictionary(out DictionaryObject? encrypt, out int encryptObjectNumber)
    {
        encrypt = null;
        encryptObjectNumber = -1;
        if (!trailer.TryGetValue("Encrypt", out var encryptObject) || encryptObject is null)
        {
            return false;
        }

        encryptObjectNumber = encryptObject is ReferenceObject reference ? reference.ObjectNumber : -1;
        encrypt = Resolve(encryptObject) as DictionaryObject
            ?? throw new DocumentParseException("The /Encrypt entry is not a dictionary.", -1);
        return true;
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

    private static bool IsRecoverableParseFailure(Exception exception)
        => exception is DocumentParseException
            or KeyNotFoundException
            or ArgumentException
            or OverflowException
            or FormatException
            or EndOfStreamException
            or InvalidDataException;

    private void LoadFromXref()
    {
        trailer = xrefLoader.Load(store);
    }

    public byte[] DecodeStream(StreamObject stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        return decoder.Decode(stream.Dictionary, stream.Data);
    }

}
