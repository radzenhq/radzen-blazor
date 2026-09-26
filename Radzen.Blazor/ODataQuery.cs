using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Radzen
{
    /// <summary>
    /// Builds the OData query options <c>$filter</c>, <c>$expand</c>, <c>$orderby</c>, <c>$skip</c>, <c>$top</c>, <c>$count</c> and <c>$apply</c>
    /// for the entity set of <typeparamref name="T" /> from lambda expressions, with the method names of Entity Framework Core.
    /// Every method returns a new query and leaves the one it was called on unchanged. A value a lambda captures is read when the method is called.
    /// Pass the query to <see cref="ODataExtensions.GetODataUri{T}(Uri, ODataQuery{T}, LoadDataArgs?)" /> to get the request URI.
    /// </summary>
    /// <typeparam name="T">The entity type of the entity set.</typeparam>
    /// <example>
    /// <code>
    /// var query = new ODataQuery&lt;Ticket&gt;()
    ///     .Where(ticket =&gt; ticket.Status == TicketStatus.Open &amp;&amp; ticket.Subject.Contains(search))
    ///     .Include(ticket =&gt; ticket.Team).ThenInclude(team =&gt; team.Schedule)
    ///     .OrderByDescending(ticket =&gt; ticket.CreatedAt)
    ///     .Take(10);
    /// var uri = new Uri(baseUri, "Tickets").GetODataUri(query);
    /// </code>
    /// </example>
    public class ODataQuery<T>
    {
        /// <summary>
        /// Initializes a new query that has no options.
        /// </summary>
        public ODataQuery() : this(ODataQueryState.Empty)
        {
        }

        internal ODataQuery(ODataQueryState state)
        {
            State = state;
        }

        internal ODataQueryState State { get; }

        /// <summary>
        /// Filters the entities with <paramref name="predicate" />, translated to <c>$filter</c>. Repeated calls combine with <c>and</c>.
        /// </summary>
        /// <param name="predicate">The condition, e.g. <c>ticket =&gt; ticket.Status == TicketStatus.Open</c>.</param>
        /// <returns>A new query with the filter added.</returns>
        /// <exception cref="NotSupportedException">The predicate uses an expression that has no OData equivalent; the message names it and the alternative.</exception>
        /// <exception cref="InvalidOperationException">The query already has <see cref="Skip" /> or <see cref="Take" />.</exception>
        public ODataQuery<T> Where(Expression<Func<T, bool>> predicate)
        {
            ArgumentNullException.ThrowIfNull(predicate);
            return new(State.Where(predicate, nameof(Where)));
        }

        /// <summary>
        /// Expands the navigation property that <paramref name="navigation" /> selects, translated to <c>$expand</c>.
        /// A path such as <c>ticket =&gt; ticket.Team.Schedule</c> expands each level, and a collection can be filtered, ordered and paged with
        /// <c>Where</c>, <c>OrderBy</c>, <c>OrderByDescending</c>, <c>ThenBy</c>, <c>ThenByDescending</c>, <c>Skip</c> and <c>Take</c>, e.g.
        /// <c>ticket =&gt; ticket.Comments.OrderByDescending(comment =&gt; comment.CreatedAt).Take(3)</c>.
        /// </summary>
        /// <typeparam name="TProperty">The type of the navigation property.</typeparam>
        /// <param name="navigation">The navigation property.</param>
        /// <returns>A new query to which <see cref="ODataExtensions.ThenInclude{T, TPreviousProperty, TProperty}(IIncludableODataQuery{T, TPreviousProperty}, Expression{Func{TPreviousProperty, TProperty}})" /> adds the next level.</returns>
        /// <exception cref="NotSupportedException"><paramref name="navigation" /> does not select a navigation property of <typeparamref name="T" />.</exception>
        /// <exception cref="InvalidOperationException">The navigation property is already included with different options.</exception>
        public IncludableODataQuery<T, TProperty> Include<TProperty>(Expression<Func<T, TProperty>> navigation)
        {
            ArgumentNullException.ThrowIfNull(navigation);
            return new(State.Include(navigation, false));
        }

        /// <summary>
        /// Orders the entities by <paramref name="keySelector" /> ascending, translated to <c>$orderby</c>. Replaces any earlier ordering.
        /// </summary>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <param name="keySelector">The key, e.g. <c>ticket =&gt; ticket.Team.Name</c>.</param>
        /// <returns>A new query to which ThenBy adds the next key.</returns>
        public OrderedODataQuery<T> OrderBy<TKey>(Expression<Func<T, TKey>> keySelector)
        {
            ArgumentNullException.ThrowIfNull(keySelector);
            return new(State.Order(keySelector, false, false, nameof(OrderBy)));
        }

        /// <summary>
        /// Orders the entities by <paramref name="keySelector" /> descending, translated to <c>$orderby</c>. Replaces any earlier ordering.
        /// </summary>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <param name="keySelector">The key, e.g. <c>ticket =&gt; ticket.CreatedAt</c>.</param>
        /// <returns>A new query to which ThenBy adds the next key.</returns>
        public OrderedODataQuery<T> OrderByDescending<TKey>(Expression<Func<T, TKey>> keySelector)
        {
            ArgumentNullException.ThrowIfNull(keySelector);
            return new(State.Order(keySelector, true, false, nameof(OrderByDescending)));
        }

        /// <summary>
        /// Skips <paramref name="count" /> entities, translated to <c>$skip</c>. Call it once, before <see cref="Take" />.
        /// </summary>
        /// <param name="count">The number of entities to skip.</param>
        /// <returns>A new query with <c>$skip</c>.</returns>
        /// <exception cref="InvalidOperationException">The query already has Skip or Take.</exception>
        public ODataQuery<T> Skip(int count)
        {
            return new(State.WithSkip(count));
        }

        /// <summary>
        /// Takes at most <paramref name="count" /> entities, translated to <c>$top</c>. Call it once.
        /// </summary>
        /// <param name="count">The number of entities to return.</param>
        /// <returns>A new query with <c>$top</c>.</returns>
        /// <exception cref="InvalidOperationException">The query already has Take.</exception>
        public ODataQuery<T> Take(int count)
        {
            return new(State.WithTop(count));
        }

        /// <summary>
        /// Requests the total count of the filtered entities, translated to <c>$count=true</c>.
        /// </summary>
        /// <returns>A new query with <c>$count=true</c>.</returns>
        public ODataQuery<T> WithCount()
        {
            return new(State.WithCount());
        }

        /// <summary>
        /// Groups the entities by <paramref name="keySelector" /> for a Select of the group keys and aggregates, translated to
        /// <c>$apply=groupby(...)</c>, or to <c>$apply=aggregate(...)</c> when the key is a constant such as <c>1</c>, which puts every entity in one group.
        /// Every Where before it becomes a <c>filter(...)</c> transformation that runs before the grouping.
        /// </summary>
        /// <typeparam name="TKey">The type of the key: a property path, an anonymous type or tuple of property paths, or a constant.</typeparam>
        /// <param name="keySelector">The key, e.g. <c>license =&gt; license.Currency</c>, <c>license =&gt; new { license.Currency, license.Country }</c> or <c>license =&gt; 1</c>.</param>
        /// <returns>A grouping on which Select sets the result.</returns>
        /// <exception cref="InvalidOperationException">The query has Include, OrderBy, Skip, Take or WithCount, which OData would apply to the groups.</exception>
        /// <exception cref="NotSupportedException">The key is not a property path or a composite of property paths.</exception>
        public GroupedODataQuery<T, TKey> GroupBy<TKey>(Expression<Func<T, TKey>> keySelector)
        {
            ArgumentNullException.ThrowIfNull(keySelector);
            State.EnsureGroupable();
            return new(State, keySelector, ODataAggregate.Keys(keySelector));
        }

        /// <summary>
        /// Returns the query options with each value escaped once with <see cref="Uri.EscapeDataString(string)" />, e.g.
        /// <c>$filter=Name%20eq%20%27O%27%27Brien%27&amp;$top=10</c>.
        /// </summary>
        /// <returns>The query string without the leading question mark, or an empty string when the query has no options.</returns>
        public string ToQueryString()
        {
            return State.ToQueryString(true);
        }

        /// <summary>
        /// Returns the query options unescaped, e.g. <c>$filter=Name eq 'O''Brien'&amp;$top=10</c>.
        /// </summary>
        /// <returns>The readable query options.</returns>
        public override string ToString()
        {
            return State.ToQueryString(false);
        }
    }

    /// <summary>
    /// An <see cref="ODataQuery{T}" /> ordered by at least one key, to which ThenBy and ThenByDescending add keys.
    /// </summary>
    /// <typeparam name="T">The entity type of the entity set.</typeparam>
    public sealed class OrderedODataQuery<T> : ODataQuery<T>
    {
        internal OrderedODataQuery(ODataQueryState state) : base(state)
        {
        }

        /// <summary>
        /// Orders the entities that the earlier keys leave equal by <paramref name="keySelector" /> ascending.
        /// </summary>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <param name="keySelector">The key, e.g. <c>ticket =&gt; ticket.Unit.Code</c>.</param>
        /// <returns>A new query with the key appended to <c>$orderby</c>.</returns>
        public OrderedODataQuery<T> ThenBy<TKey>(Expression<Func<T, TKey>> keySelector)
        {
            ArgumentNullException.ThrowIfNull(keySelector);
            return new(State.Order(keySelector, false, true, nameof(ThenBy)));
        }

        /// <summary>
        /// Orders the entities that the earlier keys leave equal by <paramref name="keySelector" /> descending.
        /// </summary>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <param name="keySelector">The key.</param>
        /// <returns>A new query with the key appended to <c>$orderby</c>.</returns>
        public OrderedODataQuery<T> ThenByDescending<TKey>(Expression<Func<T, TKey>> keySelector)
        {
            ArgumentNullException.ThrowIfNull(keySelector);
            return new(State.Order(keySelector, true, true, nameof(ThenByDescending)));
        }
    }

    /// <summary>
    /// A query whose last Include or ThenInclude expanded a navigation property of type <typeparamref name="TProperty" />, as
    /// Entity Framework Core's IIncludableQueryable. <see cref="ODataExtensions" /> defines ThenInclude on it for reference and collection properties.
    /// </summary>
    /// <typeparam name="T">The entity type of the entity set.</typeparam>
    /// <typeparam name="TProperty">The type of the navigation property that was expanded last.</typeparam>
    public interface IIncludableODataQuery<T, out TProperty>
    {
        internal ODataQuery<T> Query { get; }
    }

    /// <summary>
    /// An <see cref="ODataQuery{T}" /> whose last Include or ThenInclude expanded a navigation property of type <typeparamref name="TProperty" />.
    /// </summary>
    /// <typeparam name="T">The entity type of the entity set.</typeparam>
    /// <typeparam name="TProperty">The type of the navigation property that was expanded last.</typeparam>
    public sealed class IncludableODataQuery<T, TProperty> : ODataQuery<T>, IIncludableODataQuery<T, TProperty>
    {
        internal IncludableODataQuery(ODataQueryState state) : base(state)
        {
        }

        ODataQuery<T> IIncludableODataQuery<T, TProperty>.Query => this;
    }

    /// <summary>
    /// The entities of <typeparamref name="TSource" /> grouped by a key of type <typeparamref name="TKey" />, which Select turns into results.
    /// </summary>
    /// <typeparam name="TSource">The entity type of the entity set.</typeparam>
    /// <typeparam name="TKey">The type of the group key.</typeparam>
    public sealed class GroupedODataQuery<TSource, TKey>
    {
        private readonly ODataQueryState state;
        private readonly LambdaExpression keySelector;
        private readonly List<(string Component, string Path)> keys;

        internal GroupedODataQuery(ODataQueryState state, LambdaExpression keySelector, List<(string Component, string Path)> keys)
        {
            this.state = state;
            this.keySelector = keySelector;
            this.keys = keys;
        }

        /// <summary>
        /// Sets the result of each group, translated to <c>groupby((keys),aggregate(...))</c>. The selector creates a new result and assigns
        /// <c>g.Key</c> or the members of a composite key to members of the same names, and the aggregates <c>g.Sum(x =&gt; ...)</c>, <c>g.Min(x =&gt; ...)</c>,
        /// <c>g.Max(x =&gt; ...)</c>, <c>g.Average(x =&gt; ...)</c>, <c>g.Count()</c> and <c>g.Select(x =&gt; ...).Distinct().Count()</c> to any members.
        /// An aggregated value can be an expression, e.g. <c>g.Sum(x =&gt; x.UnitPrice * x.Quantity * (1 - x.Discount))</c>, and a conversion to another
        /// numeric type in it, e.g. <c>g.Sum(x =&gt; (int)x.Quantity)</c>, is sent as <c>cast(Quantity,Edm.Int32)</c>.
        /// </summary>
        /// <typeparam name="TResult">The type of the result, whose members the response properties deserialize into.</typeparam>
        /// <param name="selector">The result, e.g. <c>g =&gt; new LicenseTotal { Currency = g.Key, TotalPrice = g.Sum(license =&gt; license.Price) }</c>.</param>
        /// <returns>A query of the results.</returns>
        /// <exception cref="NotSupportedException">The selector assigns an expression that OData aggregation cannot return.</exception>
        public ODataQuery<TSource, TResult> Select<TResult>(Expression<Func<IGrouping<TKey, TSource>, TResult>> selector)
        {
            ArgumentNullException.ThrowIfNull(selector);
            return new(state.Group(keySelector, keys, selector));
        }
    }

    /// <summary>
    /// Builds the OData query options of the entity set of <typeparamref name="TSource" /> that return results of type <typeparamref name="TResult" />
    /// computed by <c>$apply</c>. OData applies <c>$apply</c> first, so the Where, OrderBy, Skip and Take of this query work on the results.
    /// </summary>
    /// <typeparam name="TSource">The entity type of the entity set.</typeparam>
    /// <typeparam name="TResult">The type of the results.</typeparam>
    public class ODataQuery<TSource, TResult>
    {
        internal ODataQuery(ODataQueryState state)
        {
            State = state;
        }

        internal ODataQueryState State { get; }

        /// <summary>
        /// Filters the results with <paramref name="predicate" />, translated to a <c>filter(...)</c> transformation after the grouping.
        /// </summary>
        /// <param name="predicate">The condition, e.g. <c>total =&gt; total.TotalPrice &gt; 100</c>.</param>
        /// <returns>A new query with the filter added.</returns>
        public ODataQuery<TSource, TResult> Where(Expression<Func<TResult, bool>> predicate)
        {
            ArgumentNullException.ThrowIfNull(predicate);
            return new(State.Where(predicate, nameof(Where)));
        }

        /// <summary>
        /// Orders the results by <paramref name="keySelector" /> ascending, translated to <c>$orderby</c>. Replaces any earlier ordering.
        /// </summary>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <param name="keySelector">A group key or an aggregate of the result.</param>
        /// <returns>A new query to which ThenBy adds the next key.</returns>
        public OrderedODataQuery<TSource, TResult> OrderBy<TKey>(Expression<Func<TResult, TKey>> keySelector)
        {
            ArgumentNullException.ThrowIfNull(keySelector);
            return new(State.Order(keySelector, false, false, nameof(OrderBy)));
        }

        /// <summary>
        /// Orders the results by <paramref name="keySelector" /> descending, translated to <c>$orderby</c>. Replaces any earlier ordering.
        /// </summary>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <param name="keySelector">A group key or an aggregate of the result.</param>
        /// <returns>A new query to which ThenBy adds the next key.</returns>
        public OrderedODataQuery<TSource, TResult> OrderByDescending<TKey>(Expression<Func<TResult, TKey>> keySelector)
        {
            ArgumentNullException.ThrowIfNull(keySelector);
            return new(State.Order(keySelector, true, false, nameof(OrderByDescending)));
        }

        /// <summary>
        /// Skips <paramref name="count" /> results, translated to <c>$skip</c>. Call it once, before <see cref="Take" />.
        /// </summary>
        /// <param name="count">The number of results to skip.</param>
        /// <returns>A new query with <c>$skip</c>.</returns>
        public ODataQuery<TSource, TResult> Skip(int count)
        {
            return new(State.WithSkip(count));
        }

        /// <summary>
        /// Takes at most <paramref name="count" /> results, translated to <c>$top</c>. Call it once.
        /// </summary>
        /// <param name="count">The number of results to return.</param>
        /// <returns>A new query with <c>$top</c>.</returns>
        public ODataQuery<TSource, TResult> Take(int count)
        {
            return new(State.WithTop(count));
        }

        /// <summary>
        /// Requests the count of the results, translated to <c>$count=true</c>.
        /// </summary>
        /// <returns>A new query with <c>$count=true</c>.</returns>
        public ODataQuery<TSource, TResult> WithCount()
        {
            return new(State.WithCount());
        }

        /// <summary>
        /// Returns the query options with each value escaped once with <see cref="Uri.EscapeDataString(string)" />.
        /// </summary>
        /// <returns>The query string without the leading question mark.</returns>
        public string ToQueryString()
        {
            return State.ToQueryString(true);
        }

        /// <summary>
        /// Returns the query options unescaped, e.g. <c>$apply=filter(Refunded ne true)/groupby((Currency),aggregate(Price with sum as TotalPrice))</c>.
        /// </summary>
        /// <returns>The readable query options.</returns>
        public override string ToString()
        {
            return State.ToQueryString(false);
        }
    }

    /// <summary>
    /// An <see cref="ODataQuery{TSource, TResult}" /> ordered by at least one key, to which ThenBy and ThenByDescending add keys.
    /// </summary>
    /// <typeparam name="TSource">The entity type of the entity set.</typeparam>
    /// <typeparam name="TResult">The type of the results.</typeparam>
    public sealed class OrderedODataQuery<TSource, TResult> : ODataQuery<TSource, TResult>
    {
        internal OrderedODataQuery(ODataQueryState state) : base(state)
        {
        }

        /// <summary>
        /// Orders the results that the earlier keys leave equal by <paramref name="keySelector" /> ascending.
        /// </summary>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <param name="keySelector">A group key or an aggregate of the result.</param>
        /// <returns>A new query with the key appended to <c>$orderby</c>.</returns>
        public OrderedODataQuery<TSource, TResult> ThenBy<TKey>(Expression<Func<TResult, TKey>> keySelector)
        {
            ArgumentNullException.ThrowIfNull(keySelector);
            return new(State.Order(keySelector, false, true, nameof(ThenBy)));
        }

        /// <summary>
        /// Orders the results that the earlier keys leave equal by <paramref name="keySelector" /> descending.
        /// </summary>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <param name="keySelector">A group key or an aggregate of the result.</param>
        /// <returns>A new query with the key appended to <c>$orderby</c>.</returns>
        public OrderedODataQuery<TSource, TResult> ThenByDescending<TKey>(Expression<Func<TResult, TKey>> keySelector)
        {
            ArgumentNullException.ThrowIfNull(keySelector);
            return new(State.Order(keySelector, true, true, nameof(ThenByDescending)));
        }
    }
}
