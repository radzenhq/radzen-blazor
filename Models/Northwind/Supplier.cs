using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RadzenBlazorDemos.Models.Northwind
{
  [Table("Suppliers")]
  public partial class Supplier
  {
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int SupplierID
    {
      get;
      set;
    }


    [InverseProperty("Supplier")]
    public ICollection<Product> Products { get; set; }
    public string CompanyName
    {
      get;
      set;
    }
    public string ContactName
    {
      get;
      set;
    }
    public string ContactTitle
    {
      get;
      set;
    }
    public string Address
    {
      get;
      set;
    }
    public string City
    {
      get;
      set;
    }
    public string Region
    {
      get;
      set;
    }
    public string PostalCode
    {
      get;
      set;
    }
    public string Country
    {
      get;
      set;
    }
    public string Phone
    {
      get;
      set;
    }
    public string Fax
    {
      get;
      set;
    }
    public string HomePage
    {
      get;
      set;
    }
  }
}
