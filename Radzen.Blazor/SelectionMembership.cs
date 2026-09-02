using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Radzen
{
    /// <summary>
    /// Answers "is this value one of the selected ones" for a component rendering a list of items,
    /// memoized for the span of a single render pass where the memo can answer identically.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The question is asked several times per item per render, and again for the select-all state, so
    /// answering it by scanning the bound collection each time makes a render O(items x selected). A set
    /// built once per pass makes each answer O(1) against one pass over the selected values.
    /// </para>
    /// <para>
    /// It is not available for every binding, and where it is not, the answer is the scan. Which is which
    /// belongs to <see cref="SelectionEquality" />, not here: the memo may only answer where it answers
    /// what <c>values.Cast&lt;object?&gt;().Contains(value)</c> answered, since that is the question these
    /// components have always asked. A collection carrying <c>ICollection&lt;object&gt;</c> answers it for
    /// itself, and a value whose type overrides <c>Equals</c> without <c>GetHashCode</c> cannot be found
    /// in a set at all - both fall back to the scan.
    /// </para>
    /// <para>
    /// The memo is not validated, it is discarded: nothing about a bound collection can be inferred from
    /// outside it, since a caller may swap one selected value for another in place, changing neither the
    /// reference nor the count. Components call <see cref="Invalidate" /> at the start of every render -
    /// <c>ShouldRender</c>, plus <c>OnParametersSet</c> for the first render, which <c>ShouldRender</c> is
    /// not consulted for - so the memo can never outlive the state it was built from.
    /// </para>
    /// </remarks>
    internal sealed class SelectionMembership
    {
        // The collection everything below was built for. The reference check is not the invalidation -
        // Invalidate is - it only avoids rebuilding twice within one pass, and catches a reassignment that
        // lands between two renders rather than through a parameter set.
        IEnumerable? current;

        // Null when this collection has to be scanned instead, which is decided once per pass with it.
        HashSet<object?>? set;

        /// <summary>
        /// Discards everything built for the previous render pass. Called at the start of every render by
        /// the component that owns it.
        /// </summary>
        internal void Invalidate()
        {
            set = null;

            // The collection goes with the memo. Keeping the reference pins the caller's collection for as
            // long as the component lives: if the binding becomes null, nothing overwrites it and nothing
            // reads it again either.
            current = null;
        }

        /// <summary>
        /// Whether <paramref name="value" /> is among <paramref name="values" />.
        /// </summary>
        internal bool Contains(IEnumerable? values, object? value)
        {
            if (values == null)
            {
                return false;
            }

            if (!ReferenceEquals(current, values))
            {
                current = values;
                set = SelectionEquality.TryCreateSet(values);
            }

            // The candidate is checked as well as the elements: a set built from values that hash reliably
            // still cannot be asked about one that does not.
            if (set != null && (value == null || SelectionEquality.HashesReliably(value.GetType())))
            {
                return set.Contains(value);
            }

            return values.Cast<object?>().Contains(value);
        }
    }
}
