using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Radzen.Documents.Pdf.Crypto;

internal sealed class AesCbcPump(IAesCbcProvider? supplied) : IAesCbcProvider
{
    internal enum Stage
    {
        PassThrough,
        Collect,
    }

    private readonly Dictionary<string, byte[]> results = new(StringComparer.Ordinal);
    private readonly List<(string Id, Task<byte[]> Work)> pending = [];
    private IAesCbcProvider? resolved;

    public Stage Mode { get; set; } = Stage.PassThrough;

    public int PendingCount => pending.Count;

    public ValueTask<byte[]> EncryptCbcAsync(
        ReadOnlyMemory<byte> key, ReadOnlyMemory<byte> iv, ReadOnlyMemory<byte> plaintext)
        => Run('E', key, iv, plaintext);

    public ValueTask<byte[]> DecryptCbcAsync(
        ReadOnlyMemory<byte> key, ReadOnlyMemory<byte> iv, ReadOnlyMemory<byte> ciphertext)
        => Run('D', key, iv, ciphertext);

    public async ValueTask ResolvePendingAsync()
    {
        foreach (var (id, work) in pending)
        {
            results[id] = await work.ConfigureAwait(false);
        }

        pending.Clear();
    }

    private IAesCbcProvider Provider => resolved ??= AesCbcEngine.Resolve(supplied, OperatingSystem.IsBrowser());

    private ValueTask<byte[]> Run(char tag, ReadOnlyMemory<byte> key, ReadOnlyMemory<byte> iv, ReadOnlyMemory<byte> data)
    {
        var id = Identify(tag, key, iv, data);
        if (results.TryGetValue(id, out var known))
        {
            return ValueTask.FromResult(known);
        }

        var work = tag == 'E'
            ? Provider.EncryptCbcAsync(key, iv, data)
            : Provider.DecryptCbcAsync(key, iv, data);

        if (work.IsCompletedSuccessfully)
        {
            var value = work.Result;
            results[id] = value;
            return ValueTask.FromResult(value);
        }

        if (Mode == Stage.PassThrough)
        {
            return work;
        }

        pending.Add((id, work.AsTask()));
        return ValueTask.FromResult(new byte[data.Length]);
    }

    private static string Identify(char tag, ReadOnlyMemory<byte> key, ReadOnlyMemory<byte> iv, ReadOnlyMemory<byte> data)
    {
        var buffer = new byte[3 + key.Length + iv.Length + data.Length];
        buffer[0] = (byte)tag;
        buffer[1] = (byte)key.Length;
        buffer[2] = (byte)iv.Length;
        key.Span.CopyTo(buffer.AsSpan(3));
        iv.Span.CopyTo(buffer.AsSpan(3 + key.Length));
        data.Span.CopyTo(buffer.AsSpan(3 + key.Length + iv.Length));
        return Convert.ToHexString(Sha2.ComputeHash256(buffer));
    }
}
