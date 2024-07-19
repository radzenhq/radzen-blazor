using System;
using System.Collections.Generic;
using System.Linq;

namespace RadzenBlazorDemos
{
    public class ExampleService
    {
        Example[] allExamples = new[] {
        new Example()
        {
            Name = "Overview",
            Path = "/",
            Icon = "&#xe88a"
        },
        new Example()
        {
            Name = "Dashboard",
            Path = "/dashboard",
            Title = "Sample Dashboard | Free UI Components by Radzen",
            Description = "Rich dashboard created with the Radzen Blazor Components library.",
            Icon = "&#xe871"
        },
        new Example
        {
            Name = "Get Started",
            Path = "/get-started",
            Title = "Get Started | Free UI Components by Radzen",
            Description = "How to get started with the Radzen Blazor Components library.",
            Icon = "&#xe037"
        },
        new Example
        {
            Name = "Support",
            Path = "/support",
            Title = "Support | Free UI Components by Radzen",
            Description = "How to get support for the Radzen Blazor Components library.",
            Icon = "&#xe94c"
        },
        new Example
        {
            Name = "Accessibility",
            Path = "/accessibility",
            Title = "Blazor Accessibility | Free UI Components by Radzen",
            Description = "The accessible Radzen Blazor Components library covers highest levels of web accessibility guidelines and recommendations, making you Blazor app compliant with WAI-ARIA, WCAG 2.2, section 508, and keyboard compatibility standards.",
            Icon = "&#xe92c",
            Tags = new[] { "keyboard", "accessibility", "standard", "508", "wai-aria", "wcag", "shortcut"}
        },

        new Example()
        {
            Name = "UI Fundamentals",
            Icon = "&#xe749",
            Children = new [] {
                new Example()
                {
                    Name = "Themes",
                    Path = "themes",
                    Updated = true,
                    Title = "Blazor Themes | Free UI Components by Radzen",
                    Description = "The Radzen Blazor Components package features an array of both free and premium themes, allowing you to choose the style that best suits your project's requirements.",
                    Icon = "&#xe40a",
                    Tags = new[] { "theme", "color", "background", "border", "utility", "css", "var"}
                },
                new Example()
                {
                    Name = "AppearanceToggle",
                    Path = "appearance-toggle",
                    New = true,
                    Title = "Blazor Themes | Free UI Components by Radzen",
                    Description = "The AppearanceToggle button allows you to switch between two predefined themes, most commonly light and dark.",
                    Icon = "&#xe51c",
                    Tags = new[] { "theme", "light", "dark", "mode", "appearance", "toggle", "switch"}
                },
                new Example()
                {
                    Name = "Colors",
                    Path = "colors",
                    Updated = true,
                    Title = "Blazor Color Utilities | Free UI Components by Radzen",
                    Description = "List of colors and utility CSS classes available in Radzen Blazor Components library.",
                    Icon = "&#xe891",
                    Tags = new[] { "color", "background", "border", "utility", "css", "var"}
                },
                new Example()
                {
                    Name = "Typography",
                    Path = "typography",
                    Title = "Blazor Text Component | Free UI Components by Radzen",
                    Description = "Use the RadzenText component to format text in your applications. The TextStyle property applies a predefined text style such as H1, H2, etc.",
                    Icon = "&#xe264",
                    Tags = new [] { "typo", "typography", "text", "paragraph", "header", "heading", "caption", "overline", "content" }
                },
                new Example()
                {
                    Name = "Icons",
                    Path = "icon",
                    Updated = true,
                    Title = "Blazor Icon Component | Free UI Components by Radzen",
                    Description = "Demonstration and configuration of the Radzen Blazor Icon component.",
                    Icon = "&#xe148",
                    Tags = new [] { "icon", "content" }
                },
                new Example()
                {
                    Name = "Borders",
                    Path = "borders",
                    Title = "Blazor Border Utilities | Free UI Components by Radzen",
                    Description = "Border styles and utility CSS classes for borders available in Radzen Blazor Components library.",
                    Icon = "&#xe3c6",
                    Tags = new [] { "border", "utility", "css", "var"}
                },
                new Example()
                {
                    Name = "Shadows",
                    Path = "shadows",
                    Title = "Blazor Shadow Utilities | Free UI Components by Radzen",
                    Description = "Shadow styles and utility CSS classes for shadows available in Radzen Blazor Components library.",
                    Icon = "&#xe595",
                    Tags = new [] { "shadow", "utility", "css", "var"}
                },
                new Example()
                {
                    Name = "Ripple",
                    Title = "Blazor Ripple Effect | Free UI Components by Radzen",
                    Description = "See how to apply the ripple effect to various UI elements.",
                    Path = "ripple",
                    Icon = "&#xe39e",
                    Tags = new [] { "ripple", "utility", "css", "var"}
                },
                new Example()
                {
                    Name = "Breakpoints",
                    Title = "Blazor Responsive Breakpoints | Free UI Components by Radzen",
                    Description = "Responsive breakpoints are used to adjust the layout based on the screen size of the device in use.",
                    Path = "breakpoints",
                    Icon = "&#xe1b1",
                    Tags = new [] { "breakpoints", "spacing", "margin", "padding", "gutter", "gap", "utility", "css", "responsive", "layout"}
                },
                new Example()
                {
                    Name = "Spacing",
                    Title = "Blazor Spacing Utilities | Free UI Components by Radzen",
                    Description = "Spacing styles and utility CSS classes for margin and padding available in Radzen Blazor Components library.",
                    Path = "spacing",
                    Icon = "&#xe256",
                    Tags = new [] { "spacing", "margin", "padding", "gutter", "gap", "utility", "css", "var"}
                }
            }
        },

        new Example()
        {
            Name = "DataGrid",
            Updated = true,
            Icon = "&#xf1be",
            Children = new [] {
                new Example
                {
                    Name = "Data-binding",
                    Icon = "&#xe3ec",
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
                    Icon = "&#xe871",
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
                    Icon = "&#xe336",
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
                            Name = "Filter Template",
                            Path = "datagrid-filter-template",
                            Title = "Blazor DataGrid Component - Custom Filtering | Free UI Components by Radzen",
                            Description = "This example demonstrates how to define custom RadzenDataGrid column filter template.",
                            Tags = new [] { "datagrid", "column", "filter", "template" }
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
                    Icon = "&#xef4f",
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
                            Name = "Enum filtering",
                            Path = "datagrid-enum-filter",
                            Title = "Blazor DataGrid Component - Enum Filtering | Free UI Components by Radzen",
                            Description = "This example demonstrates how to use enums in the RadzenDataGrid column filter.",
                            Tags = new [] { "filter", "enum", "grid", "datagrid", "table"}
                        },
                        new Example
                        {
                            Name = "Filter API",
                            Path = "datagrid-filter-api",
                            Title = "Blazor DataGrid Component - Filter API | Free UI Components by Radzen",
                            Description = "Set the initial filter of your RadzenDataGrid via the FilterValue and FilterOperator column properties.",
                            Tags = new [] { "filter", "api", "grid", "datagrid", "table"}
                        },
                    }
                },
                new Example
                {
                    Name = "Hierarchy",
                    Updated = true,
                    Icon = "&#xe23e",
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
                    Icon = "&#xf0c5",
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
                    Icon = "&#xe164",
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
                    Icon = "&#xe5dd",
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
                    Icon = "&#xf1be",
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
                new Example()
                {
                    Name = "Density",
                    Path = "datagrid-density",
                    Title = "Blazor DataGrid Component - Density | Free UI Components by Radzen",
                    Description = "See how to set a compact density mode of Blazor RadzenDataGrid.",
                    Icon = "&#xeb9e",
                    Tags = new [] { "density", "compact", "small", "large", "tight" }
                },
                new Example()
                {
                    Name="Custom Header",
                    Icon = "&#xe051",
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
                new Example()
                {
                    Name = "GridLines",
                    Path = "datagrid-grid-lines",
                    Title = "Blazor DataGrid Component - Grid Lines | Free UI Components by Radzen",
                    Description = "Deside where to display grid lines in your Blazor RadzenDataGrid.",
                    Icon = "&#xf016",
                    Tags = new [] { "grid", "lines", "border", "gridlines" }
                },
                new Example()
                {
                    Name = "Cell Context Menu",
                    Path = "datagrid-cell-contextmenu",
                    Title = "Blazor DataGrid Component - Cell Context Menu | Free UI Components by Radzen",
                    Description = "Right click on a table cell to open the context menu.",
                    Icon = "&#xe22b",
                    Tags = new [] { "cell", "row", "contextmenu", "menu", "rightclick" }
                },

                new Example
                {
                    Updated = true,
                    Name = "Save/Load settings",
                    Icon = "&#xf02e",
                    Children = new []
                    {
                        new Example()
                        {
                            Name = "IQueryable",
                            Path = "datagrid-save-settings",
                            Title = "Blazor DataGrid Component - Save / Load Settings | Free UI Components by Radzen",
                            Description = "This example shows how to save/load DataGrid state using Settings property. The state includes current page index, page size, groups and columns filter, sort, order, width and visibility.",
                            Tags = new [] { "save", "load", "settings" }
                        },

                        new Example()
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
                    Icon = "&#xe945",
                    Children = new []
                    {
                        new Example()
                        {
                            Name = "Rows reorder",
                            Path = "/datagrid-rowreorder",
                            Title = "Blazor DataGrid Component - Reorder rows | Free UI Components by Radzen",
                            Description = "This example demonstrates custom DataGrid rows reoder.",
                            Tags = new [] { "datagrid", "reorder", "row" }
                        },
                        new Example()
                        {
                            Name = "Drag row between two DataGrids",
                            Path = "/datagrid-rowdragbetween",
                            Title = "Blazor DataGrid Component - Drag rows between two DataGrids | Free UI Components by Radzen",
                            Description = "This example demonstrates drag and drop rows between two DataGrid components.",
                            Tags = new [] { "datagrid", "drag", "row", "between" }
                        },
                        new Example()
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

                new Example()
                {
                    Name = "InLine Editing",
                    Path = "datagrid-inline-edit",
                    Title = "Blazor DataGrid Component - InLine Editing | Free UI Components by Radzen",
                    Description = "This example demonstrates how to configure the Razden Blazor DataGrid for inline editing.",
                    Icon = "&#xe22b",
                    Tags = new [] { "inline", "editor", "datagrid", "table", "dataview" }
                },

                new Example()
                {
                    New = true,
                    Name = "InCell Editing",
                    Path = "datagrid-incell-edit",
                    Title = "Blazor DataGrid Component - InCell Editing | Free UI Components by Radzen",
                    Description = "This example demonstrates how to configure the Razden Blazor DataGrid for in-cell editing.",
                    Icon = "&#xe745",
                    Tags = new [] { "in-cell", "editor", "datagrid", "table", "dataview" }
                },

                new Example()
                {
                    Name = "Conditional formatting",
                    Path = "datagrid-conditional-template",
                    Title = "Blazor DataGrid Component - Conditional Formatting | Free UI Components by Radzen",
                    Description = "This example demonstrates RadzenDataGrid with conditional rows and cells template and styles.",
                    Icon = "&#xe41d",
                    Tags = new [] { "conditional", "template", "style", "datagrid", "table", "dataview" }
                },
                new Example()
                {
                    Name = "Export to Excel and CSV",
                    Path = "export-excel-csv",
                    Title = "Blazor DataGrid Component - Export to Excel and CSV | Free UI Components by Radzen",
                    Description = "This example demonstrates how to export a Radzen Blazor DataGrid to Excel and CSV.",
                    Icon = "&#xe0c3",
                    Tags = new [] { "export", "excel", "csv" }
                },
                new Example()
                {
                    Name = "Cascading DropDowns",
                    Path = "cascading-dropdowns",
                    Title = "Blazor DataGrid Component - Cascading DropDowns | Free UI Components by Radzen",
                    Description = "This example demonstrates cascading Radzen Blazor DropDown components.",
                    Icon = "&#xe915",
                    Tags = new [] { "related", "parent", "child" }
                },
                new Example()
                {
                    Name = "Empty Data Grid",
                    Path = "/datagrid-empty",
                    Title = "Blazor DataGrid Component - Empty Data Grid | Free UI Components by Radzen",
                    Description = "This example demonstrates Blazor DataGrid without data.",
                    Icon = "&#xe661",
                    Tags = new [] { "datagrid", "databinding" }
                }
            }
        },
        new Example
        {
            Name = "Data",
            Updated = true,
            Icon = "&#xe1db",
            Children = new [] {
                new Example()
        {
            Name = "DataList",
                    Icon = "&#xe896",
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
                new Example()
        {
            Name = "DataFilter",
                    Icon = "&#xef4f",
                    Tags = new[] { "dataview", "grid", "table", "filter" },
                    Children = new[] {
                        new Example
                        {
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
                new Example()
        {
            Name = "Pager",
                    Path = "pager",
                    Description = "Demonstration and configuration of the Radzen Blazor Pager component.",
                    Icon = "&#xe8be",
                    Tags = new[] { "pager", "paging" }
                },
                new Example()
        {
            Name = "Scheduler",
                    Path = "scheduler",
                    Updated = true,
                    Description = "Blazor Scheduler component with daily, weekly and monthly views.",
                    Icon = "&#xe616",
                    Tags = new[] { "scheduler", "calendar", "event", "appointment" }
                },
                new Example()
        {
            Name = "Tree",
                    Icon = "&#xe8ef",
                    Tags = new[] { "tree", "treeview", "nodes", "hierarchy" },
                    Children = new[] {
                        new Example
                        {
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
                        }
                    }
                },
                new Example()
                {
                    Name = "PickList",
                    New = true,
                    Description = "Use Radzen Blazor PickList component to transfer items between two collections.",
                    Path = "picklist",
                    Icon = "&#xe0b8",
                    Tags = new[] { "picklist", "list", "listbox" }
                },
            }
},
        new Example()
        {
            Name = "Images",
            Icon = "&#xe3d3",
            Children = new[] {
                new Example()
                {
                    Name = "Gravatar",
                    Description = "Demonstration and configuration of the Radzen Blazor Gravatar component.",
                    Path = "gravatar",
                    Icon = "&#xe420"
                },
                new Example()
                {
                    Name = "Image",
                    Description = "Demonstration and configuration of the Radzen Blazor Image component.",
                    Path = "image",
                    Icon = "&#xe3c4"
                },
            }
        },
        new Example()
        {
            Name = "Layout",
            Updated = true,
            Icon = "&#xe8f1",
            Children = new[] {
                new Example()
                {
                    Name = "Layout",
                    Description = "Blazor RadzenLayout allows you to define the global layout of your application.",
                    Path = "layout",
                    Icon = "&#xe8f1",
                    Tags = new [] { "layout", "sidebar", "drawer", "header", "body", "footer" }
                },
                new Example()
                {
                    Name = "Stack",
                    Description = "Use RadzenStack component to create a stack layout - a way of arranging elements in a vertical or horizontal stack.",
                    Path = "stack",
                    Icon = "&#xe8f2",
                    Tags = new [] { "stack", "layout" }
                },
                new Example()
                {
                    Name = "Row",
                    Description = "Blazor RadzenRow component is used to create a row in a responsive grid layout.",
                    Path = "row",
                    Icon = "&#xf101",
                    Tags = new [] { "row", "layout", "responsive", "grid" }
                },
                new Example()
                {
                    Name = "Column",
                    Description = "Blazor RadzenColumn component is used within a RadzenRow to create a structured grid layout. Columns are positioned on a 12-column based responsive grid.",
                    Path = "column",
                    Icon = "&#xe8ec",
                    Tags = new [] { "column", "col", "layout", "responsive", "grid" }
                },
                new Example()
                {
                    Name = "Card",
                    Description = "Use the Blazor RadzenCard component to display a piece of content, like an image and text.",
                    Path = "card",
                    Icon = "&#xe919",
                    Tags = new [] { "container" }
                },
                new Example()
                {
                    Name = "Dialog",
                    Description = "Demonstration and configuration of the Blazor RadzenDialog component.",
                    Path = "dialog",
                    Icon = "&#xe8a7",
                    Tags = new [] { "popup", "window" },
                },
                new Example()
                {
                    New = true,
                    Name = "DropZone",
                    Description = "Demonstration and configuration of the Radzen Blazor DropZone component.",
                    Path = "dropzone",
                    Icon = "&#xe945",
                    Tags = new [] { "dropzone", "drag", "drop" }
                },
                new Example()
                {
                    Name = "Panel",
                    Description = "Demonstration and configuration of the Blazor RadzenPanel component.",
                    Path = "panel",
                    Icon = "&#xe14f",
                    Tags = new [] { "container" }
                },
                new Example()
                {
                    New = true,
                    Name = "Popup",
                    Description = "Demonstration and configuration of the Radzen Blazor Popup component.",
                    Path = "popup",
                    Icon = "&#xe0cb",
                    Tags = new [] { "popup", "dropdown"}
                },
                new Example()
                {
                    Name = "Splitter",
                    Description = "Demonstration and configuration of the Blazor RadzenSplitter component.",
                    Path = "splitter",
                    Icon = "&#xe94f",
                    Tags = new [] { "splitter", "layout"}
                }
            }
        },
        new Example()
        {
            Name = "Navigation",
            Icon = "&#xe762",
            Children = new[] {
                new Example()
                {
                    Name = "Accordion",
                    Path = "accordion",
                    Description = "Demonstration and configuration of the Blazor RadzenAccordion component.",
                    Icon = "&#xe8ee",
                    Tags = new [] { "panel", "container" }
                },
                new Example()
                {
                    Name = "BreadCrumb",
                    Description = "The Blazor RadzenBreadCrumb component provides a navigation trail to help users keep track of their location.",
                    Path = "breadcrumb",
                    Icon = "&#xeac9",
                    Tags = new [] { "breadcrumb", "navigation", "menu" }
                },
                new Example()
                {
                    Name = "ContextMenu",
                    Description = "Demonstration and configuration of the Radzen Blazor Context Menu component.",
                    Path = "contextmenu",
                    Icon = "&#xe8de",
                    Tags = new [] { "popup", "dropdown", "menu" }
                },
                new Example()
                {
                    Name = "Link",
                    Description = "Demonstration and configuration of the Blazor RadzenLink component. Use Path and Target properties to specify Link component navigation.",
                    Path = "link",
                    Icon = "&#xe157"
                },
                new Example()
                {
                    Name = "Login",
                    Description = "Demonstration and configuration of the Blazor RadzenLogin component.",
                    Path = "login",
                    Icon = "&#xe8e8"
                },
                new Example()
                {
                    Name = "Menu",
                    Description = "Demonstration and configuration of the Blazor RadzenMenu component.",
                    Path = "menu",
                    Icon = "&#xe91a",
                    Tags = new [] { "navigation", "dropdown" }
                },
                new Example()
                {
                    Name = "PanelMenu",
                    Path = "panelmenu",
                    Updated = true,
                    Description = "Demonstration and configuration of the Blazor RadzenPanelMenu component.",
                    Icon = "&#xe8d2",
                    Tags = new [] { "navigation", "menu" }
                },
                new Example()
                {
                    Name = "ProfileMenu",
                    Description = "Demonstration and configuration of the Blazor RadzenProfileMenu component.",
                    Path = "profile-menu",
                    Icon = "&#xe851",
                    Tags = new [] { "navigation", "dropdown", "menu" }
                },
                new Example()
                {
                    Name = "Steps",
                    Description = "Use Radzen Blazor Steps component to guide users through a process or sequence of actions. The component consists of a series of numbered steps that represent the various stages of the process.",
                    Path = "steps",
                    Icon = "&#xe044",
                    Tags = new [] { "step", "steps", "wizard" }
                },
                new Example()
                {
                    Name = "Tabs",
                    Description = "Demonstration and configuration of the Radzen Blazor Tabs component.",
                    Path = "tabs",
                    Icon = "&#xe8d8",
                    Tags = new [] { "tabstrip", "tabview", "container" }
                }
            }
        },
        new Example()
        {
            Name = "Forms",
            Icon = "&#xf1c1",
            Children = new[] {
                new Example()
                {
                    Name = "AutoComplete",
                    Path = "autocomplete",
                    Description = "Demonstration and configuration of the Radzen Blazor AutoComplete textbox component.",
                    Icon = "&#xe03b",
                    Tags = new [] { "form", "complete", "suggest", "edit" }
                },
                new Example()
                {
                    Name = "Button",
                    Description = "Demonstration and configuration of the RadzenButton Blazor component.",
                    Path = "button",
                    Icon = "&#xf1c1"
                },
                new Example()
                {
                    Name = "ToggleButton",
                    Description = "Radzen Blazor ToggleButton is a button that changes its appearance or color when activated and returns to its original state when deactivated.",
                    Path = "toggle-button",
                    Icon = "&#xe8e0",
                    Tags = new [] { "button", "switch", "toggle" }
                },
                new Example()
                {
                    Name = "CheckBox",
                    Path = "checkbox",
                    Description = "Demonstration and configuration of the Radzen Blazor CheckBox component with optional tri-state support.",
                    Icon = "&#xe86c",
                    Tags = new [] { "form", "edit" }
                },
                new Example()
                {
                    Name = "CheckBoxList",
                    Path = "checkboxlist",
                    Description = "Demonstration and configuration of the Radzen Blazor CheckBoxList component.",
                    Icon = "&#xe065",
                    Tags = new [] { "form", "edit" }
                },
                new Example()
                {
                    Name = "ColorPicker",
                    Description = "Demonstration and configuration of the Radzen Blazor ColorPicker component. HSV Picker. RGBA Picker.",
                    Path = "colorpicker",
                    Icon = "&#xe40a",
                    Tags = new [] { "form", "edit" }
                },
                new Example()
                {
                    Name = "DatePicker",
                    Path = "datepicker",
                    Updated = true,
                    Description = "Demonstration and configuration of the Radzen Blazor Datepicker component with calendar mode.",
                    Icon = "&#xe916",
                    Tags = new [] { "calendar", "form", "edit" }
                },
                new Example()
                {
                    Name = "DropDown",
                    Icon = "&#xe875",
                    Children = new [] {
                        new Example()
                        {
                            Name = "Single selection",
                            Path = "dropdown",
                            Title = "Blazor DropDown Component | Free UI Components by Radzen",
                            Description = "Demonstration and configuration of the Radzen Blazor DropDown component.",
                            Tags = new [] { "select", "picker", "form" , "edit", "dropdown" },
                        },
                        new Example()
                        {
                            Name = "Multiple selection",
                            Path = "dropdown-multiple",
                            Title = "Blazor DropDown Component - Multiple Selection | Free UI Components by Radzen",
                            Description = "This example demonstrates multiple selection support in Radzen Blazor DropDown component.",
                            Tags = new [] { "select", "picker", "form" , "edit", "multiple", "dropdown" },
                        },
                        new Example()
                        {
                            Name = "Virtualization",
                            Path = "dropdown-virtualization",
                            Title = "Blazor DropDown Component - Virtualization | Free UI Components by Radzen",
                            Description = "This example demonstrates virtualization using IQueryable.",
                            Tags = new [] { "select", "picker", "form" , "edit", "multiple", "dropdown", "virtualization", "paging" },
                        },
                        new Example()
                        {
                            Name = "Filtering",
                            Path = "dropdown-filtering",
                            Title = "Blazor DropDown Component - Filtering | Free UI Components by Radzen",
                            Description = "This example demonstrates Blazor DropDown component filtering case sensitivity and filter operator.",
                            Tags = new [] { "select", "picker", "form" , "edit", "multiple", "dropdown", "filter" },
                        },
                        new Example()
                        {
                            Name = "Grouping",
                            Path = "dropdown-grouping",
                            Title = "Blazor DropDown Component - Grouping | Free UI Components by Radzen",
                            Description = "This example demonstrates Blazor DropDown component with grouping.",
                            Tags = new [] { "select", "picker", "form" , "edit", "multiple", "dropdown", "grouping" },
                        },
                        new Example()
                        {
                            Name = "Custom objects binding",
                            Path = "dropdown-custom-objects",
                            Title = "Blazor DropDown Component - Custom Objects Binding | Free UI Components by Radzen",
                            Description = "This example demonstrates Blazor DropDown component binding to custom objects.",
                            Tags = new [] { "select", "picker", "form" , "edit", "dropdown", "custom" },
                        },
                    }
                },
                new Example()
                {
                    Name = "DropDownDataGrid",
                    Path = "dropdown-datagrid",
                    Description = "Blazor DropDown component with columns and multiple selection support.",
                    Icon = "&#xe8b0",
                    Tags = new [] { "select", "picker", "form", "edit" }
                },
                new Example()
                {
                    Name = "Fieldset",
                    Path = "fieldset",
                    Description = "Demonstration and configuration of the Radzen Blazor Fieldset component.",
                    Icon = "&#xe850",
                    Tags = new [] { "form", "container" }
                },
                new Example()
                {
                    Name = "FileInput",
                    Path = "fileinput",
                    Description = "Blazor File input component with preview support.",
                    Icon = "&#xe226",
                    Tags = new [] { "upload", "form", "edit" }
                },
                new Example()
                {
                    Name = "FormField",
                    Path = "form-field",
                    Description = "Radzen Blazor FormField component features a floating label effect. When the user focuses on an empty input field, the label floats above, providing a visual cue as to which field is being filled out.",
                    Icon = "&#xe578",
                    Tags = new [] { "form", "label", "floating", "float", "edit", "outline", "input", "helper", "valid" }
                },
                new Example()
                {
                    Name="HtmlEditor",
                    Icon = "&#xe3c9",
                    Children = new [] {
                        new Example()
                        {
                            Name = "Default Tools",
                            Path = "html-editor",
                            Title = "Blazor HTML Editor Component | Free UI Components by Radzen",
                            Description = "Blazor HTML editor component with lots of built-in tools.",
                            Tags = new [] { "html", "editor", "rich", "text" }
                        },
                        new Example()
                        {
                            Name = "Custom Tools",
                            Path = "html-editor-custom-tools",
                            Title = "Blazor HTML Editor Component - Custom Tools | Free UI Components by Radzen",
                            Description = "This example demonstrates Blazor HTML editor component with custom tools. RadzenHtmlEditor allows the developer to create custom tools via the RadzenHtmlEditorCustomTool tag.",
                            Tags = new [] { "html", "editor", "rich", "text", "tool", "custom" }
                        },
                    }
                },
                new Example()
                {
                    Name = "ListBox",
                    Path = "listbox",
                    Icon = "&#xe8ef",
                    Description = "Demonstration and configuration of the Radzen Blazor ListBox component.",
                    Tags = new [] { "select", "picker", "form", "edit" }
                },
                new Example()
                {
                    Name = "Mask",
                    Path = "mask",
                    Description = "Demonstration and configuration of the Radzen Blazor masked textbox component.",
                    Icon = "&#xe262",
                    Tags = new [] { "input", "form", "edit", "mask" }
                },
                new Example()
                {
                    Name = "Numeric",
                    Path = "numeric",
                    Description = "Demonstration and configuration of the Radzen Blazor numeric textbox component.",
                    Icon = "&#xe85b",
                    Tags = new [] { "input", "number", "form", "edit" }
                },
                new Example()
                {
                    Name = "Password",
                    Path = "password",
                    Description = "Demonstration and configuration of the Radzen Blazor password textbox component.",
                    Icon = "&#xf042",
                    Tags = new [] { "input", "form", "edit" }
                },
                new Example()
                {
                    Name = "RadioButtonList",
                    Path = "radiobuttonlist",
                    Description = "Demonstration and configuration of the Radzen Blazor radio button list component.",
                    Icon = "&#xe837",
                    Tags = new [] { "toggle", "form", "edit" }
                },
                new Example()
                {
                    Name = "Rating",
                    Path = "rating",
                    Description = "Demonstration and configuration of the Radzen Blazor Rating component.",
                    Icon = "&#xe839",
                    Tags = new [] { "star", "form", "edit" }
                },
                new Example()
                {
                    Name = "SecurityCode",
                    Path = "security-code",
                    Description = "Demonstration and configuration of the Radzen Blazor SecurityCode component.",
                    Icon = "&#xf045",
                    New = true,
                    Tags = new [] { "security", "code", "input" }
                },
                new Example()
                {
                    Name = "SelectBar",
                    Path = "selectbar",
                    Description = "Demonstration and configuration of the Radzen Blazor SelectBar component.",
                    Icon = "&#xe86d",
                    Tags = new [] { "form", "edit" }
                },
                new Example()
                {
                    Name = "Slider",
                    Path = "slider",
                    Description = "Demonstration and configuration of the Radzen Blazor Slider component.",
                    Icon = "&#xe260",
                    Tags = new [] { "form", "slider" }
                },
                new Example()
                {
                    Name = "SpeechToTextButton",
                    Description = "Demonstration and configuration of the Radzen Blazor speech to text button component.",
                    Path = "speechtotextbutton",
                    Icon = "&#xe029"
                },
                new Example()
                {
                    Name = "SplitButton",
                    Description = "Demonstration and configuration of the Radzen Blazor split button component",
                    Path = "splitbutton",
                    Icon = "&#xe05f"
                },
                new Example()
                {
                    Name = "Switch",
                    Path = "switch",
                    Description = "Demonstration and configuration of the Radzen Blazor Switch component.",
                    Icon = "&#xe9f6",
                    Tags = new [] { "form", "edit", "switch" }
                },
                new Example()
                {
                    Name = "TemplateForm",
                    Path = "templateform",
                    Description = "Demonstration and configuration of the Radzen Blazor template form component with validation support.",
                    Icon = "&#xe06d",
                    Tags = new [] { "form", "edit" }
                },
                new Example()
                {
                    Name = "TextArea",
                    Path = "textarea",
                    Description = "Demonstration and configuration of the Radzen Blazor TextArea component.",
                    Icon = "&#xe873",
                    Tags = new [] { "input", "form", "edit" }
                },
                new Example()
                {
                    Name = "TextBox",
                    Path = "textbox",
                    Description = "Demonstration and configuration of the Radzen Blazor TextBox input component.",
                    Icon = "&#xe890",
                    Tags = new [] { "input", "form", "edit" }
                },
                new Example()
                {
                    Name = "Upload",
                    Description = "Demonstration and configuration of the Radzen Blazor Upload component.",
                    Path = "example-upload",
                    Icon = "&#xe2c6",
                    Tags = new [] { "upload", "file"}
                },
            },
        },
        new Example
        {
            Name = "Data Visualization",
            Icon = "&#xe4fb",
            Children = new[] {
                new Example
                {
                    Name="Chart",
                    Icon = "&#xe922",
                    Children = new [] {
                        new Example
                        {
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
                            Name = "Axis",
                            Path = "chart-axis",
                            Title = "Blazor Chart Component - Axis Configuration | Free UI Components by Radzen",
                            Description = "By default the Radzen Blazor Chart determines the Y axis minimum and maximum based on the range of values.",
                            Tags = new [] { "chart", "graph", "series" }
                        },
                        new Example
                        {
                            Name = "Legend",
                            Path = "chart-legend",
                            Title = "Blazor Chart Component - Legend Configuration | Free UI Components by Radzen",
                            Description = "The Radzen Blazor Chart displays a legend by default. It uses the Title property of the series (or category values for pie series) as items in the legend.",
                            Tags = new [] { "chart", "graph", "legend" }
                        },
                        new Example
                        {
                            Name = "ToolTip",
                            Path = "chart-tooltip",
                            Title = "Blazor Chart Component - ToolTip Configuration | Free UI Components by Radzen",
                            Description = "The Radzen Blazor Chart displays a tooltip when the user hovers series with the mouse. The tooltip by default includes the series category, value and series name.",
                            Tags = new [] { "chart", "graph", "legend" }
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
                    Name = "Arc Gauge",
                    Path = "arc-gauge",
                    Description = "Demonstration and configuration of Radzen Blazor Arc Gauge component.",
                    Icon = "&#xe3fc",
                    Tags = new [] { "gauge", "graph", "arc", "progress" }
                },
                new Example
                {
                    Name = "Radial Gauge",
                    Path = "radial-gauge",
                    Description = "Demonstration and configuration of Radzen Blazor Radial Gauge component.",
                    Icon = "&#xe01b",
                    Tags = new [] { "gauge", "graph", "radial", "circle" }
                },
                new Example
                {
                    Name = "Styling Gauge",
                    Path = "styling-gauge",
                    Title = "Blazor Gauge Component - Styling | Free UI Components by Radzen",
                    Description = "This example demonstrates multiple pointers with RadzenRadialGauge and multiple scales with RadzenArcGauge component.",
                    Icon = "&#xe41d",
                    Tags = new [] { "gauge", "graph", "styling" }
                },
                new Example
                {
                    Name = "Timeline",
                    Path = "timeline",
                    Description = "Demonstration and configuration of Radzen Blazor Timeline component. RadzenTimeline component is a graphical representation used to display a chronological sequence of events or data points.",
                    Icon = "&#xe00d",
                    Tags = new [] { "timeline", "time", "line" }
                },
                new Example()
                {
                    Name = "GoogleMap",
                    Path = "googlemap",
                    Description = "Demonstration and configuration of Radzen Blazor Google Map component.",
                    Icon = "&#xe55b"
                },
            }
        },
        new Example()
        {
            Name = "Feedback",
            Icon = "&#xe0cb",
            Children = new[] {
                new Example()
                {
                    Name = "Badge",
                    Path = "badge",
                    Description = "The Radzen Blazor Badge component is a small graphic that displays important information, like a count or label, within a user interface. It's commonly used to draw attention to something or provide visual feedback to the user.",
                    Icon = "&#xea67",
                    Tags = new[] { "badge", "link"}
                },
                new Example()
                {
                    Name = "Notification",
                    Path = "notification",
                    Updated = true,
                    Description = "Demonstration and configuration of the Radzen Blazor Notification component.",
                    Icon = "&#xe85a",
                    Tags = new [] { "message", "notification" }
                },
                new Example()
                {
                    Name = "Alert",
                    Title = "Blazor Alert component",
                    Icon = "&#xe88e",
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
                new Example()
                {
                    Name = "ProgressBar",
                    Description = "Demonstration and configuration of the Radzen Blazor ProgressBar component.",
                    Path = "progressbar",
                    Icon = "&#xe893",
                    Tags = new [] { "progress", "spinner", "bar", "linear" }
                },
                new Example()
                {
                    Name = "ProgressBarCircular",
                    Description = "Demonstration and configuration of the Radzen Blazor circular progress bar component.",
                    Path = "progressbarcircular",
                    Icon = "&#xe5d5",
                    Tags = new [] { "progress", "spinner", "circle", "circular" }
                },
                new Example()
                {
                    Name = "Tooltip",
                    Description = "The Radzen Blazor Tooltip component is a small pop-up box that appears when the user hovers or clicks on a UI element. It is commonly used to provide additional information or context to the user.",
                    Path = "tooltip",
                    Icon = "&#xe8cd",
                    Tags = new [] { "popup", "tooltip" }
                },
            }
        },
        new Example()
        {
            Name = "Validators",
            Icon = "&#xf1c2",
            Children = new[] {
                new Example()
                {
                    Name = "CompareValidator",
                    Path = "comparevalidator",
                    Description = "The Blazor RadzenCompareValidator compares the user input against a predefined value or another component.",
                    Icon = "&#xe877",
                    Tags = new [] { "validator", "validation", "required", "compare"}
                },
                new Example()
                {
                    Name = "DataAnnotationValidator",
                    Path = "dataannotationvalidator",
                    Description = "Demonstration and configuration of the Radzen Blazor Data Annotation Validator component.",
                    Icon = "&#xe6b3",
                    Tags = new [] { "validator", "validation", "pattern", "annotations" }
                },
                new Example()
                {
                    Name = "EmailValidator",
                    Path = "emailvalidator",
                    Description = "Demonstration and configuration of the Radzen Blazor Email Validator component.",
                    Icon = "&#xe0be",
                    Tags = new [] { "validator", "validation", "required", "email"}
                },
                new Example()
                {
                    Name = "LengthValidator",
                    Path = "lengthvalidator",
                    Description = "Demonstration and configuration of the Radzen Blazor Length Validator component.",
                    Icon = "&#xe915",
                    Tags = new [] { "validator", "validation", "required", "length"}
                } ,
                new Example()
                {
                    Name = "NumericRangeValidator",
                    Path = "numericrangevalidator",
                    Description = "Demonstration and configuration of the Radzen Blazor Numeric Range Validator component.",
                    Icon = "&#xe3d0",
                    Tags = new [] { "validator", "validation", "required", "range"}
                },
                new Example()
                {
                    Name = "RegexValidator",
                    Path = "regexvalidator",
                    Description = "Demonstration and configuration of the Radzen Blazor Regex Validator component.",
                    Icon = "&#xe53f",
                    Tags = new [] { "validator", "validation", "pattern", "regex", "regular", "expression"}
                },
                new Example()
                {
                    Name = "RequiredValidator",
                    Path = "requiredvalidator",
                    Description = "Demonstration and configuration of the Radzen Blazor Required Validator component.",
                    Icon = "&#xe5ca",
                    Tags = new [] { "validator", "validation", "required"}
                },
                new Example()
                {
                    Name = "CustomValidator",
                    Path = "customvalidator",
                    Description = "Demonstration and configuration of the Radzen Blazor Custom Validator component.",
                    Icon = "&#xe6b1",
                    Tags = new [] { "validator", "validation", "custom", "unique"}
                },
            }
        },
        new Example()
        {
            Name = "V5 Changelog",
            Path = "/changelog",
            New = true,
            Title = "Track and review changes to Radzen Blazor Components v5",
            Description = "See what's new in Radzen Blazor Components v5",
            Icon = "&#xe8e1"
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

            return "Free Blazor Components | 70+ UI controls by Radzen";
        }

        public string DescriptionFor(Example example)
        {
            return example?.Description ?? "The Radzen Blazor component library provides more than 70 UI controls for building rich ASP.NET Core web applications.";
        }
    }
}
