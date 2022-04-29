using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Radzen.Blazor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;

namespace Radzen
{
    /// <summary>
    /// Enables "onmouseenter" and "onmouseleave" event support in Blazor. Not for public use.
    /// </summary>
    [EventHandler("onmouseenter", typeof(EventArgs), true, true)]
    [EventHandler("onmouseleave", typeof(EventArgs), true, true)]
    public static class EventHandlers
    {
    }

    /// <summary>
    /// Represents the common <see cref="RadzenSelectBar{TValue}" /> API used by 
    /// its items. Injected as a cascading property in <see cref="RadzenSelectBarItem" />.
    /// </summary>
    public interface IRadzenSelectBar
    {
        /// <summary>
        /// Adds the specified item to the select bar.
        /// </summary>
        /// <param name="item">The item to add.</param>
        void AddItem(RadzenSelectBarItem item);

        /// <summary>
        /// Removes the specified item from the select bar.
        /// </summary>
        /// <param name="item">The item.</param>
        void RemoveItem(RadzenSelectBarItem item);
    }

    /// <summary>
    /// Supplies information about a <see cref="RadzenDatePicker{TValue}.DateRender" /> event that is being raised.
    /// </summary>
    public class DateRenderEventArgs
    {
        /// <summary>
        /// Gets or sets the HTML attributes that will be applied to item HTML element.
        /// </summary>
        /// <example>
        /// void OnDateRender(DateRenderEventArgs args)
        /// {
        ///     args.Attributes["style"] = "background-color: red; color: black;";
        /// }
        /// </example>
        /// <value>The attributes.</value>
        public IDictionary<string, object> Attributes { get; private set; } = new Dictionary<string, object>();
        /// <summary>
        /// Gets the date which the rendered item represents.
        /// </summary>
        public DateTime Date { get; internal set; }

        /// <summary>
        /// Gets or sets a value indicating whether the rendered item is disabled.
        /// </summary>
        /// <value><c>true</c> if disabled; otherwise, <c>false</c>.</value>
        public bool Disabled { get; set; }
    }

    /// <summary>
    /// A class that represents a <see cref="RadzenGoogleMap" /> position.
    /// </summary>
    public class GoogleMapPosition
    {
        /// <summary>
        /// Gets or sets the latitude.
        /// </summary>
        /// <value>The latitude.</value>
        public double Lat { get; set; }
        /// <summary>
        /// Gets or sets the longitude.
        /// </summary>
        /// <value>The longitude.</value>
        public double Lng { get; set; }
    }

    /// <summary>
    /// Supplies information about a <see cref="RadzenGoogleMap.MapClick" /> event that is being raised.
    /// </summary>
    public class GoogleMapClickEventArgs
    {
        /// <summary>
        /// The position which represents the clicked map location.
        /// </summary>
        public GoogleMapPosition Position { get; set; }
    }

    /// <summary>
    /// Supplies information about a <see cref="RadzenMenu.Click" /> event that is being raised.
    /// </summary>
    public class MenuItemEventArgs : MouseEventArgs
    {
        /// <summary>
        /// Gets text of the clicked item.
        /// </summary>
        public string Text { get; internal set; }
        /// <summary>
        /// Gets the value of the clicked item.
        /// </summary>
        public object Value { get; internal set; }
        /// <summary>
        /// Gets the path path of the clicked item.
        /// </summary>
        public string Path { get; internal set; }
    }

    /// <summary>
    /// Supplies information about a <see cref="RadzenUpload.Error" /> event that is being raised.
    /// </summary>
    public class UploadErrorEventArgs
    {
        /// <summary>
        /// Gets a message telling what caused the error.
        /// </summary>
        public string Message { get; set; }
    }

