using System;
using System.Collections.Generic;
using System.Linq;

namespace RadzenBlazorDemos
{
    public class KeyboardShortcut
    {
        public string Key { get; set; }
        public string Action { get; set; }
    }
    public class ExampleService
    {
        Example[] allExamples = new[] {
        new Example
        {
            Name = "Overview",
            Path = "/",
            Icon = "\ue88a"
        },
        new Example
        {
            Name = "Get Started",
            Path = "/get-started",
            Title = "Get Started | Free UI Components by Radzen",
            Description = "How to get started with the Radzen Blazor Components library.",
            Icon = "\ue1c4"
        },
        new Example
        {
            Name = "AI",
            Path = "/ai",
            Title = "AI and Radzen Blazor",
            Description = "Learn now how to integrate AI with the Radzen Blazor Components library.",
            Icon = "\uefac",
            Tags = new [] { "chat", "ai", "conversation", "message", "streaming", "mcp", "nuget" },
            New = true
        },
        new Example
        {
            Name = "Support",
            Path = "/support",
            Title = "Support | Free UI Components by Radzen",
            Description = "How to get support for the Radzen Blazor Components library.",
            Icon = "\ue0c6"
        },
        new Example
        {
            Toc = [ new () { Text = "Applying guidelines", Anchor = "#applying-guidelines" }, new () { Text = "WCAG 2.2", Anchor = "#wcag" }, new () { Text = "WCAG compliant theme colors (AA level of conformance)", Anchor = "#wcag-colors" }, new () { Text = "ARIA attributes", Anchor = "#wai-aria" }, new () { Text = "Semantic HTML", Anchor = "#semantic-html" }, new () { Text = "Screen reader compatibility", Anchor = "#screen-readers" }, new () { Text = "Responsive design", Anchor = "#responsive-design" }, new () { Text = "Keyboard compatibility", Anchor = "#keyboard-compatibility" }, new () { Text = "Accessibility Conformance Report", Anchor = "#acr" } ],
            Name = "Accessibility",
            Path = "/accessibility",
            Title = "Blazor Accessibility | Free UI Components by Radzen",
            Description = "The accessible Radzen Blazor Components library covers highest levels of web accessibility guidelines and recommendations, making you Blazor app compliant with WAI-ARIA, WCAG 2.2, section 508, and keyboard compatibility standards.",
            Icon = "\ue92c",
            Tags = new[] { "keyboard", "accessibility", "standard", "508", "wai-aria", "wcag", "shortcut"}
        },

        new Example
        {

            Toc = [ new () { Text = "Customize themes in Radzen Blazor Studio", Anchor = "#text-tag-name" } ],
            Name = "UI Fundamentals",
            Icon = "\ue749",
            Children = new [] {
                new Example
                {
                    Name = "Themes",
                    Path = "themes",
                    Updated = true,
                    Title = "Blazor Themes | Free UI Components by Radzen",
                    Description = "The Radzen Blazor Components package features an array of both free and premium themes, allowing you to choose the style that best suits your project's requirements.",
                    Icon = "\ue40a",
                    Tags = new[] { "theme", "color", "background", "border", "utility", "css", "var"}
                },
                new Example
                {
                    Name = "ThemeService",
                    Path = "theme-service",
                    Title = "ThemeService",
                    Description = "The ThemeService allows to change the theme of the application at runtime.",
                    Icon = "\ue3ae",
                    Tags = ["theme", "service", "change", "runtime", "rtl", "right to left", "direction", "wcag", "accessibility"]
                },
                new Example
                {
                    Toc = [ new () { Text = "Switch between light and dark mode", Anchor = "#light-dark-mode" }, new () { Text = "Keyboard Navigation", Anchor = "#keyboard-navigation" } ],
                    Name = "AppearanceToggle",
                    Path = "appearance-toggle",
                    Title = "Blazor Themes | Free UI Components by Radzen",
                    Description = "The AppearanceToggle button allows you to switch between two predefined themes, most commonly light and dark.",
                    Icon = "\ueb37",
                    Tags = new[] { "theme", "light", "dark", "mode", "appearance", "toggle", "switch"}
                },
                new Example
                {
                    Toc = [ new () { Text = "Theme Colors", Anchor = "#theme-colors" }, new () { Text = "Utility CSS Classes", Anchor = "#utility-css-classes" } ],
                    Name = "Colors",
                    Path = "colors",
                    Title = "Blazor Color Utilities | Free UI Components by Radzen",
                    Description = "List of colors and utility CSS classes available in Radzen Blazor Components library.",
                    Icon = "\ue997",
                    Tags = new[] { "color", "background", "border", "utility", "css", "var"}
                },
                new Example
                {
                    Toc = [ new () { Text = "Text Style", Anchor = "#text-style" }, new () { Text = "Text Style and Tag Name", Anchor = "#text-tag-name" }, new () { Text = "Display headings", Anchor = "#text-display-headings" }, new () { Text = "Text Align", Anchor = "#text-align" }, new () { Text = "Text Functional Colors", Anchor = "#text-color" }, new () { Text = "Text Transform", Anchor = "#text-transform" }, new () { Text = "Text Wrap", Anchor = "#text-wrap" } ],
                    Name = "Typography",
                    Path = "typography",
                    Title = "Blazor Text Component | Free UI Components by Radzen",
                    Description = "Use the RadzenText component to format text in your applications. The TextStyle property applies a predefined text style such as H1, H2, etc.",
                    Icon = "\ue264",
                    Tags = new [] { "typo", "typography", "text", "paragraph", "header", "heading", "caption", "overline", "content" }
                },
                new Example
                {
                    Toc = [ new () { Text = "Material Icons", Anchor = "#material-icons" }, new () { Text = "Icon color", Anchor = "#icon-color" }, new () { Text = "Filled icons", Anchor = "#filled-icons" }, new () { Text = "Syled icons", Anchor = "#styled-icons" }, new () { Text = "Using RadzenIcon with other icon fonts", Anchor = "#icons-width-other-fonts" } ],
                    Name = "Icons",
                    Path = "icon",
                    Updated = true,
                    Title = "Blazor Icon Component | Free UI Components by Radzen",
                    Description = "Demonstration and configuration of the Radzen Blazor Icon component.",
                    Icon = "\ue148",
                    Tags = new [] { "icon", "content" }
                },
                new Example
                {
                    Toc = [ new () { Text = "Border radius", Anchor = "#border-radius" }, new () { Text = "Add or remove borders arbitrarily", Anchor = "#add-remove-css-classes" }, new () { Text = "Border color utility CSS classes", Anchor = "#color-css-classes" }, new () { Text = "Border with color utility CSS classes", Anchor = "#utility-css-classes" }, new () { Text = "Set border width via CSS variable", Anchor = "#border-width" }, new () { Text = "Borders with CSS variables", Anchor = "#css-variables" } ],
                    Name = "Borders",
                    Path = "borders",
                    Title = "Blazor Border Utilities | Free UI Components by Radzen",
                    Description = "Border styles and utility CSS classes for borders available in Radzen Blazor Components library.",
                    Icon = "\ue3c6",
                    Tags = new [] { "border", "utility", "css", "var"}
                },
                new Example
                {
                    Toc = [ new () { Text = "Breakpoints", Anchor = "#breakpoints" }, new () { Text = "Usage of Breakpoints", Anchor = "#usage" } ],
                    Name = "Breakpoints",
                    Title = "Blazor Responsive Breakpoints | Free UI Components by Radzen",
                    Description = "Responsive breakpoints are used to adjust the layout based on the screen size of the device in use.",
                    Path = "breakpoints",
                    Icon = "\ue1b1",
                    Tags = new [] { "breakpoints", "spacing", "margin", "padding", "gutter", "gap", "utility", "css", "responsive", "layout"}
                },
                new Example
                {
                    Toc = [ new () { Text = "Responsive display", Anchor = "#responsive-spacing" } ],
                    Name = "Display",
                    Title = "Blazor Display Utilities | Free UI Components by Radzen",
                    Description = "Display styles and utility CSS classes available in Radzen Blazor Components library.",
                    Path = "display",
                    Icon = "\uf023",
                    Tags = new [] { "display", "hide", "show", "flex", "block", "inline", "utility", "css", "var"}
                },
                new Example
                {
                    Toc = [ new () { Text = "Responsive overflow", Anchor = "#responsive-spacing" } ],
                    Name = "Overflow",
                    Title = "Blazor Content Overflow Utilities | Free UI Components by Radzen",
                    Description = "Overflow styles and utility CSS classes available in Radzen Blazor Components library.",
                    Path = "overflow",
                    Icon = "\uf829",
                    Tags = new [] { "overflow", "content", "width", "height", "size", "wrap", "hide", "hidden", "visible", "utility", "css", "var"}
                },
                new Example
                {
                    Toc = [ new () { Text = "Ripple RadzenButton", Anchor = "#ripple-button" }, new () { Text = "Ripple RadzenLink", Anchor = "#ripple-link" }, new () { Text = "Ripple HTML div", Anchor = "#ripple-div" } ],
                    Name = "Ripple",
                    Title = "Blazor Ripple Effect | Free UI Components by Radzen",
                    Description = "See how to apply the ripple effect to various UI elements.",
                    Path = "ripple",
                    Icon = "\ue762",
                    Tags = new [] { "ripple", "utility", "css", "var"}
                },
                new Example
                {
                    Toc = [ new () { Text = "Utility CSS classes", Anchor = "#shadow-css-classes" }, new () { Text = "Custom CSS properties (CSS Variables)", Anchor = "#shadow-css-variables" } ],
                    Name = "Shadows",
                    Path = "shadows",
                    Title = "Blazor Shadow Utilities | Free UI Components by Radzen",
                    Description = "Shadow styles and utility CSS classes for shadows available in Radzen Blazor Components library.",
                    Icon = "\ue9df",
                    Tags = new [] { "shadow", "utility", "css", "var"}
                },
                new Example
                {
                    Toc = [ new () { Text = "Width percentage CSS classes", Anchor = "#width-percentage-css-classes" }, new () { Text = "Width keyword CSS classes", Anchor = "#width-keyword-css-classes" }, new () { Text = "Width viewport CSS classes", Anchor = "#width-viewport-css-classes" }, new () { Text = "Max-width and min-width CSS classes", Anchor = "#border-radius" }, new () { Text = "Height percentage CSS classes", Anchor = "#height-percentage-css-classes" }, new () { Text = "Height viewport CSS classes", Anchor = "#height-viewport-css-classes" }, new () { Text = "Max-height and min-height CSS classes", Anchor = "#border-radius" }, new () { Text = "Responsive sizing", Anchor = "#responsive-spacing" } ],
                    Name = "Sizing",
                    Title = "Blazor Sizing Utilities | Free UI Components by Radzen",
                    Description = "Sizing styles and utility CSS classes for width and height available in Radzen Blazor Components library.",
                    Path = "sizing",
                    Icon = "\uf730",
                    Tags = new [] { "sizing", "width", "height", "size", "max", "min", "utility", "css", "var"}
                },
                new Example
                {
                    Toc = [ new () { Text = "Basic Usage", Anchor = "#basic-usage" }, new () { Text = "Text Size", Anchor = "#text-size" }, new () { Text = "Animations", Anchor = "#animations" }, new () { Text = "Complex Example", Anchor = "#complex-example" }, new () { Text = "DataGrid Loading", Anchor = "#datagrid-loading" } ],
                    Name = "Skeleton",
                    Title = "Blazor Skeleton Component | Free UI Components by Radzen",
                    Description = "RadzenSkeleton component displays loading placeholders with various shapes and animations.",
                    Path = "skeleton",
                    Icon = "\uf486",
                    Tags = new [] { "skeleton", "load", "loading", "placeholder", "animation", "wave", "pulse", "text", "circular", "rectangular", "rounded" },
                    New = true
                },
                new Example
                {
                    Toc = [ new () { Text = "Margin CSS classes", Anchor = "#margin-css-classes" }, new () { Text = "Padding CSS classes", Anchor = "#padding-css-classes" }, new () { Text = "Sizes", Anchor = "#sizes" }, new () { Text = "Responsive spacing", Anchor = "#responsive-spacing" } ],
                    Name = "Spacing",
                    Title = "Blazor Spacing Utilities | Free UI Components by Radzen",
                    Description = "Spacing styles and utility CSS classes for margin and padding available in Radzen Blazor Components library.",
                    Path = "spacing",
                    Icon = "\uf773",
                    Tags = new [] { "spacing", "margin", "padding", "gutter", "gap", "utility", "css", "var"}
                }
            }
        },
        new Example
        {
            Toc = [ new () { Text = "Get and set the text", Anchor = "#text" }, new () { Text = "Markdown with Blazor components inside", Anchor = "#blazor" } ],
            Name = "Markdown",
            Icon = "\uf552",
            Path = "markdown",
            Description = "Use Radzen Blazor Markdown component to render markdown content.",
            Tags = new[] { "markdown", "text", "content" },
            New = true
        },
        new Example
        {
            Toc = [ new () { Text = "Centered CTA", Anchor = "#centered-cta" }, new () { Text = "Left-aligned CTA", Anchor = "#left-aligned-cta" }, new () { Text = "Justified CTA", Anchor = "#left-aligned-cta" }, new () { Text = "Image to the left", Anchor = "#image-to-the-left" }, new () { Text = "Image to the right", Anchor = "#image-to-the-right" } ],
            Name = "UI Blocks",
            Pro = true,
            New = true,
            Title = "Blazor UI Blocks",
            Description = "Ready to use UI building blocks and templates",
            Icon = "\uf51d",
            Children = new[] {
                new Example
                {
                    Name = "Call-to-Action",
                    Title = "Blazor Call-to-Action | UI Blocks by Radzen",
                    Icon = "\ue06c",
                    Description = "Examples of CTA UI Blocks",
                    Path = "ui-blocks-cta",
                    Tags = new [] { "cta", "call-to-action", "call", "action", "button" }
                },
                new Example
                {
                    Toc = [ new () { Text = "Simple stats", Anchor = "#simple-stats" }, new () { Text = "Simple stats with icon", Anchor = "#simple-stats-with-icon" }, new () { Text = "Stats with trends", Anchor = "#stats-with-trends" }, new () { Text = "Stats with trends to the right", Anchor = "#stats-with-trends-to-the-right" }, new () { Text = "Stats with square icon", Anchor = "#stats-with-square-icon" } ],
                    Name = "Cards",
                    Title = "Blazor Cards | UI Blocks by Radzen",
                    Icon = "\ue991",
                    Description = "Examples of Card Blocks",
                    Path = "ui-blocks-cards",
                    Tags = new [] { "card", "stats", "products" }
                },
                new Example
                {
                    Toc = [ new () { Text = "FAQ in 2 columns", Anchor = "#faq-in-2-columns" }, new () { Text = "FAQ to the right", Anchor = "#faq-to-the-right" }, new () { Text = "FAQ Accordion to the right", Anchor = "#faq-accordion-to-the-right" }, new () { Text = "FAQ in centered Accordion", Anchor = "#faq-in-centered-accordion" } ],
                    Name = "FAQ",
                    New = true,
                    Title = "Blazor FAQ - Frequently Asked Questions | UI Blocks by Radzen",
                    Icon = "\uf04c",
                    Description = "Examples of FAQ Blocks",
                    Path = "ui-blocks-faq",
                    Tags = new [] { "faq", "question", "answer" }
                },
                new Example
                {
                    Toc = [ new () { Text = "Features with small icons", Anchor = "#features-with-small-icons" }, new () { Text = "Features with large icons on top", Anchor = "#features-with-large-icons-on-top" }, new () { Text = "Centered features", Anchor = "#centered-features" } ],
                    Name = "Features",
                    Title = "Blazor Features | UI Blocks by Radzen",
                    Icon = "\ue031",
                    Description = "Examples of Features Blocks",
                    Path = "ui-blocks-features",
                    Tags = new [] { "feature", "list" }
                },
                new Example
                {
                    Toc = [ new () { Text = "Footer with a sitemap", Anchor = "#footer-with-sitemap" }, new () { Text = "Centered Footer", Anchor = "#centered-footer" }, new () { Text = "Simple Footer", Anchor = "#simple-footer" } ],
                    Name = "Footers",
                    Title = "Blazor Footer | UI Blocks by Radzen",
                    Icon = "\uf7e6",
                    Description = "Preconfigured Footer UI Blocks",
                    Path = "ui-blocks-footers",
                    Tags = new [] { "footer" }
                },
                new Example
                {
                    Toc = [ new () { Text = "Centered Logos", Anchor = "#centered-logos" }, new () { Text = "Logos to the right", Anchor = "#logos-to-the-right" } ],
                    Name = "Logo Clouds",
                    New = true,
                    Title = "Blazor Logo Clouds | UI Blocks by Radzen",
                    Icon = "\ue574",
                    Description = "Examples of Customers Logo Blocks",
                    Path = "ui-blocks-logos",
                    Tags = new [] { "logo", "customer", "logos" }
                },
                new Example
                {
                    Toc = [ new () { Text = "Centered layout", Anchor = "#centered-layout" }, new () { Text = "Left-aligned with image", Anchor = "#left-aligned-with-image" } ],
                    Name = "Newsletter",
                    New = true,
                    Title = "Blazor Newsletter | UI Blocks by Radzen",
                    Icon = "\uf18c",
                    Description = "Examples of Newsletter subscription form UI Blocks",
                    Path = "ui-blocks-newsletter",
                    Tags = new [] { "newsletter", "subscribe", "subscription" }
                },
                new Example
                {
                    Toc = [ new () { Text = "Title with actions", Anchor = "#title-with-actions" }, new () { Text = "Title with breadcrumbs", Anchor = "#title-with-breadcrumbs" }, new () { Text = "Title with breadcrumbs and actions", Anchor = "#title-with-breadcrumbs-and-actions" } ],
                    Name = "Page Headings",
                    Title = "Blazor Page Heading | UI Blocks by Radzen",
                    Icon = "\ue9ea",
                    Description = "Preconfigured Page Heading UI Blocks",
                    Path = "ui-blocks-page-headings",
                    Tags = new [] { "headings", "heading", "page", "title" }
                },
                new Example
                {
                    Toc = [ new () { Text = "Pricing Cards", Anchor = "#pricing-cards" }, new () { Text = "Basic pricing", Anchor = "#basic-pricing" } ],
                    Name = "Pricing",
                    New = true,
                    Title = "Blazor Pricing | UI Blocks by Radzen",
                    Icon = "\uf05b",
                    Description = "Examples of Pricing UI Blocks",
                    Path = "ui-blocks-pricing",
                    Tags = new [] { "pricing", "table" }
                },
                new Example
                {
                    Toc = [ new () { Text = "Quotes in Carousel", Anchor = "#quotes-in-carousel" }, new () { Text = "Quotes on a row", Anchor = "#quotes-on-a-row" }, new () { Text = "Quotes in Cards", Anchor = "#quotes-in-cards" }, new () { Text = "Quotes in a CardGroup", Anchor = "#quotes-in-cardgroup" }, new () { Text = "Centered quotes", Anchor = "#centered-quotes" }, new () { Text = "Single quote", Anchor = "#single-quote" } ],
                    Name = "Testimonials",
                    New = true,
                    Title = "Blazor Customer Testimonials | UI Blocks by Radzen",
                    Icon = "\uf054",
                    Description = "Examples of Customer Testimonials UI Blocks",
                    Path = "ui-blocks-testimonials",
                    Tags = new [] { "testimonial", "quote", "customer" }
                }
            }
        },

        new Example
        {
            Name = "App Templates",
            New = true,
            Title = "Blazor App Templates",
            Description = "Ready to use Blazor application and website templates",
            Icon = "\ue5c3",
            Children = new[] {
                new Example
                {
                    Name = "Issues Dashboard",
                    Path = "/dashboard",
                    Title = "Sample Dashboard | Free UI Components by Radzen",
                    Description = "Rich dashboard created with the Radzen Blazor Components library.",
                    Icon = "\ue868"
                },
                new Example
                {
                    Name = "Healthcare",
                    New = true,
                    Pro = true,
                    Title = "Healthcare Blazor Website | Premium App Templates by Radzen",
                    Icon = "\ueb4c",
                    Description = "A modern, responsive healthcare blazor website template.",
                    Path = "templates-healthcare",
                    Tags = new [] { "template", "health", "healthcare", "website", "app", "application", "page", "landing" }
                },
                new Example
                {
                    Name = "Real Estate",
                    New = true,
                    Pro = true,
                    Title = "Real Estate Blazor Website | Premium App Templates by Radzen",
                    Icon = "\ue73a",
                    Description = "A real estate website template designed to showcase listings, build trust, and convert leads. Featuring clean layout and responsive design.",
                    Path = "templates-realestate",
                    Tags = new [] { "template", "real estate", "apartment", "home", "website", "app", "application", "page", "landing" }
                },
                new Example
                {
                    Name = "Repair Workshop",
                    New = true,
                    Pro = true,
                    Title = "Repair Workshop Blazor Website | Premium App Templates by Radzen",
                    Icon = "\uf56c",
                    Description = "A modern, responsive auto repair blazor website template.",
                    Path = "templates-repairshop",
                    Tags = new [] { "template", "repair", "workshop", "website", "app", "application", "page", "landing" }
                }
            }
        },

        new Example
        {

            Toc = [ new () { Text = "Keyboard Navigation", Anchor = "#keyboard-navigation" } ],
            Name = "DataGrid",
            Updated = true,
            Icon = "\uf191",
            Children = new [] {
                new Example
                {
                    Name = "Data-binding",
                    Icon = "\ue3ec",
                    Children = new [] {
                        new Example
                        {
                            Name = "IQueryable",
                            Title = "Blazor DataGrid Component with code-less paging, sorting and filtering of IQueryable data sources | Free UI Components by Radzen",
                            Description = "Use RadzenDataGrid to display tabular data with ease. Perform paging, sorting and filtering through Entity Framework without extra code.",
                            Path = "datagrid",
                            Tags = new [] { "datatable", "datagridview", "dataview", "grid", "table" }
                        },
                        new Example
                        {
                            Name = "LoadData event",
                            Path = "datagrid-loaddata",
                            Title = "Blazor DataGrid Component - LoadData Event | Free UI Components by Radzen",
                            Description = "Blazor Data Grid custom data-binding via the LoadData event.",
                            Tags = new [] { "datagrid", "bind", "load", "data", "loaddata", "custom" }
                        },
                        new Example
                        {
                            Name = "OData service",
                            Path = "datagrid-odata",
                            Title = "Blazor DataGrid Component - OData Service | Free UI Components by Radzen",
                            Description = "Blazor Data Grid supports data-binding to OData.",
                            Tags = new [] { "datagrid", "bind", "load", "data", "loaddata", "odata", "service", "rest" }
                        },
                        new Example
                        {
                            Name = "Dynamic data",
                            Path = "datagrid-dynamic",
                            Title = "Blazor DataGrid Component - Dynamic Data | Free UI Components by Radzen",
                            Description = "Blazor Data Grid supports dynamic data sources.",
                            Tags = new [] { "datagrid", "bind", "load", "data", "loaddata", "dynamic" }
                        },
                        new Example
                        {
                            Name = "DataTable data",
                            Path = "datagrid-datatable",
                            Title = "Blazor DataGrid Component - DataTable Data | Free UI Components by Radzen",
                            Description = "Blazor Data Grid supports DataTable sources.",
                            Tags = new [] { "datagrid", "bind", "load", "data", "loaddata", "datatable" }
                        },
                        new Example
                        {
                            Name = "Real-time data",
                            Path = "datagrid-realtime",
                            Title = "Blazor DataGrid Component - Real-time Data | Free UI Components by Radzen",
                            Description = "Blazor Data Grid with real-time data sources.",
                            Tags = new [] { "datagrid", "bind", "load", "data", "loaddata", "real-time" }
                        },
                        new Example
                        {
                            Name = "Crosstab data",
                            Path = "datagrid-crosstab",
                            Title = "Blazor DataGrid Component - Crosstab Data | Free UI Components by Radzen",
                            Description = "Blazor Data Grid supports crosstab data sources.",
                            Tags = new [] { "datagrid", "bind", "load", "data", "loaddata", "crosstab", "rows", "columns" }
                        },
                        new Example
                        {
                            Name = "Performance",
                            Path = "datagrid-performance",
                            Title = "Blazor DataGrid Component - Performance | Free UI Components by Radzen",
                            Description = "Blazor Data Grid bound to a large collection of data",
                            Tags = new [] { "datagrid", "bind", "performance", "data", "large" }
                        },
                    }
                },
                new Example
                {
                    Name = "Virtualization",
                    Icon = "\ue871",
                    Children = new []
                    {
                        new Example
                        {
                            Name = "IQueryable support",
                            Path = "datagrid-virtualization",
                            Title = "Blazor DataGrid Component - IQueryable Virtualization | Free UI Components by Radzen",
                            Description = "Virtualization allows you to render large amounts of data on demand. The RadzenDataGrid component uses Entity Framework to query the visible data.",
                            Tags = new [] { "datagrid", "bind", "load", "data", "virtualization", "ondemand" }
                        },
                        new Example
                        {
                            Name = "LoadData support",
                            Path = "datagrid-virtualization-loaddata",
                            Title = "Blazor DataGrid Component - Custom Virtualization | Free UI Components by Radzen",
                            Description = "RadzenDataGrid supports virtualization with custom data-binding scenarios. Handle the LoadData event as usual.",
                            Tags = new [] { "datagrid", "bind", "load", "data", "loaddata", "virtualization", "ondemand" }
                        },
                    }
                },
                new Example
                {
                    Name = "Columns",
                    Icon = "\ue336",
                    Updated = true,
                    Children = new []
                    {
                        new Example
                        {
                            Name = "Template",
                            Path = "datagrid-column-template",
                            Title = "Blazor DataGrid Component - Column Template | Free UI Components by Radzen",
                            Description = "Blazor Data Grid custom appearance via column templates. The Template allows you to customize the way data is displayed.",
                            Tags = new [] { "column", "template", "grid", "datagrid", "table"}
                        },
                        new Example
                        {
                            Name = "Resizing",
                            Path = "datagrid-column-resizing",
                            Title = "Blazor DataGrid Component - Column Resizing | Free UI Components by Radzen",
                            Description = "Enable column resizing in RadzenDataGrid by setting the AllowColumnResizing property to true.",
                            Tags = new [] { "column", "resizing", "grid", "datagrid", "table"}
                        },
                        new Example
                        {
                            Name = "Column Picker",
                            Path = "datagrid-column-picker",
                            Title = "Blazor DataGrid Component - Column Picker | Free UI Components by Radzen",
                            Description = "Enable column picker in RadzenDataGrid by setting the AllowColumnPicking property to true.",
                            Tags = new [] { "datagrid", "column", "picker", "chooser" }
                        },
                        new Example
                        {
                            Updated = true,
                            Name = "Reorder",
                            Path = "datagrid-column-reorder",
                            Title = "Blazor DataGrid Component - Column Reorder | Free UI Components by Radzen",
                            Description = "Enable column reorder in RadzenDataGrid by setting the AllowColumnReorder property to true. Define column initial order using column OrderIndex property.",
                            Tags = new [] { "column", "reorder", "grid", "datagrid", "table"}
                        },
                        new Example
                        {
                            Name = "Footer Totals",
                            Path = "datagrid-footer-totals",
                            Title = "Blazor DataGrid Component - Footer Totals | Free UI Components by Radzen",
                            Description = "The FooterTemplate column property allows you to display aggregated data in the column footer.",
                            Tags = new [] { "summary", "total", "aggregate", "datagrid", "table", "dataview" }
                        },
                        new Example
                        {
                            Name = "Frozen Columns",
                            Path = "datagrid-frozen-columns",
                            Title = "Blazor DataGrid Component - Frozen Columns | Free UI Components by Radzen",
                            Description = "Lock columns in RadzenDataGrid to prevent them from scrolling out of view via the Frozen property.",
                            Tags = new [] { "datagrid", "column", "frozen", "locked" }
                        },
                        new Example
                        {
                            Name = "Composite Columns",
                            Path = "datagrid-composite-columns",
                            Title = "Blazor DataGrid Component - Composite Columns | Free UI Components by Radzen",
                            Description = "Use RadzenDataGridColumn Columns property to define child columns.",
                            Tags = new [] { "datagrid", "column", "composite", "merged", "complex" }
                        },
                        new Example
                        {
                            Name = "Conditional Columns",
                            New = true,
                            Path = "datagrid-conditional-columns-render",
                            Title = "Blazor DataGrid Component - Conditional Columns Render | Free UI Components by Radzen",
                            Description = "Use RadzenDataGridColumn Columns property to define child columns conditionally.",
                            Tags = new [] { "datagrid", "column", "conditional", "render", "complex" }
                        }
                    }
                },
                new Example
                {
                    Updated = true,
                    Name = "Filtering",
                    Icon = "\uef4f",
                    Children = new []
                    {
                        new Example
                        {
                            Name = "Simple Mode",
                            Path = "datagrid-simple-filter",
                            Title = "Blazor DataGrid Component - Simple Filter Mode | Free UI Components by Radzen",
                            Description = "RadzenDataGrid simple mode filtering.",
                            Tags = new [] { "filter", "simple", "grid", "datagrid", "table"}
                        },
                        new Example
                        {
                            Name = "Simple with menu",
                            Path = "datagrid-simple-filter-menu",
                            Title = "Blazor DataGrid Component - Simple Filter Mode with Menu | Free UI Components by Radzen",
                            Description = "RadzenDataGrid simple mode filtering with Menu.",
                            Tags = new [] { "filter", "simple", "grid", "datagrid", "table", "menu" }
                        },
                        new Example
                        {
                            Name = "Advanced Mode",
                            Path = "datagrid-advanced-filter",
                            Title = "Blazor DataGrid Component - Advanced Filter Mode | Free UI Components by Radzen",
                            Description = "RadzenDataGrid advanced mode filtering.",
                            Tags = new [] { "filter", "advanced", "grid", "datagrid", "table"}
                        },
                        new Example
                        {
                            New = true,
                            Name = "CheckBoxList (Excel like)",
                            Path = "datagrid-checkboxlist-filter",
                            Title = "Blazor DataGrid Component - Excel like filtering | Free UI Components by Radzen",
                            Description = "RadzenDataGrid Excel like filtering.",
                            Tags = new [] { "filter", "excel", "grid", "datagrid", "table", "menu", "checkbox", "list" }
                        },
                        new Example
                        {
                            New = true,
                            Name = "CheckBoxList with OData",
                            Path = "datagrid-checkboxlist-filter-odata",
                            Title = "Blazor DataGrid Component - Excel like filtering with OData | Free UI Components by Radzen",
                            Description = "RadzenDataGrid Excel like filtering with OData.",
                            Tags = new [] { "filter", "excel", "grid", "datagrid", "table", "menu", "checkbox", "list", "odata" }
                        },
                        new Example
                        {
                            Name = "Mixed Mode",
                            Path = "datagrid-mixed-filter",
                            Title = "Blazor DataGrid Component -  Excel like and Advanced mixed Filter Mode | Free UI Components by Radzen",
                            Description = "RadzenDataGrid Excel like and advanced mixed mode filtering.",
                            Tags = new [] { "filter", "advanced", "grid", "datagrid", "table"}
                        },
                        new Example
                        {
                            Name = "Enum filtering",
                            Path = "datagrid-enum-filter",
                            Title = "Blazor DataGrid Component - Enum Filtering | Free UI Components by Radzen",
                            Description = "This example demonstrates how to use enums in the RadzenDataGrid column filter.",
                            Tags = new [] { "filter", "enum", "grid", "datagrid", "table"}
                        },
                        new Example
                        {
                            New = true,
                            Name = "Filtering sub properties",
                            Path = "datagrid-sub-properties-filter",
                            Title = "Blazor DataGrid Component - Sub Properties Filtering | Free UI Components by Radzen",
                            Description = "This example demonstrates how to use sub properties in the RadzenDataGrid column filter.",
                            Tags = new [] { "filter", "sub properties", "grid", "datagrid", "table"}
                        },
                        new Example
                        {
                            Name = "Filter API",
                            Path = "datagrid-filter-api",
                            Title = "Blazor DataGrid Component - Filter API | Free UI Components by Radzen",
                            Description = "Set the initial filter of your RadzenDataGrid via the FilterValue and FilterOperator column properties.",
                            Tags = new [] { "filter", "api", "grid", "datagrid", "table"}
                        },
                        new Example
                        {
                            Name = "Filter Template",
                            Path = "datagrid-filter-template",
                            Title = "Blazor DataGrid Component - Custom Filtering | Free UI Components by Radzen",
                            Description = "This example demonstrates how to define custom RadzenDataGrid column filter template.",
                            Tags = new [] { "datagrid", "column", "filter", "template" }
                        },
                        new Example
                        {
                            New = true,
                            Name = "Filter Value Template",
                            Path = "datagrid-filtervalue-template",
                            Title = "Blazor DataGrid Component - Custom Filtering template | Free UI Components by Radzen",
                            Description = "This example demonstrates how to define custom RadzenDataGrid column filter value template.",
                            Tags = new [] { "datagrid", "column", "filter", "template", "value" }
                        },
                    }
                },
                new Example
                {
                    Name = "Hierarchy",
                    Updated = true,
                    Icon = "\ue23e",
                    Children  = new []
                    {
                        new Example
                        {
                            Name = "Hierarchy",
                            Path = "master-detail-hierarchy",
                            Title = "Blazor DataGrid Component - Hierarchy | Free UI Components by Radzen",
                            Description = "This example demonstrates how to use templates to create a hierarchy in a Blazor RadzenDataGrid.",
                            Tags = new [] { "master", "detail", "datagrid", "table", "dataview" }
                        },
                        new Example
                        {
                            Name = "Hierarchy on demand",
                            Path = "master-detail-hierarchy-demand",
                            Title = "Blazor DataGrid Component - Hierarchy on Demand | Free UI Components by Radzen",
                            Description = "This example demonstrates how to use templates to create a Radzen Blazor DataGrid hierarchy and load data on demand.",
                            Tags = new [] { "master", "detail", "datagrid", "table", "dataview", "on-demand" }
                        },
                        new Example
                        {
                            Name = "Self-reference hierarchy",
                            Path = "datagrid-selfref-hierarchy",
                            Title = "Blazor DataGrid Component - Self-reference Hierarchy | Free UI Components by Radzen",
                            Description = "This example demonstrates how to develop and show a self-referencing hierarchy.",
                            Tags = new [] { "master", "detail", "datagrid", "table", "dataview", "hierarchy", "self-reference" }
                        },
                        new Example
                        {
                            Name = "Master/Detail",
                            Path = "master-detail",
                            Title = "Blazor DataGrid Component - Master and Detail | Free UI Components by Radzen",
                            Description = "This example demonstrates how to create a master/detail relationship between two Blazor RadzenDataGrid components.",
                            Tags = new [] { "master", "detail", "datagrid", "table", "dataview" }
                        },
                    }
                },
                new Example
                {
                    Updated = true,
                    Name = "Selection",
                    Icon = "\uf0c5",
                    Children = new []
                    {
                        new Example
                        {
                            Name = "Single selection",
                            Path = "datagrid-single-selection",
                            Title = "Blazor DataGrid Component - Single Selection | Free UI Components by Radzen",
                            Description = "This example demonstrates how to enable single selection in Blazor RadzenDataGrid component.",
                            Tags = new [] { "single", "selection", "datagrid", "table", "dataview" }
                        },
                        new Example
                        {
                            Updated = true,
                            Name = "Multiple selection",
                            Path = "datagrid-multiple-selection",
                            Title = "Blazor DataGrid Component - Multiple Selection | Free UI Components by Radzen",
                            Description = "This example demonstrates how to enable multiple selection in Blazor RadzenDataGrid component.",
                            Tags = new [] { "multiple", "selection", "datagrid", "table", "dataview" }
                        },
                        new Example
                        {
                            New = true,
                            Name = "Cell selection",
                            Path = "datagrid-cell-selection",
                            Title = "Blazor DataGrid Component - Cell Selection | Free UI Components by Radzen",
                            Description = "This example demonstrates how to enable cell selection in Blazor RadzenDataGrid component.",
                            Tags = new [] { "cell", "selection", "datagrid", "table", "dataview" }
                        },
                    }
                },
                new Example
                {
                    Name = "Sorting",
                    Icon = "\ue164",
                    Children = new []
                    {
                        new Example
                        {
                            Name = "Single Column Sorting",
                            Path = "datagrid-sort",
                            Title = "Blazor DataGrid Component - Sorting | Free UI Components by Radzen",
                            Description = "This example demonstrates sorting in Blazor RadzenDataGrid component.",
                            Tags = new [] { "single", "sort", "datagrid", "table", "dataview" }
                        },
                        new Example
                        {
                            Name = "Multiple Column Sorting",
                            Path = "datagrid-multi-sort",
                            Title = "Blazor DataGrid Component - Sorting by Multiple Columns | Free UI Components by Radzen",
                            Description = "This example demonstrates multiple column sorting in Blazor RadzenDataGrid component.",
                            Tags = new [] { "multi", "sort", "datagrid", "table", "dataview" }
                        },
                        new Example
                        {
                            Name = "Sort API",
                            Path = "datagrid-sort-api",
                            Title = "Blazor DataGrid Component - Sort API | Free UI Components by Radzen",
                            Description = "Set the initial sort order of your RadzenDataGrid via the SortOrder column property.",
                            Tags = new [] { "api", "sort", "datagrid", "table", "dataview" }
                        }
                    }
                },
                new Example
                {
                    Name = "Paging",
                    Updated = true,
                    Icon = "\ue5dd",
                    Children = new []
                    {
                        new Example
                        {
                            Name = "Pager Position",
                            Path = "datagrid-pager-position",
                            Title = "Blazor DataGrid Component - Pager Position | Free UI Components by Radzen",
                            Description = "Set the pager position to Top, Bottom, or TopAndBottom.",
                            Tags = new [] { "pager", "paging", "datagrid", "table", "dataview" }
                        },
                        new Example
                        {
                            Name = "Pager Horizontal Align",
                            Path = "datagrid-pager-horizontal-align",
                            Title = "Blazor DataGrid Component - Pager Horizontal Align | Free UI Components by Radzen",
                            Description = "See how to change the horizontal alignment of the pager in a RadzenDataGrid.",
                            Tags = new [] { "pager", "paging", "datagrid", "table", "dataview" }
                        },
                        new Example
                        {
                            Name = "Pager API",
                            Path = "datagrid-pager-api",
                            Title = "Blazor DataGrid Component - Pager API | Free UI Components by Radzen",
                            Description = "Blazor RadzenDataGrid Pager API.",
                            Tags = new [] { "pager", "paging", "api", "datagrid", "table", "dataview" }
                        }
                    }
                },
                new Example
                {
                    Updated = true,
                    Name = "Grouping",
                    Icon = "\uf1be",
                    Children = new []
                    {
                        new Example
                        {
                            Updated = true,
                            Name = "Grouping API",
                            Path = "datagrid-grouping-api",
                            Title = "Blazor DataGrid Component - Grouping API | Free UI Components by Radzen",
                            Description = "Set AllowGrouping to true, to enable group by column and GroupPanelText to localize the group panel text. Set Groupable to false for column, to disable grouping by that column.",
                            Tags = new [] { "group", "grouping", "datagrid", "table", "dataview", "api" }
                        },
                        new Example
                        {
                            Updated = true,
                            Name = "Group Header Template",
                            Path = "datagrid-group-header-template",
                            Title = "Blazor DataGrid Component - Group Header Template | Free UI Components by Radzen",
                            Description = "Use GroupHeaderTemplate to customize DataGrid group header rows.",
                            Tags = new [] { "group", "grouping", "template", "datagrid", "table", "dataview" }
                        },
                        new Example
                        {
                            Name = "Group Footer Template",
                            Path = "datagrid-group-footer-template",
                            Title = "Blazor DataGrid Component - Group Footer Template | Free UI Components by Radzen",
                            Description = "The GroupFooterTemplate column property allows you to display aggregated data (totals) in the column footer for each group.",
                            Tags = new [] { "group", "grouping", "footer", "template", "datagrid", "table", "dataview" }
                        }
                    }
                },
                new Example
                {
                    Name = "Density",
                    Path = "datagrid-density",
                    Title = "Blazor DataGrid Component - Density | Free UI Components by Radzen",
                    Description = "See how to set a compact density mode of Blazor RadzenDataGrid.",
                    Icon = "\ueb9e",
                    Tags = new [] { "density", "compact", "small", "large", "tight" }
                },
                new Example
                {
                    Name="Custom Header",
                    Icon = "\ue051",
                    Children = new [] {
                      new Example
                        {
                            Name = "Header with button",
                            Path="datagrid-custom-header",
                            Title = "Blazor DataGrid Component - Custom Header | Free UI Components by Radzen",
                            Description = "Gives the grid a custom header, allowing the adding of components to create custom tool bars in addtion to column grouping and column picker.",
                            Tags = new [] { "grid header","header" }
                        },
                      new Example
                        {
                            Name = "Header with column picker",
                            Path="datagrid-custom-header-columnpicker",
                            Title = "Blazor DataGrid Component - Header with Column Picker | Free UI Components by Radzen",
                            Description = "See how to add a column picker to your Blazor RadzenDataGrid.",
                            Tags = new [] { "grid header","header" }
                        }
                    }
                },
                new Example
                {
                    Name = "GridLines",
                    Path = "datagrid-grid-lines",
                    Title = "Blazor DataGrid Component - Grid Lines | Free UI Components by Radzen",
                    Description = "Deside where to display grid lines in your Blazor RadzenDataGrid.",
                    Icon = "\uf016",
                    Tags = new [] { "grid", "lines", "border", "gridlines" }
                },
                new Example
                {
                    Name = "Cell Context Menu",
                    Path = "datagrid-cell-contextmenu",
                    Title = "Blazor DataGrid Component - Cell Context Menu | Free UI Components by Radzen",
                    Description = "Right click on a table cell to open the context menu.",
                    Icon = "\ue22b",
                    Tags = new [] { "cell", "row", "contextmenu", "menu", "rightclick" }
                },

                new Example
                {
                    Updated = true,
                    Name = "Save/Load settings",
                    Icon = "\uf02e",
                    Children = new []
                    {
                        new Example
                        {
                            Name = "IQueryable",
                            Path = "datagrid-save-settings",
                            Title = "Blazor DataGrid Component - Save / Load Settings | Free UI Components by Radzen",
                            Description = "This example shows how to save/load DataGrid state using Settings property. The state includes current page index, page size, groups and columns filter, sort, order, width and visibility.",
                            Tags = new [] { "save", "load", "settings" }
                        },

                        new Example
                        {
                            Name = "LoadData binding",
                            Path = "datagrid-save-settings-loaddata",
                            Title = "Blazor DataGrid Component - Save / Load Settings with LoadData | Free UI Components by Radzen",
                            Description = "This example shows how to save/load DataGrid state using Settings property when binding using LoadData event.",
                            Tags = new [] { "save", "load", "settings", "async", "loaddata" }
                        }
                    }
                },

                new Example
                {
                    Updated = true,
                    Name = "Drag & Drop",
                    Icon = "\ue945",
                    Children = new []
                    {
                        new Example
                        {
                            Name = "Rows reorder",
                            Path = "/datagrid-rowreorder",
                            Title = "Blazor DataGrid Component - Reorder rows | Free UI Components by Radzen",
                            Description = "This example demonstrates custom DataGrid rows reoder.",
                            Tags = new [] { "datagrid", "reorder", "row" }
                        },
                        new Example
                        {
                            Name = "Drag row between two DataGrids",
                            Path = "/datagrid-rowdragbetween",
                            Title = "Blazor DataGrid Component - Drag rows between two DataGrids | Free UI Components by Radzen",
                            Description = "This example demonstrates drag and drop rows between two DataGrid components.",
                            Tags = new [] { "datagrid", "drag", "row", "between" }
                        },
                        new Example
                        {
                            New = true,
                            Name = "Drag row between DataGrid and Scheduler",
                            Path = "/datagrid-rowdrag-scheduler",
                            Title = "Blazor DataGrid Component - Drag rows from DataGrid to Scheduler | Free UI Components by Radzen",
                            Description = "This example demonstrates drag and drop rows between DataGrid and Scheduler.",
                            Tags = new [] { "datagrid", "drag", "row", "scheduler" }
                        }
                    }
                },

                new Example
                {
                    Name = "InLine Editing",
                    Path = "datagrid-inline-edit",
                    Title = "Blazor DataGrid Component - InLine Editing | Free UI Components by Radzen",
                    Description = "This example demonstrates how to configure the Razden Blazor DataGrid for inline editing.",
                    Icon = "\ue22b",
                    Tags = new [] { "inline", "editor", "datagrid", "table", "dataview" }
                },

                new Example
                {
                    New = true,
                    Name = "InCell Editing",
                    Path = "datagrid-incell-edit",
                    Title = "Blazor DataGrid Component - InCell Editing | Free UI Components by Radzen",
                    Description = "This example demonstrates how to configure the Razden Blazor DataGrid for in-cell editing.",
                    Icon = "\ue745",
                    Tags = new [] { "in-cell", "editor", "datagrid", "table", "dataview" }
                },

                new Example
                {
                    Name = "Conditional formatting",
                    Path = "datagrid-conditional-template",
                    Title = "Blazor DataGrid Component - Conditional Formatting | Free UI Components by Radzen",
                    Description = "This example demonstrates RadzenDataGrid with conditional rows and cells template and styles.",
                    Icon = "\ue41d",
                    Tags = new [] { "conditional", "template", "style", "datagrid", "table", "dataview" }
                },
                new Example
                {
                    Name = "Export to Excel and CSV",
                    Path = "export-excel-csv",
                    Title = "Blazor DataGrid Component - Export to Excel and CSV | Free UI Components by Radzen",
                    Description = "This example demonstrates how to export a Radzen Blazor DataGrid to Excel and CSV.",
                    Icon = "\ue0c3",
                    Tags = new [] { "export", "excel", "csv" }
                },
                new Example
                {
                    Name = "Cascading DropDowns",
                    Path = "cascading-dropdowns",
                    Title = "Blazor DataGrid Component - Cascading DropDowns | Free UI Components by Radzen",
                    Description = "This example demonstrates cascading Radzen Blazor DropDown components.",
                    Icon = "\ue915",
                    Tags = new [] { "related", "parent", "child" }
                },
                new Example
                {
                    Name = "Empty Data Grid",
                    Path = "/datagrid-empty",
                    Title = "Blazor DataGrid Component - Empty Data Grid | Free UI Components by Radzen",
                    Description = "This example demonstrates Blazor DataGrid without data.",
                    Icon = "\ue661",
                    Tags = new [] { "datagrid", "databinding" }
                }
            }
        },
        new Example
        {
            Name = "Data",
            Updated = true,
            Icon = "\ue99c",
            Children = new [] {
                new Example
                {
                    Name = "DataList",
                    Icon = "\ue896",
                    Tags = new[] { "dataview", "grid", "table" },
                    Children = new[] {
                        new Example
                        {
                            Name = "IQueryable",
                            Title = "Blazor DataList Component | Free UI Components by Radzen",
                            Description = "Demonstration and configuration of the Radzen Blazor DataList component.",
                            Path = "datalist",
                            Tags = new [] { "dataview", "grid", "table", "list"},
                        },
                        new Example
                        {
                            Name = "OData service",
                            Title = "Blazor DataList Component - OData Service | Free UI Components by Radzen",
                            Description = "Demonstration and configuration of the Radzen Blazor DataList component using LoadData event.",
                            Path = "datalist-loaddata",
                            Tags = new [] { "dataview", "grid", "table", "list", "odata" },
                        }
                    }
                },
                new Example
                {
                    Name = "DataFilter",
                    Icon = "\uef4f",
                    Tags = new[] { "dataview", "grid", "table", "filter" },
                    Children = new[] {
                        new Example
                        {
                            Toc = [ new () { Text = "Keyboard Navigation", Anchor = "#keyboard-navigation" } ],
                            Name = "IQueryable",
                            Title = "Blazor DataFilter Component | Free UI Components by Radzen",
                            Description = "Filter Entity Framework IQueryable without extra code.",
                            Path = "datafilter",
                            Tags = new [] { "dataview", "grid", "table", "filter" },
                        },
                        new Example
                        {
                            Name = "LoadData",
                            Title = "Blazor DataFilter Component - LoadData event | Free UI Components by Radzen",
                            Description = "This example demonstrates DataFilter with DataGrid LoadData event.",
                            Path = "datafilter-loaddata",
                            Tags = new [] { "dataview", "grid", "table", "filter", "loaddata" },
                        },
                        new Example
                        {
                            Name = "OData service",
                            Title = "Blazor DataFilter Component - OData Service | Free UI Components by Radzen",
                            Description = "This example demonstrates data filter with OData service.",
                            Path = "datafilter-odata",
                            Tags = new [] { "dataview", "grid", "table", "filter", "odata" },
                        }
                    }
                },

                new Example
                {

                    Toc = [ new () { Text = "Pager Density", Anchor = "#pager-density" }, new () { Text = "Keyboard Navigation", Anchor = "#keyboard-navigation" } ],
                    Name = "Pager",
                    Path = "pager",
                    Description = "Demonstration and configuration of the Radzen Blazor Pager component.",
                    Icon = "\ueb8d",
                    Tags = new[] { "pager", "paging" }
                },
                new Example
                {
                    Name = "PickList",
                    Description = "Use Radzen Blazor PickList component to transfer items between two collections.",
                    Path = "picklist",
                    Icon = "\ue0b8",
                    Tags = new[] { "picklist", "list", "listbox" }
                },
                new Example
                {
                    Toc = [ new () { Text = "Day, week and month views", Anchor="#views"}, new () { Text = "Year Planner and Timeline views", Anchor = "#timeline" }, new () { Text = "Display additional content when the user hovers an appointment", Anchor = "#tooltips" }, new () { Text = "Display any number of days side-by-side", Anchor = "#multiday" }, new () { Text = "Keyboard Navigation", Anchor = "#keyboard-navigation" } ],
                    Name = "Scheduler",
                    Path = "scheduler",
                    Updated = true,
                    Description = "Blazor Scheduler component with daily, weekly and monthly views.",
                    Icon = "\ue616",
                    Tags = new[] { "scheduler", "calendar", "event", "appointment" }
                },
                new Example
                {
                    Toc = [ new () { Text = "Dynamic Table", Anchor = "#dynamic" }, new () { Text = "Scrollable Table", Anchor = "#scrollable" }, new () { Text = "Table with merged cells", Anchor = "#scrollable" } ],
                    Name = "Table",
                    New = true,
                    Description = "Blazor RadzenTable component is used to create a HTML table with rows and cells.",
                    Path = "table",
                    Icon = "\uf101",
                    Tags = new [] { "table", "cells", "row", "grid" }
                },
                new Example
                {
                    Name = "Tree",
                    Icon = "\ue94b",
                    Tags = new[] { "tree", "treeview", "nodes", "hierarchy" },
                    Children = new[] {
                        new Example
                        {
                            Toc = [ new () { Text = "Keyboard Navigation", Anchor = "#keyboard-navigation" } ],
                            Name = "Inline definition",
                            Title = "Blazor Tree Component | Free UI Components by Radzen",
                            Description = "Demonstration and configuration of the Blazor RadzenTree component.",
                            Path = "tree",
                            Tags = new [] { "tree", "treeview", "nodes", "inline" },
                        },
                        new Example
                        {
                            Name = "Data-binding",
                            Title = "Blazor Tree Component - Data-binding | Free UI Components by Radzen",
                            Description = "This example demonstrates how to populate RadzenTree from a database via Entity Framework.",
                            Path = "tree-data-binding",
                            Tags = new [] { "tree", "treeview", "nodes", "data", "table" },
                        },
                        new Example
                        {
                            Name = "Files and directories",
                            Title = "Blazor Tree Component - Data-binding to Files and Directories | Free UI Components by Radzen",
                            Description = "This example demonstrates how to populate Blazor RadzenTree from the file system.",
                            Path = "tree-file-system",
                            Tags = new [] { "tree", "treeview", "nodes", "file", "directory" },
                        },
                        new Example
                        {
                            Name = "Selection",
                            Title = "Blazor Tree Component - Selection | Free UI Components by Radzen",
                            Description = "This example demonstrates how to get or set the selected items of RadzenTree.",
                            Path = "tree-selection",
                            Tags = new [] { "tree", "treeview", "nodes", "selection" },
                        },
                        new Example
                        {
                            Name = "Checkboxes",
                            Title = "Blazor Tree Component - Tri-state Checkboxes | Free UI Components by Radzen",
                            Description = "This example demonstrates tri-state checkboxes in RadzenTree.",
                            Path = "tree-checkboxes",
                            Tags = new [] { "tree", "treeview", "nodes", "check" },
                        },
                        new Example
                        {
                            Name = "Drag & Drop",
                            Title = "Blazor Tree Component - Drag & Drop items | Free UI Components by Radzen",
                            Description = "This example demonstrates custom drag & drop logic in RadzenTree.",
                            Path = "tree-dragdrop",
                            Tags = new [] { "tree", "treeview", "nodes", "drag", "drop" },
                        },
                        new Example
                        {
                            Name = "Context menu",
                            Title = "Blazor Tree Component - Context menu | Free UI Components by Radzen",
                            Description = "This example demonstrates context menu in RadzenTree.",
                            Path = "tree-contextmenu",
                            Tags = new [] { "tree", "treeview", "nodes", "context", "menu" },
                        },
                        new Example
                        {
                            Name = "Refreshing tree data-binding",
                            Title = "Blazor Tree Component - Refreshing tree data-binding | Free UI Components by Radzen",
                            Description = "This example demonstrates how to refresh a lazily loaded RadzenTree.",
                            Path = "tree-data-binding-refresh",
                            Tags = new [] { "tree", "treeview", "nodes" },
                        },
                        new Example
                        {
                            New = true,
                            Name = "Tree filtering",
                            Title = "Blazor Tree Component - Filtering | Free UI Components by Radzen",
                            Description = "This example demonstrates how to filter RadzenTree.",
                            Path = "tree-filter",
                            Tags = new [] { "tree", "treeview", "filter" },
                        }
                    }
                }
            }
},
        new Example
        {
            Toc = [ new () { Text = "Gravatar with email (info@radzen.com)", Anchor = "#gravatar-with-email" } ],
            Name = "Images",
            Icon = "\ue3d3",
            Children = new[] {
                new Example
                {
                    Name = "Gravatar",
                    Description = "Demonstration and configuration of the Radzen Blazor Gravatar component.",
                    Path = "gravatar",
                    Icon = "\ue420"
                },
                new Example
                {
                    Toc = [ new () { Text = "Image from application assets", Anchor = "#image-from-application-assets" }, new () { Text = "Image from url", Anchor = "#image-from-url" }, new () { Text = "Image from base64 encoded string", Anchor = "#image-from-base64" }, new () { Text = "Image from binary data", Anchor = "#image-from-binary-data" } ],
                    Name = "Image",
                    Description = "Demonstration and configuration of the Radzen Blazor Image component.",
                    Path = "image",
                    Icon = "\ue3c4"
                },
            }
        },
        new Example
        {
            Name = "Layout",
            Updated = true,
            Icon = "\ue8f1",
            Children = new[] {
                new Example
                {
                    Toc = [ new () { Text = "Sidebar, Header and Footer", Anchor = "#sidebar-header-footer" }, new () { Text = "Full height Sidebar", Anchor = "#full-height-sidebar" }, new () { Text = "Overlay Sidebar", Anchor = "#overlay" }, new () { Text = "Full height overlay Sidebar", Anchor = "#overlay-full" }, new () { Text = "Right Sidebar", Anchor = "#right-sidebar" }, new () { Text = "Right full height Sidebar", Anchor = "#right-full-height-sidebar" }, new () { Text = "Right and Left Sidebar", Anchor = "#right-left-sidebar" }, new () { Text = "Icon Sidebar", Anchor = "#icon-sidebar" } ],
                    Name = "Layout",
                    Description = "Blazor RadzenLayout allows you to define the global layout of your application.",
                    Path = "layout",
                    Icon = "\ue8f1",
                    Tags = new [] { "layout", "sidebar", "drawer", "header", "body", "footer" }
                },
                new Example
                {
                    Name = "Stack",
                    Description = "Use RadzenStack component to create a stack layout - a way of arranging elements in a vertical or horizontal stack.",
                    Path = "stack",
                    Icon = "\ue8e9",
                    Tags = new [] { "stack", "layout" }
                },
                new Example
                {
                    Name = "Row",
                    Description = "Blazor RadzenRow component is used to create a row in a responsive grid layout.",
                    Path = "row",
                    Icon = "\uf676",
                    Tags = new [] { "row", "layout", "responsive", "grid" }
                },
                new Example
                {
                    Toc = [ new () { Text = "Auto-layout columns", Anchor = "#auto-layout-columns" }, new () { Text = "Column sizes", Anchor = "#column-sizes" }, new () { Text = "Responsive column sizes", Anchor = "#responsive-column-sizes" }, new () { Text = "Column wrapping", Anchor = "#column-wrapping" }, new () { Text = "Column offset", Anchor = "#column-offset" }, new () { Text = "Responsive offsetting", Anchor = "#column-responsive-offset" }, new () { Text = "Column order", Anchor = "#column-order" }, new () { Text = "Responsive column ordering", Anchor = "#column-responsive-order" }, new () { Text = "Nested Layouts", Anchor = "#nested-layouts" }, new () { Text = "Gutters", Anchor = "#gutters" } ],
                    Name = "Column",
                    Description = "Blazor RadzenColumn component is used within a RadzenRow to create a structured grid layout. Columns are positioned on a 12-column based responsive grid.",
                    Path = "column",
                    Icon = "\uf674",
                    Tags = new [] { "column", "col", "layout", "responsive", "grid" }
                },
                new Example
                {
                    Toc = [ new () { Text = "Card Variant", Anchor = "#card-variant" } ],
                    Name = "Card",
                    Description = "Use the Blazor RadzenCard component to display a piece of content, like an image and text.",
                    Path = "card",
                    Icon = "\uefad",
                    Tags = new [] { "card", "container" }
                },
                new Example
                {
                    Name = "CardGroup",
                    Description = "Use the Blazor RadzenCardGroup component to visually stick RadzenCards next to each other.",
                    Path = "card-group",
                    Icon = "\ue8f3",
                    Tags = new [] { "card", "group", "deck", "container" }
                },
                new Example
                {
                    Toc = [ new () { Text = "Open page as a dialog", Anchor = "#open-page-as-dialog" }, new () { Text = "Inline Dialog", Anchor = "#inline-dialog" }, new () { Text = "Busy Dialog", Anchor = "#busy-dialog" }, new () { Text = "Confirm Dialog", Anchor = "#confirm-dialog" }, new () { Text = "Alert Dialog", Anchor = "#alert-dialog" }, new () { Text = "Close Dialog by clicking outside", Anchor = "#close-dialog-by-clicking-outside" }, new () { Text = "Side Dialog", Anchor = "#side-dialog" }, new () { Text = "Dialog with custom CSS classes", Anchor = "#custom-css-classes" }, new () { Text = "Update dialog properties", Anchor = "#cascading-value" } ],
                    Name = "Dialog",
                    Description = "Demonstration and configuration of the Blazor RadzenDialog component.",
                    Path = "dialog",
                    Icon = "\ue069",
                    Tags = new [] { "popup", "window" },
                },
                new Example
                {
                    Toc = [ new () { Text = "Define can-drop and no-drop styles", Anchor = "#can-drop-no-drop-styles" }, new () { Text = "Define a Footer Template per Drop Zone", Anchor = "#footer-template" } ],
                    Name = "DropZone",
                    Description = "Demonstration and configuration of the Radzen Blazor DropZone component.",
                    Path = "dropzone",
                    Icon = "\ue945",
                    Tags = new [] { "dropzone", "drag", "drop" }
                },
                new Example
                {
                    Toc = [ new () { Text = "Templates", Anchor = "#templates" }, new () { Text = "Expand/Collapse", Anchor = "#expand-collapse" } ],
                    Name = "Panel",
                    Description = "Demonstration and configuration of the Blazor RadzenPanel component.",
                    Path = "panel",
                    Icon = "\uf732",
                    Tags = new [] { "container" }
                },
                new Example
                {
                    Name = "Popup",
                    Description = "Demonstration and configuration of the Radzen Blazor Popup component.",
                    Path = "popup",
                    Icon = "\ue0ca",
                    Tags = new [] { "popup", "dropdown"}
                },
                new Example
                {
                    Name = "Splitter",
                    Description = "Demonstration and configuration of the Blazor RadzenSplitter component.",
                    Path = "splitter",
                    Icon = "\ue42a",
                    Tags = new [] { "splitter", "layout"}
                }
            }
        },
        new Example
        {
            Name = "Navigation",
            Icon = "\ue762",
            Children = new[] {
                new Example
                {
                    Toc = [ new () { Text = "Accordion with single expand", Anchor = "#single-expand" }, new () { Text = "Accordion with multiple expand", Anchor = "#multiple-expand" }, new () { Text = "Dynamically create Accordion items", Anchor = "#dynamic-items" }, new () { Text = "Expand/Collapse events", Anchor = "#expand-collapse-events" }, new () { Text = "Disable expand/collapse", Anchor = "#disable-expand-collapse" }, new () { Text = "Keyboard Navigation", Anchor = "#keyboard-navigation" } ],
                    Name = "Accordion",
                    Path = "accordion",
                    Description = "Demonstration and configuration of the Blazor RadzenAccordion component.",
                    Icon = "\ue8fe",
                    Tags = new [] { "panel", "container" }
                },
                new Example
                {
                    Toc = [ new () { Text = "Default Radzen BreadCrumb", Anchor = "#default-breadcrumb" }, new () { Text = "BreadCrumb width template", Anchor = "#breadcrumb-template" }, new () { Text = "BreadCrumb with child content", Anchor = "#breadcrumb-child-template" } ],
                    Name = "BreadCrumb",
                    Description = "The Blazor RadzenBreadCrumb component provides a navigation trail to help users keep track of their location.",
                    Path = "breadcrumb",
                    Icon = "\uea50",
                    Tags = new [] { "breadcrumb", "navigation", "menu" }
                },
                new Example
                {
                    Toc = [ new () { Text = "Navigation button styles", Anchor = "#navigation-style" }, new () { Text = "Navigation button content", Anchor = "#navigation-content" }, new () { Text = "Paging", Anchor = "#paging" }, new () { Text = "Data-binding", Anchor = "#data-binding" }, new () { Text = "Carousel with RadzenPager", Anchor = "#pager" } ],
                    Name = "Carousel",
                    Description = "Demonstration and configuration of the Radzen Blazor Carousel component.",
                    Path = "carousel",
                    Icon = "\ue8eb",
                    Tags = new [] { "carousel", "gallery", "slide", "deck", "container" }
                },
                new Example
                {
                    Toc = [ new () { Text = "Show ContextMenu with items", Anchor = "#contextmenu-with-items" }, new () { Text = "Show ContextMenu with custom content and separator", Anchor = "#contextmenu-with-custom-content" }, new () { Text = "Show ContextMenu for HTML element", Anchor = "#contextmenu-for-html-element" } ],
                    Name = "ContextMenu",
                    Description = "Demonstration and configuration of the Radzen Blazor Context Menu component.",
                    Path = "contextmenu",
                    Icon = "\ue8de",
                    Tags = new [] { "popup", "dropdown", "menu" }
                },
                new Example
                {
                    Toc = [ new () { Text = "Link to path in application", Anchor = "#link-to-path" }, new () { Text = "Link to path in application with icon", Anchor = "#link-with-icon" }, new () { Text = "Link to url", Anchor = "#link-to-url" }, new () { Text = "Link with child content", Anchor = "#link-child-content" }, new () { Text = "Link disabled", Anchor = "#link-disabled" } ],
                    Name = "Link",
                    Description = "Demonstration and configuration of the Blazor RadzenLink component. Use Path and Target properties to specify Link component navigation.",
                    Path = "link",
                    Icon = "\ue157"
                },
                new Example
                {
                    Toc = [ new () { Text = "Login Events", Anchor = "#login-events" }, new () { Text = "Simple Login", Anchor = "#simple-login" }, new () { Text = "Login with Register (hide password reset)", Anchor = "#login-with-register" }, new () { Text = "Remember me", Anchor = "#remember-me" }, new () { Text = "Form fields", Anchor = "#form-fields" }, new () { Text = "Localization", Anchor = "#localization" }, new () { Text = "Horizontal login layout example", Anchor = "#horizontal-login-example" }, new () { Text = "Vertical login layout example", Anchor = "#vertical-login-example" } ],
                    Name = "Login",
                    Description = "Demonstration and configuration of the Blazor RadzenLogin component.",
                    Path = "login",
                    Icon = "\uea77"
                },
                new Example
                {
                    Toc = [ new () { Text = "Keyboard Navigation", Anchor = "#keyboard-navigation" } ],
                    Name = "Menu",
                    Description = "Demonstration and configuration of the Blazor RadzenMenu component.",
                    Path = "menu",
                    Icon = "\ue5d2",
                    Tags = new [] { "navigation", "dropdown" }
                },
                new Example
                {
                    Toc = [ new () { Text = "Statically declared items", Anchor = "#panelmenu-static" }, new () { Text = "Programmatically created items with Expanded binding", Anchor = "#panelmenu-programmatic" }, new () { Text = "Set the display style of menu items", Anchor = "#panelmenu-display-style" }, new () { Text = "Keyboard Navigation", Anchor = "#keyboard-navigation" } ],
                    Name = "PanelMenu",
                    Path = "panelmenu",
                    Updated = true,
                    Description = "Demonstration and configuration of the Blazor RadzenPanelMenu component.",
                    Icon = "\ue875",
                    Tags = new [] { "navigation", "menu" }
                },
                new Example
                {
                    Toc = [ new () { Text = "Keyboard Navigation", Anchor = "#keyboard-navigation" } ],
                    Name = "ProfileMenu",
                    Description = "Demonstration and configuration of the Blazor RadzenProfileMenu component.",
                    Path = "profile-menu",
                    Icon = "\ue851",
                    Tags = new [] { "navigation", "dropdown", "menu" }
                },
                new Example
                {
                    Toc = [ new () { Text = "CanChange event", Anchor = "#canchange-event" } ],
                    Name = "Steps",
                    Description = "Use Radzen Blazor Steps component to guide users through a process or sequence of actions. The component consists of a series of numbered steps that represent the various stages of the process.",
                    Path = "steps",
                    Icon = "\ue8be",
                    Tags = new [] { "step", "steps", "wizard" }
                },
                new Example
                {
                    Toc = [ new () { Text = "Tabs position", Anchor = "#tabs-position" }, new () { Text = "Server render mode", Anchor = "#server-render-mode" }, new () { Text = "Client render mode", Anchor = "#client-render-mode" }, new () { Text = "TabItems modify", Anchor = "#tabs-modify" }, new () { Text = "Tab items wrap", Anchor = "#tabs-wrap" }, new () { Text = "Prevent Tab change", Anchor = "#prevent-tab-change" }, new () { Text = "Keyboard Navigation", Anchor = "#keyboard-navigation" } ],
                    Name = "Tabs",
                    Description = "Demonstration and configuration of the Radzen Blazor Tabs component.",
                    Path = "tabs",
                    Icon = "\ue8d8",
                    Tags = new [] { "tabstrip", "tabview", "container" }
                },
                new Example
                {
                    Toc = [ new () { Text = "Sticky TOC", Anchor = "#sticky" }, new () { Text = "Orientation", Anchor = "#orientation" } ],
                    Name = "Toc",
                    Description = "Table of contents component",
                    Path = "toc",
                    Icon = "\ue241",
                    New = true,
                    Tags = [ "toc", "content", "navigation" ]
                }
            }
        },
        new Example
        {
            Name = "Forms",
            Icon = "\uf1c1",
            Children = new[] {
                new Example
                {
                    Name = "AIChat",
                    Path = "aichat",
                    New = true,
                    Description = "A modern chat component with AI integration that provides a conversational interface similar to popular chat applications.",
                    Icon = "\ue0b7",
                    Tags = new [] { "chat", "ai", "conversation", "message", "streaming" }
                },
                new Example
                {
                    Toc = [ new () { Text = "Get and Set the value of AutoComplete", Anchor = "#get-set-value" }, new () { Text = "Get and Set the value of AutoComplete using Value and Change event", Anchor = "#value-and-change-event" }, new () { Text = "Get the selected item of AutoComplete", Anchor = "#get-selected" }, new () { Text = "Define AutoComplete placeholder", Anchor = "#placeholder" }, new () { Text = "Define AutoComplete template", Anchor = "#template" }, new () { Text = "Change AutoComplete filter operator, case sensitivity and delay", Anchor = "#filter-operator" }, new () { Text = "Load data on-demand in AutoComplete and apply custom filter and sort", Anchor = "#load-on-demand" }, new () { Text = "AutoComplete with a List of Strings", Anchor = "#list-of-strings" }, new () { Text = "Multiline AutoComplete", Anchor = "#multiline" }, new () { Text = "Open on Focus", Anchor = "#open-on-focus" }, new () { Text = "Disabled AutoComplete", Anchor = "#disabled-autocomplete" }, new () { Text = "Keyboard Navigation", Anchor = "#keyboard-navigation" } ],
                    Name = "AutoComplete",
                    Path = "autocomplete",
                    Description = "Demonstration and configuration of the Radzen Blazor AutoComplete textbox component.",
                    Icon = "\ue03b",
                    Tags = new [] { "form", "complete", "suggest", "edit" }
                },
                new Example
                {
                    Toc = [ new () { Text = "Filled Buttons", Anchor = "#filled-buttons" }, new () { Text = "Flat Buttons", Anchor = "#flat-buttons" }, new () { Text = "Outlined Buttons", Anchor = "#outlined-buttons" }, new () { Text = "Text Buttons", Anchor = "#text-buttons" }, new () { Text = "Content in Buttons", Anchor = "#content-in-buttons" }, new () { Text = "Button Sizes", Anchor = "#button-sizes" }, new () { Text = "FAB", Anchor = "#fab" }, new () { Text = "Disabled Button", Anchor = "#disabled-button" }, new () { Text = "Busy button", Anchor = "#busy-button" }, new () { Text = "Keyboard Navigation", Anchor = "#keyboard-navigation" } ],
                    Name = "Button",
                    Description = "Demonstration and configuration of the RadzenButton Blazor component.",
                    Path = "button",
                    Icon = "\ue72f"
                },
                new Example
                {
                    Toc = [ new () { Text = "Bound ToggleButton", Anchor = "#bound-toggle-button" }, new () { Text = "ToggleButton Shade", Anchor = "#shade" }, new () { Text = "ToggleButton Style", Anchor = "#style" }, new () { Text = "ToggleButton Variants", Anchor = "#variants" }, new () { Text = "Content in ToggleButtons", Anchor = "#content" }, new () { Text = "ToggleButton Sizes", Anchor = "#sizes" }, new () { Text = "Disabled ToggleButton", Anchor = "#disabled" }, new () { Text = "Keyboard Navigation", Anchor = "#keyboard-navigation" } ],
                    Name = "ToggleButton",
                    Description = "Radzen Blazor ToggleButton is a button that changes its appearance or color when activated and returns to its original state when deactivated.",
                    Path = "toggle-button",
                    Icon = "\ue8e0",
                    Tags = new [] { "button", "switch", "toggle" }
                },
                new Example
                {
                    Toc = [ new () { Text = "Get and Set the value of CheckBox", Anchor = "#get-set-value" }, new () { Text = "Get and Set the value of CheckBox using Value and Change event", Anchor = "#value-and-change-event" }, new () { Text = "TriState CheckBox", Anchor = "#tristate-checkbox" }, new () { Text = "Disabled CheckBox", Anchor = "#disabled-checkbox" }, new () { Text = "ReadOnly CheckBox", Anchor = "#readonly-checkbox" }, new () { Text = "Keyboard Navigation", Anchor = "#keyboard-navigation" } ],
                    Name = "CheckBox",
                    Path = "checkbox",
                    Description = "Demonstration and configuration of the Radzen Blazor CheckBox component with optional tri-state support.",
                    Icon = "\ue834",
                    Tags = new [] { "form", "edit" }
                },
                new Example
                {
                    Toc = [ new () { Text = "Get and Set the value of CheckBoxList", Anchor = "#get-set-value" }, new () { Text = "Get and Set the value of CheckBoxList using Value and Change event", Anchor = "#value-and-change-event" }, new () { Text = "Set CheckBoxList orientation and layout", Anchor = "#orientation" }, new () { Text = "Populate CheckBoxList items from data", Anchor = "#populate-items" }, new () { Text = "Statically declared and populated CheckBoxList items from data", Anchor = "#statically-declared" }, new () { Text = "Select all CheckBoxList items", Anchor = "#select-all-items" }, new () { Text = "Disabled CheckBoxList item", Anchor = "#disabled-item" }, new () { Text = "ReadOnly CheckBoxList item", Anchor = "#readonly-item" }, new () { Text = "Templated CheckBoxList item", Anchor = "#templated-item" }, new () { Text = "Keyboard Navigation", Anchor = "#keyboard-navigation" } ],
                    Name = "CheckBoxList",
                    Path = "checkboxlist",
                    Updated = true,
                    Description = "Demonstration and configuration of the Radzen Blazor CheckBoxList component.",
                    Icon = "\ue6b1",
                    Tags = new [] { "form", "edit" }
                },
                new Example
                {
                    Toc = [ new () { Text = "Keyboard Navigation", Anchor = "#keyboard-navigation" } ],
                    Name = "ColorPicker",
                    Description = "Demonstration and configuration of the Radzen Blazor ColorPicker component. HSV Picker. RGBA Picker.",
                    Path = "colorpicker",
                    Icon = "\ue40a",
                    Tags = new [] { "form", "edit" }
                },
                new Example
                {
                    Toc = [ new () { Text = "Get and Set the value of DatePicker", Anchor = "#get-set-value" }, new () { Text = "Get and Set the value of DatePicker using Value and Change event", Anchor = "#value-and-change-event" }, new () { Text = "DatePicker with time", Anchor = "#datepicker-with-time" }, new () { Text = "Define hour format", Anchor = "#hour-format" }, new () { Text = "Time-only DatePicker", Anchor = "#time-only-datepicker" }, new () { Text = "DatePicker with special or disabled dates", Anchor = "#special-disabled-dates" }, new () { Text = "DatePicker with initial view date and year range", Anchor = "#initial-view-date-and-year-change" }, new () { Text = "Set Min and Max dates", Anchor = "#min-max-dates" }, new () { Text = "DatePicker with custom footer", Anchor = "#custom-footer" }, new () { Text = "DatePicker with custom input parsing", Anchor = "#custom-input-parsing" }, new () { Text = "DatePicker as calendar", Anchor = "#calendar" }, new () { Text = "DatePicker for year/month selection", Anchor = "#year-month-selection" }, new () { Text = "DatePicker binds to types DateOnly or TimeOnly", Anchor = "#dateonly-timeonly" }, new () { Text = "Keyboard Navigation", Anchor = "#keyboard-navigation" } ],
                    Name = "DatePicker",
                    Path = "datepicker",
                    Updated = true,
                    Description = "Demonstration and configuration of the Radzen Blazor Datepicker component with calendar mode. Time Picker.",
                    Icon = "\ue916",
                    Tags = new [] { "calendar", "time", "form", "edit" }
                },
                new Example
                {
                    Name = "DropDown",
                    Icon = "\ue172",
                    Children = new [] {
                        new Example
                        {
                            Toc = [ new () { Text = "Get and Set the value of DropDown", Anchor = "#get-set-value" }, new () { Text = "Get and Set the value of DropDown using Value and Change event", Anchor = "#value-and-change-event" }, new () { Text = "Define Text and Value properties", Anchor = "#text-and-value-properties" }, new () { Text = "DropDown with template", Anchor = "#template" }, new () { Text = "Disable specific item", Anchor = "#disable-item" }, new () { Text = "Clear selected item", Anchor = "#clear-selected-item" }, new () { Text = "Editable DropDown", Anchor = "#editable-dropdown" }, new () { Text = "Open and close events", Anchor = "#open-and-close-event" }, new () { Text = "Keyboard Navigation", Anchor = "#keyboard-navigation" } ],
                            Name = "Single selection",
                            Path = "dropdown",
                            Title = "Blazor DropDown Component | Free UI Components by Radzen",
                            Description = "Demonstration and configuration of the Radzen Blazor DropDown component.",
                            Tags = new [] { "select", "picker", "form" , "edit", "dropdown" },
                        },
                        new Example
                        {
                            Toc = [ new () { Text = "Define max labels and selected items text", Anchor = "#define-max-labels-and-selected-items-text" }, new () { Text = "Specify an Equality Comparer for item selection. Useful when binding directly to an object collection.", Anchor = "#item-comparer" } ],
                            Name = "Multiple selection",
                            Path = "dropdown-multiple",
                            Title = "Blazor DropDown Component - Multiple Selection | Free UI Components by Radzen",
                            Description = "This example demonstrates multiple selection support in Radzen Blazor DropDown component.",
                            Tags = new [] { "select", "picker", "form" , "edit", "multiple", "dropdown" },
                        },
                        new Example
                        {
                            Name = "Virtualization",
                            Path = "dropdown-virtualization",
                            Title = "Blazor DropDown Component - Virtualization | Free UI Components by Radzen",
                            Description = "This example demonstrates virtualization using IQueryable.",
                            Tags = new [] { "select", "picker", "form" , "edit", "multiple", "dropdown", "virtualization", "paging" },
                        },
                        new Example
                        {
                            Name = "Filtering",
                            Path = "dropdown-filtering",
                            Title = "Blazor DropDown Component - Filtering | Free UI Components by Radzen",
                            Description = "This example demonstrates Blazor DropDown component filtering case sensitivity and filter operator.",
                            Tags = new [] { "select", "picker", "form" , "edit", "multiple", "dropdown", "filter" },
                        },
                        new Example
                        {
                            Name = "Grouping",
                            Path = "dropdown-grouping",
                            Title = "Blazor DropDown Component - Grouping | Free UI Components by Radzen",
                            Description = "This example demonstrates Blazor DropDown component with grouping.",
                            Tags = new [] { "select", "picker", "form" , "edit", "multiple", "dropdown", "grouping" },
                        },
                        new Example
                        {
                            Toc = [ new () { Text = "DropDown data binding to enum", Anchor = "#data-binding-to-enum" } ],
                            Name = "Custom objects binding",
                            Path = "dropdown-custom-objects",
                            Title = "Blazor DropDown Component - Custom Objects Binding | Free UI Components by Radzen",
                            Description = "This example demonstrates Blazor DropDown component binding to custom objects.",
                            Tags = new [] { "select", "picker", "form" , "edit", "dropdown", "custom" },
                        },
                    }
                },
                new Example
                {
                    Toc = [ new () { Text = "Get and Set the value of DropDownDataGrid", Anchor = "#get-set-value" }, new () { Text = "Get and Set the value of DropDownDataGrid using Value and Change event", Anchor = "#value-and-change-event" }, new () { Text = "Define Text and Value properties", Anchor = "#text-value-properties" }, new () { Text = "DropDownDataGrid with custom header, footer, value and item templates", Anchor = "#template" }, new () { Text = "Define multiple columns", Anchor = "#multiple-columns" }, new () { Text = "Filtering case sensitivity and filter operator", Anchor = "#filtering-case-sensitivity-and-filter-operator" }, new () { Text = "Multiple selection", Anchor = "#multiple-selection" }, new () { Text = "DropDownDataGrid binding to dynamic data", Anchor = "#dynamic" }, new () { Text = "Keyboard Navigation", Anchor = "#keyboard-navigation" } ],
                    Name = "DropDownDataGrid",
                    Path = "dropdown-datagrid",
                    Description = "Blazor DropDown component with columns and multiple selection support.",
                    Icon = "\ue99c",
                    Tags = new [] { "select", "picker", "form", "edit" }
                },
                new Example
                {
                    Toc = [ new () { Text = "Keyboard Navigation", Anchor = "#keyboard-navigation" } ],
                    Name = "Fieldset",
                    Path = "fieldset",
                    Description = "Demonstration and configuration of the Radzen Blazor Fieldset component.",
                    Icon = "\ue728",
                    Tags = new [] { "form", "container" }
                },
                new Example
                {
                    Toc = [ new () { Text = "Byte Array Support", Anchor = "#byte-array" }, new () { Text = "Keyboard Navigation", Anchor = "#keyboard-navigation" } ],
                    Name = "FileInput",
                    Path = "fileinput",
                    Description = "Blazor File input component with preview support.",
                    Icon = "\ue226",
                    Tags = new [] { "upload", "form", "edit" }
                },
                new Example
                {
                    Toc = [ new () { Text = "Variants", Anchor = "#variants" }, new () { Text = "Input types", Anchor = "#input-types" }, new () { Text = "Start, End, and ChildContent", Anchor = "#start-end-child-content" }, new () { Text = "Floating Label", Anchor = "#floating-label" }, new () { Text = "Helper text", Anchor = "#helper-text" }, new () { Text = "Validation", Anchor = "#form-field-validation" }, new () { Text = "Disabled FormField", Anchor = "#disabled-form-field" } ],
                    Name = "FormField",
                    Path = "form-field",
                    Description = "Radzen Blazor FormField component features a floating label effect. When the user focuses on an empty input field, the label floats above, providing a visual cue as to which field is being filled out.",
                    Icon = "\ue578",
                    Tags = new [] { "form", "label", "floating", "float", "edit", "outline", "input", "helper", "valid" }
                },
                new Example
                {
                    Toc = [ new () { Text = "Get and set the value", Anchor = "#get-set-value" }, new () { Text = "All tools", Anchor = "#all-tools" }, new () { Text = "Custom set of tools (text-editing only)", Anchor = "#custom-set-of-tools" }, new () { Text = "Upload images", Anchor = "#upload" }, new () { Text = "Focus", Anchor = "#focus" }, new () { Text = "Keyboard Navigation", Anchor = "#keyboard-navigation" } ],
                    Name="HtmlEditor",
                    Icon = "\ue3c9",
                    Children = new [] {
                        new Example
                        {
                            Name = "Default Tools",
                            Path = "html-editor",
                            Title = "Blazor HTML Editor Component | Free UI Components by Radzen",
                            Description = "Blazor HTML editor component with lots of built-in tools.",
                            Tags = new [] { "html", "editor", "rich", "text" }
                        },
                        new Example
                        {
                            Toc = [ new () { Text = "Custom command on Execute event", Anchor = "#command-execute-event" }, new () { Text = "Custom tool with template", Anchor = "#command-template" }, new () { Text = "Custom dialog", Anchor = "#command-dialog" } ],
                            Name = "Custom Tools",
                            Path = "html-editor-custom-tools",
                            Title = "Blazor HTML Editor Component - Custom Tools | Free UI Components by Radzen",
                            Description = "This example demonstrates Blazor HTML editor component with custom tools. RadzenHtmlEditor allows the developer to create custom tools via the RadzenHtmlEditorCustomTool tag.",
                            Tags = new [] { "html", "editor", "rich", "text", "tool", "custom" }
                        },
                    }
                },
                new Example
                {
                    Toc = [ new () { Text = "Get and Set the value of ListBox", Anchor = "#get-set-value" }, new () { Text = "Get and Set the value of ListBox using Value and Change event", Anchor = "#value-and-change-event" }, new () { Text = "Define Text and Value properties", Anchor = "#text-value-properties" }, new () { Text = "ListBox with template", Anchor = "#template" }, new () { Text = "Multiple selection", Anchor = "#multiple-selection" }, new () { Text = "Filtering case sensitivity and filter operator", Anchor = "#filtering" }, new () { Text = "Custom filtering with LoadData event", Anchor = "#loaddata-event" }, new () { Text = "Virtualization using IQueryable", Anchor = "#virtualization-using-iqueryable" }, new () { Text = "Virtualization with LoadData event", Anchor = "#virtualization-with-loaddata" }, new () { Text = "Keyboard Navigation", Anchor = "#keyboard-navigation" } ],
                    Name = "ListBox",
                    Path = "listbox",
                    Icon = "\ue0ee",
                    Description = "Demonstration and configuration of the Radzen Blazor ListBox component.",
                    Tags = new [] { "select", "picker", "form", "edit" }
                },
                new Example
                {
                    Name = "Mask",
                    Path = "mask",
                    Description = "Demonstration and configuration of the Radzen Blazor masked textbox component.",
                    Icon = "\ue262",
                    Tags = new [] { "input", "form", "edit", "mask" }
                },
                new Example
                {
                    Toc = [ new () { Text = "Get and Set the value of Numeric", Anchor = "#get-set-value" }, new () { Text = "Get and Set the value of Numeric using Value and Change event", Anchor = "#value-and-change-event" }, new () { Text = "Min set to 1 and Max set to 10", Anchor = "#min-max" }, new () { Text = "Placeholder and 0.5 step", Anchor = "#placeholder-and-step" }, new () { Text = "Without Up/Down", Anchor = "#without-up-down" }, new () { Text = "Formatted value", Anchor = "#formatted-value" }, new () { Text = "Align value", Anchor = "#align-value" }, new () { Text = "Custom Value convert", Anchor = "#custom-value-convert" }, new () { Text = "Custom Numeric Type Support", Anchor = "#custom-numeric-type" }, new () { Text = "Keyboard Navigation", Anchor = "#keyboard-navigation" } ],
                    Name = "Numeric",
                    Path = "numeric",
                    Description = "Demonstration and configuration of the Radzen Blazor numeric textbox component.",
                    Icon = "\uf04a",
                    Tags = new [] { "input", "number", "form", "edit" }
                },
                new Example
                {
                    Toc = [ new () { Text = "Get and Set the value of Password", Anchor = "#get-set-value" }, new () { Text = "Get and Set the value of Password using Value and Change event", Anchor = "#value-and-change-event" }, new () { Text = "Define placeholder", Anchor = "#placeholder" }, new () { Text = "Without auto-complete", Anchor = "#without-auto-complete" } ],
                    Name = "Password",
                    Path = "password",
                    Description = "Demonstration and configuration of the Radzen Blazor password textbox component.",
                    Icon = "\uf042",
                    Tags = new [] { "input", "form", "edit" }
                },
                new Example
                {
                    Toc = [ new () { Text = "Get and Set the value of RadioButtonList", Anchor = "#get-set-value" }, new () { Text = "Get and Set the value of RadioButtonList using Value and Change event", Anchor = "#value-and-change-event" }, new () { Text = "Set RadioButtonList orientation and layout", Anchor = "#orientation" }, new () { Text = "Populate RadioButtonList items from data", Anchor = "#populate-items" }, new () { Text = "Statically declared and populated RadioButtonList items from data", Anchor = "#populate-items-statically" }, new () { Text = "RadioButtonList with null value", Anchor = "#null-value" }, new () { Text = "Populate items programmatically and disable item", Anchor = "#populate-items-programmatically" }, new () { Text = "Keyboard Navigation", Anchor = "#keyboard-navigation" } ],
                    Name = "RadioButtonList",
                    Path = "radiobuttonlist",
                    Updated = true,
                    Description = "Demonstration and configuration of the Radzen Blazor radio button list component.",
                    Icon = "\ue837",
                    Tags = new [] { "toggle", "form", "edit" }
                },
                new Example
                {
                    Toc = [ new () { Text = "Get and Set the value of Rating", Anchor = "#get-set-value" }, new () { Text = "Get and Set the value of Rating using Value and Change event", Anchor = "#value-and-change-event" }, new () { Text = "Set number of stars", Anchor = "#number-of-stars" }, new () { Text = "Disabled Rating", Anchor = "#disabled-rating" }, new () { Text = "Read-only Rating", Anchor = "#readonly-rating" }, new () { Text = "Keyboard Navigation", Anchor = "#keyboard-navigation" } ],
                    Name = "Rating",
                    Path = "rating",
                    Description = "Demonstration and configuration of the Radzen Blazor Rating component.",
                    Icon = "\ue839",
                    Tags = new [] { "star", "form", "edit" }
                },
                new Example
                {
                    Name = "SecurityCode",
                    Path = "security-code",
                    Description = "Demonstration and configuration of the Radzen Blazor SecurityCode component.",
                    Icon = "\uf045",
                    Tags = new [] { "security", "code", "input" }
                },
                new Example
                {
                    Toc = [ new () { Text = "Get and Set the value of SelectBar", Anchor = "#get-set-value" }, new () { Text = "Get and Set the value of SelectBar using Value and Change event", Anchor = "#value-and-change-event" }, new () { Text = "Multiple selection", Anchor = "#multiple-selection" }, new () { Text = "Populate SelectBar items from data", Anchor = "#populate-from-data" }, new () { Text = "Statically declared and populated SelectBar items from data", Anchor = "#populate-items-statically" }, new () { Text = "Populate items programmatically and disable item", Anchor = "#populate-items-programmatically" }, new () { Text = "SelectBar with icons", Anchor = "#icons" }, new () { Text = "SelectBar with images", Anchor = "#images" }, new () { Text = "SelectBar with template", Anchor = "#template" }, new () { Text = "SelectBar Size", Anchor = "#size" }, new () { Text = "SelectBar Orientation", Anchor = "#orientation" }, new () { Text = "Keyboard Navigation", Anchor = "#keyboard-navigation" } ],
                    Name = "SelectBar",
                    Path = "selectbar",
                    Updated = true,
                    Description = "Demonstration and configuration of the Radzen Blazor SelectBar component.",
                    Icon = "\uf8e8",
                    Tags = new [] { "form", "edit" }
                },
                new Example
                {
                    Toc = [ new () { Text = "Get and Set the value of Slider", Anchor = "#get-set-value" }, new () { Text = "Get and Set the value of Slider using Value and Change event", Anchor = "#value-and-change-event" }, new () { Text = "Slider from -100 to 100", Anchor = "#min-max-value" }, new () { Text = "Slider with Step=10", Anchor = "#step" }, new () { Text = "Range Slider", Anchor = "#range-slider" }, new () { Text = "Disabled Slider", Anchor = "#disabled-slider" }, new () { Text = "Vertical Slider", Anchor = "#vertical-slider" }, new () { Text = "Keyboard Navigation", Anchor = "#keyboard-navigation" } ],
                    Name = "Slider",
                    Path = "slider",
                    Description = "Demonstration and configuration of the Radzen Blazor Slider component.",
                    Icon = "\ue429",
                    Tags = new [] { "form", "slider" }
                },
                new Example
                {
                    Toc = [ new () { Text = "Keyboard Navigation", Anchor = "#keyboard-navigation" } ],
                    Name = "SpeechToTextButton",
                    Description = "Demonstration and configuration of the Radzen Blazor speech to text button component.",
                    Path = "speechtotextbutton",
                    Icon = "\ue029"
                },
                new Example
                {
                    Toc = [ new () { Text = "Filled SplitButton", Anchor = "#filled" }, new () { Text = "Flat SplitButton", Anchor = "#flat" }, new () { Text = "Outlined SplitButton", Anchor = "#outlined" }, new () { Text = "Text SplitButton", Anchor = "#text" }, new () { Text = "Content in SplitButton", Anchor = "#content" }, new () { Text = "SplitButton Sizes", Anchor = "#sizes" }, new () { Text = "Disabled SplitButton", Anchor = "#disabled" }, new () { Text = "Busy SplitButton", Anchor = "#busy" }, new () { Text = "AlwaysOpenPopup SplitButton", Anchor = "#always-open-popup" }, new () { Text = "DropDown icon of SplitButton", Anchor = "#customize-dropdown-icon" }, new () { Text = "Keyboard Navigation", Anchor = "#keyboard-navigation" } ],
                    Name = "SplitButton",
                    Description = "Demonstration and configuration of the Radzen Blazor split button component",
                    Path = "splitbutton",
                    Icon = "\uf756"
                },
                new Example
                {
                    Toc = [ new () { Text = "Get and set the value", Anchor = "#get-set-value" }, new () { Text = "Keyboard Navigation", Anchor = "#keyboard-navigation" }, new () { Text = "Disabled Switch", Anchor = "#disabled-switch" } ],
                    Name = "Switch",
                    Path = "switch",
                    Description = "Demonstration and configuration of the Radzen Blazor Switch component.",
                    Icon = "\ue9f6",
                    Tags = new [] { "form", "edit", "switch" }
                },
                new Example
                {
                    Name = "TemplateForm",
                    Path = "templateform",
                    Description = "Demonstration and configuration of the Radzen Blazor template form component with validation support.",
                    Icon = "\uebed",
                    Tags = new [] { "form", "edit" }
                },
                new Example
                {
                    Name = "TextArea",
                    Path = "textarea",
                    Description = "Demonstration and configuration of the Radzen Blazor TextArea component.",
                    Icon = "\ue167",
                    Tags = new [] { "input", "form", "edit" }
                },
                new Example
                {
                    Name = "TextBox",
                    Path = "textbox",
                    Description = "Demonstration and configuration of the Radzen Blazor TextBox input component.",
                    Icon = "\ue9f1",
                    Tags = new [] { "input", "form", "edit" }
                },
                new Example
                {
                    Toc = [ new () { Text = "Bind the value of TimeSpanPicker", Anchor = "#bind-value" }, new () { Text = "Get and Set the value of TimeSpanPicker using Value and Change event.", Anchor = "#value-and-change-event" }, new () { Text = "Min and Max values", Anchor = "#min-max-values" }, new () { Text = "Inline picker", Anchor = "#inline" }, new () { Text = "Various configurations", Anchor = "#various-config" }, new () { Text = "Time span format", Anchor = "#format" }, new () { Text = "Custom input parsing", Anchor = "#custom-input-parsing" }, new () { Text = "Keyboard Navigation", Anchor = "#keyboard-navigation" } ],
                    Name = "TimeSpanPicker",
                    Path = "timespanpicker",
                    New = true,
                    Description = "Demonstration and configuration of the Radzen Blazor TimeSpanPicker component.",
                    Icon = "\ue425",
                    Tags = new [] { "duration", "form", "edit" }
                },
                new Example
                {
                    Toc = [  
                        new () { Text = "Upload files", Anchor = "#change" }, 
                        new () { Text = "Upload files to server", Anchor = "#url" }, 
                        new () { Text = "Upload multiple files", Anchor = "#multiple" }, 
                        new () { Text = "Trigger from code", Anchor = "#from-code" }, 
                        new () { Text = "File filter", Anchor = "#filter" }, 
                        new () { Text = "Use parameters", Anchor = "#parameters" }, 
                        new () { Text = "Show upload progress", Anchor = "#progress" }, 
                        new () { Text = "Drag and drop to upload", Anchor = "#drag-drop" }, 
                        new () { Text = "Custom HTTP headers", Anchor = "#custom-headers" }, 
                        new () { Text = "Specify parameter name", Anchor = "#parameter-name" }, 
                        new () { Text = "Keyboard Navigation", Anchor = "#keyboard-navigation" } 
                    ],
                    Name = "Upload",
                    Description = "Demonstration and configuration of the Radzen Blazor Upload component.",
                    Path = "example-upload",
                    Icon = "\uf09b",
                    Tags = new [] { "upload", "file"}
                },
            },
        },
        new Example
        {
            Name = "Data Visualization",
            Icon = "\ue4fb",
            Children = new[] {
                new Example
                {
                    Name="Chart",
                    Icon = "\ue922",
                    Children = new [] {
                        new Example
                        {
                            Toc = [ new () { Text = "Chart Series", Anchor = "#series" }, new () { Text = "Basic usage", Anchor = "#series" }, new () { Text = "Keyboard Navigation", Anchor = "#keyboard-navigation" } ],
                            Name = "Series",
                            Path = "chart-series",
                            Title = "Blazor Chart Component - Series Configuration | Free UI Components by Radzen",
                            Description = "Use the Radzen Blazor Chart component to display data in a graphical format.",
                            Tags = new [] { "chart", "graph", "series" }
                        },
                        new Example
                        {
                            Name = "Area Chart",
                            Path = "area-chart",
                            Description = "Radzen Blazor Chart with area series.",
                            Tags = new [] { "chart", "graph", "area" }
                        },
                        new Example
                        {
                            Name = "Bar Chart",
                            Path = "bar-chart",
                            Description = "Radzen Blazor Chart with bar series.",
                            Tags = new [] { "chart", "graph", "column", "bar" }
                        },
                        new Example
                        {
                            Name = "Column Chart",
                            Path = "column-chart",
                            Description = "Radzen Blazor Chart with column series.",
                            Tags = new [] { "chart", "graph", "column", "bar" }
                        },
                        new Example
                        {
                            Name = "Donut Chart",
                            Path = "donut-chart",
                            Description = "Radzen Blazor Chart with donut series.",
                            Tags = new [] { "chart", "graph", "donut" }
                        },
                        new Example
                        {
                            Name = "Line Chart",
                            Path = "line-chart",
                            Description = "Radzen Blazor Chart with line series.",
                            Tags = new [] { "chart", "graph", "line" }
                        },
                        new Example
                        {
                            Name = "Pie Chart",
                            Path = "pie-chart",
                            Description = "Radzen Blazor Chart with pie series.",
                            Tags = new [] { "chart", "graph", "pie" }
                        },
                        new Example
                        {
                            Name = "Stacked Area Chart",
                            Path = "stacked-area-chart",
                            Description = "Radzen Blazor Chart with stacked area series.",
                            Tags = new [] { "chart", "stack", "graph", "area" }
                        },
                        new Example
                        {
                            Name = "Stacked Bar Chart",
                            Path = "stacked-bar-chart",
                            Description = "Radzen Blazor Chart with stacked bar series.",
                            Tags = new [] { "chart", "stack", "graph", "column", "bar" }
                        },
                        new Example
                        {
                            Name = "Stacked Column Chart",
                            Path = "stacked-column-chart",
                            Description = "Radzen Blazor Chart with stacked column series.",
                            Tags = new [] { "chart", "stack", "graph", "column", "bar" }
                        },
                        new Example
                        {
                            Toc = [ new () { Text = "Min, max and step", Anchor = "#min-max-and-step" }, new () { Text = "Format axis values", Anchor = "#format-axis-values" }, new () { Text = "Display grid lines", Anchor = "#display-grid-lines" }, new () { Text = "Set axis title", Anchor = "#set-axis-title" } ],
                            Name = "Axis",
                            Path = "chart-axis",
                            Title = "Blazor Chart Component - Axis Configuration | Free UI Components by Radzen",
                            Description = "By default the Radzen Blazor Chart determines the Y axis minimum and maximum based on the range of values.",
                            Tags = new [] { "chart", "graph", "series" }
                        },
                        new Example
                        {
                            Toc = [ new () { Text = "Legend position", Anchor = "#legend-position" }, new () { Text = "Hide the legend", Anchor = "#hide-the-legend" } ],
                            Name = "Legend",
                            Path = "chart-legend",
                            Title = "Blazor Chart Component - Legend Configuration | Free UI Components by Radzen",
                            Description = "The Radzen Blazor Chart displays a legend by default. It uses the Title property of the series (or category values for pie series) as items in the legend.",
                            Tags = new [] { "chart", "graph", "legend" }
                        },
                        new Example
                        {
                            Toc = [ new () { Text = "Customize tooltip content", Anchor = "#customize-tooltip-content" }, new () { Text = "Disable tooltips", Anchor = "#disable-tooltips" } ],
                            Name = "ToolTip",
                            Path = "chart-tooltip",
                            Title = "Blazor Chart Component - ToolTip Configuration | Free UI Components by Radzen",
                            Description = "The Radzen Blazor Chart displays a tooltip when the user hovers series with the mouse. The tooltip by default includes the series category, value and series name.",
                            Tags = new [] { "chart", "graph", "legend" }
                        },
                        new Example
                        {
                            Toc = [ new () { Text = "Auto Rotation", Anchor = "#auto-rotation" }, new () { Text = "Predefined Rotation", Anchor = "#rotation" } ],
                            Name = "Label Rotation",
                            Path = "chart-label-rotation",
                            Title = "Blazor Chart Component - Label Rotation | Free UI Components by Radzen",
                            Description = "The Radzen Blazor Chart can rotate the labels of the horizontal axis.",
                            New = true,
                            Tags = new [] { "chart", "label", "rotate", "rotation" }
                        },
                        new Example
                        {
                            Name = "Trends",
                            Path = "chart-trends",
                            Title = "Blazor Chart Component - Trends | Free UI Components by Radzen",
                            Description = "The mean, median and mode are measures of central tendency. Under different conditions, some measures of central tendency become more appropriate to use than others.",
                            Tags = new [] { "chart", "trend", "median", "mean", "mode" }
                        },
                        new Example
                        {
                            Name = "Annotations",
                            Path = "chart-annotations",
                            Title = "Blazor Chart Component - Annotations | Free UI Components by Radzen",
                            Description = "This example demonstrates RadzenSeriesAnnotation.",
                            Tags = new [] { "chart", "annotation", "label" }
                        },
                        new Example
                        {
                            Name = "Interpolation",
                            Path = "chart-interpolation",
                            Title = "Blazor Chart Component - Interpolation | Free UI Components by Radzen",
                            Description = "This example demonstrates Radzen Blazor Chart interpolation mode.",
                            Tags = new [] { "chart", "interpolation", "spline", "step" }
                        },
                        new Example
                        {
                            Name = "Styling Chart",
                            Path = "styling-chart",
                            Title = "Blazor Chart Component - Styling | Free UI Components by Radzen",
                            Description = "This example demonstrates different color schemes, custom colors and styling of Radzen Blazor Chart component.",
                            Tags = new [] { "chart", "graph", "styling" }
                        },
                    }
                },
                new Example
                {
                    Name = "Sparkline",
                    Path = "sparkline",
                    Description = "Demonstration and configuration of RadzenSparkline component.",
                    Icon = "\uf64f",
                    Tags = new [] { "chart", "sparkline" }
                },
                new Example
                {
                    Name = "Arc Gauge",
                    Path = "arc-gauge",
                    Description = "Demonstration and configuration of Radzen Blazor Arc Gauge component.",
                    Icon = "\ue9e4",
                    Tags = new [] { "gauge", "graph", "arc", "progress" }
                },
                new Example
                {
                    Name = "Radial Gauge",
                    Path = "radial-gauge",
                    Description = "Demonstration and configuration of Radzen Blazor Radial Gauge component.",
                    Icon = "\ue01b",
                    Tags = new [] { "gauge", "graph", "radial", "circle" }
                },
                new Example
                {
                    Name = "Styling Gauge",
                    Path = "styling-gauge",
                    Title = "Blazor Gauge Component - Styling | Free UI Components by Radzen",
                    Description = "This example demonstrates multiple pointers with RadzenRadialGauge and multiple scales with RadzenArcGauge component.",
                    Icon = "\ue41d",
                    Tags = new [] { "gauge", "graph", "styling" }
                },
                new Example
                {
                    Toc = [ new () { Text = "Basic Usage", Anchor = "#basic-usage" }, new () { Text = "Orientation and Position", Anchor = "#orientation-and-position" }, new () { Text = "Align Items", Anchor = "#align-items" }, new () { Text = "Styling", Anchor = "#line-width" }, new () { Text = "Point Size", Anchor = "#point-size" }, new () { Text = "Point Style", Anchor = "#point-style" }, new () { Text = "Point Variant", Anchor = "#point-variant" }, new () { Text = "Point Shadow", Anchor = "#point-shadow" }, new () { Text = "Point Content", Anchor = "#point-content" } ],
                    Name = "Timeline",
                    Path = "timeline",
                    Description = "Demonstration and configuration of Radzen Blazor Timeline component. RadzenTimeline component is a graphical representation used to display a chronological sequence of events or data points.",
                    Icon = "\ue00d",
                    Tags = new [] { "timeline", "time", "line" }
                },
                new Example
                {
                    Name = "GoogleMap",
                    Path = "googlemap",
                    Description = "Demonstration and configuration of Radzen Blazor Google Map component.",
                    Icon = "\ue55b"
                },
                new Example
                {
                    Toc = [ new () { Text = "Parameters", Anchor = "#parameters" }, new () { Text = "Proxy", Anchor = "#proxy" }, new () { Text = "Provide credentials", Anchor = "#credentials" } ],
                    Name = "SSRS Viewer",
                    Path = "ssrsviewer",
                    Description = "Demonstration and configuration of Radzen SSRS Viewer Radzen Blazor Arc Gauge component.",
                    Icon = "\ue9e4",
                    Tags = new [] { "report", "ssrs" }
                },
                new Example
                {
                    Name = "Sankey Diagram",
                    Path = "sankey-diagram",
                    Description = "Radzen Blazor Sankey Diagram for visualizing flow and relationships between nodes.",
                    Icon = "\uf38d",
                    Tags = new [] { "sankey", "flow", "diagram", "visualization", "relationships" },
                    New = true
                },
            }
        },
        new Example
        {
            Name = "Feedback",
            Icon = "\ue0cb",
            Children = new[] {
                new Example
                {
                    Name = "Alert",
                    Title = "Blazor Alert component",
                    Icon = "\ue88e",
                    Tags = new [] { "message", "alert" },
                    Children = new [] {
                        new Example
                        {
                            Name = "Alert Configuration",
                            Title = "Blazor Alert Component | Free UI Components by Radzen",
                            Description = "Demonstration and configuration of the Radzen Blazor Alert component.",
                            Path = "alert",
                            Tags = new [] { "message", "alert" },
                        },
                        new Example
                        {
                            Name = "Alert Styling",
                            Title = "Blazor Alert Component - Styling | Free UI Components by Radzen",
                            Description = "This example demonstrates different styles, shades and variants of Radzen Blazor Alert component.",
                            Path = "alert-styling",
                            Tags = new [] { "message", "alert" },
                        }
                    }
                },
                new Example
                {
                    Toc = [ new () { Text = "Badge Style", Anchor = "#badge-style" }, new () { Text = "Badge Shade", Anchor = "#badge-shade" }, new () { Text = "Badge Variant", Anchor = "#badge-variant" }, new () { Text = "Pill", Anchor = "#pill" }, new () { Text = "Child Content", Anchor = "#child-content" } ],
                    Name = "Badge",
                    Path = "badge",
                    Description = "The Radzen Blazor Badge component is a small graphic that displays important information, like a count or label, within a user interface. It's commonly used to draw attention to something or provide visual feedback to the user.",
                    Icon = "\uf7f1",
                    Tags = new[] { "badge", "link"}
                },
                new Example
                {
                    Toc = [ new () { Text = "Severity", Anchor = "#severity" }, new () { Text = "Position", Anchor = "#position" }, new () { Text = "Custom click handler", Anchor = "#click-handler" }, new () { Text = "Custom content", Anchor = "#custom-content" }, new () { Text = "Duration progress", Anchor = "#duration-progress" } ],
                    Name = "Notification",
                    Path = "notification",
                    Description = "Demonstration and configuration of the Radzen Blazor Notification component.",
                    Icon = "\ue87f",
                    Tags = new [] { "message", "notification" }
                },
                new Example
                {
                    Toc = [ new () { Text = "ProgressBar in determinate mode, Get and Set the value", Anchor = "#progressbar-determinate" }, new () { Text = "ProgressBar in indeterminate mode", Anchor = "#progressbar-indeterminate" }, new () { Text = "ProgressBar Min and Max values", Anchor = "#progressbar-min-max-values" } ],
                    Name = "ProgressBar",
                    Description = "Demonstration and configuration of the Radzen Blazor ProgressBar component.",
                    Path = "progressbar",
                    Icon = "\ue9e3",
                    Tags = new [] { "progress", "spinner", "bar", "linear" }
                },
                new Example
                {
                    Toc = [ new () { Text = "ProgressBarCircular in determinate mode, Get and Set the value", Anchor = "#progressbarcircular-determinate" }, new () { Text = "ProgressBarCircular in indeterminate mode", Anchor = "#progressbarcircular-indeterminate" }, new () { Text = "ProgressBarCircular sizes", Anchor = "#progressbarcircular-sizes" }, new () { Text = "ProgressBarCircular Min and Max values", Anchor = "#progressbarcircular-min-max-values" } ],
                    Name = "ProgressBarCircular",
                    Description = "Demonstration and configuration of the Radzen Blazor circular progress bar component.",
                    Path = "progressbarcircular",
                    Icon = "\ue9d0",
                    Tags = new [] { "progress", "spinner", "circle", "circular" }
                },
                new Example
                {
                    Toc = [ new () { Text = "Show tooltip with string message", Anchor = "#tooltip-string-message" }, new () { Text = "Tooltip positions", Anchor = "#tooltip-positions" }, new () { Text = "Show tooltip with HTML content", Anchor = "#tooltip-html-content" }, new () { Text = "Tooltip delay and duration", Anchor = "#tooltip-delay-duration" }, new () { Text = "Close Tooltip on page click", Anchor = "#tooltip-close-on-page-click" }, new () { Text = "Tooltip on HTML element", Anchor = "#tooltip-html-element" } ],
                    Name = "Tooltip",
                    Description = "The Radzen Blazor Tooltip component is a small pop-up box that appears when the user hovers or clicks on a UI element. It is commonly used to provide additional information or context to the user.",
                    Path = "tooltip",
                    Icon = "\ue9f8",
                    Tags = new [] { "popup", "tooltip" }
                },
            }
        },
        new Example
        {
            Name = "Validators",
            Icon = "\uf1c2",
            Children = new[] {
                new Example
                {
                    Toc = [ new () { Text = "Basic Usage", Anchor = "#basic-usage" }, new () { Text = "Coditional Validation", Anchor = "#conditional-validation" }, new () { Text = "Comparison operator", Anchor = "#comparison-operator" }, new () { Text = "Appearance", Anchor = "#appearance" } ],
                    Name = "CompareValidator",
                    Path = "comparevalidator",
                    Description = "The Blazor RadzenCompareValidator compares the user input against a predefined value or another component.",
                    Icon = "\ue877",
                    Tags = new [] { "validator", "validation", "required", "compare"}
                },
                new Example
                {
                    Name = "DataAnnotationValidator",
                    Path = "dataannotationvalidator",
                    Description = "Demonstration and configuration of the Radzen Blazor Data Annotation Validator component.",
                    Icon = "\ue6b3",
                    Tags = new [] { "validator", "validation", "pattern", "annotations" }
                },
                new Example
                {
                    Name = "EmailValidator",
                    Path = "emailvalidator",
                    Description = "Demonstration and configuration of the Radzen Blazor Email Validator component.",
                    Icon = "\uf18c",
                    Tags = new [] { "validator", "validation", "required", "email"}
                },
                new Example
                {
                    Name = "LengthValidator",
                    Path = "lengthvalidator",
                    Description = "Demonstration and configuration of the Radzen Blazor Length Validator component.",
                    Icon = "\ue90a",
                    Tags = new [] { "validator", "validation", "required", "length"}
                } ,
                new Example
                {
                    Name = "NumericRangeValidator",
                    Path = "numericrangevalidator",
                    Description = "Demonstration and configuration of the Radzen Blazor Numeric Range Validator component.",
                    Icon = "\ue3d0",
                    Tags = new [] { "validator", "validation", "required", "range"}
                },
                new Example
                {
                    Name = "RegexValidator",
                    Path = "regexvalidator",
                    Description = "Demonstration and configuration of the Radzen Blazor Regex Validator component.",
                    Icon = "\ue53f",
                    Tags = new [] { "validator", "validation", "pattern", "regex", "regular", "expression"}
                },
                new Example
                {
                    Toc = [ new () { Text = "Validate RadzenDropDown", Anchor = "#validate-radzendropdown" } ],
                    Name = "RequiredValidator",
                    Path = "requiredvalidator",
                    Description = "Demonstration and configuration of the Radzen Blazor Required Validator component.",
                    Icon = "\ue578",
                    Tags = new [] { "validator", "validation", "required"}
                },
                new Example
                {
                    Name = "CustomValidator",
                    Path = "customvalidator",
                    Description = "Demonstration and configuration of the Radzen Blazor Custom Validator component.",
                    Icon = "\ue7d8",
                    Tags = new [] { "validator", "validation", "custom", "unique"}
                },
            }
        },
        new Example
        {
            Name = "Changelog",
            Path = "/changelog",
            Updated = true,
            Title = "Track and review changes to Radzen Blazor Components",
            Description = "See what's new in Radzen Blazor Components",
            Icon = "\ue8e1"
        },
    };

