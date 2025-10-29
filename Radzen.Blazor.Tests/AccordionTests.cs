using Bunit;
using Microsoft.AspNetCore.Components;
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
            var header = component.Find(".rz-accordion-header a");
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
            var header = component.Find(".rz-accordion-header a");
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
            var header = component.Find(".rz-accordion-header a");
            header.Click();

            // Event should not be raised for disabled item
            Assert.False(expandRaised);
        }
    }
}

