using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Web;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Radzen;
using RadzenBlazorDemos.Models.Northwind;

namespace RadzenBlazorDemos
{
    public partial class NorthwindODataService
    {
        private readonly HttpClient httpClient;
        private readonly Uri baseUri;

        public NorthwindODataService(string url = "https://services.radzen.com/odata/Northwind/")
        {
            this.httpClient = new HttpClient();
            this.baseUri = new Uri(url);
        }
        partial void OnGetCategories(HttpRequestMessage requestMessage);
        public async Task<ODataServiceResult<Category>> GetCategories(string filter = default(string), int? top = default(int?), int? skip = default(int?), string orderby = default(string), string expand = default(string), string select = default(string), bool? count = default(bool?))
        {
            var uri = new Uri(baseUri, $"Categories");
            uri = uri.GetODataUri(filter:filter, top:top, skip:skip, orderby:orderby, expand:expand, select:select, count:count);

            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, uri);

            OnGetCategories(httpRequestMessage);

            var response = await httpClient.SendAsync(httpRequestMessage);

            return await response.ReadAsync<ODataServiceResult<Category>>();
        }
        partial void OnCreateCategory(HttpRequestMessage requestMessage);
        public async Task<Category> CreateCategory(Category category)
        {
            var uri = new Uri(baseUri, $"Categories");

            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, uri);

            httpRequestMessage.Content = new StringContent(ODataJsonSerializer.Serialize(category), Encoding.UTF8, "application/json");

            OnCreateCategory(httpRequestMessage);

            var response = await httpClient.SendAsync(httpRequestMessage);

