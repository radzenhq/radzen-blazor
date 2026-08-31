using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Radzen
{
    /// <summary>
    /// Selects a membership strategy that preserves a collection's equality semantics.
    /// </summary>
    /// <remarks>
    /// A collection is memoized only when its membership uses <see cref="EqualityComparer{T}.Default" />.
    /// Collections with their own membership semantics are queried through their closed
    /// <see cref="ICollection{T}" /> contract.
    /// </remarks>
    internal static class CollectionMembership
    {
        /// <summary>What to do with a collection.</summary>
        internal enum Policy
        {
            /// <summary>
            /// Read it once per render pass and answer from a set built under the default comparer.
            /// </summary>
            Memoize,

            /// <summary>Ask it, because its membership is its own.</summary>
            Ask,
        }

        static readonly ConditionalWeakTable<Type, TypeCache> typeCaches = new();

        static readonly MethodInfo asker =
            typeof(CollectionMembership).GetMethod(nameof(Ask), BindingFlags.NonPublic | BindingFlags.Static)!;

        // Known owning types whose Contains uses the default comparer. Arrays are handled separately.
        static readonly HashSet<Type> ownsItsElements = new()
        {
            typeof(List<>),
            typeof(LinkedList<>),
            typeof(ImmutableList<>),
            typeof(ImmutableArray<>),
        };

        /// <summary>What was decided about a collection type.</summary>
        /// <param name="Policy">Whether to memoize it or ask it.</param>
        /// <param name="Ask">How to ask, when that is the policy.</param>
        internal readonly record struct Decision(
            Policy Policy, Func<IEnumerable, object?, bool>? Ask);

        sealed class DecisionBox
        {
            internal DecisionBox(Decision value) => Value = value;

            internal Decision Value { get; }
        }

        sealed class TypeCache
        {
            readonly ConditionalWeakTable<Type, DecisionBox> policies = new();

            internal TypeCache(Type type)
            {
                Contracts = ContractsItAnswersWith(type);
                Unwrapper = CreateUnwrapper(type, Contracts);

                var untyped = Array.Find(Contracts,
                    contract => contract.GetGenericArguments()[0] == typeof(object));
                NullAsker = untyped == null ? null : Asker(untyped, throughInvoke: false);
            }

            internal Type[] Contracts { get; }

            internal Func<IEnumerable, IEnumerable?>? Unwrapper { get; }

            internal Func<IEnumerable, object?, bool>? NullAsker { get; }

            internal bool TryGetDecision(Type answering, out Decision decision)
            {
                if (policies.TryGetValue(answering, out var cached))
                {
                    decision = cached.Value;
                    return true;
                }

                decision = default;
                return false;
            }

            internal Decision GetDecision(Type answering, Type[] contracts) =>
                policies.GetValue(answering,
                    key => new DecisionBox(Decide(key, contracts, throughInvoke: false))).Value;
        }

        static TypeCache ForType(Type type) =>
            typeCaches.GetValue(type, static key => new TypeCache(key));

        /// <summary>
        /// What to do with the collection that answers for a binding.
        /// </summary>
        /// <param name="binding">The type the component is bound to.</param>
        /// <param name="answering">
        /// The type that answers for it, which is <paramref name="binding" /> unless a wrapper was taken
        /// off it - see <see cref="Unwrap" />.
        /// </param>
        internal static Decision For(Type binding, Type answering)
        {
            var bindingCache = ForType(binding);

            if (bindingCache.TryGetDecision(answering, out var decision))
            {
                return decision;
            }

            var answeringCache = binding == answering ? bindingCache : ForType(answering);
            var contracts = binding == answering
                ? answeringCache.Contracts
                : ContractsWithin(bindingCache.Contracts, answeringCache.Contracts);

            return bindingCache.GetDecision(answering, contracts);
        }

        /// <summary>
        /// How to put null to a binding, or null if it has to be looked for instead.
        /// </summary>
        /// <remarks>
        /// <c>ICollection&lt;object&gt;</c> receives null directly. Other bindings are scanned by
        /// <see cref="HoldsNull" /> because typed contracts may reject null.
        /// </remarks>
        internal static Func<IEnumerable, object?, bool>? NullAsker(Type binding) =>
            ForType(binding).NullAsker;

        /// <summary>
        /// The same decision, carried out through reflection invocation rather than through a delegate.
        /// </summary>
        internal static Func<IEnumerable, object?, bool>? AskByInvoking(Type type) =>
            Decide(type, ForType(type).Contracts, throughInvoke: true).Ask;

        /// <summary>
        /// The collection whose membership the given one actually answers with.
        /// </summary>
        /// <remarks>
        /// Only wrappers that forward every membership contract are removed. Subclasses that implement
        /// their own <c>Contains</c> remain authoritative.
        /// </remarks>
        internal static IEnumerable Unwrap(IEnumerable values)
        {
            // Bounded, because a wrapper may wrap a wrapper and nothing rules out a cycle.
            for (var depth = 0; depth < 8; depth++)
            {
                var inner = ForType(values.GetType()).Unwrapper?.Invoke(values);

                if (inner == null || ReferenceEquals(inner, values))
                {
                    break;
                }

                values = inner;
            }

            return values;
        }

        static Func<IEnumerable, IEnumerable?>? CreateUnwrapper(Type type, Type[] contracts)
        {
            var wrapper = TheWrapperItForwardsTo(type, contracts);

            if (wrapper == null)
            {
                return null;
            }

            // Resolve Items from the verified wrapper so a subclass cannot hide the backing list.
            var items = wrapper.GetProperty("Items", BindingFlags.Instance | BindingFlags.NonPublic);

            return items == null ? null : values => items.GetValue(values) as IEnumerable;
        }

        /// <summary>
        /// The <see cref="Collection{T}" /> or <see cref="ReadOnlyCollection{T}" /> that answers every
        /// membership contract on <paramref name="type" />, or null if anything else does.
        /// </summary>
        /// <remarks>All contracts must forward to the same wrapper; reflection order is not significant.</remarks>
        static Type? TheWrapperItForwardsTo(Type type, Type[] contracts)
        {
            if (contracts.Length == 0)
            {
                return null;
            }

            Type? wrapper = null;

            foreach (var contract in contracts)
            {
                var implementation = ImplementationOfContains(type, contract);

                if (implementation is not { IsGenericType: true }
                    || (implementation.GetGenericTypeDefinition() != typeof(Collection<>)
                        && implementation.GetGenericTypeDefinition() != typeof(ReadOnlyCollection<>)))
                {
                    return null;
                }

                if (wrapper != null && wrapper != implementation)
                {
                    return null;
                }

                wrapper = implementation;
            }

            return wrapper;
        }

        /// <summary>
        /// The type that actually implements <c>Contains</c> for <paramref name="contract" />, which is
        /// not necessarily the type that declares the collection.
        /// </summary>
        static Type? ImplementationOfContains(Type type, Type contract)
        {
            if (type.IsInterface || type.IsArray)
            {
                return null;
            }

            var map = type.GetInterfaceMap(contract);

            for (var i = 0; i < map.InterfaceMethods.Length; i++)
            {
                if (map.InterfaceMethods[i].Name == nameof(ICollection<object>.Contains))
                {
                    return map.TargetMethods[i].DeclaringType;
                }
            }

            return null;
        }

        static Decision Decide(Type type, Type[] contracts, bool throughInvoke)
        {
            if (type.IsArray || (type.IsGenericType && ownsItsElements.Contains(type.GetGenericTypeDefinition())))
            {
                return new Decision(Policy.Memoize, null);
            }

            if (contracts.Length == 0)
            {
                // Enumerable.Contains scans such sequences with the default comparer.
                return new Decision(Policy.Memoize, null);
            }

            // Cast<object>().Contains dispatches to ICollection<object> when available.
            var untyped = Array.Find(contracts, contract => contract.GetGenericArguments()[0] == typeof(object));

            if (untyped != null)
            {
                return new Decision(Policy.Ask, Asker(untyped, throughInvoke));
            }

            if (contracts.Length == 1)
            {
                return new Decision(Policy.Ask, Asker(contracts[0], throughInvoke));
            }

            // With several typed contracts, query the first one compatible with the value.
            var candidates = Array.ConvertAll(contracts, contract =>
                (Element: contract.GetGenericArguments()[0], Ask: Asker(contract, throughInvoke)));

            return new Decision(Policy.Ask, (values, value) =>
            {
                foreach (var (element, ask) in candidates)
                {
                    if (element.IsInstanceOfType(value))
                    {
                        return ask(values, value);
                    }
                }

                return false;
            });
        }

        /// <summary>
        /// The contracts <paramref name="answering" /> may be asked about on behalf of
        /// <paramref name="binding" />.
        /// </summary>
        /// <remarks>Removing wrappers may change the target, but cannot expose new contracts.</remarks>
        static Type[] ContractsWithin(Type[] binding, Type[] answering)
        {
            return Array.FindAll(answering, contract => Array.IndexOf(binding, contract) >= 0);
        }

        /// <summary>
        /// The closed <see cref="ICollection{T}" /> interfaces a type implements, which are the ones that
        /// carry <c>Contains</c>.
        /// </summary>
        static Type[] ContractsItAnswersWith(Type type) =>
            type.GetInterfaces()
                .Where(contract => contract.IsGenericType
                    && contract.GetGenericTypeDefinition() == typeof(ICollection<>))
                .ToArray();

        static Func<IEnumerable, object?, bool> Asker(Type contract, bool throughInvoke)
        {
            var element = contract.GetGenericArguments()[0];

            if (!throughInvoke)
            {
                try
                {
                    return asker.MakeGenericMethod(element)
                        .CreateDelegate<Func<IEnumerable, object?, bool>>();
                }
                catch (Exception exception) when (exception is NotSupportedException or InvalidOperationException)
                {
                    // Trimmed or AOT builds may not contain the required closed generic.
                }
            }

            return Invoking(contract, element);
        }

        static Func<IEnumerable, object?, bool> Invoking(Type contract, Type element)
        {
            var contains = contract.GetMethod(nameof(ICollection<object>.Contains))!;

            return (values, value) => value == null
                ? Unwrapped(contains, values, null)
                : element.IsInstanceOfType(value) && Unwrapped(contains, values, value);
        }

        /// <summary>
        /// Invokes <c>Contains</c> without reflection changing what comes back out of it.
        /// </summary>
        /// <remarks><see cref="BindingFlags.DoNotWrapExceptions" /> preserves caller exceptions.</remarks>
        static bool Unwrapped(MethodInfo contains, IEnumerable values, object? value) =>
            (bool)contains.Invoke(values, BindingFlags.DoNotWrapExceptions, binder: null,
                parameters: new[] { value }, culture: null)!;

        /// <summary>
        /// Whether <paramref name="values" /> holds a null.
        /// </summary>
        /// <remarks>Typed collection contracts are not queried because some reject null.</remarks>
        internal static bool HoldsNull(IEnumerable values)
        {
            // Cast preserves a compatible generic enumerator when one exists.
            foreach (var item in values.Cast<object?>())
            {
                if (item == null)
                {
                    return true;
                }
            }

            return false;
        }

        static bool Ask<T>(IEnumerable values, object? value)
        {
            var collection = (ICollection<T>)values;

            if (value is T typed)
            {
                return collection.Contains(typed);
            }

            // Null is compatible with reference and nullable element types.
            return value is null && default(T) is null && collection.Contains(default!);
        }
    }
}