        public IEnumerable<Example> Examples
        {
            get
            {
                return allExamples;
            }
        }

        public IEnumerable<Example> Filter(string term)
        {
            if (string.IsNullOrEmpty(term))
                return allExamples;

            bool contains(string value) => value != null && value.Contains(term, StringComparison.OrdinalIgnoreCase);

            bool filter(Example example) => contains(example.Name) || (example.Tags != null && example.Tags.Any(contains));

            bool deepFilter(Example example) => filter(example) || example.Children?.Any(filter) == true;

            return Examples.Where(category => category.Children?.Any(deepFilter) == true || filter(category))
                           .Select(category => new Example
                           {
                               Name = category.Name,
                               Path = category.Path,
                               Icon = category.Icon,
                               Expanded = true,
                               Children = category.Children?.Where(deepFilter).Select(example => new Example
                               {
                                   Name = example.Name,
                                   Path = example.Path,
                                   Icon = example.Icon,
                                   Expanded = true,
                                   Children = example.Children
                               }
                               ).ToArray()
                           }).ToList();
        }

        public Example FindCurrent(Uri uri)
        {
            IEnumerable<Example> Flatten(IEnumerable<Example> e)
            {
                return e.SelectMany(c => c.Children != null ? Flatten(c.Children) : new[] { c });
            }

            return Flatten(Examples)
                        .FirstOrDefault(example => example.Path == uri.AbsolutePath || $"/{example.Path}" == uri.AbsolutePath);
        }

        public string TitleFor(Example example)
        {
            if (example != null && example.Name != "Overview")
            {
                return example.Title ?? $"Blazor {example.Name} Component | Free UI Components by Radzen";
            }

            return "Free Blazor Components | 90+ UI controls by Radzen";
        }

        public string DescriptionFor(Example example)
        {
            return example?.Description ?? "The Radzen Blazor component library provides more than 90 UI controls for building rich ASP.NET Core web applications.";
        }
    }
}