            return await response.ReadAsync<Category>();
        }
        partial void OnGetCategoryById(HttpRequestMessage requestMessage);
        public async Task<Category> GetCategoryById(int categoryId)
        {
            var uri = new Uri(baseUri, $"Categories({categoryId})");

            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, uri);

            OnGetCategoryById(httpRequestMessage);

            var response = await httpClient.SendAsync(httpRequestMessage);

            return await response.ReadAsync<Category>();
        }
        partial void OnDeleteCategory(HttpRequestMessage requestMessage);
        public async Task<HttpResponseMessage> DeleteCategory(int categoryId)
        {
            var uri = new Uri(baseUri, $"Categories({categoryId})");

            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Delete, uri);

            OnDeleteCategory(httpRequestMessage);
            return await httpClient.SendAsync(httpRequestMessage);
        }
        partial void OnUpdateCategory(HttpRequestMessage requestMessage);
        public async Task<Category> UpdateCategory(int categoryId, Category category)
        {
            var uri = new Uri(baseUri, $"Categories({categoryId})");

            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Patch, uri);

            httpRequestMessage.Content = new StringContent(ODataJsonSerializer.Serialize(category), Encoding.UTF8, "application/json");

            OnUpdateCategory(httpRequestMessage);

            var response = await httpClient.SendAsync(httpRequestMessage);

            return await response.ReadAsync<Category>();
        }
        partial void OnGetCustomerDemographics(HttpRequestMessage requestMessage);
        public async Task<ODataServiceResult<CustomerDemographic>> GetCustomerDemographics(string filter = default(string), int? top = default(int?), int? skip = default(int?), string orderby = default(string), string expand = default(string), string select = default(string), bool? count = default(bool?))
        {
            var uri = new Uri(baseUri, $"CustomerDemographics");
            uri = uri.GetODataUri(filter:filter, top:top, skip:skip, orderby:orderby, expand:expand, select:select, count:count);

            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, uri);

            OnGetCustomerDemographics(httpRequestMessage);

            var response = await httpClient.SendAsync(httpRequestMessage);

            return await response.ReadAsync<ODataServiceResult<CustomerDemographic>>();
        }
        partial void OnCreateCustomerDemographic(HttpRequestMessage requestMessage);
        public async Task<CustomerDemographic> CreateCustomerDemographic(CustomerDemographic customerDemographic)
        {
            var uri = new Uri(baseUri, $"CustomerDemographics");

            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, uri);

            httpRequestMessage.Content = new StringContent(ODataJsonSerializer.Serialize(customerDemographic), Encoding.UTF8, "application/json");

            OnCreateCustomerDemographic(httpRequestMessage);

            var response = await httpClient.SendAsync(httpRequestMessage);

            return await response.ReadAsync<CustomerDemographic>();
        }
        partial void OnGetCustomerDemographicById(HttpRequestMessage requestMessage);
        public async Task<CustomerDemographic> GetCustomerDemographicById(string customerTypeId)
        {
            var uri = new Uri(baseUri, $"CustomerDemographics('{HttpUtility.UrlEncode(customerTypeId)}')");

            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, uri);

            OnGetCustomerDemographicById(httpRequestMessage);

            var response = await httpClient.SendAsync(httpRequestMessage);

            return await response.ReadAsync<CustomerDemographic>();
        }
        partial void OnDeleteCustomerDemographic(HttpRequestMessage requestMessage);
        public async Task<HttpResponseMessage> DeleteCustomerDemographic(string customerTypeId)
        {
            var uri = new Uri(baseUri, $"CustomerDemographics('{HttpUtility.UrlEncode(customerTypeId)}')");

            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Delete, uri);

            OnDeleteCustomerDemographic(httpRequestMessage);
            return await httpClient.SendAsync(httpRequestMessage);
        }
        partial void OnUpdateCustomerDemographic(HttpRequestMessage requestMessage);
        public async Task<CustomerDemographic> UpdateCustomerDemographic(string customerTypeId, CustomerDemographic customerDemographic)
        {
            var uri = new Uri(baseUri, $"CustomerDemographics('{HttpUtility.UrlEncode(customerTypeId)}')");

            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Patch, uri);

            httpRequestMessage.Content = new StringContent(ODataJsonSerializer.Serialize(customerDemographic), Encoding.UTF8, "application/json");

            OnUpdateCustomerDemographic(httpRequestMessage);

            var response = await httpClient.SendAsync(httpRequestMessage);

            return await response.ReadAsync<CustomerDemographic>();
        }
        partial void OnGetCustomers(HttpRequestMessage requestMessage);
        public async Task<ODataServiceResult<Customer>> GetCustomers(string filter = default(string), int? top = default(int?), int? skip = default(int?), string orderby = default(string), string expand = default(string), string select = default(string), bool? count = default(bool?))
        {
            var uri = new Uri(baseUri, $"Customers");
            uri = uri.GetODataUri(filter:filter, top:top, skip:skip, orderby:orderby, expand:expand, select:select, count:count);

            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, uri);

            OnGetCustomers(httpRequestMessage);

            var response = await httpClient.SendAsync(httpRequestMessage);

            return await response.ReadAsync<ODataServiceResult<Customer>>();
        }
        partial void OnCreateCustomer(HttpRequestMessage requestMessage);
        public async Task<Customer> CreateCustomer(Customer customer)
        {
            var uri = new Uri(baseUri, $"Customers");

            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, uri);

            httpRequestMessage.Content = new StringContent(ODataJsonSerializer.Serialize(customer), Encoding.UTF8, "application/json");

            OnCreateCustomer(httpRequestMessage);

            var response = await httpClient.SendAsync(httpRequestMessage);

            return await response.ReadAsync<Customer>();
        }
        partial void OnGetCustomerById(HttpRequestMessage requestMessage);
        public async Task<Customer> GetCustomerById(string customerId)
        {
            var uri = new Uri(baseUri, $"Customers('{HttpUtility.UrlEncode(customerId)}')");

            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, uri);

            OnGetCustomerById(httpRequestMessage);

            var response = await httpClient.SendAsync(httpRequestMessage);

            return await response.ReadAsync<Customer>();
        }
        partial void OnDeleteCustomer(HttpRequestMessage requestMessage);
        public async Task<HttpResponseMessage> DeleteCustomer(string customerId)
        {
            var uri = new Uri(baseUri, $"Customers('{HttpUtility.UrlEncode(customerId)}')");

            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Delete, uri);

            OnDeleteCustomer(httpRequestMessage);
            return await httpClient.SendAsync(httpRequestMessage);
        }
        partial void OnUpdateCustomer(HttpRequestMessage requestMessage);
        public async Task<Customer> UpdateCustomer(string customerId, Customer customer)
        {
            var uri = new Uri(baseUri, $"Customers('{HttpUtility.UrlEncode(customerId)}')");

            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Patch, uri);

            httpRequestMessage.Content = new StringContent(ODataJsonSerializer.Serialize(customer), Encoding.UTF8, "application/json");

            OnUpdateCustomer(httpRequestMessage);

            var response = await httpClient.SendAsync(httpRequestMessage);

            return await response.ReadAsync<Customer>();
        }
        partial void OnGetEmployees(HttpRequestMessage requestMessage);
        public async Task<ODataServiceResult<Employee>> GetEmployees(string filter = default(string), int? top = default(int?), int? skip = default(int?), string orderby = default(string), string expand = default(string), string select = default(string), bool? count = default(bool?))
        {
            var uri = new Uri(baseUri, $"Employees");
            uri = uri.GetODataUri(filter:filter, top:top, skip:skip, orderby:orderby, expand:expand, select:select, count:count);

            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, uri);

            OnGetEmployees(httpRequestMessage);

            var response = await httpClient.SendAsync(httpRequestMessage);

            return await response.ReadAsync<ODataServiceResult<Employee>>();
        }
        partial void OnCreateEmployee(HttpRequestMessage requestMessage);
        public async Task<Employee> CreateEmployee(Employee employee)
        {
            var uri = new Uri(baseUri, $"Employees");

            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, uri);

            httpRequestMessage.Content = new StringContent(ODataJsonSerializer.Serialize(employee), Encoding.UTF8, "application/json");

            OnCreateEmployee(httpRequestMessage);

            var response = await httpClient.SendAsync(httpRequestMessage);

            return await response.ReadAsync<Employee>();
        }
        partial void OnGetEmployeeById(HttpRequestMessage requestMessage);
        public async Task<Employee> GetEmployeeById(int employeeId)
        {
            var uri = new Uri(baseUri, $"Employees({employeeId})");

            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, uri);

            OnGetEmployeeById(httpRequestMessage);

            var response = await httpClient.SendAsync(httpRequestMessage);

            return await response.ReadAsync<Employee>();
        }
        partial void OnDeleteEmployee(HttpRequestMessage requestMessage);
        public async Task<HttpResponseMessage> DeleteEmployee(int employeeId)
        {
            var uri = new Uri(baseUri, $"Employees({employeeId})");

            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Delete, uri);

            OnDeleteEmployee(httpRequestMessage);
            return await httpClient.SendAsync(httpRequestMessage);
        }
        partial void OnUpdateEmployee(HttpRequestMessage requestMessage);
        public async Task<Employee> UpdateEmployee(int employeeId, Employee employee)
        {
            var uri = new Uri(baseUri, $"Employees({employeeId})");

            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Patch, uri);

            httpRequestMessage.Content = new StringContent(ODataJsonSerializer.Serialize(employee), Encoding.UTF8, "application/json");

            OnUpdateEmployee(httpRequestMessage);

            var response = await httpClient.SendAsync(httpRequestMessage);

            return await response.ReadAsync<Employee>();
        }
        partial void OnGetOrderDetails(HttpRequestMessage requestMessage);
        public async Task<ODataServiceResult<OrderDetail>> GetOrderDetails(string filter = default(string), int? top = default(int?), int? skip = default(int?), string orderby = default(string), string expand = default(string), string select = default(string), bool? count = default(bool?))
        {
            var uri = new Uri(baseUri, $"NorthwindOrderDetails");
            uri = uri.GetODataUri(filter:filter, top:top, skip:skip, orderby:orderby, expand:expand, select:select, count:count);

            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, uri);

            OnGetOrderDetails(httpRequestMessage);

            var response = await httpClient.SendAsync(httpRequestMessage);

            return await response.ReadAsync<ODataServiceResult<OrderDetail>>();
        }
        partial void OnCreateOrderDetail(HttpRequestMessage requestMessage);
        public async Task<OrderDetail> CreateOrderDetail(OrderDetail orderDetail)
        {
            var uri = new Uri(baseUri, $"NorthwindOrderDetails");

            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, uri);

            httpRequestMessage.Content = new StringContent(ODataJsonSerializer.Serialize(orderDetail), Encoding.UTF8, "application/json");

            OnCreateOrderDetail(httpRequestMessage);

            var response = await httpClient.SendAsync(httpRequestMessage);

            return await response.ReadAsync<OrderDetail>();
        }
        partial void OnGetOrderDetailById(HttpRequestMessage requestMessage);
        public async Task<OrderDetail> GetOrderDetailById(int orderId, int productId)
        {
            var uri = new Uri(baseUri, $"NorthwindOrderDetails(OrderID={orderId},ProductID={productId})");

            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, uri);

            OnGetOrderDetailById(httpRequestMessage);

            var response = await httpClient.SendAsync(httpRequestMessage);

            return await response.ReadAsync<OrderDetail>();
        }
        partial void OnDeleteOrderDetail(HttpRequestMessage requestMessage);
        public async Task<HttpResponseMessage> DeleteOrderDetail(int orderId, int productId)
        {
            var uri = new Uri(baseUri, $"NorthwindOrderDetails(OrderID={orderId},ProductID={productId})");

            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Delete, uri);

            OnDeleteOrderDetail(httpRequestMessage);
            return await httpClient.SendAsync(httpRequestMessage);
        }
        partial void OnUpdateOrderDetail(HttpRequestMessage requestMessage);
        public async Task<OrderDetail> UpdateOrderDetail(int orderId, int productId, OrderDetail orderDetail)
        {
            var uri = new Uri(baseUri, $"NorthwindOrderDetails(OrderID={orderId},ProductID={productId})");

            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Patch, uri);

            httpRequestMessage.Content = new StringContent(ODataJsonSerializer.Serialize(orderDetail), Encoding.UTF8, "application/json");

            OnUpdateOrderDetail(httpRequestMessage);

            var response = await httpClient.SendAsync(httpRequestMessage);

            return await response.ReadAsync<OrderDetail>();
        }
        partial void OnGetOrders(HttpRequestMessage requestMessage);
        public async Task<ODataServiceResult<Order>> GetOrders(string filter = default(string), int? top = default(int?), int? skip = default(int?), string orderby = default(string), string expand = default(string), string select = default(string), bool? count = default(bool?))
        {
            var uri = new Uri(baseUri, $"NorthwindOrders");
            uri = uri.GetODataUri(filter:filter, top:top, skip:skip, orderby:orderby, expand:expand, select:select, count:count);

            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, uri);

            OnGetOrders(httpRequestMessage);

            var response = await httpClient.SendAsync(httpRequestMessage);

            return await response.ReadAsync<ODataServiceResult<Order>>();
        }
        partial void OnCreateOrder(HttpRequestMessage requestMessage);
        public async Task<Order> CreateOrder(Order order)
        {
            var uri = new Uri(baseUri, $"NorthwindOrders");

            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, uri);

            httpRequestMessage.Content = new StringContent(ODataJsonSerializer.Serialize(order), Encoding.UTF8, "application/json");

            OnCreateOrder(httpRequestMessage);

            var response = await httpClient.SendAsync(httpRequestMessage);

            return await response.ReadAsync<Order>();
        }
        partial void OnGetOrderById(HttpRequestMessage requestMessage);
        public async Task<Order> GetOrderById(int orderId)
        {
            var uri = new Uri(baseUri, $"NorthwindOrders({orderId})");

            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, uri);

            OnGetOrderById(httpRequestMessage);

            var response = await httpClient.SendAsync(httpRequestMessage);

            return await response.ReadAsync<Order>();
        }
        partial void OnDeleteOrder(HttpRequestMessage requestMessage);
        public async Task<HttpResponseMessage> DeleteOrder(int orderId)
        {
            var uri = new Uri(baseUri, $"NorthwindOrders({orderId})");

            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Delete, uri);

            OnDeleteOrder(httpRequestMessage);
            return await httpClient.SendAsync(httpRequestMessage);
        }
        partial void OnUpdateOrder(HttpRequestMessage requestMessage);
        public async Task<Order> UpdateOrder(int orderId, Order order)
        {
            var uri = new Uri(baseUri, $"NorthwindOrders({orderId})");

            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Patch, uri);

            httpRequestMessage.Content = new StringContent(ODataJsonSerializer.Serialize(order), Encoding.UTF8, "application/json");

            OnUpdateOrder(httpRequestMessage);

            var response = await httpClient.SendAsync(httpRequestMessage);

            return await response.ReadAsync<Order>();
        }
        partial void OnGetProducts(HttpRequestMessage requestMessage);
        public async Task<ODataServiceResult<Product>> GetProducts(string filter = default(string), int? top = default(int?), int? skip = default(int?), string orderby = default(string), string expand = default(string), string select = default(string), bool? count = default(bool?))
        {
            var uri = new Uri(baseUri, $"NorthwindProducts");
            uri = uri.GetODataUri(filter:filter, top:top, skip:skip, orderby:orderby, expand:expand, select:select, count:count);

            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, uri);

            OnGetProducts(httpRequestMessage);

            var response = await httpClient.SendAsync(httpRequestMessage);

            return await response.ReadAsync<ODataServiceResult<Product>>();
        }
        partial void OnCreateProduct(HttpRequestMessage requestMessage);
        public async Task<Product> CreateProduct(Product product)
        {
            var uri = new Uri(baseUri, $"NorthwindProducts");

            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, uri);

            httpRequestMessage.Content = new StringContent(ODataJsonSerializer.Serialize(product), Encoding.UTF8, "application/json");

            OnCreateProduct(httpRequestMessage);

            var response = await httpClient.SendAsync(httpRequestMessage);

            return await response.ReadAsync<Product>();
        }
        partial void OnGetProductById(HttpRequestMessage requestMessage);
        public async Task<Product> GetProductById(int productId)
        {
            var uri = new Uri(baseUri, $"NorthwindProducts({productId})");

            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, uri);

            OnGetProductById(httpRequestMessage);

            var response = await httpClient.SendAsync(httpRequestMessage);

            return await response.ReadAsync<Product>();
        }
        partial void OnDeleteProduct(HttpRequestMessage requestMessage);
        public async Task<HttpResponseMessage> DeleteProduct(int productId)
        {
            var uri = new Uri(baseUri, $"NorthwindProducts({productId})");

            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Delete, uri);

            OnDeleteProduct(httpRequestMessage);
            return await httpClient.SendAsync(httpRequestMessage);
        }
        partial void OnUpdateProduct(HttpRequestMessage requestMessage);
        public async Task<Product> UpdateProduct(int productId, Product product)
        {
            var uri = new Uri(baseUri, $"NorthwindProducts({productId})");

            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Patch, uri);

            httpRequestMessage.Content = new StringContent(ODataJsonSerializer.Serialize(product), Encoding.UTF8, "application/json");

            OnUpdateProduct(httpRequestMessage);

            var response = await httpClient.SendAsync(httpRequestMessage);

            return await response.ReadAsync<Product>();
        }
        partial void OnGetRegions(HttpRequestMessage requestMessage);
        public async Task<ODataServiceResult<Region>> GetRegions(string filter = default(string), int? top = default(int?), int? skip = default(int?), string orderby = default(string), string expand = default(string), string select = default(string), bool? count = default(bool?))
        {
            var uri = new Uri(baseUri, $"Regions");
            uri = uri.GetODataUri(filter:filter, top:top, skip:skip, orderby:orderby, expand:expand, select:select, count:count);

            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, uri);

            OnGetRegions(httpRequestMessage);

            var response = await httpClient.SendAsync(httpRequestMessage);

            return await response.ReadAsync<ODataServiceResult<Region>>();
        }
        partial void OnCreateRegion(HttpRequestMessage requestMessage);
        public async Task<Region> CreateRegion(Region region)
        {
            var uri = new Uri(baseUri, $"Regions");

            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, uri);

            httpRequestMessage.Content = new StringContent(ODataJsonSerializer.Serialize(region), Encoding.UTF8, "application/json");

            OnCreateRegion(httpRequestMessage);

            var response = await httpClient.SendAsync(httpRequestMessage);

            return await response.ReadAsync<Region>();
        }
        partial void OnGetRegionById(HttpRequestMessage requestMessage);
        public async Task<Region> GetRegionById(int regionId)
        {
            var uri = new Uri(baseUri, $"Regions({regionId})");

            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, uri);

            OnGetRegionById(httpRequestMessage);

            var response = await httpClient.SendAsync(httpRequestMessage);

            return await response.ReadAsync<Region>();
        }
        partial void OnDeleteRegion(HttpRequestMessage requestMessage);
        public async Task<HttpResponseMessage> DeleteRegion(int regionId)
        {
            var uri = new Uri(baseUri, $"Regions({regionId})");

            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Delete, uri);

            OnDeleteRegion(httpRequestMessage);
            return await httpClient.SendAsync(httpRequestMessage);
        }
        partial void OnUpdateRegion(HttpRequestMessage requestMessage);
        public async Task<Region> UpdateRegion(int regionId, Region region)
        {
            var uri = new Uri(baseUri, $"Regions({regionId})");

            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Patch, uri);

            httpRequestMessage.Content = new StringContent(ODataJsonSerializer.Serialize(region), Encoding.UTF8, "application/json");

            OnUpdateRegion(httpRequestMessage);

            var response = await httpClient.SendAsync(httpRequestMessage);

            return await response.ReadAsync<Region>();
        }
        partial void OnGetSuppliers(HttpRequestMessage requestMessage);
        public async Task<ODataServiceResult<Supplier>> GetSuppliers(string filter = default(string), int? top = default(int?), int? skip = default(int?), string orderby = default(string), string expand = default(string), string select = default(string), bool? count = default(bool?))
        {
            var uri = new Uri(baseUri, $"Suppliers");
            uri = uri.GetODataUri(filter:filter, top:top, skip:skip, orderby:orderby, expand:expand, select:select, count:count);

            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, uri);

            OnGetSuppliers(httpRequestMessage);

            var response = await httpClient.SendAsync(httpRequestMessage);

            return await response.ReadAsync<ODataServiceResult<Supplier>>();
        }
        partial void OnCreateSupplier(HttpRequestMessage requestMessage);
        public async Task<Supplier> CreateSupplier(Supplier supplier)
        {
            var uri = new Uri(baseUri, $"Suppliers");

            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, uri);

            httpRequestMessage.Content = new StringContent(ODataJsonSerializer.Serialize(supplier), Encoding.UTF8, "application/json");

            OnCreateSupplier(httpRequestMessage);

            var response = await httpClient.SendAsync(httpRequestMessage);

            return await response.ReadAsync<Supplier>();
        }
        partial void OnGetSupplierById(HttpRequestMessage requestMessage);
        public async Task<Supplier> GetSupplierById(int supplierId)
        {
            var uri = new Uri(baseUri, $"Suppliers({supplierId})");

            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, uri);

            OnGetSupplierById(httpRequestMessage);

            var response = await httpClient.SendAsync(httpRequestMessage);

            return await response.ReadAsync<Supplier>();
        }
        partial void OnDeleteSupplier(HttpRequestMessage requestMessage);
        public async Task<HttpResponseMessage> DeleteSupplier(int supplierId)
        {
            var uri = new Uri(baseUri, $"Suppliers({supplierId})");

            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Delete, uri);

            OnDeleteSupplier(httpRequestMessage);
            return await httpClient.SendAsync(httpRequestMessage);
        }
        partial void OnUpdateSupplier(HttpRequestMessage requestMessage);
        public async Task<Supplier> UpdateSupplier(int supplierId, Supplier supplier)
        {
            var uri = new Uri(baseUri, $"Suppliers({supplierId})");

            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Patch, uri);

            httpRequestMessage.Content = new StringContent(ODataJsonSerializer.Serialize(supplier), Encoding.UTF8, "application/json");

            OnUpdateSupplier(httpRequestMessage);

            var response = await httpClient.SendAsync(httpRequestMessage);

            return await response.ReadAsync<Supplier>();
        }
        partial void OnGetTerritories(HttpRequestMessage requestMessage);
        public async Task<ODataServiceResult<Territory>> GetTerritories(string filter = default(string), int? top = default(int?), int? skip = default(int?), string orderby = default(string), string expand = default(string), string select = default(string), bool? count = default(bool?))
        {
            var uri = new Uri(baseUri, $"Territories");
            uri = uri.GetODataUri(filter:filter, top:top, skip:skip, orderby:orderby, expand:expand, select:select, count:count);

            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, uri);

            OnGetTerritories(httpRequestMessage);

            var response = await httpClient.SendAsync(httpRequestMessage);

            return await response.ReadAsync<ODataServiceResult<Territory>>();
        }
        partial void OnCreateTerritory(HttpRequestMessage requestMessage);
        public async Task<Territory> CreateTerritory(Territory territory)
        {
            var uri = new Uri(baseUri, $"Territories");

            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, uri);

            httpRequestMessage.Content = new StringContent(ODataJsonSerializer.Serialize(territory), Encoding.UTF8, "application/json");

            OnCreateTerritory(httpRequestMessage);

            var response = await httpClient.SendAsync(httpRequestMessage);

            return await response.ReadAsync<Territory>();
        }
        partial void OnGetTerritoryById(HttpRequestMessage requestMessage);
        public async Task<Territory> GetTerritoryById(string territoryId)
        {
            var uri = new Uri(baseUri, $"Territories('{HttpUtility.UrlEncode(territoryId)}')");

            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, uri);

            OnGetTerritoryById(httpRequestMessage);

            var response = await httpClient.SendAsync(httpRequestMessage);

            return await response.ReadAsync<Territory>();
        }
        partial void OnDeleteTerritory(HttpRequestMessage requestMessage);
        public async Task<HttpResponseMessage> DeleteTerritory(string territoryId)
        {
            var uri = new Uri(baseUri, $"Territories('{HttpUtility.UrlEncode(territoryId)}')");

            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Delete, uri);

            OnDeleteTerritory(httpRequestMessage);
            return await httpClient.SendAsync(httpRequestMessage);
        }
        partial void OnUpdateTerritory(HttpRequestMessage requestMessage);
        public async Task<Territory> UpdateTerritory(string territoryId, Territory territory)
        {
            var uri = new Uri(baseUri, $"Territories('{HttpUtility.UrlEncode(territoryId)}')");

            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Patch, uri);

            httpRequestMessage.Content = new StringContent(ODataJsonSerializer.Serialize(territory), Encoding.UTF8, "application/json");

            OnUpdateTerritory(httpRequestMessage);

            var response = await httpClient.SendAsync(httpRequestMessage);

            return await response.ReadAsync<Territory>();
        }
    }
}
