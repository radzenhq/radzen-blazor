using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
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
    /// Class EventHandlers.
    /// </summary>
    [EventHandler("onmouseenter", typeof(EventArgs), true, true)]
    [EventHandler("onmouseleave", typeof(EventArgs), true, true)]
    public static class EventHandlers
    {
    }

    /// <summary>
    /// Interface IRadzenSelectBar
    /// </summary>
    public interface IRadzenSelectBar
    {
        /// <summary>
        /// Adds the item.
        /// </summary>
        /// <param name="item">The item.</param>
        void AddItem(RadzenSelectBarItem item);
        /// <summary>
        /// Removes the item.
        /// </summary>
        /// <param name="item">The item.</param>
        void RemoveItem(RadzenSelectBarItem item);
    }

    /// <summary>
    /// RadzenDatePicker DateRender event arguments.<br />
    /// </summary>
    public class DateRenderEventArgs
    {
        /// <summary>
        /// Gets or sets the attributes that will be applied when RadzenDatePicker date cell is rendered.
        /// </summary>
        /// <value>The attributes.</value>
        public IDictionary<string, object> Attributes { get; private set; } = new Dictionary<string, object>();
        /// <summary>
        /// Gets the date.
        /// </summary>
        /// <value>The date.</value>
        public DateTime Date { get; internal set; }
        /// <summary>
        /// Gets or sets a value indicating whether this date is disabled.
        /// </summary>
        /// <value><c>true</c> if disabled; otherwise, <c>false</c>.</value>
        public bool Disabled { get; set; }
    }

    /// <summary>
    /// Class used to specify RadzenGoogleMap position.
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
    /// RadzenGoogleMap Click event arguments.
    /// </summary>
    public class GoogleMapClickEventArgs
    {
        /// <summary>
        /// Gets or sets the position.
        /// </summary>
        /// <value>The position.</value>
        public GoogleMapPosition Position { get; set; }
    }

    /// <summary>
    /// RadzenContextMenu Click event arguments.
    /// </summary>
    public class MenuItemEventArgs
    {
        /// <summary>
        /// Gets the item text.
        /// </summary>
        /// <value>The text.</value>
        public string Text { get; internal set; }
        /// <summary>
        /// Gets the item value.
        /// </summary>
        /// <value>The value.</value>
        public object Value { get; internal set; }
        /// <summary>
        /// Gets the item path.
        /// </summary>
        /// <value>The path.</value>
        public string Path { get; internal set; }
    }

    /// <summary>
    /// RadzenUpload and RadzenFileInput Error event arguments.
    /// </summary>
    public class UploadErrorEventArgs
    {
        /// <summary>
        /// Gets or sets the message.
        /// </summary>
        /// <value>The message.</value>
        public string Message { get; set; }
    }

    /// <summary>
    /// RadzenDataGrid RowRender event arguments.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class RowRenderEventArgs<T>
    {
        /// <summary>
        /// Gets or sets the row attributes.
        /// </summary>
        /// <value>The attributes.</value>
        public IDictionary<string, object> Attributes { get; private set; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets the row data item.
        /// </summary>
        /// <value>The data.</value>
        public T Data { get; internal set; }

        /// <summary>
        /// Gets or sets a value indicating whether this row is expandable.
        /// </summary>
        /// <value><c>true</c> if expandable; otherwise, <c>false</c>.</value>
        public bool Expandable { get; set; }
    }

    /// <summary>
    /// RadzenGrid Render event arguments.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class GridRenderEventArgs<T>
    {
        /// <summary>
        /// Gets the grid.
        /// </summary>
        /// <value>The grid.</value>
        public RadzenGrid<T> Grid { get; internal set; }
        /// <summary>
        /// Gets a value indicating whether is first render.
        /// </summary>
        /// <value><c>true</c> if [first render]; otherwise, <c>false</c>.</value>
        public bool FirstRender { get; internal set; }
    }

    /// <summary>
    /// RadzenDataGrid Render event arguments.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class DataGridRenderEventArgs<T>
    {
        /// <summary>
        /// Gets the grid.
        /// </summary>
        /// <value>The grid.</value>
        public RadzenDataGrid<T> Grid { get; internal set; }
        /// <summary>
        /// Gets a value indicating whether is first render.
        /// </summary>
        /// <value><c>true</c> if [first render]; otherwise, <c>false</c>.</value>
        public bool FirstRender { get; internal set; }
    }

    /// <summary>
    /// RadzenGrid CellRender event arguments.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class CellRenderEventArgs<T> : RowRenderEventArgs<T>
    {
        /// <summary>
        /// Gets the column.
        /// </summary>
        /// <value>The column.</value>
        public Blazor.RadzenGridColumn<T> Column { get; internal set; }
    }

    /// <summary>
    /// RadzenDataGrid CellRender event arguments.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class DataGridCellRenderEventArgs<T> : RowRenderEventArgs<T>
    {
        /// <summary>
        /// Gets the column.
        /// </summary>
        /// <value>The column.</value>
        public Blazor.RadzenDataGridColumn<T> Column { get; internal set; }
    }


    /// <summary>
    /// RadzenDataGrid RowClick and RowDoubleClick events arguments.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class DataGridRowMouseEventArgs<T> : Microsoft.AspNetCore.Components.Web.MouseEventArgs
    {
        /// <summary>
        /// Gets the row data item.
        /// </summary>
        /// <value>The data.</value>
        public T Data { get; internal set; }
    }

    /// <summary>
    /// RadzenUpload Change event arguments.
    /// </summary>
    public class UploadChangeEventArgs
    {
        /// <summary>
        /// Gets the files.
        /// </summary>
        /// <value>The files.</value>
        public IEnumerable<FileInfo> Files { get; set; }
    }

    /// <summary>
    /// RadzenUpload Progress event arguments.
    /// </summary>
    public class UploadProgressArgs
    {
        /// <summary>
        /// Gets or sets the number of loaded bytes.
        /// </summary>
        /// <value>The loaded bytes.</value>
        public int Loaded { get; set; }
        /// <summary>
        /// Gets the total bytes.
        /// </summary>
        /// <value>The total bytes.</value>
        public int Total { get; set; }
        /// <summary>
        /// Gets the progress.
        /// </summary>
        /// <value>The progress.</value>
        public int Progress { get; set; }

        /// <summary>
        /// Gets the files.
        /// </summary>
        /// <value>The files.</value>
        public IEnumerable<FileInfo> Files { get; set; }
    }

    /// <summary>
    /// RadzenUpload Complete event arguments.
    /// </summary>
    public class UploadCompleteEventArgs
    {
        /// <summary>
        /// Gets the json response.
        /// </summary>
        /// <value>The json response.</value>
        public JsonDocument JsonResponse { get; set; }

        /// <summary>
        /// Gets the raw response.
        /// </summary>
        /// <value>The raw response.</value>
        public string RawResponse { get; set; }
    }

    /// <summary>
    /// Class FileInfo.
    /// </summary>
    public class FileInfo
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }
        /// <summary>
        /// Gets or sets the size.
        /// </summary>
        /// <value>The size.</value>
        public int Size { get; set; }
    }

    /// <summary>
    /// Class PreviewFileInfo.
    /// Implements the <see cref="Radzen.FileInfo" />
    /// </summary>
    /// <seealso cref="Radzen.FileInfo" />
    public class PreviewFileInfo : FileInfo
    {
        /// <summary>
        /// Gets or sets the URL.
        /// </summary>
        /// <value>The URL.</value>
        public string Url { get; set; }
    }

    /// <summary>
    /// Class Query.
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
        /// Converts to url.
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
    /// Enum TabRenderMode
    /// </summary>
    public enum TabRenderMode
    {
        /// <summary>
        /// The server
        /// </summary>
        Server,
        /// <summary>
        /// The client
        /// </summary>
        Client
    }

    /// <summary>
    /// Enum PagerPosition
    /// </summary>
    public enum PagerPosition
    {
        /// <summary>
        /// The top
        /// </summary>
        Top,
        /// <summary>
        /// The bottom
        /// </summary>
        Bottom,
        /// <summary>
        /// The top and bottom
        /// </summary>
        TopAndBottom
    }

    /// <summary>
    /// Enum DataGridExpandMode
    /// </summary>
    public enum DataGridExpandMode
    {
        /// <summary>
        /// The single
        /// </summary>
        Single,
        /// <summary>
        /// The multiple
        /// </summary>
        Multiple
    }

    /// <summary>
    /// Enum DataGridEditMode
    /// </summary>
    public enum DataGridEditMode
    {
        /// <summary>
        /// The single
        /// </summary>
        Single,
        /// <summary>
        /// The multiple
        /// </summary>
        Multiple
    }

    /// <summary>
    /// Enum DataGridSelectionMode
    /// </summary>
    public enum DataGridSelectionMode
    {
        /// <summary>
        /// The single
        /// </summary>
        Single,
        /// <summary>
        /// The multiple
        /// </summary>
        Multiple
    }

    /// <summary>
    /// Enum NotificationSeverity
    /// </summary>
    public enum NotificationSeverity
    {
        /// <summary>
        /// The error
        /// </summary>
        Error,
        /// <summary>
        /// The information
        /// </summary>
        Info,
        /// <summary>
        /// The success
        /// </summary>
        Success,
        /// <summary>
        /// The warning
        /// </summary>
        Warning
    }

    /// <summary>
    /// Enum ProgressBarMode
    /// </summary>
    public enum ProgressBarMode
    {
        /// <summary>
        /// The determinate
        /// </summary>
        Determinate,
        /// <summary>
        /// The indeterminate
        /// </summary>
        Indeterminate
    }

    /// <summary>
    /// Enum Orientation
    /// </summary>
    public enum Orientation
    {
        /// <summary>
        /// The horizontal
        /// </summary>
        Horizontal,
        /// <summary>
        /// The vertical
        /// </summary>
        Vertical
    }

    /// <summary>
    /// Enum SortOrder
    /// </summary>
    public enum SortOrder
    {
        /// <summary>
        /// The ascending
        /// </summary>
        Ascending,
        /// <summary>
        /// The descending
        /// </summary>
        Descending
    }

    /// <summary>
    /// Enum ButtonType
    /// </summary>
    public enum ButtonType
    {
        /// <summary>
        /// The button
        /// </summary>
        Button,
        /// <summary>
        /// The submit
        /// </summary>
        Submit,
        /// <summary>
        /// The reset
        /// </summary>
        Reset
    }

    /// <summary>
    /// Enum ButtonSize
    /// </summary>
    public enum ButtonSize
    {
        /// <summary>
        /// The medium
        /// </summary>
        Medium,
        /// <summary>
        /// The small
        /// </summary>
        Small
    }

    /// <summary>
    /// Enum ButtonStyle
    /// </summary>
    public enum ButtonStyle
    {
        /// <summary>
        /// The primary
        /// </summary>
        Primary,
        /// <summary>
        /// The secondary
        /// </summary>
        Secondary,
        /// <summary>
        /// The light
        /// </summary>
        Light,
        /// <summary>
        /// The success
        /// </summary>
        Success,
        /// <summary>
        /// The danger
        /// </summary>
        Danger,
        /// <summary>
        /// The warning
        /// </summary>
        Warning,
        /// <summary>
        /// The information
        /// </summary>
        Info
    }

    /// <summary>
    /// Enum FilterMode
    /// </summary>
    public enum FilterMode
    {
        /// <summary>
        /// The simple
        /// </summary>
        Simple,
        /// <summary>
        /// The advanced
        /// </summary>
        Advanced
    }

    /// <summary>
    /// Enum FilterCaseSensitivity
    /// </summary>
    public enum FilterCaseSensitivity
    {
        /// <summary>
        /// The default
        /// </summary>
        Default,
        /// <summary>
        /// The case insensitive
        /// </summary>
        CaseInsensitive
    }

    /// <summary>
    /// Enum LogicalFilterOperator
    /// </summary>
    public enum LogicalFilterOperator
    {
        /// <summary>
        /// The and
        /// </summary>
        And,
        /// <summary>
        /// The or
        /// </summary>
        Or
    }

    /// <summary>
    /// Enum StringFilterOperator
    /// </summary>
    public enum StringFilterOperator
    {
        /// <summary>
        /// Determines whether this instance contains the object.
        /// </summary>
        Contains,
        /// <summary>
        /// The starts with
        /// </summary>
        StartsWith,
        /// <summary>
        /// The ends with
        /// </summary>
        EndsWith
    }

    /// <summary>
    /// Enum FilterOperator
    /// </summary>
    public enum FilterOperator
    {
        /// <summary>
        /// The equals
        /// </summary>
        Equals,
        /// <summary>
        /// The not equals
        /// </summary>
        NotEquals,
        /// <summary>
        /// The less than
        /// </summary>
        LessThan,
        /// <summary>
        /// The less than or equals
        /// </summary>
        LessThanOrEquals,
        /// <summary>
        /// The greater than
        /// </summary>
        GreaterThan,
        /// <summary>
        /// The greater than or equals
        /// </summary>
        GreaterThanOrEquals,
        /// <summary>
        /// Determines whether this instance contains the object.
        /// </summary>
        Contains,
        /// <summary>
        /// The starts with
        /// </summary>
        StartsWith,
        /// <summary>
        /// The ends with
        /// </summary>
        EndsWith,
        /// <summary>
        /// The does not contain
        /// </summary>
        DoesNotContain
    }

    /// <summary>
    /// Enum TextAlign
    /// </summary>
    public enum TextAlign
    {
        /// <summary>
        /// The left
        /// </summary>
        Left,
        /// <summary>
        /// The right
        /// </summary>
        Right,
        /// <summary>
        /// The center
        /// </summary>
        Center
    }

    /// <summary>
    /// Enum BadgeStyle
    /// </summary>
    public enum BadgeStyle
    {
        /// <summary>
        /// The primary
        /// </summary>
        Primary,
        /// <summary>
        /// The secondary
        /// </summary>
        Secondary,
        /// <summary>
        /// The light
        /// </summary>
        Light,
        /// <summary>
        /// The success
        /// </summary>
        Success,
        /// <summary>
        /// The danger
        /// </summary>
        Danger,
        /// <summary>
        /// The warning
        /// </summary>
        Warning,
        /// <summary>
        /// The information
        /// </summary>
        Info
    }

    /// <summary>
    /// Class DataGridColumnResizedEventArgs.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class DataGridColumnResizedEventArgs<T>
    {
        /// <summary>
        /// Gets the column.
        /// </summary>
        /// <value>The column.</value>
        public RadzenDataGridColumn<T> Column { get; internal set; }
        /// <summary>
        /// Gets the width.
        /// </summary>
        /// <value>The width.</value>
        public double Width { get; internal set; }
    }

    /// <summary>
    /// Class DataGridColumnReorderedEventArgs.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class DataGridColumnReorderedEventArgs<T>
    {
        /// <summary>
        /// Gets the column.
        /// </summary>
        /// <value>The column.</value>
        public RadzenDataGridColumn<T> Column { get; internal set; }
        /// <summary>
        /// Gets the old index.
        /// </summary>
        /// <value>The old index.</value>
        public int OldIndex { get; internal set; }
        /// <summary>
        /// Creates new index.
        /// </summary>
        /// <value>The new index.</value>
        public int NewIndex { get; internal set; }
    }

    /// <summary>
    /// Class ColumnResizedEventArgs.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ColumnResizedEventArgs<T>
    {
        /// <summary>
        /// Gets the column.
        /// </summary>
        /// <value>The column.</value>
        public RadzenGridColumn<T> Column { get; internal set; }
        /// <summary>
        /// Gets the width.
        /// </summary>
        /// <value>The width.</value>
        public double Width { get; internal set; }
    }
    /// <summary>
    /// Class FilterDescriptor.
    /// </summary>
    public class FilterDescriptor
    {
        /// <summary>
        /// Gets or sets the property.
        /// </summary>
        /// <value>The property.</value>
        public string Property { get; set; }
        /// <summary>
        /// Gets or sets the filter value.
        /// </summary>
        /// <value>The filter value.</value>
        public object FilterValue { get; set; }
        /// <summary>
        /// Gets or sets the filter operator.
        /// </summary>
        /// <value>The filter operator.</value>
        public FilterOperator FilterOperator { get; set; }
        /// <summary>
        /// Gets or sets the second filter value.
        /// </summary>
        /// <value>The second filter value.</value>
        public object SecondFilterValue { get; set; }
        /// <summary>
        /// Gets or sets the second filter operator.
        /// </summary>
        /// <value>The second filter operator.</value>
        public FilterOperator SecondFilterOperator { get; set; }
        /// <summary>
        /// Gets or sets the logical filter operator.
        /// </summary>
        /// <value>The logical filter operator.</value>
        public LogicalFilterOperator LogicalFilterOperator { get; set; }
    }

    /// <summary>
    /// Class SortDescriptor.
    /// </summary>
    public class SortDescriptor
    {
        /// <summary>
        /// Gets or sets the property.
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
    /// Class GroupDescriptor.
    /// </summary>
    public class GroupDescriptor
    {
        /// <summary>
        /// Gets or sets the property.
        /// </summary>
        /// <value>The property.</value>
        public string Property { get; set; }
        /// <summary>
        /// Gets or sets the title.
        /// </summary>
        /// <value>The title.</value>
        public string Title { get; set; }

        /// <summary>
        /// Gets the title.
        /// </summary>
        /// <returns>System.String.</returns>
        public string GetTitle()
        {
            return !string.IsNullOrEmpty(Title) ? Title : Property;
        }
    }

    /// <summary>
    /// Class Group.
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
    /// Class LoadDataArgs.
    /// </summary>
    public class LoadDataArgs
    {
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
        /// Gets or sets the order by.
        /// </summary>
        /// <value>The order by.</value>
        public string OrderBy { get; set; }
        /// <summary>
        /// Gets or sets the filter.
        /// </summary>
        /// <value>The filter.</value>
        public string Filter { get; set; }
        /// <summary>
        /// Gets or sets the filters.
        /// </summary>
        /// <value>The filters.</value>
        public IEnumerable<FilterDescriptor> Filters { get; set; }
        /// <summary>
        /// Gets or sets the sorts.
        /// </summary>
        /// <value>The sorts.</value>
        public IEnumerable<SortDescriptor> Sorts { get; set; }
    }

    /// <summary>
    /// Class PagerEventArgs.
    /// </summary>
    public class PagerEventArgs
    {
        /// <summary>
        /// Gets or sets the skip.
        /// </summary>
        /// <value>The skip.</value>
        public int Skip { get; set; }
        /// <summary>
        /// Gets or sets the top.
        /// </summary>
        /// <value>The top.</value>
        public int Top { get; set; }
        /// <summary>
        /// Gets or sets the index of the page.
        /// </summary>
        /// <value>The index of the page.</value>
        public int PageIndex { get; set; }
    }

    /// <summary>
    /// Class HtmlEditorCommands.
    /// </summary>
    public static class HtmlEditorCommands
    {
        /// <summary>
        /// The insert HTML
        /// </summary>
        public static string InsertHtml = "insertHtml";
        /// <summary>
        /// The align center
        /// </summary>
        public static string AlignCenter = "justifyCenter";
        /// <summary>
        /// The align left
        /// </summary>
        public static string AlignLeft = "justifyLeft";
        /// <summary>
        /// The align right
        /// </summary>
        public static string AlignRight = "justifyRight";
        /// <summary>
        /// The background
        /// </summary>
        public static string Background = "backColor";
        /// <summary>
        /// The bold
        /// </summary>
        public static string Bold = "bold";
        /// <summary>
        /// The color
        /// </summary>
        public static string Color = "foreColor";
        /// <summary>
        /// The font name
        /// </summary>
        public static string FontName = "fontName";
        /// <summary>
        /// The font size
        /// </summary>
        public static string FontSize = "fontSize";
        /// <summary>
        /// The format block
        /// </summary>
        public static string FormatBlock = "formatBlock";
        /// <summary>
        /// The indent
        /// </summary>
        public static string Indent = "indent";
        /// <summary>
        /// The italic
        /// </summary>
        public static string Italic = "italic";
        /// <summary>
        /// The justify
        /// </summary>
        public static string Justify = "justifyFull";
        /// <summary>
        /// The ordered list
        /// </summary>
        public static string OrderedList = "insertOrderedList";
        /// <summary>
        /// The outdent
        /// </summary>
        public static string Outdent = "outdent";
        /// <summary>
        /// The redo
        /// </summary>
        public static string Redo = "redo";
        /// <summary>
        /// The remove format
        /// </summary>
        public static string RemoveFormat = "removeFormat";
        /// <summary>
        /// The strike through
        /// </summary>
        public static string StrikeThrough = "strikeThrough";
        /// <summary>
        /// The subscript
        /// </summary>
        public static string Subscript = "subscript";
        /// <summary>
        /// The superscript
        /// </summary>
        public static string Superscript = "superscript";
        /// <summary>
        /// The underline
        /// </summary>
        public static string Underline = "underline";
        /// <summary>
        /// The undo
        /// </summary>
        public static string Undo = "undo";
        /// <summary>
        /// The unlink
        /// </summary>
        public static string Unlink = "unlink";
        /// <summary>
        /// The unordered list
        /// </summary>
        public static string UnorderedList = "insertUnorderedList";
    }

    /// <summary>
    /// Class HtmlEditorPasteEventArgs.
    /// </summary>
    public class HtmlEditorPasteEventArgs
    {
        /// <summary>
        /// Gets or sets the HTML.
        /// </summary>
        /// <value>The HTML.</value>
        public string Html { get; set; }
    }

    /// <summary>
    /// Class SeriesPoint.
    /// </summary>
    public class SeriesPoint
    {
        /// <summary>
        /// Gets or sets the category.
        /// </summary>
        /// <value>The category.</value>
        public double Category { get; set; }
        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        /// <value>The value.</value>
        public double Value { get; set; }
    }

    /// <summary>
    /// Class SeriesClickEventArgs.
    /// </summary>
    public class SeriesClickEventArgs
    {
        /// <summary>
        /// Gets or sets the data.
        /// </summary>
        /// <value>The data.</value>
        public object Data { get; set; }
        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        /// <value>The value.</value>
        public object Value { get; set; }
        /// <summary>
        /// Gets or sets the category.
        /// </summary>
        /// <value>The category.</value>
        public object Category { get; set; }
        /// <summary>
        /// Gets or sets the title.
        /// </summary>
        /// <value>The title.</value>
        public string Title { get; set; }
        /// <summary>
        /// Gets or sets the point.
        /// </summary>
        /// <value>The point.</value>
        public SeriesPoint Point { get; set; }
    }

    /// <summary>
    /// Class HtmlEditorExecuteEventArgs.
    /// </summary>
    public class HtmlEditorExecuteEventArgs
    {
        /// <summary>
        /// Gets or sets the editor.
        /// </summary>
        /// <value>The editor.</value>
        public RadzenHtmlEditor Editor { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="HtmlEditorExecuteEventArgs"/> class.
        /// </summary>
        /// <param name="editor">The editor.</param>
        internal HtmlEditorExecuteEventArgs(RadzenHtmlEditor editor)
        {
            Editor = editor;
        }

        /// <summary>
        /// Gets or sets the name of the command.
        /// </summary>
        /// <value>The name of the command.</value>
        public string CommandName { get; set; }
    }

    /// <summary>
    /// Class TreeEventArgs.
    /// </summary>
    public class TreeEventArgs
    {
        /// <summary>
        /// Gets or sets the text.
        /// </summary>
        /// <value>The text.</value>
        public string Text { get; set; }
        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        /// <value>The value.</value>
        public object Value { get; set; }
    }

    /// <summary>
    /// Class TreeExpandEventArgs.
    /// </summary>
    public class TreeExpandEventArgs
    {
        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        /// <value>The value.</value>
        public object Value { get; set; }
        /// <summary>
        /// Gets or sets the text.
        /// </summary>
        /// <value>The text.</value>
        public string Text { get; set; }
        /// <summary>
        /// Gets or sets the children.
        /// </summary>
        /// <value>The children.</value>
        public TreeItemSettings Children { get; set; }
    }

    /// <summary>
    /// Class TreeItemSettings.
    /// </summary>
    public class TreeItemSettings
    {
        /// <summary>
        /// Gets or sets the data.
        /// </summary>
        /// <value>The data.</value>
        public IEnumerable Data { get; set; }
        /// <summary>
        /// Gets or sets the text.
        /// </summary>
        /// <value>The text.</value>
        public Func<object, string> Text { get; set; }
        /// <summary>
        /// Gets or sets the text property.
        /// </summary>
        /// <value>The text property.</value>
        public string TextProperty { get; set; }
        /// <summary>
        /// Gets or sets the has children.
        /// </summary>
        /// <value>The has children.</value>
        public Func<object, bool> HasChildren { get; set; } = value => true;
        /// <summary>
        /// Gets or sets the expanded.
        /// </summary>
        /// <value>The expanded.</value>
        public Func<object, bool> Expanded { get; set; } = value => false;
        /// <summary>
        /// Gets or sets the selected.
        /// </summary>
        /// <value>The selected.</value>
        public Func<object, bool> Selected { get; set; } = value => false;
        /// <summary>
        /// Gets or sets the template.
        /// </summary>
        /// <value>The template.</value>
        public RenderFragment<Blazor.RadzenTreeItem> Template { get; set; }
    }

    /// <summary>
    /// Class LoginArgs.
    /// </summary>
    public class LoginArgs
    {
        /// <summary>
        /// Gets or sets the username.
        /// </summary>
        /// <value>The username.</value>
        public string Username { get; set; }
        /// <summary>
        /// Gets or sets the password.
        /// </summary>
        /// <value>The password.</value>
        public string Password { get; set; }
    }

    /// <summary>
    /// Class ConvertType.
    /// </summary>
    public static class ConvertType
    {
        /// <summary>
        /// Changes the type.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="type">The type.</param>
        /// <returns>System.Object.</returns>
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
    /// Class PropertyAccess.
    /// </summary>
    public static class PropertyAccess
    {
        /// <summary>
        /// Getters the specified property name.
        /// </summary>
        /// <typeparam name="TItem">The type of the t item.</typeparam>
        /// <typeparam name="TValue">The type of the t value.</typeparam>
        /// <param name="propertyName">Name of the property.</param>
        /// <returns>Func&lt;TItem, TValue&gt;.</returns>
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
        /// Determines whether the specified source is date.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <returns><c>true</c> if the specified source is date; otherwise, <c>false</c>.</returns>
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
        /// Gets the type of the element.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>Type.</returns>
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
        /// Gets the property.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns>System.String.</returns>
        public static string GetProperty(string property)
        {
            var type = Type.GetType($"System.{property}");
            var propertyName = $"{(type != null ? "@" : "")}{property}";

            if (propertyName.IndexOf(".") != -1)
            {
                return $"{propertyName.Split('.')[0]} == null ? null : {propertyName}";

            }

            return propertyName;
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="path">The path.</param>
        /// <returns>System.Object.</returns>
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
        /// Getters the specified data.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data">The data.</param>
        /// <param name="propertyName">Name of the property.</param>
        /// <returns>Func&lt;System.Object, T&gt;.</returns>
        public static Func<object, T> Getter<T>(object data, string propertyName)
        {
            var type = data.GetType();
            var arg = Expression.Parameter(typeof(object));
            var body = Expression.Property(Expression.Convert(arg, type), propertyName);

            return Expression.Lambda<Func<object, T>>(body, arg).Compile();
        }

        /// <summary>
        /// Tries the get item or value from property.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="item">The item.</param>
        /// <param name="property">The property.</param>
        /// <param name="result">The result.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        public static bool TryGetItemOrValueFromProperty<T>(object item, string property, out T result)
        {
            object r = GetItemOrValueFromProperty(item, property);

            if (r != null)
            {
                result = (T)r;
                return true;
            } else
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
        /// Determines whether the specified source is numeric.
        /// </summary>
        /// <param name="source">The source.</param>
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
    /// Interface IRadzenForm
    /// </summary>
    public interface IRadzenForm
    {
        /// <summary>
        /// Adds the component.
        /// </summary>
        /// <param name="component">The component.</param>
        void AddComponent(IRadzenFormComponent component);
        /// <summary>
        /// Removes the component.
        /// </summary>
        /// <param name="component">The component.</param>
        void RemoveComponent(IRadzenFormComponent component);
        /// <summary>
        /// Finds the component.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>IRadzenFormComponent.</returns>
        IRadzenFormComponent FindComponent(string name);
    }

    /// <summary>
    /// Interface IRadzenFormComponent
    /// </summary>
    public interface IRadzenFormComponent
    {
        /// <summary>
        /// Gets a value indicating whether this instance is bound.
        /// </summary>
        /// <value><c>true</c> if this instance is bound; otherwise, <c>false</c>.</value>
        bool IsBound { get; }
        /// <summary>
        /// Gets a value indicating whether this instance has value.
        /// </summary>
        /// <value><c>true</c> if this instance has value; otherwise, <c>false</c>.</value>
        bool HasValue { get; }

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <returns>System.Object.</returns>
        object GetValue();

        /// <summary>
        /// Gets or sets the name.
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
    /// Class FormInvalidSubmitEventArgs.
    /// </summary>
    public class FormInvalidSubmitEventArgs
    {
        /// <summary>
        /// Gets or sets the errors.
        /// </summary>
        /// <value>The errors.</value>
        public IEnumerable<string> Errors { get; set; }
    }

    /// <summary>
    /// Interface IRadzenFormValidator
    /// </summary>
    public interface IRadzenFormValidator
    {
        /// <summary>
        /// Returns true if ... is valid.
        /// </summary>
        /// <value><c>true</c> if this instance is valid; otherwise, <c>false</c>.</value>
        bool IsValid { get; }
        /// <summary>
        /// Gets or sets the component.
        /// </summary>
        /// <value>The component.</value>
        string Component { get; set; }
        /// <summary>
        /// Gets or sets the text.
        /// </summary>
        /// <value>The text.</value>
        string Text { get; set; }
    }

    /// <summary>
    /// Class RadzenComponentWithChildren.
    /// Implements the <see cref="Radzen.RadzenComponent" />
    /// </summary>
    /// <seealso cref="Radzen.RadzenComponent" />
    public class RadzenComponentWithChildren : RadzenComponent
    {
        /// <summary>
        /// Gets or sets the content of the child.
        /// </summary>
        /// <value>The content of the child.</value>
        [Parameter]
        public RenderFragment ChildContent { get; set; }
    }

    /// <summary>
    /// Class Debouncer.
    /// </summary>
    class Debouncer
    {
        /// <summary>
        /// The timer
        /// </summary>
        System.Timers.Timer timer;
        /// <summary>
        /// Gets or sets the timer started.
        /// </summary>
        /// <value>The timer started.</value>
        DateTime timerStarted { get; set; } = DateTime.UtcNow.AddYears(-1);

        /// <summary>
        /// Debounces the specified interval.
        /// </summary>
        /// <param name="interval">The interval.</param>
        /// <param name="action">The action.</param>
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

        /// <summary>
        /// Throttles the specified interval.
        /// </summary>
        /// <param name="interval">The interval.</param>
        /// <param name="action">The action.</param>
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
    /// Class ParameterViewExtensions.
    /// </summary>
    public static class ParameterViewExtensions
    {
        /// <summary>
        /// Dids the parameter change.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="parameters">The parameters.</param>
        /// <param name="parameterName">Name of the parameter.</param>
        /// <param name="parameterValue">The parameter value.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        public static bool DidParameterChange<T>(this ParameterView parameters, string parameterName, T parameterValue)
        {
            T value;

            if (parameters.TryGetValue(parameterName, out value))
            {
                return !EqualityComparer<T>.Default.Equals(value, parameterValue);
            }

            return false;
        }
    }
    /// <summary>
    /// Class RadzenSplitterEventArgs.
    /// </summary>
    public class RadzenSplitterEventArgs
    {
        /// <summary>
        /// Gets or sets the index of the pane.
        /// </summary>
        /// <value>The index of the pane.</value>
        public int PaneIndex { get; set; }
        /// <summary>
        /// Gets or sets the pane.
        /// </summary>
        /// <value>The pane.</value>
        public RadzenSplitterPane Pane { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="RadzenSplitterEventArgs"/> is cancel.
        /// </summary>
        /// <value><c>true</c> if cancel; otherwise, <c>false</c>.</value>
        public bool Cancel { get; set; }

    }

    /// <summary>
    /// Class RadzenSplitterResizeEventArgs.
    /// Implements the <see cref="Radzen.RadzenSplitterEventArgs" />
    /// </summary>
    /// <seealso cref="Radzen.RadzenSplitterEventArgs" />
    public class RadzenSplitterResizeEventArgs:RadzenSplitterEventArgs
    {
        /// <summary>
        /// Creates new size.
        /// </summary>
        /// <value>The new size.</value>
        public double NewSize { get; set; }
    }
}
