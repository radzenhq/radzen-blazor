using Microsoft.JSInterop;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Radzen.Blazor.Tests
{
    internal class TestJSStreamReference : IJSStreamReference
    {
        private readonly byte[] data;

        public TestJSStreamReference(byte[] data)
        {
            this.data = data;
        }

        public long Length => data.Length;

        public ValueTask<Stream> OpenReadStreamAsync(long maxAllowedSize = 512000, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<Stream>(new MemoryStream(data));

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
