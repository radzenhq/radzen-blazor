using Bunit;
using Bunit.JSInterop;
using Microsoft.AspNetCore.Components;
using System.Linq;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class AccordionTests
    {
        [Fact]
        public void Accordion_Renders_CssClasses()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenAccordion>();

            Assert.Contains(@"rz-accordion", component.Markup);
        }

        [Fact]
        public void Accordion_Renders_AccordionItems()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenAccordion>(parameters =>
            {
                parameters.Add(p => p.Items, builder =>
                {
                    builder.OpenComponent<RadzenAccordionItem>(0);
                    builder.AddAttribute(1, "Text", "Test Item");
                    builder.AddAttribute(2, "ChildContent", (RenderFragment)(contentBuilder =>
                    {
                        contentBuilder.AddContent(0, "Item Content");
                    }));
                    builder.CloseComponent();
                });
            });

            Assert.Contains("Test Item", component.Markup);
            Assert.Contains("Item Content", component.Markup);
        }

        [Fact]
        public void Accordion_Renders_ItemWithIcon()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenAccordion>(parameters =>
            {
                parameters.Add(p => p.Items, builder =>
                {
                    builder.OpenComponent<RadzenAccordionItem>(0);
                    builder.AddAttribute(1, "Text", "Orders");
                    builder.AddAttribute(2, "Icon", "account_balance_wallet");
                    builder.AddAttribute(3, "ChildContent", (RenderFragment)(contentBuilder =>
                    {
                        contentBuilder.AddContent(0, "Order Details");
                    }));
                    builder.CloseComponent();
                });
            });

            Assert.Contains("account_balance_wallet", component.Markup);
            Assert.Contains("Orders", component.Markup);
        }

        [Fact]
        public void Accordion_SingleExpand_OnlyOneItemExpanded()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenAccordion>(parameters =>
            {
                parameters.Add(p => p.Multiple, false); // Single expand mode
                parameters.Add(p => p.Items, builder =>
                {
                    // Add first item
                    builder.OpenComponent<RadzenAccordionItem>(0);
                    builder.AddAttribute(1, "Text", "Item 1");
                    builder.AddAttribute(2, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Content 1")));
                    builder.CloseComponent();

                    // Add second item
                    builder.OpenComponent<RadzenAccordionItem>(1);
                    builder.AddAttribute(1, "Text", "Item 2");
                    builder.AddAttribute(2, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Content 2")));
                    builder.CloseComponent();
                });
            });

            Assert.False(component.Instance.Multiple);
        }

        [Fact]
        public void Accordion_MultipleExpand_AllowsMultipleItemsExpanded()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenAccordion>(parameters =>
            {
                parameters.Add(p => p.Multiple, true);
            });

            Assert.True(component.Instance.Multiple);
        }

        [Fact]
        public void Accordion_Raises_ExpandEvent()
        {
            using var ctx = new TestContext();
            
            var expandRaised = false;
            int expandedIndex = -1;

            var component = ctx.RenderComponent<RadzenAccordion>(parameters =>
            {
                parameters.Add(p => p.Expand, EventCallback.Factory.Create<int>(this, (index) =>
                {
                    expandRaised = true;
                    expandedIndex = index;
                }));
                parameters.Add(p => p.Items, builder =>
                {
                    builder.OpenComponent<RadzenAccordionItem>(0);
                    builder.AddAttribute(1, "Text", "Test Item");
                    builder.AddAttribute(2, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Content")));
                    builder.CloseComponent();
                });
            });

            // Find and click the accordion header link to expand
            var header = component.Find(".rz-accordion-header button");
            header.Click();

            Assert.True(expandRaised);
            Assert.Equal(0, expandedIndex);
        }

        [Fact]
        public void Accordion_Raises_CollapseEvent()
        {
            using var ctx = new TestContext();
            
            var collapseRaised = false;
            int collapsedIndex = -1;

            var component = ctx.RenderComponent<RadzenAccordion>(parameters =>
            {
                parameters.Add(p => p.Collapse, EventCallback.Factory.Create<int>(this, (index) =>
                {
                    collapseRaised = true;
                    collapsedIndex = index;
                }));
                parameters.Add(p => p.Items, builder =>
                {
                    builder.OpenComponent<RadzenAccordionItem>(0);
                    builder.AddAttribute(1, "Text", "Test Item");
                    builder.AddAttribute(2, "Selected", true); // Start expanded
                    builder.AddAttribute(3, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Content")));
                    builder.CloseComponent();
                });
            });

            // Find and click the accordion header link to collapse
            var header = component.Find(".rz-accordion-header button");
            header.Click();

            Assert.True(collapseRaised);
            Assert.Equal(0, collapsedIndex);
        }

        [Fact]
        public void Accordion_DisabledItem_CannotExpand()
        {
            using var ctx = new TestContext();
            
            var expandRaised = false;

            var component = ctx.RenderComponent<RadzenAccordion>(parameters =>
            {
                parameters.Add(p => p.Expand, EventCallback.Factory.Create<int>(this, (_) => expandRaised = true));
                parameters.Add(p => p.Items, builder =>
                {
                    builder.OpenComponent<RadzenAccordionItem>(0);
                    builder.AddAttribute(1, "Text", "Disabled Item");
                    builder.AddAttribute(2, "Disabled", true);
                    builder.AddAttribute(3, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Content")));
                    builder.CloseComponent();
                });
            });

            // Try to click the disabled item
            var header = component.Find(".rz-accordion-header button");
            header.Click();

            // Event should not be raised for disabled item
            Assert.False(expandRaised);
        }
        [Fact]
        public void Accordion_RenderMode_DefaultsToServer()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenAccordion>();

            Assert.Equal(AccordionRenderMode.Server, component.Instance.RenderMode);
        }

        [Fact]
        public void Accordion_RenderMode_CanBeSetToClient()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenAccordion>(parameters =>
            {
                parameters.Add(p => p.RenderMode, AccordionRenderMode.Client);
                parameters.Add(p => p.Items, builder =>
                {
                    builder.OpenComponent<RadzenAccordionItem>(0);
                    builder.AddAttribute(1, "Text", "Item 1");
                    builder.AddAttribute(2, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Content 1")));
                    builder.CloseComponent();
                });
            });

            Assert.Equal(AccordionRenderMode.Client, component.Instance.RenderMode);
            Assert.Contains("Item 1", component.Markup);
            Assert.Contains("Content 1", component.Markup);
        }

        [Fact]
        public void Accordion_ClientMode_RendersAllItems()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenAccordion>(parameters =>
            {
                parameters.Add(p => p.RenderMode, AccordionRenderMode.Client);
                parameters.Add(p => p.Items, builder =>
                {
                    builder.OpenComponent<RadzenAccordionItem>(0);
                    builder.AddAttribute(1, "Text", "Item 1");
                    builder.AddAttribute(2, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Content 1")));
                    builder.CloseComponent();

                    builder.OpenComponent<RadzenAccordionItem>(3);
                    builder.AddAttribute(4, "Text", "Item 2");
                    builder.AddAttribute(5, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Content 2")));
                    builder.CloseComponent();

                    builder.OpenComponent<RadzenAccordionItem>(6);
                    builder.AddAttribute(7, "Text", "Item 3");
                    builder.AddAttribute(8, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Content 3")));
                    builder.CloseComponent();
                });
            });

            // All items should be rendered in client mode
            Assert.Contains("Content 1", component.Markup);
            Assert.Contains("Content 2", component.Markup);
            Assert.Contains("Content 3", component.Markup);
        }

        [Fact]
        public void Accordion_ClientMode_Raises_ExpandEvent()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var expandRaised = false;
            int expandedIndex = -1;

            var component = ctx.RenderComponent<RadzenAccordion>(parameters =>
            {
                parameters.Add(p => p.RenderMode, AccordionRenderMode.Client);
                parameters.Add(p => p.Expand, EventCallback.Factory.Create<int>(this, (index) =>
                {
                    expandRaised = true;
                    expandedIndex = index;
                }));
                parameters.Add(p => p.Items, builder =>
                {
                    builder.OpenComponent<RadzenAccordionItem>(0);
                    builder.AddAttribute(1, "Text", "Test Item");
                    builder.AddAttribute(2, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Content")));
                    builder.CloseComponent();
                });
            });

            var header = component.Find(".rz-accordion-header button");
            header.Click();

            Assert.True(expandRaised);
            Assert.Equal(0, expandedIndex);
        }

        [Fact]
        public void Accordion_ClientMode_Raises_CollapseEvent()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var collapseRaised = false;
            int collapsedIndex = -1;

            var component = ctx.RenderComponent<RadzenAccordion>(parameters =>
            {
                parameters.Add(p => p.RenderMode, AccordionRenderMode.Client);
                parameters.Add(p => p.Collapse, EventCallback.Factory.Create<int>(this, (index) =>
                {
                    collapseRaised = true;
                    collapsedIndex = index;
                }));
                parameters.Add(p => p.Items, builder =>
                {
                    builder.OpenComponent<RadzenAccordionItem>(0);
                    builder.AddAttribute(1, "Text", "Test Item");
                    builder.AddAttribute(2, "Selected", true);
                    builder.AddAttribute(3, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Content")));
                    builder.CloseComponent();
                });
            });

            var header = component.Find(".rz-accordion-header button");
            header.Click();

            Assert.True(collapseRaised);
            Assert.Equal(0, collapsedIndex);
        }

        [Fact]
        public void Accordion_ClientMode_DisabledItem_CannotExpand()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var expandRaised = false;

            var component = ctx.RenderComponent<RadzenAccordion>(parameters =>
            {
                parameters.Add(p => p.RenderMode, AccordionRenderMode.Client);
                parameters.Add(p => p.Expand, EventCallback.Factory.Create<int>(this, (_) => expandRaised = true));
                parameters.Add(p => p.Items, builder =>
                {
                    builder.OpenComponent<RadzenAccordionItem>(0);
                    builder.AddAttribute(1, "Text", "Disabled Item");
                    builder.AddAttribute(2, "Disabled", true);
                    builder.AddAttribute(3, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Content")));
                    builder.CloseComponent();
                });
            });

            var header = component.Find(".rz-accordion-header button");
            header.Click();

            Assert.False(expandRaised);
        }

        [Fact]
        public void Accordion_ClientMode_SingleExpand_CollapsesOthers()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var collapseRaised = false;
            int collapsedIndex = -1;

            var component = ctx.RenderComponent<RadzenAccordion>(parameters =>
            {
                parameters.Add(p => p.RenderMode, AccordionRenderMode.Client);
                parameters.Add(p => p.Multiple, false);
                parameters.Add(p => p.Collapse, EventCallback.Factory.Create<int>(this, (index) =>
                {
                    collapseRaised = true;
                    collapsedIndex = index;
                }));
                parameters.Add(p => p.Items, builder =>
                {
                    builder.OpenComponent<RadzenAccordionItem>(0);
                    builder.AddAttribute(1, "Text", "Item 1");
                    builder.AddAttribute(2, "Selected", true);
                    builder.AddAttribute(3, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Content 1")));
                    builder.CloseComponent();

                    builder.OpenComponent<RadzenAccordionItem>(4);
                    builder.AddAttribute(5, "Text", "Item 2");
                    builder.AddAttribute(6, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Content 2")));
                    builder.CloseComponent();
                });
            });

            // Click Item 2 to expand it — Item 1 should collapse
            var headers = component.FindAll(".rz-accordion-header button");
            headers[1].Click();

            Assert.True(collapseRaised);
            Assert.Equal(0, collapsedIndex);
        }

        [Fact]
        public void Accordion_ClientMode_Multiple_DoesNotCollapseOthers()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var collapsedIndices = new System.Collections.Generic.List<int>();

            var component = ctx.RenderComponent<RadzenAccordion>(parameters =>
            {
                parameters.Add(p => p.RenderMode, AccordionRenderMode.Client);
                parameters.Add(p => p.Multiple, true);
                parameters.Add(p => p.Collapse, EventCallback.Factory.Create<int>(this, (index) =>
                {
                    collapsedIndices.Add(index);
                }));
                parameters.Add(p => p.Items, builder =>
                {
                    builder.OpenComponent<RadzenAccordionItem>(0);
                    builder.AddAttribute(1, "Text", "Item 1");
                    builder.AddAttribute(2, "Selected", true);
                    builder.AddAttribute(3, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Content 1")));
                    builder.CloseComponent();

                    builder.OpenComponent<RadzenAccordionItem>(4);
                    builder.AddAttribute(5, "Text", "Item 2");
                    builder.AddAttribute(6, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Content 2")));
                    builder.CloseComponent();
                });
            });

            // Click Item 2 to expand it — Item 1 should NOT collapse
            var headers = component.FindAll(".rz-accordion-header button");
            headers[1].Click();

            // No collapse events should fire (Item 1 stays expanded)
            Assert.Empty(collapsedIndices);
        }

        [Fact]
        public void Accordion_ClientMode_SelectedIndexChanged_Fires()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            int selectedIndex = -1;

            var component = ctx.RenderComponent<RadzenAccordion>(parameters =>
            {
                parameters.Add(p => p.RenderMode, AccordionRenderMode.Client);
                parameters.Add(p => p.SelectedIndexChanged, EventCallback.Factory.Create<int>(this, (index) =>
                {
                    selectedIndex = index;
                }));
                parameters.Add(p => p.Items, builder =>
                {
                    builder.OpenComponent<RadzenAccordionItem>(0);
                    builder.AddAttribute(1, "Text", "Item 1");
                    builder.AddAttribute(2, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Content 1")));
                    builder.CloseComponent();

                    builder.OpenComponent<RadzenAccordionItem>(3);
                    builder.AddAttribute(4, "Text", "Item 2");
                    builder.AddAttribute(5, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Content 2")));
                    builder.CloseComponent();
                });
            });

            var headers = component.FindAll(".rz-accordion-header button");
            headers[1].Click();

            Assert.Equal(1, selectedIndex);
        }
    }
}

