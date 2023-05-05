using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace RadzenBlazorDemos.Data
{
    public partial class NorthwindContext : Microsoft.EntityFrameworkCore.DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseInMemoryDatabase("Northwind");
            }
        }

        partial void OnModelBuilding(ModelBuilder builder);

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<RadzenBlazorDemos.Models.Northwind.CustomerCustomerDemo>().HasKey(table => new
            {
                table.CustomerID,
                table.CustomerTypeID
            });

            builder.Entity<RadzenBlazorDemos.Models.Northwind.EmployeeTerritory>().HasKey(table => new
            {
                table.EmployeeID,
                table.TerritoryID
            });

            builder.Entity<RadzenBlazorDemos.Models.Northwind.OrderDetail>().HasKey(table => new
            {
                table.OrderID,
                table.ProductID
            });

            builder.Entity<RadzenBlazorDemos.Models.Northwind.Employee>()
                   .HasOne(i => i.Employee1)
                   .WithMany(i => i.Employees1)
                   .HasForeignKey(i => i.ReportsTo)
                   .HasPrincipalKey(i => i.EmployeeID);

            builder.Entity<RadzenBlazorDemos.Models.Northwind.CustomerCustomerDemo>()
                  .HasOne(i => i.Customer)
                  .WithMany(i => i.CustomerCustomerDemos)
                  .HasForeignKey(i => i.CustomerID)
                  .HasPrincipalKey(i => i.CustomerID);

            builder.Entity<RadzenBlazorDemos.Models.Northwind.CustomerCustomerDemo>()
                  .HasOne(i => i.CustomerDemographic)
                  .WithMany(i => i.CustomerCustomerDemos)
                  .HasForeignKey(i => i.CustomerTypeID)
                  .HasPrincipalKey(i => i.CustomerTypeID);

            builder.Entity<RadzenBlazorDemos.Models.Northwind.EmployeeTerritory>()
                  .HasOne(i => i.Employee)
                  .WithMany(i => i.EmployeeTerritories)
                  .HasForeignKey(i => i.EmployeeID)
                  .HasPrincipalKey(i => i.EmployeeID);

            builder.Entity<RadzenBlazorDemos.Models.Northwind.EmployeeTerritory>()
                  .HasOne(i => i.Territory)
                  .WithMany(i => i.EmployeeTerritories)
                  .HasForeignKey(i => i.TerritoryID)
                  .HasPrincipalKey(i => i.TerritoryID);

            builder.Entity<RadzenBlazorDemos.Models.Northwind.Order>()
                  .HasOne(i => i.Customer)
                  .WithMany(i => i.Orders)
                  .HasForeignKey(i => i.CustomerID)
                  .HasPrincipalKey(i => i.CustomerID);

            builder.Entity<RadzenBlazorDemos.Models.Northwind.Order>()
                  .HasOne(i => i.Employee)
                  .WithMany(i => i.Orders)
                  .HasForeignKey(i => i.EmployeeID)
                  .HasPrincipalKey(i => i.EmployeeID);

            builder.Entity<RadzenBlazorDemos.Models.Northwind.OrderDetail>()
                  .HasOne(i => i.Order)
                  .WithMany(i => i.OrderDetails)
                  .HasForeignKey(i => i.OrderID)
                  .HasPrincipalKey(i => i.OrderID);

            builder.Entity<RadzenBlazorDemos.Models.Northwind.OrderDetail>()
                  .HasOne(i => i.Product)
                  .WithMany(i => i.OrderDetails)
                  .HasForeignKey(i => i.ProductID)
                  .HasPrincipalKey(i => i.ProductID);

            builder.Entity<RadzenBlazorDemos.Models.Northwind.Product>()
                  .HasOne(i => i.Supplier)
                  .WithMany(i => i.Products)
                  .HasForeignKey(i => i.SupplierID)
                  .HasPrincipalKey(i => i.SupplierID);

            builder.Entity<RadzenBlazorDemos.Models.Northwind.Product>()
                  .HasOne(i => i.Category)
                  .WithMany(i => i.Products)
                  .HasForeignKey(i => i.CategoryID)
                  .HasPrincipalKey(i => i.CategoryID);

            builder.Entity<RadzenBlazorDemos.Models.Northwind.Territory>()
                  .HasOne(i => i.Region)
                  .WithMany(i => i.Territories)
                  .HasForeignKey(i => i.RegionID)
                  .HasPrincipalKey(i => i.RegionID);

            builder.Entity<RadzenBlazorDemos.Models.Northwind.Order>()
                  .Property(p => p.Freight);

            builder.Entity<RadzenBlazorDemos.Models.Northwind.OrderDetail>()
                  .Property(p => p.UnitPrice);

            builder.Entity<RadzenBlazorDemos.Models.Northwind.OrderDetail>()
                  .Property(p => p.Quantity);

            builder.Entity<RadzenBlazorDemos.Models.Northwind.OrderDetail>()
                  .Property(p => p.Discount);

            builder.Entity<RadzenBlazorDemos.Models.Northwind.Product>()
                  .Property(p => p.UnitPrice);

            builder.Entity<RadzenBlazorDemos.Models.Northwind.Product>()
                  .Property(p => p.UnitsInStock);

            builder.Entity<RadzenBlazorDemos.Models.Northwind.Product>()
                  .Property(p => p.UnitsOnOrder);

            builder.Entity<RadzenBlazorDemos.Models.Northwind.Product>()
                  .Property(p => p.ReorderLevel);

            builder.Entity<RadzenBlazorDemos.Models.Northwind.Product>()
                  .Property(p => p.Discontinued);

            this.OnModelBuilding(builder);
        }


        public DbSet<RadzenBlazorDemos.Models.Northwind.Category> Categories
        {
            get;
            set;
        }

        public DbSet<RadzenBlazorDemos.Models.Northwind.Customer> Customers
        {
            get;
            set;
        }

        public DbSet<RadzenBlazorDemos.Models.Northwind.CustomerCustomerDemo> CustomerCustomerDemos
        {
            get;
            set;
        }

        public DbSet<RadzenBlazorDemos.Models.Northwind.CustomerDemographic> CustomerDemographics
        {
            get;
            set;
        }

        public DbSet<RadzenBlazorDemos.Models.Northwind.Employee> Employees
        {
            get;
            set;
        }

        public DbSet<RadzenBlazorDemos.Models.Northwind.EmployeeTerritory> EmployeeTerritories
        {
            get;
            set;
        }

        public DbSet<RadzenBlazorDemos.Models.Northwind.Order> Orders
        {
            get;
            set;
        }

        public DbSet<RadzenBlazorDemos.Models.Northwind.OrderDetail> OrderDetails
        {
            get;
            set;
        }

        public DbSet<RadzenBlazorDemos.Models.Northwind.Product> Products
        {
            get;
            set;
        }

        public DbSet<RadzenBlazorDemos.Models.Northwind.Region> Regions
        {
            get;
            set;
        }

        public DbSet<RadzenBlazorDemos.Models.Northwind.Supplier> Suppliers
        {
            get;
            set;
        }

        public DbSet<RadzenBlazorDemos.Models.Northwind.Territory> Territories
        {
            get;
            set;
        }

        public async Task SeedAsync()
        {
            try
            {
                AddData();

                if (ChangeTracker.HasChanges())
                {
                    await SaveChangesAsync();
                }
            }
            catch
            {
                //
            }
        }

        public void Seed()
        {
            try
            {
                AddData();

                if (ChangeTracker.HasChanges())
                {
                    SaveChanges();
                }
            }
            catch
            {
                //
            }
        }

        public void AddData()
        {
            if (!Customers.Any())
            {
                Customers.AddRange(CustomersData.Data);
            }

            if (!Categories.Any())
            {
                Categories.AddRange(CategoriesData.Data);
            }

            if (!Employees.Any())
            {
                Employees.AddRange(EmployeesData.Data);
            }

            if (!Orders.Any())
            {
                Orders.AddRange(OrdersData.Data);
            }

            if (!OrderDetails.Any())
            {
                OrderDetails.AddRange(OrderDetailsData.Data);
            }

            if (!Products.Any())
            {
                Products.AddRange(ProductsData.Data);
            }

            if (!Regions.Any())
            {
                Regions.AddRange(RegionsData.Data);
            }

            if (!Territories.Any())
            {
                Territories.AddRange(TerritoriesData.Data);
            }

            if (!Suppliers.Any())
            {
                Suppliers.AddRange(SuppliersData.Data);
            }
        }
    }
}
