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
            Name = "First Look",
            Path = "/",
            Icon = "&#xe88a"
        },
        new Example()
        {
            Name = "Dashboard",
            Path = "/dashboard",
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
            Name="DataGrid",
            Children = new [] {
                new Example
                {
                    Name = "Data-binding",
                    Icon = "&#xe3ec",
                    Children = new [] {
                        new Example
                        {
                            Name = "IQueryable",
                            Path = "datagrid",
                            Tags = new [] { "datatable", "datagridview", "dataview", "grid", "table" }
                        },
                        new Example
                        {
                            Name = "LoadData event",
                            Path = "datagrid-loaddata",
                            Title = "Blazor DataGrid custom data-binding",
                            Tags = new [] { "datagrid", "bind", "load", "data", "loaddata", "custom" }
                        },
                        new Example
                        {
                            Name = "OData service",
                            Path = "datagrid-odata",
                            Title = "Blazor DataGrid OData data-binding",
                            Icon = "&#xe871",
                            Tags = new [] { "datagrid", "bind", "load", "data", "loaddata", "odata", "service", "rest" }
                        },
                        new Example
                        {
                            Name = "Dynamic data",
                            Path = "datagrid-dynamic",
                            Title = "Blazor DataGrid binding dynamic data",
                            Icon = "&#xe871",
                            Tags = new [] { "datagrid", "bind", "load", "data", "loaddata", "dynamic" }
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
                    Children = new []
                    {
                        new Example
                        {
                            Name = "Template",
                            Path = "datagrid-column-template",
                            Title = "Blazor DataGrid column template",
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
                        }
                    }
                },
                new Example
                {
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
                            Name = "Advanced Mode",
                            Path = "datagrid-advanced-filter",
                            Title = "Blazor DataGrid Simple filter mode",
                            Tags = new [] { "filter", "advanced", "grid", "datagrid", "table"}
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
                            Title = "Blazor DataGrid Hierarchy on demand",
                            Tags = new [] { "master", "detail", "datagrid", "table", "dataview", "on-demand" }
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
                            Name = "Multiple selection",
                            Path = "datagrid-multiple-selection",
                            Title = "Blazor DataGrid Multiple selection",
                            Tags = new [] { "multiple", "selection", "datagrid", "table", "dataview" }
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
                            Title = "Blazor DataGrid multiple column sorting",
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
                            Name = "Pager API",
                            Path = "datagrid-pager-api",
                            Title = "Blazor DataGrid pager API",
                            Tags = new [] { "pager", "paging", "api", "datagrid", "table", "dataview" }
                        }
                    }
                },
                new Example()
                {
                    Name = "InLine Editing",
                    Path = "datagrid-inline-edit",
                    Title = "Blazor DataGrid InLine Editing",
                    Icon = "&#xe22b",
                    Tags = new [] { "inline", "editor", "datagrid", "table", "dataview" }
                },

                new Example()
                {
                    Name = "Conditional formatting",
                    Path = "datagrid-conditional-template",
                    Title = "DataGrid conditional template",
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
                    Title = "Blazor Cascading DropDowns",
                    Icon = "&#xe915",
                    Tags = new [] { "related", "parent", "child" }
                },
            }
        },
        new Example
        {
            Name="Data",
            Children = new [] {
                new Example()
                {
                    Name = "DataList",
                    Path = "datalist",
                    Icon = "&#xe896",
                    Tags = new [] { "dataview", "grid", "table" }
                },

                new Example()
                {
                    Name = "Pager",
                    Path = "pager",
                    Icon = "&#xe8be",
                    Tags = new [] { "pager", "paging" }
                },

                new Example()
                {
                    Name = "Tree",
                    Path = "tree",
                    Icon = "&#xe8ef",
                    Tags = new [] { "tree", "treeview", "nodes", "hierarchy" }
                },

                new Example()
                {
                    Name = "Scheduler",
                    Path = "scheduler",
                    Icon = "&#xe616",
                    Tags = new [] { "scheduler", "calendar", "event", "appointment"}
                },
            }
        },
        new Example()
        {
            Name="HtmlEditor",
            Children = new [] {
                new Example()
                {
                    Name = "Default Tools",
                    Path = "html-editor",
                    Icon = "&#xe3c9",
                    Tags = new [] { "html", "editor", "rich", "text" }
                },
                new Example()
                {
                    Name = "Custom Tools",
                    Path = "html-editor-custom-tools",
                    Icon = "&#xe8b8",
                    Tags = new [] { "html", "editor", "rich", "text", "tool", "custom" }
                },
            }
        },
        new Example
        {
            Name="Charts",
            Children= new [] {
                new Example
                {
                    Name = "Line Chart",
                    Path = "line-chart",
                    Icon = "&#xe922",
                    Tags = new [] { "chart", "graph", "line" }
                },
                new Example
                {
                    Name = "Area Chart",
                    Path = "area-chart",
                    Icon = "&#xe251",
                    Tags = new [] { "chart", "graph", "area" }
                },
                new Example
                {
                    Name = "Column Chart",
                    Path = "column-chart",
                    Icon = "&#xe24b",
                    Tags = new [] { "chart", "graph", "column", "bar" }
                },
                new Example
                {
                    Name = "Bar Chart",
                    Path = "bar-chart",
                    Icon = "&#xe164",
                    Tags = new [] { "chart", "graph", "column", "bar" }
                },
                new Example
                {
                    Name = "Pie Chart",
                    Path = "pie-chart",
                    Icon = "&#xe6c4",
                    Tags = new [] { "chart", "graph", "pie" }
                },
                new Example
                {
                    Name = "Donut Chart",
                    Path = "donut-chart",
                    Icon = "&#xe917",
                    Tags = new [] { "chart", "graph", "donut" }
                },
                new Example
                {
                    Name = "Styling",
                    Path = "styling-chart",
                    Icon = "&#xe41d",
                    Tags = new [] { "chart", "graph", "styling" }
                },
            }
        },

        new Example()
        {
            Name="Forms",
            Children = new [] {
                new Example()
                {
                    Name = "AutoComplete",
                    Path = "autocomplete",
                    Icon = "&#xe03b",
                    Tags = new [] { "form", "complete", "suggest", "edit" }
                },
                new Example()
                {
                    Name = "Switch",
                    Path = "switch",
                    Icon = "&#xe8e0",
                    Tags = new [] { "form", "edit", "switch" }
                },
                new Example()
                {
                    Name = "CheckBox",
                    Path = "checkbox",
                    Icon = "&#xe86c",
                    Tags = new [] { "form", "edit" }
                },
                new Example()
                {
                    Name = "CheckBoxList",
                    Path = "checkboxlist",
                    Icon = "&#xe065",
                    Tags = new [] { "form", "edit" }
                },
                new Example()
                {
                    Name = "ColorPicker",
                    Path = "colorpicker",
                    Icon = "&#xe40a",
                    Tags = new [] { "form", "edit" }
                },
                new Example()
                {
                    Name = "DatePicker",
                    Path = "datepicker",
                    Icon = "&#xe916",
                    Tags = new [] { "calendar", "form", "edit" }
                },
                new Example()
                {
                    Name = "DropDown",
                    Path = "dropdown",
                    Icon = "&#xe875",
                    Tags = new [] { "select", "picker", "form" , "edit" }
                },
                new Example()
                {
                    Name = "DropDownDataGrid",
                    Path = "dropdown-datagrid",
                    Icon = "&#xe8b0",
                    Tags = new [] { "select", "picker", "form", "edit" }
                },
                new Example()
                {
                    Name = "FileInput",
                    Path = "fileinput",
                    Icon = "&#xe226",
                    Tags = new [] { "upload", "form", "edit" }
                },
                new Example()
                {
                    Name = "ListBox",
                    Path = "listbox",
                    Icon = "&#xe8ef",
                    Tags = new [] { "select", "picker", "form", "edit" }
                },
                new Example()
                {
                    Name = "Numeric",
                    Path = "numeric",
                    Icon = "&#xe85b",
                    Tags = new [] { "input", "number", "form", "edit" }
                },
                new Example()
                {
                    Name = "Password",
                    Path = "password",
                    Icon = "&#xe8a1",
                    Tags = new [] { "input", "form", "edit" }
                },
                new Example()
                {
                    Name = "RadioButtonList",
                    Path = "radiobuttonlist",
                    Icon = "&#xe837",
                    Tags = new [] { "toggle", "form", "edit" }
                },
                new Example()
                {
                    Name = "Rating",
                    Path = "rating",
                    Icon = "&#xe839",
                    Tags = new [] { "star", "form", "edit" }
                },
                new Example()
                {
                    Name = "SelectBar",
                    Path = "selectbar",
                    Icon = "&#xe86d",
                    Tags = new [] { "form", "edit" }
                },
                new Example()
                {
                    Name = "Slider",
                    Path = "slider",
                    Icon = "&#xe260",
                    Tags = new [] { "form", "slider" }
                },
                new Example()
                {
                    Name = "TemplateForm",
                    Path = "templateform",
                    Icon = "&#xe06d",
                    Tags = new [] { "form", "edit" }
                },
                new Example()
                {
                    Name = "TextBox",
                    Path = "textbox",
                    Icon = "&#xe890",
                    Tags = new [] { "input", "form", "edit" }
                },
                new Example()
                {
                    Name = "Mask",
                    Path = "mask",
                    Icon = "&#xe262",
                    Tags = new [] { "input", "form", "edit", "mask" }
                },
                new Example()
                {
                    Name = "TextArea",
                    Path = "textarea",
                    Icon = "&#xe873",
                    Tags = new [] { "input", "form", "edit" }
                },
            },
        },
        new Example()
        {
            Name = "Validators",
            Children = new [] {
                new Example()
                {
                    Name = "RequiredValidator",
                    Path = "requiredvalidator",
                    Icon = "&#xe5ca",
                    Tags = new [] { "validator", "validation", "required"}
                },
                new Example()
                {
                    Name = "LengthValidator",
                    Path = "lengthvalidator",
                    Icon = "&#xe915",
                    Tags = new [] { "validator", "validation", "required", "length"}
                } ,
                new Example()
                {
                    Name = "NumericRangeValidator",
                    Path = "numericrangevalidator",
                    Icon = "&#xe3d0",
                    Tags = new [] { "validator", "validation", "required", "range"}
                },
                new Example()
                {
                    Name = "CompareValidator",
                    Path = "comparevalidator",
                    Icon = "&#xe877",
                    Tags = new [] { "validator", "validation", "required", "compare"}
                },
                new Example()
                {
                    Name = "EmailValidator",
                    Path = "emailvalidator",
                    Icon = "&#xe0be",
                    Tags = new [] { "validator", "validation", "required", "email"}
                },
                new Example()
                {
                    Name = "RegexValidator",
                    Path = "regexvalidator",
                    Icon = "&#xe53f",
                    Tags = new [] { "validator", "validation", "pattern", "regex", "regular", "expression"}
                }
            }
        },

        new Example()
        {
            Name="Containers",
            Children = new [] {
                new Example()
                {
                    Name = "Accordion",
                    Path = "accordion",
                    Icon = "&#xe8ee",
                    Tags = new [] { "panel", "container" }
                },
                new Example()
                {
                    Name = "Card",
                    Path = "card",
                    Icon = "&#xe919",
                    Tags = new [] { "container" }
                },
                new Example()
                {
                    Name = "Fieldset",
                    Path = "fieldset",
                    Icon = "&#xe850",
                    Tags = new [] { "form", "container" }
                },
                new Example()
                {
                    Name = "Panel",
                    Path = "panel",
                    Icon = "&#xe14f",
                    Tags = new [] { "container" }
                },
                new Example()
                {
                    Name = "Tabs",
                    Path = "tabs",
                    Icon = "&#xe8d8",
                    Tags = new [] { "tabstrip", "tabview", "container" }
                },
                new Example()
                {
                    Name = "Steps",
                    Path = "steps",
                    Icon = "&#xe044",
                    Tags = new [] { "step", "steps", "wizard" }
                },
				new Example()
                {
                    Name = "Splitter",
                    Path = "splitter",
                    Icon = "&#xe94f",
                    Tags = new [] { "splitter"}
                },
            }
        },
        new Example
        {
            Name="Gauges",
            Children= new [] {
                new Example
                {
                    Name = "Radial Gauge",
                    Path = "radial-gauge",
                    Icon = "&#xe01b",
                    Tags = new [] { "gauge", "graph", "radial", "circle" }
                },
                new Example
                {
                    Name = "Arc Gauge",
                    Path = "arc-gauge",
                    Icon = "&#xe3fc",
                    Tags = new [] { "gauge", "graph", "arc", "progress" }
                },
                new Example
                {
                    Name = "Styling Gauge",
                    Path = "styling-gauge",
                    Icon = "&#xe41d",
                    Tags = new [] { "gauge", "graph", "styling" }
                },
            }
        },
        new Example()
        {
            Name = "Misc",
            Children = new [] {
                new Example()
                {
                    Name = "Button",
                    Path = "button",
                    Icon = "&#xe86d"
                },
                new Example()
                {
                    Name = "GoogleMap",
                    Path = "googlemap",
                    Icon = "&#xe55b"
                },
                new Example()
                {
                    Name = "Gravatar",
                    Path = "gravatar",
                    Icon = "&#xe84e"
                },
                new Example()
                {
                    Name = "SplitButton",
                    Path = "splitbutton",
                    Icon = "&#xe05f"
                },
                new Example()
                {
                    Name = "Icon",
                    Path = "icon",
                    Icon = "&#xe84f"
                },
                new Example()
                {
                    Name = "Image",
                    Path = "image",
                    Icon = "&#xe8aa"
                },
                new Example()
                {
                    Name = "Link",
                    Path = "link",
                    Icon = "&#xe157"
                },
                new Example()
                {
                    Name = "Login",
                    Path = "login",
                    Icon = "&#xe8e8"
                },
                new Example()
                {
                    Name = "ProgressBar",
                    Path = "progressbar",
                    Icon = "&#xe893",
                    Tags = new [] { "progress", "spinner" }
                },
                new Example()
                {
                    Name = "Dialog",
                    Path = "dialog",
                    Icon = "&#xe8a7",
                    Tags = new [] { "popup", "window" }
                },
                new Example()
                {
                    Name = "Notification",
                    Path = "notification",
                    Icon = "&#xe85a",
                    Tags = new [] { "message", "alert" }
                },
                new Example()
                {
                    Name = "Tooltip",
                    Path = "tooltip",
                    Icon = "&#xe8cd",
                    Tags = new [] { "popup", "tooltip" }
                },
                new Example()
                {
                    Name = "Menu",
                    Path = "menu",
                    Icon = "&#xe91a",
                    Tags = new [] { "navigation", "dropdown" }
                },
                new Example()
                {
                    Name = "PanelMenu",
                    Path = "panelmenu",
                    Icon = "&#xe8d2",
                    Tags = new [] { "navigation", "menu" }
                },
                new Example()
                {
                    Name = "ContextMenu",
                    Path = "contextmenu",
                    Icon = "&#xe8de",
                    Tags = new [] { "popup", "dropdown", "menu" }
                },
                new Example()
                {
                    Name = "ProfileMenu",
                    Path = "profile-menu",
                    Icon = "&#xe851",
                    Tags = new [] { "navigation", "dropdown", "menu" }
                },
                new Example()
                {
                    Name = "Upload",
                    Path = "example-upload",
                    Icon = "&#xe2c6",
                    Tags = new [] { "upload", "file"}
                },
                new Example()
                {
                    Name = "Badge",
                    Path = "badge",
                    Icon = "&#xea67",
                    Tags = new[] { "badge", "link"}
                }
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
            bool contains(string value) => value.Contains(term, StringComparison.OrdinalIgnoreCase);

            bool filter(Example example) => contains(example.Name) || (example.Tags != null && example.Tags.Any(contains));

            bool deepFilter(Example example) => filter(example) || example.Children?.Any(filter) == true;

            return Examples.Where(category => category.Children?.Any(deepFilter) == true)
                           .Select(category => new Example
                           {
                               Name = category.Name,
                               Expanded = true,
                               Children = category.Children.Where(deepFilter).Select(example => new Example
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
            return Examples.SelectMany(example => example.Children ?? new[] { example })
                           .FirstOrDefault(example => example.Path == uri.AbsolutePath || $"/{example.Path}" == uri.AbsolutePath);
        }

        public string TitleFor(Example example)
        {
            if (example != null && example.Name != "First Look")
            {
                return example.Title ?? $"Blazor {example.Name} | a free UI component by Radzen";
            }

            return "Free Blazor Components | 60+ controls by Radzen";
        }

        public string DescriptionFor(Example example)
        {
            return example?.Description ?? "The Radzen Blazor component library provides more than 50 UI controls for building rich ASP.NET Core web applications.";
        }
    }
}