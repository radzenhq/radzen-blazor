using Radzen.Blazor.Tests.Helpdesk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class ODataQueryServerTests(ODataServer server) : IClassFixture<ODataServer>
    {
        private Uri Set(string name) => new(server.Root, name);

        private static int[] Ids(JsonElement root) => root.GetProperty("value").EnumerateArray().Select(item => item.GetProperty("Id").GetInt32()).ToArray();

        private async Task<int[]> TicketIds(ODataQuery<Ticket> query) => Ids(await server.GetAsync(Set("Tickets").GetODataUri(query)));

        public static TheoryData<Expression<Func<Ticket, bool>>> Conditions()
        {
            var name = "O'Brien \"Jr\" & 100%";
            var search = "PRINTER";
            var status = TicketStatus.Closed;
            var since = new DateTime(2026, 1, 31, 9, 30, 0);
            var utcSince = DateTime.SpecifyKind(since, DateTimeKind.Utc);
            var lastSecond = new DateTime(2026, 1, 31, 23, 59, 59);
            var instant = new DateTimeOffset(2026, 1, 31, 9, 30, 0, TimeSpan.FromHours(2));
            var midnight = new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero);
            var eastern = new DateTimeOffset(2026, 1, 31, 23, 30, 0, TimeSpan.FromHours(-5));
            var newYear = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.FromHours(1));
            var due = new DateOnly(2026, 2, 1);
            var reference = new Guid("5d0c8c1e-8f0a-4f6e-9a1d-2b7f3c4d5e6f");
            int? teamId = 2;
            TicketStatus? anyStatus = null;
            TicketStatus? someStatus = TicketStatus.Open;
            var team = new Team { Id = 1, Name = "Support" };
            var ids = new List<int> { 1, 3, 5 };
            var none = new List<int>();
            var statuses = new[] { TicketStatus.Open, TicketStatus.Pending };
            var subjects = new[] { name, "Printer jam" };
            var references = new[] { reference };
            var day = new DateTime(2026, 1, 31);
            var both = Channel.Email | Channel.Phone;

            return new()
            {
                ticket => ticket.Subject == "Printer jam",
                ticket => ticket.Subject == name,
                ticket => ticket.Subject != null,
                ticket => ticket.Priority > 2,
                ticket => ticket.Priority <= 3 && ticket.Priority >= 1,
                ticket => ticket.Price < 10.5m,
                ticket => ticket.Hours >= 1.25,
                ticket => ticket.Urgent,
                ticket => !ticket.Urgent,
                ticket => ticket.Urgent == false,
                ticket => ticket.Reference == reference,
                ticket => ticket.Status == TicketStatus.Open,
                ticket => ticket.Status != status,
                ticket => ticket.Previous == TicketStatus.Closed,
                ticket => ticket.Previous == null,
                ticket => ticket.OpenedAt >= since,
                ticket => ticket.OpenedAt < new DateTime(2026, 1, 31, 9, 30, 15, 500),
                ticket => ticket.CreatedAt > instant,
                ticket => ticket.Due <= due,
                ticket => ticket.ClosedAt == null,
                ticket => ticket.ClosedAt.HasValue && ticket.ClosedAt.Value < since,
                ticket => ticket.ClosedAt > since,
                ticket => ticket.TeamId == teamId,
                ticket => ticket.Team.Name == team.Name,
                ticket => ticket.Team != null,
                ticket => ticket.Subject.Contains("jam"),
                ticket => ticket.Subject.ToLower().Contains(search.ToLower()),
                ticket => ticket.Subject.ToUpper().StartsWith("SCREEN"),
                ticket => ticket.Subject.EndsWith("jam"),
                ticket => ticket.Urgent || ticket.Priority == 1 && !(ticket.Status == TicketStatus.Closed),
                ticket => !anyStatus.HasValue || ticket.Status == anyStatus.Value,
                ticket => !teamId.HasValue || ticket.TeamId == teamId.Value,
                ticket => string.IsNullOrEmpty(search) || ticket.Subject.Contains(search),
                ticket => anyStatus != null && ticket.Status == anyStatus.Value,
                ticket => (anyStatus == null || ticket.Status == anyStatus.Value) && ticket.Urgent,
                ticket => (someStatus == null || ticket.Status == someStatus.Value) && ticket.Urgent,
                ticket => ticket.Subject.Length > 12,
                ticket => ticket.Priority * 2 > 5,

                ticket => ticket.Subject == "a+b=c #1 ?x",
                ticket => ticket.Subject.Contains("%"),
                ticket => ticket.Subject.Contains("&"),
                ticket => ticket.Subject.Contains("#"),
                ticket => ticket.Subject.Contains("+"),
                ticket => ticket.Subject.Contains("?"),
                ticket => ticket.Subject.Contains("\""),
                ticket => ticket.Subject.Contains("'"),
                ticket => ticket.Subject == "\u00dcn\u00efc\u00f8d\u00e9 \u2603 \u65e5\u672c",
                ticket => ticket.Subject.Contains("\u2603"),
                ticket => ticket.Subject.StartsWith("O'"),
                ticket => ticket.Subject.StartsWith("50%"),
                ticket => ticket.Subject.EndsWith("_sale"),
                ticket => ticket.Team.Name == "O'Brien & Co",

                ticket => ticket.Discount > 1,
                ticket => ticket.Discount == null,
                ticket => ticket.Discount == 0,
                ticket => ticket.Discount.HasValue && ticket.Discount.Value < 1,
                ticket => !ticket.Discount.HasValue,
                ticket => ticket.Escalated == true,
                ticket => ticket.Escalated == false,
                ticket => ticket.Escalated == null,
                ticket => ticket.Previous == ticket.Status,
                ticket => ticket.Previous == null || ticket.Previous == TicketStatus.Open,
                ticket => ticket.ClosedAt > ticket.OpenedAt,

                ticket => ticket.OpenedAt >= utcSince,
                ticket => ticket.OpenedAt < lastSecond,
                ticket => ticket.OpenedAt <= lastSecond,
                ticket => ticket.CreatedAt >= midnight,
                ticket => ticket.CreatedAt == eastern,
                ticket => ticket.CreatedAt < eastern,
                ticket => ticket.CreatedAt >= newYear,
                ticket => ticket.Due == new DateOnly(2026, 1, 31),
                ticket => ticket.Due.Year == 2025,
                ticket => ticket.Due.Month == 2 && ticket.Due.Day == 1,
                ticket => ticket.OpenedAt.Date == day,
                ticket => ticket.ClosedAt != null && ticket.ClosedAt.Value.Date == new DateTime(2026, 1, 5),
                ticket => ticket.OpenedAt.Year == 2026 && ticket.OpenedAt.Month == 1 && ticket.OpenedAt.Day == 31,
                ticket => ticket.OpenedAt.Hour == 23 && ticket.OpenedAt.Minute == 59 && ticket.OpenedAt.Second == 59,
                ticket => ticket.Effort > TimeSpan.FromMinutes(40),
                ticket => ticket.Effort == TimeSpan.Zero,

                ticket => (int)ticket.Status == 2,
                ticket => statuses.Contains(ticket.Status),
                ticket => !statuses.Contains(ticket.Status),
                ticket => ticket.Channels == Channel.Email,
                ticket => ticket.Channels == both,
                ticket => ticket.Channels.HasFlag(Channel.Email),
                ticket => ticket.Channels.HasFlag(Channel.Email | Channel.Chat),

                ticket => ids.Contains(ticket.Id),
                ticket => new[] { 2, 4 }.Contains(ticket.Id),
                ticket => none.Contains(ticket.Id),
                ticket => subjects.Contains(ticket.Subject),
                ticket => references.Contains(ticket.Reference),

                ticket => ticket.Subject.IndexOf("jam") == 8,
                ticket => ticket.Subject.Substring(0, 6) == "Screen",
                ticket => ticket.Subject.Substring(4) == "ter jam",
                ticket => ticket.Subject.Trim() == "Printer jam",
                ticket => ticket.Subject + "!" == "Printer jam!",
                ticket => string.IsNullOrEmpty(ticket.Subject),
                ticket => ticket.Subject.Contains("JAM", StringComparison.OrdinalIgnoreCase),
                ticket => ticket.Subject.StartsWith("P", StringComparison.Ordinal),
                ticket => ticket.Subject.ToUpper() == "PRINTER JAM",

                ticket => (ticket.Priority + 1) * 2 > 6,
                ticket => ticket.Priority % 2 == 0,
                ticket => ticket.Priority / 2 == 1,
                ticket => -ticket.Priority < -3,
                ticket => ticket.Hours * 2 >= 4,
                ticket => ticket.Price > ticket.Priority,
                ticket => ticket.Discount * ticket.Priority > 10,
                ticket => ticket.Views > 1000000000L,
                ticket => ticket.Rating >= 3.25f,
                ticket => Math.Floor(ticket.Hours) == 1,
                ticket => Math.Ceiling(ticket.Hours) == 2,
                ticket => (ticket.Priority > 1) == ticket.Urgent,

                ticket => ticket.Team.Schedule != null && ticket.Team.Schedule.Owner != null && ticket.Team.Schedule.Owner.Name == "Ann",
                ticket => ticket.Unit == null,
                ticket => ticket.Unit != null && ticket.Unit.Code == "KG",

                ticket => ticket.Comments.Any(),
                ticket => !ticket.Comments.Any(),
                ticket => ticket.Comments.Any(comment => comment.Score > 3),
                ticket => ticket.Comments.All(comment => comment.Score > 1),
                ticket => ticket.Comments.Count > 1,
                ticket => ticket.Comments.Count() == 1,
                ticket => ticket.Team.Members.Any(member => member.Name == "Bob"),
                ticket => ticket.Comments.Any(comment => comment.Score > ticket.Priority),
                ticket => ticket.Comments.Any(comment => ticket.Team.Members.Any(member => member.Id == comment.AuthorId)),
                ticket => ticket.Comments.Any(comment => comment.Author.Name == "Ann" && comment.Text.Contains("O")),
            };
        }

        public static TheoryData<Expression<Func<Ticket, bool>>> NullAndRoundingConditions() => new()
        {
            ticket => !(ticket.Discount > 1),
            ticket => ticket.Discount != 0,
            ticket => !(ticket.Discount == 0),
            ticket => ticket.Escalated != true,
            ticket => ticket.Escalated != false,
            ticket => !(ticket.Escalated == true),
            ticket => ticket.Previous != ticket.Status,
            ticket => ticket.Previous != TicketStatus.Closed,
            ticket => !(ticket.ClosedAt > ticket.OpenedAt),
            ticket => Math.Round(ticket.Hours, MidpointRounding.AwayFromZero) == 1,
        };

        public static TheoryData<Expression<Func<Ticket, bool>>> TimeOfDayConditions()
        {
            var nine = new TimeOnly(9, 0);

            return new()
            {
                ticket => ticket.Slot >= nine,
                ticket => ticket.Slot == new TimeOnly(13, 30),
                ticket => ticket.Slot != null && ticket.Slot.Value.Hour == 13,
                ticket => ticket.Slot != null && ticket.Slot.Value.Minute == 59 && ticket.Slot.Value.Second == 59,
            };
        }

        [Theory]
        [MemberData(nameof(Conditions))]
        [MemberData(nameof(NullAndRoundingConditions))]
        public async Task ODataQuery_Where_ReturnsFromEntityFrameworkTheTicketsTheSameLambdaReturnsInMemory(Expression<Func<Ticket, bool>> where)
        {
            var expected = HelpdeskSeed.Tickets.Where(where.Compile()).Select(ticket => ticket.Id).Order().ToArray();

            var actual = await TicketIds(new ODataQuery<Ticket>().Where(where).OrderBy(ticket => ticket.Id));

            Assert.Equal(expected, actual);
        }

        [Theory]
        [MemberData(nameof(Conditions))]
        [MemberData(nameof(TimeOfDayConditions))]
        public async Task ODataQuery_Where_ReturnsFromLinqToObjectsTheTicketsTheSameLambdaReturnsInMemory(Expression<Func<Ticket, bool>> where)
        {
            var expected = HelpdeskSeed.Tickets.Where(where.Compile()).Select(ticket => ticket.Id).Order().ToArray();

            var actual = Ids(await server.GetAsync(Set("MemoryTickets").GetODataUri(new ODataQuery<Ticket>().Where(where).OrderBy(ticket => ticket.Id))));

            Assert.Equal(expected, actual);
        }

        [Fact]
        public async Task ODataQuery_Where_ConvertsALocalDateTimeToUtcForTheServer()
        {
            var local = DateTime.SpecifyKind(new DateTime(2026, 1, 31, 9, 30, 0), DateTimeKind.Local);
            var utc = local.ToUniversalTime();

            var actual = await TicketIds(new ODataQuery<Ticket>().Where(ticket => ticket.OpenedAt >= local).OrderBy(ticket => ticket.Id));

            Assert.Equal(HelpdeskSeed.Tickets.Where(ticket => ticket.OpenedAt >= utc).Select(ticket => ticket.Id).Order(), actual);
        }

        [Fact]
        public async Task ODataQuery_Where_ResolvesARenamedShadowedVariableOnTheServer()
        {
            var ticket = Expression.Parameter(typeof(Ticket), "ticket");
            var comment = Expression.Parameter(typeof(Comment), "x");
            var member = Expression.Parameter(typeof(Member), "x");
            var inner = Expression.Call(typeof(Enumerable), nameof(Enumerable.Any), [typeof(Member)], Expression.Property(Expression.Property(ticket, nameof(Ticket.Team)), nameof(Team.Members)),
                Expression.Lambda<Func<Member, bool>>(Expression.AndAlso(Expression.Equal(Expression.Property(member, nameof(Member.Id)), Expression.Property(comment, nameof(Comment.AuthorId))), Expression.Equal(Expression.Property(member, nameof(Member.Name)), Expression.Constant("Bob"))), member));
            var where = Expression.Lambda<Func<Ticket, bool>>(Expression.Call(typeof(Enumerable), nameof(Enumerable.Any), [typeof(Comment)], Expression.Property(ticket, nameof(Ticket.Comments)),
                Expression.Lambda<Func<Comment, bool>>(inner, comment)), ticket);

            var actual = await TicketIds(new ODataQuery<Ticket>().Where(where).OrderBy(ticket => ticket.Id));

            Assert.Equal(HelpdeskSeed.Tickets.Where(where.Compile()).Select(ticket => ticket.Id).Order(), actual);
            Assert.NotEmpty(actual);
        }

        [Theory]
        [InlineData("Status eq 'Open'", new[] { 1, 3, 6 })]
        [InlineData("Status eq Radzen.Blazor.Tests.Helpdesk.TicketStatus'Open'", new[] { 1, 3, 6 })]
        [InlineData("Previous eq 'Closed'", new[] { 2, 6 })]
        [InlineData("Status in ('Open','Pending')", new[] { 1, 3, 4, 6 })]
        [InlineData("Channels has 'Email'", new[] { 1, 5, 6 })]
        [InlineData("Channels eq Radzen.Blazor.Tests.Helpdesk.Channel'Email,Phone'", new[] { 1 })]
        public async Task Server_AcceptsUnqualifiedEnumMembersAndQualifiedFlagCombinations(string filter, int[] expected)
        {
            var (success, body) = await server.TryGetAsync($"Tickets?$filter={Uri.EscapeDataString(filter)}&$orderby=Id");

            Assert.True(success, body);
            using var json = JsonDocument.Parse(body);
            Assert.Equal(expected, Ids(json.RootElement));
        }

        [Theory]
        [InlineData("Channels eq 'Email,Phone'")]
        [InlineData("Channels eq 'Email, Phone'")]
        public async Task Server_RejectsAnUnqualifiedFlagCombination(string filter)
        {
            var (success, body) = await server.TryGetAsync($"Tickets?$filter={Uri.EscapeDataString(filter)}");

            Assert.False(success);
            Assert.Contains("is not a valid enumeration type constant", body);
        }

        [Fact]
        public async Task ODataQuery_Include_ExpandsThreeLevelsAndSharedBranches()
        {
            var query = new ODataQuery<Ticket>()
                .Where(ticket => ticket.Id == 1)
                .Include(ticket => ticket.Team).ThenInclude(team => team.Schedule).ThenInclude(schedule => schedule.Owner)
                .Include(ticket => ticket.Team).ThenInclude(team => team.Members)
                .Include(ticket => ticket.Unit);

            var ticket = (await server.GetAsync(Set("Tickets").GetODataUri(query))).GetProperty("value")[0];

            Assert.Equal("Ann", ticket.GetProperty("Team").GetProperty("Schedule").GetProperty("Owner").GetProperty("Name").GetString());
            Assert.Equal(new[] { "Ann", "Bob" }, ticket.GetProperty("Team").GetProperty("Members").EnumerateArray().Select(member => member.GetProperty("Name").GetString()).Order());
            Assert.Equal("KG", ticket.GetProperty("Unit").GetProperty("Code").GetString());
        }

        [Fact]
        public async Task ODataQuery_ThenInclude_ExpandsTheItemsOfACollection()
        {
            var query = new ODataQuery<Ticket>().Include(ticket => ticket.Comments).ThenInclude(comment => comment.Author).OrderBy(ticket => ticket.Id);

            var tickets = (await server.GetAsync(Set("Tickets").GetODataUri(query))).GetProperty("value").EnumerateArray().ToList();

            Assert.Equal(HelpdeskSeed.Tickets.Length, tickets.Count);
            foreach (var ticket in tickets)
            {
                var expected = HelpdeskSeed.Tickets.Single(seed => seed.Id == ticket.GetProperty("Id").GetInt32()).Comments.OrderBy(comment => comment.Id).Select(comment => $"{comment.Id}:{comment.Author.Name}");
                Assert.Equal(expected, ticket.GetProperty("Comments").EnumerateArray().OrderBy(comment => comment.GetProperty("Id").GetInt32()).Select(comment => $"{comment.GetProperty("Id").GetInt32()}:{comment.GetProperty("Author").GetProperty("Name").GetString()}"));
            }
        }

        [Theory]
        [InlineData("Tickets", false)]
        [InlineData("MemoryTickets", false)]
        [InlineData("MemoryTickets", true)]
        public async Task ODataQuery_Include_FiltersOrdersAndPagesTheChildrenNotTheParents(string set, bool paged)
        {
            var minimum = 1;
            Expression<Func<Ticket, IEnumerable<Comment>>> include = paged
                ? ticket => ticket.Comments.Where(comment => comment.Score > minimum).OrderByDescending(comment => comment.CreatedAt).Skip(1).Take(1)
                : ticket => ticket.Comments.Where(comment => comment.Score > minimum).OrderByDescending(comment => comment.CreatedAt);
            var query = new ODataQuery<Ticket>().Include(include).ThenInclude(comment => comment.Author).OrderBy(ticket => ticket.Id);

            var tickets = (await server.GetAsync(Set(set).GetODataUri(query))).GetProperty("value").EnumerateArray().ToList();

            Assert.Equal(HelpdeskSeed.Tickets.Select(ticket => ticket.Id).Order(), tickets.Select(ticket => ticket.GetProperty("Id").GetInt32()));
            foreach (var ticket in tickets)
            {
                var comments = HelpdeskSeed.Tickets.Single(seed => seed.Id == ticket.GetProperty("Id").GetInt32()).Comments
                    .Where(comment => comment.Score > minimum).OrderByDescending(comment => comment.CreatedAt);
                var expected = (paged ? comments.Skip(1).Take(1) : comments).Select(comment => $"{comment.Id}:{comment.Author.Name}");
                Assert.Equal(expected, ticket.GetProperty("Comments").EnumerateArray().Select(comment => $"{comment.GetProperty("Id").GetInt32()}:{comment.GetProperty("Author").GetProperty("Name").GetString()}"));
            }
        }

        [Fact]
        public async Task ODataQuery_OrderBy_OrdersByNestedMembers()
        {
            var query = new ODataQuery<Ticket>().OrderBy(ticket => ticket.Team.Name).ThenByDescending(ticket => ticket.Unit.Code).ThenBy(ticket => ticket.Id);

            var actual = Ids(await server.GetAsync(Set("Tickets").GetODataUri(query)));

            Assert.Equal(HelpdeskSeed.Tickets.OrderBy(ticket => ticket.Team.Name, StringComparer.Ordinal).ThenByDescending(ticket => ticket.Unit?.Code, StringComparer.Ordinal).ThenBy(ticket => ticket.Id).Select(ticket => ticket.Id), actual);
        }

        [Fact]
        public async Task GetODataUri_CombinesAGridOrGroupWithTheTypedFilterAndPagesTheResult()
        {
            var query = new ODataQuery<Ticket>().Where(ticket => ticket.Status != TicketStatus.Closed).OrderBy(ticket => ticket.Id);
            var args = new LoadDataArgs { Filter = "Priority ge 2 or Urgent eq true", OrderBy = "Subject desc", Skip = 1, Top = 2 };
            var matching = HelpdeskSeed.Tickets.Where(ticket => (ticket.Priority >= 2 || ticket.Urgent) && ticket.Status != TicketStatus.Closed).ToList();

            var result = await server.GetAsync(Set("Tickets").GetODataUri(query, args));

            Assert.Equal(matching.OrderByDescending(ticket => ticket.Subject, StringComparer.Ordinal).Skip(1).Take(2).Select(ticket => ticket.Id), Ids(result));
            Assert.Equal(matching.Count, result.GetProperty("@odata.count").GetInt32());
        }

        [Fact]
        public async Task GetODataUri_KeepsTheTypedOrderWhileTheGridIsUnsorted()
        {
            var query = new ODataQuery<Ticket>().Where(ticket => ticket.Urgent).OrderByDescending(ticket => ticket.CreatedAt);

            var result = await server.GetAsync(Set("Tickets").GetODataUri(query, new LoadDataArgs { Skip = 0, Top = 10 }));

            Assert.Equal(HelpdeskSeed.Tickets.Where(ticket => ticket.Urgent).OrderByDescending(ticket => ticket.CreatedAt).Select(ticket => ticket.Id), Ids(result));
        }

        [Fact]
        public async Task ODataQuery_GroupBy_ReturnsTheLicensesTotalsUnaffectedByTheGridPaging()
        {
            var search = "radzen";
            var query = new ODataQuery<License>()
                .Where(license => license.Refunded != true)
                .Where(license => license.Email.Contains(search) || license.LicenseKey.Contains(search) || license.ProductName.Contains(search))
                .GroupBy(license => license.Currency)
                .Select(group => new LicenseTotal { Currency = group.Key, TotalPrice = group.Sum(license => license.Price) });
            var args = new LoadDataArgs { Filter = "Currency ne 'GBP'", OrderBy = "Email", Skip = 1, Top = 1 };

            var result = await server.GetAsync(Set("Licenses").GetODataUri(query, args));

            var expected = HelpdeskSeed.Licenses
                .Where(license => license.Currency != "GBP" && license.Refunded != true && (license.Email.Contains(search) || license.LicenseKey.Contains(search) || license.ProductName.Contains(search)))
                .GroupBy(license => license.Currency)
                .Select(group => $"{group.Key}:{group.Sum(license => license.Price):0.##}")
                .Order();
            var actual = result.GetProperty("value").EnumerateArray()
                .Select(total => $"{total.GetProperty("Currency").GetString()}:{total.GetProperty("TotalPrice").GetDecimal():0.##}")
                .Order();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public async Task ODataQuery_GroupBy_ReturnsCompositeGroupsWithEveryAggregate()
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
                })
                .Where(summary => summary.Count > 1)
                .OrderByDescending(summary => summary.Highest)
                .ThenBy(summary => summary.Currency)
                .WithCount();

            var result = await server.GetAsync(Set("Licenses").GetODataUri(query));

            var expected = HelpdeskSeed.Licenses
                .GroupBy(license => new { license.Currency, license.ProductName })
                .Select(group => new { group.Key.Currency, group.Key.ProductName, Count = group.Count(), Customers = group.Select(license => license.Email).Distinct().Count(), Lowest = group.Min(license => license.Price), Highest = group.Max(license => license.Price) })
                .Where(summary => summary.Count > 1)
                .OrderByDescending(summary => summary.Highest)
                .ThenBy(summary => summary.Currency, StringComparer.Ordinal)
                .Select(summary => $"{summary.Currency}/{summary.ProductName}:{summary.Count}:{summary.Customers}:{summary.Lowest:0.##}:{summary.Highest:0.##}");
            var actual = result.GetProperty("value").EnumerateArray()
                .Select(summary => $"{summary.GetProperty("Currency").GetString()}/{summary.GetProperty("ProductName").GetString()}:{summary.GetProperty("Count").GetInt32()}:{summary.GetProperty("Customers").GetInt32()}:{Decimal(summary.GetProperty("Lowest")):0.##}:{Decimal(summary.GetProperty("Highest")):0.##}");
            Assert.Equal(expected, actual);
            Assert.Equal(expected.Count(), result.GetProperty("@odata.count").GetInt32());
        }

        private static void AssertClose(double expected, double actual) => Assert.InRange(actual, expected - Math.Abs(expected) * 1e-7, expected + Math.Abs(expected) * 1e-7);

        private static double? Number(JsonElement item, string name) => item.TryGetProperty(name, out var value) && value.ValueKind != JsonValueKind.Null ? value.GetDouble() : null;

        [Fact]
        public async Task ODataQuery_GroupBy_ReturnsTheConstantKeyAggregatesTheSameLambdasReturnInMemory()
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

            var totals = (await server.GetAsync(Set("OrderLines").GetODataUri(query))).GetProperty("value").EnumerateArray().Single();

            var lines = HelpdeskSeed.OrderLines;
            AssertClose(lines.Sum(line => line.UnitPrice * line.Quantity * (1 - line.Discount)).Value, Number(totals, "Amount").Value);
            Assert.Equal(lines.Sum(line => (int?)line.Quantity), (int?)Number(totals, "TotalQuantity"));
            Assert.Equal(lines.Average(line => line.UnitPrice).Value, Number(totals, "AveragePrice").Value, 6);
            Assert.Equal(lines.Average(line => line.Discount).Value, Number(totals, "AverageDiscount").Value, 6);
            Assert.Equal(lines.Length, (int)Number(totals, "Lines"));
        }

        [Fact]
        public async Task ODataQuery_GroupBy_FoldsTheWhereAndTheGridFilterIntoAConstantKeyAggregate()
        {
            var query = new ODataQuery<OrderLine>()
                .Where(line => line.Category != "Seafood")
                .GroupBy(line => 1)
                .Select(group => new LineTotals { Amount = group.Sum(line => line.UnitPrice * line.Quantity), Lines = group.Count() });

            var totals = (await server.GetAsync(Set("OrderLines").GetODataUri(query, new LoadDataArgs { Filter = "Quantity gt 5", OrderBy = "UnitPrice", Skip = 1, Top = 1 }))).GetProperty("value").EnumerateArray().Single();

            var lines = HelpdeskSeed.OrderLines.Where(line => line.Quantity > 5 && line.Category != "Seafood").ToList();
            Assert.Equal(lines.Sum(line => line.UnitPrice * line.Quantity).Value, Number(totals, "Amount").Value, 6);
            Assert.Equal(lines.Count, (int)Number(totals, "Lines"));
        }

        [Fact]
        public async Task ODataQuery_GroupBy_CastsAggregatedValuesTheWayTheSameLambdasDoInMemory()
        {
            var query = new ODataQuery<OrderLine>()
                .GroupBy(line => line.Category)
                .Select(group => new LineTotals
                {
                    Category = group.Key,
                    TotalQuantity = group.Sum(line => line.Quantity),
                    LongQuantity = group.Sum(line => (long)line.Quantity),
                    HighestDiscount = group.Max(line => (double)line.Discount),
                    Amount = group.Sum(line => line.UnitPrice * line.Quantity),
                })
                .OrderBy(totals => totals.Category);

            var actual = (await server.GetAsync(Set("OrderLines").GetODataUri(query))).GetProperty("value").EnumerateArray()
                .Select(totals => $"{totals.GetProperty("Category").GetString()}:{Number(totals, "TotalQuantity")}:{Number(totals, "LongQuantity")}:{Number(totals, "HighestDiscount"):0.######}:{Number(totals, "Amount"):0.######}");

            var expected = HelpdeskSeed.OrderLines
                .GroupBy(line => line.Category)
                .OrderBy(group => group.Key, StringComparer.Ordinal)
                .Select(group => $"{group.Key}:{group.Sum(line => line.Quantity)}:{group.Sum(line => (long?)line.Quantity)}:{group.Max(line => (double?)line.Discount):0.######}:{group.Sum(line => line.UnitPrice * line.Quantity):0.######}");
            Assert.Equal(expected, actual);
        }

        private static decimal? Decimal(JsonElement value) => value.ValueKind == JsonValueKind.Null ? null : value.GetDecimal();

        [Fact]
        public async Task ODataQuery_Where_ReturnsTheLogsOfOneDay()
        {
            var date = new DateTime(2026, 9, 26, 15, 45, 0, DateTimeKind.Utc);

            var parts = Ids(await server.GetAsync(Set("Logs").GetODataUri(new ODataQuery<Log>().Where(log => log.Date.Day == date.Day && log.Date.Month == date.Month && log.Date.Year == date.Year).OrderBy(log => log.Id))));
            var day = Ids(await server.GetAsync(Set("Logs").GetODataUri(new ODataQuery<Log>().Where(log => log.Date.Date == date.Date).OrderBy(log => log.Id))));

            Assert.Equal([2, 3, 4], parts);
            Assert.Equal([2, 3, 4], day);
        }

        public static TheoryData<Expression<Func<AnalyticsSession, bool>>> SessionConditions()
        {
            var application = "Radzen Blazor for Visual Studio";

            return new()
            {
                session => session.Events.Any(e => e.Type == "trial_check" && (int)e.Data["remaining"] == 15),
                session => session.Events.Any(e => e.Type == "trial_check" && (int)e.Data["remaining"] == 15) && !session.Events.Any(e => e.Type == "application_open"),
                session => session.Events.Any(e => e.Type == "trial_check" && (int)e.Data["remaining"] == 15) && !session.Events.Any(e => e.Type == "application_open") && session.Events.Any(e => e.Type != "toolbox_drop" && (string)e.Data["application"] == application),
                session => !session.Events.Any(),
            };
        }

        [Theory]
        [MemberData(nameof(SessionConditions))]
        public async Task ODataQuery_Where_ReadsOpenTypePropertiesLikeTheSameLambdaInMemory(Expression<Func<AnalyticsSession, bool>> where)
        {
            var result = await server.GetAsync(Set("AnalyticsSessions").GetODataUri(new ODataQuery<AnalyticsSession>().Where(where).OrderBy(session => session.Id)));

            Assert.Equal(HelpdeskSeed.Sessions.Where(where.Compile()).Select(session => session.Id), result.GetProperty("value").EnumerateArray().Select(session => session.GetProperty("Id").GetString()));
        }
    }
}
