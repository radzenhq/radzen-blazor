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
        // --- Upstream review (radzenhq/radzen-blazor#2687, @enchev) -------------------------------
        //
        // Both of these are about one thing: the memo may only answer where it answers what master
        // answered. Master asked exactly one question - values.Cast<object?>().Contains(value) - and these
        // pin the two ways this PR had started answering a different one.

        // Overriding Equals without GetHashCode is a warning, not an error, so callers really do ship it.
        // Master scanned with Equals alone and found this; a HashSet cannot, because the two instances
        // hash apart. The memo has to stand down rather than answer differently.
#pragma warning disable 659
        class EqualButNotHashable
        {
            public int Id { get; set; }

            public override bool Equals(object? obj) => obj is EqualButNotHashable other && other.Id == Id;
        }
#pragma warning restore 659

        [Fact]
        public void AValueThatOverridesEqualsWithoutGetHashCodeIsStillAMember()
        {
            var selected = new List<EqualButNotHashable> { new() { Id = 1 } };

            var rendered = new EqualButNotHashable { Id = 1 };

            var selection = new SelectionMembership();

            // The premise: master's question says yes, and a memo built under default hashing cannot.
            Assert.True(selected.Cast<object?>().Contains(rendered));
            Assert.False(new HashSet<object?>(selected.Cast<object?>()).Contains(rendered));

            Assert.True(selection.Contains(selected, rendered));
        }

        // A typed set with a comparer of its own is not an ICollection<object>, so Enumerable.Contains
        // never reached its Contains - it scanned, under default equality. Asking it through its typed
        // contract selects items that were not selected before, which is a behaviour change in a released
        // component whatever else can be said for it.
        [Fact]
        public void ATypedSetWithItsOwnComparerKeepsTheAnswerMasterGave()
        {
            var selected = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "alpha" };

            var selection = new SelectionMembership();

            // The premise: the set says yes about its own element, and master's question says no.
            Assert.True(selected.Contains("ALPHA"));
            Assert.False(selected.Cast<object?>().Contains("ALPHA"));

            Assert.False(selection.Contains(selected, "ALPHA"));
            Assert.True(selection.Contains(selected, "alpha"));
        }

        // The other half of the same rule. ICollection<object> is the one contract Enumerable.Contains did
        // dispatch to, so a collection carrying it has always answered for itself and still must.
        sealed class CaseInsensitiveValueCollection : ICollection<object>
        {
            readonly HashSet<object> items = new(new CaseInsensitiveObjectComparer());

            public int Count => items.Count;
            public bool IsReadOnly => false;
            public void Add(object item) => items.Add(item);
            public void Clear() => items.Clear();
            public bool Contains(object item) => items.Contains(item);
            public void CopyTo(object[] array, int index) => items.CopyTo(array, index);
            public bool Remove(object item) => items.Remove(item);
            public IEnumerator<object> GetEnumerator() => items.GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => items.GetEnumerator();
        }

        sealed class CaseInsensitiveObjectComparer : IEqualityComparer<object>
        {
            public new bool Equals(object? x, object? y) =>
                string.Equals(x as string, y as string, StringComparison.OrdinalIgnoreCase);

            public int GetHashCode(object obj) =>
                obj is string s ? StringComparer.OrdinalIgnoreCase.GetHashCode(s) : obj?.GetHashCode() ?? 0;
        }

        [Fact]
        public void AnUntypedCollectionIsStillAskedForItself()
        {
            var selected = new CaseInsensitiveValueCollection { "alpha" };

            var selection = new SelectionMembership();

            // The premise: this is the contract master dispatched to, so its own answer is master's.
            Assert.True(selected.Cast<object?>().Contains("ALPHA"));

            Assert.True(selection.Contains(selected, "ALPHA"));
        }

        // A caller's own list-like collection: default equality, membership by walking. It records both
        // how many elements were walked and how many times its own Contains was asked, so a test can tell
        // a memoized answer from a per-question scan without timing anything.
        sealed class CountingEnumerable : IEnumerable<object>, ICollection<object>
        {
            readonly List<object> items;

            public CountingEnumerable(params object[] values) => items = new List<object>(values);

            public int Walked { get; private set; }

            public int Asked { get; private set; }

            public IEnumerator<object> GetEnumerator()
            {
                foreach (var item in items)
                {
                    Walked++;

                    yield return item;
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            public int Count => items.Count;

            public bool IsReadOnly => true;

            public bool Contains(object item)
            {
                Asked++;
                Walked += items.Count;

                return items.Contains(item);
            }

            public void Add(object item) => items.Add(item);
            public void Clear() => items.Clear();
            public void CopyTo(object[] array, int arrayIndex) => items.CopyTo(array, arrayIndex);
            public bool Remove(object item) => items.Remove(item);
        }

        // Which bindings the memo may answer for is master's own dispatch, drawn by SelectionEquality: a
        // collection carrying ICollection<object> is what Enumerable.Contains handed the question to, so
        // it keeps answering for itself. Everything else was scanned under default equality, which is the
        // memo's own equality, so memoizing it cannot contradict it. List<object> and object[] are the
        // exception on both counts - they carry the contract, but their Contains *is* that scan.
        //
        // Memoized and asked are told apart by what a mid-pass addition does: the memo is a snapshot of
        // the pass it was built in and does not see one, while asking the collection reads it every time
        // and does.
        public static IEnumerable<object[]> Memoized()
        {
            var array = new object[] { "a", "b", "c" };

            yield return new object[] { array, (Action)(() => array[2] = "z") };

            var list = new List<object> { "a", "b", "c" };

            yield return new object[] { list, (Action)(() => list.Add("z")) };

            var typed = new List<string> { "a", "b", "c" };

            yield return new object[] { typed, (Action)(() => typed.Add("z")) };

            var set = new HashSet<string> { "a", "b", "c" };

            yield return new object[] { set, (Action)(() => set.Add("z")) };
        }

        // And the ones master asked, which therefore answer live rather than from a memo. Each is an
        // ICollection<object> that is not a plain list, so each keeps whatever membership it has - the
        // point of the boundary, not an omission from the one above.
        public static IEnumerable<object[]> Asked()
        {
            var collection = new Collection<object> { "a", "b", "c" };

            yield return new object[] { collection, (Action)(() => collection.Add("z")) };

            var backing = new List<object> { "a", "b", "c" };

            yield return new object[] { new ReadOnlyCollection<object>(backing), (Action)(() => backing.Add("z")) };

            var observable = new ObservableCollection<object> { "a", "b", "c" };

            yield return new object[] { observable, (Action)(() => observable.Add("z")) };

            var linked = new LinkedList<object>(new object[] { "a", "b", "c" });

            yield return new object[] { linked, (Action)(() => linked.AddLast("z")) };
        }

        [Theory]
        [MemberData(nameof(Asked))]
        public void TheCollectionsMasterAskedAreStillAsked(IEnumerable source, Action addZ)
        {
            var selection = new SelectionMembership();

            Assert.True(selection.Contains(source, "a"));
            Assert.False(selection.Contains(source, "z"));

            addZ();

            // Asked, so the addition is visible at once - no memo stands between the two.
            Assert.True(selection.Contains(source, "z"));
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

            // And the next render sees the new value.
            selection.Invalidate();

            Assert.True(selection.Contains(source, "z"));
        }

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

            // Answered under default equality, not the set's comparer - see
            // ATypedSetWithItsOwnComparerKeepsTheAnswerMasterGave. What this pins is that the switch of
            // binding is noticed at all.
            var typed = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "alpha" };
            Assert.True(selection.Contains(typed, "alpha"));
            Assert.False(selection.Contains(typed, "ALPHA"));
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

        public static IEnumerable<object[]> AskableSources()
        {
            yield return new object[] { new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "alpha", "beta" } };

            yield return new object[] { new HashSet<int> { 1, 2, 3 } };

            yield return new object[] { new TwoContracts() };
        }

        sealed class ReimplementingCollection : Collection<object>, ICollection<object>
        {
            public ReimplementingCollection(IList<object> inner) : base(inner) { }

            bool ICollection<object>.Contains(object item) =>
                this.Any(x => string.Equals(x as string, item as string, StringComparison.OrdinalIgnoreCase));
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

        sealed class ForwardingButEnumeratingDifferently : Collection<object>, IEnumerable<object>
        {
            public ForwardingButEnumeratingDifferently(IList<object> inner) : base(inner) { }

            IEnumerator<object> IEnumerable<object>.GetEnumerator() =>
                Enumerable.Empty<object>().GetEnumerator();
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

        sealed class ForwardingButEnumeratingNothingForObjects : Collection<object>, IEnumerable<object>
        {
            public ForwardingButEnumeratingNothingForObjects(IList<object> inner) : base(inner) { }

            IEnumerator<object> IEnumerable<object>.GetEnumerator() =>
                Enumerable.Empty<object>().GetEnumerator();
        }

        sealed class ForwardingButEnumeratingANullForObjects : Collection<string>, IEnumerable<object>
        {
            public ForwardingButEnumeratingANullForObjects(IList<string> inner) : base(inner) { }

            IEnumerator<object> IEnumerable<object>.GetEnumerator()
            {
                yield return null!;
            }
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

        class WrapperHidingItems : Collection<string>
        {
            public WrapperHidingItems(IList<string> inner) : base(inner) { }

            protected new IList<string> Items { get; } = new List<string> { "hidden" };
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
        public void AskingAboutACollectibleCollectionTypeDoesNotRetainIt()
        {
            var reference = UseCollectibleCollectionType();

            for (var attempt = 0; attempt < 10 && reference.IsAlive; attempt++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }

            Assert.False(reference.IsAlive, "the collectible collection type should have been collected");
        }
    }
}
