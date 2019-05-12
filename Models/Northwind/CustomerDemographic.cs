using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NorthwindBlazor.Models.Northwind
{
  [Table("CustomerDemographics")]
  public partial class CustomerDemographic
  {
    [Key]
    public string CustomerTypeID
    {
      get;
      set;
    }


    [InverseProperty("CustomerDemographic")]
    public ICollection<CustomerCustomerDemo> CustomerCustomerDemos { get; set; }
    public string CustomerDesc
    {
      get;
      set;
    }
  }
}
