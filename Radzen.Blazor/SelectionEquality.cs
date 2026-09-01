using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Radzen
{
    internal static class SelectionEquality
    {
        static readonly ConcurrentDictionary<Type, bool> reliableHashes = new();

        internal static bool HashesReliably(Type type) =>
            reliableHashes.GetOrAdd(type, static t =>
            {
                if (t.IsPrimitive || t.IsEnum || t == typeof(string))
                {
                    return true;
                }

                var equals = t.GetMethod(nameof(object.Equals), new[] { typeof(object) });
                var hashCode = t.GetMethod(nameof(object.GetHashCode), Type.EmptyTypes);

                return equals == null || hashCode == null
                    || equals.DeclaringType == hashCode.DeclaringType
                    || !hashCode.DeclaringType!.IsAssignableFrom(equals.DeclaringType);
            });

        internal static HashSet<object?>? TryCreateSet(IEnumerable values)
        {
            if (values is ICollection<object> && values is not List<object> && values is not object[])
            {
                return null;
            }

            var set = new HashSet<object?>();

            foreach (var item in values.Cast<object?>())
            {
                if (item != null && !HashesReliably(item.GetType()))
                {
                    return null;
                }

                set.Add(item);
            }

            return set;
        }
    }
}
