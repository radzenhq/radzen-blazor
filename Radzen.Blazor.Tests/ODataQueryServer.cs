using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using Radzen.Blazor.Tests.Helpdesk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public static class HelpdeskSeed
    {
        public static readonly Member[] Members =
        [
            new() { Id = 1, Name = "Ann", TeamId = 1 },
            new() { Id = 2, Name = "Bob", TeamId = 1 },
            new() { Id = 3, Name = "Cid", TeamId = 2 },
            new() { Id = 4, Name = "Dee", TeamId = 3 },
        ];

        public static readonly Schedule[] Schedules =
        [
            new() { Id = 1, Name = "Weekdays", OwnerId = 1, Owner = Members[0] },
            new() { Id = 2, Name = "Weekends" },
        ];

        public static readonly Team[] Teams =
        [
            new() { Id = 1, Name = "Support", ScheduleId = 1, Schedule = Schedules[0], Members = [Members[0], Members[1]] },
            new() { Id = 2, Name = "Sales", ScheduleId = 2, Schedule = Schedules[1], Members = [Members[2]] },
            new() { Id = 3, Name = "O'Brien & Co", Members = [Members[3]] },
        ];

        public static readonly Unit[] Units =
        [
            new() { Id = 1, Code = "KG" },
            new() { Id = 2, Code = "LB" },
            new() { Id = 3, Code = "EA" },
        ];

        public static readonly Ticket[] Tickets = CreateTickets();

        public static readonly License[] Licenses =
        [
            new() { Id = 1, Email = "ann@radzen.com", LicenseKey = "K-1", ProductName = "Studio", Price = 100m, Currency = "USD", Refunded = null, CreatedAt = new DateTime(2026, 1, 1) },
            new() { Id = 2, Email = "bob@example.com", LicenseKey = "K-2", ProductName = "Studio", Price = 250.5m, Currency = "USD", Refunded = false, CreatedAt = new DateTime(2026, 1, 2) },
            new() { Id = 3, Email = "cid@radzen.com", LicenseKey = "K-3", ProductName = "Blazor", Price = 80m, Currency = "EUR", Refunded = true, CreatedAt = new DateTime(2026, 1, 3) },
            new() { Id = 4, Email = "dee@radzen.com", LicenseKey = "K-4", ProductName = "Blazor", Price = 40m, Currency = "EUR", Refunded = null, CreatedAt = new DateTime(2026, 1, 4) },
            new() { Id = 5, Email = "eve@example.com", LicenseKey = "K-5", ProductName = "Studio", Price = null, Currency = "EUR", Refunded = false, CreatedAt = new DateTime(2026, 1, 5) },
            new() { Id = 6, Email = "fay@radzen.com", LicenseKey = "K-6", ProductName = "Blazor", Price = 12.25m, Currency = "GBP", Refunded = false, CreatedAt = new DateTime(2026, 1, 6) },
            new() { Id = 7, Email = "gus@radzen.com", LicenseKey = "K-7", ProductName = "Studio", Price = 60m, Currency = "USD", Refunded = null, CreatedAt = new DateTime(2026, 1, 7) },
            new() { Id = 8, Email = "hal@radzen.com", LicenseKey = "K-8", ProductName = "Studio", Price = 60m, Currency = "USD", Refunded = true, CreatedAt = new DateTime(2026, 1, 8) },
        ];

        public static readonly Log[] Logs =
        [
            new() { Id = 1, Date = new DateTime(2026, 9, 25, 23, 59, 59), Product = "Studio", LicenseId = 1 },
            new() { Id = 2, Date = new DateTime(2026, 9, 26, 0, 0, 0), Product = "Studio", LicenseId = 2 },
            new() { Id = 3, Date = new DateTime(2026, 9, 26, 13, 30, 0), Product = "Blazor", LicenseId = 3 },
            new() { Id = 4, Date = new DateTime(2026, 9, 26, 23, 59, 59), Product = "Blazor", LicenseId = 4 },
            new() { Id = 5, Date = new DateTime(2026, 9, 27, 0, 0, 0), Product = "Studio", LicenseId = 1 },
            new() { Id = 6, Date = new DateTime(2025, 9, 26, 12, 0, 0), Product = "Studio", LicenseId = 2 },
        ];

        public static readonly AnalyticsSession[] Sessions = CreateSessions();

        public static readonly OrderLine[] OrderLines =
        [
            new() { Id = 1, Category = "Beverages", UnitPrice = 18, Quantity = 12, Discount = 0f },
            new() { Id = 2, Category = "Beverages", UnitPrice = 19.5, Quantity = 10, Discount = 0.15f },
            new() { Id = 3, Category = "Condiments", UnitPrice = 10, Quantity = 30000, Discount = 0.05f },
            new() { Id = 4, Category = "Condiments", UnitPrice = 22.35, Quantity = 30000, Discount = 0.25f },
            new() { Id = 5, Category = "Condiments", UnitPrice = null, Quantity = 5, Discount = 0.1f },
            new() { Id = 6, Category = "Seafood", UnitPrice = 31.23, Quantity = null, Discount = null },
            new() { Id = 7, Category = "Seafood", UnitPrice = 6, Quantity = 40, Discount = 0.2f },
        ];

        private static Ticket[] CreateTickets()
        {
            Ticket[] tickets =
            [
                new() { Id = 1, Subject = "O'Brien \"Jr\" & 100%", Status = TicketStatus.Open, Previous = null, Price = 12.5m, Discount = null, Priority = 1, Hours = 0.5, Urgent = true, Escalated = null, Reference = new Guid("5d0c8c1e-8f0a-4f6e-9a1d-2b7f3c4d5e6f"), OpenedAt = new DateTime(2026, 2, 1, 10, 0, 0), CreatedAt = new DateTimeOffset(2026, 1, 31, 6, 0, 0, TimeSpan.Zero), Due = new DateOnly(2026, 1, 15), ClosedAt = null, Slot = new TimeOnly(9, 0), Effort = TimeSpan.FromHours(1), Views = 5000000000L, Rating = 4.5f, TeamId = 1, UnitId = 1, Channels = Channel.Email | Channel.Phone, Grade = 'A' },
                new() { Id = 2, Subject = "Printer jam", Status = TicketStatus.Closed, Previous = TicketStatus.Closed, Price = 3m, Discount = 1.5m, Priority = 3, Hours = 2, Urgent = false, Escalated = true, Reference = new Guid("11111111-1111-1111-1111-111111111111"), OpenedAt = new DateTime(2026, 1, 1, 8, 0, 0), CreatedAt = new DateTimeOffset(2026, 1, 1, 8, 0, 0, TimeSpan.Zero), Due = new DateOnly(2026, 3, 1), ClosedAt = new DateTime(2026, 2, 2, 8, 0, 0), Slot = null, Effort = TimeSpan.FromMinutes(30), Views = 10, Rating = 2f, TeamId = 2, UnitId = 2, Channels = Channel.Chat, Grade = 'B' },
                new() { Id = 3, Subject = "Screen flicker", Status = TicketStatus.Open, Previous = TicketStatus.Open, Price = 10.5m, Discount = 0m, Priority = 2, Hours = 1.25, Urgent = false, Escalated = false, Reference = new Guid("22222222-2222-2222-2222-222222222222"), OpenedAt = new DateTime(2026, 1, 31, 9, 30, 0), CreatedAt = new DateTimeOffset(2026, 1, 31, 8, 0, 0, TimeSpan.Zero), Due = new DateOnly(2026, 2, 1), ClosedAt = new DateTime(2026, 1, 20, 12, 0, 0), Slot = new TimeOnly(13, 30), Effort = new TimeSpan(2, 30, 0), Views = 0, Rating = 3.25f, TeamId = 1, UnitId = 1, Channels = Channel.None, Grade = 'A' },
                new() { Id = 4, Subject = "a+b=c #1 ?x", Status = TicketStatus.Pending, Previous = null, Price = 7.25m, Discount = null, Priority = 5, Hours = 3.75, Urgent = true, Escalated = null, Reference = new Guid("33333333-3333-3333-3333-333333333333"), OpenedAt = new DateTime(2026, 1, 31, 23, 59, 59), CreatedAt = new DateTimeOffset(2026, 1, 31, 23, 30, 0, TimeSpan.FromHours(-5)), Due = new DateOnly(2026, 1, 31), ClosedAt = null, Slot = new TimeOnly(23, 59, 59), Effort = TimeSpan.FromMinutes(45), Views = 7, Rating = 1.5f, TeamId = 3, UnitId = null, Channels = Channel.Phone, Grade = 'C' },
                new() { Id = 5, Subject = "\u00dcn\u00efc\u00f8d\u00e9 \u2603 \u65e5\u672c", Status = TicketStatus.Closed, Previous = TicketStatus.Pending, Price = 100m, Discount = 10m, Priority = 4, Hours = 10, Urgent = false, Escalated = true, Reference = new Guid("44444444-4444-4444-4444-444444444444"), OpenedAt = new DateTime(2025, 12, 31, 23, 0, 0), CreatedAt = new DateTimeOffset(2026, 1, 1, 0, 30, 0, TimeSpan.FromHours(1)), Due = new DateOnly(2025, 12, 31), ClosedAt = new DateTime(2026, 1, 5, 0, 0, 0), Slot = new TimeOnly(0, 0), Effort = TimeSpan.FromMinutes(5), Views = 3, Rating = 5f, TeamId = 2, UnitId = 3, Channels = Channel.Email, Grade = 'B' },
                new() { Id = 6, Subject = "50% off_sale", Status = TicketStatus.Open, Previous = TicketStatus.Closed, Price = 0.1m, Discount = 0.05m, Priority = 2, Hours = 0.1, Urgent = true, Escalated = false, Reference = new Guid("55555555-5555-5555-5555-555555555555"), OpenedAt = new DateTime(2026, 2, 1, 0, 0, 0), CreatedAt = new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero), Due = new DateOnly(2026, 2, 1), ClosedAt = null, Slot = null, Effort = TimeSpan.Zero, Views = 42, Rating = 0f, TeamId = 3, UnitId = 2, Channels = Channel.Email | Channel.Chat, Grade = 'A' },
            ];

            Comment[] comments =
            [
                new() { Id = 1, TicketId = 1, Text = "Oops", Score = 1, CreatedAt = new DateTime(2026, 2, 1, 11, 0, 0), AuthorId = 1 },
                new() { Id = 2, TicketId = 1, Text = "On it", Score = 4, CreatedAt = new DateTime(2026, 2, 1, 12, 0, 0), AuthorId = 2 },
                new() { Id = 3, TicketId = 1, Text = "Done", Score = 5, CreatedAt = new DateTime(2026, 2, 1, 13, 0, 0), AuthorId = 1 },
                new() { Id = 4, TicketId = 2, Text = "Paper", Score = 2, CreatedAt = new DateTime(2026, 1, 2, 8, 0, 0), AuthorId = 3 },
                new() { Id = 5, TicketId = 3, Text = "Screen swapped", Score = 3, CreatedAt = new DateTime(2026, 1, 31, 10, 0, 0), AuthorId = 2 },
                new() { Id = 6, TicketId = 3, Text = "Still flickers", Score = 1, CreatedAt = new DateTime(2026, 1, 31, 11, 0, 0), AuthorId = 1 },
                new() { Id = 7, TicketId = 6, Text = "5 left", Score = 2, CreatedAt = new DateTime(2026, 2, 1, 1, 0, 0), AuthorId = 4 },
            ];

            foreach (var comment in comments)
            {
                comment.Author = Members.First(member => member.Id == comment.AuthorId);
                tickets.First(ticket => ticket.Id == comment.TicketId).Comments.Add(comment);
            }

            foreach (var ticket in tickets)
            {
                ticket.Team = Teams.First(team => team.Id == ticket.TeamId);
                ticket.Unit = Units.FirstOrDefault(unit => unit.Id == ticket.UnitId);
            }

            return tickets;
        }

        private static AnalyticsSession[] CreateSessions()
        {
            static AnalyticsEvent Event(int id, string session, string type, params (string Key, object Value)[] data) =>
                new() { Id = id, SessionId = session, Type = type, Data = data.ToDictionary(pair => pair.Key, pair => pair.Value) };

            return
            [
                new() { Id = "s1", UserId = "u1", Events = [Event(1, "s1", "trial_check", ("remaining", 15), ("application", "Radzen Blazor for Visual Studio")), Event(2, "s1", "application_open", ("application", "Radzen Blazor for Visual Studio"))] },
                new() { Id = "s2", UserId = "u2", Events = [Event(3, "s2", "trial_check", ("remaining", 15), ("application", "Radzen Blazor for Visual Studio"))] },
                new() { Id = "s3", UserId = "u3", Events = [Event(4, "s3", "trial_check", ("remaining", 14), ("application", "Radzen Blazor Studio"))] },
                new() { Id = "s4", UserId = "u4", Events = [Event(5, "s4", "trial_check", ("remaining", 15), ("application", "Radzen Blazor Studio")), Event(6, "s4", "toolbox_drop", ("component", "RadzenButton"))] },
                new() { Id = "s5", UserId = "u5", Events = [] },
            ];
        }
    }

    public class HelpdeskContext(DbContextOptions<HelpdeskContext> options) : DbContext(options)
    {
        public DbSet<Ticket> Tickets => Set<Ticket>();
        public DbSet<Team> Teams => Set<Team>();
        public DbSet<Member> Members => Set<Member>();
        public DbSet<Schedule> Schedules => Set<Schedule>();
        public DbSet<Unit> Units => Set<Unit>();
        public DbSet<Comment> Comments => Set<Comment>();
        public DbSet<License> Licenses => Set<License>();
        public DbSet<Log> Logs => Set<Log>();
        public DbSet<OrderLine> OrderLines => Set<OrderLine>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Ticket>().Property(ticket => ticket.Price).HasConversion<double>();
            modelBuilder.Entity<Ticket>().Property(ticket => ticket.Discount).HasConversion<double?>();
            modelBuilder.Entity<Ticket>().Property(ticket => ticket.CreatedAt).HasConversion(value => value.UtcTicks, value => new DateTimeOffset(value, TimeSpan.Zero));
            modelBuilder.Entity<Ticket>().Property(ticket => ticket.Effort).HasConversion(value => value.Ticks, value => new TimeSpan(value));
            modelBuilder.Entity<Team>().HasMany(team => team.Members).WithOne().HasForeignKey(member => member.TeamId);
            modelBuilder.Entity<License>().Property(license => license.Price).HasConversion<double?>();
            modelBuilder.Entity<Ticket>().Ignore(ticket => ticket.Grade);
        }
    }

    public class TicketsController(HelpdeskContext context) : ODataController
    {
        [EnableQuery(MaxExpansionDepth = 5, MaxAnyAllExpressionDepth = 5, MaxNodeCount = 1000)]
        public IQueryable<Ticket> Get() => context.Tickets;
    }

    public class MemoryTicketsController : ODataController
    {
        [EnableQuery(MaxExpansionDepth = 5, MaxAnyAllExpressionDepth = 5, MaxNodeCount = 1000)]
        public IQueryable<Ticket> Get() => HelpdeskSeed.Tickets.AsQueryable();
    }

    public class LicensesController(HelpdeskContext context) : ODataController
    {
        [EnableQuery(MaxNodeCount = 1000)]
        public IQueryable<License> Get() => context.Licenses;
    }

    public class OrderLinesController(HelpdeskContext context) : ODataController
    {
        [EnableQuery(MaxNodeCount = 1000)]
        public IQueryable<OrderLine> Get() => context.OrderLines;
    }

    public class LogsController(HelpdeskContext context) : ODataController
    {
        [EnableQuery]
        public IQueryable<Log> Get() => context.Logs;
    }

    public class AnalyticsSessionsController : ODataController
    {
        [EnableQuery(MaxExpansionDepth = 5, MaxAnyAllExpressionDepth = 5, MaxNodeCount = 1000)]
        public IQueryable<AnalyticsSession> Get() => HelpdeskSeed.Sessions.AsQueryable();
    }

    public sealed class ODataServer : IAsyncLifetime
    {
        private readonly SqliteConnection connection = new("Data Source=:memory:");
        private WebApplication app;

        public HttpClient Client { get; private set; }

        public Uri Root => new(Client.BaseAddress, "odata/");

        public static IEdmModel Model()
        {
            var builder = new ODataConventionModelBuilder();
            builder.EntitySet<Ticket>("Tickets");
            builder.EntitySet<Ticket>("MemoryTickets");
            builder.EntitySet<License>("Licenses");
            builder.EntitySet<Log>("Logs");
            builder.EntitySet<OrderLine>("OrderLines");
            builder.EntitySet<AnalyticsSession>("AnalyticsSessions");
            builder.EntityType<Ticket>().Ignore(ticket => ticket.Grade);
            return builder.GetEdmModel();
        }

        public async Task InitializeAsync()
        {
            await connection.OpenAsync();

            using (var pragma = connection.CreateCommand())
            {
                pragma.CommandText = "PRAGMA case_sensitive_like = ON;";
                await pragma.ExecuteNonQueryAsync();
            }

            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseTestServer();
            builder.Logging.ClearProviders();
            builder.Services.AddDbContext<HelpdeskContext>(options => options.UseSqlite(connection));
            builder.Services.AddControllers()
                .AddApplicationPart(typeof(TicketsController).Assembly)
                .AddOData(options => options.Count().Filter().OrderBy().Expand().Select().SetMaxTop(null).AddRouteComponents("odata", Model()).TimeZone = TimeZoneInfo.Utc);

            app = builder.Build();
            app.MapControllers();

            using (var scope = app.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<HelpdeskContext>();
                await context.Database.EnsureCreatedAsync();
                var schedules = HelpdeskSeed.Teams.ToDictionary(team => team, team => team.Schedule);

                foreach (var team in HelpdeskSeed.Teams)
                {
                    team.Schedule = null;
                    team.ScheduleId = null;
                }

                context.AddRange(HelpdeskSeed.Tickets);
                context.AddRange(HelpdeskSeed.Schedules);
                context.AddRange(HelpdeskSeed.Licenses);
                context.AddRange(HelpdeskSeed.Logs);
                context.AddRange(HelpdeskSeed.OrderLines);
                await context.SaveChangesAsync();

                foreach (var (team, schedule) in schedules)
                {
                    team.Schedule = schedule;
                    team.ScheduleId = schedule?.Id;
                }

                await context.SaveChangesAsync();
            }

            await app.StartAsync();
            Client = app.GetTestClient();
        }

        public async Task DisposeAsync()
        {
            Client?.Dispose();

            if (app != null)
            {
                await app.StopAsync();
                await app.DisposeAsync();
            }

            await connection.DisposeAsync();
        }

        public async Task<JsonElement> GetAsync(Uri uri)
        {
            var response = await Client.GetAsync(uri);
            var body = await response.Content.ReadAsStringAsync();
            Assert.True(response.IsSuccessStatusCode, $"GET {uri.PathAndQuery} answered {(int)response.StatusCode}: {body}");
            using var json = JsonDocument.Parse(body);
            return json.RootElement.Clone();
        }

        public async Task<(bool Success, string Body)> TryGetAsync(string relative)
        {
            var response = await Client.GetAsync(new Uri(Root, relative));
            return (response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        }
    }
}
