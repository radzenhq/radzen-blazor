using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace NorthwindBlazor.Data
{
    public partial class NorthwindContext : Microsoft.EntityFrameworkCore.DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                var builder = new SqliteConnectionStringBuilder()
                {
                    DataSource = "northwind.db",
                    Cache = SqliteCacheMode.Private
                };

                optionsBuilder.UseSqlite(builder.ConnectionString);
            }
        }

        partial void OnModelBuilding(ModelBuilder builder);

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<NorthwindBlazor.Models.Northwind.CustomerCustomerDemo>().HasKey(table => new
            {
                table.CustomerID,
                table.CustomerTypeID
            });

            builder.Entity<NorthwindBlazor.Models.Northwind.EmployeeTerritory>().HasKey(table => new
            {
                table.EmployeeID,
                table.TerritoryID
            });

            builder.Entity<NorthwindBlazor.Models.Northwind.OrderDetail>().HasKey(table => new
            {
                table.OrderID,
                table.ProductID
            });

            builder.Entity<NorthwindBlazor.Models.Northwind.CustomerCustomerDemo>()
                  .HasOne(i => i.Customer)
                  .WithMany(i => i.CustomerCustomerDemos)
                  .HasForeignKey(i => i.CustomerID)
                  .HasPrincipalKey(i => i.CustomerID);

            builder.Entity<NorthwindBlazor.Models.Northwind.CustomerCustomerDemo>()
                  .HasOne(i => i.CustomerDemographic)
                  .WithMany(i => i.CustomerCustomerDemos)
                  .HasForeignKey(i => i.CustomerTypeID)
                  .HasPrincipalKey(i => i.CustomerTypeID);

            builder.Entity<NorthwindBlazor.Models.Northwind.EmployeeTerritory>()
                  .HasOne(i => i.Employee)
                  .WithMany(i => i.EmployeeTerritories)
                  .HasForeignKey(i => i.EmployeeID)
                  .HasPrincipalKey(i => i.EmployeeID);

            builder.Entity<NorthwindBlazor.Models.Northwind.EmployeeTerritory>()
                  .HasOne(i => i.Territory)
                  .WithMany(i => i.EmployeeTerritories)
                  .HasForeignKey(i => i.TerritoryID)
                  .HasPrincipalKey(i => i.TerritoryID);

            builder.Entity<NorthwindBlazor.Models.Northwind.Order>()
                  .HasOne(i => i.Customer)
                  .WithMany(i => i.Orders)
                  .HasForeignKey(i => i.CustomerID)
                  .HasPrincipalKey(i => i.CustomerID);

            builder.Entity<NorthwindBlazor.Models.Northwind.Order>()
                  .HasOne(i => i.Employee)
                  .WithMany(i => i.Orders)
                  .HasForeignKey(i => i.EmployeeID)
                  .HasPrincipalKey(i => i.EmployeeID);

            builder.Entity<NorthwindBlazor.Models.Northwind.OrderDetail>()
                  .HasOne(i => i.Order)
                  .WithMany(i => i.OrderDetails)
                  .HasForeignKey(i => i.OrderID)
                  .HasPrincipalKey(i => i.OrderID);

            builder.Entity<NorthwindBlazor.Models.Northwind.OrderDetail>()
                  .HasOne(i => i.Product)
                  .WithMany(i => i.OrderDetails)
                  .HasForeignKey(i => i.ProductID)
                  .HasPrincipalKey(i => i.ProductID);

            builder.Entity<NorthwindBlazor.Models.Northwind.Product>()
                  .HasOne(i => i.Supplier)
                  .WithMany(i => i.Products)
                  .HasForeignKey(i => i.SupplierID)
                  .HasPrincipalKey(i => i.SupplierID);

            builder.Entity<NorthwindBlazor.Models.Northwind.Product>()
                  .HasOne(i => i.Category)
                  .WithMany(i => i.Products)
                  .HasForeignKey(i => i.CategoryID)
                  .HasPrincipalKey(i => i.CategoryID);

            builder.Entity<NorthwindBlazor.Models.Northwind.Territory>()
                  .HasOne(i => i.Region)
                  .WithMany(i => i.Territories)
                  .HasForeignKey(i => i.RegionID)
                  .HasPrincipalKey(i => i.RegionID);

            builder.Entity<NorthwindBlazor.Models.Northwind.Order>()
                  .Property(p => p.Freight)
                  .HasDefaultValueSql("(0)");

            builder.Entity<NorthwindBlazor.Models.Northwind.OrderDetail>()
                  .Property(p => p.UnitPrice)
                  .HasDefaultValueSql("(0)");

            builder.Entity<NorthwindBlazor.Models.Northwind.OrderDetail>()
                  .Property(p => p.Quantity)
                  .HasDefaultValueSql("(1)");

            builder.Entity<NorthwindBlazor.Models.Northwind.OrderDetail>()
                  .Property(p => p.Discount)
                  .HasDefaultValueSql("(0)");

            builder.Entity<NorthwindBlazor.Models.Northwind.Product>()
                  .Property(p => p.UnitPrice)
                  .HasDefaultValueSql("(0)");

            builder.Entity<NorthwindBlazor.Models.Northwind.Product>()
                  .Property(p => p.UnitsInStock)
                  .HasDefaultValueSql("(0)");

            builder.Entity<NorthwindBlazor.Models.Northwind.Product>()
                  .Property(p => p.UnitsOnOrder)
                  .HasDefaultValueSql("(0)");

            builder.Entity<NorthwindBlazor.Models.Northwind.Product>()
                  .Property(p => p.ReorderLevel)
                  .HasDefaultValueSql("(0)");

            builder.Entity<NorthwindBlazor.Models.Northwind.Product>()
                  .Property(p => p.Discontinued)
                  .HasDefaultValueSql("(0)");

            this.OnModelBuilding(builder);
        }


        public DbSet<NorthwindBlazor.Models.Northwind.Category> Categories
        {
            get;
            set;
        }

        public DbSet<NorthwindBlazor.Models.Northwind.Customer> Customers
        {
            get;
            set;
        }

        public DbSet<NorthwindBlazor.Models.Northwind.CustomerCustomerDemo> CustomerCustomerDemos
        {
            get;
            set;
        }

        public DbSet<NorthwindBlazor.Models.Northwind.CustomerDemographic> CustomerDemographics
        {
            get;
            set;
        }

        public DbSet<NorthwindBlazor.Models.Northwind.Employee> Employees
        {
            get;
            set;
        }

        public DbSet<NorthwindBlazor.Models.Northwind.EmployeeTerritory> EmployeeTerritories
        {
            get;
            set;
        }

        public DbSet<NorthwindBlazor.Models.Northwind.Order> Orders
        {
            get;
            set;
        }

        public DbSet<NorthwindBlazor.Models.Northwind.OrderDetail> OrderDetails
        {
            get;
            set;
        }

        public DbSet<NorthwindBlazor.Models.Northwind.Product> Products
        {
            get;
            set;
        }

        public DbSet<NorthwindBlazor.Models.Northwind.Region> Regions
        {
            get;
            set;
        }

        public DbSet<NorthwindBlazor.Models.Northwind.Supplier> Suppliers
        {
            get;
            set;
        }

        public DbSet<NorthwindBlazor.Models.Northwind.Territory> Territories
        {
            get;
            set;
        }
    }
}
