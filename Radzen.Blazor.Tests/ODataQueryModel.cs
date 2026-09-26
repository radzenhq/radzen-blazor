using System;
using System.Collections.Generic;

namespace Radzen.Blazor.Tests.Helpdesk
{
    public enum TicketStatus
    {
        Open,
        Closed,
        Pending,
    }

    [Flags]
    public enum Channel
    {
        None = 0,
        Email = 1,
        Phone = 2,
        Chat = 4,
    }

    public class Member
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int TeamId { get; set; }
    }

    public class Schedule
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int? OwnerId { get; set; }
        public Member Owner { get; set; }
    }

    public class Team
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int? ScheduleId { get; set; }
        public Schedule Schedule { get; set; }
        public List<Member> Members { get; set; } = [];
        public ICollection<Ticket> Tickets { get; set; } = [];
    }

    public class Unit
    {
        public int Id { get; set; }
        public string Code { get; set; }
    }

    public class Comment
    {
        public int Id { get; set; }
        public int TicketId { get; set; }
        public string Text { get; set; }
        public int Score { get; set; }
        public DateTime CreatedAt { get; set; }
        public int AuthorId { get; set; }
        public Member Author { get; set; }
    }

    public class Ticket
    {
        public int Id { get; set; }
        public string Subject { get; set; }
        public TicketStatus Status { get; set; }
        public TicketStatus? Previous { get; set; }
        public decimal Price { get; set; }
        public decimal? Discount { get; set; }
        public int Priority { get; set; }
        public double Hours { get; set; }
        public bool Urgent { get; set; }
        public bool? Escalated { get; set; }
        public Guid Reference { get; set; }
        public DateTime OpenedAt { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateOnly Due { get; set; }
        public DateTime? ClosedAt { get; set; }
        public TimeOnly? Slot { get; set; }
        public TimeSpan Effort { get; set; }
        public long Views { get; set; }
        public float Rating { get; set; }
        public int TeamId { get; set; }
        public Team Team { get; set; }
        public int? UnitId { get; set; }
        public Unit Unit { get; set; }
        public List<Comment> Comments { get; set; } = [];
        public Channel Channels { get; set; }
        public char Grade { get; set; }
    }

    public class License
    {
        public long Id { get; set; }
        public string Email { get; set; }
        public string LicenseKey { get; set; }
        public string ProductName { get; set; }
        public decimal? Price { get; set; }
        public string Currency { get; set; }
        public bool? Refunded { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class LicenseTotal
    {
        public string Currency { get; set; }
        public decimal? TotalPrice { get; set; }
    }

    public class LicenseSummary
    {
        public string Currency { get; set; }
        public string ProductName { get; set; }
        public int Count { get; set; }
        public int Customers { get; set; }
        public decimal? Lowest { get; set; }
        public decimal? Highest { get; set; }
        public decimal? Mean { get; set; }
    }

    public class OrderLine
    {
        public int Id { get; set; }
        public string Category { get; set; }
        public double? UnitPrice { get; set; }
        public short? Quantity { get; set; }
        public float? Discount { get; set; }
    }

    public class LineTotals
    {
        public string Category { get; set; }
        public double? Amount { get; set; }
        public int? TotalQuantity { get; set; }
        public long? LongQuantity { get; set; }
        public double? AveragePrice { get; set; }
        public double? AverageDiscount { get; set; }
        public double? HighestDiscount { get; set; }
        public int? WholePrices { get; set; }
        public int Lines { get; set; }
    }

    public class Log
    {
        public long Id { get; set; }
        public DateTime Date { get; set; }
        public string Product { get; set; }
        public long LicenseId { get; set; }
        public License License { get; set; }
    }

    public class AnalyticsSession
    {
        public string Id { get; set; }
        public string UserId { get; set; }
        public ICollection<AnalyticsEvent> Events { get; set; } = [];
    }

    public class AnalyticsEvent
    {
        public int Id { get; set; }
        public string Type { get; set; }
        public string SessionId { get; set; }
        public IDictionary<string, object> Data { get; set; } = new Dictionary<string, object>();
    }
}
