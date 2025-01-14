using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RadzenBlazorDemos.Models.Northwind
{
  [Table("Products")]
  public partial class Product
  {
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int ProductID
    {
      get;
      set;
    }

    public static ProductComparer Comparer { get; } = new();
    public class ProductComparer : IEqualityComparer<Product>, IEqualityComparer<object>
    {
      public bool Equals(Product x, Product y)
      {
        if (ReferenceEquals(x, y)) return true;
        if (x is null) return false;
        if (y is null) return false;
        if (x.GetType() != y.GetType()) return false;
        return x.ProductName == y.ProductName;
      }

      public int GetHashCode(Product obj)
      {
        return (obj.ProductName != null ? obj.ProductName.GetHashCode() : 0);
      }

      public new bool Equals(object x, object y)
      {
        return Equals((Product)x, (Product)y);
      }

      public int GetHashCode(object obj)
      {
        return GetHashCode((Product)obj);
      }
    }


    [InverseProperty("Product")]
    public ICollection<OrderDetail> OrderDetails { get; set; }
    public string ProductName
    {
      get;
      set;
    }
    public int? SupplierID
    {
      get;
      set;
    }

    [ForeignKey("SupplierID")]
    public Supplier Supplier { get; set; }
    public int? CategoryID
    {
      get;
      set;
    }

    [ForeignKey("CategoryID")]
    public Category Category { get; set; }
    public string QuantityPerUnit
    {
      get;
      set;
    }
    public decimal? UnitPrice
    {
      get;
      set;
    }
    public Int16? UnitsInStock
    {
      get;
      set;
    }
    public Int16? UnitsOnOrder
    {
      get;
      set;
    }
    public Int16? ReorderLevel
    {
      get;
      set;
    }
    public bool? Discontinued
    {
      get;
      set;
    }
  }
}
