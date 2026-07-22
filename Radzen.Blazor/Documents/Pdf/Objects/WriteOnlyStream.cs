using System;
using System.IO;

namespace Radzen.Documents.Pdf.Objects;

internal abstract class WriteOnlyStream : Stream
{
    public sealed override bool CanRead => false;

    public sealed override bool CanSeek => false;

    public sealed override bool CanWrite => true;

    public sealed override long Position
    {
        get => Length;
        set => throw new NotSupportedException();
    }

    public sealed override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    public sealed override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    public sealed override void SetLength(long value) => throw new NotSupportedException();
}
