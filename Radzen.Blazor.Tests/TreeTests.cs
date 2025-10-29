using Bunit;
using Xunit;
using System.Collections.Generic;
using System.Linq;

namespace Radzen.Blazor.Tests
{
    public class TreeTests
    {
        class Category
        {
            public string Name { get; set; }
            public List<Product> Products { get; set; } = new List<Product>();
        }

        class Product
        {
            public string Name { get; set; }
        }

        class Employee
        {
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public List<Employee> Employees { get; set; } = new List<Employee>();
        }

        [Fact]
        public void Tree_Renders_WithClassName()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenTree>();

            Assert.Contains(@"rz-tree", component.Markup);
        }

        [Fact]
        public void Tree_Renders_TreeContainer()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenTree>();

            Assert.Contains("rz-tree-container", component.Markup);
        }

        [Fact]
        public void Tree_Renders_TabIndex()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenTree>();

            Assert.Contains("tabindex=\"0\"", component.Markup);
        }

        [Fact]
        public void Tree_Renders_WithData_SingleLevel()
        {
            using var ctx = new TestContext();
            var data = new List<Category>
            {
                new Category { Name = "Electronics" },
                new Category { Name = "Clothing" }
            };

            var component = ctx.RenderComponent<RadzenTree>(parameters =>
            {
                parameters.Add(p => p.Data, data);
                parameters.Add(p => p.ChildContent, builder =>
                {
                    builder.OpenComponent<RadzenTreeLevel>(0);
                    builder.AddAttribute(1, "TextProperty", "Name");
                    builder.CloseComponent();
                });
            });

            Assert.Contains("Electronics", component.Markup);
            Assert.Contains("Clothing", component.Markup);
        }

        [Fact]
        public void Tree_Renders_WithData_HierarchicalData()
        {
            using var ctx = new TestContext();
            var data = new List<Category>
            {
                new Category 
                { 
                    Name = "Electronics",
                    Products = new List<Product>
                    {
                        new Product { Name = "Laptop" },
                        new Product { Name = "Phone" }
                    }
                }
            };

            var component = ctx.RenderComponent<RadzenTree>(parameters =>
            {
                parameters.Add(p => p.Data, data);
                parameters.Add(p => p.ChildContent, builder =>
                {
                    builder.OpenComponent<RadzenTreeLevel>(0);
                    builder.AddAttribute(1, "TextProperty", "Name");
                    builder.AddAttribute(2, "ChildrenProperty", "Products");
                    builder.CloseComponent();

                    builder.OpenComponent<RadzenTreeLevel>(3);
                    builder.AddAttribute(4, "TextProperty", "Name");
                    builder.AddAttribute(5, "HasChildren", (object product) => false);
                    builder.CloseComponent();
                });
            });

            Assert.Contains("Electronics", component.Markup);
        }

        [Fact]
        public void Tree_Renders_WithData_SelfReferencing()
        {
            using var ctx = new TestContext();
            var data = new List<Employee>
            {
                new Employee 
                { 
                    FirstName = "Nancy", 
                    LastName = "Davolio",
                    Employees = new List<Employee>
                    {
                        new Employee { FirstName = "Andrew", LastName = "Fuller" }
                    }
                }
            };

            var component = ctx.RenderComponent<RadzenTree>(parameters =>
            {
                parameters.Add(p => p.Data, data);
                parameters.Add(p => p.ChildContent, builder =>
                {
                    builder.OpenComponent<RadzenTreeLevel>(0);
                    builder.AddAttribute(1, "TextProperty", "LastName");
                    builder.AddAttribute(2, "ChildrenProperty", "Employees");
                    builder.AddAttribute(3, "HasChildren", (object e) => (e as Employee).Employees.Any());
                    builder.CloseComponent();
                });
            });

            Assert.Contains("Davolio", component.Markup);
        }

        [Fact]
        public void Tree_Renders_WithCheckBoxes()
        {
            using var ctx = new TestContext();
            var data = new List<Category>
            {
                new Category { Name = "Electronics" }
            };

            var component = ctx.RenderComponent<RadzenTree>(parameters =>
            {
                parameters.Add(p => p.AllowCheckBoxes, true);
                parameters.Add(p => p.Data, data);
                parameters.Add(p => p.ChildContent, builder =>
                {
                    builder.OpenComponent<RadzenTreeLevel>(0);
                    builder.AddAttribute(1, "TextProperty", "Name");
                    builder.CloseComponent();
                });
            });

            Assert.Contains("rz-chkbox", component.Markup);
        }

        [Fact]
        public void Tree_Renders_WithExpandableItems()
        {
            using var ctx = new TestContext();
            var data = new List<Category>
            {
                new Category 
                { 
                    Name = "Electronics",
                    Products = new List<Product>
                    {
                        new Product { Name = "Laptop" }
                    }
                }
            };

            var component = ctx.RenderComponent<RadzenTree>(parameters =>
            {
                parameters.Add(p => p.Data, data);
                parameters.Add(p => p.ChildContent, builder =>
                {
                    builder.OpenComponent<RadzenTreeLevel>(0);
                    builder.AddAttribute(1, "TextProperty", "Name");
                    builder.AddAttribute(2, "ChildrenProperty", "Products");
                    builder.CloseComponent();

                    builder.OpenComponent<RadzenTreeLevel>(3);
                    builder.AddAttribute(4, "TextProperty", "Name");
                    builder.CloseComponent();
                });
            });

            // Expandable items should have a toggle icon
            Assert.Contains("rz-tree-toggler", component.Markup);
        }
    }
}

