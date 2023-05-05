using Microsoft.AspNetCore.Mvc;
using RadzenBlazorDemos.Data;

namespace RadzenBlazorDemos
{
    public partial class ExportNorthwindController : ExportController
    {
        private readonly NorthwindContext context;

        public ExportNorthwindController(NorthwindContext context)
        {
            this.context = context;
            context.Seed();
        }

        [HttpGet("/export/Northwind/categories/csv")]
        public FileStreamResult ExportCategoriesToCSV()
        {
            return ToCSV(ApplyQuery(context.Categories, Request.Query));
        }

        [HttpGet("/export/Northwind/categories/excel")]
        public FileStreamResult ExportCategoriesToExcel()
        {
            return ToExcel(ApplyQuery(context.Categories, Request.Query));
        }

        [HttpGet("/export/Northwind/customers/csv")]
        public FileStreamResult ExportCustomersToCSV()
        {
            return ToCSV(ApplyQuery(context.Customers, Request.Query));
        }

        [HttpGet("/export/Northwind/customers/excel")]
        public FileStreamResult ExportCustomersToExcel()
        {
            return ToExcel(ApplyQuery(context.Customers, Request.Query));
        }

        [HttpGet("/export/Northwind/customercustomerdemos/csv")]
        public FileStreamResult ExportCustomerCustomerDemosToCSV()
        {
            return ToCSV(ApplyQuery(context.CustomerCustomerDemos, Request.Query));
        }

        [HttpGet("/export/Northwind/customercustomerdemos/excel")]
        public FileStreamResult ExportCustomerCustomerDemosToExcel()
        {
            return ToExcel(ApplyQuery(context.CustomerCustomerDemos, Request.Query));
        }

        [HttpGet("/export/Northwind/customerdemographics/csv")]
        public FileStreamResult ExportCustomerDemographicsToCSV()
        {
            return ToCSV(ApplyQuery(context.CustomerDemographics, Request.Query));
        }

        [HttpGet("/export/Northwind/customerdemographics/excel")]
        public FileStreamResult ExportCustomerDemographicsToExcel()
        {
            return ToExcel(ApplyQuery(context.CustomerDemographics, Request.Query));
        }

        [HttpGet("/export/Northwind/employees/csv")]
        public FileStreamResult ExportEmployeesToCSV()
        {
            return ToCSV(ApplyQuery(context.Employees, Request.Query));
        }

        [HttpGet("/export/Northwind/employees/excel")]
        public FileStreamResult ExportEmployeesToExcel()
        {
            return ToExcel(ApplyQuery(context.Employees, Request.Query));
        }

        [HttpGet("/export/Northwind/employeeterritories/csv")]
        public FileStreamResult ExportEmployeeTerritoriesToCSV()
        {
            return ToCSV(ApplyQuery(context.EmployeeTerritories, Request.Query));
        }

        [HttpGet("/export/Northwind/employeeterritories/excel")]
        public FileStreamResult ExportEmployeeTerritoriesToExcel()
        {
            return ToExcel(ApplyQuery(context.EmployeeTerritories, Request.Query));
        }

        [HttpGet("/export/Northwind/orders/csv")]
        public FileStreamResult ExportOrdersToCSV()
        {
            return ToCSV(ApplyQuery(context.Orders, Request.Query));
        }

        [HttpGet("/export/Northwind/orders/excel")]
        public FileStreamResult ExportOrdersToExcel()
        {
            return ToExcel(ApplyQuery(context.Orders, Request.Query));
        }

        [HttpGet("/export/Northwind/orderdetails/csv")]
        public FileStreamResult ExportOrderDetailsToCSV()
        {
            return ToCSV(ApplyQuery(context.OrderDetails, Request.Query));
        }

        [HttpGet("/export/Northwind/orderdetails/excel")]
        public FileStreamResult ExportOrderDetailsToExcel()
        {
            return ToExcel(ApplyQuery(context.OrderDetails, Request.Query));
        }

        [HttpGet("/export/Northwind/products/csv")]
        public FileStreamResult ExportProductsToCSV()
        {
            return ToCSV(ApplyQuery(context.Products, Request.Query));
        }

        [HttpGet("/export/Northwind/products/excel")]
        public FileStreamResult ExportProductsToExcel()
        {
            return ToExcel(ApplyQuery(context.Products, Request.Query));
        }

        [HttpGet("/export/Northwind/regions/csv")]
        public FileStreamResult ExportRegionsToCSV()
        {
            return ToCSV(ApplyQuery(context.Regions, Request.Query));
        }

        [HttpGet("/export/Northwind/regions/excel")]
        public FileStreamResult ExportRegionsToExcel()
        {
            return ToExcel(ApplyQuery(context.Regions, Request.Query));
        }

        [HttpGet("/export/Northwind/suppliers/csv")]
        public FileStreamResult ExportSuppliersToCSV()
        {
            return ToCSV(ApplyQuery(context.Suppliers, Request.Query));
        }

        [HttpGet("/export/Northwind/suppliers/excel")]
        public FileStreamResult ExportSuppliersToExcel()
        {
            return ToExcel(ApplyQuery(context.Suppliers, Request.Query));
        }

        [HttpGet("/export/Northwind/territories/csv")]
        public FileStreamResult ExportTerritoriesToCSV()
        {
            return ToCSV(ApplyQuery(context.Territories, Request.Query));
        }

        [HttpGet("/export/Northwind/territories/excel")]
        public FileStreamResult ExportTerritoriesToExcel()
        {
            return ToExcel(ApplyQuery(context.Territories, Request.Query));
        }
    }
}
