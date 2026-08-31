using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Radzen;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class SelectionMembershipTests
    {
        sealed class CountingCollection : IEnumerable<object>, ICollection<object>
        {
            readonly List<object> items;

            public CountingCollection(params object[] values) => items = new List<object>(values);

            public int Asked { get; private set; }

            public IEnumerator<object> GetEnumerator() => items.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            public int Count => items.Count;

            public bool IsReadOnly => true;

            public bool Contains(object item)
            {
                Asked++;

                return items.Contains(item);
            }

            public void Add(object item) => items.Add(item);
            public void Clear() => items.Clear();
            public void CopyTo(object[] array, int arrayIndex) => items.CopyTo(array, arrayIndex);
            public bool Remove(object item) => items.Remove(item);
        }

        public static IEnumerable<object[]> Memoized()
        {
            var array = new object[] { "a", "b", "c" };

            yield return new object[] { array, (Action)(() => array[2] = "z") };

            var list = new List<object> { "a", "b", "c" };

            yield return new object[] { list, (Action)(() => list.Add("z")) };

            var collection = new Collection<object> { "a", "b", "c" };

            yield return new object[] { collection, (Action)(() => collection.Add("z")) };

            var backing = new List<object> { "a", "b", "c" };

            yield return new object[] { new ReadOnlyCollection<object>(backing), (Action)(() => backing.Add("z")) };

            var linked = new LinkedList<object>(new object[] { "a", "b", "c" });

            yield return new object[] { linked, (Action)(() => linked.AddLast("z")) };
        }

        [Theory]
        [MemberData(nameof(Memoized))]
        public void TheCollectionsCallersBindAreMemoized(IEnumerable source, Action addZ)
        {
            var selection = new SelectionMembership();

            Assert.True(selection.Contains(source, "a"));
            Assert.False(selection.Contains(source, "z"));

            addZ();

            Assert.False(selection.Contains(source, "z"));

            selection.Invalidate();

            Assert.True(selection.Contains(source, "z"));
        }

        // Deliberately has no membership contract.
        sealed class CountingSequence : IEnumerable<object>
        {
            readonly object[] items;

            public CountingSequence(params object[] values) => items = values;

            public int Enumerations { get; private set; }

            public IEnumerator<object> GetEnumerator()
            {
                Enumerations++;

                return ((IEnumerable<object>)items).GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        [Fact]
        public void AMemoizedSourceIsReadOncePerPass()
        {
            var source = new CountingSequence("a", "b", "c", "d");

            var selection = new SelectionMembership();

            Assert.True(selection.Contains(source, "a"));
            Assert.True(selection.Contains(source, "c"));
            Assert.True(selection.Contains(source, "d"));
            Assert.False(selection.Contains(source, "z"));

            Assert.Equal(1, source.Enumerations);

            selection.Invalidate();

            Assert.True(selection.Contains(source, "a"));

            Assert.Equal(2, source.Enumerations);
        }

        [Fact]
        public void ANullScanIsReadOncePerPass()
        {
            var source = new CountingSequence("a", null!);
            var selection = new SelectionMembership();

            Assert.True(selection.Contains(source, null));
            Assert.True(selection.Contains(source, null));
            Assert.Equal(1, source.Enumerations);

            selection.Invalidate();

            Assert.True(selection.Contains(source, null));
            Assert.Equal(2, source.Enumerations);
        }

        [Fact]
        public void AValueOfAnotherTypeIsNotAMember()
        {
            var source = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "alpha" };

            var selection = new SelectionMembership();

            Assert.False(selection.Contains(source, 42));
        }

        [Fact]
        public void ACollectionWithItsOwnMembershipIsAsked()
        {
            var source = new CountingCollection("a", "b", "c");

            var selection = new SelectionMembership();

            Assert.True(selection.Contains(source, "b"));
            Assert.False(selection.Contains(source, "z"));

            Assert.Equal(2, source.Asked);
        }

        sealed class CaseInsensitiveList : List<object>, IList<object>
        {
            public CaseInsensitiveList(params object[] values) => AddRange(values);

            bool ICollection<object>.Contains(object item) =>
                Exists(x => string.Equals(x as string, item as string, StringComparison.OrdinalIgnoreCase));
        }

        public static IEnumerable<object[]> Wrappers => new object[][]
        {
            new object[] { new Collection<object>(new CaseInsensitiveList("alpha", "beta")) },
            new object[] { new ReadOnlyCollection<object>(new CaseInsensitiveList("alpha", "beta")) },
        };

        [Theory]
        [MemberData(nameof(Wrappers))]
        public void AWrapperAnswersWithTheListItHolds(IEnumerable source)
        {
            var selection = new SelectionMembership();

            Assert.True(((ICollection<object>)source).Contains("ALPHA"));

            Assert.True(selection.Contains(source, "ALPHA"));
            Assert.False(selection.Contains(source, "gamma"));
        }

        sealed class TwoContracts : IEnumerable<object>, ICollection<object>, ICollection<string>
        {
            readonly List<string> strings = new() { "alpha" };
            readonly List<object> objects = new() { 42 };

            public IEnumerator<object> GetEnumerator() => strings.Cast<object>().Concat(objects).GetEnumerator();
            IEnumerator<string> IEnumerable<string>.GetEnumerator() => strings.GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            bool ICollection<string>.Contains(string item) =>
                strings.Exists(x => string.Equals(x, item, StringComparison.OrdinalIgnoreCase));

            bool ICollection<object>.Contains(object item) => objects.Contains(item);

            int ICollection<object>.Count => objects.Count;
            int ICollection<string>.Count => strings.Count;
            bool ICollection<object>.IsReadOnly => true;
            bool ICollection<string>.IsReadOnly => true;
            void ICollection<object>.Add(object item) { }
            void ICollection<string>.Add(string item) { }
            void ICollection<object>.Clear() { }
            void ICollection<string>.Clear() { }
            void ICollection<object>.CopyTo(object[] array, int index) => objects.CopyTo(array, index);
            void ICollection<string>.CopyTo(string[] array, int index) => strings.CopyTo(array, index);
            bool ICollection<object>.Remove(object item) => false;
            bool ICollection<string>.Remove(string item) => false;
        }

        [Fact]
        public void TheUntypedContractIsTheOneAskedWhenACollectionHasIt()
        {
            var source = new TwoContracts();

            var selection = new SelectionMembership();

            Assert.False(source.Cast<object>().Contains("ALPHA"));
            Assert.True(source.Cast<object>().Contains(42));

            Assert.False(selection.Contains(source, "ALPHA"));
            Assert.True(selection.Contains(source, 42));
        }

        [Fact]
        public void DictionaryKeysUseTheirComparerButAreScannedForNull()
        {
            var source = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                ["alpha"] = 1,
            }.Keys;
            var selection = new SelectionMembership();

            Assert.Throws<ArgumentNullException>(() => source.Contains(null!));

            Assert.False(selection.Contains(source, null));
            Assert.True(selection.Contains(source, "ALPHA"));
        }

        sealed class NullMeansUnset : IEnumerable<object>, ICollection<object>
        {
            readonly List<object> items = new() { "alpha" };

            public bool Unset = true;

            public IEnumerator<object> GetEnumerator() => items.GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            public int Count => items.Count;
            public bool IsReadOnly => true;

            public bool Contains(object item) => item == null ? Unset : items.Contains(item);

            public void Add(object item) { }
            public void Clear() { }

            public void CopyTo(object[] array, int arrayIndex) => items.CopyTo(array, arrayIndex);

            public bool Remove(object item) => false;
        }

        [Fact]
        public void TheUntypedContractIsAskedAboutNullAsWell()
        {
            var source = new NullMeansUnset();

            var selection = new SelectionMembership();

            Assert.True(((ICollection<object>)source).Contains(null!));
            Assert.All(source, item => Assert.NotNull(item));

            Assert.True(selection.Contains(source, null));

            selection.Invalidate();
            source.Unset = false;

            Assert.False(selection.Contains(source, null));
        }

        [Fact]
        public void ChangingTheBindingResetsCachedState()
        {
            var selection = new SelectionMembership();

            var memoized = new List<object> { null!, "first" };
            Assert.True(selection.Contains(memoized, "first"));
            Assert.True(selection.Contains(memoized, null));

            var typed = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "alpha" };
            Assert.True(selection.Contains(typed, "ALPHA"));
            Assert.False(selection.Contains(typed, null));

            Assert.True(selection.Contains(new NullMeansUnset(), null));
        }

        sealed class SplitEnumerators : IEnumerable<object>, ICollection<string>
        {
            readonly List<string> items = new() { "alpha" };

            public IEnumerator<object> GetEnumerator() => items.Cast<object>().GetEnumerator();

            IEnumerator<string> IEnumerable<string>.GetEnumerator() => items.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator()
            {
                yield return null!;
            }

            bool ICollection<string>.Contains(string item) => items.Contains(item);

            int ICollection<string>.Count => items.Count;
            bool ICollection<string>.IsReadOnly => true;
            void ICollection<string>.Add(string item) { }
            void ICollection<string>.Clear() { }
            void ICollection<string>.CopyTo(string[] array, int index) => items.CopyTo(array, index);
            bool ICollection<string>.Remove(string item) => false;
        }

        [Fact]
        public void TheScanReadsTheEnumeratorTheComponentsAlwaysRead()
        {
            var source = new SplitEnumerators();

            var selection = new SelectionMembership();

            Assert.False(source.Cast<object?>().Contains(null));
            Assert.Contains<object?>(null, NonGeneric(source));

            Assert.False(selection.Contains(source, null));
            Assert.True(selection.Contains(source, "alpha"));
        }

        static IEnumerable<object?> NonGeneric(IEnumerable values)
        {
            foreach (var item in values)
            {
                yield return item;
            }
        }

        [Fact]
        public void ATypedSetUsesItsComparerAndCanHoldNull()
        {
            var source = new HashSet<string?>(StringComparer.OrdinalIgnoreCase) { null, "alpha" };

            var selection = new SelectionMembership();

            Assert.True(selection.Contains(source, null));
            Assert.True(selection.Contains(source, "ALPHA"));
        }

        public static IEnumerable<object[]> AskableSources()
        {
            yield return new object[] { new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "alpha", "beta" } };

            yield return new object[] { new HashSet<int> { 1, 2, 3 } };

            yield return new object[] { new TwoContracts() };
        }

        [Theory]
        [MemberData(nameof(AskableSources))]
        public void TheAheadOfTimeFallbackAnswersTheSameAsTheDelegate(IEnumerable source)
        {
            var invoking = CollectionMembership.AskByInvoking(source.GetType())!;

            var selection = new SelectionMembership();

            foreach (var value in new object[] { null!, "alpha", "ALPHA", "gamma", 1, 7, 42.5 })
            {
                selection.Invalidate();

                Assert.Equal(selection.Contains(source, value), invoking(source, value));
            }
        }

        sealed class ReimplementingCollection : Collection<object>, ICollection<object>
        {
            public ReimplementingCollection(IList<object> inner) : base(inner) { }

            bool ICollection<object>.Contains(object item) =>
                this.Any(x => string.Equals(x as string, item as string, StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void ASubclassThatReimplementsMembershipIsNotUnwrapped()
        {
            var source = new ReimplementingCollection(new List<object> { "alpha", "beta" });

            var selection = new SelectionMembership();

            Assert.True(((ICollection<object>)source).Contains("ALPHA"));

            Assert.True(selection.Contains(source, "ALPHA"));
            Assert.False(selection.Contains(source, "gamma"));
        }

        sealed class BrokenOnContains : IEnumerable<object>, ICollection<object>
        {
            readonly List<object> items = new() { "a" };

            public IEnumerator<object> GetEnumerator() => items.GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            public int Count => items.Count;
            public bool IsReadOnly => true;

            public bool Contains(object item) => throw new ArgumentNullException("somethingElse");

            public void Add(object item) { }
            public void Clear() { }
            public void CopyTo(object[] array, int arrayIndex) => items.CopyTo(array, arrayIndex);
            public bool Remove(object item) => false;
        }

        [Fact]
        public void AFailureInsideACollectionIsNotReportedAsNotSelected()
        {
            var selection = new SelectionMembership();

            Assert.Throws<ArgumentNullException>(() => selection.Contains(new BrokenOnContains(), "a"));
        }

        [Fact]
        public void TheAheadOfTimeFallbackReportsAFailureAsTheFailureItIs()
        {
            var source = new BrokenOnContains();

            var invoking = CollectionMembership.AskByInvoking(source.GetType())!;

            Assert.Throws<ArgumentNullException>(() => invoking(source, "a"));
        }

        sealed class ForwardingButEnumeratingDifferently : Collection<object>, IEnumerable<object>
        {
            public ForwardingButEnumeratingDifferently(IList<object> inner) : base(inner) { }

            IEnumerator<object> IEnumerable<object>.GetEnumerator() =>
                Enumerable.Empty<object>().GetEnumerator();
        }

        [Fact]
        public void TheMemoIsBuiltFromTheCollectionThePolicyWasChosenFor()
        {
            var backing = new List<object> { "alpha", "beta" };

            var source = new ForwardingButEnumeratingDifferently(backing);

            var selection = new SelectionMembership();

            Assert.True(((ICollection<object>)source).Contains("alpha"));

            Assert.True(selection.Contains(source, "alpha"));
        }

        sealed class WrapperWithAContractOfItsOwn : Collection<string>, ICollection<int>
        {
            readonly List<int> numbers = new() { 42 };

            public WrapperWithAContractOfItsOwn(IList<string> inner) : base(inner) { }

            bool ICollection<int>.Contains(int item) => numbers.Contains(item);

            IEnumerator<int> IEnumerable<int>.GetEnumerator() => numbers.GetEnumerator();

            int ICollection<int>.Count => numbers.Count;
            bool ICollection<int>.IsReadOnly => true;
            void ICollection<int>.Add(int item) { }
            void ICollection<int>.Clear() { }
            void ICollection<int>.CopyTo(int[] array, int index) => numbers.CopyTo(array, index);
            bool ICollection<int>.Remove(int item) => false;
        }

        [Fact]
        public void AWrapperThatAnswersOnAContractOfItsOwnIsNotUnwrapped()
        {
            var source = new WrapperWithAContractOfItsOwn(new List<string> { "alpha" });

            var selection = new SelectionMembership();

            Assert.True(((ICollection<int>)source).Contains(42));

            Assert.True(selection.Contains(source, 42));
            Assert.True(selection.Contains(source, "alpha"));
            Assert.False(selection.Contains(source, 7));
            Assert.False(selection.Contains(source, 42.5));
        }

        sealed class ForwardingButEnumeratingNothingForObjects : Collection<object>, IEnumerable<object>
        {
            public ForwardingButEnumeratingNothingForObjects(IList<object> inner) : base(inner) { }

            IEnumerator<object> IEnumerable<object>.GetEnumerator() =>
                Enumerable.Empty<object>().GetEnumerator();
        }

        [Fact]
        public void TheBindingsObjectContractIsAskedAboutNullEvenWhenWhatItHoldsIsMemoized()
        {
            var backing = new List<object> { null!, "alpha" };

            var source = new ForwardingButEnumeratingNothingForObjects(backing);

            var selection = new SelectionMembership();

            Assert.True(((ICollection<object>)source).Contains(null!));
            Assert.True(source.Cast<object?>().Contains(null));

            using (var enumerator = ((IEnumerable<object>)source).GetEnumerator())
            {
                Assert.False(enumerator.MoveNext());
            }

            Assert.True(selection.Contains(source, null));
            Assert.True(selection.Contains(source, "alpha"));
        }

        sealed class ForwardingButEnumeratingANullForObjects : Collection<string>, IEnumerable<object>
        {
            public ForwardingButEnumeratingANullForObjects(IList<string> inner) : base(inner) { }

            IEnumerator<object> IEnumerable<object>.GetEnumerator()
            {
                yield return null!;
            }
        }

        [Fact]
        public void TheNullScanReadsTheBindingUnderEitherPolicy()
        {
            var backing = new List<string> { "alpha" };

            var source = new ForwardingButEnumeratingANullForObjects(backing);

            var selection = new SelectionMembership();

            Assert.Contains<object?>(null, source.Cast<object?>());
            Assert.False(backing.Cast<object?>().Contains(null));

            Assert.True(selection.Contains(source, null));
            Assert.True(selection.Contains(source, "alpha"));
        }

        sealed class ForwardingButEnumeratingForObjectsToo : Collection<string>, ICollection<object>
        {
            public ForwardingButEnumeratingForObjectsToo(IList<string> inner) : base(inner) { }

            IEnumerator<object> IEnumerable<object>.GetEnumerator()
            {
                yield return null!;
            }

            bool ICollection<object>.Contains(object item) => item == null;

            int ICollection<object>.Count => 1;
            bool ICollection<object>.IsReadOnly => true;
            void ICollection<object>.Add(object item) { }
            void ICollection<object>.Clear() { }
            void ICollection<object>.CopyTo(object[] array, int index) => array[index] = null!;
            bool ICollection<object>.Remove(object item) => false;
        }

        [Fact]
        public void TheNullScanReadsTheBindingRatherThanWhatAnswersForIt()
        {
            var inner = new ForwardingButEnumeratingForObjectsToo(new List<string> { "alpha" });

            var source = new ReadOnlyCollection<string>(inner);

            var selection = new SelectionMembership();

            Assert.Contains<object?>(null, inner.Cast<object?>());
            Assert.False(source.Cast<object?>().Contains(null));

            Assert.False(selection.Contains(source, null));
            Assert.True(selection.Contains(source, "alpha"));
        }

        [Fact]
        public void UnwrappingDoesNotExposeAContractTheBindingNeverForwarded()
        {
            var inner = new WrapperWithAContractOfItsOwn(new List<string> { "alpha" });

            var source = new ReadOnlyCollection<string>(inner);

            var selection = new SelectionMembership();

            Assert.True(((ICollection<int>)inner).Contains(42));
            Assert.True(((ICollection<string>)source).Contains("alpha"));
            Assert.False(source.Cast<object?>().Contains(42));

            Assert.True(selection.Contains(source, "alpha"));
            Assert.False(selection.Contains(source, 42));
        }

        class WrapperHidingItems : Collection<string>
        {
            public WrapperHidingItems(IList<string> inner) : base(inner) { }

            protected new IList<string> Items { get; } = new List<string> { "hidden" };
        }

        [Fact]
        public void AWrapperThatHidesItsBackingListIsStillReadFromTheOneThatAnswers()
        {
            var source = new WrapperHidingItems(new List<string> { "alpha" });

            var selection = new SelectionMembership();

            Assert.Throws<AmbiguousMatchException>(
                () => source.GetType().GetProperty("Items", BindingFlags.Instance | BindingFlags.NonPublic));

            Assert.True(((ICollection<string>)source).Contains("alpha"));
            Assert.False(((ICollection<string>)source).Contains("hidden"));

            Assert.True(selection.Contains(source, "alpha"));
            Assert.False(selection.Contains(source, "hidden"));
        }

        // A separate frame prevents debug locals from keeping the collection alive.
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        static System.WeakReference BuildMemoFromAThrowawayCollection(SelectionMembership selection)
        {
            var source = new List<object> { "a", "b" };

            Assert.True(selection.Contains(source, "a"));

            return new System.WeakReference(source);
        }

        [Fact]
        public void InvalidatingReleasesTheCollectionTheMemoWasBuiltFrom()
        {
            var selection = new SelectionMembership();

            var reference = BuildMemoFromAThrowawayCollection(selection);

            selection.Invalidate();

            Assert.False(selection.Contains(null, "a"));

            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();
            System.GC.Collect();

            Assert.False(reference.IsAlive, "the memo should not still be holding the previous collection");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static WeakReference UseCollectibleCollectionType()
        {
            var assembly = AssemblyBuilder.DefineDynamicAssembly(
                new AssemblyName($"SelectionMembershipTests_{Guid.NewGuid():N}"), AssemblyBuilderAccess.RunAndCollect);
            var module = assembly.DefineDynamicModule("Main");
            var builder = module.DefineType("Values", TypeAttributes.Public, typeof(List<int>));
            builder.DefineDefaultConstructor(MethodAttributes.Public);

            var type = builder.CreateType()!;
            var values = (IList)Activator.CreateInstance(type)!;
            values.Add(42);

            var selection = new SelectionMembership();
            Assert.True(selection.Contains((IEnumerable)values, 42));
            selection.Invalidate();

            return new WeakReference(type);
        }

        [Fact]
        public void TypePolicyCache_DoesNotRetainCollectibleTypes()
        {
            var reference = UseCollectibleCollectionType();

            for (var attempt = 0; attempt < 10 && reference.IsAlive; attempt++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }

            Assert.False(reference.IsAlive);
        }
    }
}
