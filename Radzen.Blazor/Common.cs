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
    [EventHandler("onmouseenter", typeof(EventArgs), true, true)]
    [EventHandler("onmouseleave", typeof(EventArgs), true, true)]
    public static class EventHandlers
    {
    }

    public interface IRadzenSelectBar
    {
        void AddItem(RadzenSelectBarItem item);
        void RemoveItem(RadzenSelectBarItem item);
    }

    public class DateRenderEventArgs
    {
        public IDictionary<string, object> Attributes { get; private set; } = new Dictionary<string, object>();
        public DateTime Date { get; internal set; }
        public bool Disabled { get; set; }
    }

    public class GoogleMapPosition
    {
        public double Lat { get; set; }
        public double Lng { get; set; }
    }

    public class GoogleMapClickEventArgs
    {
        public GoogleMapPosition Position { get; set; }
    }

    public class MenuItemEventArgs
    {
        public string Text { get; internal set; }
        public object Value { get; internal set; }
        public string Path { get; internal set; }
    }

    public class UploadErrorEventArgs
    {
        public string Message { get; set; }
    }

    public class RowRenderEventArgs<T>
    {
        public IDictionary<string, object> Attributes { get; private set; } = new Dictionary<string, object>();

        public T Data { get; internal set; }

        public bool Expandable { get; set; }
    }

    public class GridRenderEventArgs<T>
    {
        public RadzenGrid<T> Grid { get; internal set; }
        public bool FirstRender { get; internal set; }
    }

    public class DataGridRenderEventArgs<T>
    {
        public RadzenDataGrid<T> Grid { get; internal set; }
        public bool FirstRender { get; internal set; }
    }

    public class CellRenderEventArgs<T> : RowRenderEventArgs<T>
    {
        public Blazor.RadzenGridColumn<T> Column { get; internal set; }
    }

    public class DataGridCellRenderEventArgs<T> : RowRenderEventArgs<T>
    {
        public Blazor.RadzenDataGridColumn<T> Column { get; internal set; }
    }


    public class DataGridRowMouseEventArgs<T> : Microsoft.AspNetCore.Components.Web.MouseEventArgs
    {
        public T Data { get; internal set; }
    }

    public class UploadChangeEventArgs
    {
        public IEnumerable<FileInfo> Files { get; set; }
    }

    public class UploadProgressArgs
    {
        public int Loaded { get; set; }
        public int Total { get; set; }
        public int Progress { get; set; }

        public IEnumerable<FileInfo> Files { get; set; }
    }

    public class UploadCompleteEventArgs
    {
        public JsonDocument JsonResponse { get; set; }

        public string RawResponse { get; set; }
    }

    public class FileInfo
    {
        public string Name { get; set; }
        public int Size { get; set; }
    }

    public class PreviewFileInfo : FileInfo
    {
        public string Url { get; set; }
    }

    public class Query
    {
        public string Filter { get; set; }
        public object[] FilterParameters { get; set; }
        public string OrderBy { get; set; }
        public string Expand { get; set; }
        public string Select { get; set; }
        public int? Skip { get; set; }
        public int? Top { get; set; }

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

    public enum TabRenderMode
    {
        Server,
        Client
    }

    public enum PagerPosition
    {
        Top,
        Bottom,
        TopAndBottom
    }

    public enum DataGridExpandMode
    {
        Single,
        Multiple
    }

    public enum DataGridEditMode
    {
        Single,
        Multiple
    }

    public enum DataGridSelectionMode
    {
        Single,
        Multiple
    }

    public enum NotificationSeverity
    {
        Error,
        Info,
        Success,
        Warning
    }

    public enum ProgressBarMode
    {
        Determinate,
        Indeterminate
    }

    public enum Orientation
    {
        Horizontal,
        Vertical
    }

    public enum SortOrder
    {
        Ascending,
        Descending
    }

    public enum ButtonType
    {
        Button,
        Submit,
        Reset
    }

    public enum ButtonSize
    {
        Medium,
        Small
    }

    public enum ButtonStyle
    {
        Primary,
        Secondary,
        Light,
        Success,
        Danger,
        Warning,
        Info
    }

    public enum FilterMode
    {
        Simple,
        Advanced
    }

    public enum FilterCaseSensitivity
    {
        Default,
        CaseInsensitive
    }

    public enum LogicalFilterOperator
    {
        And,
        Or
    }

    public enum StringFilterOperator
    {
        Contains,
        StartsWith,
        EndsWith
    }

    public enum FilterOperator
    {
        Equals,
        NotEquals,
        LessThan,
        LessThanOrEquals,
        GreaterThan,
        GreaterThanOrEquals,
        Contains,
        StartsWith,
        EndsWith,
        DoesNotContain
    }

    public enum TextAlign
    {
        Left,
        Right,
        Center
    }

    public enum BadgeStyle
    {
        Primary,
        Secondary,
        Light,
        Success,
        Danger,
        Warning,
        Info
    }

    public class DataGridColumnResizedEventArgs<T>
    {
        public RadzenDataGridColumn<T> Column { get; internal set; }
        public double Width { get; internal set; }
    }

    public class DataGridColumnReorderedEventArgs<T>
    {
        public RadzenDataGridColumn<T> Column { get; internal set; }
        public int OldIndex { get; internal set; }
        public int NewIndex { get; internal set; }
    }

    public class ColumnResizedEventArgs<T>
    {
        public RadzenGridColumn<T> Column { get; internal set; }
        public double Width { get; internal set; }
    }
    public class FilterDescriptor
    {
        public string Property { get; set; }
        public object FilterValue { get; set; }
        public FilterOperator FilterOperator { get; set; }
        public object SecondFilterValue { get; set; }
        public FilterOperator SecondFilterOperator { get; set; }
        public LogicalFilterOperator LogicalFilterOperator { get; set; }
    }

    public class SortDescriptor
    {
        public string Property { get; set; }
        public SortOrder? SortOrder { get; set; }
    }

    public class GroupDescriptor
    {
        public string Property { get; set; }
        public string Title { get; set; }

        public string GetTitle()
        {
            return !string.IsNullOrEmpty(Title) ? Title : Property;
        }
    }

    public class Group
    {
        public GroupResult Data { get; set; }
        public GroupDescriptor GroupDescriptor { get; set; }
        public int Level { get; set; }
    }

    public class LoadDataArgs
    {
        public int? Skip { get; set; }
        public int? Top { get; set; }
        public string OrderBy { get; set; }
        public string Filter { get; set; }
        public IEnumerable<FilterDescriptor> Filters { get; set; }
        public IEnumerable<SortDescriptor> Sorts { get; set; }
    }

    public class PagerEventArgs
    {
        public int Skip { get; set; }
        public int Top { get; set; }
        public int PageIndex { get; set; }
    }

    public static class HtmlEditorCommands
    {
        public static string InsertHtml = "insertHtml";
        public static string AlignCenter = "justifyCenter";
        public static string AlignLeft = "justifyLeft";
        public static string AlignRight = "justifyRight";
        public static string Background = "backColor";
        public static string Bold = "bold";
        public static string Color = "foreColor";
        public static string FontName = "fontName";
        public static string FontSize = "fontSize";
        public static string FormatBlock = "formatBlock";
        public static string Indent = "indent";
        public static string Italic = "italic";
        public static string Justify = "justifyFull";
        public static string OrderedList = "insertOrderedList";
        public static string Outdent = "outdent";
        public static string Redo = "redo";
        public static string RemoveFormat = "removeFormat";
        public static string StrikeThrough = "strikeThrough";
        public static string Subscript = "subscript";
        public static string Superscript = "superscript";
        public static string Underline = "underline";
        public static string Undo = "undo";
        public static string Unlink = "unlink";
        public static string UnorderedList = "insertUnorderedList";
    }

    public class HtmlEditorPasteEventArgs
    {
        public string Html { get; set; }
    }

    public class SeriesPoint
    {
        public double Category { get; set; }
        public double Value { get; set; }
    }

    public class SeriesClickEventArgs
    {
        public object Data { get; set; }
        public object Value { get; set; }
        public object Category { get; set; }
        public string Title { get; set; }
        public SeriesPoint Point { get; set; }
    }

    public class HtmlEditorExecuteEventArgs
    {
        public RadzenHtmlEditor Editor { get; set; }

        internal HtmlEditorExecuteEventArgs(RadzenHtmlEditor editor)
        {
            Editor = editor;
        }

        public string CommandName { get; set; }
    }

    public class TreeEventArgs
    {
        public string Text { get; set; }
        public object Value { get; set; }
    }

    public class TreeExpandEventArgs
    {
        public object Value { get; set; }
        public string Text { get; set; }
        public TreeItemSettings Children { get; set; }
    }

    public class TreeItemSettings
    {
        public IEnumerable Data { get; set; }
        public Func<object, string> Text { get; set; }
        public string TextProperty { get; set; }
        public Func<object, bool> HasChildren { get; set; } = value => true;
        public Func<object, bool> Expanded { get; set; } = value => false;
        public Func<object, bool> Selected { get; set; } = value => false;
        public RenderFragment<Blazor.RadzenTreeItem> Template { get; set; }
    }

    public class LoginArgs
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public static class ConvertType
    {
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

    public static class PropertyAccess
    {
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
        public static Func<object, T> Getter<T>(object data, string propertyName)
        {
            var type = data.GetType();
            var arg = Expression.Parameter(typeof(object));
            var body = Expression.Property(Expression.Convert(arg, type), propertyName);

            return Expression.Lambda<Func<object, T>>(body, arg).Compile();
        }

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

    public interface IRadzenForm
    {
        void AddComponent(IRadzenFormComponent component);
        void RemoveComponent(IRadzenFormComponent component);
        IRadzenFormComponent FindComponent(string name);
    }

    public interface IRadzenFormComponent
    {
        bool IsBound { get; }
        bool HasValue { get; }

        object GetValue();

        string Name { get; set; }
        FieldIdentifier FieldIdentifier { get; }
    }

    public class FormInvalidSubmitEventArgs
    {
        public IEnumerable<string> Errors { get; set; }
    }

    public interface IRadzenFormValidator
    {
        bool IsValid { get; }
        string Component { get; set; }
        string Text { get; set; }
    }

    public class RadzenComponentWithChildren : RadzenComponent
    {
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

    public static class ParameterViewExtensions
    {
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
    public class RadzenSplitterEventArgs
    {
        public int PaneIndex { get; set; }
        public RadzenSplitterPane Pane { get; set; }
        public bool Cancel { get; set; }

    }

    public class RadzenSplitterResizeEventArgs:RadzenSplitterEventArgs
    {
        public double NewSize { get; set; }
    }
}
