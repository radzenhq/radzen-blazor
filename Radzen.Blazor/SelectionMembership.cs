using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Radzen
{
    /// <summary>
    /// Tests selection membership, memoizing compatible collections for one render pass.
    /// </summary>
    /// <remarks>
    /// <see cref="CollectionMembership" /> decides whether a binding can be memoized without changing its
    /// equality semantics. Components must call <see cref="Invalidate" /> between render passes.
    /// </remarks>
    internal sealed class SelectionMembership
    {
        // The binding for the current render pass.
        IEnumerable? current;

        CollectionMembership.Decision decision;

        // Null follows the binding's ICollection<object> contract when present.
        Func<IEnumerable, object?, bool>? nullAsker;

        // The collection that answers after forwarding wrappers are removed.
        IEnumerable? target;

        HashSet<object?>? set;

        // Cached because finding null may require a scan.
        bool? holdsNull;

        /// <summary>
        /// Discards state from the previous render pass.
        /// </summary>
        internal void Invalidate()
        {
            set = null;
            holdsNull = null;

            // Release the binding together with the memo.
            current = null;
            target = null;
            decision = default;
            nullAsker = null;
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
                target = CollectionMembership.Unwrap(values);
                decision = CollectionMembership.For(values.GetType(), target.GetType());
                nullAsker = CollectionMembership.NullAsker(values.GetType());
                set = null;
                holdsNull = null;
            }

            // Null dispatch belongs to the binding, not its unwrapped target.
            if (value == null)
            {
                return nullAsker != null
                    ? nullAsker(current!, null)
                    : holdsNull ??= CollectionMembership.HoldsNull(current!);
            }

            if (decision.Policy == CollectionMembership.Policy.Ask)
            {
                return decision.Ask!(target!, value);
            }

            return (set ??= new HashSet<object?>(target!.Cast<object?>())).Contains(value);
        }
    }
}
