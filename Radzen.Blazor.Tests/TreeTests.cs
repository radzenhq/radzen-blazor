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

        [Fact]
        public void Tree_Renders_TreeRole()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenTree>();

            var container = component.Find("[role=tree]");

            Assert.Equal("0", container.GetAttribute("tabindex"));
        }

        [Fact]
        public void Tree_Renders_AriaLabel()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenTree>(parameters =>
            {
                parameters.Add(p => p.AriaLabel, "Categories");
            });

            var container = component.Find("[role=tree]");

            Assert.Equal("Categories", container.GetAttribute("aria-label"));
        }

        [Fact]
        public void Tree_Renders_AriaLabelledBy()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenTree>(parameters =>
            {
                parameters.Add(p => p.AriaLabelledBy, "tree-heading");
            });

            var container = component.Find("[role=tree]");

            Assert.Equal("tree-heading", container.GetAttribute("aria-labelledby"));
        }

        [Fact]
        public void Tree_Renders_SetSizeAndPosInSet()
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
                        new Product { Name = "Phone" },
                        new Product { Name = "Tablet" }
                    }
                },
                new Category
                {
                    Name = "Books",
                    Products = new List<Product>()
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
                    builder.AddAttribute(3, "Expanded", (object c) => true);
                    builder.AddAttribute(4, "HasChildren", (object c) => c is Category);
                    builder.CloseComponent();

                    builder.OpenComponent<RadzenTreeLevel>(5);
                    builder.AddAttribute(6, "TextProperty", "Name");
                    builder.AddAttribute(7, "HasChildren", (object product) => false);
                    builder.CloseComponent();
                });
            });

            var treeItems = component.FindAll("[role=treeitem]");

            var roots = treeItems.Where(i => i.GetAttribute("aria-level") == "1").ToList();

            Assert.Equal(2, roots.Count);

            Assert.Equal("2", roots[0].GetAttribute("aria-setsize"));
            Assert.Equal("1", roots[0].GetAttribute("aria-posinset"));
            Assert.Equal("2", roots[1].GetAttribute("aria-setsize"));
            Assert.Equal("2", roots[1].GetAttribute("aria-posinset"));

            var children = treeItems.Where(i => i.GetAttribute("aria-level") == "2").ToList();

            Assert.Equal(3, children.Count);

            for (var i = 0; i < children.Count; i++)
            {
                Assert.Equal("3", children[i].GetAttribute("aria-setsize"));
                Assert.Equal((i + 1).ToString(), children[i].GetAttribute("aria-posinset"));
            }
        }

        [Fact]
        public void Tree_Renders_TreeItemRoleAndLevel()
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
                    builder.AddAttribute(3, "Expanded", (object c) => true);
                    builder.AddAttribute(4, "HasChildren", (object c) => c is Category);
                    builder.CloseComponent();

                    builder.OpenComponent<RadzenTreeLevel>(5);
                    builder.AddAttribute(6, "TextProperty", "Name");
                    builder.AddAttribute(7, "HasChildren", (object product) => false);
                    builder.CloseComponent();
                });
            });

            var treeItems = component.FindAll("[role=treeitem]");

            Assert.NotEmpty(treeItems);

            var root = treeItems.First();
            Assert.Equal("1", root.GetAttribute("aria-level"));
            Assert.Equal("true", root.GetAttribute("aria-expanded"));

            var child = treeItems.Last();
            Assert.Equal("2", child.GetAttribute("aria-level"));
        }

        [Fact]
        public void Tree_Renders_GroupRoleOnSubtree()
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
                    builder.AddAttribute(3, "Expanded", (object c) => true);
                    builder.AddAttribute(4, "HasChildren", (object c) => c is Category);
                    builder.CloseComponent();

                    builder.OpenComponent<RadzenTreeLevel>(5);
                    builder.AddAttribute(6, "TextProperty", "Name");
                    builder.AddAttribute(7, "HasChildren", (object product) => false);
                    builder.CloseComponent();
                });
            });

            var groups = component.FindAll("[role=group]");

            Assert.NotEmpty(groups);
        }

        [Fact]
        public void Tree_Renders_AriaSelectedOnItems()
        {
            using var ctx = new TestContext();
            var data = new List<Category>
            {
                new Category { Name = "Electronics" }
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

            var item = component.Find("[role=treeitem]");

            Assert.Equal("false", item.GetAttribute("aria-selected"));
        }

        [Fact]
        public void Tree_Exposes_ActiveDescendant_AsFocusMoves()
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

            var container = component.Find("[role=tree]");
            var items = component.FindAll("[role=treeitem]");

            var firstId = items.First().GetAttribute("id");
            Assert.Equal(firstId, container.GetAttribute("aria-activedescendant"));

            container.KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Code = "ArrowDown" });

            container = component.Find("[role=tree]");
            var secondId = component.FindAll("[role=treeitem]").Last().GetAttribute("id");

            Assert.NotEqual(firstId, secondId);
            Assert.Equal(secondId, container.GetAttribute("aria-activedescendant"));
        }

        [Fact]
        public void Tree_HomeEnd_MoveActiveDescendant()
        {
            using var ctx = new TestContext();
            var data = new List<Category>
            {
                new Category { Name = "Electronics" },
                new Category { Name = "Clothing" },
                new Category { Name = "Books" }
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

            var items = component.FindAll("[role=treeitem]");
            var firstId = items.First().GetAttribute("id");
            var lastId = items.Last().GetAttribute("id");

            var container = component.Find("[role=tree]");
            container.KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Code = "End" });

            container = component.Find("[role=tree]");
            Assert.Equal(lastId, container.GetAttribute("aria-activedescendant"));

            container.KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Code = "Home" });

            container = component.Find("[role=tree]");
            Assert.Equal(firstId, container.GetAttribute("aria-activedescendant"));
        }

        [Fact]
        public void Tree_ArrowRight_ExpandsThenMovesToFirstChild()
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
                    builder.AddAttribute(3, "HasChildren", (object c) => c is Category);
                    builder.CloseComponent();

                    builder.OpenComponent<RadzenTreeLevel>(4);
                    builder.AddAttribute(5, "TextProperty", "Name");
                    builder.AddAttribute(6, "HasChildren", (object product) => false);
                    builder.CloseComponent();
                });
            });

            var container = component.Find("[role=tree]");
            var rootId = component.FindAll("[role=treeitem]").First().GetAttribute("id");
            Assert.Equal(rootId, container.GetAttribute("aria-activedescendant"));

            container.KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Code = "ArrowRight" });

            container = component.Find("[role=tree]");
            Assert.Equal(rootId, container.GetAttribute("aria-activedescendant"));

            container.KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Code = "ArrowRight" });

            container = component.Find("[role=tree]");
            var childId = component.FindAll("[role=treeitem]").Last().GetAttribute("id");

            Assert.NotEqual(rootId, childId);
            Assert.Equal(childId, container.GetAttribute("aria-activedescendant"));
        }

        [Fact]
        public void Tree_ArrowLeft_CollapsesThenMovesToParent()
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
                    builder.AddAttribute(3, "Expanded", (object c) => true);
                    builder.AddAttribute(4, "HasChildren", (object c) => c is Category);
                    builder.CloseComponent();

                    builder.OpenComponent<RadzenTreeLevel>(5);
                    builder.AddAttribute(6, "TextProperty", "Name");
                    builder.AddAttribute(7, "HasChildren", (object product) => false);
                    builder.CloseComponent();
                });
            });

            var container = component.Find("[role=tree]");
            var rootId = component.FindAll("[role=treeitem]").First().GetAttribute("id");

            container.KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Code = "ArrowDown" });

            container = component.Find("[role=tree]");
            var childId = component.FindAll("[role=treeitem]").Last().GetAttribute("id");
            Assert.Equal(childId, container.GetAttribute("aria-activedescendant"));

            container.KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Code = "ArrowLeft" });

            container = component.Find("[role=tree]");
            Assert.Equal(rootId, container.GetAttribute("aria-activedescendant"));
        }

        [Fact]
        public void Tree_TypeAhead_MovesActiveDescendant()
        {
            using var ctx = new TestContext();
            var data = new List<Category>
            {
                new Category { Name = "Electronics" },
                new Category { Name = "Clothing" },
                new Category { Name = "Books" }
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

            var items = component.FindAll("[role=treeitem]");
            var booksId = items.Last().GetAttribute("id");

            var container = component.Find("[role=tree]");
            container.KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "b" });

            container = component.Find("[role=tree]");
            Assert.Equal(booksId, container.GetAttribute("aria-activedescendant"));
        }

        [Fact]
        public void Tree_Enter_TogglesExpandableNode()
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
                    builder.AddAttribute(3, "HasChildren", (object c) => c is Category);
                    builder.CloseComponent();

                    builder.OpenComponent<RadzenTreeLevel>(4);
                    builder.AddAttribute(5, "TextProperty", "Name");
                    builder.AddAttribute(6, "HasChildren", (object product) => false);
                    builder.CloseComponent();
                });
            });

            var root = component.Find("[role=treeitem]");
            Assert.Equal("false", root.GetAttribute("aria-expanded"));

            var container = component.Find("[role=tree]");
            container.KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Code = "Enter" });

            root = component.Find("[role=treeitem]");
            Assert.Equal("true", root.GetAttribute("aria-expanded"));
        }

        [Fact]
        public void Tree_DoesNotRender_AriaSelected_WhenCheckBoxesAllowed()
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

            var item = component.Find("[role=treeitem]");

            Assert.False(item.HasAttribute("aria-selected"));
        }
    }
}

