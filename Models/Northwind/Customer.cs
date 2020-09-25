using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RadzenBlazorDemos.Models.Northwind
{
  [Table("Customers")]
  public partial class Customer
  {
    [Key]
    public string CustomerID
    {
      get;
      set;
    }


    [InverseProperty("Customer")]
    public ICollection<Order> Orders { get; set; }

    [InverseProperty("Customer")]
    public ICollection<CustomerCustomerDemo> CustomerCustomerDemos { get; set; }
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
  }
}
