using System;
using System.Linq;
using System.Linq.Expressions;
using System.Collections.Generic;

namespace LatestBlazor
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
        new Example()
        {
            Name = "General",
            Expanded = true,
            Children = new [] {
                new Example()
                {
                    Name = "Button",
                    Path = "button",
                    Icon = "&#x853"
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
            Name="Data",
            Children = new [] {
                new Example()
                {
                    Name = "DataGrid",
                    Path = "datagrid",
                    Icon = "&#xe3ec",
                    Tags = new [] { "datatable", "datagridview", "dataview", "grid", "table" }
                },
                new Example()
                {
                    Name = "DataList",
                    Path = "datalist",
                    Icon = "&#xe896",
                    Tags = new [] { "dataview", "grid", "table" }
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
            Name="Application Scenarios",
            Children = new [] {
                new Example()
                {
                    Name = "Hierarchy",
                    Path = "master-detail-hierarchy",
                    Icon = "&#xe23e",
                    Title = "Blazor DataGrid Hierarchy",
                    Tags = new [] { "master", "detail", "datagrid", "table", "dataview" }
                },
                new Example()
                {
                    Name = "Master/Detail",
                    Path = "master-detail",
                    Icon = "&#xe1b2",
                    Title = "Master and detail Blazor DataGrid",
                    Tags = new [] { "master", "detail", "datagrid", "table", "dataview" }
                },
                new Example()
                {
                    Name = "DataGrid InLine Editing",
                    Path = "datagrid-inline-edit",
                    Title = "Blazor DataGrid InLine Editing",
                    Icon = "&#xe22b",
                    Tags = new [] { "inline", "editor", "datagrid", "table", "dataview" }
                },
                new Example()
                {
                    Name = "DataGrid Footer Totals",
                    Path = "datagrid-footer-totals",
                    Title = "Blazor DataGrid footer totals",
                    Icon = "&#xe336",
                    Tags = new [] { "summary", "total", "aggregate", "datagrid", "table", "dataview" }
                },
                new Example()
                {
                    Name = "DataGrid conditional template",
                    Path = "datagrid-conditional-template",
                    Title = "DataGrid conditional template",
                    Icon = "&#xe41d",
                    Tags = new [] { "conditional", "template", "style", "datagrid", "table", "dataview" }
                },
                new Example()
                {
                    Name = "Cascading DropDowns",
                    Path = "cascading-dropdowns",
                    Title = "Blazor Cascading DropDowns",
                    Icon = "&#xe915",
                    Tags = new [] { "related", "parent", "child" }
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
                    Name = "DataGrid with LoadData",
                    Path = "datagrid-loaddata",
                    Title = "Blazor DataGrid custom data-binding",
                    Icon = "&#xe265",
                    Tags = new [] { "datagrid", "bind", "load", "data", "loaddata" }
                },
                new Example()
                {
                    Name = "DataGrid with OData",
                    Path = "datagrid-odata",
                    Title = "Blazor DataGrid OData data-binding",
                    Icon = "&#xe871",
                    Tags = new [] { "datagrid", "bind", "load", "data", "loaddata", "odata" }
                },
                new Example()
                {
                    Name = "DataGrid Column FilterTemplate",
                    Path = "datagrid-filter-template",
                    Title = "Blazor DataGrid custom filtering",
                    Icon = "&#xe152",
                    Tags = new [] { "datagrid", "column", "filter", "template" }
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
            Func<string, bool> contains = value => value.Contains(term, StringComparison.OrdinalIgnoreCase);

            Func<Example, bool> filter = (example) => contains(example.Name) || (example.Tags != null && example.Tags.Any(contains));

            return Examples.Where(category => category.Children != null && category.Children.Any(filter))
                           .Select(category => new Example()
                           {
                               Name = category.Name,
                               Expanded = true,
                               Children = category.Children.Where(filter).ToArray()
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

            return "Free Blazor Components | 50+ controls by Radzen";
        }

        public string DescriptionFor(Example example)
        {
            return example?.Description ?? "The Radzen Blazor component library provides more than 50 UI controls for building rich ASP.NET Core web applications.";
        }
    }
}