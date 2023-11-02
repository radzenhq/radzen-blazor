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
            Title = "Rich dasboard created with the Radzen Blazor components",
            Icon = "&#xe871"
        },
        new Example
        {
            Name = "Get Started",
            Title = "How to get started with the Radzen Blazor components",
            Path = "/get-started",
            Icon = "&#xe037"
        },
        new Example
        {
            Name = "Support",
            Title = "How to get support for the Radzen Blazor components",
            Path = "/support",
            Icon = "&#xe94c"
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
                    Title = "Blazor Themes",
                    Icon = "&#xe40a",
                    Tags = new[] { "theme", "color", "background", "border", "utility", "css", "var"}
                },
                new Example()
                {
                    Name = "Colors",
                    Path = "colors",
                    Title = "Blazor Theme Colors",
                    Icon = "&#xe891",
                    Tags = new[] { "color", "background", "border", "utility", "css", "var"}
                },
                new Example()
                {
                    Name = "Typography",
                    Path = "typography",
                    Title = "Blazor Text component",
                    Icon = "&#xe264",
                    Tags = new [] { "typo", "typography", "text", "paragraph", "header", "heading", "caption", "overline", "content" }
                },
                new Example()
                {
                    Name = "Icons",
                    Title = "Blazor Icon component",
                    Path = "icon",
                    Icon = "&#xe148",
                    Tags = new [] { "icon", "content" }
                },
                new Example()
                {
                    Name = "Borders",
                    Path = "borders",
                    Title = "Blazor Border styles",
                    Icon = "&#xe3c6",
                    Tags = new [] { "border", "utility", "css", "var"}
                },
                new Example()
                {
                    Name = "Shadows",
                    Path = "shadows",
                    Title = "Blazor Shadow styles",
                    Icon = "&#xe595",
                    Tags = new [] { "shadow", "utility", "css", "var"}
                },
                new Example()
                {
                    Name = "Ripple",
                    Title = "Blazor Ripple effect",
                    Path = "ripple",
                    Icon = "&#xe39e",
                    Tags = new [] { "ripple", "utility", "css", "var"}
                },
                new Example()
                {
                    Name = "Breakpoints",
                    Title = "Blazor Responsive Breakpoints",
                    Path = "breakpoints",
                    Icon = "&#xe1b1",
                    Tags = new [] { "breakpoints", "spacing", "margin", "padding", "gutter", "gap", "utility", "css", "responsive", "layout"}
                },
                new Example()
                {
                    Name = "Spacing",
                    Title = "Blazor Spacing styles",
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
                            Title = "Blazor DataGrid commponent with code-less paging, sorting and filtering of IQueryable data sources",
                            Path = "datagrid",
                            Tags = new [] { "datatable", "datagridview", "dataview", "grid", "table" }
                        },
                        new Example
                        {
                            Name = "LoadData event",
                            Path = "datagrid-loaddata",
                            Title = "Blazor DataGrid custom data-binding via the LoadData event",
                            Tags = new [] { "datagrid", "bind", "load", "data", "loaddata", "custom" }
                        },
                        new Example
                        {
                            Name = "OData service",
                            Path = "datagrid-odata",
                            Title = "Blazor DataGrid supports data-binding to OData",
                            Tags = new [] { "datagrid", "bind", "load", "data", "loaddata", "odata", "service", "rest" }
                        },
                        new Example
                        {
                            Name = "Dynamic data",
                            Path = "datagrid-dynamic",
                            Title = "Blazor DataGrid supports dynamic data sources",
                            Tags = new [] { "datagrid", "bind", "load", "data", "loaddata", "dynamic" }
                        },
                        new Example
                        {
                            Name = "Performance",
                            Path = "datagrid-performance",
                            Title = "Blazor DataGrid bound to large collection of data",
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
                            Title = "Blazor DataGrid IQueryable virtualization",
                            Tags = new [] { "datagrid", "bind", "load", "data", "virtualization", "ondemand" }
                        },
                        new Example
                        {
                            Name = "LoadData support",
                            Path = "datagrid-virtualization-loaddata",
                            Title = "Blazor DataGrid custom virtualization",
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
                            Title = "Blazor DataGrid custom appearance via column templates",
                            Tags = new [] { "column", "template", "grid", "datagrid", "table"}
                        },
                        new Example
                        {
                            Name = "Resizing",
                            Path = "datagrid-column-resizing",
                            Title = "Blazor DataGrid column resizing",
                            Tags = new [] { "column", "resizing", "grid", "datagrid", "table"}
                        },
                        new Example
                        {
                            Name = "Column Picker",
                            New = true,
                            Path = "datagrid-column-picker",
                            Title = "Blazor DataGrid column picker",
                            Tags = new [] { "datagrid", "column", "picker", "chooser" }
                        },
                        new Example
                        {
                            Updated = true,
                            Name = "Reorder",
                            Path = "datagrid-column-reorder",
                            Title = "Blazor DataGrid column reorder",
                            Tags = new [] { "column", "reorder", "grid", "datagrid", "table"}
                        },
                        new Example
                        {
                            Name = "Footer Totals",
                            Path = "datagrid-footer-totals",
                            Title = "Blazor DataGrid footer totals",
                            Tags = new [] { "summary", "total", "aggregate", "datagrid", "table", "dataview" }
                        },
                        new Example
                        {
                            Name = "Filter Template",
                            Path = "datagrid-filter-template",
                            Title = "Blazor DataGrid custom filtering",
                            Tags = new [] { "datagrid", "column", "filter", "template" }
                        },
                        new Example
                        {
                            Name = "Frozen Columns",
                            Path = "datagrid-frozen-columns",
                            Title = "Blazor DataGrid frozen columns",
                            Tags = new [] { "datagrid", "column", "frozen", "locked" }
                        },
                        new Example
                        {
                            Name = "Composite Columns",
                            New = true,
                            Path = "datagrid-composite-columns",
                            Title = "Blazor DataGrid composite columns",
                            Tags = new [] { "datagrid", "column", "composite", "merged", "complex" }
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
                            Title = "Blazor DataGrid Simple filter mode",
                            Tags = new [] { "filter", "simple", "grid", "datagrid", "table"}
                        },
                        new Example
                        {
                            New = true,
                            Name = "Simple with menu",
                            Path = "datagrid-simple-filter-menu",
                            Title = "Blazor DataGrid Simple filter mode with menu",
                            Tags = new [] { "filter", "simple", "grid", "datagrid", "table", "menu" }
                        },
                        new Example
                        {
                            Name = "Advanced Mode",
                            Path = "datagrid-advanced-filter",
                            Title = "Blazor DataGrid Simple filter mode",
                            Tags = new [] { "filter", "advanced", "grid", "datagrid", "table"}
                        },
                        new Example
                        {
                            New = true,
                            Name = "Enum filtering",
                            Path = "datagrid-enum-filter",
                            Title = "Blazor DataGrid enum filtering",
                            Tags = new [] { "filter", "enum", "grid", "datagrid", "table"}
                        },
                        new Example
                        {
                            Name = "Filter API",
                            Path = "datagrid-filter-api",
                            Title = "Blazor DataGrid Filter API",
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
                            Title = "Blazor DataGrid Hierarchy",
                            Tags = new [] { "master", "detail", "datagrid", "table", "dataview" }
                        },
                        new Example
                        {
                            Name = "Hierarchy on demand",
                            Path = "master-detail-hierarchy-demand",
                            Title = "Blazor DataGrid hierarchy on demand",
                            Tags = new [] { "master", "detail", "datagrid", "table", "dataview", "on-demand" }
                        },
                        new Example
                        {
                            Name = "Self-reference hierarchy",
                            New = true,
                            Path = "datagrid-selfref-hierarchy",
                            Title = "Blazor DataGrid self-reference hierarchy",
                            Tags = new [] { "master", "detail", "datagrid", "table", "dataview", "hierarchy", "self-reference" }
                        },
                        new Example
                        {
                            Name = "Master/Detail",
                            Path = "master-detail",
                            Title = "Master and detail Blazor DataGrid",
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
                            Title = "Blazor DataGrid single selection",
                            Tags = new [] { "single", "selection", "datagrid", "table", "dataview" }
                        },
                        new Example
                        {
                            Updated = true,
                            Name = "Multiple selection",
                            Path = "datagrid-multiple-selection",
                            Title = "Blazor DataGrid multiple selection",
                            Tags = new [] { "multiple", "selection", "datagrid", "table", "dataview" }
                        },
                        new Example
                        {
                            New = true,
                            Name = "Cell selection",
                            Path = "datagrid-cell-selection",
                            Title = "Blazor DataGrid cell selection",
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
                            Title = "Blazor DataGrid sorting",
                            Tags = new [] { "single", "sort", "datagrid", "table", "dataview" }
                        },
                        new Example
                        {
                            Name = "Multiple Column Sorting",
                            Path = "datagrid-multi-sort",
                            Title = "Blazor DataGrid sorting by multiple columns",
                            Tags = new [] { "multi", "sort", "datagrid", "table", "dataview" }
                        },
                        new Example
                        {
                            Name = "Sort API",
                            Path = "datagrid-sort-api",
                            Title = "Blazor DataGrid Sort API",
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
                            Title = "Blazor DataGrid pager position",
                            Tags = new [] { "pager", "paging", "datagrid", "table", "dataview" }
                        },
                        new Example
                        {
                            Name = "Pager Horizontal Align",
                            New = true,
                            Path = "datagrid-pager-horizontal-align",
                            Title = "Blazor DataGrid pager horizontal align",
                            Tags = new [] { "pager", "paging", "datagrid", "table", "dataview" }
                        },
                        new Example
                        {
                            Name = "Pager API",
                            Path = "datagrid-pager-api",
                            Title = "Blazor DataGrid pager API",
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
                            Title = "Blazor DataGrid grouping API",
                            Tags = new [] { "group", "grouping", "datagrid", "table", "dataview", "api" }
                        },
                        new Example
                        {
                            Updated = true,
                            Name = "Group Header Template",
                            Path = "datagrid-group-header-template",
                            Title = "Blazor DataGrid group header template",
                            Tags = new [] { "group", "grouping", "template", "datagrid", "table", "dataview" }
                        },
                        new Example
                        {
                            Name = "Group Footer Template",
                            Path = "datagrid-group-footer-template",
                            Title = "Blazor DataGrid group footer template",
                            Tags = new [] { "group", "grouping", "footer", "template", "datagrid", "table", "dataview" }
                        }
                    }
                },
                new Example()
                {
                    Name = "Density",
                    Path = "datagrid-density",
                    Title = "Blazor DataGrid density",
                    Icon = "&#xeb9e",
                    Tags = new [] { "density", "compact", "small", "large", "tight" }
                },
                new Example()
                {
                    Name="Custom Header",
                    Title = "Blazor DataGrid custom header",
                    Icon = "&#xe051",
                    Children = new [] {
                      new Example
                        {
                            Name = "Header with button",
                            Path="datagrid-custom-header",
                            Title = "Blazor DataGrid grouping API",
                            Tags = new [] { "grid header","header" }
                        },
                      new Example
                        {
                            Name = "Header with column picker",
                            Path="datagrid-custom-header-columnpicker",
                            Title = "Blazor DataGrid grouping API",
                            Tags = new [] { "grid header","header" }
                        }
                    }
                },
                new Example()
                {
                    Name = "GridLines",
                    Path = "datagrid-grid-lines",
                    Title = "Blazor DataGrid grid lines",
                    Icon = "&#xf016",
                    Tags = new [] { "grid", "lines", "border", "gridlines" }
                },
                new Example()
                {
                    Name = "Cell Context Menu",
                    Path = "datagrid-cell-contextmenu",
                    Title = "Blazor DataGrid cell context menu",
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
                            New = true,
                            Name = "IQueryable",
                            Path = "datagrid-save-settings",
                            Title = "Blazor DataGrid save/load settings",
                            Tags = new [] { "save", "load", "settings" }
                        },

                        new Example()
                        {
                            New = true,
                            Name = "LoadData binding",
                            Path = "datagrid-save-settings-loaddata",
                            Title = "Blazor DataGrid save/load settings with LoadData",
                            Tags = new [] { "save", "load", "settings", "async", "loaddata" }
                        }
                    }
                },

                new Example()
                {
                    Name = "InLine Editing",
                    Path = "datagrid-inline-edit",
                    Title = "Blazor DataGrid inline editing",
                    Icon = "&#xe22b",
                    Tags = new [] { "inline", "editor", "datagrid", "table", "dataview" }
                },

                new Example()
                {
                    Name = "Conditional formatting",
                    Path = "datagrid-conditional-template",
                    Title = "Blazor DataGrid conditional template",
                    Icon = "&#xe41d",
                    Tags = new [] { "conditional", "template", "style", "datagrid", "table", "dataview" }
                },
                new Example()
                {
                    Name = "Export to Excel and CSV",
                    Path = "export-excel-csv",
                    Title = "Blazor DataGrid export to Excel and CSV",
                    Icon = "&#xe0c3",
                    Tags = new [] { "export", "excel", "csv" }
                },
                new Example()
                {
                    Name = "Cascading DropDowns",
                    Path = "cascading-dropdowns",
                    Title = "Blazor cascading dropdowns",
                    Icon = "&#xe915",
                    Tags = new [] { "related", "parent", "child" }
                },
                new Example()
                {
                    Name = "Empty Data Grid",
                    Path = "/datagrid-empty",
                    Title = "Blazor DataGrid without data",
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
                            Title = "Blazor data list component",
                            New = true,
                            Path = "datalist",
                            Tags = new [] { "dataview", "grid", "table", "list"},
                        },
                        new Example
                        {
                            Name = "OData service",
                            Title = "Blazor data list with OData",
                            New = true,
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
                            Title = "Blazor data filter component",
                            Path = "datafilter",
                            Tags = new [] { "dataview", "grid", "table", "filter" },
                        },
                        new Example
                        {
                            Name = "LoadData",
                            Title = "Blazor data filter with LoadData",
                            Path = "datafilter-loaddata",
                            Tags = new [] { "dataview", "grid", "table", "filter", "loaddata" },
                        },
                        new Example
                        {
                            Name = "OData service",
                            Title = "Blazor data filter with OData",
                            Path = "datafilter-odata",
                            Tags = new [] { "dataview", "grid", "table", "filter", "odata" },
                        }
                    }
                },
                new Example()
        {
            Name = "Pager",
                    Title = "Blazor paging component",
                    Path = "pager",
                    Icon = "&#xe8be",
                    Tags = new[] { "pager", "paging" }
                },
                new Example()
        {
            Name = "Scheduler",
                    Path = "scheduler",
                    Updated = true,
                    Title = "Blazor scheduler component with daily, weekly and monthly views",
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
                            Title = "Blazor Tree component",
                            Path = "tree",
                            Tags = new [] { "tree", "treeview", "nodes", "inline" },
                        },
                        new Example
                        {
                            Name = "Data-binding",
                            Title = "Blazor Tree data-binding",
                            Path = "tree-data-binding",
                            Tags = new [] { "tree", "treeview", "nodes", "data", "table" },
                        },
                        new Example
                        {
                            Name = "Files and directories",
                            Title = "Blazor Tree data-binding to files and directories",
                            Path = "tree-file-system",
                            Tags = new [] { "tree", "treeview", "nodes", "file", "directory" },
                        },
                        new Example
                        {
                            Name = "Selection",
                            Title = "Blazor Tree selection",
                            Path = "tree-selection",
                            Tags = new [] { "tree", "treeview", "nodes", "selection" },
                        },
                        new Example
                        {
                            Name = "Checkboxes",
                            Title = "Blazor Tree tri-state checkboxes",
                            Path = "tree-checkboxes",
                            Tags = new [] { "tree", "treeview", "nodes", "check" },
                        }
                    }
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
                    Title = "Blazor Gravatar component",
                    Path = "gravatar",
                    Icon = "&#xe420"
                },
                new Example()
                {
                    Name = "Image",
                    Title = "Blazor Image component",
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
                    Title = "Blazor Layout component",
                    Path = "layout",
                    Icon = "&#xe8f1",
                    Tags = new [] { "layout", "sidebar", "drawer", "header", "body", "footer" }
                },
                new Example()
                {
                    Name = "Stack",
                    Title = "Blazor Stack component",
                    Path = "stack",
                    Icon = "&#xe8f2",
                    Tags = new [] { "stack", "layout" }
                },
                new Example()
                {
                    Name = "Row",
                    Title = "Blazor Row component",
                    Path = "row",
                    Icon = "&#xf101",
                    Tags = new [] { "row", "layout", "responsive", "grid" }
                },
                new Example()
                {
                    Name = "Column",
                    Title = "Blazor Column component",
                    Path = "column",
                    Icon = "&#xe8ec",
                    Tags = new [] { "column", "col", "layout", "responsive", "grid" }
                },
                new Example()
                {
                    Name = "Card",
                    Title = "Blazor Card component",
                    Updated = true,
                    Path = "card",
                    Icon = "&#xe919",
                    Tags = new [] { "container" }
                },
                new Example()
                {
                    Name = "Dialog",
                    Title = "Blazor Dialog component",
                    Path = "dialog",
                    Icon = "&#xe8a7",
                    Tags = new [] { "popup", "window" },
                },
                new Example()
                {
                    Name = "Panel",
                    Title = "Blazor Panel component",
                    Path = "panel",
                    Icon = "&#xe14f",
                    Tags = new [] { "container" }
                },
                new Example()
                {
                    Name = "Splitter",
                    Title = "Blazor Splitter component",
                    Path = "splitter",
                    Icon = "&#xe94f",
                    Tags = new [] { "splitter", "layout"}
                },
                new Example()
                {
                    New = true,
                    Name = "Popup",
                    Title = "Blazor Popup component",
                    Path = "popup",
                    Icon = "&#xe0cb",
                    Tags = new [] { "popup", "dropdown"}
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
                    Title = "Blazor Accordion component",
                    Icon = "&#xe8ee",
                    Tags = new [] { "panel", "container" }
                },
                new Example()
                {
                    Name = "BreadCrumb",
                    Title = "Blazor BreadCrumb component",
                    Path = "breadcrumb",
                    Icon = "&#xeac9",
                    Tags = new [] { "breadcrumb", "navigation", "menu" }
                },
                new Example()
                {
                    Name = "ContextMenu",
                    Title = "Blazor Context menu component",
                    Path = "contextmenu",
                    Icon = "&#xe8de",
                    Tags = new [] { "popup", "dropdown", "menu" }
                },
                new Example()
                {
                    Name = "Link",
                    Title = "Blazor Link component",
                    Path = "link",
                    Icon = "&#xe157"
                },
                new Example()
                {
                    Name = "Login",
                    Title = "Blazor Login component",
                    Path = "login",
                    Icon = "&#xe8e8"
                },
                new Example()
                {
                    Name = "Menu",
                    Title = "Blazor Menu component",
                    Path = "menu",
                    Icon = "&#xe91a",
                    Tags = new [] { "navigation", "dropdown" }
                },
                new Example()
                {
                    Name = "PanelMenu",
                    Title = "Blazor PanelMenu component",
                    Path = "panelmenu",
                    Icon = "&#xe8d2",
                    Tags = new [] { "navigation", "menu" }
                },
                new Example()
                {
                    Name = "ProfileMenu",
                    Title = "Blazor ProfileMenu component",
                    Path = "profile-menu",
                    Icon = "&#xe851",
                    Tags = new [] { "navigation", "dropdown", "menu" }
                },
                new Example()
                {
                    Name = "Steps",
                    Title = "Blazor Steps component",
                    Path = "steps",
                    Icon = "&#xe044",
                    Tags = new [] { "step", "steps", "wizard" }
                },
                new Example()
                {
                    Name = "Tabs",
                    Title = "Blazor Tabs component",
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
                    Title = "Blazor AutoComplete textbox component",
                    Icon = "&#xe03b",
                    Tags = new [] { "form", "complete", "suggest", "edit" }
                },
                new Example()
                {
                    Name = "Button",
                    Title = "Blazor Button component",
                    Path = "button",
                    Icon = "&#xf1c1"
                },
                new Example()
                {
                    Name = "ToggleButton",
                    Title = "Blazor ToggleButton component",
                    Path = "toggle-button",
                    Icon = "&#xe8e0",
                    Tags = new [] { "button", "switch", "toggle" }
                },
                new Example()
                {
                    Name = "CheckBox",
                    Path = "checkbox",
                    Title = "Blazor checkbox component with optional tri-state support",
                    Icon = "&#xe86c",
                    Tags = new [] { "form", "edit" }
                },
                new Example()
                {
                    Name = "CheckBoxList",
                    Path = "checkboxlist",
                    Title = "Blazor Checkbox list component",
                    Icon = "&#xe065",
                    Tags = new [] { "form", "edit" }
                },
                new Example()
                {
                    Name = "ColorPicker",
                    Title = "Blazor Colorpicker component",
                    Path = "colorpicker",
                    Icon = "&#xe40a",
                    Tags = new [] { "form", "edit" }
                },
                new Example()
                {
                    Name = "DatePicker",
                    Title = "Blazor Datepicker component with calendar mode",
                    Path = "datepicker",
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
                            Updated = true,
                            Name = "Single selection",
                            Path = "dropdown",
                            Title = "Blazor DropDown component",
                            Tags = new [] { "select", "picker", "form" , "edit", "dropdown" },
                        },
                        new Example()
                        {
                            Updated = true,
                            Name = "Multiple selection",
                            Path = "dropdown-multiple",
                            Title = "Blazor DropDown component with multiple selection support",
                            Tags = new [] { "select", "picker", "form" , "edit", "multiple", "dropdown" },
                        },
                        new Example()
                        {
                            Updated = true,
                            Name = "Virtualization",
                            Path = "dropdown-virtualization",
                            Title = "Blazor DropDown component with virtualization",
                            Tags = new [] { "select", "picker", "form" , "edit", "multiple", "dropdown", "virtualization", "paging" },
                        },
                        new Example()
                        {
                            Updated = true,
                            Name = "Filtering",
                            Path = "dropdown-filtering",
                            Title = "Blazor DropDown component with filtering",
                            Tags = new [] { "select", "picker", "form" , "edit", "multiple", "dropdown", "filter" },
                        },
                        new Example()
                        {
                            Updated = true,
                            Name = "Grouping",
                            Path = "dropdown-grouping",
                            Title = "Blazor DropDown component with grouping",
                            Tags = new [] { "select", "picker", "form" , "edit", "multiple", "dropdown", "grouping" },
                        },
                        new Example()
                        {
                            Updated = true,
                            Name = "Custom objects binding",
                            Path = "dropdown-custom-objects",
                            Title = "Blazor DropDown component binding to custom objects",
                            Tags = new [] { "select", "picker", "form" , "edit", "dropdown", "custom" },
                        },
                    }
                },
                new Example()
                {
                    Name = "DropDownDataGrid",
                    Path = "dropdown-datagrid",
                    Title = "Blazor DropDown component with columns and multiple selection support",
                    Icon = "&#xe8b0",
                    Tags = new [] { "select", "picker", "form", "edit" }
                },
                new Example()
                {
                    Name = "Fieldset",
                    Path = "fieldset",
                    Title = "Blazor fieldset component",
                    Icon = "&#xe850",
                    Tags = new [] { "form", "container" }
                },
                new Example()
                {
                    Name = "FileInput",
                    Path = "fileinput",
                    Title = "Blazor File input component with preview support",
                    Icon = "&#xe226",
                    Tags = new [] { "upload", "form", "edit" }
                },
                new Example()
                {
                    Name = "FormField",
                    Path = "form-field",
                    Title = "Blazor form field component",
                    Icon = "&#xe578",
                    New = true,
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
                            Title = "Blazor HTML editor component with lots of built-in tools",
                            Tags = new [] { "html", "editor", "rich", "text" }
                        },
                        new Example()
                        {
                            Name = "Custom Tools",
                            Path = "html-editor-custom-tools",
                            Title = "Blazor HTML editor component with custom tools",
                            Tags = new [] { "html", "editor", "rich", "text", "tool", "custom" }
                        },
                    }
                },
                new Example()
                {
                    Name = "ListBox",
                    Path = "listbox",
                    Icon = "&#xe8ef",
                    Title = "Blazor listbox component",
                    Tags = new [] { "select", "picker", "form", "edit" }
                },
                new Example()
                {
                    Name = "Mask",
                    Path = "mask",
                    Title = "Blazor masked textbox component",
                    Icon = "&#xe262",
                    Tags = new [] { "input", "form", "edit", "mask" }
                },
                new Example()
                {
                    Name = "Numeric",
                    Path = "numeric",
                    Title = "Blazor numeric textbox component",
                    Icon = "&#xe85b",
                    Tags = new [] { "input", "number", "form", "edit" }
                },
                new Example()
                {
                    Name = "Password",
                    Path = "password",
                    Title = "Blazor password textbox component",
                    Icon = "&#xf042",
                    Tags = new [] { "input", "form", "edit" }
                },
                new Example()
                {
                    Name = "RadioButtonList",
                    Path = "radiobuttonlist",
                    Title = "Blazor radio button list component",
                    Icon = "&#xe837",
                    Tags = new [] { "toggle", "form", "edit" }
                },
                new Example()
                {
                    Name = "Rating",
                    Path = "rating",
                    Title = "Blazor rating component",
                    Icon = "&#xe839",
                    Tags = new [] { "star", "form", "edit" }
                },
                new Example()
                {
                    Name = "SelectBar",
                    Path = "selectbar",
                    Title = "Blazor selectbar component",
                    Icon = "&#xe86d",
                    Tags = new [] { "form", "edit" }
                },
                new Example()
                {
                    Name = "Slider",
                    Path = "slider",
                    Title = "Blazor slider component",
                    Icon = "&#xe260",
                    Tags = new [] { "form", "slider" }
                },
                new Example()
                {
                    Name = "SpeechToTextButton",
                    New = true,
                    Title = "Blazor speech to text button component",
                    Path = "speechtotextbutton",
                    Icon = "&#xe029"
                },
                new Example()
                {
                    Name = "SplitButton",
                    Title = "Blazor split button component",
                    Path = "splitbutton",
                    Icon = "&#xe05f"
                },
                new Example()
                {
                    Name = "Switch",
                    Path = "switch",
                    Title = "Blazor switch component",
                    Icon = "&#xe9f6",
                    Tags = new [] { "form", "edit", "switch" }
                },
                new Example()
                {
                    Name = "TemplateForm",
                    Path = "templateform",
                    Title = "Blazor template form component with validation support",
                    Icon = "&#xe06d",
                    Tags = new [] { "form", "edit" }
                },
                new Example()
                {
                    Name = "TextArea",
                    Path = "textarea",
                    Title = "Blazor textarea component",
                    Icon = "&#xe873",
                    Tags = new [] { "input", "form", "edit" }
                },
                new Example()
                {
                    Name = "TextBox",
                    Path = "textbox",
                    Title = "Blazor textbox component",
                    Icon = "&#xe890",
                    Tags = new [] { "input", "form", "edit" }
                },
                new Example()
                {
                    Name = "Upload",
                    Title = "Blazor upload component",
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
                    Updated = true,
                    Children = new [] {
                        new Example
                        {
                            Name = "Series",
                            Path = "chart-series",
                            Title = "Blazor chart component - Series configuration",
                            Tags = new [] { "chart", "graph", "series" }
                        },
                        new Example
                        {
                            Name = "Area Chart",
                            Path = "area-chart",
                            Title = "Blazor area chart component",
                            Tags = new [] { "chart", "graph", "area" }
                        },
                        new Example
                        {
                            Name = "Bar Chart",
                            Path = "bar-chart",
                            Title = "Blazor bar chart component",
                            Tags = new [] { "chart", "graph", "column", "bar" }
                        },
                        new Example
                        {
                            Name = "Column Chart",
                            Path = "column-chart",
                            Title = "Blazor column chart component",
                            Tags = new [] { "chart", "graph", "column", "bar" }
                        },
                        new Example
                        {
                            Name = "Donut Chart",
                            Path = "donut-chart",
                            Title = "Blazor donut chart component",
                            Tags = new [] { "chart", "graph", "donut" }
                        },
                        new Example
                        {
                            Name = "Line Chart",
                            Path = "line-chart",
                            Title = "Blazor line chart component",
                            Tags = new [] { "chart", "graph", "line" }
                        },
                        new Example
                        {
                            Name = "Pie Chart",
                            Title = "Blazor pie chart component",
                            Path = "pie-chart",
                            Tags = new [] { "chart", "graph", "pie" }
                        },
                        new Example
                        {
                            Name = "Stacked Bar Chart",
                            Path = "stacked-bar-chart",
                            Title = "Blazor stacked bar chart component",
                            Tags = new [] { "chart", "stack", "graph", "column", "bar" }
                        },
                        new Example
                        {
                            Name = "Stacked Column Chart",
                            Path = "stacked-column-chart",
                            Title = "Blazor stacked column chart component",
                            Tags = new [] { "chart", "stack", "graph", "column", "bar" }
                        },
                        new Example
                        {
                            Name = "Axis",
                            Path = "chart-axis",
                            Title = "Blazor chart component - Axis configuration",
                            Tags = new [] { "chart", "graph", "series" }
                        },
                        new Example
                        {
                            Name = "Legend",
                            Path = "chart-legend",
                            Title = "Blazor chart component - Legend configuration",
                            Tags = new [] { "chart", "graph", "legend" }
                        },
                        new Example
                        {
                            Name = "ToolTip",
                            Path = "chart-tooltip",
                            Title = "Blazor chart component - ToolTip configuration",
                            Tags = new [] { "chart", "graph", "legend" }
                        },
                        new Example
                        {
                            Name = "Trends",
                            Path = "chart-trends",
                            Tags = new [] { "chart", "trend", "median", "mean", "mode" }
                        },
                        new Example
                        {
                            Name = "Annotations",
                            Path = "chart-annotations",
                            Tags = new [] { "chart", "annotation", "label" }
                        },
                        new Example
                        {
                            Name = "Interpolation",
                            Path = "chart-interpolation",
                            New = true,
                            Tags = new [] { "chart", "interpolation", "spline", "step" }
                        },
                        new Example
                        {
                            Name = "Styling Chart",
                            Path = "styling-chart",
                            Title = "Blazor chart styling",
                            Tags = new [] { "chart", "graph", "styling" }
                        },
                    }
                },
                new Example
                {
                    Name = "Arc Gauge",
                    Path = "arc-gauge",
                    Title = "Blazor arc gauge component",
                    Icon = "&#xe3fc",
                    Tags = new [] { "gauge", "graph", "arc", "progress" }
                },
                new Example
                {
                    Name = "Radial Gauge",
                    Path = "radial-gauge",
                    Title = "Blazor radial gauge component",
                    Icon = "&#xe01b",
                    Tags = new [] { "gauge", "graph", "radial", "circle" }
                },
                new Example
                {
                    Name = "Styling Gauge",
                    Path = "styling-gauge",
                    Title = "Blazor gauge styling",
                    Icon = "&#xe41d",
                    Tags = new [] { "gauge", "graph", "styling" }
                },
                new Example
                {
                    Name = "Timeline",
                    New = true,
                    Path = "timeline",
                    Title = "Blazor Timeline component",
                    Icon = "&#xe00d",
                    Tags = new [] { "timeline", "time", "line" }
                },
                new Example()
                {
                    Name = "GoogleMap",
                    Path = "googlemap",
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
                    Title = "Blazor Badge component",
                    Icon = "&#xea67",
                    Tags = new[] { "badge", "link"}
                },
                new Example()
                {
                    Name = "Notification",
                    Path = "notification",
                    Title = "Blazor Notification component",
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
                            Title = "Blazor Alert component",
                            Path = "alert",
                            Tags = new [] { "message", "alert" },
                        },
                        new Example
                        {
                            Name = "Alert Styling",
                            Title = "Blazor Alert styling",
                            Path = "alert-styling",
                            Tags = new [] { "message", "alert" },
                        }
                    }
                },
                new Example()
                {
                    Name = "ProgressBar",
                    Updated = true,
                    Title = "Blazor progress bar component",
                    Path = "progressbar",
                    Icon = "&#xe893",
                    Tags = new [] { "progress", "spinner", "bar", "linear" }
                },
                new Example()
                {
                    Name = "ProgressBarCircular",
                    New = true,
                    Title = "Blazor circular progress bar component",
                    Path = "progressbarcircular",
                    Icon = "&#xe5d5",
                    Tags = new [] { "progress", "spinner", "circle", "circular" }
                },
                new Example()
                {
                    Name = "Tooltip",
                    Title = "Blazor tooltip component",
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
                    Title = "Blazor compare validator component",
                    Icon = "&#xe877",
                    Tags = new [] { "validator", "validation", "required", "compare"}
                },
                new Example()
                {
                    Name = "EmailValidator",
                    Path = "emailvalidator",
                    Title = "Blazor email validator component",
                    Icon = "&#xe0be",
                    Tags = new [] { "validator", "validation", "required", "email"}
                },
                new Example()
                {
                    Name = "LengthValidator",
                    Path = "lengthvalidator",
                    Title = "Blazor length validator component",
                    Icon = "&#xe915",
                    Tags = new [] { "validator", "validation", "required", "length"}
                } ,
                new Example()
                {
                    Name = "NumericRangeValidator",
                    Path = "numericrangevalidator",
                    Title = "Blazor range validator component",
                    Icon = "&#xe3d0",
                    Tags = new [] { "validator", "validation", "required", "range"}
                },
                new Example()
                {
                    Name = "RegexValidator",
                    Path = "regexvalidator",
                    Title = "Blazor regex validator component",
                    Icon = "&#xe53f",
                    Tags = new [] { "validator", "validation", "pattern", "regex", "regular", "expression"}
                },
                new Example()
                {
                    Name = "RequiredValidator",
                    Path = "requiredvalidator",
                    Title = "Blazor required validator component",
                    Icon = "&#xe5ca",
                    Tags = new [] { "validator", "validation", "required"}
                },
                new Example()
                {
                    Name = "CustomValidator",
                    New = true,
                    Path = "customvalidator",
                    Title = "Blazor custom validator component",
                    Icon = "&#xe6b1",
                    Tags = new [] { "validator", "validation", "custom", "unique"}
                },
            }
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
                return example.Title ?? $"Blazor {example.Name} | a free UI component by Radzen";
            }

            return "Free Blazor Components | 70+ controls by Radzen";
        }

        public string DescriptionFor(Example example)
        {
            return example?.Description ?? "The Radzen Blazor component library provides more than 70 UI controls for building rich ASP.NET Core web applications.";
        }
    }
}