    /// <summary>
    /// Supplies information about a <see cref="RadzenDataGrid{TItem}" /> event that is being raised.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class RowRenderEventArgs<T>
    {
        /// <summary>
        /// Gets or sets the row HTML attributes. They will apply to the table row (tr) element which RadzenDataGrid renders for every row.
        /// </summary>
        public IDictionary<string, object> Attributes { get; private set; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets the data item which the current row represents.
        /// </summary>
        public T Data { get; internal set; }

        /// <summary>
        /// Gets or sets a value indicating whether this row is expandable.
        /// </summary>
        /// <value><c>true</c> if expandable; otherwise, <c>false</c>.</value>
        public bool Expandable { get; set; }
    }

    /// <summary>
    /// Supplies information about a <see cref="RadzenDataGrid{TItem}" /> event that is being raised.
    /// </summary>
    public class GroupRowRenderEventArgs
    {
        /// <summary>
        /// Gets or sets the group row HTML attributes. They will apply to the table row (tr) element which RadzenDataGrid renders for every group row.
        /// </summary>
        public IDictionary<string, object> Attributes { get; private set; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets the data item which the current row represents.
        /// </summary>
        public Group Group { get; internal set; }

        /// <summary>
        /// Gets or sets a value indicating whether this group row is expanded.
        /// </summary>
        /// <value><c>true</c> if expanded; otherwise, <c>false</c>.</value>
        public bool? Expanded { get; set; }

        /// <summary>
        /// Gets a value indicating whether this is the first time the RadzenGrid has rendered.
        /// </summary>
        /// <value><c>true</c> if this is the first time; otherwise, <c>false</c>.</value>
        public bool FirstRender { get; internal set; }
    }

    /// <summary>
    /// Supplies information about a <see cref="RadzenGrid{TItem}.Render" /> event that is being raised.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class GridRenderEventArgs<T>
    {
        /// <summary>
        /// Gets the instance of the RadzenGrid component which has rendered.
        /// </summary>
        public RadzenGrid<T> Grid { get; internal set; }
        /// <summary>
        /// Gets a value indicating whether this is the first time the RadzenGrid has rendered.
        /// </summary>
        /// <value><c>true</c> if this is the first time; otherwise, <c>false</c>.</value>
        public bool FirstRender { get; internal set; }
    }

    /// <summary>
    /// Supplies information about a <see cref="RadzenDataGrid{TItem}.Render" /> event that is being raised.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class DataGridRenderEventArgs<T>
    {
        /// <summary>
        /// Gets the instance of the RadzenDataGrid component which has rendered.
        /// </summary>
        public RadzenDataGrid<T> Grid { get; internal set; }
        /// <summary>
        /// Gets a value indicating whether this is the first time the RadzenDataGrid has rendered.
        /// </summary>
        /// <value><c>true</c> if this is the first time; otherwise, <c>false</c>.</value>
        public bool FirstRender { get; internal set; }
    }

    /// <summary>
    /// Supplies information about a <see cref="RadzenGrid{TItem}.CellRender" /> event that is being raised.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class CellRenderEventArgs<T> : RowRenderEventArgs<T>
    {
        /// <summary>
        /// Gets the RadzenGridColumn which this cells represents.
        /// </summary>
        public RadzenGridColumn<T> Column { get; internal set; }
    }

    /// <summary>
    /// Supplies information about a <see cref="RadzenDataGrid{TItem}.CellRender" /> event that is being raised.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class DataGridCellRenderEventArgs<T> : RowRenderEventArgs<T>
    {
        /// <summary>
        /// Gets the RadzenDataGridColumn which this cells represents.
        /// </summary>
        public RadzenDataGridColumn<T> Column { get; internal set; }
    }

    /// <summary>
    /// Supplies information about a <see cref="RadzenDataGrid{TItem}.CellContextMenu" /> event that is being raised.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class DataGridCellMouseEventArgs<T> : Microsoft.AspNetCore.Components.Web.MouseEventArgs
    {
        /// <summary>
        /// Gets the data item which the clicked DataGrid row represents.
        /// </summary>
        public T Data { get; internal set; }

        /// <summary>
        /// Gets the RadzenDataGridColumn which this cells represents.
        /// </summary>
        public RadzenDataGridColumn<T> Column { get; internal set; }
    }

    /// <summary>
    /// Supplies information about a <see cref="RadzenDataGrid{TItem}.RowClick" /> or <see cref="RadzenDataGrid{TItem}.RowDoubleClick" /> event that is being raised.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class DataGridRowMouseEventArgs<T> : Microsoft.AspNetCore.Components.Web.MouseEventArgs
    {
        /// <summary>
        /// Gets the data item which the clicked DataGrid row represents.
        /// </summary>
        public T Data { get; internal set; }
    }

    /// <summary>
    /// Supplies information about a <see cref="RadzenUpload.Change" /> event that is being raised.
    /// </summary>
    public class UploadChangeEventArgs
    {
        /// <summary>
        /// Gets a collection of the selected files.
        /// </summary>
        public IEnumerable<FileInfo> Files { get; set; }
    }

    /// <summary>
    /// Supplies information about a <see cref="RadzenUpload.Progress" /> event that is being raised.
    /// </summary>
    public class UploadProgressArgs
    {
        /// <summary>
        /// Gets or sets the number of bytes that have been uploaded.
        /// </summary>
        public long Loaded { get; set; }
        /// <summary>
        /// Gets the total number of bytes that need to be uploaded.
        /// </summary>
        public long Total { get; set; }
        /// <summary>
        /// Gets the progress as a percentage value (from <c>0</c> to <c>100</c>).
        /// </summary>
        public int Progress { get; set; }
        /// <summary>
        /// Gets a collection of files that are being uploaded.
        /// </summary>
        public IEnumerable<FileInfo> Files { get; set; }
    }

    /// <summary>
    /// Supplies information about a <see cref="RadzenUpload.Complete" /> event that is being raised.
    /// </summary>
    public class UploadCompleteEventArgs
    {
        /// <summary>
        /// Gets the JSON response which the server returned after the upload.
        /// </summary>
        public JsonDocument JsonResponse { get; set; }

        /// <summary>
        /// Gets the raw server response.
        /// </summary>
        public string RawResponse { get; set; }
    }

    /// <summary>
    /// Represents a file which the user selects for upload via <see cref="RadzenUpload" />.
    /// </summary>
    public class FileInfo
    {
        /// <summary>
        /// Gets the name of the selected file.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets the size (in bytes) of the selected file.
        /// </summary>
        public long Size { get; set; }
    }

    /// <summary>
    /// Represents the preview which <see cref="RadzenUpload" /> displays for selected files.
    /// </summary>
    public class PreviewFileInfo : FileInfo
    {
        /// <summary>
        /// Gets the URL of the previewed file.
        /// </summary>
        public string Url { get; set; }
    }

    /// <summary>
    /// Contains a LINQ query in a serializable format.
    /// </summary>
    public class Query
    {
        /// <summary>
        /// Gets or sets the filter.
        /// </summary>
        /// <value>The filter.</value>
        public string Filter { get; set; }
        /// <summary>
        /// Gets or sets the filter parameters.
        /// </summary>
        /// <value>The filter parameters.</value>
        public object[] FilterParameters { get; set; }
        /// <summary>
        /// Gets or sets the order by.
        /// </summary>
        /// <value>The order by.</value>
        public string OrderBy { get; set; }
        /// <summary>
        /// Gets or sets the expand.
        /// </summary>
        /// <value>The expand.</value>
        public string Expand { get; set; }
        /// <summary>
        /// Gets or sets the select.
        /// </summary>
        /// <value>The select.</value>
        public string Select { get; set; }
        /// <summary>
        /// Gets or sets the skip.
        /// </summary>
        /// <value>The skip.</value>
        public int? Skip { get; set; }
        /// <summary>
        /// Gets or sets the top.
        /// </summary>
        /// <value>The top.</value>
        public int? Top { get; set; }

        /// <summary>
        /// Converts the query to OData query format.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <returns>System.String.</returns>
        public string ToUrl(string url)
        {
            var queryParameters = new Dictionary<string, object>();

            if (Skip != null)
            {
                queryParameters.Add("$skip", Skip.Value);
            }

            if (Top != null)
            {
                queryParameters.Add("$top", Top.Value);
            }

            if (!string.IsNullOrEmpty(OrderBy))
            {
                queryParameters.Add("$orderBy", OrderBy);
            }

            if (!string.IsNullOrEmpty(Filter))
            {
                queryParameters.Add("$filter", UrlEncoder.Default.Encode(Filter));
            }

            if (!string.IsNullOrEmpty(Expand))
            {
                queryParameters.Add("$expand", Expand);
            }

            if (!string.IsNullOrEmpty(Select))
            {
                queryParameters.Add("$select", Select);
            }

            return string.Format("{0}{1}", url, queryParameters.Any() ? "?" + string.Join("&", queryParameters.Select(a => $"{a.Key}={a.Value}")) : "");
        }
    }

    /// <summary>
    /// Specifies the ways a <see cref="RadzenTabs" /> component renders its items.
    /// </summary>
    public enum TabRenderMode
    {
        /// <summary>
        /// The RadzenTabs component switches its items server side. Only the selected item is rendered.
        /// </summary>
        Server,
        /// <summary>
        /// The RadzenTabs components switches its items client-side. All items are rendered and the unselected ones are hidden with CSS.
        /// </summary>
        Client
    }

    /// <summary>
    /// Specifies the ways a <see cref="RadzenTabs" /> component renders its titles.
    /// </summary>
    public enum TabPosition
    {
        /// <summary>
        /// The RadzenTabs titles are displayed at the top of the component.
        /// </summary>
        Top,
        /// <summary>
        /// The RadzenTabs titles are displayed at the bottom of the component.
        /// </summary>
        Bottom,
        /// <summary>
        /// The RadzenTabs titles are displayed at the left side of the component.
        /// </summary>
        Left,
        /// <summary>
        /// The RadzenTabs titles are displayed at the right side of the component.
        /// </summary>
        Right
    }

    /// <summary>
    /// Specifies the position at which a Radzen Blazor component renders its built-in <see cref="RadzenPager" />.
    /// </summary>
    public enum PagerPosition
    {
        /// <summary>
        /// RadzenPager is displayed at the top of the component.
        /// </summary>
        Top,
        /// <summary>
        /// RadzenPager is displayed at the bottom of the component.
        /// </summary>
        Bottom,
        /// <summary>
        /// RadzenPager is displayed at the top and at the bottom of the component.
        /// </summary>
        TopAndBottom
    }

    /// <summary>
    /// Specifies the expand behavior of <see cref="RadzenDataGrid{TItem}" />.
    /// </summary>
    public enum DataGridExpandMode
    {
        /// <summary>
        /// The user can expand only one row at a time. Expanding a different row collapses the last expanded row.
        /// </summary>
        Single,
        /// <summary>
        /// The user can expand multiple rows.
        /// </summary>
        Multiple
    }

    /// <summary>
    /// Specifies the inline edit mode behavior of <see cref="RadzenDataGrid{TItem}" />.
    /// </summary>
    public enum DataGridEditMode
    {
        /// <summary>
        /// The user can edit only one row at a time. Editing a different row cancels edit mode for the last edited row.
        /// </summary>
        Single,
        /// <summary>
        /// The user can edit multiple rows.
        /// </summary>
        Multiple
    }

    /// <summary>
    /// Specifies the selection mode behavior of <see cref="RadzenDataGrid{TItem}" />.
    /// </summary>
    public enum DataGridSelectionMode
    {
        /// <summary>
        /// The user can select only one row at a time. Selecting a different row deselects the last selected row.
        /// </summary>
        Single,
        /// <summary>
        /// The user can select multiple rows.
        /// </summary>
        Multiple
    }

    /// <summary>
    /// Specifies the severity of a <see cref="RadzenNotification" />. Severity changes the visual styling of the RadzenNotification (icon and background color).
    /// </summary>
    public enum NotificationSeverity
    {
        /// <summary>
        /// Represents an error.
        /// </summary>
        Error,
        /// <summary>
        /// Represents some generic information.
        /// </summary>
        Info,
        /// <summary>
        /// Represents a success. 
        /// </summary>
        Success,
        /// <summary>
        /// Represents a warning.
        /// </summary>
        Warning
    }

    /// <summary>
    /// Specifies the behavior of <see cref="RadzenProgressBar" />.
    /// </summary>
    public enum ProgressBarMode
    {
        /// <summary>
        /// RadzenProgressBar displays its value as a percentage range (0 to 100).
        /// </summary>
        Determinate,
        /// <summary>
        /// RadzenProgressBar displays continuous animation. 
        /// </summary>
        Indeterminate
    }

    /// <summary>
    /// Represents orientation of components that have items.
    /// </summary>
    public enum Orientation
    {
        /// <summary>
        /// Sibling items are displayed next to each other.
        /// </summary>
        Horizontal,
        /// <summary>
        /// Sibling items are displayed below each other.
        /// </summary>
        Vertical
    }

    /// <summary>
    /// Specifies the sort order in components that support sorting.
    /// </summary>
    public enum SortOrder
    {
        /// <summary>
        /// Ascending sort order.
        /// </summary>
        Ascending,
        /// <summary>
        /// Descending sort order.
        /// </summary>
        Descending
    }

    /// <summary>
    /// Specifies the type of a <see cref="RadzenButton" />. Renders as the <c>type</c> HTML attribute.
    /// </summary>
    public enum ButtonType
    {
        /// <summary>
        /// Generic button which does not submit its parent form.
        /// </summary>
        Button,
        /// <summary>
        /// Clicking a submit button submits its parent form.
        /// </summary>
        Submit,
        /// <summary>
        /// Clicking a reset button clears the value of all inputs in its parent form.
        /// </summary>
        Reset
    }

    /// <summary>
    /// Specifies the size of a <see cref="RadzenButton" />.
    /// </summary>
    public enum ButtonSize
    {
        /// <summary>
        /// The default size of a button.
        /// </summary>
        Medium,
        /// <summary>
        /// A button smaller than the default.
        /// </summary>
        Small
    }

    /// <summary>
    /// Specifies the display style of a <see cref="RadzenButton" />. Affects the visual styling of RadzenButton (background and text color).
    /// </summary>
    public enum ButtonStyle
    {
        /// <summary>
        /// A primary button. Clicking it performs the primary action in a form or dialog (e.g. "save").
        /// </summary>
        Primary,
        /// <summary>
        /// A secondary button. Clicking it performs a secondaryprimary action in a form or dialog (e.g. close a dialog or cancel a form).
        /// </summary>
        Secondary,
        /// <summary>
        /// A button with lighter styling.
        /// </summary>
        Light,
        /// <summary>
        /// A button with success styling.
        /// </summary>
        Success,
        /// <summary>
        /// A button which represents a dangerous action e.g. "delete".
        /// </summary>
        Danger,
        /// <summary>
        /// A button with warning styling.
        /// </summary>
        Warning,
        /// <summary>
        /// A button with informative styling.
        /// </summary>
        Info
    }

    /// <summary>
    /// Specifies the filtering mode of <see cref="RadzenDataGrid{TItem}" />.
    /// </summary>
    public enum FilterMode
    {
        /// <summary>
        /// The component displays inline filtering UI and filters as you type.
        /// </summary>
        Simple,
        /// <summary>
        /// The component displays inline filtering UI and filters as you type combined with filter operator menu.
        /// </summary>
        SimpleWithMenu,
        /// <summary>
        /// The component displays a popup filtering UI and allows you to pick filtering operator and or filter by multiple values.
        /// </summary>
        Advanced
    }

    /// <summary>
    /// Specifies the filter case sensitivity of a component.
    /// </summary>
    public enum FilterCaseSensitivity
    {
        /// <summary>
        /// Relies on the underlying provider (LINQ to Objects, Entity Framework etc.) to handle case sensitivity. LINQ to Objects is case sensitive. Entity Framework relies on the database collection settings.
        /// </summary>
        Default,
        /// <summary>
        /// Filters are case insensitive regardless of the underlying provider.
        /// </summary>
        CaseInsensitive
    }

    /// <summary>
    /// Specifies the logical operator between filters.
    /// </summary>
    public enum LogicalFilterOperator
    {
        /// <summary>
        /// All filters should be satisfied.
        /// </summary>
        And,
        /// <summary>
        /// Any filter should be satisfied.
        /// </summary>
        Or
    }

    /// <summary>
    /// Specifies the string comparison operator of a filter.
    /// </summary>
    public enum StringFilterOperator
    {
        /// <summary>
        /// Satisfied if a string contains the specified value.
        /// </summary>
        Contains,
        /// <summary>
        /// Satisfied if a string starts with the specified value.
        /// </summary>
        StartsWith,
        /// <summary>
        /// Satisfied if a string ends with with the specified value.
        /// </summary>
        EndsWith
    }

    /// <summary>
    /// Specifies the comparison operator of a filter.
    /// </summary>
    public enum FilterOperator
    {
        /// <summary>
        /// Satisfied if the current value equals the specified value.
        /// </summary>
        Equals,
        /// <summary>
        /// Satisfied if the current value does not equal the specified value.
        /// </summary>
        NotEquals,
        /// <summary>
        /// Satisfied if the current value is less than the specified value.
        /// </summary>
        LessThan,
        /// <summary>
        /// Satisfied if the current value is less than or equal to the specified value.
        /// </summary>
        LessThanOrEquals,
        /// <summary>
        /// Satisfied if the current value is greater than the specified value.
        /// </summary>
        GreaterThan,
        /// <summary>
        /// Satisfied if the current value is greater than or equal to the specified value.
        /// </summary>
        GreaterThanOrEquals,
        /// <summary>
        /// Satisfied if the current value contains the specified value.
        /// </summary>
        Contains,
        /// <summary>
        /// Satisfied if the current value starts with the specified value.
        /// </summary>
        StartsWith,
        /// <summary>
        /// Satisfied if the current value ends with the specified value.
        /// </summary>
        EndsWith,
        /// <summary>
        /// Satisfied if the current value does not contain the specified value.
        /// </summary>
        DoesNotContain,
        /// <summary>
        /// Satisfied if the current value is null.
        /// </summary>
        IsNull,
        /// <summary>
        /// Satisfied if the current value is not null.
        /// </summary>
        IsNotNull
    }

    /// <summary>
    /// Specifies text alignment. Usually rendered as CSS <c>text-align</c> attribute.
    /// </summary>
    public enum TextAlign
    {
        /// <summary>
        /// The text is aligned to the left side of its container.
        /// </summary>
        Left,
        /// <summary>
        /// The text is aligned to the right side of its container.
        /// </summary>
        Right,
        /// <summary>
        /// The text is centered in its container.
        /// </summary>
        Center
    }

    /// <summary>
    /// Specifies horizontal alignment.
    /// </summary>
    public enum HorizontalAlign
    {
        /// <summary>
        /// Left horizontal alignment.
        /// </summary>
        Left,
        /// <summary>
        /// Right horizontal alignment.
        /// </summary>
        Right,
        /// <summary>
        /// Center horizontal alignment.
        /// </summary>
        Center,
        /// <summary>
        /// Justify horizontal alignment.
        /// </summary>
        Justify
    }

    /// <summary>
    /// Specifies the display style of a <see cref="RadzenBadge" />. Affects the visual styling of RadzenBadge (background and text color).
    /// </summary>
    public enum BadgeStyle
    {
        /// <summary>
        /// Primary styling. Similar to primary buttons.
        /// </summary>
        Primary,
        /// <summary>
        /// Secondary styling. Similar to secondary buttons.
        /// </summary>
        Secondary,
        /// <summary>
        /// Light styling. Similar to light buttons.
        /// </summary>
        Light,
        /// <summary>
        /// Success styling.
        /// </summary>
        Success,
        /// <summary>
        /// Danger styling.
        /// </summary>
        Danger,
        /// <summary>
        /// Warning styling.
        /// </summary>
        Warning,
        /// <summary>
        /// Informative styling.
        /// </summary>
        Info
    }

    /// <summary>
    /// Specifies the display style of a <see cref="RadzenIcon" />. Affects the visual styling of RadzenIcon (Icon (text) color).
    /// </summary>
    public enum IconStyle
    {
        /// <summary>
        /// Primary styling. Similar to primary buttons.
        /// </summary>
        Primary,
        /// <summary>
        /// Secondary styling. Similar to secondary buttons.
        /// </summary>
        Secondary,
        /// <summary>
        /// Light styling. Similar to light buttons.
        /// </summary>
        Light,
        /// <summary>
        /// Success styling.
        /// </summary>
        Success,
        /// <summary>
        /// Danger styling.
        /// </summary>
        Danger,
        /// <summary>
        /// Warning styling.
        /// </summary>
        Warning,
        /// <summary>
        /// Informative styling.
        /// </summary>
        Info
    }

    /// <summary>
    /// Supplies information about a <see cref="RadzenDataGrid{TItem}.Sort" /> event that is being raised.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class DataGridColumnSortEventArgs<T>
    {
        /// <summary>
        /// Gets the sorted RadzenDataGridColumn.
        /// </summary>
        public RadzenDataGridColumn<T> Column { get; internal set; }

        /// <summary>
        /// Gets the new sort order of the sorted column.
        /// </summary>
        public SortOrder? SortOrder { get; internal set; }
    }

    /// <summary>
    /// Supplies information about a <see cref="RadzenDataGrid{TItem}.Group" /> event that is being raised.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class DataGridColumnGroupEventArgs<T>
    {
        /// <summary>
        /// Gets the sorted RadzenDataGridColumn.
        /// </summary>
        public RadzenDataGridColumn<T> Column { get; internal set; }

        /// <summary>
        /// Gets the new sort order of the sorted column.
        /// </summary>
        public GroupDescriptor GroupDescriptor { get; internal set; }
    }

    /// <summary>
    /// Supplies information about a <see cref="RadzenDataGrid{TItem}.Filter" /> event that is being raised.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class DataGridColumnFilterEventArgs<T>
    {
        /// <summary>
        /// Gets the filtered RadzenDataGridColumn.
        /// </summary>
        public RadzenDataGridColumn<T> Column { get; internal set; }

        /// <summary>
        /// Gets the new filter value of the filtered column.
        /// </summary>
        public object FilterValue { get; internal set; }

        /// <summary>
        /// Gets the new second filter value of the filtered column.
        /// </summary>
        public object SecondFilterValue { get; internal set; }

        /// <summary>
        /// Gets the new filter operator of the filtered column.
        /// </summary>
        public FilterOperator FilterOperator { get; internal set; }

        /// <summary>
        /// Gets the new second filter operator of the filtered column.
        /// </summary>
        public FilterOperator SecondFilterOperator { get; internal set; }

        /// <summary>
        /// Gets the new logical filter operator of the filtered column.
        /// </summary>
        public LogicalFilterOperator LogicalFilterOperator { get; internal set; }
    }

    /// <summary>
    /// Supplies information about a <see cref="RadzenDataGrid{TItem}.ColumnResized" /> event that is being raised.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class DataGridColumnResizedEventArgs<T>
    {
        /// <summary>
        /// Gets the resized RadzenDataGridColumn.
        /// </summary>
        public RadzenDataGridColumn<T> Column { get; internal set; }

        /// <summary>
        /// Gets the new width of the resized column.
        /// </summary>
        public double Width { get; internal set; }
    }

    /// <summary>
    /// Supplies information about a <see cref="RadzenDataGrid{TItem}.ColumnReordered" /> event that is being raised.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class DataGridColumnReorderedEventArgs<T>
    {
        /// <summary>
        /// Gets the reordered RadzenDataGridColumn.
        /// </summary>
        public RadzenDataGridColumn<T> Column { get; internal set; }
        /// <summary>
        /// Gets the old index of the column.
        /// </summary>
        public int OldIndex { get; internal set; }
        /// <summary>
        /// Gets the new index of the column.
        /// </summary>
        public int NewIndex { get; internal set; }
    }

    /// <summary>
    /// Supplies information about a <see cref="RadzenGrid{TItem}.ColumnResized" /> event that is being raised.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ColumnResizedEventArgs<T>
    {
        /// <summary>
        /// Gets the resized RadzenGridColumn.
        /// </summary>
        public RadzenGridColumn<T> Column { get; internal set; }
        /// <summary>
        /// Gets the new width of the column.
        /// </summary>
        public double Width { get; internal set; }
    }

    /// <summary>
    /// Represents a filter in a component that supports filtering.
    /// </summary>
    public class FilterDescriptor
    {
        /// <summary>
        /// Gets or sets the name of the filtered property.
        /// </summary>
        /// <value>The property.</value>
        public string Property { get; set; }
        /// <summary>
        /// Gets or sets the value to filter by.
        /// </summary>
        /// <value>The filter value.</value>
        public object FilterValue { get; set; }
        /// <summary>
        /// Gets or sets the operator which will compare the property value with <see cref="FilterValue" />.
        /// </summary>
        /// <value>The filter operator.</value>
        public FilterOperator FilterOperator { get; set; }
        /// <summary>
        /// Gets or sets a second value to filter by.
        /// </summary>
        /// <value>The second filter value.</value>
        public object SecondFilterValue { get; set; }
        /// <summary>
        /// Gets or sets the operator which will compare the property value with <see cref="SecondFilterValue" />.
        /// </summary>
        /// <value>The second filter operator.</value>
        public FilterOperator SecondFilterOperator { get; set; }
        /// <summary>
        /// Gets or sets the logic used to combine the outcome of filtering by <see cref="FilterValue" /> and <see cref="SecondFilterValue" />.
        /// </summary>
        /// <value>The logical filter operator.</value>
        public LogicalFilterOperator LogicalFilterOperator { get; set; }
    }

    /// <summary>
    /// Represents a sorting description. Used in components that support sorting.
    /// </summary>
    public class SortDescriptor
    {
        /// <summary>
        /// Gets or sets the property to sort by.
        /// </summary>
        /// <value>The property.</value>
        public string Property { get; set; }
        /// <summary>
        /// Gets or sets the sort order.
        /// </summary>
        /// <value>The sort order.</value>
        public SortOrder? SortOrder { get; set; }
    }

    /// <summary>
    /// Represents a grouping description. Used in components that support grouping.
    /// </summary>
    public class GroupDescriptor
    {
        /// <summary>
        /// Gets or sets the property to group by.
        /// </summary>
        /// <value>The property.</value>
        public string Property { get; set; }

        /// <summary>
        /// Gets or sets the sort order.
        /// </summary>
        /// <value>The sort order.</value>
        public SortOrder? SortOrder { get; set; }

        /// <summary>
        /// Gets or sets the title displayed in the group.
        /// </summary>
        /// <value>The title.</value>
        public string Title { get; set; }

        /// <summary>
        /// Gets the title of the group.
        /// </summary>
        /// <returns>System.String.</returns>
        public string GetTitle()
        {
            return !string.IsNullOrEmpty(Title) ? Title : Property;
        }
    }

    /// <summary>
    /// Represents a group of data.
    /// </summary>
    public class Group
    {
        /// <summary>
        /// Gets or sets the data.
        /// </summary>
        /// <value>The data.</value>
        public GroupResult Data { get; set; }
        /// <summary>
        /// Gets or sets the group descriptor.
        /// </summary>
        /// <value>The group descriptor.</value>
        public GroupDescriptor GroupDescriptor { get; set; }
        /// <summary>
        /// Gets or sets the level.
        /// </summary>
        /// <value>The level.</value>
        public int Level { get; set; }
    }

    /// <summary>
    /// Supplies information about a <see cref="PagedDataBoundComponent{TItem}.LoadData" /> event that is being raised.
    /// </summary>
    public class LoadDataArgs
    {
        /// <summary>
        /// Gets how many items to skip. Related to paging and the current page. Usually used with the <see cref="Enumerable.Skip{TSource}(IEnumerable{TSource}, int)"/> LINQ method.
        /// </summary>
        public int? Skip { get; set; }
        /// <summary>
        /// Gets how many items to take. Related to paging and the current page size. Usually used with the <see cref="Enumerable.Take{TSource}(IEnumerable{TSource}, int)"/> LINQ method.
        /// </summary>
        /// <value>The top.</value>
        public int? Top { get; set; }
        /// <summary>
        /// Gets the sort expression as a string.
        /// </summary>
        public string OrderBy { get; set; }
        /// <summary>
        /// Gets the filter expression as a string.
        /// </summary>
        /// <value>The filter.</value>
        public string Filter { get; set; }
        /// <summary>
        /// Gets the filter expression as a collection of filter descriptors.
        /// </summary>
        public IEnumerable<FilterDescriptor> Filters { get; set; }
        /// <summary>
        /// Gets the sort expression as a collection of sort descriptors.
        /// </summary>
        /// <value>The sorts.</value>
        public IEnumerable<SortDescriptor> Sorts { get; set; }
    }

    /// <summary>
    /// Supplies information about a <see cref="RadzenPager.PageChanged" /> event that is being raised.
    /// </summary>
    public class PagerEventArgs
    {
        /// <summary>
        /// Gets how many items to skip.
        /// </summary>
        public int Skip { get; set; }
        /// <summary>
        /// Gets how many items to take.
        /// </summary>
        public int Top { get; set; }
        /// <summary>
        /// Gets the current zero-based page index
        /// </summary>
        public int PageIndex { get; set; }
    }

    /// <summary>
    /// Contains the commands which <see cref="RadzenHtmlEditor" /> supports.
    /// </summary>
    public static class HtmlEditorCommands
    {
        /// <summary>
        /// Inserts html at cursor location.
        /// </summary>
        public static string InsertHtml = "insertHtml";
        /// <summary>
        /// Centers the selected text.
        /// </summary>
        public static string AlignCenter = "justifyCenter";
        /// <summary>
        /// Aligns the selected text to the left.
        /// </summary>
        public static string AlignLeft = "justifyLeft";
        /// <summary>
        /// Aligns the selected text to the right.
        /// </summary>
        public static string AlignRight = "justifyRight";
        /// <summary>
        /// Sets the background color of the selected text.
        /// </summary>
        public static string Background = "backColor";
        /// <summary>
        /// Bolds the selected text.
        /// </summary>
        public static string Bold = "bold";
        /// <summary>
        /// Sets the text color of the selection.
        /// </summary>
        public static string Color = "foreColor";
        /// <summary>
        /// Sets the font of the selected text.
        /// </summary>
        public static string FontName = "fontName";
        /// <summary>
        /// Sets the font size of the selected text.
        /// </summary>
        public static string FontSize = "fontSize";
        /// <summary>
        /// Formats the selection as paragraph, heading etc.
        /// </summary>
        public static string FormatBlock = "formatBlock";
        /// <summary>
        /// Indents the selection.
        /// </summary>
        public static string Indent = "indent";
        /// <summary>
        /// Makes the selected text italic.
        /// </summary>
        public static string Italic = "italic";
        /// <summary>
        /// Justifies the selected text.
        /// </summary>
        public static string Justify = "justifyFull";
        /// <summary>
        /// Inserts an empty ordered list or makes an ordered list from the selected text.
        /// </summary>
        public static string OrderedList = "insertOrderedList";
        /// <summary>
        /// Outdents the selected text.
        /// </summary>
        public static string Outdent = "outdent";
        /// <summary>
        /// Repeats the last edit operations.
        /// </summary>
        public static string Redo = "redo";
        /// <summary>
        /// Removes visual formatting from the selected text.
        /// </summary>
        public static string RemoveFormat = "removeFormat";
        /// <summary>
        /// Strikes through the selected text.
        /// </summary>
        public static string StrikeThrough = "strikeThrough";
        /// <summary>
        /// Applies subscript styling to the selected text.
        /// </summary>
        public static string Subscript = "subscript";
        /// <summary>
        /// Applies superscript styling to the selected text.
        /// </summary>
        public static string Superscript = "superscript";
        /// <summary>
        /// Underlines the selected text.
        /// </summary>
        public static string Underline = "underline";
        /// <summary>
        /// Undoes the last edit operation.
        /// </summary>
        public static string Undo = "undo";
        /// <summary>
        /// Unlinks a link.
        /// </summary>
        public static string Unlink = "unlink";
        /// <summary>
        /// Inserts an empty unordered list or makes an unordered list from the selected text.
        /// </summary>
        public static string UnorderedList = "insertUnorderedList";
    }

    /// <summary>
    /// Supplies information about a <see cref="RadzenHtmlEditor.Paste" /> event that is being raised.
    /// </summary>
    public class HtmlEditorPasteEventArgs
    {
        /// <summary>
        /// Gets or sets the HTML content that is pasted in RadzenHtmlEditor. Use the setter to filter unwanted markup from the pasted value.
        /// </summary>
        /// <value>The HTML.</value>
        public string Html { get; set; }
    }

    /// <summary>
    /// Represents a data item in a <see cref="RadzenChart" />.
    /// </summary>
    public class SeriesPoint
    {
        /// <summary>
        /// Gets the category axis value.
        /// </summary>
        public double Category { get; set; }
        /// <summary>
        /// Gets the value axis value.
        /// </summary>
        public double Value { get; set; }
    }

    /// <summary>
    /// Supplies information about a <see cref="RadzenChart.SeriesClick" /> event that is being raised.
    /// </summary>
    public class SeriesClickEventArgs
    {
        /// <summary>
        /// Gets the data at the clicked location.
        /// </summary>
        public object Data { get; set; }
        /// <summary>
        /// Gets the value of the clicked location. Determined by <see cref="CartesianSeries{TItem}.ValueProperty" />.
        /// </summary>
        /// <value>The value.</value>
        public object Value { get; set; }
        /// <summary>
        /// Gets the category of the clicked location. Determined by <see cref="CartesianSeries{TItem}.CategoryProperty" />.
        /// </summary>
        /// <value>The category.</value>
        public object Category { get; set; }
        /// <summary>
        /// Gets the title of the clicked series. Determined by <see cref="CartesianSeries{TItem}.Title" />.
        /// </summary>
        /// <value>The title.</value>
        public string Title { get; set; }
        /// <summary>
        /// Gets the clicked point in axis coordinates.
        /// </summary>
        public SeriesPoint Point { get; set; }
    }

    /// <summary>
    /// Supplies information about a <see cref="RadzenHtmlEditor.Execute" /> event that is being raised.
    /// </summary>
    public class HtmlEditorExecuteEventArgs
    {
        /// <summary>
        /// Gets the RadzenHtmlEditor instance which raised the event.
        /// </summary>
        public RadzenHtmlEditor Editor { get; set; }

        internal HtmlEditorExecuteEventArgs(RadzenHtmlEditor editor)
        {
            Editor = editor;
        }

        /// <summary>
        /// Gets the name of the command which RadzenHtmlEditor is executing.
        /// </summary>
        public string CommandName { get; set; }
    }

    /// <summary>
    /// Supplies information about a <see cref="RadzenTree.Change" /> event that is being raised.
    /// </summary>
    public class TreeEventArgs
    {
        /// <summary>
        /// Gets the <see cref="RadzenTreeItem.Text" /> the selected RadzenTreeItem.
        /// </summary>
        public string Text { get; set; }
        /// <summary>
        /// Gets the <see cref="RadzenTreeItem.Value" /> the selected RadzenTreeItem.
        /// </summary>
        public object Value { get; set; }
    }

    /// <summary>
    /// Supplies information about a <see cref="RadzenTree.Expand" /> event that is being raised.
    /// </summary>
    public class TreeExpandEventArgs
    {
        /// <summary>
        /// Gets the <see cref="RadzenTreeItem.Text" /> the expanded RadzenTreeItem.
        /// </summary>
        public object Value { get; set; }
        /// <summary>
        /// Gets the <see cref="RadzenTreeItem.Value" /> the expanded RadzenTreeItem.
        /// </summary>
        public string Text { get; set; }
        /// <summary>
        /// Gets or sets the children of the expanded RadzenTreeItem.
        /// </summary>
        /// <value>The children.</value>
        public TreeItemSettings Children { get; set; }
    }

    /// <summary>
    /// The configuration used by a <see cref="RadzenTreeItem" /> to create child items.
    /// </summary>
    public class TreeItemSettings
    {
        /// <summary>
        /// Gets or sets the data from which child items will be created. The parent node enumerates the data and creates a new RadzenTreeItem for every item.
        /// </summary>
        public IEnumerable Data { get; set; }
        /// <summary>
        /// Gets or sets the function which returns a value for the <see cref="RadzenTreeItem.Text" /> of a child item.
        /// </summary>
        public Func<object, string> Text { get; set; }
        /// <summary>
        /// Gets or sets the name of the property which provides the value for the <see cref="RadzenTreeItem.Text" /> of a child item.
        /// </summary>
        public string TextProperty { get; set; }
        /// <summary>
        /// Gets or sets a function which returns whether a child item has children of its own. Called with an item from <see cref="Data" />.
        /// By default all items are considered to have children.
        /// </summary>
        public Func<object, bool> HasChildren { get; set; } = value => true;
        /// <summary>
        /// Gets or sets a function which determines whether a child item is expanded. Called with an item from <see cref="Data" />.
        /// By default all items are collapsed.
        /// </summary>
        public Func<object, bool> Expanded { get; set; } = value => false;
        /// <summary>
        /// Gets or sets a function which determines whether a child item is selected. Called with an item from <see cref="Data" />.
        /// By default all items are not selected.
        /// </summary>
        public Func<object, bool> Selected { get; set; } = value => false;
        /// <summary>
        /// Gets or sets the <see cref="RadzenTreeItem.Template" /> of a child item.
        /// </summary>
        /// <value>The template.</value>
        public RenderFragment<RadzenTreeItem> Template { get; set; }
    }

    /// <summary>
    /// Supplies information about a <see cref="RadzenLogin.Login" /> event that is being raised.
    /// </summary>
    public class LoginArgs
    {
        /// <summary>
        /// Gets or sets the username.
        /// </summary>
        public string Username { get; set; }
        /// <summary>
        /// Gets or sets the password.
        /// </summary>
        public string Password { get; set; }
    }

    /// <summary>
    /// Converts values to different types. Used internally.
    /// </summary>
    public static class ConvertType
    {
        /// <summary>
        /// Changes the type of an object.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="type">The type.</param>
        /// <returns>System.Object</returns>
        public static object ChangeType(object value, Type type)
        {
            if (value == null && Nullable.GetUnderlyingType(type) != null)
            {
                return value;
            }

            if ((Nullable.GetUnderlyingType(type) ?? type) == typeof(Guid) && value is string)
            {
                return Guid.Parse((string)value);
            }

            if (Nullable.GetUnderlyingType(type)?.IsEnum == true)
            {
                return Enum.Parse(Nullable.GetUnderlyingType(type), value.ToString());
            }
            
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                Type itemType = type.GetGenericArguments()[0];
                var enumerable = value as IEnumerable<object>;

                if (enumerable != null)
                {
                    return enumerable.AsQueryable().Cast(itemType);
                }

            }

            return value is IConvertible ? Convert.ChangeType(value, Nullable.GetUnderlyingType(type) ?? type) : value;
        }
    }

    /// <summary>
    /// Utility class that provides property access based on strings.
    /// </summary>
    public static class PropertyAccess
    {
        /// <summary>
        /// Creates a function that will return the specified property.
        /// </summary>
        /// <typeparam name="TItem">The owner type.</typeparam>
        /// <typeparam name="TValue">The value type.</typeparam>
        /// <param name="propertyName">Name of the property to return.</param>
        /// <returns>A function which return the specified property by its name.</returns>
        public static Func<TItem, TValue> Getter<TItem, TValue>(string propertyName)
        {
            var arg = Expression.Parameter(typeof(TItem));

            Expression body = arg;

            foreach (var member in propertyName.Split("."))
            {
                body = Expression.PropertyOrField(body, member);
            }

            body = Expression.Convert(body, typeof(TValue));

            return Expression.Lambda<Func<TItem, TValue>>(body, arg).Compile();
        }

        /// <summary>
        /// Determines whether the specified type is a <see cref="DateTime" />.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <returns><c>true</c> if the specified type is a DateTime instance or nullable DateTime; otherwise, <c>false</c>.</returns>
        public static bool IsDate(Type source)
        {
            if (source == null) return false;
            var type = source.IsGenericType ? source.GetGenericArguments()[0] : source;

            if (type == typeof(DateTime) || type == typeof(DateTimeOffset))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Gets the type of the element of a collection time.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>The type of the collection element.</returns>
        public static Type GetElementType(Type type)
        {
            if (type.IsArray)
            {
                return type.GetElementType();
            }

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                return type.GetGenericArguments()[0];
            }

            var enumType = type.GetInterfaces()
                                    .Where(t => t.IsGenericType &&
                                           t.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                                    .Select(t => t.GenericTypeArguments[0]).FirstOrDefault();
            return enumType ?? type;
        }

        /// <summary>
        /// Converts the property to a value that can be used by Dynamic LINQ.
        /// </summary>
        /// <param name="property">The property.</param>
        public static string GetProperty(string property)
        {
            var type = Type.GetType($"System.{property}");
            var propertyName = $"{(type != null ? "@" : "")}{property}";

            if (propertyName.IndexOf(".") != -1)
            {
                return $"np({propertyName})";
            }

            return propertyName;
        }

        /// <summary>
        /// Gets the value of the specified expression via reflection.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="path">The path.</param>
        /// <returns>The value of the specified expression or <paramref name="value"/> if not found.</returns>
        public static object GetValue(object value, string path)
        {
            Type currentType = value.GetType();

            foreach (string propertyName in path.Split('.'))
            {
                var property = currentType.GetProperty(propertyName);
                if (property != null)
                {
                    if (value != null)
                    {
                        value = property.GetValue(value, null);
                    }

                    currentType = property.PropertyType;
                }
            }
            return value;
        }
        /// <summary>
        /// Creates a function that returns the specified property.
        /// </summary>
        /// <typeparam name="T">The value type</typeparam>
        /// <param name="data">The value.</param>
        /// <param name="propertyName">The name of the property to return.</param>
        /// <returns>A function that returns the specified property.</returns>
        public static Func<object, T> Getter<T>(object data, string propertyName)
        {
            var type = data.GetType();
            var arg = Expression.Parameter(typeof(object));
            var body = Expression.Property(Expression.Convert(arg, type), propertyName);

            return Expression.Lambda<Func<object, T>>(body, arg).Compile();
        }

        /// <summary>
        /// Tries to get a property by its name.
        /// </summary>
        /// <typeparam name="T">The target type</typeparam>
        /// <param name="item">The item.</param>
        /// <param name="property">The property.</param>
        /// <param name="result">The property value.</param>
        /// <returns><c>true</c> if successful, <c>false</c> otherwise.</returns>
        public static bool TryGetItemOrValueFromProperty<T>(object item, string property, out T result)
        {
            object r = GetItemOrValueFromProperty(item, property);

            if (r != null)
            {
                result = (T)r;
                return true;
            }
            else
            {
                result = default;
                return false;
            }

        }

        /// <summary>
        /// Gets the item or value from property.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="property">The property.</param>
        /// <returns>System.Object.</returns>
        public static object GetItemOrValueFromProperty(object item, string property)
        {
            if (item == null)
            {
                return null;
            }

            if (Convert.GetTypeCode(item) != TypeCode.Object || string.IsNullOrEmpty(property))
            {
                return item;
            }

            return PropertyAccess.GetValue(item, property);
        }

        /// <summary>
        /// Determines whether the specified type is numeric.
        /// </summary>
        /// <param name="source">The type.</param>
        /// <returns><c>true</c> if the specified source is numeric; otherwise, <c>false</c>.</returns>
        public static bool IsNumeric(Type source)
        {
            if (source == null)
                return false;

            var type = source.IsGenericType ? source.GetGenericArguments()[0] : source;

            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Single:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Determines whether the specified type is an enum.
        /// </summary>
        /// <param name="source">The type.</param>
        /// <returns><c>true</c> if the specified source is an enum; otherwise, <c>false</c>.</returns>
        public static bool IsEnum(Type source)
        {
            if (source == null)
                return false;

            return source.IsEnum;
        }

        /// <summary> 
        /// Determines whether the specified type is a Nullable enum. 
        /// </summary> 
        /// <param name="source">The type.</param> 
        /// <returns><c>true</c> if the specified source is an enum; otherwise, <c>false</c>.</returns> 
        public static bool IsNullableEnum(Type source)
        {
            if (source == null) return false;
            Type u = Nullable.GetUnderlyingType(source);
            return (u != null) && u.IsEnum;
        }

        /// <summary>
        /// Determines whether the specified type is anonymous.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns><c>true</c> if the specified type is anonymous; otherwise, <c>false</c>.</returns>
        public static bool IsAnonymous(this Type type)
        {
            if (type.IsGenericType)
            {
                var d = type.GetGenericTypeDefinition();
                if (d.IsClass && d.IsSealed && d.Attributes.HasFlag(System.Reflection.TypeAttributes.NotPublic))
                {
                    var attributes = d.GetCustomAttributes(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), false);
                    if (attributes != null && attributes.Length > 0)
                    {
                        return true;
                    }
                }
            }
            return false;
        }


        /// <summary>
        /// Gets the type of the property.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="property">The property.</param>
        /// <returns>Type.</returns>
        public static Type GetPropertyType(Type type, string property)
        {
            if (property.Contains("."))
            {
                var part = property.Split('.').FirstOrDefault();
                return GetPropertyType(type?.GetProperty(part)?.PropertyType, property.Replace($"{part}.", ""));
            }

            return type?.GetProperty(property)?.PropertyType;
        }
    }

    /// <summary>
    /// Represents the common <see cref="RadzenTemplateForm{TItem}" /> API used by 
    /// its items. Injected as a cascading property in <see cref="IRadzenFormComponent" />.
    /// </summary>
    public interface IRadzenForm
    {
        /// <summary>
        /// Adds the specified component to the form.
        /// </summary>
        /// <param name="component">The component to add to the form.</param>
        void AddComponent(IRadzenFormComponent component);
        /// <summary>
        /// Removes the component from the form.
        /// </summary>
        /// <param name="component">The component to remove from the form.</param>
        void RemoveComponent(IRadzenFormComponent component);
        /// <summary>
        /// Finds a form component by its name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>The component whose <see cref="IRadzenFormComponent.Name" /> equals to <paramref name="name" />; <c>null</c> if such a component is not found.</returns>
        IRadzenFormComponent FindComponent(string name);
    }

    /// <summary>
    /// Specifies the interface that form components must implement in order to be supported by <see cref="RadzenTemplateForm{TItem}" />.
    /// </summary>
    public interface IRadzenFormComponent
    {
        /// <summary>
        /// Gets a value indicating whether this component is bound.
        /// </summary>
        /// <value><c>true</c> if this component is bound; otherwise, <c>false</c>.</value>
        bool IsBound { get; }
        /// <summary>
        /// Gets a value indicating whether the component has value.
        /// </summary>
        /// <value><c>true</c> if this instance has value; otherwise, <c>false</c>.</value>
        bool HasValue { get; }

        /// <summary>
        /// Gets the value of the component.
        /// </summary>
        /// <returns>the value of the component - for example the text of RadzenTextBox.</returns>
        object GetValue();

        /// <summary>
        /// Gets or sets the name of the component.
        /// </summary>
        /// <value>The name.</value>
        string Name { get; set; }

        /// <summary>
        /// Gets the field identifier.
        /// </summary>
        /// <value>The field identifier.</value>
        FieldIdentifier FieldIdentifier { get; }
    }

    /// <summary>
    /// Supplies information about a <see cref="RadzenTemplateForm{TItem}.InvalidSubmit" /> event that is being raised.
    /// </summary>
    public class FormInvalidSubmitEventArgs
    {
        /// <summary>
        /// Gets the validation errors.
        /// </summary>
        public IEnumerable<string> Errors { get; set; }
    }

    /// <summary>
    /// The interface that a validator component must implement in order to be supported by <see cref="RadzenTemplateForm{TItem}" />.
    /// </summary>
    public interface IRadzenFormValidator
    {
        /// <summary>
        /// Returns true if valid.
        /// </summary>
        /// <value><c>true</c> if the validator is valid; otherwise, <c>false</c>.</value>
        bool IsValid { get; }
        /// <summary>
        /// Gets or sets the name of the component which is validated.
        /// </summary>
        /// <value>The component name.</value>
        string Component { get; set; }
        /// <summary>
        /// Gets or sets the validation error displayed when invalid.
        /// </summary>
        /// <value>The text to display when invalid.</value>
        string Text { get; set; }
    }

    /// <summary>
    /// A base class of components that have child content.
    /// </summary>
    public class RadzenComponentWithChildren : RadzenComponent
    {
        /// <summary>
        /// Gets or sets the child content
        /// </summary>
        /// <value>The content of the child.</value>
        [Parameter]
        public RenderFragment ChildContent { get; set; }
    }

    class Debouncer
    {
        System.Timers.Timer timer;
        DateTime timerStarted { get; set; } = DateTime.UtcNow.AddYears(-1);

        public void Debounce(int interval, Func<Task> action)
        {
            timer?.Stop();
            timer = null;

            timer = new System.Timers.Timer() { Interval = interval, Enabled = false, AutoReset = false };
            timer.Elapsed += (s, e) =>
            {
                if (timer == null)
                {
                    return;
                }

                timer?.Stop();
                timer = null;

                try
                {
                    Task.Run(action);
                }
                catch (TaskCanceledException)
                {
                    //
                }
            };

            timer.Start();
        }

        public void Throttle(int interval, Func<Task> action)
        {
            timer?.Stop();
            timer = null;

            var curTime = DateTime.UtcNow;

            if (curTime.Subtract(timerStarted).TotalMilliseconds < interval)
            {
                interval -= (int)curTime.Subtract(timerStarted).TotalMilliseconds;
            }

            timer = new System.Timers.Timer() { Interval = interval, Enabled = false, AutoReset = false };
            timer.Elapsed += (s, e) =>
            {
                if (timer == null)
                {
                    return;
                }

                timer?.Stop();
                timer = null;

                try
                {
                    Task.Run(action);
                }
                catch (TaskCanceledException)
                {
                    //
                }
            };

            timer.Start();
            timerStarted = curTime;
        }
    }

    /// <summary>
    /// Contains extension methods for <see cref="ParameterView" />.
    /// </summary>
    public static class ParameterViewExtensions
    {
        /// <summary>
        /// Checks if a parameter changed.
        /// </summary>
        /// <typeparam name="T">The value type</typeparam>
        /// <param name="parameters">The parameters.</param>
        /// <param name="parameterName">Name of the parameter.</param>
        /// <param name="parameterValue">The parameter value.</param>
        /// <returns><c>true</c> if the parameter value has changed, <c>false</c> otherwise.</returns>
        public static bool DidParameterChange<T>(this ParameterView parameters, string parameterName, T parameterValue)
        {
            if (parameters.TryGetValue(parameterName, out T value))
            {
                return !EqualityComparer<T>.Default.Equals(value, parameterValue);
            }

            return false;
        }
    }

    /// <summary>
    /// Supplies information about a <see cref="RadzenSplitter.Expand" /> or <see cref="RadzenSplitter.Collapse" /> event that is being raised.
    /// </summary>
    public class RadzenSplitterEventArgs
    {
        /// <summary>
        /// Gets the index of the pane.
        /// </summary>
        public int PaneIndex { get; set; }
        /// <summary>
        /// Gets the pane which the event applies to.
        /// </summary>
        /// <value>The pane.</value>
        public RadzenSplitterPane Pane { get; set; }
        /// <summary>
        /// Gets or sets a value which will cancel the event.
        /// </summary>
        /// <value><c>true</c> to cancel the event; otherwise, <c>false</c>.</value>
        public bool Cancel { get; set; }
    }

    /// <summary>
    /// Supplies information about a <see cref="RadzenSplitter.Resize" /> event that is being raised.
    /// </summary>
    public class RadzenSplitterResizeEventArgs : RadzenSplitterEventArgs
    {
        /// <summary>
        /// The new size of the pane
        /// </summary>
        public double NewSize { get; set; }
    }

    /// <inheritdoc />
    public class MD5
    {
        /*
         * Round shift values
         */
        static int[] s = new int[64] {
            7, 12, 17, 22,  7, 12, 17, 22,  7, 12, 17, 22,  7, 12, 17, 22,
            5,  9, 14, 20,  5,  9, 14, 20,  5,  9, 14, 20,  5,  9, 14, 20,
            4, 11, 16, 23,  4, 11, 16, 23,  4, 11, 16, 23,  4, 11, 16, 23,
            6, 10, 15, 21,  6, 10, 15, 21,  6, 10, 15, 21,  6, 10, 15, 21
        };

        /*
         * Constant K Values
         */
        static uint[] K = new uint[64] {
            0xd76aa478, 0xe8c7b756, 0x242070db, 0xc1bdceee,
            0xf57c0faf, 0x4787c62a, 0xa8304613, 0xfd469501,
            0x698098d8, 0x8b44f7af, 0xffff5bb1, 0x895cd7be,
            0x6b901122, 0xfd987193, 0xa679438e, 0x49b40821,
            0xf61e2562, 0xc040b340, 0x265e5a51, 0xe9b6c7aa,
            0xd62f105d, 0x02441453, 0xd8a1e681, 0xe7d3fbc8,
            0x21e1cde6, 0xc33707d6, 0xf4d50d87, 0x455a14ed,
            0xa9e3e905, 0xfcefa3f8, 0x676f02d9, 0x8d2a4c8a,
            0xfffa3942, 0x8771f681, 0x6d9d6122, 0xfde5380c,
            0xa4beea44, 0x4bdecfa9, 0xf6bb4b60, 0xbebfbc70,
            0x289b7ec6, 0xeaa127fa, 0xd4ef3085, 0x04881d05,
            0xd9d4d039, 0xe6db99e5, 0x1fa27cf8, 0xc4ac5665,
            0xf4292244, 0x432aff97, 0xab9423a7, 0xfc93a039,
            0x655b59c3, 0x8f0ccc92, 0xffeff47d, 0x85845dd1,
            0x6fa87e4f, 0xfe2ce6e0, 0xa3014314, 0x4e0811a1,
            0xf7537e82, 0xbd3af235, 0x2ad7d2bb, 0xeb86d391
        };

        /// <inheritdoc />
        public static uint leftRotate(uint x, int c)
        {
            return (x << c) | (x >> (32 - c));
        }

        // assumes whole bytes as input
        /// <inheritdoc />
        public static string Calculate(byte[] input)
        {
            uint a0 = 0x67452301;   // A
            uint b0 = 0xefcdab89;   // B
            uint c0 = 0x98badcfe;   // C
            uint d0 = 0x10325476;   // D

            var addLength = (56 - ((input.Length + 1) % 64)) % 64; // calculate the new length with padding
            var processedInput = new byte[input.Length + 1 + addLength + 8];
            Array.Copy(input, processedInput, input.Length);
            processedInput[input.Length] = 0x80; // add 1

            byte[] length = BitConverter.GetBytes(input.Length * 8); // bit converter returns little-endian
            Array.Copy(length, 0, processedInput, processedInput.Length - 8, 4); // add length in bits

            for (int i = 0; i < processedInput.Length / 64; ++i)
            {
                // copy the input to M
                uint[] M = new uint[16];
                for (int j = 0; j < 16; ++j)
                    M[j] = BitConverter.ToUInt32(processedInput, (i * 64) + (j * 4));

                // initialize round variables
                uint A = a0, B = b0, C = c0, D = d0, F = 0, g = 0;

                // primary loop
                for (uint k = 0; k < 64; ++k)
                {
                    if (k <= 15)
                    {
                        F = (B & C) | (~B & D);
                        g = k;
                    }
                    else if (k >= 16 && k <= 31)
                    {
                        F = (D & B) | (~D & C);
                        g = ((5 * k) + 1) % 16;
                    }
                    else if (k >= 32 && k <= 47)
                    {
                        F = B ^ C ^ D;
                        g = ((3 * k) + 5) % 16;
                    }
                    else if (k >= 48)
                    {
                        F = C ^ (B | ~D);
                        g = (7 * k) % 16;
                    }

                    var dtemp = D;
                    D = C;
                    C = B;
                    B = B + leftRotate((A + F + K[k] + M[g]), s[k]);
                    A = dtemp;
                }

                a0 += A;
                b0 += B;
                c0 += C;
                d0 += D;
            }

            return GetByteString(a0) + GetByteString(b0) + GetByteString(c0) + GetByteString(d0);
        }

        private static string GetByteString(uint x)
        {
            return String.Join("", BitConverter.GetBytes(x).Select(y => y.ToString("x2")));
        }
    }
}