using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RadzenBlazorDemos.Models.Northwind
{
  [Table("OrderDetails")]
  public partial class OrderDetail
  {
    [Key]
    public int OrderID
    {
      get;
      set;
    }

    [ForeignKey("OrderID")]
    public Order Order { get; set; }
    public int ProductID
    {
      get;
      set;
    }

    [ForeignKey("ProductID")]
    public Product Product { get; set; }
    public double? UnitPrice
    {
      get;
      set;
    }
    public Int16? Quantity
    {
      get;
      set;
    }
    public float? Discount
    {
      get;
      set;
    }
  }
}
