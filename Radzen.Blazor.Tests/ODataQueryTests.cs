using Radzen.Blazor.Tests.Helpdesk;
using System;
using System.Linq;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class ODataQueryTests
    {
        private static readonly Uri Tickets = new("https://example.com/odata/Tickets");

        private static void AssertQuery<T>(string expected, ODataQuery<T> query, string entitySet = "Tickets")
        {
            Assert.Equal(expected, query.ToString());
            ODataParser.AssertQueryParses(entitySet, query.ToQueryString());
        }

        private static void AssertQuery<TSource, TResult>(string expected, ODataQuery<TSource, TResult> query, string entitySet = "Licenses")
        {
            Assert.Equal(expected, query.ToString());
            ODataParser.AssertQueryParses(entitySet, query.ToQueryString());
        }

        [Fact]
        public void ODataQuery_WithoutOptions_IsEmpty()
        {
            Assert.Equal(string.Empty, new ODataQuery<Ticket>().ToString());
            Assert.Equal(string.Empty, new ODataQuery<Ticket>().ToQueryString());
            Assert.Equal("https://example.com/odata/Tickets", Tickets.GetODataUri(new ODataQuery<Ticket>()).AbsoluteUri);
        }

        [Fact]
        public void ODataQuery_Where_CombinesRepeatedCallsWithAnd()
        {
            var query = new ODataQuery<Ticket>()
                .Where(ticket => ticket.Urgent || ticket.Priority > 3)
                .Where(ticket => ticket.Status == TicketStatus.Open);

            AssertQuery("$filter=(Urgent or Priority gt 3) and Status eq 'Open'", query);
        }

        [Fact]
        public void ODataQuery_Where_DropsAConditionThatFoldsToTrueAndKeepsOneThatFoldsToFalse()
        {
            string search = null;
            TicketStatus? status = null;

            Assert.Equal(string.Empty, new ODataQuery<Ticket>().Where(ticket => string.IsNullOrEmpty(search) || ticket.Subject.Contains(search)).ToString());
            Assert.Equal("$filter=false", new ODataQuery<Ticket>().Where(ticket => ticket.Urgent).Where(ticket => status != null && ticket.Status == status.Value).ToString());
            Assert.Equal("$filter=false", new ODataQuery<Ticket>().Where(ticket => status.HasValue).Where(ticket => ticket.Urgent).ToString());
        }

        [Fact]
        public void ODataQuery_ReadsCapturedValuesWhenAClauseIsAdded()
        {
            var subject = "Printer jam";
            var query = new ODataQuery<Ticket>().Where(ticket => ticket.Subject == subject);
            subject = "changed";
            var second = query.Where(ticket => ticket.Subject != subject);

            Assert.Equal("$filter=Subject eq 'Printer jam'", query.ToString());
            Assert.Equal("$filter=Subject eq 'Printer jam' and Subject ne 'changed'", second.ToString());
        }

        [Fact]
        public void ODataQuery_ReturnsANewQueryAndLeavesTheOriginalUnchanged()
        {
            var original = new ODataQuery<Ticket>().Where(ticket => ticket.Urgent);

            var ordered = original.OrderBy(ticket => ticket.Subject);
            var paged = ordered.Skip(5).Take(5);
            var included = original.Include(ticket => ticket.Team);

            Assert.Equal("$filter=Urgent", original.ToString());
            Assert.Equal("$filter=Urgent&$orderby=Subject", ordered.ToString());
            Assert.Equal("$filter=Urgent&$orderby=Subject&$skip=5&$top=5", paged.ToString());
            Assert.Equal("$filter=Urgent&$expand=Team", included.ToString());
        }

        [Fact]
        public void ODataQuery_Include_ExpandsReferenceChainsThreeLevelsDeep()
        {
            AssertQuery("$expand=Team($expand=Schedule($expand=Owner))", new ODataQuery<Ticket>()
                .Include(ticket => ticket.Team).ThenInclude(team => team.Schedule).ThenInclude(schedule => schedule.Owner));
            AssertQuery("$expand=Team($expand=Schedule($expand=Owner))", new ODataQuery<Ticket>().Include(ticket => ticket.Team.Schedule.Owner));
            AssertQuery("$expand=Team($expand=Schedule($expand=Owner))", new ODataQuery<Ticket>().Include(ticket => ticket.Team.Schedule).ThenInclude(schedule => schedule.Owner));
        }

        [Fact]
        public void ODataQuery_ThenInclude_FollowsACollection()
        {
            AssertQuery("$expand=Comments($expand=Author)", new ODataQuery<Ticket>().Include(ticket => ticket.Comments).ThenInclude(comment => comment.Author));
            AssertQuery("$expand=Team($expand=Members)", new ODataQuery<Ticket>().Include(ticket => ticket.Team).ThenInclude(team => team.Members));
            AssertQuery("$expand=Team($expand=Tickets($expand=Unit))", new ODataQuery<Ticket>().Include(ticket => ticket.Team).ThenInclude(team => team.Tickets).ThenInclude(ticket => ticket.Unit));
        }

        [Fact]
        public void ODataQuery_Include_FiltersOrdersAndPagesACollectionWithSemicolonSeparatedOptions()
        {
            var minimum = 1;

            var query = new ODataQuery<Ticket>().Include(ticket => ticket.Comments
                .Where(comment => comment.Score > minimum)
                .Where(comment => comment.Text != "O'Brien")
                .OrderByDescending(comment => comment.CreatedAt)
                .ThenBy(comment => comment.Author.Name)
                .Skip(1)
                .Take(3))
                .ThenInclude(comment => comment.Author);

            AssertQuery("$expand=Comments($filter=Score gt 1 and Text ne 'O''Brien';$orderby=CreatedAt desc,Author/Name;$skip=1;$top=3;$expand=Author)", query);
        }

        [Fact]
        public void ODataQuery_Include_MergesSharedPathsAndKeepsSiblingBranches()
        {
            var query = new ODataQuery<Ticket>()
                .Include(ticket => ticket.Team).ThenInclude(team => team.Schedule).ThenInclude(schedule => schedule.Owner)
                .Include(ticket => ticket.Unit)
                .Include(ticket => ticket.Team).ThenInclude(team => team.Members)
                .Include(ticket => ticket.Comments.OrderBy(comment => comment.Id).Take(2))
                .Include(ticket => ticket.Comments).ThenInclude(comment => comment.Author);

            AssertQuery("$expand=Team($expand=Schedule($expand=Owner),Members),Unit,Comments($orderby=Id;$top=2;$expand=Author)", query);
        }

        [Fact]
        public void ODataQuery_Include_RejectsDifferentOptionsOnOneNavigation()
        {
            var query = new ODataQuery<Ticket>().Include(ticket => ticket.Comments.Take(2));

            Assert.Equal("$expand=Comments($top=2)", query.Include(ticket => ticket.Comments.Take(2)).ToString());
            var error = Assert.Throws<InvalidOperationException>(() => query.Include(ticket => ticket.Comments.Take(3)));
            Assert.Contains("Include of Comments with ($top=3) conflicts with its earlier include with ($top=2)", error.Message);
        }

        [Theory]
        [InlineData("subject")]
        [InlineData("id")]
        [InlineData("self")]
        [InlineData("skipAfterTake")]
        [InlineData("whereAfterTake")]
        [InlineData("outer")]
        public void ODataQuery_Include_ThrowsForWhatIsNotANavigationOrCannotBeExpanded(string include)
        {
            var query = new ODataQuery<Ticket>();

            Action action = include switch
            {
                "subject" => () => query.Include(ticket => ticket.Subject),
                "id" => () => query.Include(ticket => ticket.Id),
                "self" => () => query.Include(ticket => ticket),
                "skipAfterTake" => () => query.Include(ticket => ticket.Comments.Take(2).Skip(1)),
                "whereAfterTake" => () => query.Include(ticket => ticket.Comments.Take(2).Where(comment => comment.Score > 1)),
                _ => () => query.Include(ticket => ticket.Comments.Where(comment => comment.Score > ticket.Priority)),
            };

            var error = Assert.Throws<NotSupportedException>(action);
            Assert.StartsWith("ODataQuery cannot ", error.Message);
        }

        [Fact]
        public void ODataQuery_OrderBy_WritesNestedMembersAsPathsAndThenByAppends()
        {
            AssertQuery("$orderby=Unit/Code,Team/Schedule/Name desc,Id", new ODataQuery<Ticket>()
                .OrderBy(ticket => ticket.Unit.Code).ThenByDescending(ticket => ticket.Team.Schedule.Name).ThenBy(ticket => ticket.Id));
            AssertQuery("$orderby=CreatedAt desc", new ODataQuery<Ticket>().OrderBy(ticket => ticket.Subject).OrderByDescending(ticket => ticket.CreatedAt));
            AssertQuery("$orderby=tolower(Subject),Comments/$count desc", new ODataQuery<Ticket>().OrderBy(ticket => ticket.Subject.ToLower()).ThenByDescending(ticket => ticket.Comments.Count));
        }

        [Fact]
        public void ODataQuery_OrderBy_ThrowsForAConstantKey()
        {
            var error = Assert.Throws<NotSupportedException>(() => new ODataQuery<Ticket>().OrderBy(ticket => 1));

            Assert.Contains("select a property of Ticket", error.Message);
        }

        [Fact]
        public void ODataQuery_SkipAndTake_WriteSkipAndTopAndRejectLinqSurprisingSequences()
        {
            AssertQuery("$skip=10&$top=5", new ODataQuery<Ticket>().Skip(10).Take(5));
            AssertQuery("$top=5", new ODataQuery<Ticket>().Take(5));

            Assert.Contains("Skip cannot follow Take", Assert.Throws<InvalidOperationException>(() => new ODataQuery<Ticket>().Take(5).Skip(10)).Message);
            Assert.Contains("Skip can be called once", Assert.Throws<InvalidOperationException>(() => new ODataQuery<Ticket>().Skip(5).Skip(10)).Message);
            Assert.Contains("Take can be called once", Assert.Throws<InvalidOperationException>(() => new ODataQuery<Ticket>().Take(5).Take(10)).Message);
            Assert.Contains("Where cannot follow Skip or Take", Assert.Throws<InvalidOperationException>(() => new ODataQuery<Ticket>().Take(5).Where(ticket => ticket.Urgent)).Message);
            Assert.Contains("OrderBy cannot follow Skip or Take", Assert.Throws<InvalidOperationException>(() => new ODataQuery<Ticket>().Skip(5).OrderBy(ticket => ticket.Id)).Message);
            Assert.Throws<ArgumentOutOfRangeException>(() => new ODataQuery<Ticket>().Take(-1));
        }

        [Fact]
        public void ODataQuery_WritesEveryOptionInOneQueryString()
        {
            var query = new ODataQuery<Ticket>()
                .Where(ticket => ticket.Status == TicketStatus.Open)
                .Include(ticket => ticket.Team)
                .OrderByDescending(ticket => ticket.CreatedAt)
                .Skip(20)
                .Take(10)
                .WithCount();

            AssertQuery("$filter=Status eq 'Open'&$expand=Team&$orderby=CreatedAt desc&$skip=20&$top=10&$count=true", query);
        }

        [Fact]
        public void ODataQuery_ToQueryString_EscapesEachValueOnceAndOnlyDoublesApostrophes()
        {
            var query = new ODataQuery<Ticket>().Where(ticket => ticket.Subject == "O'Brien \"Jr\" & 100% #1 + \u2603").Take(5);

            Assert.Equal("$filter=Subject eq 'O''Brien \"Jr\" & 100% #1 + \u2603'&$top=5", query.ToString());
            Assert.Equal("$filter=Subject%20eq%20%27O%27%27Brien%20%22Jr%22%20%26%20100%25%20%231%20%2B%20%E2%98%83%27&$top=5", query.ToQueryString());
        }

        [Fact]
        public void GetODataUri_KeepsOtherParametersAndReplacesTheOptionsTheQuerySets()
        {
            var query = new ODataQuery<Ticket>().Where(ticket => ticket.CreatedAt > new DateTimeOffset(2026, 1, 31, 9, 30, 0, TimeSpan.FromHours(2))).Take(3);

            var uri = new Uri("https://example.com/odata/Tickets?api-key=a%26b&$top=100&$select=Id").GetODataUri(query);

            Assert.Equal("https://example.com/odata/Tickets?api-key=a%26b&$select=Id&$filter=CreatedAt%20gt%202026-01-31T09%3A30%3A00%2B02%3A00&$top=3", uri.AbsoluteUri);
        }

        [Fact]
        public void GetODataUri_AppliesTheGridArgumentsLast()
        {
            var query = new ODataQuery<Ticket>().Where(ticket => ticket.Status == TicketStatus.Open).OrderBy(ticket => ticket.Id).Take(50);
            var args = new LoadDataArgs { Filter = "Priority ge 2 or Urgent eq true", OrderBy = "Subject desc", Skip = 10, Top = 5 };

            var uri = Tickets.GetODataUri(query, args);

            Assert.Equal("$filter=(Priority ge 2 or Urgent eq true) and (Status eq 'Open')&$orderby=Subject desc&$skip=10&$top=5&$count=true", Uri.UnescapeDataString(uri.Query.TrimStart('?')));
            ODataParser.AssertQueryParses("Tickets", uri.Query.TrimStart('?'));
        }

        [Fact]
        public void GetODataUri_KeepsTheTypedOrderWhileTheGridIsUnsortedAndOmitsEmptyFilterParts()
        {
            var typed = new ODataQuery<Ticket>().Where(ticket => ticket.Urgent).OrderByDescending(ticket => ticket.CreatedAt);
            var untyped = new ODataQuery<Ticket>().OrderBy(ticket => ticket.Id);

            Assert.Equal("$filter=Urgent&$orderby=CreatedAt desc&$count=true", Uri.UnescapeDataString(Tickets.GetODataUri(typed, new LoadDataArgs()).Query.TrimStart('?')));
            Assert.Equal("$filter=Priority gt 1&$orderby=Id&$skip=0&$top=10&$count=true", Uri.UnescapeDataString(Tickets.GetODataUri(untyped, new LoadDataArgs { Filter = "Priority gt 1", OrderBy = "", Skip = 0, Top = 10 }).Query.TrimStart('?')));
            Assert.Equal("$filter=Urgent&$orderby=CreatedAt desc", Uri.UnescapeDataString(Tickets.GetODataUri(typed).Query.TrimStart('?')));
        }

        [Fact]
        public void GetODataUri_KeepsTheGridFilterDoubleQuotes()
        {
            var uri = Tickets.GetODataUri(new ODataQuery<Ticket>(), new LoadDataArgs { Filter = "Subject eq 'say \"hi\"'" });

            Assert.Equal("$filter=Subject eq 'say \"hi\"'&$count=true", Uri.UnescapeDataString(uri.Query.TrimStart('?')));
        }

        [Fact]
        public void ODataQuery_GroupBy_TranslatesTheLicensesTotalsWithTheWhereFoldedIntoApply()
        {
            var query = new ODataQuery<License>()
                .Where(license => license.Refunded != true)
                .GroupBy(license => license.Currency)
                .Select(group => new LicenseTotal { Currency = group.Key, TotalPrice = group.Sum(license => license.Price) });

            AssertQuery("$apply=filter(Refunded ne true)/groupby((Currency),aggregate(Price with sum as TotalPrice))", query);
        }

        [Fact]
        public void GetODataUri_FoldsOnlyTheGridFilterIntoAGroupedQuery()
        {
            var search = "radzen";
            var query = new ODataQuery<License>()
                .Where(license => license.Refunded != true)
                .Where(license => license.Email.Contains(search) || license.LicenseKey.Contains(search) || license.ProductName.Contains(search))
                .GroupBy(license => license.Currency)
                .Select(group => new LicenseTotal { Currency = group.Key, TotalPrice = group.Sum(license => license.Price) });
            var args = new LoadDataArgs { Filter = "Currency ne 'GBP'", OrderBy = "Email", Skip = 20, Top = 10 };

            var uri = new Uri("https://example.com/odata/Licenses").GetODataUri(query, args);

            Assert.Equal("$apply=filter((Currency ne 'GBP') and (Refunded ne true and (contains(Email,'radzen') or contains(LicenseKey,'radzen') or contains(ProductName,'radzen'))))/groupby((Currency),aggregate(Price with sum as TotalPrice))", Uri.UnescapeDataString(uri.Query.TrimStart('?')));
            ODataParser.AssertQueryParses("Licenses", uri.Query.TrimStart('?'));
        }

        [Fact]
        public void ODataQuery_GroupBy_TranslatesCompositeKeysEveryAggregateAndTheResultStage()
        {
            var query = new ODataQuery<License>()
                .GroupBy(license => new { license.Currency, license.ProductName })
                .Select(group => new LicenseSummary
                {
                    Currency = group.Key.Currency,
                    ProductName = group.Key.ProductName,
                    Count = group.Count(),
                    Customers = group.Select(license => license.Email).Distinct().Count(),
                    Lowest = group.Min(license => license.Price),
                    Highest = group.Max(license => license.Price),
                    Mean = group.Average(license => license.Price),
                })
                .Where(summary => summary.Count > 1)
                .OrderByDescending(summary => summary.Highest)
                .ThenBy(summary => summary.Currency)
                .Skip(1)
                .Take(2)
                .WithCount();

            AssertQuery("$apply=groupby((Currency,ProductName),aggregate($count as Count,Email with countdistinct as Customers,Price with min as Lowest,Price with max as Highest,Price with average as Mean))/filter(Count gt 1)&$orderby=Highest desc,Currency&$skip=1&$top=2&$count=true", query);
        }

        [Fact]
        public void ODataQuery_GroupBy_AcceptsTupleKeysAndAnonymousResults()
        {
            var query = new ODataQuery<License>()
                .GroupBy(license => new ValueTuple<string, string>(license.Currency, license.ProductName))
                .Select(group => new { Currency = group.Key.Item1, ProductName = group.Key.Item2, Total = group.Sum(license => license.Price) });

            AssertQuery("$apply=groupby((Currency,ProductName),aggregate(Price with sum as Total))", query);
        }

        [Fact]
        public void ODataQuery_GroupBy_TranslatesAConstantKeyToAggregateWithoutGroupBy()
        {
            var query = new ODataQuery<OrderLine>()
                .GroupBy(line => 1)
                .Select(group => new LineTotals
                {
                    Amount = group.Sum(line => line.UnitPrice * line.Quantity * (1 - line.Discount)),
                    TotalQuantity = group.Sum(line => (int)line.Quantity),
                    AveragePrice = group.Average(line => line.UnitPrice),
                    AverageDiscount = group.Average(line => line.Discount),
                    Lines = group.Count(),
                });

            AssertQuery("$apply=aggregate((UnitPrice mul Quantity mul (1 sub Discount)) with sum as Amount,cast(Quantity,Edm.Int32) with sum as TotalQuantity,UnitPrice with average as AveragePrice,Discount with average as AverageDiscount,$count as Lines)", query, "OrderLines");
        }

        [Fact]
        public void GetODataUri_FoldsTheWhereAndTheGridFilterIntoAConstantKeyAggregate()
        {
            var query = new ODataQuery<OrderLine>()
                .Where(line => line.Category != "Seafood")
                .GroupBy(line => 1)
                .Select(group => new LineTotals { Amount = group.Sum(line => line.UnitPrice * line.Quantity) });

            var uri = new Uri("https://example.com/odata/OrderLines").GetODataUri(query, new LoadDataArgs { Filter = "Quantity gt 5", OrderBy = "UnitPrice", Skip = 1, Top = 2 });

            Assert.Equal("$apply=filter((Quantity gt 5) and (Category ne 'Seafood'))/aggregate((UnitPrice mul Quantity) with sum as Amount)", Uri.UnescapeDataString(uri.Query.TrimStart('?')));
            ODataParser.AssertQueryParses("OrderLines", uri.Query.TrimStart('?'));
        }

        [Fact]
        public void ODataQuery_GroupBy_CastsWhereAnAggregateChangesTheNumericType()
        {
            var query = new ODataQuery<OrderLine>()
                .GroupBy(line => line.Category)
                .Select(group => new LineTotals
                {
                    Category = group.Key,
                    TotalQuantity = group.Sum(line => line.Quantity),
                    LongQuantity = group.Sum(line => (long)line.Quantity),
                    HighestDiscount = group.Max(line => (double)line.Discount),
                    WholePrices = group.Sum(line => (int)line.UnitPrice),
                    Amount = group.Sum(line => (int)line.Quantity * 2 + line.UnitPrice),
                });

            AssertQuery("$apply=groupby((Category),aggregate(cast(Quantity,Edm.Int32) with sum as TotalQuantity,cast(Quantity,Edm.Int64) with sum as LongQuantity,cast(Discount,Edm.Double) with max as HighestDiscount,cast(UnitPrice,Edm.Int32) with sum as WholePrices,(cast(Quantity,Edm.Int32) mul 2 add UnitPrice) with sum as Amount))", query, "OrderLines");
        }

        [Fact]
        public void ODataQuery_GroupBy_UnwrapsTheImplicitDecimalConversionAndCastsTheExplicitOne()
        {
            var query = new ODataQuery<Ticket>()
                .GroupBy(ticket => 1)
                .Select(group => new { Amount = group.Sum(ticket => ticket.Discount * ticket.Priority), Whole = group.Sum(ticket => (int)ticket.Price) });

            AssertQuery("$apply=aggregate((Discount mul Priority) with sum as Amount,cast(Price,Edm.Int32) with sum as Whole)", query, "Tickets");
        }

        [Fact]
        public void ODataQuery_Where_KeepsUnwrappingTheConversionsCSharpInsertsOutsideAggregates()
        {
            AssertQuery("$filter=Quantity gt 5 and UnitPrice mul Quantity gt 100", new ODataQuery<OrderLine>().Where(line => (int)line.Quantity > 5 && line.UnitPrice * line.Quantity > 100), "OrderLines");
        }

        [Fact]
        public void ODataQuery_GroupBy_ThrowsForAConstantKeyWithoutAggregatesOrWithTheKeySelected()
        {
            var lines = new ODataQuery<OrderLine>().GroupBy(line => 1);

            Assert.Contains("select only aggregates", Assert.Throws<NotSupportedException>(() => lines.Select(group => new { group.Key, Lines = group.Count() })).Message);
            Assert.Contains("select at least one aggregate", Assert.Throws<NotSupportedException>(() => lines.Select(group => new LineTotals { })).Message);
            Assert.Contains("aggregate a property of OrderLine", Assert.Throws<NotSupportedException>(() => lines.Select(group => new LineTotals { Amount = group.Sum(line => 1.5) })).Message);
        }

        [Fact]
        public void ODataQuery_GroupBy_ThrowsWhereOData()
        {
            var licenses = new ODataQuery<License>();
            var tickets = new ODataQuery<Ticket>();

            Assert.Contains("GroupBy cannot follow Include", Assert.Throws<InvalidOperationException>(() => tickets.Include(ticket => ticket.Team).GroupBy(ticket => ticket.Status)).Message);
            Assert.Contains("GroupBy cannot follow OrderBy, Skip, Take or WithCount", Assert.Throws<InvalidOperationException>(() => licenses.OrderBy(license => license.Id).GroupBy(license => license.Currency)).Message);
            Assert.Contains("GroupBy cannot follow OrderBy, Skip, Take or WithCount", Assert.Throws<InvalidOperationException>(() => licenses.Take(5).GroupBy(license => license.Currency)).Message);
            Assert.Contains("name the member Currency", Assert.Throws<NotSupportedException>(() => licenses.GroupBy(license => license.Currency).Select(group => new { Code = group.Key })).Message);
            Assert.Contains("nested under Team", Assert.Throws<NotSupportedException>(() => tickets.GroupBy(ticket => ticket.Team.Name).Select(group => new { Name = group.Key })).Message);
            Assert.Contains("OData groups by property paths", Assert.Throws<NotSupportedException>(() => licenses.GroupBy(license => license.CreatedAt.Year)).Message);
            Assert.Contains("a grouped Select assigns g.Key", Assert.Throws<NotSupportedException>(() => licenses.GroupBy(license => license.Currency).Select(group => new { Currency = group.Key, First = group.First().Email })).Message);
            Assert.Contains("a grouped Select assigns g.Key", Assert.Throws<NotSupportedException>(() => licenses.GroupBy(license => license.Currency).Select(group => group.Count())).Message);
        }

        [Fact]
        public void ODataQuery_TranslatesTheAppRadzenComPages()
        {
            var date = new DateTime(2026, 9, 26, 15, 45, 0, DateTimeKind.Utc);

            AssertQuery("$filter=day(Date) eq 26 and month(Date) eq 9 and year(Date) eq 2026", new ODataQuery<Log>()
                .Where(log => log.Date.Day == date.Day && log.Date.Month == date.Month && log.Date.Year == date.Year), "Logs");
            AssertQuery("$filter=date(Date) eq 2026-09-26", new ODataQuery<Log>().Where(log => log.Date.Date == date.Date), "Logs");
            AssertQuery("$filter=Events/any(e:e/Type eq 'trial_check' and e/remaining eq 15)&$expand=Events&$orderby=Id&$count=true", new ODataQuery<AnalyticsSession>()
                .Where(session => session.Events.Any(e => e.Type == "trial_check" && (int)e.Data["remaining"] == 15))
                .Include(session => session.Events)
                .OrderBy(session => session.Id)
                .WithCount(), "AnalyticsSessions");
        }
    }
}
