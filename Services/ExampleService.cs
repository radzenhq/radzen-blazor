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
            Icon = "home"
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
                    Icon = "account_circle"
                },
                new Example()
                {
                    Name = "Gravatar",
                    Path = "gravatar",
                    Icon = "accessibility"
                },
                new Example()
                {
                    Name = "SplitButton",
                    Path = "splitbutton",
                    Icon = "playlist_play"
                },
                new Example()
                {
                    Name = "Icon",
                    Path = "icon",
                    Icon = "account_balance"
                },
                new Example()
                {
                    Name = "Image",
                    Path = "image",
                    Icon = "picture_in_picture"
                },
                new Example()
                {
                    Name = "Link",
                    Path = "link",
                    Icon = "link"
                },
                new Example()
                {
                    Name = "Login",
                    Path = "login",
                    Icon = "verified_user"
                },
                new Example()
                {
                    Name = "ProgressBar",
                    Path = "progressbar",
                    Icon = "label_outline",
                    Tags = new [] { "progress", "spinner" }
                },
                new Example()
                {
                    Name = "Dialog",
                    Path = "dialog",
                    Icon = "perm_media",
                    Tags = new [] { "popup", "window" }
                },
                new Example()
                {
                    Name = "Notification",
                    Path = "notification",
                    Icon = "announcement",
                    Tags = new [] { "message", "alert" }
                },
                new Example()
                {
                    Name = "Menu",
                    Path = "menu",
                    Icon = "line_weight",
                    Tags = new [] { "navigation", "dropdown" }
                },
                new Example()
                {
                    Name = "Upload",
                    Path = "example-upload",
                    Icon = "file_upload",
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
                    Icon = "view_headline",
                    Tags = new [] { "panel", "container" }
                },
                new Example()
                {
                    Name = "Card",
                    Path = "card",
                    Icon = "line_style",
                    Tags = new [] { "container" }
                },
                new Example()
                {
                    Name = "Fieldset",
                    Path = "fieldset",
                    Icon = "account_balance_wallet",
                    Tags = new [] { "form", "container" }
                },
                new Example()
                {
                    Name = "Panel",
                    Path = "panel",
                    Icon = "content_paste",
                    Tags = new [] { "container" }
                },
                new Example()
                {
                    Name = "Tabs",
                    Path = "tabs",
                    Icon = "tab",
                    Tags = new [] { "tabstrip", "tabview", "container" }
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
                    Icon = "playlist_add",
                    Tags = new [] { "form", "complete", "suggest", "edit" }
                },
                new Example()
                {
                    Name = "CheckBox",
                    Path = "checkbox",
                    Icon = "check_circle",
                    Tags = new [] { "form", "edit" }
                },
                new Example()
                {
                    Name = "CheckBoxList",
                    Path = "checkboxlist",
                    Icon = "playlist_add_check",
                    Tags = new [] { "form", "edit" }
                },
                new Example()
                {
                    Name = "DatePicker",
                    Path = "datepicker",
                    Icon = "date_range",
                    Tags = new [] { "calendar", "form", "edit" }
                },
                new Example()
                {
                    Name = "DropDown",
                    Path = "dropdown",
                    Icon = "dns",
                    Tags = new [] { "select", "picker", "form" , "edit" }
                },
                new Example()
                {
                    Name = "DropDownDataGrid",
                    Path = "dropdown-datagrid",
                    Icon = "receipt",
                    Tags = new [] { "select", "picker", "form", "edit" }
                },
                new Example()
                {
                    Name = "FileInput",
                    Path = "fileinput",
                    Icon = "attach_file",
                    Tags = new [] { "upload", "form", "edit" }
                },
                new Example()
                {
                    Name = "ListBox",
                    Path = "listbox",
                    Icon = "view_list",
                    Tags = new [] { "select", "picker", "form", "edit" }
                },
                new Example()
                {
                    Name = "Numeric",
                    Path = "numeric",
                    Icon = "aspect_ratio",
                    Tags = new [] { "input", "number", "form", "edit" }
                },
                new Example()
                {
                    Name = "Password",
                    Path = "password",
                    Icon = "payment",
                    Tags = new [] { "input", "form", "edit" }
                },
                new Example()
                {
                    Name = "RadioButtonList",
                    Path = "radiobuttonlist",
                    Icon = "radio_button_checked",
                    Tags = new [] { "toggle", "form", "edit" }
                },
                new Example()
                {
                    Name = "Rating",
                    Path = "rating",
                    Icon = "star_rate",
                    Tags = new [] { "star", "form", "edit" }
                },
                new Example()
                {
                    Name = "SelectBar",
                    Path = "selectbar",
                    Icon = "chrome_reader_mode",
                    Tags = new [] { "form", "edit" }
                },
                new Example()
                {
                    Name = "TemplateForm",
                    Path = "templateform",
                    Icon = "featured_play_list",
                    Tags = new [] { "form", "edit" }
                },
                new Example()
                {
                    Name = "TextBox",
                    Path = "textbox",
                    Icon = "input",
                    Tags = new [] { "input", "form", "edit" }
                },
                new Example()
                {
                    Name = "TextArea",
                    Path = "textarea",
                    Icon = "description",
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
                    Icon = "check",
                    Tags = new [] { "validator", "validation", "required"}
                },
                new Example()
                {
                    Name = "LengthValidator",
                    Path = "lengthvalidator",
                    Icon = "compare_arrows",
                    Tags = new [] { "validator", "validation", "required", "length"}
                } ,
                new Example()
                {
                    Name = "NumericRangeValidator",
                    Path = "numericrangevalidator",
                    Icon = "filter_1",
                    Tags = new [] { "validator", "validation", "required", "range"}
                },
                new Example()
                {
                    Name = "CompareValidator",
                    Path = "comparevalidator",
                    Icon = "done_all",
                    Tags = new [] { "validator", "validation", "required", "compare"}
                },
                new Example()
                {
                    Name = "EmailValidator",
                    Path = "emailvalidator",
                    Icon = "email",
                    Tags = new [] { "validator", "validation", "required", "email"}
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
                    Icon = "grid_on",
                    Tags = new [] { "datatable", "datagridview", "dataview", "grid", "table" }
                },
                new Example()
                {
                    Name = "DataList",
                    Path = "datalist",
                    Icon = "list",
                    Tags = new [] { "dataview", "grid", "table" }
                },

                new Example()
                {
                    Name = "Tree",
                    Path = "tree",
                    Icon = "view_list",
                    Tags = new [] { "tree", "treeview", "nodes", "hierarchy" }
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
                    Icon = "format_indent_increase",
                    Title = "Blazor DataGrid Hierarchy",
                    Tags = new [] { "master", "detail", "datagrid", "table", "dataview" }
                },
                new Example()
                {
                    Name = "Master/Detail",
                    Path = "master-detail",
                    Icon = "dvr",
                    Title = "Master and detail Blazor DataGrid",
                    Tags = new [] { "master", "detail", "datagrid", "table", "dataview" }
                },
                new Example()
                {
                    Name = "DataGrid InLine Editing",
                    Path = "datagrid-inline-edit",
                    Title = "Blazor DataGrid InLine Editing",
                    Icon = "border_color",
                    Tags = new [] { "inline", "editor", "datagrid", "table", "dataview" }
                },
                new Example()
                {
                    Name = "DataGrid Footer Totals",
                    Path = "datagrid-footer-totals",
                    Title = "Blazor DataGrid footer totals",
                    Icon = "power_input",
                    Tags = new [] { "summary", "total", "aggregate", "datagrid", "table", "dataview" }
                },
                new Example()
                {
                    Name = "DataGrid conditional template",
                    Path = "datagrid-conditional-template",
                    Title = "DataGrid conditional template",
                    Icon = "style",
                    Tags = new [] { "conditional", "template", "style", "datagrid", "table", "dataview" }
                },
                new Example()
                {
                    Name = "Cascading DropDowns",
                    Path = "cascading-dropdowns",
                    Title = "Blazor Cascading DropDowns",
                    Icon = "compare_arrows",
                    Tags = new [] { "related", "parent", "child" }
                },
                new Example()
                {
                    Name = "Export to Excel and CSV",
                    Path = "export-excel-csv",
                    Title = "Blazor DataGrid export to Excel and CSV",
                    Icon = "import_export",
                    Tags = new [] { "export", "excel", "csv" }
                },
                new Example()
                {
                    Name = "DataGrid with LoadData",
                    Path = "datagrid-loaddata",
                    Title = "Blazor DataGrid custom data-binding",
                    Icon = "dashboard",
                    Tags = new [] { "datagrid", "bind", "load", "data", "loaddata" }
                },
                new Example()
                {
                    Name = "DataGrid Column FilterTemplate",
                    Path = "datagrid-filter-template",
                    Title = "Blazor DataGrid custom filtering",
                    Icon = "filter_list",
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
           return Examples.SelectMany(example => example.Children ?? new [] {example})
                          .FirstOrDefault(example => example.Path == uri.AbsolutePath || $"/{example.Path}" == uri.AbsolutePath); 
        }

        public string TitleFor(Example example)
        {
            if (example != null && example.Name != "First Look")
            {
                return example.Title ?? $"Blazor {example.Name} | a free UI component by Radzen";
            }

            return "Free Blazor Components | 40+ controls by Radzen";
        }

        public string DescriptionFor(Example example)
        {
            return example?.Description ?? "The Radzen Blazor component library provides more than 30 UI controls for building rich ASP.NET Core web applications.";
        }
    }
}