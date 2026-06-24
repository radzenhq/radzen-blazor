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
            Description = "145+ free, open-source Blazor UI components for data-rich web apps. DataGrid, Scheduler, Charts, Forms, and more. MIT licensed.",
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
            Title = "AI and Radzen Blazor | Free UI Components by Radzen",
            Description = "Learn now how to integrate AI with the Radzen Blazor Components library.",
            Icon = "\uefac",
            Tags = new [] { "chat", "ai", "conversation", "message", "streaming", "mcp", "nuget" }
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

            Toc = [ new () { Text = "Keyboard Navigation", Anchor = "#keyboard-navigation" } ],
            Name = "DataGrid",
            Icon = "\uf191",
            Children = new [] {
                new Example
                {
                    Name = "Overview",
                    Path = "datagrid",
                    Title = "Blazor DataGrid Component | Free UI Components by Radzen",
                    Description = "A free, open-source Blazor DataGrid with sorting, filtering, paging, grouping, virtualization, inline editing, and Excel/CSV export. Bind to IQueryable, Entity Framework, OData, or any data source.",
                    Tags = new [] { "datagrid", "datatable", "datagridview", "grid", "table", "overview" },
                    Faq = new []
                    {
                        new FaqItem { Question = "How do I bind data to the Blazor DataGrid?", Answer = "Set the Data property to an IQueryable or IEnumerable and define columns; for remote data use the LoadData event or bind to an OData service. With IQueryable and Entity Framework, paging, sorting, and filtering run as part of the query." },
                        new FaqItem { Question = "Does the DataGrid page, sort, and filter on the server?", Answer = "Yes. Bound to an IQueryable (such as Entity Framework) or via LoadData, it translates paging, sorting, and filtering into the query so only the current page is fetched." },
                        new FaqItem { Question = "Can the DataGrid handle large datasets?", Answer = "Yes. Turn on virtualization or use server-side paging so only the visible rows are rendered and fetched, which keeps it fast on very large sets." },
                        new FaqItem { Question = "Can I export the DataGrid to Excel?", Answer = "Yes. The grid exports to Excel and CSV." }
                    }
                },
                new Example
                {
                    Name = "Data-binding",
                    Icon = "\ue3ec",
                    Children = new [] {
                        new Example
                        {
                            Name = "IQueryable",
                            Title = "Blazor DataGrid - IQueryable Data Source | Free UI Components by Radzen",
                            Description = "Use RadzenDataGrid to display tabular data with ease. Perform paging, sorting and filtering through Entity Framework without extra code.",
                            Path = "datagrid-iqueryable",
                            Tags = new [] { "datatable", "datagridview", "dataview", "grid", "table" }
                        },
                        new Example
                        {
                            Name = "LoadData event",
                            Path = "datagrid-loaddata",
                            Related = new [] { "datagrid-odata", "datagrid-virtualization-loaddata", "datagrid-iqueryable" },
                            Faq = new []
                            {
                                new FaqItem { Question = "When should I use the LoadData event?", Answer = "Use LoadData when the data comes from a web API or any source you query yourself - the grid passes you the current page, sort, and filter, and you return that page plus the total count." },
                                new FaqItem { Question = "How is IQueryable binding different from LoadData?", Answer = "With IQueryable, such as Entity Framework, the grid builds and runs the query for you. With LoadData you run the query yourself and return the page, which suits REST APIs and custom back ends." }
                            },
                            Title = "Blazor DataGrid - LoadData Event | Free UI Components by Radzen",
                            Description = "Blazor Data Grid custom data-binding via the LoadData event.",
                            Tags = new [] { "datagrid", "bind", "load", "data", "loaddata", "custom" }
                        },
                        new Example
                        {
                            Name = "OData service",
                            Path = "datagrid-odata",
                            Related = new [] { "datagrid-loaddata", "datagrid-checkboxlist-filter-odata", "datagrid-iqueryable" },
                            Faq = new []
                            {
                                new FaqItem { Question = "How do I bind a DataGrid to OData?", Answer = "Point the grid at the OData endpoint; it turns paging, sorting, and filtering into $skip, $top, $orderby, and $filter so the server returns only the rows in view." }
                            },
                            Title = "Blazor DataGrid - OData Service | Free UI Components by Radzen",
                            Description = "Blazor Data Grid supports data-binding to OData.",
                            Tags = new [] { "datagrid", "bind", "load", "data", "loaddata", "odata", "service", "rest" }
                        },
                        new Example
                        {
                            Name = "Dynamic data",
                            Path = "datagrid-dynamic",
                            Related = new [] { "datagrid-datatable", "datagrid-iqueryable", "datagrid-column-template" },
                            Faq = new []
                            {
                                new FaqItem { Question = "Can the DataGrid display data without a C# model?", Answer = "Yes. Bind to dynamic rows such as dictionaries or ExpandoObject and declare columns at runtime by property name." }
                            },
                            Title = "Blazor DataGrid - Dynamic Data | Free UI Components by Radzen",
                            Description = "Blazor Data Grid supports dynamic data sources.",
                            Tags = new [] { "datagrid", "bind", "load", "data", "loaddata", "dynamic" }
                        },
                        new Example
                        {
                            Name = "DataTable data",
                            Path = "datagrid-datatable",
                            Related = new [] { "datagrid-dynamic", "datagrid-iqueryable" },
                            Faq = new []
                            {
                                new FaqItem { Question = "How do I bind a DataGrid to a DataTable?", Answer = "Bind the grid to the DataTable and map each column by field name; sorting, filtering, and paging work as usual." }
                            },
                            Title = "Blazor DataGrid - DataTable Data | Free UI Components by Radzen",
                            Description = "Blazor Data Grid supports DataTable sources.",
                            Tags = new [] { "datagrid", "bind", "load", "data", "loaddata", "datatable" }
                        },
                        new Example
                        {
                            Name = "Real-time data",
                            Path = "datagrid-realtime",
                            Related = new [] { "datagrid-iqueryable", "datagrid-loaddata" },
                            Faq = new []
                            {
                                new FaqItem { Question = "How do I refresh the DataGrid with real-time data?", Answer = "Update the bound collection as data arrives and call the grid's Reload to re-render the current page." }
                            },
                            Title = "Blazor DataGrid - Real-time Data | Free UI Components by Radzen",
                            Description = "Blazor Data Grid with real-time data sources.",
                            Tags = new [] { "datagrid", "bind", "load", "data", "loaddata", "real-time" }
                        },
                        new Example
                        {
                            Name = "Crosstab data",
                            Path = "datagrid-crosstab",
                            Related = new [] { "datagrid-grouping-api", "datagrid-footer-totals", "datagrid-iqueryable" },
                            Faq = new []
                            {
                                new FaqItem { Question = "Can the DataGrid show a crosstab or pivot layout?", Answer = "Yes - project each cross dimension into its own column and define those columns; for a full pivot UI with row and column groups, use the PivotDataGrid." }
                            },
                            Title = "Blazor DataGrid - Crosstab Data | Free UI Components by Radzen",
                            Description = "Blazor Data Grid supports crosstab data sources.",
                            Tags = new [] { "datagrid", "bind", "load", "data", "loaddata", "crosstab", "rows", "columns" }
                        },
                        new Example
                        {
                            Name = "Performance",
                            Path = "datagrid-performance",
                            Related = new [] { "datagrid-virtualization", "datagrid-loaddata", "datagrid-iqueryable" },
                            Faq = new []
                            {
                                new FaqItem { Question = "How do I make the DataGrid fast with large datasets?", Answer = "Enable virtualization or server-side paging via IQueryable or LoadData so only the visible rows render, and keep cell templates simple." },
                                new FaqItem { Question = "How many rows can the DataGrid handle?", Answer = "With virtualization or server-side paging it stays responsive on very large sets, since it only renders and fetches the rows currently in view." }
                            },
                            Title = "Blazor DataGrid - Performance | Free UI Components by Radzen",
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
                            Title = "Blazor DataGrid - Virtualization | Free UI Components by Radzen",
                            Description = "Virtualization allows you to render large amounts of data on demand. The RadzenDataGrid component uses Entity Framework to query the visible data.",
                            Tags = new [] { "datagrid", "bind", "load", "data", "virtualization", "ondemand" }
                        },
                        new Example
                        {
                            Name = "LoadData support",
                            Path = "datagrid-virtualization-loaddata",
                            Title = "Blazor DataGrid - LoadData Virtualization | Free UI Components by Radzen",
                            Description = "RadzenDataGrid supports virtualization with custom data-binding scenarios. Handle the LoadData event as usual.",
                            Tags = new [] { "datagrid", "bind", "load", "data", "loaddata", "virtualization", "ondemand" }
                        },
                    }
                },
                new Example
                {
                    Name = "Columns",
                    Icon = "\ue336",
                    Children = new []
                    {
                        new Example
                        {
                            Name = "Template",
                            Path = "datagrid-column-template",
                            Related = new [] { "datagrid-composite-columns", "datagrid-conditional-columns-render", "datagrid-inline-edit" },
                            Faq = new []
                            {
                                new FaqItem { Question = "How do I customize a DataGrid column's cell content?", Answer = "Add a Template to the column and put any Razor markup inside; you get the row item as context to bind to." }
                            },
                            Title = "Blazor DataGrid - Column Template | Free UI Components by Radzen",
                            Description = "Blazor Data Grid custom appearance via column templates. The Template allows you to customize the way data is displayed.",
                            Tags = new [] { "column", "template", "grid", "datagrid", "table"}
                        },
                        new Example
                        {
                            Name = "Resizing",
                            Path = "datagrid-column-resizing",
                            Related = new [] { "datagrid-column-reorder", "datagrid-frozen-columns", "datagrid-column-picker" },
                            Faq = new []
                            {
                                new FaqItem { Question = "How do I enable column resizing in the DataGrid?", Answer = "Set AllowColumnResize to true on the grid; users can then drag column borders to resize, and you can set each column's Width for the initial layout." }
                            },
                            Title = "Blazor DataGrid - Column Resizing | Free UI Components by Radzen",
                            Description = "Enable column resizing in RadzenDataGrid by setting the AllowColumnResizing property to true.",
                            Tags = new [] { "column", "resizing", "grid", "datagrid", "table"}
                        },
                        new Example
                        {
                            Name = "Column Picker",
                            Path = "datagrid-column-picker",
                            Related = new [] { "datagrid-column-reorder", "datagrid-column-resizing", "datagrid-frozen-columns" },
                            Faq = new []
                            {
                                new FaqItem { Question = "How do I let users show and hide DataGrid columns?", Answer = "Enable column picking (AllowColumnPicking) and the grid renders a menu where users toggle column visibility." }
                            },
                            Title = "Blazor DataGrid - Column Picker | Free UI Components by Radzen",
                            Description = "Enable column picker in RadzenDataGrid by setting the AllowColumnPicking property to true.",
                            Tags = new [] { "datagrid", "column", "picker", "chooser" }
                        },
                        new Example
                        {
                            Name = "Reorder",
                            Path = "datagrid-column-reorder",
                            Related = new [] { "datagrid-column-resizing", "datagrid-column-picker", "datagrid-frozen-columns" },
                            Faq = new []
                            {
                                new FaqItem { Question = "How do I enable column reordering?", Answer = "Set AllowColumnReorder to true; users can then drag column headers to change the column order." }
                            },
                            Title = "Blazor DataGrid - Column Reorder | Free UI Components by Radzen",
                            Description = "Enable column reorder in RadzenDataGrid by setting the AllowColumnReorder property to true. Define column initial order using column OrderIndex property.",
                            Tags = new [] { "column", "reorder", "grid", "datagrid", "table"}
                        },
                        new Example
                        {
                            Name = "Footer Totals",
                            Path = "datagrid-footer-totals",
                            Related = new [] { "datagrid-grouping-api", "datagrid-crosstab", "datagrid-iqueryable" },
                            Faq = new []
                            {
                                new FaqItem { Question = "How do I show column totals in the DataGrid?", Answer = "Add a FooterTemplate to the column and compute the aggregate - sum, average, count, min, or max - over the grid's view; it updates as the data is filtered and paged." }
                            },
                            Title = "Blazor DataGrid - Footer Totals | Free UI Components by Radzen",
                            Description = "The FooterTemplate column property allows you to display aggregated data in the column footer.",
                            Tags = new [] { "summary", "total", "aggregate", "datagrid", "table", "dataview" }
                        },
                        new Example
                        {
                            Name = "Frozen Columns",
                            Path = "datagrid-frozen-columns",
                            Related = new [] { "datagrid-column-resizing", "datagrid-column-reorder", "datagrid-virtualization" },
                            Faq = new []
                            {
                                new FaqItem { Question = "How do I freeze a column in the DataGrid?", Answer = "Set Frozen on the column, and FrozenPosition for left or right; it stays in place while the other columns scroll horizontally." }
                            },
                            Title = "Blazor DataGrid - Frozen Columns | Free UI Components by Radzen",
                            Description = "Lock columns in RadzenDataGrid to prevent them from scrolling out of view via the Frozen property.",
                            Tags = new [] { "datagrid", "column", "frozen", "locked" }
                        },
                        new Example
                        {
                            Name = "Composite Columns",
                            Path = "datagrid-composite-columns",
                            Related = new [] { "datagrid-column-template", "datagrid-frozen-columns", "datagrid-grouping-api" },
                            Faq = new []
                            {
                                new FaqItem { Question = "How do I create grouped or multi-level column headers?", Answer = "Nest child columns inside a parent RadzenDataGridColumn; the parent renders as a spanning header above its children." }
                            },
                            Title = "Blazor DataGrid - Composite Columns | Free UI Components by Radzen",
                            Description = "Use RadzenDataGridColumn Columns property to define child columns.",
                            Tags = new [] { "datagrid", "column", "composite", "merged", "complex" }
                        },
                        new Example
                        {
                            Name = "Conditional Columns",
                            Path = "datagrid-conditional-columns-render",
                            Related = new [] { "datagrid-column-template", "datagrid-column-picker", "datagrid-conditional-template" },
                            Faq = new []
                            {
                                new FaqItem { Question = "How do I show a DataGrid column conditionally?", Answer = "Bind the column's Visible property to your condition, or render columns with normal Razor control flow so they appear only when needed." }
                            },
                            Title = "Blazor DataGrid - Conditional Columns | Free UI Components by Radzen",
                            Description = "Use RadzenDataGridColumn Columns property to define child columns conditionally.",
                            Tags = new [] { "datagrid", "column", "conditional", "render", "complex" }
                        }
                    }
                },
                new Example
                {
                    Name = "Filtering",
                    Icon = "\uef4f",
                    Children = new []
                    {
                        new Example
                        {
                            Name = "Simple Mode",
                            Path = "datagrid-simple-filter",
                            Title = "Blazor DataGrid - Simple Filter Mode | Free UI Components by Radzen",
                            Description = "RadzenDataGrid simple mode filtering.",
                            Tags = new [] { "filter", "simple", "grid", "datagrid", "table"},
                            Related = new [] { "datagrid-simple-filter-menu", "datagrid-advanced-filter", "datagrid-checkboxlist-filter" },
                            Faq = new []
                            {
                                new FaqItem { Question = "How do I add a filter box to each DataGrid column?", Answer = "Set AllowFiltering to true and FilterMode to Simple; each column shows an inline filter input in its header that filters as the user types." }
                            }
                        },
                        new Example
                        {
                            Name = "Simple with menu",
                            Path = "datagrid-simple-filter-menu",
                            Title = "Blazor DataGrid - Simple Filter with Menu | Free UI Components by Radzen",
                            Description = "RadzenDataGrid simple mode filtering with Menu.",
                            Tags = new [] { "filter", "simple", "grid", "datagrid", "table", "menu" },
                            Related = new [] { "datagrid-simple-filter", "datagrid-advanced-filter", "datagrid-mixed-filter" },
                            Faq = new []
                            {
                                new FaqItem { Question = "How do I let users pick the filter operator?", Answer = "Set FilterMode to SimpleWithMenu; each column shows a filter input and a menu where users choose the operator (contains, equals, greater than, and more)." }
                            }
                        },
                        new Example
                        {
                            Name = "Advanced Mode",
                            Path = "datagrid-advanced-filter",
                            Title = "Blazor DataGrid - Advanced Filter Mode | Free UI Components by Radzen",
                            Description = "RadzenDataGrid advanced mode filtering.",
                            Tags = new [] { "filter", "advanced", "grid", "datagrid", "table"},
                            Related = new [] { "datagrid-simple-filter", "datagrid-mixed-filter", "datagrid-filter-api" },
                            Faq = new []
                            {
                                new FaqItem { Question = "How do I let users build multi-condition filters?", Answer = "Set FilterMode to Advanced; each column opens a menu where users add multiple conditions combined with AND/OR." }
                            }
                        },
                        new Example
                        {
                            Name = "CheckBoxList (Excel like)",
                            Path = "datagrid-checkboxlist-filter",
                            Title = "Blazor DataGrid - Excel like filtering | Free UI Components by Radzen",
                            Description = "RadzenDataGrid Excel like filtering.",
                            Tags = new [] { "filter", "excel", "grid", "datagrid", "table", "menu", "checkbox", "list" },
                            Related = new [] { "datagrid-checkboxlist-filter-odata", "datagrid-checkboxlist-lookup-filter", "datagrid-checkboxlist-auto-apply-filter" },
                            Faq = new []
                            {
                                new FaqItem { Question = "How do I add Excel-style checkbox filtering?", Answer = "Set the column's FilterMode to CheckBoxList; the filter shows distinct values as checkboxes for multi-value selection." }
                            }
                        },
                        new Example
                        {
                            Name = "CheckBoxList with Lookup",
                            Path = "datagrid-checkboxlist-lookup-filter",
                            Title = "Blazor DataGrid - CheckBoxList Filter with Lookup Data | Free UI Components by Radzen",
                            Description = "Drive the CheckBoxList filter from a lookup data source: filter by id while showing and searching by name.",
                            Tags = new [] { "checkboxlist", "lookup", "filter", "filtering", "datagrid", "table", "dataview" },
                            Related = new [] { "datagrid-checkboxlist-filter", "datagrid-checkboxlist-auto-apply-filter", "datagrid-checkboxlist-filter-odata" },
                            Faq = new []
                            {
                                new FaqItem { Question = "How do I show lookup values in a checkbox filter?", Answer = "Set FilterLookupData with FilterLookupTextProperty and FilterLookupValueProperty on the column; the filter shows the text values and filters by the underlying keys." }
                            }
                        },
                        new Example
                        {
                            Name = "CheckBoxList Auto-Apply",
                            Path = "datagrid-checkboxlist-auto-apply-filter",
                            Title = "Blazor DataGrid - CheckBoxList Filter Auto-Apply | Free UI Components by Radzen",
                            Description = "Apply CheckBoxList column filters immediately as options are selected, without the Apply button.",
                            Tags = new [] { "checkboxlist", "auto", "apply", "filter", "filtering", "datagrid", "table", "dataview" },
                            Related = new [] { "datagrid-checkboxlist-filter", "datagrid-checkboxlist-lookup-filter", "datagrid-checkboxlist-filter-odata" },
                            Faq = new []
                            {
                                new FaqItem { Question = "How do I apply checkbox filters as soon as they're selected?", Answer = "Set AutoApplyCheckBoxListFilter on the grid; CheckBoxList selections filter the data immediately instead of waiting for an Apply button." }
                            }
                        },
                        new Example
                        {
                            Name = "CheckBoxList with OData",
                            Path = "datagrid-checkboxlist-filter-odata",
                            Title = "Blazor DataGrid - Excel Filter with OData | Free UI Components by Radzen",
                            Description = "RadzenDataGrid Excel like filtering with OData.",
                            Tags = new [] { "filter", "excel", "grid", "datagrid", "table", "menu", "checkbox", "list", "odata" },
                            Related = new [] { "datagrid-checkboxlist-filter", "datagrid-odata", "datagrid-checkboxlist-lookup-filter" },
                            Faq = new []
                            {
                                new FaqItem { Question = "Does checkbox filtering work with OData?", Answer = "Yes. With CheckBoxList filtering over OData, the grid requests distinct values and applies the selected ones as a server-side $filter." }
                            }
                        },
                        new Example
                        {
                            Name = "Mixed Mode",
                            Path = "datagrid-mixed-filter",
                            Title = "Blazor DataGrid - Mixed Filter Mode | Free UI Components by Radzen",
                            Description = "RadzenDataGrid Excel like and advanced mixed mode filtering.",
                            Tags = new [] { "filter", "advanced", "grid", "datagrid", "table"},
                            Related = new [] { "datagrid-advanced-filter", "datagrid-simple-filter", "datagrid-filter-api" },
                            Faq = new []
                            {
                                new FaqItem { Question = "Can different DataGrid columns use different filter modes?", Answer = "Yes. Set each column's FilterMode so some use the Advanced filter menu and others use the inline Simple filter, in the same grid." }
                            }
                        },
                        new Example
                        {
                            Name = "Enum filtering",
                            Path = "datagrid-enum-filter",
                            Title = "Blazor DataGrid - Enum Filtering | Free UI Components by Radzen",
                            Description = "This example demonstrates how to use enums in the RadzenDataGrid column filter.",
                            Tags = new [] { "filter", "enum", "grid", "datagrid", "table"},
                            Related = new [] { "datagrid-checkboxlist-filter", "datagrid-simple-filter", "datagrid-advanced-filter" },
                            Faq = new []
                            {
                                new FaqItem { Question = "How do I filter a DataGrid column that holds an enum?", Answer = "Bind the column to the enum property; the filter offers the enum values, and you can use Description attributes for friendly labels." }
                            }
                        },
                        new Example
                        {
                            Name = "Filtering sub properties",
                            Path = "datagrid-sub-properties-filter",
                            Title = "Blazor DataGrid - Sub Property Filter | Free UI Components by Radzen",
                            Description = "This example demonstrates how to use sub properties in the RadzenDataGrid column filter.",
                            Tags = new [] { "filter", "sub properties", "grid", "datagrid", "table"},
                            Related = new [] { "datagrid-advanced-filter", "datagrid-column-template", "datagrid-iqueryable" },
                            Faq = new []
                            {
                                new FaqItem { Question = "Can I filter a column bound to a nested property?", Answer = "Yes. Set the column Property to the nested path (for example Order.Customer.Country); filtering, sorting, and grouping follow the same path." }
                            }
                        },
                        new Example
                        {
                            Name = "Filter API",
                            Path = "datagrid-filter-api",
                            Title = "Blazor DataGrid - Filter API | Free UI Components by Radzen",
                            Description = "Set the initial filter of your RadzenDataGrid via the FilterValue and FilterOperator column properties.",
                            Tags = new [] { "filter", "api", "grid", "datagrid", "table"},
                            Related = new [] { "datagrid-advanced-filter", "datagrid-simple-filter", "datagrid-mixed-filter" },
                            Faq = new []
                            {
                                new FaqItem { Question = "How do I set the DataGrid's initial filter in code?", Answer = "Set each column's FilterValue and FilterOperator (and SecondFilterValue for ranges); the grid applies them on load and whenever you change them." }
                            }
                        },
                        new Example
                        {
                            Name = "Filter Template",
                            Path = "datagrid-filter-template",
                            Title = "Blazor DataGrid - Custom Filtering | Free UI Components by Radzen",
                            Description = "This example demonstrates how to define custom RadzenDataGrid column filter template.",
                            Tags = new [] { "datagrid", "column", "filter", "template" },
                            Related = new [] { "datagrid-filtervalue-template", "datagrid-advanced-filter", "datagrid-column-template" },
                            Faq = new []
                            {
                                new FaqItem { Question = "How do I create a custom column filter UI?", Answer = "Add a FilterTemplate to the column and place your own controls inside; update the column's filter value to drive filtering." }
                            }
                        },
                        new Example
                        {
                            Name = "Filter Value Template",
                            Path = "datagrid-filtervalue-template",
                            Title = "Blazor DataGrid - Filter Value Template | Free UI Components by Radzen",
                            Description = "This example demonstrates how to define custom RadzenDataGrid column filter value template.",
                            Tags = new [] { "datagrid", "column", "filter", "template", "value" },
                            Related = new [] { "datagrid-filter-template", "datagrid-advanced-filter", "datagrid-column-template" },
                            Faq = new []
                            {
                                new FaqItem { Question = "What is the difference between FilterTemplate and FilterValueTemplate?", Answer = "FilterTemplate replaces the whole filter UI; FilterValueTemplate only customizes how the selected filter value is displayed." }
                            }
                        },
                    }
                },
                new Example
                {
                    Name = "Hierarchy",
                    Icon = "\ue23e",
                    Children  = new []
                    {
                        new Example
                        {
                            Name = "Hierarchy",
                            Path = "master-detail-hierarchy",
                            Related = new [] { "master-detail-hierarchy-demand", "datagrid-selfref-hierarchy", "master-detail" },
                            Faq = new []
                            {
                                new FaqItem { Question = "How do I show a nested grid for each row?", Answer = "Add a Template to the grid that renders a child RadzenDataGrid bound to the row's children; users expand a row to see it." }
                            },
                            Title = "Blazor DataGrid - Hierarchy | Free UI Components by Radzen",
                            Description = "This example demonstrates how to use templates to create a hierarchy in a Blazor RadzenDataGrid.",
                            Tags = new [] { "master", "detail", "datagrid", "table", "dataview" }
                        },
                        new Example
                        {
                            Name = "Hierarchy on demand",
                            Path = "master-detail-hierarchy-demand",
                            Related = new [] { "master-detail-hierarchy", "datagrid-selfref-hierarchy", "datagrid-loaddata" },
                            Faq = new []
                            {
                                new FaqItem { Question = "How do I load child rows only when a row is expanded?", Answer = "Handle the RowExpand event to fetch that row's children on demand and bind them to the nested grid." }
                            },
                            Title = "Blazor DataGrid - Hierarchy on Demand | Free UI Components by Radzen",
                            Description = "This example demonstrates how to use templates to create a Radzen Blazor DataGrid hierarchy and load data on demand.",
                            Tags = new [] { "master", "detail", "datagrid", "table", "dataview", "on-demand" }
                        },
                        new Example
                        {
                            Name = "Self-reference hierarchy",
                            Path = "datagrid-selfref-hierarchy",
                            Related = new [] { "master-detail-hierarchy", "master-detail-hierarchy-demand", "master-detail" },
                            Faq = new []
                            {
                                new FaqItem { Question = "How do I show a self-referencing (parent/child) hierarchy?", Answer = "Bind the grid to the parent rows and render each row's children, matched by the parent id, in a nested grid." }
                            },
                            Title = "Blazor DataGrid - Self-Reference Hierarchy | Free UI Components by Radzen",
                            Description = "This example demonstrates how to develop and show a self-referencing hierarchy.",
                            Tags = new [] { "master", "detail", "datagrid", "table", "dataview", "hierarchy", "self-reference" }
                        },
                        new Example
                        {
                            Name = "Master/Detail",
                            Path = "master-detail",
                            Related = new [] { "master-detail-hierarchy", "datagrid-single-selection", "datagrid-selfref-hierarchy" },
                            Faq = new []
                            {
                                new FaqItem { Question = "How do I create a master-detail view with two grids?", Answer = "Handle the master grid's RowSelect event and load the related rows into a second DataGrid bound to the selected item." }
                            },
                            Title = "Blazor DataGrid - Master and Detail | Free UI Components by Radzen",
                            Description = "This example demonstrates how to create a master/detail relationship between two Blazor RadzenDataGrid components.",
                            Tags = new [] { "master", "detail", "datagrid", "table", "dataview" }
                        },
                    }
                },
                new Example
                {
                    Name = "Selection",
                    Icon = "\uf0c5",
                    Children = new []
                    {
                        new Example
                        {
                            Name = "Single selection",
                            Path = "datagrid-single-selection",
                            Related = new [] { "datagrid-multiple-selection", "datagrid-cell-selection", "master-detail" },
                            Faq = new []
                            {
                                new FaqItem { Question = "How do I enable single row selection?", Answer = "Set SelectionMode to Single and bind Value to a single item; the grid raises RowSelect when the selection changes." }
                            },
                            Title = "Blazor DataGrid - Single Selection | Free UI Components by Radzen",
                            Description = "This example demonstrates how to enable single selection in Blazor RadzenDataGrid component.",
                            Tags = new [] { "single", "selection", "datagrid", "table", "dataview" }
                        },
                        new Example
                        {
                            Name = "Multiple selection",
                            Path = "datagrid-multiple-selection",
                            Related = new [] { "datagrid-single-selection", "datagrid-cell-selection", "datagrid-iqueryable" },
                            Faq = new []
                            {
                                new FaqItem { Question = "How do I enable multiple row selection with checkboxes?", Answer = "Set SelectionMode to Multiple and bind Value to a collection; add a header checkbox to select or clear all rows." }
                            },
                            Title = "Blazor DataGrid - Multi Selection | Free UI Components by Radzen",
                            Description = "This example demonstrates how to enable multiple selection in Blazor RadzenDataGrid component.",
                            Tags = new [] { "multiple", "selection", "datagrid", "table", "dataview" }
                        },
                        new Example
                        {
                            Name = "Cell selection",
                            Path = "datagrid-cell-selection",
                            Related = new [] { "datagrid-single-selection", "datagrid-multiple-selection", "datagrid-cell-contextmenu" },
                            Faq = new []
                            {
                                new FaqItem { Question = "How do I enable cell selection in the DataGrid?", Answer = "Turn on cell selection and handle the CellClick event to track the selected cells." }
                            },
                            Title = "Blazor DataGrid - Cell Selection | Free UI Components by Radzen",
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
                            Related = new [] { "datagrid-multi-sort", "datagrid-sort-api", "datagrid-sort-comparer" },
                            Faq = new []
                            {
                                new FaqItem { Question = "How do I enable sorting in the DataGrid?", Answer = "Set AllowSorting to true on the grid (or Sortable on a column); users sort by clicking the column header." }
                            },
                            Title = "Blazor DataGrid - Sorting | Free UI Components by Radzen",
                            Description = "This example demonstrates sorting in Blazor RadzenDataGrid component.",
                            Tags = new [] { "single", "sort", "datagrid", "table", "dataview" }
                        },
                        new Example
                        {
                            Name = "Multiple Column Sorting",
                            Path = "datagrid-multi-sort",
                            Related = new [] { "datagrid-sort", "datagrid-sort-api", "datagrid-sort-comparer" },
                            Faq = new []
                            {
                                new FaqItem { Question = "How do I enable multi-column sorting?", Answer = "Set AllowMultiColumnSorting to true; users add columns to the sort by clicking more headers, and you can show the order with sort indexes." }
                            },
                            Title = "Blazor DataGrid - Multi-Column Sorting | Free UI Components by Radzen",
                            Description = "This example demonstrates multiple column sorting in Blazor RadzenDataGrid component.",
                            Tags = new [] { "multi", "sort", "datagrid", "table", "dataview" }
                        },
                        new Example
                        {
                            Name = "Sort API",
                            Path = "datagrid-sort-api",
                            Related = new [] { "datagrid-sort", "datagrid-multi-sort", "datagrid-sort-comparer" },
                            Faq = new []
                            {
                                new FaqItem { Question = "How do I set the initial sort order in code?", Answer = "Set each column's SortOrder to Ascending or Descending; the grid applies it on load." }
                            },
                            Title = "Blazor DataGrid - Sort API | Free UI Components by Radzen",
                            Description = "Set the initial sort order of your RadzenDataGrid via the SortOrder column property.",
                            Tags = new [] { "api", "sort", "datagrid", "table", "dataview" }
                        },
                        new Example
                        {
                            Name = "Custom Sort Comparer",
                            Path = "datagrid-sort-comparer",
                            Related = new [] { "datagrid-sort", "datagrid-multi-sort", "datagrid-sort-api" },
                            Faq = new []
                            {
                                new FaqItem { Question = "How do I customize how a column sorts?", Answer = "Set the column's SortComparer to an IComparer implementation; the grid uses it instead of the default comparison." }
                            },
                            Title = "Blazor DataGrid - Custom Sort Comparer | Free UI Components by Radzen",
                            Description = "Sort a column with a custom IComparer, for example ordering id values by their mapped display name.",
                            Tags = new [] { "comparer", "custom", "sort", "datagrid", "table", "dataview" }
                        }
                    }
                },
                new Example
                {
                    Name = "Paging",
                    Icon = "\ue5dd",
                    Children = new []
                    {
                        new Example
                        {
                            Name = "Pager Position",
                            Path = "datagrid-pager-position",
                            Related = new [] { "datagrid-pager-horizontal-align", "datagrid-pager-api", "datagrid-virtualization" },
                            Faq = new []
                            {
                                new FaqItem { Question = "How do I move the DataGrid pager to the top?", Answer = "Set PagerPosition to Top, Bottom, or TopAndBottom on the grid." }
                            },
                            Title = "Blazor DataGrid - Pager Position | Free UI Components by Radzen",
                            Description = "Set the pager position to Top, Bottom, or TopAndBottom.",
                            Tags = new [] { "pager", "paging", "datagrid", "table", "dataview" }
                        },
                        new Example
                        {
                            Name = "Pager Horizontal Align",
                            Path = "datagrid-pager-horizontal-align",
                            Related = new [] { "datagrid-pager-position", "datagrid-pager-api", "datagrid-iqueryable" },
                            Faq = new []
                            {
                                new FaqItem { Question = "How do I align the DataGrid pager?", Answer = "Set PagerHorizontalAlign to Left, Center, Right, or Justify." }
                            },
                            Title = "Blazor DataGrid - Pager Alignment | Free UI Components by Radzen",
                            Description = "See how to change the horizontal alignment of the pager in a RadzenDataGrid.",
                            Tags = new [] { "pager", "paging", "datagrid", "table", "dataview" }
                        },
                        new Example
                        {
                            Name = "Pager API",
                            Path = "datagrid-pager-api",
                            Related = new [] { "datagrid-pager-position", "datagrid-pager-horizontal-align", "datagrid-loaddata" },
                            Faq = new []
                            {
                                new FaqItem { Question = "How do I set the DataGrid page size and current page in code?", Answer = "Set PageSize and PageSizeOptions, use GoToPage or the Page property to change pages, and handle the PageChanged event." }
                            },
                            Title = "Blazor DataGrid - Pager API | Free UI Components by Radzen",
                            Description = "Blazor RadzenDataGrid Pager API.",
                            Tags = new [] { "pager", "paging", "api", "datagrid", "table", "dataview" }
                        }
                    }
                },
                new Example
                {
                    Name = "Grouping",
                    Icon = "\uf1be",
                    Children = new []
                    {
                        new Example
                        {
                            Name = "Grouping API",
                            Path = "datagrid-grouping-api",
                            Related = new [] { "datagrid-group-header-template", "datagrid-group-footer-template", "datagrid-footer-totals" },
                            Faq = new []
                            {
                                new FaqItem { Question = "How do I enable grouping in the DataGrid?", Answer = "Set AllowGrouping to true; users drag column headers into the group panel, and you can group from code via the Groups collection." }
                            },
                            Title = "Blazor DataGrid - Grouping API | Free UI Components by Radzen",
                            Description = "Enable DataGrid grouping with AllowGrouping. Localize group panel text and disable grouping per column.",
                            Tags = new [] { "group", "grouping", "datagrid", "table", "dataview", "api" }
                        },
                        new Example
                        {
                            Name = "Group Header Template",
                            Path = "datagrid-group-header-template",
                            Related = new [] { "datagrid-grouping-api", "datagrid-group-footer-template", "datagrid-footer-totals" },
                            Faq = new []
                            {
                                new FaqItem { Question = "How do I customize the group header row?", Answer = "Add a GroupHeaderTemplate to the grid and render the group key, counts, or aggregates with your own markup." }
                            },
                            Title = "Blazor DataGrid - Group Header | Free UI Components by Radzen",
                            Description = "Use GroupHeaderTemplate to customize DataGrid group header rows.",
                            Tags = new [] { "group", "grouping", "template", "datagrid", "table", "dataview" }
                        },
                        new Example
                        {
                            Name = "Group Footer Template",
                            Path = "datagrid-group-footer-template",
                            Related = new [] { "datagrid-grouping-api", "datagrid-group-header-template", "datagrid-footer-totals" },
                            Faq = new []
                            {
                                new FaqItem { Question = "How do I show totals for each group?", Answer = "Add a GroupFooterTemplate to the column and compute the aggregate over the group's data; it renders in each group's footer." }
                            },
                            Title = "Blazor DataGrid - Group Footer | Free UI Components by Radzen",
                            Description = "The GroupFooterTemplate column property allows you to display aggregated data (totals) in the column footer for each group.",
                            Tags = new [] { "group", "grouping", "footer", "template", "datagrid", "table", "dataview" }
                        }
                    }
                },
                new Example
                {
                    Name = "Density",
                    Path = "datagrid-density",
                    Related = new [] { "datagrid-grid-lines", "datagrid-virtualization", "datagrid-iqueryable" },
                    Faq = new []
                    {
                        new FaqItem { Question = "How do I make the DataGrid more compact?", Answer = "Set Density to Compact to reduce row padding and fit more rows; the default is comfortable." }
                    },
                    Title = "Blazor DataGrid - Density | Free UI Components by Radzen",
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
                            Related = new [] { "datagrid-custom-header-columnpicker", "datagrid-column-picker", "export-excel-csv" },
                            Faq = new []
                            {
                                new FaqItem { Question = "How do I add a toolbar to the DataGrid?", Answer = "Use the HeaderTemplate to render your own toolbar - buttons, search, or any components - above the grid's columns." }
                            },
                            Title = "Blazor DataGrid - Custom Header | Free UI Components by Radzen",
                            Description = "Gives the grid a custom header, allowing the adding of components to create custom tool bars in addtion to column grouping and column picker.",
                            Tags = new [] { "grid header","header" }
                        },
                      new Example
                        {
                            Name = "Header with column picker",
                            Path="datagrid-custom-header-columnpicker",
                            Related = new [] { "datagrid-custom-header", "datagrid-column-picker", "datagrid-column-reorder" },
                            Faq = new []
                            {
                                new FaqItem { Question = "How do I put a column picker in a custom header?", Answer = "Render a RadzenDataGridColumnPicker inside the grid's HeaderTemplate so users toggle column visibility from your toolbar." }
                            },
                            Title = "Blazor DataGrid - Header Column Picker | Free UI Components by Radzen",
                            Description = "See how to add a column picker to your Blazor RadzenDataGrid.",
                            Tags = new [] { "grid header","header" }
                        }
                    }
                },
                new Example
                {
                    Name = "GridLines",
                    Path = "datagrid-grid-lines",
                    Related = new [] { "datagrid-density", "datagrid-conditional-template", "datagrid-iqueryable" },
                    Faq = new []
                    {
                        new FaqItem { Question = "How do I change the DataGrid grid lines?", Answer = "Set GridLines to Both, Horizontal, Vertical, or None to control which cell borders are shown." }
                    },
                    Title = "Blazor DataGrid - Grid Lines | Free UI Components by Radzen",
                    Description = "Deside where to display grid lines in your Blazor RadzenDataGrid.",
                    Icon = "\uf016",
                    Tags = new [] { "grid", "lines", "border", "gridlines" }
                },
                new Example
                {
                    Name = "Cell Context Menu",
                    Path = "datagrid-cell-contextmenu",
                    Related = new [] { "datagrid-cell-selection", "datagrid-inline-edit", "datagrid-column-template" },
                    Faq = new []
                    {
                        new FaqItem { Question = "How do I add a context menu to DataGrid cells?", Answer = "Handle the CellContextMenu event and open a RadzenContextMenu with your actions for the clicked cell and row." }
                    },
                    Title = "Blazor DataGrid - Cell Menu | Free UI Components by Radzen",
                    Description = "Right click on a table cell to open the context menu.",
                    Icon = "\ue22b",
                    Tags = new [] { "cell", "row", "contextmenu", "menu", "rightclick" }
                },

                new Example
                {
                    Name = "Save/Load settings",
                    Icon = "\uf02e",
                    Children = new []
                    {
                        new Example
                        {
                            Name = "IQueryable",
                            Path = "datagrid-save-settings",
                            Related = new [] { "datagrid-save-settings-loaddata", "datagrid-column-picker", "datagrid-iqueryable" },
                            Faq = new []
                            {
                                new FaqItem { Question = "How do I save and restore DataGrid state?", Answer = "Bind the Settings property (or handle its change) to capture page, sort, filter, and column layout, then reapply it on load." }
                            },
                            Title = "Blazor DataGrid - Save/Load Settings | Free UI Components by Radzen",
                            Description = "Save and load DataGrid state including page index, page size, column filters, sort order, width, and visibility.",
                            Tags = new [] { "save", "load", "settings" }
                        },

                        new Example
                        {
                            Name = "LoadData binding",
                            Path = "datagrid-save-settings-loaddata",
                            Related = new [] { "datagrid-save-settings", "datagrid-loaddata", "datagrid-iqueryable" },
                            Faq = new []
                            {
                                new FaqItem { Question = "Does saving DataGrid state work with LoadData binding?", Answer = "Yes. Capture the Settings and reapply them; the grid raises LoadData with the restored sort, filter, and page so your query returns the right rows." }
                            },
                            Title = "Blazor DataGrid - Save/Load Settings | Free UI Components by Radzen",
                            Description = "This example shows how to save/load DataGrid state using Settings property when binding using LoadData event.",
                            Tags = new [] { "save", "load", "settings", "async", "loaddata" }
                        }
                    }
                },

                new Example
                {
                    Name = "Drag & Drop",
                    Icon = "\ue945",
                    Children = new []
                    {
                        new Example
                        {
                            Name = "Rows reorder",
                            Path = "/datagrid-rowreorder",
                            Related = new [] { "datagrid-rowdragbetween", "datagrid-column-reorder", "datagrid-inline-edit" },
                            Faq = new []
                            {
                                new FaqItem { Question = "How do I let users drag to reorder rows?", Answer = "Enable row reordering and handle the RowReorder event to update your data when a row is dropped in a new position." }
                            },
                            Title = "Blazor DataGrid - Reorder rows | Free UI Components by Radzen",
                            Description = "This example demonstrates custom DataGrid rows reoder.",
                            Tags = new [] { "datagrid", "reorder", "row" }
                        },
                        new Example
                        {
                            Name = "Drag row between two DataGrids",
                            Path = "/datagrid-rowdragbetween",
                            Related = new [] { "datagrid-rowreorder", "datagrid-rowdrag-scheduler", "datagrid-multiple-selection" },
                            Faq = new []
                            {
                                new FaqItem { Question = "How do I drag rows from one DataGrid to another?", Answer = "Handle the row drag events on both grids to remove the item from the source and add it to the target collection." }
                            },
                            Title = "Blazor DataGrid - Drag Rows Between Grids | Free UI Components by Radzen",
                            Description = "This example demonstrates drag and drop rows between two DataGrid components.",
                            Tags = new [] { "datagrid", "drag", "row", "between" }
                        },
                        new Example
                        {
                            Name = "Drag row between DataGrid and Scheduler",
                            Path = "/datagrid-rowdrag-scheduler",
                            Related = new [] { "datagrid-rowdragbetween", "datagrid-rowreorder", "datagrid-iqueryable" },
                            Faq = new []
                            {
                                new FaqItem { Question = "How do I drag DataGrid rows into the Scheduler?", Answer = "Make the rows draggable and handle the Scheduler's drop to create an appointment from the dropped row." }
                            },
                            Title = "Blazor DataGrid - Drag Rows to Scheduler | Free UI Components by Radzen",
                            Description = "This example demonstrates drag and drop rows between DataGrid and Scheduler.",
                            Tags = new [] { "datagrid", "drag", "row", "scheduler" }
                        }
                    }
                },

                new Example
                {
                    Name = "InLine Editing",
                    Path = "datagrid-inline-edit",
                    Related = new [] { "datagrid-incell-edit", "datagrid-column-template", "datagrid-iqueryable" },
                    Faq = new []
                    {
                        new FaqItem { Question = "How do I enable inline row editing in the DataGrid?", Answer = "Add an EditTemplate to each editable column and call EditRow to switch a row into edit mode; UpdateRow saves the changes." }
                    },
                    Title = "Blazor DataGrid - InLine Editing | Free UI Components by Radzen",
                    Description = "This example demonstrates how to configure the Razden Blazor DataGrid for inline editing.",
                    Icon = "\ue22b",
                    Tags = new [] { "inline", "editor", "datagrid", "table", "dataview" }
                },

                new Example
                {
                    Name = "InCell Editing",
                    Path = "datagrid-incell-edit",
                    Related = new [] { "datagrid-inline-edit", "datagrid-column-template", "datagrid-cell-selection" },
                    Faq = new []
                    {
                        new FaqItem { Question = "How do I enable in-cell editing?", Answer = "Use cell edit mode with an EditTemplate per column; edits commit as the user moves between cells." }
                    },
                    Title = "Blazor DataGrid - InCell Editing | Free UI Components by Radzen",
                    Description = "This example demonstrates how to configure the Razden Blazor DataGrid for in-cell editing.",
                    Icon = "\ue745",
                    Tags = new [] { "in-cell", "editor", "datagrid", "table", "dataview" }
                },

                new Example
                {
                    Name = "Conditional formatting",
                    Path = "datagrid-conditional-template",
                    Related = new [] { "datagrid-column-template", "datagrid-conditional-columns-render", "datagrid-grid-lines" },
                    Faq = new []
                    {
                        new FaqItem { Question = "How do I style DataGrid rows or cells by value?", Answer = "Use CellRender and RowRender to add CSS classes or inline styles based on the data, or a column Template to render conditional content." }
                    },
                    Title = "Blazor DataGrid - Conditional Format | Free UI Components by Radzen",
                    Description = "This example demonstrates RadzenDataGrid with conditional rows and cells template and styles.",
                    Icon = "\ue41d",
                    Tags = new [] { "conditional", "template", "style", "datagrid", "table", "dataview" }
                },
                new Example
                {
                    Name = "Export to Excel and CSV",
                    Path = "export-excel-csv",
                    Related = new [] { "datagrid-footer-totals", "datagrid-grouping-api", "datagrid-iqueryable" },
                    Faq = new []
                    {
                        new FaqItem { Question = "How do I export the DataGrid to Excel or CSV?", Answer = "Call the grid's export and choose Excel or CSV; the export uses the current sort, filter, and columns." }
                    },
                    Title = "Blazor DataGrid - Excel & CSV Export | Free UI Components by Radzen",
                    Description = "This example demonstrates how to export a Radzen Blazor DataGrid to Excel and CSV.",
                    Icon = "\ue0c3",
                    Tags = new [] { "export", "excel", "csv" }
                },
                new Example
                {
                    Name = "Cascading DropDowns",
                    Path = "cascading-dropdowns",
                    Related = new [] { "datagrid-inline-edit", "datagrid-incell-edit", "datagrid-column-template" },
                    Faq = new []
                    {
                        new FaqItem { Question = "How do I create cascading dropdowns in the DataGrid?", Answer = "Bind each dropdown's data to the value of the previous one and reload the dependent options when the parent selection changes." }
                    },
                    Title = "Blazor DataGrid - Cascading DropDown | Free UI Components by Radzen",
                    Description = "This example demonstrates cascading Radzen Blazor DropDown components.",
                    Icon = "\ue915",
                    Tags = new [] { "related", "parent", "child" }
                },
                new Example
                {
                    Name = "Empty Data Grid",
                    Path = "/datagrid-empty",
                    Related = new [] { "datagrid-iqueryable", "datagrid-loaddata", "datagrid-column-template" },
                    Faq = new []
                    {
                        new FaqItem { Question = "How do I customize the DataGrid's empty state?", Answer = "Use the EmptyTemplate to render your own message or content when the grid has no rows." }
                    },
                    Title = "Blazor DataGrid - Empty Data Grid | Free UI Components by Radzen",
                    Description = "This example demonstrates Blazor DataGrid without data.",
                    Icon = "\ue661",
                    Tags = new [] { "datagrid", "databinding" }
                }
            }
        },
        new Example
        {
            Name = "Data Visualization",
            Updated = true,
            Icon = "\ue4fb",
            Children = new[] {
                new Example
                {
                    Name = "Chart Gallery",
                    Path = "charts",
                    Title = "Blazor Charts - 30+ Chart Types | Free UI Components by Radzen",
                    Description = "Browse 40+ free Blazor data-visualization components, including 30+ chart types plus gauges, sparklines, treemap and Sankey. Drawn in C# as SVG, with no JavaScript charting library.",
                    Icon = "\ue3b6",
                    Tags = new [] { "chart", "gallery", "overview", "visualization" },
                    New = true
                },
                new Example
                {
                    Name = "Configuration",
                    Icon = "\ue8b8",
                    Children = new [] {
                        new Example
                        {
                            Toc = [ new () { Text = "Chart Series", Anchor = "#series" }, new () { Text = "Basic usage", Anchor = "#basic-usage" }, new () { Text = "Keyboard Navigation", Anchor = "#keyboard-navigation" } ],
                            Name = "Series",
                            Path = "chart-series",
                            Title = "Blazor Chart - Series Config | Free UI Components by Radzen",
                            Description = "Bind data to a line, bar, pie, or other series - the building block of every Blazor chart.",
                            Related = new [] { "line-chart", "column-chart", "chart-axis" },
                            Faq = new []
                            {
                                new FaqItem { Question = "How do I add a series to a chart?", Answer = "Place a series component such as RadzenLineSeries or RadzenColumnSeries inside the chart and bind its Data, then point CategoryProperty and ValueProperty at the fields you want." },
                                new FaqItem { Question = "Can I combine different series types?", Answer = "Yes. Put more than one series inside the same chart - a column series with a line overlay, for example - as long as they share compatible axes." }
                            },
                            Tags = new [] { "chart", "graph", "series" }
                        },
                        new Example
                        {
                            Toc = [ new () { Text = "Min, max and step", Anchor = "#min-max-and-step" }, new () { Text = "Format axis values", Anchor = "#format-axis-values" }, new () { Text = "Display grid lines", Anchor = "#display-grid-lines" }, new () { Text = "Set axis title", Anchor = "#set-axis-title" } ],
                            Name = "Axis",
                            Path = "chart-axis",
                            Title = "Blazor Chart - Axis Config | Free UI Components by Radzen",
                            Description = "Control the scale, gridlines, labels, and title of a Blazor chart's axes, or let them fit the data automatically.",
                            Related = new [] { "chart-series", "multiple-axes-chart", "logarithmic-axis-chart" },
                            Faq = new []
                            {
                                new FaqItem { Question = "How do I set the axis minimum, maximum, and step?", Answer = "Set Min, Max, and Step on the value axis to control the range and tick spacing instead of letting the chart pick them from the data." },
                                new FaqItem { Question = "How do I format axis labels?", Answer = "Use the axis FormatString to display values as currency, percentages, dates, or any format you need." }
                            },
                            Tags = new [] { "chart", "graph", "series" }
                        },
                        new Example
                        {
                            Name = "Multiple Axes",
                            Path = "multiple-axes-chart",
                            Description = "Plot series with very different scales on one Blazor chart, each reading against its own value axis.",
                            Related = new [] { "chart-axis", "line-chart", "column-chart" },
                            Faq = new []
                            {
                                new FaqItem { Question = "When should I use multiple axes?", Answer = "When two series share a category axis but live on different scales - units sold versus conversion rate - so one would flatten the other on a single axis." },
                                new FaqItem { Question = "How do I assign a series to a second axis?", Answer = "Add a second value axis and point the series at it, so each series reads against the axis that fits its scale." }
                            },
                            Tags = new [] { "chart", "graph", "multiple", "axes", "axis", "dual", "secondary" },
                            New = true
                        },
                        new Example
                        {
                            Name = "Inverted Axis",
                            Path = "inverted-axis-chart",
                            Description = "Reverse the direction of a Blazor chart axis, so values count down or categories run the opposite way.",
                            Related = new [] { "chart-axis", "bar-chart", "column-chart" },
                            Faq = new []
                            {
                                new FaqItem { Question = "When would I invert an axis?", Answer = "When the natural reading runs backwards - a leaderboard where rank 1 belongs at the top, or a depth scale that increases downward." }
                            },
                            Tags = new [] { "chart", "graph", "inverted", "reversed", "axis", "flip" },
                            New = true
                        },
                        new Example
                        {
                            Name = "Axis Crossing",
                            Path = "axis-crossing-chart",
                            Description = "Position a Blazor chart axis to cross at a specific value instead of the chart edge.",
                            Related = new [] { "chart-axis", "negative-column-chart", "chart-reference-line" },
                            Faq = new []
                            {
                                new FaqItem { Question = "When should I move the axis crossing?", Answer = "When zero is not at the edge of your data - for example values centered on a target, where you want the axis to cross at that target rather than at the bottom." }
                            },
                            Tags = new [] { "chart", "graph", "axis", "crossing", "origin", "intersect" },
                            New = true
                        },
                        new Example
                        {
                            Name = "Logarithmic Axis",
                            Path = "logarithmic-axis-chart",
                            Description = "Use a logarithmic axis on a Blazor chart to keep values spanning several orders of magnitude readable.",
                            Related = new [] { "chart-axis", "line-chart", "scatter-chart" },
                            Faq = new []
                            {
                                new FaqItem { Question = "When should I use a logarithmic axis?", Answer = "When your values span orders of magnitude and a linear axis would squash the small ones into the baseline - population, revenue across very different sizes, or scientific data." }
                            },
                            Tags = new [] { "chart", "graph", "logarithmic", "log", "axis", "scale" },
                            New = true
                        },
                        new Example
                        {
                            Name = "Indexed Category Axis",
                            Path = "indexed-category-axis-chart",
                            Description = "Align Blazor chart series by position rather than matching category values with an indexed category axis.",
                            Related = new [] { "chart-axis", "line-chart", "chart-series" },
                            Faq = new []
                            {
                                new FaqItem { Question = "When do I need an indexed category axis?", Answer = "When your series do not share the same category values but you still want them aligned by order - comparing sequences of different lengths or with gaps." }
                            },
                            Tags = new [] { "chart", "graph", "indexed", "category", "axis" },
                            New = true
                        },
                        new Example
                        {
                            Toc = [ new () { Text = "Legend position", Anchor = "#legend-position" }, new () { Text = "Hide the legend", Anchor = "#hide-the-legend" } ],
                            Name = "Legend",
                            Path = "chart-legend",
                            Title = "Blazor Chart - Legend Config | Free UI Components by Radzen",
                            Description = "Show, move, restyle, or hide the legend that tells readers which series is which on a Blazor chart.",
                            Related = new [] { "chart-series", "chart-tooltip", "pie-chart" },
                            Faq = new []
                            {
                                new FaqItem { Question = "How do I hide the chart legend?", Answer = "Add a legend component with Visible set to false - the chart then renders without the legend." },
                                new FaqItem { Question = "Can I move the legend?", Answer = "Yes. Set the legend Position to place it on any side of the chart." }
                            },
                            Tags = new [] { "chart", "graph", "legend" }
                        },
                        new Example
                        {
                            Toc = [ new () { Text = "Customize tooltip content", Anchor = "#customize-tooltip-content" }, new () { Text = "Shared tooltip", Anchor = "#shared-tooltip" }, new () { Text = "Split tooltip", Anchor = "#split-tooltip" }, new () { Text = "Disable tooltips", Anchor = "#disable-tooltips" } ],
                            Name = "ToolTip",
                            Path = "chart-tooltip",
                            Title = "Blazor Chart - ToolTip Config | Free UI Components by Radzen",
                            Description = "Show values on hover with Blazor chart tooltips - customize content, share, split, or turn them off.",
                            Related = new [] { "chart-crosshair", "chart-sync", "chart-legend" },
                            Faq = new []
                            {
                                new FaqItem { Question = "How do I customize tooltip content?", Answer = "Provide a tooltip template for the series to render any markup you want in place of the default value." },
                                new FaqItem { Question = "How do I show one shared tooltip for all series?", Answer = "Enable the shared tooltip so a single tooltip lists every series value at the hovered category." }
                            },
                            Tags = new [] { "chart", "graph", "legend", "shared", "split", "tooltip" },
                            Updated = true
                        },
                        new Example
                        {
                            Toc = [ new () { Text = "Auto Rotation", Anchor = "#auto-rotation" }, new () { Text = "Predefined Rotation", Anchor = "#rotation" } ],
                            Name = "Label Rotation",
                            Path = "chart-label-rotation",
                            Title = "Blazor Chart - Label Rotation | Free UI Components by Radzen",
                            Description = "Rotate crowded category labels on a Blazor chart, automatically or to an angle you set, to keep them readable.",
                            Related = new [] { "chart-axis", "column-chart", "bar-chart" },
                            Faq = new []
                            {
                                new FaqItem { Question = "How do I stop axis labels from overlapping?", Answer = "Let the chart auto-rotate the labels, or set a fixed rotation angle so long labels tilt instead of colliding." }
                            },
                            Tags = new [] { "chart", "label", "rotate", "rotation" }
                        },
                        new Example
                        {
                            Name = "Interpolation",
                            Path = "chart-interpolation",
                            Title = "Blazor Chart - Interpolation | Free UI Components by Radzen",
                            Description = "Choose how a Blazor line or area chart connects its points - straight, smooth spline, or flat steps.",
                            Related = new [] { "line-chart", "spline-chart", "step-line-chart" },
                            Faq = new []
                            {
                                new FaqItem { Question = "What interpolation modes are available?", Answer = "Straight line (the default), spline for a smooth curve, and step to hold each value until the next point." },
                                new FaqItem { Question = "Which one should I use?", Answer = "Use straight lines for general trends, spline when the data changes smoothly, and step when values stay constant between changes." }
                            },
                            Tags = new [] { "chart", "interpolation", "spline", "step" }
                        },
                        new Example
                        {
                            Name = "Annotations",
                            Path = "chart-annotations",
                            Title = "Blazor Chart - Annotations | Free UI Components by Radzen",
                            Description = "Add text callouts to specific points on a Blazor chart to flag an event, a peak, or a note.",
                            Related = new [] { "chart-reference-line", "chart-data-labels", "line-chart" },
                            Faq = new []
                            {
                                new FaqItem { Question = "When should I use chart annotations?", Answer = "To call out a specific point in context - a launch date on a sales line, or an anomaly worth explaining - without a separate caption." }
                            },
                            Tags = new [] { "chart", "annotation", "label" }
                        },
                        new Example
                        {
                            Name = "Data Labels",
                            Path = "chart-data-labels",
                            Description = "Print the value next to each point or bar on a Blazor chart, with position, formatting, and overlap handling.",
                            Related = new [] { "chart-series", "chart-tooltip", "column-chart" },
                            Faq = new []
                            {
                                new FaqItem { Question = "How do I show values on a chart?", Answer = "Add data labels to the series; each point then displays its value, with options for position, formatting, and a background chip." },
                                new FaqItem { Question = "How do I stop data labels from overlapping?", Answer = "Turn on overlap hiding so the chart drops labels that would collide, keeping the readable ones." }
                            },
                            Tags = new [] { "chart", "graph", "label", "data labels", "values", "format", "overlap" },
                            New = true
                        },
                        new Example
                        {
                            Name = "Crosshair",
                            Path = "chart-crosshair",
                            Title = "Blazor Chart - Crosshair | Free UI Components by Radzen",
                            Description = "Add guide lines that track the cursor on a Blazor chart, snapping to the nearest point with an optional value label.",
                            Related = new [] { "chart-tooltip", "chart-sync", "line-chart" },
                            Faq = new []
                            {
                                new FaqItem { Question = "How do I add a crosshair to a chart?", Answer = "Enable the crosshair per axis; it then follows the cursor or snaps to the nearest data point, with an optional label showing the formatted value." }
                            },
                            Tags = new [] { "chart", "crosshair", "hover" },
                            New = true
                        },
                        new Example
                        {
                            Name = "Styling Chart",
                            Path = "styling-chart",
                            Title = "Blazor Chart - Styling | Free UI Components by Radzen",
                            Description = "Restyle a Blazor chart with color schemes, custom series colors, fills, and fonts to match your theme.",
                            Related = new [] { "chart-series", "styling-gauge", "column-chart" },
                            Faq = new []
                            {
                                new FaqItem { Question = "How do I change a chart's colors?", Answer = "Pick a built-in color scheme or supply your own palette; you can also set a custom color per series." }
                            },
                            Tags = new [] { "chart", "graph", "styling" }
                        },
                    }
                },
                new Example
                {
                    Name = "Area Chart",
                    Icon = "\ue770",
                    Children = new [] {
                        new Example
                        {
                            Name = "Area Chart",
                            Path = "area-chart",
                            Description = "Show how a value's volume changes over time with a Blazor area chart - a filled line chart. Free and open source.",
                            Tags = new [] { "chart", "graph", "area" },
                            Related = new [] { "line-chart", "stacked-area-chart", "range-area-chart", "column-chart" },
                            Faq = new []
                            {
                                new FaqItem { Question = "When should I use an area chart instead of a line chart?", Answer = "Use an area chart when the size of the value matters as much as its trend - the filled region draws attention to volume, totals, or how much accumulated. A line chart is better when you only care about direction or are comparing many series." },
                                new FaqItem { Question = "What data does an area chart need?", Answer = "A series of points with a category or date on one axis and a numeric value on the other, mapped through the CategoryProperty and ValueProperty of the area series." },
                                new FaqItem { Question = "Can I stack multiple area series?", Answer = "Yes. Use stacked area to show how parts add up to a total, or full stacked area to show each part's share of 100%." }
                            }
                        },
                        new Example
                        {
                            Name = "Range Area Chart",
                            Path = "range-area-chart",
                            Description = "Display a band between a low and a high value with a Blazor range area chart - ideal for ranges and confidence intervals.",
                            Tags = new [] { "chart", "graph", "area", "range", "band", "min", "max" },
                            New = true,
                            Related = new [] { "area-chart", "range-step-area-chart", "range-bar-chart", "highlow-chart" },
                            Faq = new []
                            {
                                new FaqItem { Question = "When is a range area chart the right choice?", Answer = "When each point has two bounds rather than one value - forecast ranges, min/max bands, or uncertainty around an estimate - and you want the spread shown as a filled band." },
                                new FaqItem { Question = "What data do I bind to it?", Answer = "Each point needs a category or date plus a low and a high value, mapped to the low and high properties of the range area series." }
                            }
                        },
                        new Example
                        {
                            Name = "Area Negative Points",
                            Path = "negative-area-chart",
                            Description = "Plot values that cross zero with a Blazor area chart that fills above and below the baseline.",
                            Tags = new [] { "chart", "graph", "area", "negative", "gdp", "growth" },
                            New = true,
                            Related = new [] { "area-chart", "column-chart", "line-chart" },
                            Faq = new []
                            {
                                new FaqItem { Question = "How are negative values shown?", Answer = "The fill follows the series across the zero line, so the area below the baseline is negative and the area above it is positive." },
                                new FaqItem { Question = "When should I use it?", Answer = "Whenever your data swings between positive and negative - profit and loss, temperature anomalies, growth rates - and you want the direction obvious from the shape." }
                            }
                        },
                        new Example
                        {
                            Name = "Step Area Chart",
                            Path = "step-area-chart",
                            Description = "Show values that change in discrete steps with a Blazor step area chart.",
                            Tags = new [] { "chart", "graph", "area", "step", "interpolation" },
                            New = true,
                            Related = new [] { "area-chart", "step-line-chart", "range-step-area-chart" },
                            Faq = new []
                            {
                                new FaqItem { Question = "When should I use step interpolation?", Answer = "When a value stays constant between readings and then changes suddenly - inventory levels, pricing tiers, a setting that holds until updated. A smooth line would imply gradual change that did not happen." },
                                new FaqItem { Question = "How is it different from a regular area chart?", Answer = "A regular area chart connects points with sloped lines, suggesting continuous change. A step area keeps each value level until the next point, so the change reads as a clean jump." }
                            }
                        },
                        new Example
                        {
                            Name = "Range Step Area Chart",
                            Path = "range-step-area-chart",
                            Description = "Fill a low-to-high band that holds flat between points with a Blazor range step area chart.",
                            Tags = new [] { "chart", "graph", "area", "range", "step", "interpolation", "band" },
                            New = true,
                            Related = new [] { "range-area-chart", "step-area-chart", "area-chart" },
                            Faq = new []
                            {
                                new FaqItem { Question = "When would I use this over a plain range area?", Answer = "When the band is constant across an interval and then changes at a known moment - a rate that applies for a month, or a quota that holds until reset - rather than drifting between points." }
                            }
                        },
                        new Example
                        {
                            Name = "Stacked Area Chart",
                            Path = "stacked-area-chart",
                            Description = "Show a total and its composition over time with a Blazor stacked area chart.",
                            Tags = new [] { "chart", "stack", "graph", "area" },
                            Related = new [] { "area-chart", "100-percent-stacked-area-chart", "stacked-column-chart" },
                            Faq = new []
                            {
                                new FaqItem { Question = "When should I use a stacked area chart?", Answer = "When you care about a running total and its composition at once - revenue by product line, traffic by channel - and the parts add up to something meaningful." },
                                new FaqItem { Question = "What is the difference between stacked and full stacked area?", Answer = "Stacked area shows actual totals, so the top edge is the sum. Full stacked area normalizes every point to 100%, showing each part's share rather than its absolute size." }
                            }
                        },
                        new Example
                        {
                            Name = "Full Stacked Area Chart",
                            Path = "100-percent-stacked-area-chart",
                            Description = "Show each part's share of 100% over time with a Blazor full stacked area chart.",
                            Tags = new [] { "chart", "graph", "area", "stack", "percent", "100", "proportional", "full" },
                            New = true,
                            Related = new [] { "stacked-area-chart", "area-chart", "100-percent-stacked-column-chart" },
                            Faq = new []
                            {
                                new FaqItem { Question = "When should I use full stacked area instead of stacked area?", Answer = "When relative share matters more than absolute totals - for example how market share between products moves over time, even as the overall market grows or shrinks." },
                                new FaqItem { Question = "What data does it need?", Answer = "Several series sharing the same categories or dates; each point is converted to its percentage of the total automatically." }
                            }
                        },
                    }
                },
                new Example
                {
                    Name = "Bar Chart",
                    Icon = "\ue00d",
                    Children = new [] {
                        new Example
                        {
                            Name = "Bar Chart",
                            Path = "bar-chart",
                            Description = "Rank categories with a Blazor bar chart - horizontal bars sized by value, with room for long labels. Free and open source.",
                            Tags = new [] { "chart", "graph", "column", "bar" },
                            Related = new [] { "column-chart", "stacked-bar-chart", "range-bar-chart", "line-chart" },
                            Faq = new []
                            {
                                new FaqItem { Question = "When should I use a bar chart instead of a column chart?", Answer = "Bars run horizontally, columns run vertically. Reach for a bar chart when category names are long, or when ranking many categories - the eye scans a vertical list more comfortably than a crowded horizontal axis." },
                                new FaqItem { Question = "What data does a bar chart need?", Answer = "A category for each bar and a numeric value for its length, mapped through the CategoryProperty and ValueProperty of the bar series." },
                                new FaqItem { Question = "Can I show several values per category?", Answer = "Yes. Add more bar series to place bars side by side, or use a stacked bar chart to combine them into one bar per category." }
                            }
                        },
                        new Example
                        {
                            Name = "Range Bar Chart",
                            Path = "range-bar-chart",
                            Description = "Show spans from a start to an end value with a Blazor range bar chart - schedules, date ranges, and low-to-high bands.",
                            Tags = new [] { "chart", "graph", "bar", "range", "gantt", "timeline", "min", "max" },
                            New = true,
                            Related = new [] { "bar-chart", "range-column-chart", "range-area-chart" },
                            Faq = new []
                            {
                                new FaqItem { Question = "What is a range bar chart good for?", Answer = "Anything with a start and an end on the same row: simple Gantt-style schedules, opening hours, or the min-to-max range of a measurement." },
                                new FaqItem { Question = "What data do I bind to it?", Answer = "Each bar needs a category plus two values, a start and an end, mapped to the corresponding properties of the range bar series." }
                            }
                        },
                        new Example
                        {
                            Name = "Stacked Bar Chart",
                            Path = "stacked-bar-chart",
                            Description = "Show a total and its parts per category with a Blazor stacked bar chart.",
                            Tags = new [] { "chart", "stack", "graph", "column", "bar" },
                            Related = new [] { "bar-chart", "100-percent-stacked-bar-chart", "stacked-column-chart" },
                            Faq = new []
                            {
                                new FaqItem { Question = "When should I use a stacked bar chart?", Answer = "When each category breaks into parts that add up to a meaningful total - survey responses per group, budget by department - and you want totals and composition in one view." },
                                new FaqItem { Question = "Stacked bar or grouped bars?", Answer = "Stack the segments when the total matters and the parts sum to it. Place bars side by side instead when you mainly want to compare the parts against each other." }
                            }
                        },
                        new Example
                        {
                            Name = "Full Stacked Bar Chart",
                            Path = "100-percent-stacked-bar-chart",
                            Description = "Compare composition across categories with a Blazor full stacked bar chart, each bar scaled to 100%.",
                            Tags = new [] { "chart", "graph", "bar", "stack", "percent", "100", "proportional", "horizontal", "full" },
                            New = true,
                            Related = new [] { "stacked-bar-chart", "bar-chart", "100-percent-stacked-column-chart" },
                            Faq = new []
                            {
                                new FaqItem { Question = "When should I use full stacked bar instead of stacked bar?", Answer = "When you care about proportions rather than absolute size - the share of responses by category, or how a budget splits - and the categories have different totals you want to set aside." }
                            }
                        },
                        new Example
                        {
                            Name = "Negative Stacked Bar Chart",
                            Path = "negative-stacked-bar-chart",
                            Description = "Show opposing quantities around a zero baseline with a Blazor negative stacked bar chart - like revenue against expenses.",
                            Tags = new [] { "chart", "graph", "bar", "stack", "negative", "revenue", "expenses" },
                            New = true,
                            Related = new [] { "stacked-bar-chart", "bar-chart", "negative-area-chart" },
                            Faq = new []
                            {
                                new FaqItem { Question = "When is a negative stacked bar chart useful?", Answer = "When each category has values that pull in opposite directions - income versus spending, inflows versus outflows - and you want them mirrored around a shared center line." },
                                new FaqItem { Question = "How do I show values on both sides?", Answer = "Provide positive values for one direction and negative values for the other; the bars extend left and right from the zero baseline." }
                            }
                        },
                    }
                },
                new Example
                {
                    Name = "Column Chart",
                    Icon = "\ue015",
                    Children = new [] {
                        new Example
                        {
                            Name = "Column Chart",
                            Path = "column-chart",
                            Description = "Compare values across categories with a Blazor column chart - vertical bars, ideal for time series. Free and open source.",
                            Tags = new [] { "chart", "graph", "column", "bar" },
                            Related = new [] { "bar-chart", "grouped-column-chart", "stacked-column-chart", "line-chart" },
                            Faq = new []
                            {
                                new FaqItem { Question = "When should I use a column chart instead of a bar chart?", Answer = "Columns are vertical, bars are horizontal. Columns work best for time on the x-axis (months, quarters) and for a small number of categories; switch to a bar chart when labels are long or you are ranking many categories." },
                                new FaqItem { Question = "What data does a column chart need?", Answer = "A category for each column and a numeric value for its height, mapped through the CategoryProperty and ValueProperty of the column series." },
                                new FaqItem { Question = "Can I compare several series?", Answer = "Yes. Add more column series to group them side by side, or stack them to show how parts make up a total." }
                            }
                        },
                        new Example
                        {
                            Name = "Grouped Column Chart",
                            Path = "grouped-column-chart",
                            Description = "Compare several series side by side with a Blazor grouped column chart.",
                            Tags = new [] { "chart", "graph", "column", "grouped", "bar", "comparison" },
                            New = true,
                            Related = new [] { "column-chart", "stacked-column-chart", "bar-chart" },
                            Faq = new []
                            {
                                new FaqItem { Question = "When should I group columns instead of stacking them?", Answer = "Group when you want to compare the individual values against each other - this quarter versus last - rather than show how they sum to a total. Stack them when the total is the point." },
                                new FaqItem { Question = "How many series can I group before it gets crowded?", Answer = "A few per category stays readable; beyond that the columns get thin and hard to compare. With many series, consider a line chart or small multiples instead." }
                            }
                        },
                        new Example
                        {
                            Name = "Column Negative Points",
                            Path = "negative-column-chart",
                            Description = "Plot positive and negative values around a zero baseline with a Blazor column chart.",
                            Tags = new [] { "chart", "graph", "column", "negative", "positive", "loss", "profit" },
                            New = true,
                            Related = new [] { "column-chart", "negative-area-chart", "line-chart" },
                            Faq = new []
                            {
                                new FaqItem { Question = "How are negative values displayed?", Answer = "Columns grow upward from the zero line for positive values and downward for negative ones, often in different colors so the two directions stand apart." },
                                new FaqItem { Question = "When should I use it?", Answer = "Whenever values can go either way - net profit, budget variance, temperature anomalies - and the sign matters as much as the size." }
                            }
                        },
                        new Example
                        {
                            Name = "Range Column Chart",
                            Path = "range-column-chart",
                            Description = "Show a low-to-high band as upright columns with a Blazor range column chart.",
                            Tags = new [] { "chart", "graph", "column", "range", "min", "max" },
                            New = true,
                            Related = new [] { "column-chart", "range-bar-chart", "range-area-chart", "highlow-chart" },
                            Faq = new []
                            {
                                new FaqItem { Question = "What is a range column chart good for?", Answer = "Showing the spread between two values per category - daily highs and lows, or an estimate's min and max - as an upright band." },
                                new FaqItem { Question = "What data do I bind to it?", Answer = "Each column needs a category plus a low and a high value, mapped to the corresponding properties of the range column series." }
                            }
                        },
                        new Example
                        {
                            Name = "Stacked Column Chart",
                            Path = "stacked-column-chart",
                            Description = "Show a total and its parts per category with a Blazor stacked column chart.",
                            Tags = new [] { "chart", "stack", "graph", "column", "bar" },
                            Related = new [] { "column-chart", "100-percent-stacked-column-chart", "stacked-bar-chart" },
                            Faq = new []
                            {
                                new FaqItem { Question = "When should I use a stacked column chart?", Answer = "When categories break into parts that sum to a meaningful total - sales by region within each quarter - and you want totals and composition together." },
                                new FaqItem { Question = "Stacked or grouped columns?", Answer = "Stack when the total matters and the parts add up to it; group them side by side when you mainly want to compare the parts." }
                            }
                        },
                        new Example
                        {
                            Name = "Full Stacked Column Chart",
                            Path = "100-percent-stacked-column-chart",
                            Description = "Compare composition across categories with a Blazor full stacked column chart, each column scaled to 100%.",
                            Tags = new [] { "chart", "graph", "column", "stack", "percent", "100", "proportional", "full" },
                            New = true,
                            Related = new [] { "stacked-column-chart", "column-chart", "100-percent-stacked-bar-chart" },
                            Faq = new []
                            {
                                new FaqItem { Question = "When should I use full stacked column instead of stacked column?", Answer = "When share matters more than totals - how the mix of categories shifts over time even as the overall amount changes." }
                            }
                        },
                        new Example
                        {
                            Name = "Histogram",
                            Path = "histogram-chart",
                            Description = "Show the shape of a distribution with a Blazor histogram - values grouped into bins and counted.",
                            Tags = new [] { "chart", "graph", "histogram", "frequency", "distribution", "bin" },
                            New = true,
                            Related = new [] { "column-chart", "box-plot-chart", "scatter-chart" },
                            Faq = new []
                            {
                                new FaqItem { Question = "How is a histogram different from a column chart?", Answer = "A column chart compares values across named categories. A histogram groups a single set of numbers into ranges and counts how many land in each, so the x-axis is a continuous scale rather than labels." },
                                new FaqItem { Question = "How do I choose the number of bins?", Answer = "Fewer, wider bins smooth the shape; more, narrower bins show detail but can look noisy. Pick a bin width that makes the overall pattern clear without over-fragmenting the data." }
                            }
                        },
                    }
                },
                new Example
                {
                    Name = "Line Chart",
                    Icon = "\ue922",
                    Children = new [] {
                        new Example
                        {
                            Name = "Line Chart",
                            Path = "line-chart",
                            Description = "Show a trend over time with a Blazor line chart - the clearest way to follow how a value changes. Free and open source.",
                            Tags = new [] { "chart", "graph", "line" },
                            Related = new [] { "area-chart", "spline-chart", "step-line-chart", "column-chart" },
                            Faq = new []
                            {
                                new FaqItem { Question = "When should I use a line chart?", Answer = "When the trend matters most - how a value moves over time, or across an ordered scale - especially when comparing several series at once. If magnitude or volume is the point, an area chart fills the space beneath the line to emphasize it." },
                                new FaqItem { Question = "What data does a line chart need?", Answer = "A category or date on one axis and a numeric value on the other, mapped through the CategoryProperty and ValueProperty of the line series." },
                                new FaqItem { Question = "Can I smooth the line or make it step?", Answer = "Yes. Set interpolation to spline for a smooth curve, or to step to hold each value until the next point. Straight-line interpolation is the default." }
                            }
                        },
                        new Example
                        {
                            Name = "Spline Chart",
                            Path = "spline-chart",
                            Description = "Connect points with a smooth curve using a Blazor spline chart - a softer line for gradually changing data.",
                            Tags = new [] { "chart", "graph", "spline", "smooth", "interpolation" },
                            New = true,
                            Related = new [] { "line-chart", "step-line-chart", "area-chart" },
                            Faq = new []
                            {
                                new FaqItem { Question = "When should I use a spline instead of a straight line?", Answer = "When the underlying value changes smoothly and the curve aids readability. Avoid it when exact values matter, since the curve can overshoot between points and imply readings that are not in the data." },
                                new FaqItem { Question = "How do I enable spline interpolation?", Answer = "Set the line or area series interpolation to spline; the same data renders with a smooth curve instead of straight segments." }
                            }
                        },
                        new Example
                        {
                            Name = "Step Line Chart",
                            Path = "step-line-chart",
                            Description = "Draw a staircase line for values that change in discrete steps with a Blazor step line chart.",
                            Tags = new [] { "chart", "graph", "line", "step", "interpolation" },
                            New = true,
                            Related = new [] { "line-chart", "spline-chart", "step-area-chart" },
                            Faq = new []
                            {
                                new FaqItem { Question = "When should I use a step line?", Answer = "When a value stays constant between readings and changes suddenly - interest rates, inventory counts, a setting that holds until updated. A straight line would imply gradual change that did not happen." },
                                new FaqItem { Question = "How is it different from a spline?", Answer = "A spline smooths between points, suggesting continuous change. A step line does the opposite: it keeps each value level and shows the change as a clean jump." }
                            }
                        },
                        new Example
                        {
                            Name = "Stacked Line Chart",
                            Path = "stacked-line-chart",
                            Description = "Stack series to show a cumulative total over time with a Blazor stacked line chart.",
                            Tags = new [] { "chart", "graph", "line", "stack", "cumulative" },
                            New = true,
                            Related = new [] { "line-chart", "stacked-area-chart", "100-percent-stacked-line-chart" },
                            Faq = new []
                            {
                                new FaqItem { Question = "When should I use a stacked line chart?", Answer = "When several series add up to a meaningful total over time and you want both the total and the parts - though a stacked area chart often reads more clearly for the same data." }
                            }
                        },
                        new Example
                        {
                            Name = "Full Stacked Line Chart",
                            Path = "100-percent-stacked-line-chart",
                            Description = "Trace each series' share of 100% over time with a Blazor full stacked line chart.",
                            Tags = new [] { "chart", "graph", "line", "stack", "percent", "100", "proportional", "full" },
                            New = true,
                            Related = new [] { "stacked-line-chart", "line-chart", "100-percent-stacked-area-chart" },
                            Faq = new []
                            {
                                new FaqItem { Question = "When should I use full stacked line instead of stacked line?", Answer = "When relative share is the story - how the mix between series changes over time - rather than the absolute totals." }
                            }
                        },
                    }
                },
                new Example
                {
                    Name = "Waterfall Chart",
                    Icon = "\uea00",
                    Children = new [] {
                        new Example
                        {
                            Name = "Waterfall Chart",
                            Path = "waterfall-chart",
                            Description = "Explain how a starting value reaches a final total with a Blazor waterfall chart - each step shown as a rise or fall. Free and open source.",
                            Tags = new [] { "chart", "graph", "waterfall", "column", "cumulative", "running total", "summary" },
                            New = true,
                            Related = new [] { "horizontal-waterfall-chart", "column-chart", "negative-column-chart" },
                            Faq = new []
                            {
                                new FaqItem { Question = "When should I use a waterfall chart?", Answer = "When you want to explain how you got from one total to another - opening to closing balance, or budget to actuals - by showing each contributing increase and decrease in order." },
                                new FaqItem { Question = "What data does it need?", Answer = "An ordered list of labeled changes, each a positive or negative value. The chart accumulates them from the starting point and can mark running totals along the way." }
                            }
                        },
                        new Example
                        {
                            Name = "Horizontal Waterfall Chart",
                            Path = "horizontal-waterfall-chart",
                            Description = "Show a step-by-step waterfall laid out along horizontal bars with a Blazor horizontal waterfall chart.",
                            Tags = new [] { "chart", "graph", "waterfall", "bar", "horizontal", "cumulative", "running total", "summary" },
                            New = true,
                            Related = new [] { "waterfall-chart", "bar-chart" },
                            Faq = new []
                            {
                                new FaqItem { Question = "When should I use the horizontal layout?", Answer = "When your step labels are long or there are many steps; horizontal bars give the labels room and keep a tall sequence readable." }
                            }
                        },
                    }
                },
                new Example
                {
                    Name = "Part-to-Whole Charts",
                    Icon = "\uf724",
                    Children = new [] {
                        new Example
                        {
                            Toc = [ new () { Text = "Showcase", Anchor = "#showcase" }, new () { Text = "Basic pie", Anchor = "#basic" }, new () { Text = "Various radius", Anchor = "#various-radius" }, new () { Text = "Segment gap", Anchor = "#segment-gap" }, new () { Text = "Rounded corners", Anchor = "#rounded-corners" }, new () { Text = "Semi-circle", Anchor = "#semi-circle" }, new () { Text = "Explode on hover", Anchor = "#explode-on-hover" }, new () { Text = "Custom colors", Anchor = "#custom-colors" }, new () { Text = "Labels and legend", Anchor = "#labels-legend" }, new () { Text = "Playground", Anchor = "#playground" } ],
                            Name = "Pie Chart",
                            Path = "pie-chart",
                            Description = "Show parts of a whole at a glance with a Blazor pie chart. Free and open source.",
                            Tags = new [] { "chart", "graph", "pie" },
                            Updated = true,
                            Related = new [] { "donut-chart", "column-chart", "funnel-chart" },
                            Faq = new []
                            {
                                new FaqItem { Question = "When should I use a pie chart?", Answer = "When you are showing parts of a single whole and there are only a few of them. With many slices, or when precise comparison matters, a bar or column chart is easier to read." },
                                new FaqItem { Question = "What data does a pie chart need?", Answer = "One value per category; the chart converts each to its share of the total automatically. Bind your collection and set the value and category properties of the pie series." },
                                new FaqItem { Question = "Pie or donut?", Answer = "They show the same thing. A donut leaves a hole in the middle, which gives you space for a total or label and can look cleaner with more slices." }
                            }
                        },
                        new Example
                        {
                            Toc = [ new () { Text = "Showcase", Anchor = "#showcase" }, new () { Text = "Basic donut", Anchor = "#basic" }, new () { Text = "Inner radius", Anchor = "#inner-radius" }, new () { Text = "Center label", Anchor = "#center-label" }, new () { Text = "Various radius", Anchor = "#various-radius" }, new () { Text = "Segment gap", Anchor = "#segment-gap" }, new () { Text = "Rounded corners", Anchor = "#rounded-corners" }, new () { Text = "Semi-circle", Anchor = "#semi-circle" }, new () { Text = "Explode on hover", Anchor = "#explode-on-hover" }, new () { Text = "Custom colors", Anchor = "#custom-colors" }, new () { Text = "Labels and legend", Anchor = "#labels-legend" }, new () { Text = "Playground", Anchor = "#playground" } ],
                            Name = "Donut Chart",
                            Path = "donut-chart",
                            Description = "Show proportions with a Blazor donut chart - a pie with an open center for a total or label.",
                            Tags = new [] { "chart", "graph", "donut" },
                            Updated = true,
                            Related = new [] { "pie-chart", "column-chart", "funnel-chart" },
                            Faq = new []
                            {
                                new FaqItem { Question = "When should I use a donut instead of a pie?", Answer = "When you want to place a total or label in the center, or when a slightly cleaner look helps with a handful of slices. Both convey the same part-to-whole comparison." },
                                new FaqItem { Question = "Can I show the total in the middle?", Answer = "Yes. The open center is a natural place for a summary number or short label." }
                            }
                        },
                        new Example
                        {
                            Name = "Funnel Chart",
                            Path = "funnel-chart",
                            Description = "Visualize conversion and pipeline stages with a Blazor funnel chart as each stage narrows.",
                            Tags = new [] { "chart", "graph", "funnel", "conversion", "pipeline", "sales" },
                            New = true,
                            Related = new [] { "pyramid-chart", "pie-chart", "bar-chart" },
                            Faq = new []
                            {
                                new FaqItem { Question = "When should I use a funnel chart?", Answer = "When you are tracking how many items make it through each stage of a sequence - visitors to signups to purchases, or leads to closed deals - and want the drop-off between stages to stand out." },
                                new FaqItem { Question = "What data does it need?", Answer = "One value per stage, listed in order. Each stage is drawn as a band sized by its value, so the funnel narrows as the values fall." }
                            }
                        },
                        new Example
                        {
                            Name = "Pyramid Chart",
                            Path = "pyramid-chart",
                            Description = "Show ranked levels or a hierarchy with a Blazor pyramid chart, widest at the base.",
                            Tags = new [] { "chart", "graph", "pyramid", "hierarchy", "triangle" },
                            New = true,
                            Related = new [] { "funnel-chart", "pie-chart", "bar-chart" },
                            Faq = new []
                            {
                                new FaqItem { Question = "When should I use a pyramid chart?", Answer = "When your categories form levels or a hierarchy you want to show by size - population age bands, or tiers of a market - with the largest at the bottom." },
                                new FaqItem { Question = "How is it different from a funnel chart?", Answer = "They are mirror images in spirit. A funnel emphasizes drop-off through a process from top to bottom, while a pyramid emphasizes the size of ranked levels built up from a base." }
                            }
                        },
                    }
                },
                new Example
                {
                    Name = "Scatter & Bubble",
                    Icon = "\ue268",
                    Children = new [] {
                        new Example
                        {
                            Name = "Scatter Chart",
                            Path = "scatter-chart",
                            Description = "Plot X/Y points to reveal correlation, clusters, and outliers with a Blazor scatter chart. Free and open source.",
                            Tags = new [] { "chart", "graph", "scatter", "point", "xy" },
                            Related = new [] { "bubble-chart", "scatter-line-chart", "line-chart" },
                            Faq = new []
                            {
                                new FaqItem { Question = "When should I use a scatter chart?", Answer = "When both axes are numeric and you want to see how two measures relate - height against weight, price against demand - or to spot clusters and outliers in raw points." },
                                new FaqItem { Question = "What data does it need?", Answer = "Each point needs an X and a Y value, mapped to the scatter series. Unlike a line or column chart, the category axis is numeric rather than labeled." },
                                new FaqItem { Question = "How is a bubble chart different?", Answer = "A bubble chart adds a third dimension: the size of each point encodes another value, so you can show three measures at once." }
                            }
                        },
                        new Example
                        {
                            Name = "Scatter Line Chart",
                            Path = "scatter-line-chart",
                            Description = "Plot X/Y points joined by a line with a Blazor scatter line chart - markers plus trend on a numeric axis.",
                            Tags = new [] { "chart", "graph", "scatter", "line", "marker", "point" },
                            New = true,
                            Related = new [] { "scatter-chart", "line-chart", "bubble-chart" },
                            Faq = new []
                            {
                                new FaqItem { Question = "When should I use scatter points with a line?", Answer = "When the points sit on a numeric X axis and connecting them helps show a path or trend - a measured curve, or a sequence of readings - while still marking each actual point." },
                                new FaqItem { Question = "How is it different from a line chart?", Answer = "A line chart spaces points evenly along a labeled category axis. A scatter line places them by their true X value, so uneven spacing is preserved." }
                            }
                        },
                        new Example
                        {
                            Name = "Bubble Chart",
                            Path = "bubble-chart",
                            Description = "Show three numeric dimensions at once with a Blazor bubble chart - X, Y, and point size.",
                            Tags = new [] { "chart", "graph", "bubble", "scatter", "size" },
                            Related = new [] { "scatter-chart", "scatter-line-chart", "heatmap-series-chart" },
                            Faq = new []
                            {
                                new FaqItem { Question = "When should I use a bubble chart?", Answer = "When you have a third measure worth showing alongside an X/Y relationship - market size on top of price and share, say - and want it encoded as the size of each point." },
                                new FaqItem { Question = "What data does it need?", Answer = "Each bubble needs X, Y, and a size value. Keep the number of bubbles modest, since overlapping circles get hard to read." }
                            }
                        },
                        new Example
                        {
                            Name = "Heatmap Series Chart",
                            Path = "heatmap-series-chart",
                            Description = "Color a grid of X/Y cells by value with a Blazor heatmap series to spot hotspots and density.",
                            Tags = new [] { "chart", "graph", "heatmap", "grid", "intensity", "density" },
                            New = true,
                            Related = new [] { "contour-chart", "heatmap-chart", "scatter-chart" },
                            Faq = new []
                            {
                                new FaqItem { Question = "When should I use a heatmap?", Answer = "When you want to see how a value varies across two axes at once - activity by hour and day, or density across a grid - and color reads faster than numbers." },
                                new FaqItem { Question = "How is the heatmap series different from the standalone heatmap?", Answer = "The heatmap series plots on numeric X/Y axes inside a chart, alongside other series if needed. The standalone heatmap component is built for a labeled category grid, like a calendar or matrix." }
                            }
                        },
                        new Example
                        {
                            Name = "Contour Chart",
                            Path = "contour-chart",
                            Description = "Map a scalar field as filled bands and iso-lines with a Blazor contour chart - temperature, illuminance, and more.",
                            Tags = new [] { "chart", "graph", "contour", "isoilluminance", "isoline", "isoband", "heatmap", "scalar field" },
                            New = true,
                            Related = new [] { "heatmap-series-chart", "heatmap-chart", "scatter-chart" },
                            Faq = new []
                            {
                                new FaqItem { Question = "When should I use a contour chart?", Answer = "When your data is a value sampled across a 2D grid and you want to see its shape - peaks, valleys, and gradients - as bands of color with optional iso-lines between them." },
                                new FaqItem { Question = "What data does it need?", Answer = "Values on a regular X/Y grid. The chart interpolates between samples to draw smooth bands, and you can turn on iso-lines to outline each level." }
                            }
                        },
                    }
                },
                new Example
                {
                    Name = "Financial Charts",
                    Icon = "\uef92",
                    Children = new [] {
                        new Example
                        {
                            Name = "Candlestick Chart",
                            Path = "candlestick-chart",
                            Description = "Show how a price opened, closed, and swung with a Blazor candlestick chart - one candle per trading period. Free and open source.",
                            Tags = new [] { "chart", "graph", "candlestick", "ohlc", "financial", "stock" },
                            New = true,
                            Related = new [] { "ohlc-chart", "highlow-chart", "line-chart", "area-chart" },
                            Faq = new []
                            {
                                new FaqItem { Question = "How do I read a candlestick?", Answer = "The body spans the opening and closing prices, and the thin wicks above and below reach the period's high and low. By convention the candle is colored one way when the price closed higher than it opened, and another way when it closed lower." },
                                new FaqItem { Question = "What data does the candlestick series need?", Answer = "Each point needs a date and four numbers: open, high, low, and close. Bind your collection and point the Date, Open, High, Low, and Close properties at the matching fields." },
                                new FaqItem { Question = "Should I use a candlestick or an OHLC chart?", Answer = "They show the same four values. Candlesticks make the open-to-close range easy to spot, while OHLC bars stay readable when you are plotting a lot of periods at once." }
                            }
                        },
                        new Example
                        {
                            Name = "OHLC Chart",
                            Path = "ohlc-chart",
                            Description = "Follow price action with a Blazor OHLC bar chart - open, high, low, and close in one compact tick per period. Free and open source.",
                            Tags = new [] { "chart", "graph", "ohlc", "financial", "stock", "open", "high", "low", "close" },
                            New = true,
                            Related = new [] { "candlestick-chart", "highlow-chart", "line-chart" },
                            Faq = new []
                            {
                                new FaqItem { Question = "How is an OHLC bar drawn?", Answer = "Each bar is a vertical line from the low to the high, with a tick on the left for the opening price and a tick on the right for the closing price." },
                                new FaqItem { Question = "When would I pick OHLC over candlesticks?", Answer = "Reach for OHLC bars when you are showing many periods at once. They take up less room and stay legible where candle bodies would start to overlap." },
                                new FaqItem { Question = "What fields does the OHLC series expect?", Answer = "A date plus open, high, low, and close values for each point, mapped through the Date, Open, High, Low, and Close properties." }
                            }
                        },
                        new Example
                        {
                            Name = "High-Low Chart",
                            Path = "highlow-chart",
                            Description = "Show the range between a low and a high value for each point with a Blazor high-low chart. Free and open source.",
                            Tags = new [] { "chart", "graph", "highlow", "high", "low", "range", "temperature" },
                            New = true,
                            Related = new [] { "candlestick-chart", "ohlc-chart", "range-column-chart", "range-bar-chart" },
                            Faq = new []
                            {
                                new FaqItem { Question = "What is a high-low chart good for?", Answer = "Anywhere you care about a range rather than a single number: daily temperature lows and highs, the day's trading range for a stock, or the spread of estimates around a value." },
                                new FaqItem { Question = "What data do I bind to it?", Answer = "Each point needs a category or date and two values, a low and a high, which you map to the Low and High properties of the series." },
                                new FaqItem { Question = "How is it different from a range bar chart?", Answer = "A high-low series draws a thin line between the two values, while a range bar fills the space between them. High-low keeps things light when you have many points to show." }
                            }
                        },
                        new Example
                        {
                            Name = "Box Plot Chart",
                            Path = "box-plot-chart",
                            Description = "Compare distributions with a Blazor box plot - quartiles, median, and outliers at a glance. Free and open source.",
                            Tags = new [] { "chart", "graph", "box", "plot", "whisker", "quartile", "statistics", "distribution" },
                            New = true,
                            Related = new [] { "highlow-chart", "scatter-chart", "column-chart" },
                            Faq = new []
                            {
                                new FaqItem { Question = "What do the parts of a box plot mean?", Answer = "The box runs from the first quartile to the third, so it holds the middle half of the values. The line inside is the median, and the whiskers reach out to the minimum and maximum." },
                                new FaqItem { Question = "When should I use a box plot?", Answer = "When you want to compare the shape of several distributions side by side - test scores across classes, or response times across servers - and you care about spread and outliers, not just the average." },
                                new FaqItem { Question = "What data does the box plot need?", Answer = "Each box is built from a five-number summary - minimum, first quartile, median, third quartile, and maximum - that you provide for every category." }
                            }
                        },
                    }
                },
                new Example
                {
                    Name = "Statistical & Interactive",
                    Icon = "\uf866",
                    Children = new [] {
                        new Example
                        {
                            Name = "Trendlines & Statistical Overlays",
                            Path = "chart-trends",
                            Description = "Add regression, moving-average, and mean/median/mode overlays to any Blazor chart with trendlines and statistical overlays.",
                            Tags = new [] { "chart", "graph", "trendline", "trend", "trends", "regression", "polynomial", "moving average", "forecast", "mean", "median", "mode", "statistics" },
                            New = true,
                            Related = new [] { "line-chart", "scatter-chart", "chart-reference-line" },
                            Faq = new []
                            {
                                new FaqItem { Question = "What overlays can I add to a chart?", Answer = "Linear and polynomial trendlines, a moving average, and statistical markers for mean, median, and mode. Each is added alongside the series it describes." },
                                new FaqItem { Question = "When should I use a trendline?", Answer = "When the raw points are noisy and you want to show the underlying direction - a sales trend through seasonal swings, for example - without changing the data itself." }
                            }
                        },
                        new Example
                        {
                            Name = "Reference Lines & Bands",
                            Path = "chart-reference-line",
                            Description = "Mark targets, thresholds, and acceptable ranges on a Blazor chart with reference lines and bands.",
                            Tags = new [] { "chart", "graph", "reference", "line", "band", "target", "threshold", "limit", "range", "overlay" },
                            New = true,
                            Related = new [] { "chart-trends", "line-chart", "column-chart" },
                            Faq = new []
                            {
                                new FaqItem { Question = "When should I use a reference line or band?", Answer = "When the data only means something relative to a benchmark - a sales target, a safety limit, or a normal range - and you want that benchmark drawn right on the chart." },
                                new FaqItem { Question = "What is the difference between a line and a band?", Answer = "A reference line marks a single value, like a target. A band shades the space between two values, like an acceptable range." }
                            }
                        },
                        new Example
                        {
                            Name = "Synchronized Charts",
                            Path = "chart-sync",
                            Description = "Link multiple Blazor charts with a shared crosshair, active point, and tooltip using synchronized charts.",
                            Tags = new [] { "chart", "graph", "sync", "synchronized", "crosshair", "tooltip", "dashboard", "linked", "export" },
                            New = true,
                            Related = new [] { "line-chart", "chart-trends", "chart-reference-line" },
                            Faq = new []
                            {
                                new FaqItem { Question = "When should I synchronize charts?", Answer = "When several charts share an axis - usually time - and you want to read them together; moving the cursor on one lines up the same point on all of them." },
                                new FaqItem { Question = "How do I link charts together?", Answer = "Put the charts in the same sync group; they then coordinate their crosshair, active point, and tooltips." }
                            }
                        },
                        new Example
                        {
                            Name = "Pareto Chart",
                            Path = "pareto-chart",
                            Description = "Find the vital few with a Blazor pareto chart - sorted bars plus a cumulative line. Free and open source.",
                            Tags = new [] { "chart", "graph", "pareto", "cumulative", "quality" },
                            New = true,
                            Related = new [] { "column-chart", "line-chart", "funnel-chart" },
                            Faq = new []
                            {
                                new FaqItem { Question = "When should I use a pareto chart?", Answer = "When you want to find the vital few - the handful of causes, defects, or customers that drive most of the result - and show how quickly the cumulative total adds up." },
                                new FaqItem { Question = "What data does it need?", Answer = "A value per category. The chart sorts the bars from largest to smallest and overlays a line of the running cumulative percentage." },
                                new FaqItem { Question = "Why combine bars and a line?", Answer = "The bars rank the categories by size, while the cumulative line shows how few of them it takes to reach most of the total." }
                            }
                        },
                        new Example
                        {
                            Name = "DrillDown Chart",
                            Path = "drilldown-chart",
                            Description = "Let readers click from summary to detail with an interactive Blazor drill-down chart.",
                            Tags = new [] { "chart", "graph", "drilldown", "drill", "click", "interactive", "navigation", "hierarchy" },
                            New = true,
                            Related = new [] { "column-chart", "pie-chart", "line-chart" },
                            Faq = new []
                            {
                                new FaqItem { Question = "When should I use drill-down?", Answer = "When your data is hierarchical and a single view would be too dense; start with the summary and let readers click into the level they care about." },
                                new FaqItem { Question = "How does the interaction work?", Answer = "Handle the click on a data point and swap in the detailed data for that point; a back step returns to the previous level." }
                            }
                        },
                        new Example
                        {
                            Name = "Bullet Chart",
                            Path = "bullet-chart",
                            Description = "Show a KPI against its target in compact space with a Blazor bullet chart.",
                            Tags = new [] { "chart", "graph", "bullet", "gauge", "target", "kpi", "performance" },
                            New = true,
                            Related = new [] { "arc-gauge", "linear-gauge", "column-chart" },
                            Faq = new []
                            {
                                new FaqItem { Question = "When should I use a bullet chart?", Answer = "When you need to show performance against a target in little space - a KPI on a dashboard, where a full gauge would be too big - with bands for poor, okay, and good." },
                                new FaqItem { Question = "What data does it need?", Answer = "A measured value, a target to compare against, and optional range thresholds that shade the background." },
                                new FaqItem { Question = "How is it different from a gauge?", Answer = "A bullet chart carries the same information as a gauge in a fraction of the space, which makes it better when you have many KPIs to line up." }
                            }
                        },
                        new Example
                        {
                            Name = "Live Chart",
                            Path = "live-chart",
                            Description = "Stream real-time data over a rolling window with a live updating Blazor chart.",
                            Tags = new [] { "chart", "graph", "live", "real-time", "streaming", "update", "timer", "monitor", "telemetry" },
                            New = true,
                            Related = new [] { "line-chart", "area-chart", "scatter-line-chart" },
                            Faq = new []
                            {
                                new FaqItem { Question = "How do I build a live updating chart?", Answer = "Append new points on a timer or from a data feed and drop old ones past your window; the chart redraws as the bound data changes." },
                                new FaqItem { Question = "When should I use a live chart?", Answer = "For real-time monitoring where the latest values matter most - system metrics, sensor readings, market ticks - rather than a fixed historical range." }
                            }
                        },
                    }
                },
                new Example
                {
                    Name = "Gauges",
                    Icon = "\ue9e4",
                    Children = new [] {
                        new Example
                        {
                            Name = "Arc Gauge",
                            Path = "arc-gauge",
                            Description = "Show a value on a curved arc with a Blazor arc gauge - a compact dial for cards and tiles.",
                            Tags = new [] { "gauge", "graph", "arc", "progress" },
                            Related = new [] { "radial-gauge", "linear-gauge", "bullet-chart" },
                            Faq = new []
                            {
                                new FaqItem { Question = "When should I use an arc gauge?", Answer = "When you want a dial but have limited height; the shallow arc shows the same value-against-range without the footprint of a full circle." },
                                new FaqItem { Question = "Can I show colored ranges and a value label?", Answer = "Yes. Add scale ranges to shade sections and place the current value in the center of the arc." }
                            }
                        },
                        new Example
                        {
                            Name = "Linear Gauge",
                            Path = "linear-gauge",
                            Description = "Show a value on a straight scale with a Blazor linear gauge - horizontal or vertical, with ranges and pointers.",
                            Tags = new [] { "gauge", "graph", "linear", "scale", "bar" },
                            Related = new [] { "radial-gauge", "arc-gauge", "bullet-chart" },
                            Faq = new []
                            {
                                new FaqItem { Question = "When should I use a linear gauge?", Answer = "When a straight scale fits your layout better than a dial - a vertical level beside a stat, or a horizontal meter in a row - or when you want several scales side by side." },
                                new FaqItem { Question = "What can I configure?", Answer = "The orientation, scale ranges and ticks, one or more pointers (including a draggable one), and a reversed scale." }
                            },
                            Toc =
                            [
                                new () { Text = "Basic Usage", Anchor = "#basic-usage" },
                                new () { Text = "Ranges and Value Display", Anchor = "#ranges" },
                                new () { Text = "Rounded Ranges", Anchor = "#rounded-ranges" },
                                new () { Text = "Reversed Scale", Anchor = "#reversed" },
                                new () { Text = "Draggable Pointer", Anchor = "#draggable" },
                                new () { Text = "Vertical Orientation", Anchor = "#vertical" },
                                new () { Text = "Multiple Scales", Anchor = "#multiple-scales" },
                            ]
                        },
                        new Example
                        {
                            Name = "Radial Gauge",
                            Path = "radial-gauge",
                            Description = "Show a value on a circular dial with a Blazor radial gauge - speedometer-style, with ranges and pointers. Free and open source.",
                            Tags = new [] { "gauge", "graph", "radial", "circle" },
                            Related = new [] { "arc-gauge", "linear-gauge", "bullet-chart" },
                            Faq = new []
                            {
                                new FaqItem { Question = "When should I use a radial gauge?", Answer = "When you want to show one value against a known range at a glance - a score, a utilization percentage, a speed - and a dial reads more naturally than a number alone." },
                                new FaqItem { Question = "What can I configure on it?", Answer = "The scale range and ticks, one or more pointers, and colored ranges that shade sections of the dial to mark good, warning, and critical zones." },
                                new FaqItem { Question = "Radial, arc, or linear gauge?", Answer = "Radial is a full or near-full circle, arc is a shallower curved segment that fits tighter layouts, and linear is a straight bar. They show the same kind of value; pick the shape that fits your space." }
                            }
                        },
                        new Example
                        {
                            Name = "Styling Gauge",
                            Path = "styling-gauge",
                            Title = "Blazor Gauge - Styling | Free UI Components by Radzen",
                            Description = "Style Blazor gauges with multiple pointers, multiple scales, and custom colors.",
                            Tags = new [] { "gauge", "graph", "styling" },
                            Related = new [] { "radial-gauge", "arc-gauge", "linear-gauge" },
                            Faq = new []
                            {
                                new FaqItem { Question = "How much can I customize a gauge's appearance?", Answer = "Quite a lot: multiple pointers and scales on one gauge, custom colors and ranges, tick formatting, and center content - enough to match most dashboard designs." }
                            }
                        },
                    }
                },
                new Example
                {
                    Name = "Zoom & Navigation",
                    Icon = "\ue8ff",
                    Children = new [] {
                        new Example
                        {
                            Name = "Zoom and Pan",
                            Path = "zoom-pan-chart",
                            Description = "Let readers scroll to zoom and drag to pan through a large dataset with an interactive Blazor chart.",
                            Tags = new [] { "chart", "zoom", "pan", "scroll", "interactive", "mouse", "wheel" },
                            New = true,
                            Related = new [] { "range-navigator", "line-chart", "live-chart" },
                            Faq = new []
                            {
                                new FaqItem { Question = "When should I enable zoom and pan?", Answer = "When a series has more points than fit comfortably at once - a year of daily readings, say - and you want readers to focus on a slice and move through it rather than see it all at once." },
                                new FaqItem { Question = "How do readers control it?", Answer = "The mouse wheel zooms in and out, and a scrollbar or drag pans along the axis." }
                            }
                        },
                        new Example
                        {
                            Toc = [ new () { Text = "With Series", Anchor = "#with-series" }, new () { Text = "Compact", Anchor = "#compact" } ],
                            Name = "Range Navigator",
                            Path = "range-navigator",
                            Description = "Frame the visible window of a chart with a draggable Blazor range navigator overview strip.",
                            Tags = new [] { "chart", "range", "navigator", "selector", "zoom", "pan", "interactive" },
                            New = true,
                            Related = new [] { "zoom-pan-chart", "line-chart", "area-chart" },
                            Faq = new []
                            {
                                new FaqItem { Question = "When should I use a range navigator?", Answer = "When readers need to move through a long series and keep their bearings; the mini-chart shows the whole picture while the selection frames the part on display." },
                                new FaqItem { Question = "How is it different from zoom and pan?", Answer = "Zoom and pan act on the chart itself. A range navigator adds a separate overview strip, so readers always see the full extent while choosing the window." }
                            }
                        },
                    }
                },
                new Example
                {
                    Name = "Heatmap",
                    Path = "heatmap-chart",
                    Description = "Show values on a labeled color-coded grid with a Blazor heatmap - calendars, matrices, and density. Free and open source.",
                    Tags = new [] { "chart", "heatmap", "grid", "matrix", "color", "intensity" },
                    Icon = "\ue8f0",
                    New = true,
                    Related = new [] { "heatmap-series-chart", "treemap-chart", "contour-chart" },
                    Faq = new []
                    {
                        new FaqItem { Question = "When should I use a heatmap?", Answer = "When you want to compare a value across two categorical dimensions at once - day against hour, product against region - and color makes the pattern faster to read than numbers." },
                        new FaqItem { Question = "What data does it need?", Answer = "A value for each row-and-column pair. The component lays them out on the grid and maps each value to a color from your scheme." },
                        new FaqItem { Question = "How is it different from the heatmap chart series?", Answer = "This standalone heatmap is built for labeled category grids like calendars and matrices. The heatmap series plots on numeric X/Y axes inside a chart, alongside other series." }
                    }
                },
                new Example
                {
                    Name = "Treemap",
                    Path = "treemap-chart",
                    Description = "Show hierarchy and proportion as nested rectangles with a Blazor treemap.",
                    Tags = new [] { "chart", "treemap", "hierarchy", "rectangle", "proportion", "area" },
                    Related = new [] { "sankey-diagram", "pie-chart", "column-chart" },
                    Faq = new []
                    {
                        new FaqItem { Question = "When should I use a treemap?", Answer = "When you have hierarchical or part-to-whole data with many items and limited space; the rectangle sizes show proportion without the gaps a pie or bar chart would leave." },
                        new FaqItem { Question = "What data does it need?", Answer = "Items with a value, optionally nested into parent groups. Each rectangle is sized by its share, and groups are drawn as blocks of their children." }
                    },
                    Icon= "\ue8f1",
                    New = true
                },
                new Example
                {
                    Name = "Sparkline",
                    Path = "sparkline",
                    Description = "Show a trend inline in word-sized space with a Blazor sparkline - no axes or labels.",
                    Icon = "\uf64f",
                    Tags = new [] { "chart", "sparkline" },
                    Related = new [] { "line-chart", "area-chart", "bullet-chart" },
                    Faq = new []
                    {
                        new FaqItem { Question = "When should I use a sparkline?", Answer = "When you want to show the shape of a trend in very little space - a row in a table, a KPI card - where a full chart would be too much and a single number too little." },
                        new FaqItem { Question = "What data does it need?", Answer = "Just a series of values. The sparkline draws their shape without axes, gridlines, or a legend." }
                    }
                },
                new Example
                {
                    Toc = [ new () { Text = "Basic Usage", Anchor = "#basic-usage" }, new () { Text = "Grid Shape", Anchor = "#grid-shape" }, new () { Text = "Color Scheme", Anchor = "#color-scheme" }, new () { Text = "Legend", Anchor = "#legend" }, new () { Text = "Value Format", Anchor = "#value-format" }, new () { Text = "Markers", Anchor = "#markers" } ],
                    Name = "Spider Chart",
                    Path = "spider-chart",
                    Title = "Blazor Spider Chart Component | Free UI Components by Radzen",
                    Description = "Compare a profile across many dimensions with a Blazor spider (radar) chart.",
                    Tags = new [] { "spider", "radar", "chart", "multivariate", "radial", "web" },
                    Related = new [] { "radar-column-chart", "line-chart", "column-chart" },
                    Faq = new []
                    {
                        new FaqItem { Question = "When should I use a spider chart?", Answer = "When you want to compare items across several measures at once and see their overall shape - player stats, product attributes, or assessment scores across categories." },
                        new FaqItem { Question = "What data does it need?", Answer = "A value per axis for each series. Every series is drawn as a closed shape across the same set of axes, so you can overlay and compare them." },
                        new FaqItem { Question = "Spider or radar column?", Answer = "They share the radial layout. A spider chart connects points into a shape, while a radar column draws bars out from the center - use columns when you want to compare each category's magnitude rather than an overall profile." }
                    },
                    Icon = "\ueb39",
                    Updated = true
                },
                new Example
                {
                    Name = "Radar Column Chart",
                    Path = "radar-column-chart",
                    Description = "Arrange columns around a circle for cyclical categories with a Blazor radar column chart.",
                    Tags = new [] { "spider", "radar", "column", "chart", "radial", "bar" },
                    Related = new [] { "spider-chart", "column-chart", "pie-chart" },
                    Faq = new []
                    {
                        new FaqItem { Question = "When should I use a radar column chart?", Answer = "When your categories are cyclical or you want a compact circular comparison - seasonal patterns by month, or activity by hour of day - rather than a long horizontal axis." },
                        new FaqItem { Question = "How is it different from a spider chart?", Answer = "A spider chart connects values into a shape to compare profiles. A radar column draws each value as a bar from the center, which reads better when you care about each category's size." }
                    },
                    Icon = "\uf04e",
                    New = true
                },
                new Example
                {
                    Toc = [ new () { Text = "Basic Usage", Anchor = "#basic-usage" }, new () { Text = "Color Scheme", Anchor = "#color-scheme" }, new () { Text = "Node Properties", Anchor = "#node-properties" }, new () { Text = "Custom Colors", Anchor = "#custom-colors" }, new () { Text = "Custom Tooltips", Anchor = "#custom-tooltips" }, new () { Text = "Animation", Anchor = "#animation" } ],
                    Name = "Sankey Diagram",
                    Path = "sankey-diagram",
                    Description = "Trace flow between stages as proportional bands with a Blazor Sankey diagram.",
                    Icon = "\uf38d",
                    Tags = new [] { "sankey", "flow", "diagram", "visualization", "relationships" },
                    Related = new [] { "treemap-chart", "funnel-chart", "bar-chart" },
                    Faq = new []
                    {
                        new FaqItem { Question = "When should I use a Sankey diagram?", Answer = "When you want to show how a quantity flows and divides between stages - traffic from sources to pages, money from income to spending - with the band widths carrying the magnitude." },
                        new FaqItem { Question = "What data does it need?", Answer = "A list of links, each with a source node, a target node, and a value. The diagram lays out the nodes and sizes the connecting bands by value." },
                        new FaqItem { Question = "Is it good for many nodes?", Answer = "It handles a fair number, but very dense flows get hard to read; group small flows or split the diagram when it gets crowded." }
                    }
                },
                new Example
                {
                    Toc = [ new () { Text = "Basic Usage", Anchor = "#basic-usage" }, new () { Text = "Orientation and Position", Anchor = "#orientation-and-position" }, new () { Text = "Align Items", Anchor = "#align-items" }, new () { Text = "Styling", Anchor = "#line-width" }, new () { Text = "Point Size", Anchor = "#point-size" }, new () { Text = "Point Style", Anchor = "#point-style" }, new () { Text = "Point Variant", Anchor = "#point-variant" }, new () { Text = "Point Shadow", Anchor = "#point-shadow" }, new () { Text = "Point Content", Anchor = "#point-content" } ],
                    Name = "Timeline",
                    Path = "timeline",
                    Description = "Blazor Timeline component for displaying a chronological sequence of events with flexible orientation and styling.",
                    Icon = "\ue00d",
                    Tags = new [] { "timeline", "time", "line" }
                },
                new Example
                {
                    Toc = [ new () { Text = "Basic", Anchor = "#basic" }, new () { Text = "Large", Anchor = "#large" }, new () { Text = "Styled", Anchor = "#styled" } ],
                    Name = "QRCode",
                    Description = "Generate and display QR codes as SVG using RadzenQRCode.",
                    Path = "qrcode",
                    Icon = "\uef6b",
                    Tags = new [] { "qr", "qrcode", "barcode", "svg" }
                },
                new Example
                {
                    Name = "Barcode",
                    Description = "Generate and display 1D barcodes as SVG using RadzenBarcode.",
                    Path = "barcode",
                    Icon = "\ue70b",
                    Tags = new [] { "barcode", "svg" }
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
                    Title = "Blazor AI Chat Component | Free UI Components by Radzen",
                    Path = "aichat",
                    Description = "The Blazor AI Chat component provides a conversational, streaming chat interface for AI assistants.",
                    Related = new [] { "chat", "ai", "speechtotextbutton" },
                    Faq = new []
                    {
                        new FaqItem { Question = "What is the Blazor AI Chat component?", Answer = "It is a chat UI for AI assistants, with a conversational layout and support for streaming responses as they are generated." }
                    },
                    Icon = "\ue0b7",
                    Tags = new [] { "chat", "ai", "conversation", "message", "streaming" }
                },
                new Example
                {
                    Name = "Chat",
                    Title = "Blazor Chat Component | Free UI Components by Radzen",
                    Path = "chat",
                    Description = "The Blazor Chat component supports multi-participant conversations with distinct user identities and real-time messaging.",
                    Related = new [] { "aichat", "ai" },
                    Faq = new []
                    {
                        new FaqItem { Question = "Does the Blazor Chat support multiple participants?", Answer = "Yes. It renders messages from distinct users with their own identity and supports real-time, multi-participant conversations." }
                    },
                    Icon = "\uefd1",
                    Tags = new [] { "chat", "conversation", "message", "users", "team", "group" }
                },
                new Example
                {
                    Toc = [ new () { Text = "Basic Label with Input", Anchor = "#basic-usage" }, new () { Text = "Labels with Different Input Types", Anchor = "#input-types" }, new () { Text = "Label with Custom Content", Anchor = "#custom-content" }, new () { Text = "Required Field Indicators", Anchor = "#required-fields" }, new () { Text = "Label Typography", Anchor = "#typography" }, new () { Text = "Label Styling", Anchor = "#styling" } ],
                    Name = "Label",
                    Updated = true,
                    Title = "Blazor Label Component | Free UI Components by Radzen",
                    Description = "Associate descriptive text labels with form inputs for better accessibility and usability. Clicking a label focuses its associated input.",
                    Path = "label",
                    Icon = "\ue893",
                    Tags = new [] { "label", "form", "input", "accessibility", "required", "validation", "formfield", "association", "aria" },
                    Related = new [] { "form-field", "fieldset", "templateform" },
                    Faq = new []
                    {
                        new FaqItem { Question = "Why use the Blazor Label component?", Answer = "It links descriptive text to a form input so clicking the label focuses the input and screen readers announce it, which improves accessibility." }
                    }
                },
                new Example
                {
                    Toc = [ new () { Text = "Get and Set the value of AutoComplete", Anchor = "#get-set-value" }, new () { Text = "Get and Set the value of AutoComplete using Value and Change event", Anchor = "#value-and-change-event" }, new () { Text = "Get the selected item of AutoComplete", Anchor = "#get-selected" }, new () { Text = "Define AutoComplete placeholder", Anchor = "#placeholder" }, new () { Text = "Define AutoComplete template", Anchor = "#template" }, new () { Text = "Change AutoComplete filter operator, case sensitivity and delay", Anchor = "#filter-operator" }, new () { Text = "Load data on-demand in AutoComplete and apply custom filter and sort", Anchor = "#load-on-demand" }, new () { Text = "Empty and Loading templates", Anchor = "#empty-and-loading-templates" }, new () { Text = "AutoComplete with a List of Strings", Anchor = "#list-of-strings" }, new () { Text = "Multiline AutoComplete", Anchor = "#multiline" }, new () { Text = "Open on Focus", Anchor = "#open-on-focus" }, new () { Text = "Disabled AutoComplete", Anchor = "#disabled-autocomplete" }, new () { Text = "AutoComplete Sizes", Anchor = "#sizes" }, new () { Text = "Keyboard Navigation", Anchor = "#keyboard-navigation" } ],
                    Name = "AutoComplete",
                    Title = "Blazor AutoComplete - Search Suggestions | Free UI Components by Radzen",
                    Path = "autocomplete",
                    Description = "The Blazor AutoComplete suggests matching items as the user types, with templates, custom filter operators, and on-demand data loading.",
                    Related = new [] { "dropdown", "listbox", "textbox" },
                    Faq = new []
                    {
                        new FaqItem { Question = "How is AutoComplete different from a DropDown?", Answer = "AutoComplete is a free-text input that suggests matching items as the user types, while a DropDown restricts selection to the listed options." },
                        new FaqItem { Question = "How do I load AutoComplete suggestions on demand?", Answer = "Handle the LoadData event to fetch and filter suggestions from the server as the user types." }
                    },
                    Icon = "\ue03b",
                    Tags = new [] { "form", "complete", "suggest", "edit" }
                },
                new Example
                {
                    Toc = [ new () { Text = "Filled Buttons", Anchor = "#filled-buttons" }, new () { Text = "Flat Buttons", Anchor = "#flat-buttons" }, new () { Text = "Outlined Buttons", Anchor = "#outlined-buttons" }, new () { Text = "Text Buttons", Anchor = "#text-buttons" }, new () { Text = "Content in Buttons", Anchor = "#content-in-buttons" }, new () { Text = "Button Sizes", Anchor = "#button-sizes" }, new () { Text = "FAB", Anchor = "#fab" }, new () { Text = "Disabled Button", Anchor = "#disabled-button" }, new () { Text = "Busy button", Anchor = "#busy-button" }, new () { Text = "Keyboard Navigation", Anchor = "#keyboard-navigation" } ],
                    Name = "Button",
                    Title = "Blazor Button Component | Free UI Components by Radzen",
                    Description = "The Radzen Blazor Button comes in filled, flat, outlined, and text variants, with sizes, icons, shades, busy and disabled states, and click handling.",
                    Path = "button",
                    Tags = new [] { "button", "form", "click" },
                    Related = new [] { "splitbutton", "toggle-button", "fab" },
                    Faq = new []
                    {
                        new FaqItem { Question = "What button styles does the Radzen Blazor Button support?", Answer = "It supports filled, flat, outlined, and text variants, each with shades, sizes, and icon support, so you can match primary, secondary, and subtle actions." },
                        new FaqItem { Question = "How do I add an icon to a Blazor Button?", Answer = "Set the Icon property to a Material icon name; you can combine it with Text or use an icon-only button." },
                        new FaqItem { Question = "How do I show a busy or disabled Button?", Answer = "Set IsBusy to true to show a spinner and block clicks during an operation, and set Disabled to true to disable the button." }
                    },
                    Icon = "\ue72f"
                },
                new Example
                {
                    Toc = [ new () { Text = "Bound ToggleButton", Anchor = "#bound-toggle-button" }, new () { Text = "ToggleButton Shade", Anchor = "#shade" }, new () { Text = "ToggleButton Style", Anchor = "#style" }, new () { Text = "ToggleButton Variants", Anchor = "#variants" }, new () { Text = "Content in ToggleButtons", Anchor = "#content" }, new () { Text = "ToggleButton Sizes", Anchor = "#sizes" }, new () { Text = "Disabled ToggleButton", Anchor = "#disabled" }, new () { Text = "Keyboard Navigation", Anchor = "#keyboard-navigation" } ],
                    Name = "ToggleButton",
                    Title = "Blazor ToggleButton - Toggle / On-Off Button | Free UI Components by Radzen",
                    Description = "The Blazor ToggleButton switches between on and off states, changing its appearance when activated - ideal for toolbars and settings.",
                    Path = "toggle-button",
                    Icon = "\ue8e0",
                    Tags = new [] { "button", "switch", "toggle" },
                    Related = new [] { "button", "switch", "selectbar" },
                    Faq = new []
                    {
                        new FaqItem { Question = "How do I bind the Blazor ToggleButton state?", Answer = "Bind Value to a bool with @bind-Value; it is true when the button is toggled on and false when off." }
                    }
                },
                new Example
                {
                    Toc = [ new () { Text = "Get and Set the value of CheckBox", Anchor = "#get-set-value" }, new () { Text = "Get and Set the value of CheckBox using Value and Change event", Anchor = "#value-and-change-event" }, new () { Text = "TriState CheckBox", Anchor = "#tristate-checkbox" }, new () { Text = "Disabled CheckBox", Anchor = "#disabled-checkbox" }, new () { Text = "ReadOnly CheckBox", Anchor = "#readonly-checkbox" }, new () { Text = "Keyboard Navigation", Anchor = "#keyboard-navigation" } ],
                    Name = "CheckBox",
                    Title = "Blazor CheckBox Component | Free UI Components by Radzen",
                    Path = "checkbox",
                    Description = "The Blazor CheckBox binds a bool value with optional tri-state (true/false/null) support, plus disabled and read-only modes.",
                    Related = new [] { "checkboxlist", "switch", "radiobuttonlist" },
                    Faq = new []
                    {
                        new FaqItem { Question = "How do I enable tri-state in the Blazor CheckBox?", Answer = "Set TriState to true and bind Value to a nullable bool; the checkbox then cycles through checked, unchecked, and null (indeterminate)." }
                    },
                    Icon = "\ue834",
                    Tags = new [] { "form", "edit" }
                },
                new Example
                {
                    Toc = [ new () { Text = "Get and Set the value of CheckBoxList", Anchor = "#get-set-value" }, new () { Text = "Get and Set the value of CheckBoxList using Value and Change event", Anchor = "#value-and-change-event" }, new () { Text = "Set CheckBoxList orientation and layout", Anchor = "#orientation" }, new () { Text = "Populate CheckBoxList items from data", Anchor = "#populate-items" }, new () { Text = "Statically declared and populated CheckBoxList items from data", Anchor = "#statically-declared" }, new () { Text = "Select all CheckBoxList items", Anchor = "#select-all-items" }, new () { Text = "Disabled CheckBoxList item", Anchor = "#disabled-item" }, new () { Text = "ReadOnly CheckBoxList item", Anchor = "#readonly-item" }, new () { Text = "Templated CheckBoxList item", Anchor = "#templated-item" }, new () { Text = "Keyboard Navigation", Anchor = "#keyboard-navigation" } ],
                    Name = "CheckBoxList",
                    Title = "Blazor CheckBoxList - Multiple Checkboxes | Free UI Components by Radzen",
                    Path = "checkboxlist",
                    Description = "The Blazor CheckBoxList lets users select multiple items from a data-bound list, with orientation, select-all, and item templates.",
                    Related = new [] { "checkbox", "radiobuttonlist", "listbox" },
                    Faq = new []
                    {
                        new FaqItem { Question = "How do I select multiple items with the Blazor CheckBoxList?", Answer = "Bind Value to a collection; each checked item is added to it. Populate the list with Data plus TextProperty and ValueProperty." }
                    },
                    Icon = "\ue6b1",
                    Tags = new [] { "form", "edit" }
                },
                new Example
                {
                    Toc = [ new () { Text = "ColorPicker configuration", Anchor = "#configuration" }, new () { Text = "ColorPicker Sizes", Anchor = "#sizes" }, new () { Text = "Keyboard Navigation", Anchor = "#keyboard-navigation" } ],
                    Name = "ColorPicker",
                    Title = "Blazor ColorPicker Component | Free UI Components by Radzen",
                    Description = "The Blazor ColorPicker lets users choose a color with HSV and RGBA modes, opacity, and predefined palettes.",
                    Path = "colorpicker",
                    Icon = "\ue40a",
                    Tags = new [] { "form", "edit", "color", "picker" },
                    Related = new [] { "slider", "numeric" },
                    Faq = new []
                    {
                        new FaqItem { Question = "How do I get the selected color from the Blazor ColorPicker?", Answer = "Bind Value to a string; the ColorPicker sets it to the chosen color (for example an rgba() or hex value) and raises Change when the user picks a new color." }
                    }
                },
                new Example
                {
                    Toc = [ new () { Text = "Get and Set the value of DatePicker", Anchor = "#get-set-value" }, new () { Text = "DatePicker with immediate value update", Anchor = "#immediate" }, new () { Text = "Get and Set the value of DatePicker using Value and Change event", Anchor = "#value-and-change-event" }, new () { Text = "DatePicker with time", Anchor = "#datepicker-with-time" }, new () { Text = "Define hour format", Anchor = "#hour-format" }, new () { Text = "Time-only DatePicker", Anchor = "#time-only-datepicker" }, new () { Text = "DatePicker with special or disabled dates", Anchor = "#special-disabled-dates" }, new () { Text = "DatePicker with initial view date and year range", Anchor = "#initial-view-date-and-year-change" }, new () { Text = "Set Min and Max dates", Anchor = "#min-max-dates" }, new () { Text = "DatePicker with custom footer", Anchor = "#custom-footer" }, new () { Text = "DatePicker with custom input parsing", Anchor = "#custom-input-parsing" }, new () { Text = "DatePicker as calendar", Anchor = "#calendar" }, new () { Text = "DatePicker for year/month selection", Anchor = "#year-month-selection" }, new () { Text = "DatePicker binds to types DateOnly or TimeOnly", Anchor = "#dateonly-timeonly" }, new () { Text = "DatePicker Sizes", Anchor = "#sizes" }, new () { Text = "Keyboard Navigation", Anchor = "#keyboard-navigation" } ],
                    Name = "DatePicker",
                    Path = "datepicker",
                    Title = "Blazor DatePicker & Calendar Component | Free UI Components by Radzen",
                    Description = "The Radzen Blazor DatePicker is a date and time picker with an inline calendar mode, time selection, date ranges, min/max and disabled dates, and DateOnly/TimeOnly binding.",
                    Icon = "\ue916",
                    Tags = new [] { "calendar", "time", "form", "edit", "datepicker" },
                    Related = new [] { "timespanpicker", "numeric", "scheduler", "mask" },
                    Faq = new []
                    {
                        new FaqItem { Question = "Can I use the Blazor DatePicker as a calendar?", Answer = "Yes. Set Inline to true to render an always-visible calendar instead of a popup, and bind Value to the selected date." },
                        new FaqItem { Question = "How do I let users pick a date and time together?", Answer = "Set ShowTime to true so the picker includes a time selector alongside the calendar; use HourFormat to switch between 12- and 24-hour input." },
                        new FaqItem { Question = "How do I disable specific or past dates?", Answer = "Use the DateRender callback to mark dates as disabled, and set Min and Max to bound the selectable range." },
                        new FaqItem { Question = "Does the DatePicker support DateOnly and TimeOnly?", Answer = "Yes. It binds to DateTime, DateTimeOffset, DateOnly, and TimeOnly values." }
                    }
                },
                new Example
                {
                    Name = "DropDown",
                    Icon = "\ue172",
                    Children = new [] {
                        new Example
                        {
                            Toc = [ new () { Text = "Get and Set the value of DropDown", Anchor = "#get-set-value" }, new () { Text = "Get and Set the value of DropDown using Value and Change event", Anchor = "#value-and-change-event" }, new () { Text = "Define Text and Value properties", Anchor = "#text-and-value-properties" }, new () { Text = "DropDown with template", Anchor = "#template" }, new () { Text = "Disable specific item", Anchor = "#disable-item" }, new () { Text = "Clear selected item", Anchor = "#clear-selected-item" }, new () { Text = "Editable DropDown", Anchor = "#editable-dropdown" }, new () { Text = "Open and close events", Anchor = "#open-and-close-event" }, new () { Text = "DropDown Sizes", Anchor = "#sizes" }, new () { Text = "Keyboard Navigation", Anchor = "#keyboard-navigation" } ],
                            Name = "Single selection",
                            Path = "dropdown",
                            Title = "Blazor DropDown / Select Component | Free UI Components by Radzen",
                            Description = "Free Blazor DropDown (select) component with data binding, filtering, multiple selection, grouping, templates, and virtualization for large lists. Bind to any IEnumerable or IQueryable.",
                            Tags = new [] { "select", "picker", "form" , "edit", "dropdown", "combobox", "multiselect" },
                            Related = new [] { "dropdown-multiple", "dropdown-filtering", "dropdown-grouping", "dropdown-virtualization", "dropdown-tree", "dropdown-datagrid" },
                            Faq = new []
                            {
                                new FaqItem { Question = "How do I bind data to the Blazor DropDown?", Answer = "Set the Data property to any IEnumerable or IQueryable and use TextProperty and ValueProperty to choose the display text and the bound value. Bind the selection with @bind-Value." },
                                new FaqItem { Question = "How do I enable multiple selection (multiselect)?", Answer = "Set Multiple to true and bind Value to a collection. The DropDown then shows checkboxes and a summary of the selected items." },
                                new FaqItem { Question = "How do I add search or filtering to the DropDown?", Answer = "Set AllowFiltering to true so users can type to filter the list. You can choose the filter operator and case sensitivity, or load filtered data on demand." },
                                new FaqItem { Question = "Can the DropDown handle large lists?", Answer = "Yes. Turn on virtualization and bind to an IQueryable so only the visible options are rendered and fetched, which keeps it fast on very large lists." }
                            },
                        },
                        new Example
                        {
                            Toc = [ new () { Text = "Define max labels and selected items text", Anchor = "#define-max-labels-and-selected-items-text" }, new () { Text = "Specify an Equality Comparer for item selection. Useful when binding directly to an object collection.", Anchor = "#item-comparer" } ],
                            Name = "Multiple selection",
                            Path = "dropdown-multiple",
                            Title = "Blazor MultiSelect DropDown | Free UI Components by Radzen",
                            Description = "Select multiple items from the Blazor DropDown (multiselect). Bind to a collection, show a summary label, and set an equality comparer when binding to objects.",
                            Tags = new [] { "select", "picker", "form" , "edit", "multiple", "dropdown", "multiselect" },
                            Related = new [] { "dropdown", "dropdown-filtering", "dropdown-grouping", "dropdown-tree" },
                            Faq = new []
                            {
                                new FaqItem { Question = "How do I enable multiple selection in the Blazor DropDown?", Answer = "Set Multiple to true and bind Value to a collection (such as a List). The DropDown shows checkboxes and a summary of the selected items." },
                                new FaqItem { Question = "How do I bind multiselect to a list of objects?", Answer = "Bind Value to a collection of the value type and set an equality comparer so the DropDown can match selected objects back to the list items." }
                            },
                        },
                        new Example
                        {
                            Name = "Virtualization",
                            Path = "dropdown-virtualization",
                            Title = "Blazor DropDown - Virtualization for Large Lists | Free UI Components by Radzen",
                            Description = "Render large Blazor DropDown lists efficiently with UI virtualization. Load items on demand from an IQueryable so only the visible options are fetched.",
                            Tags = new [] { "select", "picker", "form" , "edit", "multiple", "dropdown", "virtualization", "paging" },
                            Related = new [] { "dropdown", "dropdown-multiple", "dropdown-filtering", "datagrid-virtualization" },
                            Faq = new []
                            {
                                new FaqItem { Question = "How does the Blazor DropDown handle very large lists?", Answer = "Turn on virtualization and bind to an IQueryable. Only the visible options are rendered and fetched, so the dropdown stays fast even with tens of thousands of items." }
                            },
                        },
                        new Example
                        {
                            Name = "Filtering",
                            Path = "dropdown-filtering",
                            Title = "Blazor DropDown - Filtering & Search | Free UI Components by Radzen",
                            Description = "Add search to the Blazor DropDown with built-in filtering. Choose the filter operator (contains, starts with), toggle case sensitivity, or filter data on demand.",
                            Tags = new [] { "select", "picker", "form" , "edit", "multiple", "dropdown", "filter", "search" },
                            Related = new [] { "dropdown", "dropdown-multiple", "dropdown-virtualization", "dropdown-grouping" },
                            Faq = new []
                            {
                                new FaqItem { Question = "How do I add a search box to the Blazor DropDown?", Answer = "Set AllowFiltering to true. Users can then type to filter the items, and you can control the filter operator (contains, starts with) and case sensitivity." },
                                new FaqItem { Question = "How do I make DropDown filtering case-insensitive?", Answer = "Set FilterCaseSensitivity to CaseInsensitive so the search matches items regardless of letter case." }
                            },
                        },
                        new Example
                        {
                            Name = "Grouping",
                            Path = "dropdown-grouping",
                            Title = "Blazor DropDown - Grouping | Free UI Components by Radzen",
                            Description = "Group Blazor DropDown items into categories with group headers bound from a property in your data.",
                            Tags = new [] { "select", "picker", "form" , "edit", "multiple", "dropdown", "grouping" },
                            Related = new [] { "dropdown", "dropdown-filtering", "dropdown-multiple", "dropdown-tree" },
                        },
                        new Example
                        {
                            Toc = [ new () { Text = "DropDown data binding to enum", Anchor = "#data-binding-to-enum" } ],
                            Name = "Custom objects binding",
                            Path = "dropdown-custom-objects",
                            Title = "Blazor DropDown - Bind to Objects & Enums | Free UI Components by Radzen",
                            Description = "Bind the Blazor DropDown to custom objects or enums, using TextProperty and ValueProperty to control the display text and the bound value.",
                            Tags = new [] { "select", "picker", "form" , "edit", "dropdown", "custom", "enum" },
                            Related = new [] { "dropdown", "dropdown-multiple", "dropdown-grouping", "dropdown-tree" },
                            Faq = new []
                            {
                                new FaqItem { Question = "How do I bind a Blazor DropDown to a list of objects?", Answer = "Set Data to your object collection and set TextProperty to the property shown in the list and ValueProperty to the property bound to Value." },
                                new FaqItem { Question = "How do I bind a Blazor DropDown to an enum?", Answer = "Bind Data to the enum values (for example Enum.GetValues) and bind Value to the enum field; the DropDown displays each name and stores the selected enum value." }
                            },
                        },
                        new Example
                        {
                            Toc = [ new () { Text = "Single selection", Anchor = "#single-selection" }, new () { Text = "Multiple selection with checkboxes", Anchor = "#multiple-selection" }, new () { Text = "Filtering", Anchor = "#filtering" } ],
                            Name = "DropDown with Tree",
                            Path = "dropdown-tree",
                            Title = "Blazor DropDown Tree - Hierarchical Select | Free UI Components by Radzen",
                            Description = "Combine a popup and a tree to build a Blazor DropDownTree for hierarchical single or multiple selection with filtering.",
                            Tags = new [] { "select", "picker", "form", "edit", "dropdown", "tree", "hierarchical" },
                            Related = new [] { "dropdown", "dropdown-multiple", "dropdown-filtering", "tree" },
                        },
                    }
                },
                new Example
                {
                    Toc = [ new () { Text = "Get and Set the value of DropDownDataGrid", Anchor = "#get-set-value" }, new () { Text = "Get and Set the value of DropDownDataGrid using Value and Change event", Anchor = "#value-and-change-event" }, new () { Text = "Define Text and Value properties", Anchor = "#text-value-properties" }, new () { Text = "DropDownDataGrid with custom header, footer, value and item templates", Anchor = "#template" }, new () { Text = "Define multiple columns", Anchor = "#multiple-columns" }, new () { Text = "Filtering case sensitivity and filter operator", Anchor = "#filtering-case-sensitivity-and-filter-operator" }, new () { Text = "Multiple selection", Anchor = "#multiple-selection" }, new () { Text = "DropDownDataGrid binding to dynamic data", Anchor = "#dynamic" }, new () { Text = "DropDownDataGrid Sizes", Anchor = "#sizes" }, new () { Text = "Keyboard Navigation", Anchor = "#keyboard-navigation" } ],
                    Name = "DropDownDataGrid",
                    Path = "dropdown-datagrid",
                    Title = "Blazor DropDownDataGrid - Grid in a DropDown | Free UI Components by Radzen",
                    Description = "Show tabular data inside a dropdown with the Blazor DropDownDataGrid - multiple columns, filtering, paging, and single or multiple selection.",
                    Icon = "\ue99c",
                    Tags = new [] { "select", "picker", "form", "edit", "dropdown", "grid", "multiselect" },
                    Related = new [] { "dropdown", "dropdown-multiple", "dropdown-filtering", "datagrid" },
                    Faq = new []
                    {
                        new FaqItem { Question = "What is the Blazor DropDownDataGrid?", Answer = "It is a dropdown that shows a DataGrid in its popup, so users pick a value from a multi-column, filterable, paged grid instead of a plain list." },
                        new FaqItem { Question = "Can the DropDownDataGrid select multiple rows?", Answer = "Yes. Set Multiple to true and bind Value to a collection to let users select several rows, with filtering and paging in the popup grid." }
                    }
                },
                new Example
                {
                    Toc = [ new () { Text = "Basic usage", Anchor = "#basic-usage" }, new () { Text = "FAB position", Anchor = "#position" }, new () { Text = "Multiple FABs", Anchor = "#multiple-fabs" }, new () { Text = "Busy FAB", Anchor = "#busy-fab" }, new () { Text = "Keyboard Navigation", Anchor = "#keyboard-navigation" } ],
                    Name = "Fab",
                    Path = "fab",
                    Title = "Blazor FAB - Floating Action Button | Free UI Components by Radzen",
                    Description = "The Blazor FAB (floating action button) highlights your app's primary action with a circular, elevated button.",
                    Icon = "\ue147",
                    Tags = new [] { "fab", "button", "floating", "action" },
                    Related = new [] { "fab-menu", "button" },
                    Faq = new []
                    {
                        new FaqItem { Question = "What is a FAB (floating action button) in Blazor?", Answer = "A FAB is a circular, elevated button that floats above the UI to highlight the primary action on a screen, such as add or compose." }
                    }
                },
                new Example
                {
                    Toc = [ new () { Text = "Basic usage", Anchor = "#basic-usage" }, new () { Text = "FAB menu with icon only buttons", Anchor = "#icons" }, new () { Text = "Expand direction", Anchor = "#direction" }, new () { Text = "Keyboard Navigation", Anchor = "#keyboard-navigation" } ],
                    Name = "FabMenu",
                    Path = "fab-menu",
                    Title = "Blazor FAB Menu - Floating Action Menu | Free UI Components by Radzen",
                    Description = "The Blazor FAB Menu expands a floating action button into a menu of quick actions.",
                    Icon = "\ue091",
                    Tags = new [] { "fab", "menu", "button", "floating", "action" },
                    Related = new [] { "fab", "button", "menu" },
                    Faq = new []
                    {
                        new FaqItem { Question = "How does the Blazor FAB Menu work?", Answer = "The FAB Menu shows a floating action button that expands into a set of smaller action buttons when pressed, giving quick access to related actions." }
                    }
                },
                new Example
                {
                    Toc = [ new () { Text = "Keyboard Navigation", Anchor = "#keyboard-navigation" } ],
                    Name = "Fieldset",
                    Title = "Blazor Fieldset Component | Free UI Components by Radzen",
                    Path = "fieldset",
                    Description = "The Blazor Fieldset groups related form fields under a titled, collapsible container.",
                    Related = new [] { "form-field", "templateform", "label" },
                    Faq = new []
                    {
                        new FaqItem { Question = "How do I group form fields in Blazor?", Answer = "Wrap related inputs in a RadzenFieldset with a title; it can be made collapsible to show or hide the group." }
                    },
                    Icon = "\ue728",
                    Tags = new [] { "form", "container" }
                },
                new Example
                {
                    Toc = [ new () { Text = "Byte Array Support", Anchor = "#byte-array" }, new () { Text = "Keyboard Navigation", Anchor = "#keyboard-navigation" } ],
                    Name = "FileInput",
                    Title = "Blazor FileInput - File Upload Input | Free UI Components by Radzen",
                    Path = "fileinput",
                    Description = "The Blazor FileInput uploads a file as base64 with preview support, bound directly to your model.",
                    Related = new [] { "upload", "signature-pad" },
                    Faq = new []
                    {
                        new FaqItem { Question = "How does the Blazor FileInput return the selected file?", Answer = "It reads the chosen file and binds it to your model as a base64 data string, with an optional preview." }
                    },
                    Icon = "\ue226",
                    Tags = new [] { "upload", "form", "edit" }
                },
                new Example
                {
                    Toc = [ new () { Text = "Variants", Anchor = "#variants" }, new () { Text = "Input types", Anchor = "#input-types" }, new () { Text = "Start, End, and ChildContent", Anchor = "#start-end-child-content" }, new () { Text = "Floating Label", Anchor = "#floating-label" }, new () { Text = "Helper text", Anchor = "#helper-text" }, new () { Text = "Validation", Anchor = "#form-field-validation" }, new () { Text = "Disabled FormField", Anchor = "#disabled-form-field" } ],
                    Name = "FormField",
                    Title = "Blazor FormField - Floating Label Input | Free UI Components by Radzen",
                    Path = "form-field",
                    Description = "The Blazor FormField wraps an input with a floating label, helper text, and validation styling.",
                    Related = new [] { "label", "fieldset", "templateform" },
                    Faq = new []
                    {
                        new FaqItem { Question = "What does the Blazor FormField do?", Answer = "It wraps an input with a floating label, helper text, and validation styling, similar to Material outlined fields." }
                    },
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
                            Description = "The Blazor HTML Editor is a rich text (WYSIWYG) editor with a full toolbar of formatting tools, image and link support, and HTML output.",
                            Related = new [] { "html-editor-custom-tools", "textarea" },
                            Faq = new []
                            {
                                new FaqItem { Question = "Is the Blazor HTML Editor a WYSIWYG editor?", Answer = "Yes. It is a rich text editor where users format content visually using a toolbar, and it produces HTML you can bind to your model." },
                                new FaqItem { Question = "How do I add custom buttons to the HTML Editor?", Answer = "Use RadzenHtmlEditorCustomTool to add your own toolbar buttons that run custom commands on the selected content." }
                            },
                            Tags = new [] { "html", "editor", "rich", "text", "wysiwyg" }
                        },
                        new Example
                        {
                            Toc = [ new () { Text = "Custom command on Execute event", Anchor = "#command-execute-event" }, new () { Text = "Custom tool with template", Anchor = "#command-template" }, new () { Text = "Custom dialog", Anchor = "#command-dialog" } ],
                            Name = "Custom Tools",
                            Path = "html-editor-custom-tools",
                            Title = "Blazor HTML Editor - Custom Tools | Free UI Components by Radzen",
                            Description = "Add your own buttons to the Blazor HTML Editor toolbar with RadzenHtmlEditorCustomTool.",
                            Related = new [] { "html-editor", "textarea" },
                            Faq = new []
                            {
                                new FaqItem { Question = "How do I create a custom tool in the Blazor HTML Editor?", Answer = "Declare a RadzenHtmlEditorCustomTool inside the editor and handle its Execute callback to run your command on the selected content." }
                            },
                            Tags = new [] { "html", "editor", "rich", "text", "tool", "custom" }
                        },
                    }
                },
                new Example
                {
                    Toc = [ new () { Text = "Get and Set the value of ListBox", Anchor = "#get-set-value" }, new () { Text = "Get and Set the value of ListBox using Value and Change event", Anchor = "#value-and-change-event" }, new () { Text = "Define Text and Value properties", Anchor = "#text-value-properties" }, new () { Text = "ListBox with template", Anchor = "#template" }, new () { Text = "Multiple selection", Anchor = "#multiple-selection" }, new () { Text = "Filtering case sensitivity and filter operator", Anchor = "#filtering" }, new () { Text = "Custom filtering with LoadData event", Anchor = "#loaddata-event" }, new () { Text = "Virtualization using IQueryable", Anchor = "#virtualization-using-iqueryable" }, new () { Text = "Virtualization with LoadData event", Anchor = "#virtualization-with-loaddata" }, new () { Text = "Keyboard Navigation", Anchor = "#keyboard-navigation" } ],
                    Name = "ListBox",
                    Title = "Blazor ListBox - Selectable List | Free UI Components by Radzen",
                    Path = "listbox",
                    Icon = "\ue0ee",
                    Description = "The Blazor ListBox shows a selectable list for single or multiple selection, with filtering and virtualization for large data.",
                    Related = new [] { "dropdown", "checkboxlist", "autocomplete" },
                    Faq = new []
                    {
                        new FaqItem { Question = "How do I enable multiple selection in the Blazor ListBox?", Answer = "Set Multiple to true and bind Value to a collection so users can select several items." },
                        new FaqItem { Question = "Can the Blazor ListBox filter and handle large lists?", Answer = "Yes. Enable AllowFiltering for a search box and turn on virtualization to render large lists efficiently." }
                    },
                    Tags = new [] { "select", "picker", "form", "edit" }
                },
                new Example
                {
                    Toc = [ new () { Text = "Mask Sizes", Anchor = "#sizes" } ],
                    Name = "Mask",
                    Path = "mask",
                    Title = "Blazor Masked TextBox - Input Mask | Free UI Components by Radzen",
                    Description = "The Blazor Masked TextBox formats input as the user types using a pattern - phone numbers, dates, IP addresses, and more.",
                    Icon = "\ue262",
                    Tags = new [] { "input", "form", "edit", "mask" },
                    Related = new [] { "textbox", "numeric", "datepicker" },
                    Faq = new []
                    {
                        new FaqItem { Question = "How do I mask a phone number or date in Blazor?", Answer = "Set the Mask property to a pattern (for example (***) ***-**** ) and the masked textbox enforces it as the user types." },
                        new FaqItem { Question = "Which characters can I use in a mask pattern?", Answer = "Use the placeholder characters (such as * for any character and 9 for digits) together with literal characters that appear as-is in the input." }
                    }
                },
                new Example
                {
                    Toc = [ new () { Text = "Get and Set the value of Numeric", Anchor = "#get-set-value" }, new () { Text = "Get and Set the value of Numeric using Value and Change event", Anchor = "#value-and-change-event" }, new () { Text = "Min set to 1 and Max set to 10", Anchor = "#min-max" }, new () { Text = "Placeholder and 0.5 step", Anchor = "#placeholder-and-step" }, new () { Text = "Without Up/Down", Anchor = "#without-up-down" }, new () { Text = "Formatted value", Anchor = "#formatted-value" }, new () { Text = "Align value", Anchor = "#align-value" }, new () { Text = "Custom Value convert", Anchor = "#custom-value-convert" }, new () { Text = "Custom Numeric Type Support", Anchor = "#custom-numeric-type" }, new () { Text = "Numeric Sizes", Anchor = "#sizes" }, new () { Text = "Keyboard Navigation", Anchor = "#keyboard-navigation" } ],
                    Name = "Numeric",
                    Path = "numeric",
                    Title = "Blazor Numeric TextBox - Number Input | Free UI Components by Radzen",
                    Description = "The Blazor Numeric TextBox edits numbers with min/max limits, step buttons, formatted display, and culture-aware parsing.",
                    Icon = "\uf04a",
                    Tags = new [] { "input", "number", "form", "edit", "numeric" },
                    Related = new [] { "slider", "mask", "textbox" },
                    Faq = new []
                    {
                        new FaqItem { Question = "How do I set minimum and maximum values on the Numeric TextBox?", Answer = "Set the Min and Max properties; the component clamps input to that range and the step buttons stay within it." },
                        new FaqItem { Question = "How do I format the number shown in the Numeric TextBox?", Answer = "Set the Format property to a standard or custom .NET numeric format string (for example C for currency or N2 for two decimals)." }
                    }
                },
                new Example
                {
                    Toc = [ new () { Text = "Get and Set the value of Password", Anchor = "#get-set-value" }, new () { Text = "Get and Set the value of Password using Value and Change event", Anchor = "#value-and-change-event" }, new () { Text = "Define placeholder", Anchor = "#placeholder" }, new () { Text = "Without auto-complete", Anchor = "#without-auto-complete" }, new () { Text = "Password Sizes", Anchor = "#sizes" } ],
                    Name = "Password",
                    Title = "Blazor Password TextBox | Free UI Components by Radzen",
                    Path = "password",
                    Description = "The Blazor Password TextBox masks input, with autocomplete control and placeholder support.",
                    Related = new [] { "textbox", "security-code" },
                    Faq = new []
                    {
                        new FaqItem { Question = "Does the Blazor Password TextBox hide what the user types?", Answer = "Yes. It renders as a password field that masks the characters, and you can control the autocomplete behavior and placeholder." }
                    },
                    Icon = "\uf042",
                    Tags = new [] { "input", "form", "edit" }
                },
                new Example
                {
                    Toc = [ new () { Text = "Get and Set the value of RadioButtonList", Anchor = "#get-set-value" }, new () { Text = "Get and Set the value of RadioButtonList using Value and Change event", Anchor = "#value-and-change-event" }, new () { Text = "Set RadioButtonList orientation and layout", Anchor = "#orientation" }, new () { Text = "Populate RadioButtonList items from data", Anchor = "#populate-items" }, new () { Text = "Statically declared and populated RadioButtonList items from data", Anchor = "#populate-items-statically" }, new () { Text = "RadioButtonList with null value", Anchor = "#null-value" }, new () { Text = "Populate items programmatically and disable item", Anchor = "#populate-items-programmatically" }, new () { Text = "Keyboard Navigation", Anchor = "#keyboard-navigation" } ],
                    Name = "RadioButtonList",
                    Title = "Blazor RadioButtonList - Radio Buttons | Free UI Components by Radzen",
                    Path = "radiobuttonlist",
                    Description = "The Blazor RadioButtonList shows a set of radio buttons bound to data, with horizontal or vertical orientation and null value support.",
                    Related = new [] { "checkboxlist", "selectbar", "dropdown" },
                    Faq = new []
                    {
                        new FaqItem { Question = "How do I bind the Blazor RadioButtonList to data?", Answer = "Set Data to your collection with TextProperty and ValueProperty (or declare RadzenRadioButtonListItem items), then bind the selected value with @bind-Value." }
                    },
                    Icon = "\ue837",
                    Tags = new [] { "toggle", "form", "edit" }
                },
                new Example
                {
                    Toc = [ new () { Text = "Get and Set the value of Rating", Anchor = "#get-set-value" }, new () { Text = "Get and Set the value of Rating using Value and Change event", Anchor = "#value-and-change-event" }, new () { Text = "Set number of stars", Anchor = "#number-of-stars" }, new () { Text = "Disabled Rating", Anchor = "#disabled-rating" }, new () { Text = "Read-only Rating", Anchor = "#readonly-rating" }, new () { Text = "Keyboard Navigation", Anchor = "#keyboard-navigation" } ],
                    Name = "Rating",
                    Title = "Blazor Rating - Star Rating | Free UI Components by Radzen",
                    Path = "rating",
                    Description = "The Blazor Rating captures a star rating, with a configurable number of stars and disabled or read-only modes.",
                    Related = new [] { "slider", "numeric" },
                    Faq = new []
                    {
                        new FaqItem { Question = "How do I set the number of stars in the Blazor Rating?", Answer = "Set Stars to the maximum number of stars, bind Value to the selected rating, and use ReadOnly to display a fixed rating." }
                    },
                    Icon = "\ue839",
                    Tags = new [] { "star", "form", "edit" }
                },
                new Example
                {
                    Name = "SecurityCode",
                    Title = "Blazor SecurityCode - OTP / PIN Input | Free UI Components by Radzen",
                    Path = "security-code",
                    Description = "The Blazor SecurityCode is a multi-box input for one-time passwords (OTP), PINs, and verification codes.",
                    Related = new [] { "password", "textbox" },
                    Faq = new []
                    {
                        new FaqItem { Question = "What is the Blazor SecurityCode used for?", Answer = "It collects a multi-digit code such as a one-time password (OTP), PIN, or email/SMS verification code, with one box per digit." },
                        new FaqItem { Question = "How many digits can the SecurityCode have?", Answer = "Set Count to the number of digits; the component renders that many input cells and concatenates them into the bound value." }
                    },
                    Icon = "\uf045",
                    Tags = new [] { "security", "code", "input" }
                },
                new Example
                {
                    New = true,
                    Name = "SignaturePad",
                    Title = "Blazor Signature Pad Component | Free UI Components by Radzen",
                    Path = "signature-pad",
                    Description = "The Blazor Signature Pad captures a handwritten signature by mouse or touch and exports it as an image.",
                    Related = new [] { "fileinput", "upload" },
                    Faq = new []
                    {
                        new FaqItem { Question = "How do I capture a signature in Blazor?", Answer = "Use the SignaturePad component; users draw with mouse or touch and the signature is exported as an image you can save." }
                    },
                    Icon = "\ue22b",
                    Tags = new [] { "signature", "sign", "draw", "pen", "touch", "form", "input" }
                },
                new Example
                {
                    Toc = [ new () { Text = "Chip Style", Anchor = "#chip-style" }, new () { Text = "Variant", Anchor = "#variant" }, new () { Text = "Sizes", Anchor = "#sizes" }, new () { Text = "Icons", Anchor = "#icons" }, new () { Text = "Selected", Anchor = "#selected" }, new () { Text = "Disabled", Anchor = "#disabled" }, new () { Text = "Events", Anchor = "#events" }, new () { Text = "Add / Remove", Anchor = "#add-remove" }, new () { Text = "Keyboard Navigation", Anchor = "#keyboard-navigation" } ],
                    Name = "Chip",
                    Title = "Blazor Chip Component | Free UI Components by Radzen",
                    Path = "chip",
                    Description = "The Blazor Chip is a compact element for tags, statuses, and filters, with optional remove and selection.",
                    Related = new [] { "chiplist", "label" },
                    Faq = new []
                    {
                        new FaqItem { Question = "What is the Blazor Chip used for?", Answer = "Chips are compact elements for tags, statuses, categories, or filters, and can be made removable or selectable." }
                    },
                    Icon = "\uf852",
                    Tags = new [] { "chip", "tag", "label", "status" }
                },
                new Example
                {
                    Toc = [ new () { Text = "Single selection", Anchor = "#single-selection" }, new () { Text = "Multiple selection", Anchor = "#multiple-selection" }, new () { Text = "Events", Anchor = "#events" }, new () { Text = "Templates", Anchor = "#templates" }, new () { Text = "Keyboard Navigation", Anchor = "#keyboard-navigation" } ],
                    Name = "ChipList",
                    Title = "Blazor ChipList Component | Free UI Components by Radzen",
                    Path = "chiplist",
                    Description = "The Blazor ChipList shows a set of selectable, removable chips bound to data.",
                    Related = new [] { "chip", "listbox", "checkboxlist" },
                    Faq = new []
                    {
                        new FaqItem { Question = "How do I make a selectable list of chips in Blazor?", Answer = "Use ChipList with data and bind the selection; each chip can be selected or removed." }
                    },
                    Icon = "\uf852",
                    Tags = new [] { "chip", "chiplist", "tag", "form", "edit", "selection" }
                },
                new Example
                {
                    Toc = [ new () { Text = "Get and Set the value of SelectBar", Anchor = "#get-set-value" }, new () { Text = "Get and Set the value of SelectBar using Value and Change event", Anchor = "#value-and-change-event" }, new () { Text = "Multiple selection", Anchor = "#multiple-selection" }, new () { Text = "Populate SelectBar items from data", Anchor = "#populate-from-data" }, new () { Text = "Statically declared and populated SelectBar items from data", Anchor = "#populate-items-statically" }, new () { Text = "Populate items programmatically and disable item", Anchor = "#populate-items-programmatically" }, new () { Text = "SelectBar with icons", Anchor = "#icons" }, new () { Text = "SelectBar with images", Anchor = "#images" }, new () { Text = "SelectBar with template", Anchor = "#template" }, new () { Text = "SelectBar Size", Anchor = "#size" }, new () { Text = "SelectBar Orientation", Anchor = "#orientation" }, new () { Text = "Keyboard Navigation", Anchor = "#keyboard-navigation" } ],
                    Name = "SelectBar",
                    Title = "Blazor SelectBar - Button Group Selector | Free UI Components by Radzen",
                    Path = "selectbar",
                    Description = "The Blazor SelectBar is a button-group selector for single or multiple choices, with icons and templates.",
                    Related = new [] { "radiobuttonlist", "toggle-button", "dropdown" },
                    Faq = new []
                    {
                        new FaqItem { Question = "Can the Blazor SelectBar select multiple values?", Answer = "Yes. Set Multiple to true and bind Value to a collection to allow more than one selected button." }
                    },
                    Icon = "\uf8e8",
                    Tags = new [] { "form", "edit" }
                },
                new Example
                {
                    Toc = [ new () { Text = "Get and Set the value of Slider", Anchor = "#get-set-value" }, new () { Text = "Get and Set the value of Slider using Value and Change event", Anchor = "#value-and-change-event" }, new () { Text = "Slider from -100 to 100", Anchor = "#min-max-value" }, new () { Text = "Slider with Step=10", Anchor = "#step" }, new () { Text = "Range Slider", Anchor = "#range-slider" }, new () { Text = "Disabled Slider", Anchor = "#disabled-slider" }, new () { Text = "Vertical Slider", Anchor = "#vertical-slider" }, new () { Text = "Keyboard Navigation", Anchor = "#keyboard-navigation" } ],
                    Name = "Slider",
                    Path = "slider",
                    Title = "Blazor Slider & Range Slider | Free UI Components by Radzen",
                    Description = "The Blazor Slider selects a single value or a range by dragging, with step increments and horizontal or vertical orientation.",
                    Icon = "\ue429",
                    Tags = new [] { "form", "slider", "range" },
                    Related = new [] { "numeric", "rating", "colorpicker" },
                    Faq = new []
                    {
                        new FaqItem { Question = "How do I create a range slider in Blazor?", Answer = "Set Range to true and bind Value to a collection of two numbers; the slider then shows two handles for the lower and upper bounds." },
                        new FaqItem { Question = "How do I make the Blazor Slider vertical?", Answer = "Set Orientation to Orientation.Vertical to render the slider top-to-bottom instead of left-to-right." }
                    }
                },
                new Example
                {
                    Toc = [ new () { Text = "Keyboard Navigation", Anchor = "#keyboard-navigation" } ],
                    Name = "SpeechToTextButton",
                    Title = "Blazor Speech to Text Button | Free UI Components by Radzen",
                    Description = "The Blazor Speech to Text Button captures voice input using the browser's speech recognition and writes the transcript to your field.",
                    Path = "speechtotextbutton",
                    Tags = new [] { "button", "speech", "voice", "dictation", "form" },
                    Related = new [] { "button", "textbox", "aichat" },
                    Faq = new []
                    {
                        new FaqItem { Question = "How does the Blazor Speech to Text Button work?", Answer = "It uses the browser's built-in speech recognition to capture the user's voice and writes the recognized text to the bound value." }
                    },
                    Icon = "\ue029"
                },
                new Example
                {
                    Toc = [ new () { Text = "Filled SplitButton", Anchor = "#filled" }, new () { Text = "Flat SplitButton", Anchor = "#flat" }, new () { Text = "Outlined SplitButton", Anchor = "#outlined" }, new () { Text = "Text SplitButton", Anchor = "#text" }, new () { Text = "Content in SplitButton", Anchor = "#content" }, new () { Text = "SplitButton Sizes", Anchor = "#sizes" }, new () { Text = "Disabled SplitButton", Anchor = "#disabled" }, new () { Text = "Busy SplitButton", Anchor = "#busy" }, new () { Text = "AlwaysOpenPopup SplitButton", Anchor = "#always-open-popup" }, new () { Text = "DropDown icon of SplitButton", Anchor = "#customize-dropdown-icon" }, new () { Text = "Keyboard Navigation", Anchor = "#keyboard-navigation" } ],
                    Name = "SplitButton",
                    Title = "Blazor SplitButton - Button with Dropdown Menu | Free UI Components by Radzen",
                    Description = "The Blazor SplitButton pairs a primary action with a dropdown menu of additional options.",
                    Path = "splitbutton",
                    Tags = new [] { "button", "menu", "dropdown", "split", "form" },
                    Related = new [] { "button", "fab-menu", "menu" },
                    Faq = new []
                    {
                        new FaqItem { Question = "How is a SplitButton different from a regular button?", Answer = "A SplitButton runs a primary action when its main area is clicked and opens a dropdown menu of secondary actions from its arrow, combining a button and a menu." }
                    },
                    Icon = "\uf756"
                },
                new Example
                {
                    Toc = [ new () { Text = "Get and set the value", Anchor = "#get-set-value" }, new () { Text = "Keyboard Navigation", Anchor = "#keyboard-navigation" }, new () { Text = "Disabled Switch", Anchor = "#disabled-switch" } ],
                    Name = "Switch",
                    Title = "Blazor Switch - Toggle Switch | Free UI Components by Radzen",
                    Path = "switch",
                    Description = "The Blazor Switch is a toggle switch that binds a bool value for on and off settings.",
                    Related = new [] { "checkbox", "toggle-button" },
                    Faq = new []
                    {
                        new FaqItem { Question = "How do I bind the Blazor Switch?", Answer = "Bind Value to a bool with @bind-Value; it is true when the switch is on and false when off." }
                    },
                    Icon = "\ue9f6",
                    Tags = new [] { "form", "edit", "switch" }
                },
                new Example
                {
                    Toc = [ new () { Text = "Basic Usage", Anchor = "#basic-usage" }, new () { Text = "Custom EditContext", Anchor = "#custom-edit-context" }, new () { Text = "Form Action", Anchor = "#form-action" } ],
                    Name = "TemplateForm",
                    Title = "Blazor Form - Template Form with Validation | Free UI Components by Radzen",
                    Path = "templateform",
                    Description = "The Blazor Form (TemplateForm) builds data-bound forms with built-in validation and submit handling.",
                    Related = new [] { "form-field", "label", "fieldset" },
                    Faq = new []
                    {
                        new FaqItem { Question = "How do I build a form with validation in Blazor?", Answer = "Use RadzenTemplateForm with data-bound inputs and add validators such as RadzenRequiredValidator to validate fields on submit." },
                        new FaqItem { Question = "Does the Blazor form support EditContext?", Answer = "Yes. RadzenTemplateForm integrates with EditContext and standard data annotations validation." }
                    },
                    Icon = "\uebed",
                    Tags = new [] { "form", "edit", "validation", "submit", "editcontext" }
                },
                new Example
                {
                    Name = "TextArea",
                    Title = "Blazor TextArea - Multiline Text Input | Free UI Components by Radzen",
                    Path = "textarea",
                    Description = "The Blazor TextArea is a multi-line text input with auto-resize, value binding, and placeholder support.",
                    Related = new [] { "textbox", "html-editor" },
                    Faq = new []
                    {
                        new FaqItem { Question = "How do I bind the Blazor TextArea?", Answer = "Bind Value to a string with @bind-Value to capture multi-line input from the user." }
                    },
                    Icon = "\ue167",
                    Tags = new [] { "input", "form", "edit" }
                },
                new Example
                {
                    Toc = [ new () { Text = "Get and Set the value of TextBox", Anchor = "#bind-value" }, new () { Text = "Placeholder", Anchor = "#placeholder" }, new () { Text = "Maximum length", Anchor = "#max-length" }, new () { Text = "Change on every input", Anchor = "#immediate" }, new () { Text = "Disabled TextBox", Anchor = "#disabled" }, new () { Text = "AutoComplete", Anchor = "#autocomplete" }, new () { Text = "TextBox Sizes", Anchor = "#sizes" } ],
                    Name = "TextBox",
                    Title = "Blazor TextBox - Text Input | Free UI Components by Radzen",
                    Path = "textbox",
                    Description = "The Blazor TextBox is a single-line text input with value binding, placeholder, max length, and read-only support.",
                    Related = new [] { "textarea", "mask", "password" },
                    Faq = new []
                    {
                        new FaqItem { Question = "How do I two-way bind a Blazor TextBox?", Answer = "Use @bind-Value to bind the input to a string property; the value updates as the user edits the field." }
                    },
                    Icon = "\ue9f1",
                    Tags = new [] { "input", "form", "edit" }
                },
                new Example
                {
                    Toc = [ new () { Text = "Bind the value of TimeSpanPicker", Anchor = "#bind-value" }, new () { Text = "Get and Set the value of TimeSpanPicker using Value and Change event.", Anchor = "#value-and-change-event" }, new () { Text = "Min and Max values", Anchor = "#min-max-values" }, new () { Text = "Inline picker", Anchor = "#inline" }, new () { Text = "Various configurations", Anchor = "#various-config" }, new () { Text = "Time span format", Anchor = "#format" }, new () { Text = "Custom input parsing", Anchor = "#custom-input-parsing" }, new () { Text = "TimeSpanPicker Sizes", Anchor = "#sizes" }, new () { Text = "Keyboard Navigation", Anchor = "#keyboard-navigation" } ],
                    Name = "TimeSpanPicker",
                    Path = "timespanpicker",
                    Title = "Blazor TimeSpanPicker - Duration Input | Free UI Components by Radzen",
                    Description = "Pick a duration or time span in the Blazor TimeSpanPicker, with inline mode, custom formatting, and min/max values.",
                    Icon = "\ue425",
                    Tags = new [] { "duration", "form", "edit", "timespan" },
                    Related = new [] { "datepicker", "numeric", "slider" },
                    Faq = new []
                    {
                        new FaqItem { Question = "How do I bind a TimeSpan value in Blazor?", Answer = "Bind Value to a TimeSpan (or nullable TimeSpan); the TimeSpanPicker reads and writes that value and raises Change when the user edits it." }
                    }
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
                    Title = "Blazor Upload - File Upload Component | Free UI Components by Radzen",
                    Description = "The Blazor Upload component uploads single or multiple files to a server endpoint, with progress, validation, and custom headers.",
                    Path = "upload",
                    Related = new [] { "fileinput", "signature-pad" },
                    Faq = new []
                    {
                        new FaqItem { Question = "How do I upload files in Blazor?", Answer = "Use the RadzenUpload component: point its Url at a server endpoint, allow single or multiple files, and handle progress and completion events." }
                    },
                    Icon = "\uf09b",
                    Tags = new [] { "upload", "file"}
                },
            },
        },
        new Example
        {
            Name = "Spreadsheet",
            Icon = "\ue3ec",
            New = true,
            Children = new []
            {
                new Example
                {
                    Name = "Overview",
                    Path = "spreadsheet",
                    Title = "Open-Source Blazor Spreadsheet Component | Free UI Components by Radzen",
                    Description = "Free open-source Blazor Spreadsheet component with Excel-like editing, formulas, cell formatting, filtering, sorting, data validation, conditional formatting, frozen panes, XLSX import/export, clipboard, autofill, undo/redo, multiple sheets, virtualization, custom cell types, and data binding.",
                    Tags = new [] { "spreadsheet", "excel", "xls", "xlsx", "csv", "ods" },
                    Related = new [] { "spreadsheet-formulas", "spreadsheet-cell-formatting", "spreadsheet-conditional-formatting", "spreadsheet-charts", "spreadsheet-data-validation" },
                    Faq = new []
                    {
                        new FaqItem { Question = "What can the Blazor Spreadsheet do?", Answer = "It offers Excel-like editing with formulas, cell formatting, filtering and sorting, data validation, conditional formatting, frozen panes, multiple sheets, and XLSX import/export." },
                        new FaqItem { Question = "Does the Blazor Spreadsheet import and export Excel files?", Answer = "Yes. It reads and writes XLSX files, so you can load existing workbooks and let users download edited ones, and it also supports CSV." },
                        new FaqItem { Question = "Does the Blazor Spreadsheet support formulas?", Answer = "Yes. It includes built-in functions such as SUM, AVERAGE, VLOOKUP, and IF, recalculated automatically as cell values change." },
                        new FaqItem { Question = "Can the Blazor Spreadsheet handle large data?", Answer = "Yes. It virtualizes rows so it stays responsive with tens of thousands of rows, even with live formula calculations." },
                        new FaqItem { Question = "Can I customize cells and the toolbar?", Answer = "Yes. You can define custom cell types (renderers and editors) and replace or extend the toolbar with your own tools." }
                    },
                    Toc = [ new () { Text = "Keyboard Navigation", Anchor = "#keyboard-navigation" } ]
                },
                new Example
                {
                    Name = "Formulas",
                    Path = "spreadsheet-formulas",
                    Title = "Blazor Spreadsheet Formulas | Free UI Components by Radzen",
                    Description = "Use built-in formula functions including SUM, AVERAGE, VLOOKUP, IF, and more.",
                    Tags = new [] { "spreadsheet", "formula", "function", "sum", "average", "vlookup", "if", "calculate" },
                    Related = new [] { "spreadsheet", "spreadsheet-data-validation", "spreadsheet-large-data" },
                    Faq = new []
                    {
                        new FaqItem { Question = "Which formula functions does the Blazor Spreadsheet support?", Answer = "Built-in functions include SUM, AVERAGE, VLOOKUP, IF, and many more, recalculated automatically when cell values change." }
                    }
                },
                new Example
                {
                    Name = "Cell Formatting",
                    Path = "spreadsheet-cell-formatting",
                    Title = "Blazor Spreadsheet Cell Formatting | Free UI Components by Radzen",
                    Description = "Apply fonts, colors, alignment, number formats, borders, and text styles to spreadsheet cells.",
                    Tags = new [] { "spreadsheet", "format", "font", "color", "alignment", "number", "style", "border" },
                    Related = new [] { "spreadsheet", "spreadsheet-conditional-formatting", "spreadsheet-merge-cells-borders" }
                },
                new Example
                {
                    Name = "Filtering & Sorting",
                    Path = "spreadsheet-filtering-sorting",
                    Title = "Blazor Spreadsheet Filtering & Sorting | Free UI Components by Radzen",
                    Description = "Filter and sort spreadsheet data using auto-filter and sort operations.",
                    Tags = new [] { "spreadsheet", "filter", "sort", "autofilter", "data" },
                    Related = new [] { "spreadsheet", "spreadsheet-tables", "spreadsheet-data-validation" }
                },
                new Example
                {
                    Name = "Tables",
                    Path = "spreadsheet-tables",
                    Title = "Blazor Spreadsheet Tables | Free UI Components by Radzen",
                    Description = "Wrap a range in an Excel-style table with style, banded rows, calculated columns, and a totals row.",
                    Tags = new [] { "spreadsheet", "table", "tables", "listobject", "totals", "subtotal", "calculated column", "banded rows", "table style" },
                    Related = new [] { "spreadsheet", "spreadsheet-filtering-sorting", "spreadsheet-formulas" }
                },
                new Example
                {
                    Name = "Data Validation",
                    Path = "spreadsheet-data-validation",
                    Title = "Blazor Spreadsheet Data Validation | Free UI Components by Radzen",
                    Description = "Add validation rules to cells including number ranges, lists, dates, and custom formulas.",
                    Tags = new [] { "spreadsheet", "validation", "rule", "list", "number", "date", "custom" },
                    Related = new [] { "spreadsheet", "spreadsheet-formulas", "spreadsheet-conditional-formatting" },
                    Faq = new []
                    {
                        new FaqItem { Question = "How do I restrict what users can enter in a cell?", Answer = "Add data validation rules - number ranges, dropdown lists, dates, or custom formulas - to control the accepted input." }
                    }
                },
                new Example
                {
                    Name = "Conditional Formatting",
                    Path = "spreadsheet-conditional-formatting",
                    Title = "Blazor Spreadsheet Conditional Formatting | Free UI Components by Radzen",
                    Description = "Apply conditional formatting rules to highlight cells based on their values.",
                    Tags = new [] { "spreadsheet", "conditional", "formatting", "highlight", "rule", "color" },
                    Related = new [] { "spreadsheet", "spreadsheet-cell-formatting", "spreadsheet-data-validation" },
                    Faq = new []
                    {
                        new FaqItem { Question = "How do I highlight cells based on their value?", Answer = "Add conditional formatting rules and matching cells are styled automatically when their values meet the condition." }
                    }
                },
                new Example
                {
                    Name = "Frozen Panes",
                    Path = "spreadsheet-frozen-panes",
                    Title = "Blazor Spreadsheet Frozen Panes | Free UI Components by Radzen",
                    Description = "Freeze rows and columns to keep headers visible while scrolling.",
                    Tags = new [] { "spreadsheet", "freeze", "frozen", "panes", "rows", "columns", "scroll" },
                    Related = new [] { "spreadsheet", "spreadsheet-large-data", "spreadsheet-multiple-sheets" },
                    Faq = new []
                    {
                        new FaqItem { Question = "How do I keep headers visible while scrolling a spreadsheet?", Answer = "Freeze the top rows and/or left columns so the headers stay in place as users scroll." }
                    }
                },
                new Example
                {
                    Name = "Images & Hyperlinks",
                    Path = "spreadsheet-images-hyperlinks",
                    Title = "Blazor Spreadsheet Images & Hyperlinks | Free UI Components by Radzen",
                    Description = "Insert and manage images and hyperlinks in spreadsheet cells.",
                    Tags = new [] { "spreadsheet", "image", "hyperlink", "link", "picture" },
                    Related = new [] { "spreadsheet", "spreadsheet-cell-formatting", "spreadsheet-charts" }
                },
                new Example
                {
                    Name = "Merge Cells",
                    Path = "spreadsheet-merge-cells-borders",
                    Title = "Blazor Spreadsheet Merge Cells | Free UI Components by Radzen",
                    Description = "Merge cells to create headers and build form layouts in the spreadsheet.",
                    Tags = new [] { "spreadsheet", "merge", "cells", "layout" },
                    Related = new [] { "spreadsheet", "spreadsheet-cell-formatting", "spreadsheet-templates" }
                },
                new Example
                {
                    Name = "Custom Cell Types",
                    Path = "spreadsheet-custom-cell-types",
                    Title = "Blazor Spreadsheet Custom Cell Types | Free UI Components by Radzen",
                    Description = "Create custom cell renderers and editors for the Radzen Blazor Spreadsheet.",
                    Tags = new [] { "spreadsheet", "custom", "cell", "type", "renderer", "editor" },
                    Related = new [] { "spreadsheet", "spreadsheet-custom-toolbar", "spreadsheet-cell-formatting" }
                },
                new Example
                {
                    Name = "Multiple Sheets",
                    Path = "spreadsheet-multiple-sheets",
                    Title = "Blazor Spreadsheet Multiple Sheets | Free UI Components by Radzen",
                    Description = "Work with multiple worksheets and use cross-sheet references to aggregate data across sheets.",
                    Tags = new [] { "spreadsheet", "sheets", "worksheets", "tabs", "cross-sheet", "reference" },
                    Related = new [] { "spreadsheet", "spreadsheet-formulas", "spreadsheet-frozen-panes" }
                },
                new Example
                {
                    Name = "Large Data",
                    Path = "spreadsheet-large-data",
                    Title = "Blazor Spreadsheet Large Data | Free UI Components by Radzen",
                    Description = "Virtualized spreadsheet with 10,000 rows and formula calculations for smooth scrolling performance.",
                    Tags = new [] { "spreadsheet", "performance", "virtualization", "large", "data", "virtual", "scrolling" },
                    Related = new [] { "spreadsheet", "spreadsheet-formulas", "spreadsheet-frozen-panes" },
                    Faq = new []
                    {
                        new FaqItem { Question = "How many rows can the Blazor Spreadsheet handle?", Answer = "Row virtualization keeps it smooth with tens of thousands of rows, even with formula calculations." }
                    }
                },
                new Example
                {
                    Name = "Templates",
                    Path = "spreadsheet-templates",
                    Title = "Blazor Spreadsheet Templates | Free UI Components by Radzen",
                    Description = "Real-world spreadsheet templates: annual budget tracker and weekly timesheet with formulas and conditional formatting.",
                    Tags = new [] { "spreadsheet", "template", "budget", "timesheet", "invoice", "financial", "planning" },
                    Related = new [] { "spreadsheet", "spreadsheet-formulas", "spreadsheet-conditional-formatting" }
                },
                new Example
                {
                    Name = "Protection",
                    Path = "spreadsheet-protection",
                    Title = "Blazor Spreadsheet Protection | Free UI Components by Radzen",
                    Description = "Protect sheets to prevent editing of locked cells while allowing input in unlocked cells with XLSX round-trip support.",
                    Tags = new [] { "spreadsheet", "protection", "locked", "unlock", "readonly", "security", "sheet" },
                    Related = new [] { "spreadsheet", "spreadsheet-permissions", "spreadsheet-data-validation" },
                    Faq = new []
                    {
                        new FaqItem { Question = "How do I make a spreadsheet read-only or lock specific cells?", Answer = "Protect the sheet to lock chosen cells while allowing input in others, with XLSX round-trip support." }
                    }
                },
                new Example
                {
                    Name = "Charts",
                    Path = "spreadsheet-charts",
                    Title = "Blazor Spreadsheet Charts - Embed Column, Bar, Line, Pie Charts | Free UI Components by Radzen",
                    Description = "Embed interactive charts in Blazor spreadsheet cells. Supports column, bar, line, area, pie, donut, and scatter charts with live data binding and XLSX import/export.",
                    Tags = new [] { "spreadsheet", "chart", "charts", "graph", "visualization", "column chart", "bar chart", "line chart", "pie chart", "donut chart", "scatter chart", "area chart", "excel chart", "embedded chart", "data visualization", "xlsx", "dashboard" },
                    Related = new [] { "spreadsheet", "spreadsheet-formulas", "spreadsheet-cell-formatting" },
                    Faq = new []
                    {
                        new FaqItem { Question = "Can I embed charts in the Blazor Spreadsheet?", Answer = "Yes. Embed column, bar, line, area, pie, donut, and scatter charts in cells, bound to live spreadsheet data." }
                    }
                },
                new Example
                {
                    Name = "Custom Toolbar",
                    Path = "spreadsheet-custom-toolbar",
                    Title = "Blazor Spreadsheet Custom Toolbar | Free UI Components by Radzen",
                    Description = "Replace the built-in toolbar with your own selection of tools. Reuse the predefined tool components in any order or layout, and add custom tools that dispatch undoable commands.",
                    Tags = new [] { "spreadsheet", "toolbar", "custom", "custom tools", "childcontent", "command", "icommand", "undo", "extend" },
                    Related = new [] { "spreadsheet", "spreadsheet-custom-cell-types", "spreadsheet-permissions" },
                    Toc = [ new () { Text = "Predefined tools in a custom layout", Anchor = "#predefined-tools" }, new () { Text = "Custom tool with an undoable command", Anchor = "#custom-tool" } ]
                },
                new Example
                {
                    Name = "Permissions",
                    Path = "spreadsheet-permissions",
                    Title = "Blazor Spreadsheet Permissions | Free UI Components by Radzen",
                    Description = "Lock the spreadsheet for view-only embedding with ReadOnly, disable individual features with Allow* flags, or veto commands dynamically with a CommandExecuting handler.",
                    Tags = new [] { "spreadsheet", "permissions", "readonly", "read-only", "view-only", "allow", "allowediting", "allowfiltering", "allowsorting", "commandexecuting", "preventdefault", "audit", "role", "restrict" },
                    Related = new [] { "spreadsheet", "spreadsheet-protection", "spreadsheet-custom-toolbar" },
                    Toc = [ new () { Text = "Read-only mode", Anchor = "#read-only" }, new () { Text = "Configuration toggles", Anchor = "#toggles" }, new () { Text = "Dynamic veto with CommandExecuting", Anchor = "#dynamic-veto" } ]
                },
            }
        },
        new Example
        {
            Name = "PivotDataGrid",
            Icon = "\ue9ce",
            Tags = new [] { "pivot", "crosstab", "analysis", "aggregation", "drill-down", "datagrid", "table" },
            Children = new[]
            {
                new Example
                {
                    Name = "IQueryable",
                    Path = "/pivot-data-grid",
                    Title = "Blazor Pivot Table - Pivot DataGrid (IQueryable) | Free UI Components by Radzen",
                    Description = "The Blazor Pivot DataGrid (RadzenPivotDataGrid) creates cross-tabulation reports - rows, columns, and aggregated values - from an IQueryable data source.",
                    Tags = new [] { "pivot", "pivot table", "crosstab", "analysis", "aggregation", "drill-down", "datagrid", "table", "query", "IQueryable" },
                    Related = new [] { "pivot-data-grid-load-data", "pivot-data-grid-dynamic", "pivot-data-grid-odata", "datagrid" },
                    Faq = new []
                    {
                        new FaqItem { Question = "What is the Blazor Pivot DataGrid?", Answer = "It is a pivot table (cross-tab) component that groups data into rows and columns and shows aggregated values, with drill-down." },
                        new FaqItem { Question = "How do I bind the Pivot DataGrid to data?", Answer = "Set Data to an IQueryable (or use the LoadData event for remote data) and define the row, column, and value fields to aggregate." }
                    }
                },
                new Example
                {
                    Name = "LoadData",
                    Path = "/pivot-data-grid-load-data",
                    Title = "Blazor Pivot DataGrid - LoadData Binding | Free UI Components by Radzen",
                    Description = "Bind the Blazor Pivot DataGrid to remote data with the LoadData event, fetching aggregated cross-tab results on demand.",
                    Tags = new [] { "pivot", "crosstab", "analysis", "aggregation", "drill-down", "datagrid", "table", "loaddata", "remote" },
                    Related = new [] { "pivot-data-grid", "pivot-data-grid-odata", "datagrid-loaddata" },
                    Faq = new []
                    {
                        new FaqItem { Question = "How do I load Pivot DataGrid data on demand?", Answer = "Handle the LoadData event to fetch and aggregate data from the server as the grid needs it." }
                    }
                },
                new Example
                {
                    Name = "Dynamic data",
                    Path = "/pivot-data-grid-dynamic",
                    Title = "Blazor Pivot DataGrid - Dynamic Data | Free UI Components by Radzen",
                    Description = "Bind the Blazor Pivot DataGrid to schema-less IDictionary<string, object> records and configure pivot fields dynamically.",
                    Tags = new [] { "pivot", "dynamic", "dictionary", "analysis", "aggregation", "drill-down", "datagrid", "table" },
                    Related = new [] { "pivot-data-grid", "pivot-data-grid-load-data", "datagrid-dynamic" },
                    Faq = new []
                    {
                        new FaqItem { Question = "How do I bind the Pivot DataGrid to dynamic data?", Answer = "Bind Data to records of IDictionary<string, object> and configure the row, column, and value fields at runtime, without a fixed model." }
                    }
                },
                new Example
                {
                    Name = "OData",
                    Path = "/pivot-data-grid-odata",
                    Title = "Blazor Pivot DataGrid - OData Binding | Free UI Components by Radzen",
                    Description = "Bind the Blazor Pivot DataGrid to an OData service and build cross-tabulation reports from the remote query.",
                    Tags = new [] { "odata", "pivot", "crosstab", "analysis", "aggregation", "drill-down", "datagrid", "table", "query", "remote" },
                    Related = new [] { "pivot-data-grid", "pivot-data-grid-load-data", "datagrid-odata" },
                    Faq = new []
                    {
                        new FaqItem { Question = "How do I bind the Pivot DataGrid to OData?", Answer = "Point the grid at an OData endpoint via the LoadData event; it builds the cross-tabulation from the remote query results." }
                    }
                }
            }
        },
        new Example
        {
            Name = "Document Processing",
            Icon = "",
            New = true,
            Title = "Blazor Document Processing | Free UI Components by Radzen",
            Description = "Read and write Excel (XLSX) and CSV files in Blazor and C#. Generate downloads, parse uploads, and evaluate Excel formulas in code.",
            Children = new []
            {
                new Example
                {
                    Name = "Spreadsheet API",
                    Path = "document-processing-spreadsheet",
                    Title = "Generate Excel (XLSX) and CSV Files in Blazor | Radzen",
                    Description = "Create Excel (XLSX) and CSV files from a list of objects and let users download them in Blazor.",
                    Tags = new [] { "document", "processing", "spreadsheet", "api", "xlsx", "csv", "excel", "generate", "create", "download", "list", "objects", "workbook" }
                },
                new Example
                {
                    Toc = [ new () { Text = "XLSX", Anchor = "#xlsx" }, new () { Text = "CSV", Anchor = "#csv" } ],
                    Name = "Import & Export",
                    Path = "document-processing-import-export",
                    Title = "Import and Export Excel (XLSX) and CSV in Blazor | Radzen",
                    Description = "Import and export Excel (XLSX) and CSV files in Blazor. Upload a file, parse the data, and display the rows, or generate a file users can download.",
                    Tags = new [] { "document", "processing", "import", "export", "xlsx", "csv", "excel", "upload", "download", "read", "write", "parse", "separator", "encoding", "quoting" }
                },
                new Example
                {
                    Toc = [ new () { Text = "Formulas in code", Anchor = "#in-code" }, new () { Text = "Stateless evaluation", Anchor = "#stateless-engine" }, new () { Text = "Stateful evaluation", Anchor = "#stateful-engine" }, new () { Text = "Custom functions", Anchor = "#custom-functions" } ],
                    Name = "Formulas",
                    Path = "document-processing-formulas",
                    Title = "Excel Formula Evaluator for Blazor and C# | Radzen",
                    Description = "Evaluate Excel formulas in Blazor and C#. Use them as cell formulas in a workbook, calculate them in code, or add your own custom Excel functions.",
                    Tags = new [] { "document", "processing", "formula", "formulas", "excel", "evaluate", "calculate", "engine", "evaluator", "custom", "function", "compound", "vlookup", "sum", "average", "if", "iferror", "edate", "sumif" }
                },
            }
        },
        new Example
        {
            Toc = [ new () { Text = "Live demo", Anchor = "#live-demo" }, new () { Text = "How it works", Anchor = "#how-it-works" }, new () { Text = "ILocalizer", Anchor = "#ilocalizer" }, new () { Text = "Satellite assemblies", Anchor = "#satellite-assemblies" }, new () { Text = "Parameter override", Anchor = "#parameter-override" }, new () { Text = "Resource keys", Anchor = "#resource-keys" }, new () { Text = "Culture resolution", Anchor = "#culture" }, new () { Text = "Priority order", Anchor = "#priority" } ],
            Name = "Localization",
            New = true,
            Path = "/localization",
            Title = "Blazor Localization | Free UI Components by Radzen",
            Description = "How to localize Radzen Blazor Components using resource files, satellite assemblies, or the ILocalizer interface.",
            Icon = "\ue8e2",
            Tags = new[] { "localization", "globalization", "culture", "translation", "language", "i18n", "l10n", "resource", "resx", "satellite" }
        },
        new Example
        {
            Toc = [ new () { Text = "Get and set the text", Anchor = "#text" }, new () { Text = "Markdown with Blazor components inside", Anchor = "#blazor" } ],
            Name = "Markdown",
            Icon = "\uf552",
            Path = "markdown",
            Title = "Blazor Markdown - Render Markdown Content | Free UI Components by Radzen",
            Description = "Render Markdown content as HTML in Blazor with RadzenMarkdown - auto-linked headings and support for embedded Blazor components.",
            Tags = new[] { "markdown", "text", "content", "render" },
            Related = new [] { "html-editor", "textarea" },
            Faq = new []
            {
                new FaqItem { Question = "How do I render Markdown in Blazor?", Answer = "Add RadzenMarkdown and set its Text property (or place markdown as its child content); it renders the Markdown as HTML." },
                new FaqItem { Question = "Can I embed Blazor components inside Markdown?", Answer = "Yes. RadzenMarkdown renders Blazor components placed inside the markdown content, alongside standard Markdown syntax." }
            }
        },
        new Example
        {
            Name = "Data",
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
                            Title = "Blazor DataList - OData Service | Free UI Components by Radzen",
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
                            Title = "Blazor DataFilter - LoadData event | Free UI Components by Radzen",
                            Description = "This example demonstrates DataFilter with DataGrid LoadData event.",
                            Path = "datafilter-loaddata",
                            Tags = new [] { "dataview", "grid", "table", "filter", "loaddata" },
                        },
                        new Example
                        {
                            Name = "OData service",
                            Title = "Blazor DataFilter - OData Service | Free UI Components by Radzen",
                            Description = "This example demonstrates data filter with OData service.",
                            Path = "datafilter-odata",
                            Tags = new [] { "dataview", "grid", "table", "filter", "odata" },
                        }
                    }
                },

                new Example
                {

                    Toc = [ new () { Text = "Allow Reload", Anchor = "#allow-reload" }, new () { Text = "Pager Density", Anchor = "#pager-density" }, new () { Text = "Keyboard Navigation", Anchor = "#keyboard-navigation" } ],
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
                    Name = "Empty PickList",
                    Path = "/picklist-empty",
                    Title = "Blazor PickList - Empty PickList | Free UI Components by Radzen",
                    Description = "This example demonstrates Blazor PickList with empty text and empty template.",
                    Icon = "\ue0b8",
                    Tags = new[] { "picklist", "empty", "list", "listbox" }
                },
                new Example
                {
                    Toc = [ new () { Text = "Day, week and month views", Anchor="#views"}, new () { Text = "Year Planner and Timeline views", Anchor = "#timeline" }, new () { Text = "Display additional content when the user hovers an appointment", Anchor = "#tooltips" }, new () { Text = "Display any number of days side-by-side", Anchor = "#multiday" }, new () { Text = "Keyboard Navigation", Anchor = "#keyboard-navigation" } ],
                    Name = "Scheduler",
                    Title = "Blazor Scheduler & Calendar Component | Free UI Components by Radzen",
                    Path = "scheduler",
                    Description = "The Blazor Scheduler is a calendar that shows appointments in day, week, month, year planner, and timeline views, with event editing, tooltips, and multi-day layouts.",
                    Related = new [] { "datepicker", "timespanpicker" },
                    Faq = new []
                    {
                        new FaqItem { Question = "Can I use the Blazor Scheduler as a calendar?", Answer = "Yes. The Scheduler is a calendar with day, week, month, year planner, and timeline views; bind your appointments to its Data and it renders them on the calendar." },
                        new FaqItem { Question = "What views does the Blazor Scheduler support?", Answer = "Day, week, month, year planner, and timeline views, and you can show any number of days side by side." },
                        new FaqItem { Question = "How do I add and edit appointments in the Scheduler?", Answer = "Handle the slot and appointment events (such as SlotSelect and AppointmentSelect) to open a dialog where users create or edit events bound to your data." }
                    },
                    Icon = "\ue616",
                    Tags = new[] { "scheduler", "calendar", "event", "appointment" }
                },
                new Example
                {
                    Name = "Gantt",
                    Icon = "\ueb85",
                    Tags = new[] { "gantt", "timeline", "project", "task", "schedule" },
                    Children = new[]
                    {
                        new Example
                        {
                            Name = "Overview",
                            Path = "gantt",
                            Title = "Blazor Gantt Component | Free UI Components by Radzen",
                            Description = "Blazor Gantt component with a hierarchical task list and a timeline view.",
                            Tags = new[] { "gantt", "timeline", "project", "task", "schedule" }
                        },
                        new Example
                        {
                            Name = "Tooltips",
                            Path = "gantt-tooltips",
                            Title = "Blazor Gantt Tooltips | Free UI Components by Radzen",
                            Description = "Show tooltips when hovering over Gantt task bars using TaskMouseEnter and TaskMouseLeave events.",
                            Tags = new[] { "gantt", "tooltip", "hover", "task", "mouse" }
                        },
                        new Example
                        {
                            Name = "Filtering",
                            Path = "gantt-filtering",
                            Title = "Blazor Gantt Filtering | Free UI Components by Radzen",
                            Description = "Filter Gantt tasks using Simple, SimpleWithMenu, or Advanced filter modes.",
                            Tags = new[] { "gantt", "filter", "filtermode", "simple", "advanced" }
                        },
                        new Example
                        {
                            Name = "InLine Editing",
                            Path = "gantt-inline-edit",
                            Title = "Blazor Gantt Inline Editing | Free UI Components by Radzen",
                            Description = "Edit Gantt tasks inline with add, edit, and save actions.",
                            Tags = new[] { "gantt", "edit", "inline", "row", "tasks" }
                        },
                        new Example
                        {
                            Name = "In-Cell Editing",
                            Path = "gantt-incell-edit",
                            Title = "Blazor Gantt In-Cell Editing | Free UI Components by Radzen",
                            Description = "Edit Gantt task fields in-cell with inline editors per column.",
                            Tags = new[] { "gantt", "edit", "incell", "cell", "tasks" }
                        },
                        new Example
                        {
                            Name = "Drag & Resize",
                            Path = "gantt-drag-resize",
                            Title = "Blazor Gantt Drag & Resize | Free UI Components by Radzen",
                            Description = "Drag task bars to move them and drag their edges to resize. Zero-duration tasks render as milestone diamonds.",
                            Tags = new[] { "gantt", "drag", "resize", "move", "milestone", "diamond", "interactive" }
                        },
                        new Example
                        {
                            Name = "Dependency Types",
                            Path = "gantt-dependency-types",
                            Title = "Blazor Gantt Dependency Types | Free UI Components by Radzen",
                            Description = "All four dependency types: Finish-to-Start, Start-to-Start, Finish-to-Finish, and Start-to-Finish.",
                            Tags = new[] { "gantt", "dependency", "link", "finish-to-start", "start-to-start", "finish-to-finish", "start-to-finish" }
                        },
                        new Example
                        {
                            Name = "Dependency Data",
                            Path = "gantt-dependency-data",
                            Title = "Blazor Gantt Dependency Data Binding | Free UI Components by Radzen",
                            Description = "Bind dependencies using a separate POCO collection with ID-based references — ideal for relational databases.",
                            Tags = new[] { "gantt", "dependency", "data", "binding", "database", "id", "predecessor", "successor" }
                        },
                        new Example
                        {
                            Name = "Critical Path",
                            Path = "gantt-critical-path",
                            Title = "Blazor Gantt Critical Path | Free UI Components by Radzen",
                            Description = "Highlight the longest chain of dependent tasks that determines the project end date.",
                            Tags = new[] { "gantt", "critical", "path", "highlight", "schedule", "dependency" }
                        },
                        new Example
                        {
                            Name = "Baselines",
                            Path = "gantt-baselines",
                            Title = "Blazor Gantt Baselines | Free UI Components by Radzen",
                            Description = "Show planned vs. actual schedule side by side using baseline bars.",
                            Tags = new[] { "gantt", "baseline", "planned", "actual", "schedule", "comparison" }
                        },
                        new Example
                        {
                            Name = "Customization",
                            Path = "gantt-customization",
                            Title = "Blazor Gantt Customization | Free UI Components by Radzen",
                            Description = "Customize the Gantt with a today line, weekend shading, vertical markers, per-bar styling via TaskRender, and custom bar templates.",
                            Tags = new[] { "gantt", "today", "marker", "weekend", "taskrender", "template", "customize" }
                        }
                    }
                },
                new Example
                {
                    Toc = [ new () { Text = "Dynamic Table", Anchor = "#dynamic" }, new () { Text = "Scrollable Table", Anchor = "#scrollable" }, new () { Text = "Table with merged cells", Anchor = "#scrollable" } ],
                    Name = "Table",
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
                            Title = "Blazor Tree - Data-binding | Free UI Components by Radzen",
                            Description = "This example demonstrates how to populate RadzenTree from a database via Entity Framework.",
                            Path = "tree-data-binding",
                            Tags = new [] { "tree", "treeview", "nodes", "data", "table" },
                        },
                        new Example
                        {
                            Name = "Files and directories",
                            Title = "Blazor Tree - File & Directory Binding | Free UI Components by Radzen",
                            Description = "This example demonstrates how to populate Blazor RadzenTree from the file system.",
                            Path = "tree-file-system",
                            Tags = new [] { "tree", "treeview", "nodes", "file", "directory" },
                        },
                        new Example
                        {
                            Name = "Selection",
                            Title = "Blazor Tree - Selection | Free UI Components by Radzen",
                            Description = "This example demonstrates how to get or set the selected items of RadzenTree.",
                            Path = "tree-selection",
                            Tags = new [] { "tree", "treeview", "nodes", "selection" },
                        },
                        new Example
                        {
                            Name = "Checkboxes",
                            Title = "Blazor Tree - Tri-State Checkboxes | Free UI Components by Radzen",
                            Description = "This example demonstrates tri-state checkboxes in RadzenTree.",
                            Path = "tree-checkboxes",
                            Tags = new [] { "tree", "treeview", "nodes", "check" },
                        },
                        new Example
                        {
                            Name = "Drag & Drop",
                            Title = "Blazor Tree - Drag & Drop items | Free UI Components by Radzen",
                            Description = "This example demonstrates custom drag & drop logic in RadzenTree.",
                            Path = "tree-dragdrop",
                            Tags = new [] { "tree", "treeview", "nodes", "drag", "drop" },
                        },
                        new Example
                        {
                            Name = "Context menu",
                            Title = "Blazor Tree - Context menu | Free UI Components by Radzen",
                            Description = "This example demonstrates context menu in RadzenTree.",
                            Path = "tree-contextmenu",
                            Tags = new [] { "tree", "treeview", "nodes", "context", "menu" },
                        },
                        new Example
                        {
                            Name = "Refreshing tree data-binding",
                            Title = "Blazor Tree - Refresh Data Binding | Free UI Components by Radzen",
                            Description = "This example demonstrates how to refresh a lazily loaded RadzenTree.",
                            Path = "tree-data-binding-refresh",
                            Tags = new [] { "tree", "treeview", "nodes" },
                        },
                        new Example
                        {
                            Name = "Tree filtering",
                            Title = "Blazor Tree - Filtering | Free UI Components by Radzen",
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
            Name = "Navigation",
            Icon = "\ue762",
            Children = new[] {
                new Example
                {
                    Toc = [ new () { Text = "Accordion with single expand", Anchor = "#single-expand" }, new () { Text = "Accordion with multiple expand", Anchor = "#multiple-expand" }, new () { Text = "Dynamically create Accordion items", Anchor = "#dynamic-items" }, new () { Text = "Expand/Collapse events", Anchor = "#expand-collapse-events" }, new () { Text = "Client-side rendering", Anchor = "#client-render-mode" }, new () { Text = "Disable expand/collapse", Anchor = "#disable-expand-collapse" }, new () { Text = "Keyboard Navigation", Anchor = "#keyboard-navigation" } ],
                    Name = "Accordion",
                    Title = "Blazor Accordion Component | Free UI Components by Radzen",
                    Path = "accordion",
                    Description = "The Blazor Accordion shows collapsible panels with single or multiple expand modes, dynamic items, and expand/collapse events.",
                    Related = new [] { "tabs", "panelmenu" },
                    Faq = new []
                    {
                        new FaqItem { Question = "How do I allow multiple panels open in the Blazor Accordion?", Answer = "Set Multiple to true so more than one panel can be expanded at once; otherwise opening one collapses the others." }
                    },
                    Icon = "\ue8fe",
                    Tags = new [] { "panel", "container" }
                },
                new Example
                {
                    Toc = [ new () { Text = "Default Radzen BreadCrumb", Anchor = "#default-breadcrumb" }, new () { Text = "BreadCrumb width template", Anchor = "#breadcrumb-template" }, new () { Text = "BreadCrumb with child content", Anchor = "#breadcrumb-child-template" } ],
                    Name = "BreadCrumb",
                    Title = "Blazor BreadCrumb Component | Free UI Components by Radzen",
                    Description = "The Blazor BreadCrumb shows a navigation trail so users can see and jump back to their location in the app.",
                    Path = "breadcrumb",
                    Related = new [] { "menu", "steps" },
                    Faq = new []
                    {
                        new FaqItem { Question = "How do I add breadcrumbs in Blazor?", Answer = "Add RadzenBreadCrumb with RadzenBreadCrumbItem children, each with Text and Path, to show the navigation trail." }
                    },
                    Icon = "\uea50",
                    Tags = new [] { "breadcrumb", "navigation", "menu" }
                },
                new Example
                {
                    Toc = [ new () { Text = "Navigation button styles", Anchor = "#navigation-style" }, new () { Text = "Navigation button content", Anchor = "#navigation-content" }, new () { Text = "Paging", Anchor = "#paging" }, new () { Text = "Data-binding", Anchor = "#data-binding" }, new () { Text = "Carousel with RadzenPager", Anchor = "#pager" } ],
                    Name = "Carousel",
                    Title = "Blazor Carousel Component | Free UI Components by Radzen",
                    Description = "The Blazor Carousel cycles through content - images or any markup - with navigation arrows and paging.",
                    Path = "carousel",
                    Related = new [] { "tabs", "steps" },
                    Faq = new []
                    {
                        new FaqItem { Question = "How do I build an image carousel in Blazor?", Answer = "Add RadzenCarousel with item content; it cycles through items with navigation arrows and optional auto-cycle and paging." }
                    },
                    Icon = "\ue8eb",
                    Tags = new [] { "carousel", "gallery", "slide", "deck", "container" }
                },
                new Example
                {
                    Toc = [ new () { Text = "Show ContextMenu with items", Anchor = "#contextmenu-with-items" }, new () { Text = "Show ContextMenu with custom content and separator", Anchor = "#contextmenu-with-custom-content" }, new () { Text = "Show ContextMenu for HTML element", Anchor = "#contextmenu-for-html-element" } ],
                    Name = "ContextMenu",
                    Title = "Blazor ContextMenu - Right-Click Menu | Free UI Components by Radzen",
                    Description = "The Blazor ContextMenu opens a right-click menu of actions anywhere in your app via ContextMenuService.",
                    Path = "contextmenu",
                    Related = new [] { "menu", "profile-menu" },
                    Faq = new []
                    {
                        new FaqItem { Question = "How do I add a right-click menu in Blazor?", Answer = "Handle an element's @oncontextmenu and call ContextMenuService.Open with your menu items to show a context menu." }
                    },
                    Icon = "\ue8de",
                    Tags = new [] { "popup", "dropdown", "menu" }
                },
                new Example
                {
                    Toc = [ new () { Text = "Link to path in application", Anchor = "#link-to-path" }, new () { Text = "Link to path in application with icon", Anchor = "#link-with-icon" }, new () { Text = "Link to url", Anchor = "#link-to-url" }, new () { Text = "Link with child content", Anchor = "#link-child-content" }, new () { Text = "Link disabled", Anchor = "#link-disabled" } ],
                    Name = "Link",
                    Title = "Blazor Link Component | Free UI Components by Radzen",
                    Description = "The Blazor Link renders a navigation link with Path and Target, integrated with Blazor routing.",
                    Path = "link",
                    Related = new [] { "menu", "breadcrumb" },
                    Faq = new []
                    {
                        new FaqItem { Question = "How is RadzenLink different from a plain anchor?", Answer = "It renders a styled navigation link with Path and Target that integrates with Blazor routing and the component theme." }
                    },
                    Icon = "\ue157"
                },
                new Example
                {
                    Toc = [ new () { Text = "Login Events", Anchor = "#login-events" }, new () { Text = "Simple Login", Anchor = "#simple-login" }, new () { Text = "Login with Register (hide password reset)", Anchor = "#login-with-register" }, new () { Text = "Remember me", Anchor = "#remember-me" }, new () { Text = "Form fields", Anchor = "#form-fields" }, new () { Text = "Localization", Anchor = "#localization" }, new () { Text = "Horizontal login layout example", Anchor = "#horizontal-login-example" }, new () { Text = "Vertical login layout example", Anchor = "#vertical-login-example" } ],
                    Name = "Login",
                    Title = "Blazor Login Component | Free UI Components by Radzen",
                    Description = "The Blazor Login component is a ready-made sign-in form with configurable fields, events, and layout.",
                    Path = "login",
                    Related = new [] { "profile-menu", "templateform" },
                    Faq = new []
                    {
                        new FaqItem { Question = "Does the Blazor Login component handle authentication?", Answer = "It provides the sign-in form and raises a Login event with the credentials; you wire that to your own authentication logic." }
                    },
                    Icon = "\uea77"
                },
                new Example
                {
                    Toc = [ new () { Text = "Keyboard Navigation", Anchor = "#keyboard-navigation" } ],
                    Name = "Menu",
                    Title = "Blazor Menu - Navigation Menu | Free UI Components by Radzen",
                    Description = "The Blazor Menu builds horizontal or vertical navigation menus with nested submenus, icons, and templates.",
                    Path = "menu",
                    Related = new [] { "panelmenu", "contextmenu", "profile-menu" },
                    Faq = new []
                    {
                        new FaqItem { Question = "How do I create a navigation menu in Blazor?", Answer = "Add RadzenMenu with RadzenMenuItem children; nest items for submenus and set Icon and Path on each item." }
                    },
                    Icon = "\ue5d2",
                    Tags = new [] { "navigation", "dropdown" }
                },
                new Example
                {
                    Toc = [ new () { Text = "Statically declared items", Anchor = "#panelmenu-static" }, new () { Text = "Programmatically created items with Expanded binding", Anchor = "#panelmenu-programmatic" }, new () { Text = "Set the display style of menu items", Anchor = "#panelmenu-display-style" }, new () { Text = "Keyboard Navigation", Anchor = "#keyboard-navigation" } ],
                    Name = "PanelMenu",
                    Title = "Blazor PanelMenu - Sidebar Menu | Free UI Components by Radzen",
                    Path = "panelmenu",
                    Description = "The Blazor PanelMenu is a vertical, expandable sidebar menu with nested items - ideal for app navigation.",
                    Related = new [] { "menu", "accordion", "profile-menu" },
                    Faq = new []
                    {
                        new FaqItem { Question = "What is the Blazor PanelMenu used for?", Answer = "It is a vertical, collapsible sidebar menu with nested items, commonly used for application navigation." }
                    },
                    Icon = "\ue875",
                    Tags = new [] { "navigation", "menu" }
                },
                new Example
                {
                    Toc = [ new () { Text = "Keyboard Navigation", Anchor = "#keyboard-navigation" } ],
                    Name = "ProfileMenu",
                    Title = "Blazor ProfileMenu Component | Free UI Components by Radzen",
                    Description = "The Blazor ProfileMenu shows a user avatar with a dropdown of account and navigation actions.",
                    Path = "profile-menu",
                    Related = new [] { "menu", "panelmenu", "login" },
                    Faq = new []
                    {
                        new FaqItem { Question = "How do I add a user profile menu in Blazor?", Answer = "Use RadzenProfileMenu with an avatar and RadzenProfileMenuItem children for account and navigation actions." }
                    },
                    Icon = "\ue851",
                    Tags = new [] { "navigation", "dropdown", "menu" }
                },
                new Example
                {
                    Toc = [ new () { Text = "Transition", Anchor = "#transition" }, new () { Text = "CanChange event", Anchor = "#canchange-event" } ],
                    Name = "Steps",
                    Title = "Blazor Steps - Wizard / Stepper | Free UI Components by Radzen",
                    Description = "The Blazor Steps component guides users through a multi-step process (wizard) with numbered stages.",
                    Path = "steps",
                    Related = new [] { "tabs", "breadcrumb" },
                    Faq = new []
                    {
                        new FaqItem { Question = "How do I build a wizard in Blazor?", Answer = "Use RadzenSteps with RadzenStepsItem children; each step shows its content and you can validate before advancing." }
                    },
                    Icon = "\ue8be",
                    Tags = new [] { "step", "steps", "wizard", "transition", "animation" }
                },
                new Example
                {
                    Toc = [ new () { Text = "Tabs position", Anchor = "#tabs-position" }, new () { Text = "Server render mode", Anchor = "#server-render-mode" }, new () { Text = "Client render mode", Anchor = "#client-render-mode" }, new () { Text = "TabItems modify", Anchor = "#tabs-modify" }, new () { Text = "Tab items wrap", Anchor = "#tabs-wrap" }, new () { Text = "Prevent Tab change", Anchor = "#prevent-tab-change" }, new () { Text = "Keyboard Navigation", Anchor = "#keyboard-navigation" } ],
                    Name = "Tabs",
                    Title = "Blazor Tabs Component | Free UI Components by Radzen",
                    Description = "The Blazor Tabs component organizes content into tabbed panels, with positioning, dynamic tabs, and lazy or client/server rendering.",
                    Path = "tabs",
                    Related = new [] { "accordion", "steps" },
                    Faq = new []
                    {
                        new FaqItem { Question = "How do I render tabs lazily in Blazor?", Answer = "Set RenderMode so tab content loads on demand (client) or is kept server-side, and bind SelectedIndex to control the active tab." }
                    },
                    Icon = "\ue8d8",
                    Tags = new [] { "tabstrip", "tabview", "container" }
                },
                new Example
                {
                    Toc = [ new () { Text = "Sticky TOC", Anchor = "#sticky" }, new () { Text = "Orientation", Anchor = "#orientation" } ],
                    Name = "Toc",
                    Title = "Blazor Table of Contents (ToC) | Free UI Components by Radzen",
                    Description = "The Blazor ToC auto-generates a table of contents from the headings on the current page.",
                    Path = "toc",
                    Related = new [] { "breadcrumb", "link" },
                    Faq = new []
                    {
                        new FaqItem { Question = "How does the Blazor ToC build its list?", Answer = "It scans the page's heading elements and generates a linked table of contents automatically." }
                    },
                    Icon = "\ue241",
                    Tags = [ "toc", "content", "navigation" ]
                }
            }
        },
        new Example
        {
            Name = "Layout",
            Icon = "\ue8f1",
            Children = new[] {
                new Example
                {
                    Toc = [ new () { Text = "Sidebar, Header and Footer", Anchor = "#sidebar-header-footer" }, new () { Text = "Full height Sidebar", Anchor = "#full-height-sidebar" }, new () { Text = "Overlay Sidebar", Anchor = "#overlay" }, new () { Text = "Full height overlay Sidebar", Anchor = "#overlay-full" }, new () { Text = "Right Sidebar", Anchor = "#right-sidebar" }, new () { Text = "Right full height Sidebar", Anchor = "#right-full-height-sidebar" }, new () { Text = "Right and Left Sidebar", Anchor = "#right-left-sidebar" }, new () { Text = "Start and End Sidebar", Anchor = "#start-end-sidebar" }, new () { Text = "Icon Sidebar", Anchor = "#icon-sidebar" } ],
                    Name = "Layout",
                    Title = "Blazor Layout - Header, Sidebar, Footer | Free UI Components by Radzen",
                    Description = "The Blazor Layout arranges a page into header, sidebar, body, and footer regions, with a collapsible sidebar.",
                    Path = "layout",
                    Related = new [] { "panel", "stack", "splitter" },
                    Faq = new []
                    {
                        new FaqItem { Question = "How do I add a collapsible sidebar in Blazor?", Answer = "Use RadzenLayout with RadzenSidebar and a RadzenSidebarToggle in the header to expand and collapse the sidebar." }
                    },
                    Icon = "\ue8f1",
                    Tags = new [] { "layout", "sidebar", "drawer", "header", "body", "footer" }
                },
                new Example
                {
                    Name = "Stack",
                    Title = "Blazor Stack - Flex Layout | Free UI Components by Radzen",
                    Description = "The Blazor Stack arranges children horizontally or vertically with consistent spacing.",
                    Path = "stack",
                    Related = new [] { "row", "column" },
                    Faq = new []
                    {
                        new FaqItem { Question = "How do I space elements evenly in Blazor?", Answer = "Use RadzenStack with Orientation and Gap to arrange children in a row or column with consistent spacing." }
                    },
                    Icon = "\ue8e9",
                    Tags = new [] { "stack", "layout" }
                },
                new Example
                {
                    Name = "Row",
                    Title = "Blazor Row - Grid Row Layout | Free UI Components by Radzen",
                    Description = "The Blazor Row arranges columns in a responsive 12-column grid row, with gap and alignment control.",
                    Path = "row",
                    Related = new [] { "column", "stack" },
                    Faq = new []
                    {
                        new FaqItem { Question = "How do I build a responsive grid in Blazor?", Answer = "Use RadzenRow with RadzenColumn children sized per breakpoint to lay out a responsive 12-column grid." }
                    },
                    Icon = "\uf676",
                    Tags = new [] { "row", "layout", "responsive", "grid" }
                },
                new Example
                {
                    Toc = [ new () { Text = "Auto-layout columns", Anchor = "#auto-layout-columns" }, new () { Text = "Column sizes", Anchor = "#column-sizes" }, new () { Text = "Responsive column sizes", Anchor = "#responsive-column-sizes" }, new () { Text = "Column wrapping", Anchor = "#column-wrapping" }, new () { Text = "Column offset", Anchor = "#column-offset" }, new () { Text = "Responsive offsetting", Anchor = "#column-responsive-offset" }, new () { Text = "Column order", Anchor = "#column-order" }, new () { Text = "Responsive column ordering", Anchor = "#column-responsive-order" }, new () { Text = "Nested Layouts", Anchor = "#nested-layouts" }, new () { Text = "Gutters", Anchor = "#gutters" } ],
                    Name = "Column",
                    Title = "Blazor Column - Grid Column | Free UI Components by Radzen",
                    Description = "The Blazor Column defines a responsive column within a Row's 12-column grid, sized per breakpoint.",
                    Path = "column",
                    Related = new [] { "row", "stack" },
                    Faq = new []
                    {
                        new FaqItem { Question = "How do I size columns responsively in Blazor?", Answer = "Set the Size properties (and per-breakpoint sizes) on RadzenColumn within a RadzenRow to control its width across screen sizes." }
                    },
                    Icon = "\uf674",
                    Tags = new [] { "column", "col", "layout", "responsive", "grid" }
                },
                new Example
                {
                    Toc = [ new () { Text = "Card Variant", Anchor = "#card-variant" } ],
                    Name = "Card",
                    Title = "Blazor Card Component | Free UI Components by Radzen",
                    Description = "The Blazor Card is a surface for grouping content, with variants, shadow, and customizable padding.",
                    Path = "card",
                    Related = new [] { "card-group", "panel" },
                    Faq = new []
                    {
                        new FaqItem { Question = "What is the Blazor Card used for?", Answer = "It is a surface that groups related content - text, images, and actions - with elevation and padding." }
                    },
                    Icon = "\uefad",
                    Tags = new [] { "card", "container" }
                },
                new Example
                {
                    Name = "CardGroup",
                    Title = "Blazor CardGroup Component | Free UI Components by Radzen",
                    Description = "The Blazor CardGroup lays out a set of cards as a connected, responsive group.",
                    Path = "card-group",
                    Related = new [] { "card", "stack" },
                    Faq = new []
                    {
                        new FaqItem { Question = "What is the Blazor CardGroup?", Answer = "It arranges multiple RadzenCard elements as a single connected group that wraps responsively." }
                    },
                    Icon = "\ue8f3",
                    Tags = new [] { "card", "group", "deck", "container" }
                },
                new Example
                {
                    Toc = [ new () { Text = "Open page as a dialog", Anchor = "#open-page-as-dialog" }, new () { Text = "Inline Dialog", Anchor = "#inline-dialog" }, new () { Text = "Busy Dialog", Anchor = "#busy-dialog" }, new () { Text = "Confirm Dialog", Anchor = "#confirm-dialog" }, new () { Text = "Alert Dialog", Anchor = "#alert-dialog" }, new () { Text = "Prevent dialog from closing", Anchor = "#prevent-close" }, new () { Text = "Close Dialog by clicking outside", Anchor = "#close-dialog-by-clicking-outside" }, new () { Text = "Side Dialog", Anchor = "#side-dialog" }, new () { Text = "Dialog with custom CSS classes", Anchor = "#custom-css-classes" }, new () { Text = "Update dialog properties", Anchor = "#cascading-value" } ],
                    Name = "Dialog",
                    Title = "Blazor Dialog - Modal Dialog | Free UI Components by Radzen",
                    Description = "The Blazor Dialog opens modal dialogs and side panels from code via DialogService, with Alert and Confirm helpers, custom content, sizing, and async results.",
                    Path = "dialog",
                    Related = new [] { "popup", "card" },
                    Faq = new []
                    {
                        new FaqItem { Question = "How do I open a dialog in Blazor?", Answer = "Inject DialogService and call Open or OpenAsync with a component or content; OpenAsync returns the dialog's result when it closes." }
                    },
                    Icon = "\ue069",
                    Tags = new [] { "popup", "window" },
                },
                new Example
                {
                    Toc = [ new () { Text = "Define can-drop and no-drop styles", Anchor = "#can-drop-no-drop-styles" }, new () { Text = "Define a Footer Template per Drop Zone", Anchor = "#footer-template" } ],
                    Name = "DropZone",
                    Title = "Blazor DropZone - Drag & Drop | Free UI Components by Radzen",
                    Description = "The Blazor DropZone lets users drag and drop items between zones - for kanban boards and reordering.",
                    Path = "dropzone",
                    Related = new [] { "tile-layout", "splitter" },
                    Faq = new []
                    {
                        new FaqItem { Question = "How do I build a drag-and-drop list in Blazor?", Answer = "Use RadzenDropZoneContainer with RadzenDropZone areas and handle the drop events to move items between zones." }
                    },
                    Icon = "\ue945",
                    Tags = new [] { "dropzone", "drag", "drop" }
                },
                new Example
                {
                    Name = "Panel",
                    Title = "Blazor Panel - Collapsible Panel | Free UI Components by Radzen",
                    Description = "The Blazor Panel is a titled, collapsible container for grouping content.",
                    Path = "panel",
                    Related = new [] { "card", "fieldset" },
                    Faq = new []
                    {
                        new FaqItem { Question = "How do I make a collapsible panel in Blazor?", Answer = "Set AllowCollapse on RadzenPanel so users can expand and collapse its content under the title." }
                    },
                    Icon = "\uf732",
                    Tags = new [] { "container" }
                },
                new Example
                {
                    Name = "Popup",
                    Title = "Blazor Popup Component | Free UI Components by Radzen",
                    Description = "The Blazor Popup shows floating content anchored to an element via PopupService, for custom dropdowns and overlays.",
                    Path = "popup",
                    Related = new [] { "dialog", "contextmenu" },
                    Faq = new []
                    {
                        new FaqItem { Question = "How do I show a popup anchored to an element in Blazor?", Answer = "Use PopupService (or the RadzenPopup component) to open floating content positioned relative to a target element." }
                    },
                    Icon = "\ue0ca",
                    Tags = new [] { "popup", "dropdown"}
                },
                new Example
                {
                    Name = "Splitter",
                    Title = "Blazor Splitter - Resizable Panes | Free UI Components by Radzen",
                    Description = "The Blazor Splitter divides an area into resizable, collapsible panes, horizontally or vertically.",
                    Path = "splitter",
                    Related = new [] { "layout", "stack" },
                    Faq = new []
                    {
                        new FaqItem { Question = "How do I create resizable panes in Blazor?", Answer = "Use RadzenSplitter with RadzenSplitterPane children; users drag the bars to resize, and panes can be collapsible." }
                    },
                    Icon = "\ue42a",
                    Tags = new [] { "splitter", "layout"}
                },
                new Example
                {
                    Name = "TileLayout",
                    New = true,
                    Title = "Blazor TileLayout - Dashboard Tiles | Free UI Components by Radzen",
                    Description = "The Blazor TileLayout builds dashboards from draggable, resizable tiles.",
                    Path = "tile-layout",
                    Related = new [] { "dashboard", "dropzone" },
                    Faq = new []
                    {
                        new FaqItem { Question = "How do I build a tile dashboard in Blazor?", Answer = "Use RadzenTileLayout with RadzenTile children; users can drag and resize tiles to arrange the dashboard." }
                    },
                    Icon = "\ue871",
                    Tags = new [] { "tile", "layout", "grid", "dashboard", "drag", "resize" }
                }
            }
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
                    Title = "Blazor Themes | Free UI Components by Radzen",
                    Description = "Choose from free and premium Blazor themes for Radzen Blazor components - including Material and dark themes - or build your own with the theme customization tools.",
                    Related = new [] { "theme-service", "colors", "appearance-toggle" },
                    Faq = new []
                    {
                        new FaqItem { Question = "How do I change the theme in Blazor?", Answer = "Reference the theme's CSS and set it through ThemeService (or the Theme parameter); themes can switch at runtime, including a dark-mode toggle." },
                        new FaqItem { Question = "Does Radzen Blazor offer dark mode themes?", Answer = "Yes. Several themes ship with dark variants, and users can toggle between light and dark at runtime." },
                        new FaqItem { Question = "Can I create a custom Blazor theme?", Answer = "Yes. Start from a built-in theme and customize its colors and variables, or use the theme customization tools to build your own." }
                    },
                    Icon = "\ue40a",
                    Tags = new[] { "theme", "color", "background", "border", "utility", "css", "var"}
                },
                new Example
                {
                    Toc = [ new () { Text = "Persist the Theme", Anchor = "#persist" }, new () { Text = "Video: Changing themes at runtime in Radzen Blazor Studio", Anchor = "#video-changing-themes-at-runtime" } ],
                    Name = "ThemeService",
                    Path = "theme-service",
                    Title = "Blazor ThemeService | Free UI Components by Radzen",
                    Description = "The ThemeService allows to change the theme of the application at runtime.",
                    Icon = "\ue3ae",
                    Tags = ["theme", "service", "change", "runtime", "rtl", "right to left", "direction", "wcag", "accessibility"]
                },
                new Example
                {
                    Toc = [ new () { Text = "Switch between light and dark mode", Anchor = "#light-dark-mode" }, new () { Text = "Video: AppearanceToggle in Radzen Blazor Studio", Anchor = "#video-radzen-blazor-studio-config" }, new () { Text = "Keyboard Navigation", Anchor = "#keyboard-navigation" } ],
                    Name = "AppearanceToggle",
                    Path = "appearance-toggle",
                    Title = "Blazor Appearance Toggle | Free UI Components by Radzen",
                    Description = "The AppearanceToggle button allows you to switch between two predefined themes, most commonly light and dark.",
                    Icon = "\ueb37",
                    Tags = new[] { "theme", "light", "dark", "mode", "appearance", "toggle", "switch"}
                },
                new Example
                {
                    Toc = [ new () { Text = "Theme Colors", Anchor = "#theme-colors" }, new () { Text = "Utility CSS Classes", Anchor = "#utility-css-classes" }, new () { Text = "Video: Theme Colors in Radzen Blazor Studio", Anchor = "#video-theme-colors" } ],
                    Name = "Colors",
                    Path = "colors",
                    Title = "Blazor Color Utilities | Free UI Components by Radzen",
                    Description = "List of colors and utility CSS classes available in Radzen Blazor Components library.",
                    Icon = "\ue997",
                    Tags = new[] { "color", "background", "border", "utility", "css", "var"}
                },
                new Example
                {
                    Toc = [ new () { Text = "Text Style", Anchor = "#text-style" }, new () { Text = "Text Style and Tag Name", Anchor = "#text-tag-name" }, new () { Text = "Display headings", Anchor = "#text-display-headings" }, new () { Text = "Text Align", Anchor = "#text-align" }, new () { Text = "Text Functional Colors", Anchor = "#text-color" }, new () { Text = "Text Transform", Anchor = "#text-transform" }, new () { Text = "Text Wrap", Anchor = "#text-wrap" }, new () { Text = "Video: How Typography Works in Radzen Blazor Studio", Anchor = "#video-radzen-text" } ],
                    Name = "Typography",
                    Path = "typography",
                    Title = "Blazor Text Component | Free UI Components by Radzen",
                    Description = "Use the RadzenText component to format text in your applications. The TextStyle property applies a predefined text style such as H1, H2, etc.",
                    Icon = "\ue264",
                    Tags = new [] { "typo", "typography", "text", "paragraph", "header", "heading", "caption", "overline", "content" }
                },
                new Example
                {
                    Toc = [ new () { Text = "Material Icons", Anchor = "#material-icons" }, new () { Text = "Icon color", Anchor = "#icon-color" }, new () { Text = "Filled icons", Anchor = "#filled-icons" }, new () { Text = "Styled icons", Anchor = "#styled-icons" }, new () { Text = "Using RadzenIcon with other icon fonts", Anchor = "#icons-width-other-fonts" }, new () { Text = "Video: RadzenIcon in Radzen Blazor Studio", Anchor = "#video-icons" } ],
                    Name = "Icons",
                    Path = "icon",
                    Title = "Blazor Icon Component | Free UI Components by Radzen",
                    Description = "Display Material icons in Blazor with the RadzenIcon component - control size and color, and use custom icon fonts.",
                    Related = new [] { "button", "fab" },
                    Faq = new []
                    {
                        new FaqItem { Question = "How do I add an icon in Blazor?", Answer = "Use the RadzenIcon component and set its Icon property to a Material icon name; the icon renders inline wherever you place it." },
                        new FaqItem { Question = "How do I change an icon's size or color?", Answer = "Control the color with IconStyle or a Style/class, and set the font size (for example via Style) to change the icon size." },
                        new FaqItem { Question = "Can I use custom icons?", Answer = "Yes. In addition to the built-in Material icons, you can use a custom icon font or image-based icons." }
                    },
                    Icon = "\ue148",
                    Tags = new [] { "icon", "content" }
                },
                new Example
                {
                    Toc = [ new () { Text = "Video: Styling Borders in Radzen Blazor Studio", Anchor = "#video-borders" }, new () { Text = "Border radius", Anchor = "#border-radius" }, new () { Text = "Add or remove borders arbitrarily", Anchor = "#add-remove-css-classes" }, new () { Text = "Border color utility CSS classes", Anchor = "#color-css-classes" }, new () { Text = "Border with color utility CSS classes", Anchor = "#utility-css-classes" }, new () { Text = "Set border width via CSS variable", Anchor = "#border-width" }, new () { Text = "Borders with CSS variables", Anchor = "#css-variables" } ],
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
                    Title = "Blazor Overflow Utilities | Free UI Components by Radzen",
                    Description = "Overflow styles and utility CSS classes available in Radzen Blazor Components library.",
                    Path = "overflow",
                    Icon = "\uf829",
                    Tags = new [] { "overflow", "content", "width", "height", "size", "wrap", "hide", "hidden", "visible", "utility", "css", "var"}
                },
                new Example
                {
                    Toc = [ new () { Text = "Basic Usage", Anchor = "#basic-usage" }, new () { Text = "Show/Hide Content Based on Screen Size", Anchor = "#responsive-content" }, new () { Text = "Multiple Breakpoints", Anchor = "#multiple-breakpoints" }, new () { Text = "Device Orientation", Anchor = "#orientation" } ],
                    Name = "MediaQuery",
                    Title = "Blazor MediaQuery Component | Free UI Components by Radzen",
                    Description = "Respond to browser viewport size changes using CSS media queries. Perfect for creating responsive Blazor applications.",
                    Path = "media-query",
                    Icon = "\ue337",
                    Tags = new [] { "mediaquery", "media", "query", "responsive", "breakpoint", "viewport", "screen", "mobile", "tablet", "desktop", "orientation", "utility"}
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
                    Tags = new [] { "skeleton", "load", "loading", "placeholder", "animation", "wave", "pulse", "text", "circular", "rectangular", "rounded" }
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
            Name = "App Templates",
            Title = "Blazor App Templates | Free UI Components by Radzen",
            Description = "Ready to use Blazor application and website templates",
            Icon = "\ue5c3",
            Children = new[] {
                new Example
                {
                    Name = "Issues Dashboard",
                    Path = "/dashboard",
                    Title = "Sample Blazor Dashboard | Free UI Components by Radzen",
                    Description = "A sample Blazor dashboard built with Radzen Blazor Components - charts, grids, and cards on one page, visualizing live GitHub issues.",
                    Related = new [] { "tile-layout", "datagrid", "charts" },
                    Faq = new []
                    {
                        new FaqItem { Question = "How do I build a dashboard in Blazor?", Answer = "Combine Radzen Blazor components such as charts, DataGrids, cards, and the TileLayout on a single page and bind them to your data; this sample visualizes live GitHub issues." }
                    },
                    Icon = "\ue868"
                },
                new Example
                {
                    Name = "Healthcare",
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
            Toc = [ new () { Text = "Centered CTA", Anchor = "#centered-cta" }, new () { Text = "Left-aligned CTA", Anchor = "#left-aligned-cta" }, new () { Text = "Justified CTA", Anchor = "#left-aligned-cta" }, new () { Text = "Image to the left", Anchor = "#image-to-the-left" }, new () { Text = "Image to the right", Anchor = "#image-to-the-right" } ],
            Name = "UI Blocks",
            Pro = true,
            Title = "Blazor UI Blocks | Free UI Components by Radzen",
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
            Name = "Feedback",
            Icon = "\ue0cb",
            Children = new[] {
                new Example
                {
                    Name = "Alert",
                    Title = "Blazor Alert Component | Free UI Components by Radzen",
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
                            Title = "Blazor Alert - Styling | Free UI Components by Radzen",
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
                    Description = "Blazor Badge component for displaying counts, labels, and status indicators with multiple styles and variants.",
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
                    Description = "Blazor Tooltip component with configurable positions, HTML content, delay, and duration settings.",
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
            Toc = [ new () { Text = "Applying guidelines", Anchor = "#applying-guidelines" }, new () { Text = "WCAG 2.2", Anchor = "#wcag" }, new () { Text = "WCAG compliant theme colors (AA level of conformance)", Anchor = "#wcag-colors" }, new () { Text = "ARIA attributes", Anchor = "#wai-aria" }, new () { Text = "Semantic HTML", Anchor = "#semantic-html" }, new () { Text = "Screen reader compatibility", Anchor = "#screen-readers" }, new () { Text = "Responsive design", Anchor = "#responsive-design" }, new () { Text = "Keyboard compatibility", Anchor = "#keyboard-compatibility" }, new () { Text = "Accessibility Conformance Report", Anchor = "#acr" } ],
            Name = "Accessibility",
            Path = "/accessibility",
            Title = "Blazor Accessibility | Free UI Components by Radzen",
            Description = "Accessible Blazor components compliant with WAI-ARIA, WCAG 2.2, Section 508, and keyboard navigation standards.",
            Icon = "\ue92c",
            Tags = new[] { "keyboard", "accessibility", "standard", "508", "wai-aria", "wcag", "shortcut"}
        },
        new Example
        {
            Name = "Changelog",
            Path = "/changelog",
            Updated = true,
            Title = "Blazor Components Changelog | Free UI Components by Radzen",
            Description = "See what's new in Radzen Blazor Components",
            Icon = "\ue8e1"
        }
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

        public string NameFor(string path)
        {
            IEnumerable<Example> Flatten(IEnumerable<Example> e) =>
                e.SelectMany(c => c.Children != null ? Flatten(c.Children) : new[] { c });

            var p = path?.TrimStart('/');
            return Flatten(Examples).FirstOrDefault(e => e.Path?.TrimStart('/') == p)?.Name ?? p;
        }

        // The chart/data-visualization pages listed in the /charts gallery: every leaf under the
        // "Data Visualization" category except the gallery itself, the "Configuration" feature demos,
        // and the non-chart components bundled in that category (timeline, qrcode, barcode, googlemap, ssrsviewer).
        public IEnumerable<Example> GetChartPages()
        {
            var nonChart = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "charts", "timeline", "qrcode", "barcode", "googlemap", "ssrsviewer"
            };

            IEnumerable<Example> Collect(IEnumerable<Example> nodes)
            {
                foreach (var node in nodes)
                {
                    if (node.Name == "Configuration")
                    {
                        continue;
                    }

                    if (node.Children != null)
                    {
                        foreach (var child in Collect(node.Children))
                        {
                            yield return child;
                        }
                    }
                    else if (!string.IsNullOrEmpty(node.Path) && !nonChart.Contains(node.Path.TrimStart('/')))
                    {
                        yield return node;
                    }
                }
            }

            var dataViz = Examples.FirstOrDefault(c => c.Name == "Data Visualization");
            return dataViz?.Children != null ? Collect(dataViz.Children).ToList() : Enumerable.Empty<Example>();
        }

        // The chart configuration/feature demos (axis, legend, tooltip, ...). Article-eligible for
        // schema, but intentionally NOT part of the /charts gallery ItemList.
        public IEnumerable<Example> GetChartConfigPages()
        {
            var dataViz = Examples.FirstOrDefault(c => c.Name == "Data Visualization");
            var config = dataViz?.Children?.FirstOrDefault(c => c.Name == "Configuration");
            return config?.Children?.Where(e => !string.IsNullOrEmpty(e.Path)) ?? Enumerable.Empty<Example>();
        }

        static IEnumerable<Example> CollectLeaves(IEnumerable<Example> nodes)
        {
            foreach (var node in nodes)
            {
                if (node.Children != null)
                {
                    foreach (var child in CollectLeaves(node.Children))
                    {
                        yield return child;
                    }
                }
                else if (!string.IsNullOrEmpty(node.Path))
                {
                    yield return node;
                }
            }
        }

        // Every leaf page under the "DataGrid" category (across its sub-groups). Article-eligible for schema.
        public IEnumerable<Example> GetDataGridPages()
        {
            var dataGrid = Examples.FirstOrDefault(c => c.Name == "DataGrid");
            return dataGrid?.Children != null ? CollectLeaves(dataGrid.Children).ToList() : Enumerable.Empty<Example>();
        }

        // Every leaf page under the "PivotDataGrid" category. Article-eligible for schema.
        public IEnumerable<Example> GetPivotDataGridPages()
        {
            var pivot = Examples.FirstOrDefault(c => c.Name == "PivotDataGrid");
            return pivot?.Children != null ? CollectLeaves(pivot.Children).ToList() : Enumerable.Empty<Example>();
        }

        // Every leaf page under the "Spreadsheet" category. Article-eligible for schema.
        public IEnumerable<Example> GetSpreadsheetPages()
        {
            var spreadsheet = Examples.FirstOrDefault(c => c.Name == "Spreadsheet");
            return spreadsheet?.Children != null ? CollectLeaves(spreadsheet.Children).ToList() : Enumerable.Empty<Example>();
        }

        // Maps each Forms page path to its component hub (display label + primary page path). Unlike
        // charts or DataGrid, Forms has no single landing page; every component (DropDown, Button, ...)
        // is its own hub. Single-page components hub on themselves; grouped ones (DropDown, HtmlEditor)
        // hub on their first child. Drives per-component breadcrumbs (Home > Component > variant).
        public Dictionary<string, (string Label, string Path)> GetFormsComponentHubs()
        {
            var hubs = new Dictionary<string, (string, string)>(StringComparer.OrdinalIgnoreCase);
            var forms = Examples.FirstOrDefault(c => c.Name == "Forms");
            if (forms?.Children == null)
            {
                return hubs;
            }

            foreach (var component in forms.Children)
            {
                var leaves = (component.Children != null ? CollectLeaves(component.Children) : new[] { component })
                    .Where(e => !string.IsNullOrEmpty(e.Path)).ToList();
                if (!leaves.Any())
                {
                    continue;
                }

                var primaryPath = leaves[0].Path.TrimStart('/');
                foreach (var leaf in leaves)
                {
                    hubs[leaf.Path.TrimStart('/')] = (component.Name, primaryPath);
                }
            }

            return hubs;
        }

        public string TitleFor(Example example)
        {
            if (example != null && (example.Name != "Overview" || example.Title != null))
            {
                return example.Title ?? $"Blazor {example.Name} | Free UI Components by Radzen";
            }

            return "Free Blazor Components | 145+ UI controls by Radzen";
        }

        public string DescriptionFor(Example example)
        {
            return example?.Description ?? "The Radzen Blazor component library provides more than 100 UI controls for building rich ASP.NET Core web applications.";
        }
    }
}
