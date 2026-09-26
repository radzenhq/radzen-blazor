using Radzen.Blazor.Tests.Helpdesk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class ODataQueryTranslationTests
    {
        public static TheoryData<Expression<Func<Ticket, bool>>, string> FactoryConditions()
        {
            var name = "O'Brien \"Jr\" & 100%";
            var search = "PRINTER";
            var status = TicketStatus.Closed;
            var since = new DateTime(2026, 1, 31, 9, 30, 0);
            var instant = new DateTimeOffset(2026, 1, 31, 9, 30, 0, TimeSpan.FromHours(2));
            var due = new DateOnly(2026, 2, 1);
            var reference = new Guid("5d0c8c1e-8f0a-4f6e-9a1d-2b7f3c4d5e6f");
            int? teamId = 2;
            TicketStatus? anyStatus = null;
            var team = new Team { Id = 1, Name = "Support" };

            return new()
            {
                { ticket => ticket.Subject == "Printer jam", "Subject eq 'Printer jam'" },
                { ticket => ticket.Subject == name, "Subject eq 'O''Brien \"Jr\" & 100%'" },
                { ticket => ticket.Subject != null, "Subject ne null" },
                { ticket => ticket.Priority > 2, "Priority gt 2" },
                { ticket => ticket.Priority <= 3 && ticket.Priority >= 1, "Priority le 3 and Priority ge 1" },
                { ticket => ticket.Price < 10.5m, "Price lt 10.5" },
                { ticket => ticket.Hours >= 1.25, "Hours ge 1.25" },
                { ticket => ticket.Urgent, "Urgent" },
                { ticket => !ticket.Urgent, "not Urgent" },
                { ticket => ticket.Urgent == false, "Urgent eq false" },
                { ticket => ticket.Reference == reference, "Reference eq 5d0c8c1e-8f0a-4f6e-9a1d-2b7f3c4d5e6f" },
                { ticket => ticket.Status == TicketStatus.Open, "Status eq 'Open'" },
                { ticket => ticket.Status != status, "Status ne 'Closed'" },
                { ticket => ticket.Previous == TicketStatus.Closed, "Previous eq 'Closed'" },
                { ticket => ticket.Previous == null, "Previous eq null" },
                { ticket => ticket.OpenedAt >= since, "OpenedAt ge 2026-01-31T09:30:00Z" },
                { ticket => ticket.OpenedAt < new DateTime(2026, 1, 31, 9, 30, 15, 500), "OpenedAt lt 2026-01-31T09:30:15.5Z" },
                { ticket => ticket.CreatedAt > instant, "CreatedAt gt 2026-01-31T09:30:00+02:00" },
                { ticket => ticket.Due <= due, "Due le 2026-02-01" },
                { ticket => ticket.ClosedAt == null, "ClosedAt eq null" },
                { ticket => ticket.ClosedAt.HasValue && ticket.ClosedAt.Value < since, "ClosedAt ne null and ClosedAt lt 2026-01-31T09:30:00Z" },
                { ticket => ticket.ClosedAt > since, "ClosedAt gt 2026-01-31T09:30:00Z" },
                { ticket => ticket.TeamId == teamId, "TeamId eq 2" },
                { ticket => ticket.Team.Name == team.Name, "Team/Name eq 'Support'" },
                { ticket => ticket.Team != null, "Team ne null" },
                { ticket => ticket.Subject.Contains("jam"), "contains(Subject,'jam')" },
                { ticket => ticket.Subject.ToLower().Contains(search.ToLower()), "contains(tolower(Subject),'printer')" },
                { ticket => ticket.Subject.ToUpper().StartsWith("SCREEN"), "startswith(toupper(Subject),'SCREEN')" },
                { ticket => ticket.Subject.EndsWith("jam"), "endswith(Subject,'jam')" },
                { ticket => ticket.Urgent || ticket.Priority == 1 && !(ticket.Status == TicketStatus.Closed), "Urgent or (Priority eq 1 and not (Status eq 'Closed'))" },
                { ticket => !anyStatus.HasValue || ticket.Status == anyStatus.Value, "true" },
                { ticket => !teamId.HasValue || ticket.TeamId == teamId.Value, "TeamId eq 2" },
                { ticket => string.IsNullOrEmpty(search) || ticket.Subject.Contains(search), "contains(Subject,'PRINTER')" },
                { ticket => anyStatus != null && ticket.Status == anyStatus.Value, "false" },
                { ticket => (anyStatus == null || ticket.Status == anyStatus.Value) && ticket.Urgent, "Urgent" },
                { ticket => ticket.Subject.Length > 3, "length(Subject) gt 3" },
                { ticket => ticket.Priority * 2 > 3, "Priority mul 2 gt 3" },
            };
        }

        [Theory]
        [MemberData(nameof(FactoryConditions))]
        public void ODataQuery_Where_TranslatesTheFactoryConditions(Expression<Func<Ticket, bool>> where, string filter)
        {
            Assert.Equal(filter, ODataTranslator.Filter(where));
            ODataParser.AssertParses("Tickets", $"$filter={filter}");
        }

        public static TheoryData<Expression<Func<Ticket, bool>>, string> Literals()
        {
            var utc = new DateTime(2026, 1, 31, 9, 30, 0, DateTimeKind.Utc);
            var offset = new DateTimeOffset(2026, 1, 31, 9, 30, 0, 123, TimeSpan.FromHours(-5)).AddTicks(4567);
            var zero = new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero);
            var channels = Channel.Email | Channel.Chat;
            var status = (TicketStatus)1;
            var code = 2;

            return new()
            {
                { ticket => ticket.Views == 5000000000L, "Views eq 5000000000" },
                { ticket => ticket.Rating > 1.5f, "Rating gt 1.5" },
                { ticket => ticket.Hours == 0.1, "Hours eq 0.1" },
                { ticket => ticket.Hours > 1e20, "Hours gt 1E+20" },
                { ticket => ticket.Hours == double.NaN, "Hours eq NaN" },
                { ticket => ticket.Hours < double.PositiveInfinity, "Hours lt INF" },
                { ticket => ticket.Hours > double.NegativeInfinity, "Hours gt -INF" },
                { ticket => ticket.Price == 12.50m, "Price eq 12.50" },
                { ticket => ticket.Price < 12345678901234567890.123456789m, "Price lt 12345678901234567890.123456789" },
                { ticket => ticket.Priority > -3, "Priority gt -3" },
                { ticket => ticket.OpenedAt == utc, "OpenedAt eq 2026-01-31T09:30:00Z" },
                { ticket => ticket.CreatedAt == offset, "CreatedAt eq 2026-01-31T09:30:00.1234567-05:00" },
                { ticket => ticket.CreatedAt >= zero, "CreatedAt ge 2026-02-01T00:00:00Z" },
                { ticket => ticket.Due == new DateOnly(999, 1, 1), "Due eq 0999-01-01" },
                { ticket => ticket.Slot == new TimeOnly(9, 30), "Slot eq 09:30:00.0000000" },
                { ticket => ticket.Effort > TimeSpan.FromMinutes(90), "Effort gt duration'PT1H30M'" },
                { ticket => ticket.Effort == new TimeSpan(1, 2, 3, 4, 500), "Effort eq duration'P1DT2H3M4.5S'" },
                { ticket => ticket.Effort == TimeSpan.Zero, "Effort eq duration'PT0S'" },
                { ticket => ticket.Effort != TimeSpan.FromDays(2), "Effort ne duration'P2D'" },
                { ticket => ticket.Effort > -TimeSpan.FromMinutes(1), "Effort gt duration'-PT1M'" },
                { ticket => ticket.Subject == "a\"b", "Subject eq 'a\"b'" },
                { ticket => ticket.Subject == "it's", "Subject eq 'it''s'" },
                { ticket => ticket.Subject == "\u2603", "Subject eq '\u2603'" },
                { ticket => ticket.Status == status, "Status eq 'Closed'" },
                { ticket => (int)ticket.Status == 2, "Status eq 'Pending'" },
                { ticket => ticket.Priority == (int)TicketStatus.Pending, "Priority eq 2" },
                { ticket => ticket.Status == (TicketStatus)code, "Status eq 'Pending'" },
                { ticket => ticket.Previous != TicketStatus.Pending, "Previous ne 'Pending'" },
                { ticket => ticket.Channels == Channel.Email, "Channels eq 'Email'" },
                { ticket => ticket.Channels == channels, "Channels eq Radzen.Blazor.Tests.Helpdesk.Channel'Email,Chat'" },
                { ticket => ticket.Channels.HasFlag(Channel.Phone), "Channels has 'Phone'" },
                { ticket => ticket.Channels.HasFlag(channels), "Channels has Radzen.Blazor.Tests.Helpdesk.Channel'Email,Chat'" },
            };
        }

        [Theory]
        [MemberData(nameof(Literals))]
        public void ODataQuery_Where_WritesOneLiteralPerOData(Expression<Func<Ticket, bool>> where, string filter)
        {
            Assert.Equal(filter, ODataTranslator.Filter(where));
            ODataParser.AssertParses("Tickets", $"$filter={filter}");
        }

        [Fact]
        public void ODataQuery_Where_WritesALocalDateTimeInUtc()
        {
            var local = new DateTime(2026, 1, 31, 9, 30, 0, DateTimeKind.Local);

            Assert.Equal($"OpenedAt ge {local.ToUniversalTime():yyyy'-'MM'-'dd'T'HH':'mm':'ss}Z", ODataTranslator.Filter((Expression<Func<Ticket, bool>>)(ticket => ticket.OpenedAt >= local)));
        }

        public static TheoryData<Expression<Func<Ticket, bool>>, string> Functions()
        {
            var ids = new List<int> { 1, 3 };
            var none = new List<int>();
            var statuses = new[] { TicketStatus.Open, TicketStatus.Pending };
            var subjects = new[] { "O'Brien", "Printer jam" };
            var day = new DateTime(2026, 1, 31);
            var set = new HashSet<long> { 7 };

            return new()
            {
                { ticket => ticket.Subject.IndexOf("jam") == 8, "indexof(Subject,'jam') eq 8" },
                { ticket => ticket.Subject.Substring(1) == "x", "substring(Subject,1) eq 'x'" },
                { ticket => ticket.Subject.Substring(0, 6) == "Screen", "substring(Subject,0,6) eq 'Screen'" },
                { ticket => ticket.Subject.Trim() == "x", "trim(Subject) eq 'x'" },
                { ticket => ticket.Subject.ToUpperInvariant() == "X", "toupper(Subject) eq 'X'" },
                { ticket => ticket.Subject.ToLowerInvariant() == "x", "tolower(Subject) eq 'x'" },
                { ticket => ticket.Subject.Contains('%'), "contains(Subject,'%')" },
                { ticket => ticket.Subject.Contains("JAM", StringComparison.OrdinalIgnoreCase), "contains(tolower(Subject),tolower('JAM'))" },
                { ticket => ticket.Subject.StartsWith("P", StringComparison.Ordinal), "startswith(Subject,'P')" },
                { ticket => ticket.Subject + "!" == "x!", "concat(Subject,'!') eq 'x!'" },
                { ticket => string.Concat(ticket.Subject, "-", ticket.Team.Name) == "a-b", "concat(concat(Subject,'-'),Team/Name) eq 'a-b'" },
                { ticket => string.IsNullOrEmpty(ticket.Subject), "Subject eq null or Subject eq ''" },
                { ticket => !string.IsNullOrEmpty(ticket.Subject) && ticket.Urgent, "not (Subject eq null or Subject eq '') and Urgent" },
                { ticket => ticket.Subject.Equals("x"), "Subject eq 'x'" },
                { ticket => ticket.Discount > 1, "Discount gt 1" },
                { ticket => ticket.Discount.Value > 1, "Discount gt 1" },
                { ticket => ticket.Discount.HasValue, "Discount ne null" },
                { ticket => !ticket.Discount.HasValue, "Discount eq null" },
                { ticket => ticket.Escalated == true, "Escalated eq true" },
                { ticket => ticket.Escalated != true, "Escalated ne true" },
                { ticket => !(ticket.Escalated == true), "not (Escalated eq true)" },
                { ticket => ticket.Priority + 1 == 3, "Priority add 1 eq 3" },
                { ticket => (ticket.Priority + 1) * 2 > 6, "(Priority add 1) mul 2 gt 6" },
                { ticket => ticket.Priority - (ticket.Priority - 1) == 1, "Priority sub (Priority sub 1) eq 1" },
                { ticket => ticket.Priority - ticket.Priority - 1 == -1, "Priority sub Priority sub 1 eq -1" },
                { ticket => ticket.Priority % 2 == 0, "Priority mod 2 eq 0" },
                { ticket => ticket.Priority / 2 == 1, "Priority div 2 eq 1" },
                { ticket => -ticket.Priority < -3, "-Priority lt -3" },
                { ticket => ticket.Price * 2 > 10, "Price mul 2 gt 10" },
                { ticket => ticket.Hours * ticket.Priority >= 4, "Hours mul Priority ge 4" },
                { ticket => ticket.Price > ticket.Priority, "Price gt Priority" },
                { ticket => ticket.Discount * ticket.Priority > 10, "Discount mul Priority gt 10" },
                { ticket => (ticket.Priority > 1) == ticket.Urgent, "(Priority gt 1) eq Urgent" },
                { ticket => ticket.OpenedAt.Year == 2026 && ticket.OpenedAt.Month == 1 && ticket.OpenedAt.Day == 31, "year(OpenedAt) eq 2026 and month(OpenedAt) eq 1 and day(OpenedAt) eq 31" },
                { ticket => ticket.OpenedAt.Hour == 9 && ticket.OpenedAt.Minute == 30 && ticket.OpenedAt.Second == 0, "hour(OpenedAt) eq 9 and minute(OpenedAt) eq 30 and second(OpenedAt) eq 0" },
                { ticket => ticket.OpenedAt.Date == day, "date(OpenedAt) eq 2026-01-31" },
                { ticket => day == ticket.OpenedAt.Date, "2026-01-31 eq date(OpenedAt)" },
                { ticket => ticket.CreatedAt.Hour == 6, "hour(CreatedAt) eq 6" },
                { ticket => ticket.ClosedAt.Value.Year == 2026, "year(ClosedAt) eq 2026" },
                { ticket => ticket.Due.Month == 2, "month(Due) eq 2" },
                { ticket => ticket.Slot.Value.Minute == 30, "minute(Slot) eq 30" },
                { ticket => Math.Floor(ticket.Hours) == 1, "floor(Hours) eq 1" },
                { ticket => Math.Ceiling(ticket.Price) == 11, "ceiling(Price) eq 11" },
                { ticket => Math.Round(ticket.Hours, MidpointRounding.AwayFromZero) == 1, "round(Hours) eq 1" },
                { ticket => ids.Contains(ticket.Id), "Id in (1,3)" },
                { ticket => new[] { 1, 3 }.Contains(ticket.Id), "Id in (1,3)" },
                { ticket => none.Contains(ticket.Id), "false" },
                { ticket => none.Contains(ticket.Id) || ticket.Urgent, "Urgent" },
                { ticket => statuses.Contains(ticket.Status), "Status in ('Open','Pending')" },
                { ticket => subjects.Contains(ticket.Subject), "Subject in ('O''Brien','Printer jam')" },
                { ticket => set.Contains(ticket.Views), "Views in (7)" },
                { ticket => ticket.Previous != null && statuses.Contains(ticket.Previous.Value), "Previous ne null and Previous in ('Open','Pending')" },
                { ticket => !statuses.Contains(ticket.Status), "not (Status in ('Open','Pending'))" },
                { ticket => ticket.Comments.Any(), "Comments/any()" },
                { ticket => !ticket.Comments.Any(), "not Comments/any()" },
                { ticket => ticket.Comments.Any(comment => comment.Score > 3), "Comments/any(comment:comment/Score gt 3)" },
                { ticket => ticket.Comments.All(comment => comment.Score > 1), "Comments/all(comment:comment/Score gt 1)" },
                { ticket => ticket.Comments.Count > 1, "Comments/$count gt 1" },
                { ticket => ticket.Comments.Count() > 1, "Comments/$count gt 1" },
                { ticket => ticket.Team.Members.Any(member => member.Name == "Bob"), "Team/Members/any(member:member/Name eq 'Bob')" },
                { ticket => ticket.Comments.Any(comment => comment.Score > ticket.Priority), "Comments/any(comment:comment/Score gt Priority)" },
                { ticket => ticket.Comments.Any(comment => ticket.Team.Members.Any(member => member.Id == comment.AuthorId)), "Comments/any(comment:Team/Members/any(member:member/Id eq comment/AuthorId))" },
                { ticket => ticket.Comments.Any(comment => comment.Author.Name == "Ann" && comment.Text.Contains("O'")), "Comments/any(comment:comment/Author/Name eq 'Ann' and contains(comment/Text,'O'''))" },
                { ticket => ticket.Comments.Any(comment => true), "Comments/any()" },
                { ticket => ticket.Comments.Any(comment => false), "false" },
                { ticket => ticket.Comments.All(comment => true), "true" },
                { ticket => ticket.Comments.All(comment => false), "not Comments/any()" },
                { ticket => ticket.Team.Schedule != null && ticket.Team.Schedule.Owner.Name == "Ann", "Team/Schedule ne null and Team/Schedule/Owner/Name eq 'Ann'" },
                { ticket => ticket.Unit == null, "Unit eq null" },
            };
        }

        [Theory]
        [MemberData(nameof(Functions))]
        public void ODataQuery_Where_TranslatesFunctionsCollectionsAndOperators(Expression<Func<Ticket, bool>> where, string filter)
        {
            Assert.Equal(filter, ODataTranslator.Filter(where));
            ODataParser.AssertParses("Tickets", $"$filter={filter}");
        }

        [Fact]
        public void ODataQuery_Where_RenamesAShadowedLambdaVariable()
        {
            var ticket = Expression.Parameter(typeof(Ticket), "ticket");
            var comment = Expression.Parameter(typeof(Comment), "x");
            var member = Expression.Parameter(typeof(Member), "x");
            var members = Expression.Property(Expression.Property(ticket, nameof(Ticket.Team)), nameof(Team.Members));
            var inner = Expression.Call(typeof(Enumerable), nameof(Enumerable.Any), [typeof(Member)], members,
                Expression.Lambda<Func<Member, bool>>(Expression.Equal(Expression.Property(member, nameof(Member.Id)), Expression.Property(comment, nameof(Comment.AuthorId))), member));
            var outer = Expression.Call(typeof(Enumerable), nameof(Enumerable.Any), [typeof(Comment)], Expression.Property(ticket, nameof(Ticket.Comments)),
                Expression.Lambda<Func<Comment, bool>>(inner, comment));

            var filter = ODataTranslator.Filter(Expression.Lambda<Func<Ticket, bool>>(outer, ticket));

            Assert.Equal("Comments/any(x:Team/Members/any(x1:x1/Id eq x/AuthorId))", filter);
            ODataParser.AssertParses("Tickets", $"$filter={filter}");
        }

        [Theory]
        [InlineData("$it")]
        [InlineData("")]
        [InlineData("<>h__TransparentIdentifier0")]
        public void ODataQuery_Where_NamesALambdaVariableThatIsNoODataIdentifierX(string name)
        {
            var ticket = Expression.Parameter(typeof(Ticket), "ticket");
            var comment = Expression.Parameter(typeof(Comment), name);
            var any = Expression.Call(typeof(Enumerable), nameof(Enumerable.Any), [typeof(Comment)], Expression.Property(ticket, nameof(Ticket.Comments)),
                Expression.Lambda<Func<Comment, bool>>(Expression.GreaterThan(Expression.Property(comment, nameof(Comment.Score)), Expression.Constant(1)), comment));

            Assert.Equal("Comments/any(x:x/Score gt 1)", ODataTranslator.Filter(Expression.Lambda<Func<Ticket, bool>>(any, ticket)));
        }

        [Fact]
        public void ODataQuery_Where_ReadsAnOpenTypePropertyFromItsDictionaryContainer()
        {
            var application = "Radzen Blazor for Visual Studio";
            Expression<Func<AnalyticsSession, bool>> abandoned = session =>
                session.Events.Any(e => e.Type == "trial_check" && (int)e.Data["remaining"] == 15)
                && !session.Events.Any(e => e.Type == "application_open")
                && session.Events.Any(e => (string)e.Data["application"] == application);

            var filter = ODataTranslator.Filter(abandoned);

            Assert.Equal("Events/any(e:e/Type eq 'trial_check' and e/remaining eq 15) and not Events/any(e:e/Type eq 'application_open') and Events/any(e:e/application eq 'Radzen Blazor for Visual Studio')", filter);
            ODataParser.AssertParses("AnalyticsSessions", $"$filter={filter}");
        }

        public static TheoryData<Expression<Func<Ticket, bool>>, string> Unsupported()
        {
            var since = new DateTime(2026, 1, 31);
            var team = new Team { Id = 1 };
            var labels = new Dictionary<string, string>();

            return new()
            {
                { ticket => (ticket.Discount ?? 0) > 1, "OData has no ?? operator" },
                { ticket => ticket.Subject.CompareTo("a") > 0, "use == or StartsWith" },
                { ticket => Math.Round(ticket.Hours) == 1, "Math.Round(value, MidpointRounding.AwayFromZero)" },
                { ticket => Math.Round(ticket.Hours, 1, MidpointRounding.AwayFromZero) == 1, "Math.Round(value, MidpointRounding.AwayFromZero)" },
                { ticket => ticket.Subject.Contains("a", StringComparison.CurrentCultureIgnoreCase), "StringComparison.OrdinalIgnoreCase" },
                { ticket => (int)ticket.Hours == 1, "Math.Floor" },
                { ticket => (int)ticket.Price == 1, "no conversion from Decimal to Int32" },
                { ticket => (ticket.Urgent ? ticket.Priority : 0) > 1, "no conditional operator" },
                { ticket => ticket.Comments.Count(comment => comment.Score > 1) > 0, "use Any(predicate)" },
                { ticket => ticket.Comments.Where(comment => comment.Score > 1).Any(), "Enumerable.Where has no OData equivalent" },
                { ticket => ticket.OpenedAt.AddDays(1) > since, "DateTime.AddDays has no OData equivalent" },
                { ticket => ticket.OpenedAt.Date == new DateTime(2026, 1, 31, 10, 0, 0), "compare it with the date alone" },
                { ticket => ticket.Team == team, "compare a property such as the key" },
                { ticket => ticket.Comments.Contains(null), "use ticket.Comments.Any(item => item == value)" },
                { ticket => labels[ticket.Subject] == "x", "Dictionary`2.get_Item has no OData equivalent" },
                { ticket => ticket.Grade == 'A', "no conversion from Char to Int32" },
            };
        }

        [Theory]
        [MemberData(nameof(Unsupported))]
        public void ODataQuery_Where_ThrowsNamingTheExpressionAndTheAlternative(Expression<Func<Ticket, bool>> where, string alternative)
        {
            var error = Assert.Throws<NotSupportedException>(() => new ODataQuery<Ticket>().Where(where));

            Assert.StartsWith("ODataQuery cannot ", error.Message);
            Assert.Contains(alternative, error.Message);
        }

        [Fact]
        public void ODataQuery_Where_ThrowsForAnOpenTypeKeyThatIsNoIdentifier()
        {
            var error = Assert.Throws<NotSupportedException>(() => new ODataQuery<AnalyticsEvent>().Where(e => (int)e.Data["a b"] == 1));

            Assert.Contains("e.Data.get_Item(\"a b\")", error.Message);
            Assert.Contains("constant OData identifier", error.Message);
        }
    }
}
