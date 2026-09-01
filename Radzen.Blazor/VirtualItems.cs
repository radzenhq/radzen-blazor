using System.Collections.Generic;
using System.Linq;

namespace Radzen
{
    /// <summary>Owns the last accepted range of rows fetched for a virtualized query.</summary>
    /// <remarks>
    /// Fetch identities prevent superseded work from publishing even when cancellation is ignored.
    /// Invalidating a query also supersedes every fetch composed from it.
    /// </remarks>
    /// <typeparam name="T">The row type the fetched range holds.</typeparam>
    internal sealed class VirtualItems<T>
    {
        List<T>? items;

        long published;

        long sequence;

        /// <summary>Identifies the query from which the current range was composed.</summary>
        internal long QueryGeneration { get; private set; }

        /// <summary>The index within the whole sequence that the current range starts at.</summary>
        internal int StartIndex { get; private set; }

        internal int IndexOf(T value) => items?.IndexOf(value) ?? -1;

        /// <summary>How many rows the whole sequence holds, as of the last fetch.</summary>
        internal int TotalCount { get; private set; }

        // Unknown is treated as non-empty so Virtualize remains rendered long enough to fetch.
        internal bool HasAny => items == null || TotalCount > 0 || items.Count > 0;

        /// <summary>Claims the identity passed to <see cref="TryPublish" />.</summary>
        internal long BeginFetch() => ++sequence;

        /// <summary>Publishes a fetched range only when its fetch is still current.</summary>
        /// <returns>Whether the range was published.</returns>
        internal bool TryPublish(long fetch, int startIndex, int totalCount, List<T> rows)
        {
            if (fetch != sequence || fetch <= published)
            {
                return false;
            }

            published = fetch;
            StartIndex = startIndex;
            TotalCount = totalCount;
            items = rows;

            return true;
        }

        /// <summary>Discards the rows and supersedes every fetch composed from their query.</summary>
        internal void Invalidate()
        {
            items = null;
            StartIndex = 0;
            TotalCount = 0;
            published = sequence;

            QueryGeneration++;
        }

        internal bool TryGetAt(int index, out T value)
        {
            var offset = index - StartIndex;

            if (items != null && offset >= 0 && offset < items.Count)
            {
                value = items[offset];

                return true;
            }

            value = default!;

            return false;
        }
    }
}
