using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RadzenBlazorDemos.Models.Northwind
{
    public partial class Employee
    {
        [NotMapped]
        public ICollection<Order> NorthwindOrders { get; set; }
    }
}
