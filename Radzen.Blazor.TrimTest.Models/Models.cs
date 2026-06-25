namespace Radzen.Blazor.TrimTest.Models;

public enum OrderStatus { New, Processing, Shipped, Closed }

public class Customer
{
    public string Name { get; set; } = "";
    // Nested path Customer.City (audit H14). DAM does NOT flow transitively into nested types, so this
    // getter is a consumer-rooting responsibility - the runtime gate does NOT assert it survives.
    public string City { get; set; } = "";
}

public class Order
{
    public string CustomerName { get; set; } = "";
    public double Total { get; set; }
    public OrderStatus Status { get; set; }
    public OrderStatus? Priority { get; set; }   // nullable enum filter column (audit L5)
    public Customer Customer { get; set; } = new();
    public DateTime Created { get; set; }
}

public class Person
{
    public int Id { get; set; }
    public string FullName { get; set; } = "";
}

public class Appt
{
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public string Text { get; set; } = "";
}

public class Sale
{
    public string Region { get; set; } = "";
    public double Revenue { get; set; }
}
