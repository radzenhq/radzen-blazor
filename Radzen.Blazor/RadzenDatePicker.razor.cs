using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Radzen.Blazor.Rendering;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenDatePicker component.
    /// </summary>
    /// <typeparam name="TValue">The type of the t value.</typeparam>
    /// <example>
    /// <code>
    /// &lt;RadzenDatePicker @bind-Value=@someValue TValue="DateTime" Change=@(args => Console.WriteLine($"Selected date: {args}")) /&gt;
    /// </code>
    /// </example>
    public partial class RadzenDatePicker<TValue> : RadzenComponent, IRadzenFormComponent
    {
        /// <summary>
        /// Gets or sets a value indicating whether calendar week will be shown.
        /// </summary>
        /// <value><c>true</c> if calendar week is shown; otherwise, <c>false</c>.</value>

        [Parameter]
        public bool ShowCalendarWeek { get; set; }

        /// <summary>
        /// Gets or sets the previous month aria label text.
        /// </summary>
        /// <value>The previous month aria label text.</value>
        [Parameter]
        public string CalendarWeekTitle { get; set; } = "#";

        /// <summary>
        /// Gets or sets the toggle popup aria label text.
        /// </summary>
        /// <value>The toggle popup aria label text.</value>
        [Parameter]
        public string ToggleAriaLabel { get; set; } = "Toggle";

        /// <summary>
        /// Gets or sets the OK button aria label text.
        /// </summary>
        /// <value>The OK button aria label text.</value>
        [Parameter]
        public string OkAriaLabel { get; set; } = "Ok";

        /// <summary>
        /// Gets or sets the previous month aria label text.
        /// </summary>
        /// <value>The previous month aria label text.</value>
        [Parameter]
        public string PrevMonthAriaLabel { get; set; } = "Previous month";

        /// <summary>
        /// Gets or sets the next month aria label text.
        /// </summary>
        /// <value>The next month aria label text.</value>
        [Parameter]
        public string NextMonthAriaLabel { get; set; } = "Next month";

        /// <summary>
        /// Gets or sets the toggle Am/Pm aria label text.
        /// </summary>
        /// <value>The toggle Am/Pm aria label text.</value>
        [Parameter]
        public string ToggleAmPmAriaLabel { get; set; } = "Toggle Am/Pm";

        /// <summary>
        /// Specifies additional custom attributes that will be rendered by the input.
        /// </summary>
        /// <value>The attributes.</value>
        [Parameter]
        public IReadOnlyDictionary<string, object> InputAttributes { get; set; }

        RadzenDropDown<int> monthDropDown;
        RadzenDropDown<int> yearDropDown;

        async Task ToggleAmPm()
        {
            if (Disabled) return;

            var newHour = (CurrentDate.Hour + 12) % 24;

            var newValue = new DateTime(CurrentDate.Year, CurrentDate.Month, CurrentDate.Day, newHour, CurrentDate.Minute, CurrentDate.Second);

            hour = newValue.Hour;
            await UpdateValueFromTime(newValue);
        }

        int GetHour24FormatFrom12Format(int hour12)
        {
            hour12 = Math.Max(Math.Min(hour12, 12), 1);

            return CurrentDate.Hour < 12 ?
                (hour12 == 12 ? 0 : hour12) // AM
                : (hour12 == 12 ? 12 : hour12 + 12); // PM
        }

        int? hour;

        void OnUpdateHourInput(ChangeEventArgs args)
        {
            var value = $"{args.Value}";
            if (string.IsNullOrWhiteSpace(value) || !int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int v))
            {
                hour = null;
                return;
            }

            hour = HourFormat == "12" ? GetHour24FormatFrom12Format(v) : Math.Max(Math.Min(v, 23), 0);
        }

        int? minutes;

        void OnUpdateHourMinutes(ChangeEventArgs args)
        {
            var value = $"{args.Value}";
            if (string.IsNullOrWhiteSpace(value) || !int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int v))
            {
                minutes = null;
                return;
            }

            minutes = Math.Max(Math.Min(v, 59), 0);
        }

        int? seconds;

        void OnUpdateHourSeconds(ChangeEventArgs args)
        {
            var value = $"{args.Value}";
            if (string.IsNullOrWhiteSpace(value) || !int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int v))
            {
                seconds = null;
                return;
            }

            seconds = Math.Max(Math.Min(v, 59), 0);
        }

        async Task UpdateValueFromTime(DateTime newValue)
        {
            if (ShowTimeOkButton)
            {
                DateTimeValue = newValue;
                CurrentDate = newValue;
            }
            else
            {
                Value = newValue;
                CurrentDate = newValue;
                await OnChange();
            }
        }

        async Task UpdateHour(int v)
        {
            var newHour = HourFormat == "12" ? GetHour24FormatFrom12Format(v) : v;
            var newMinute = CurrentDate.Minute;
            var newSecond = CurrentDate.Second;

            if (v < 0)
            {
                newHour = string.IsNullOrEmpty(HoursStep) ? 23 : 0;
                newMinute = string.IsNullOrEmpty(MinutesStep) ? 59 : 0;
                newSecond = string.IsNullOrEmpty(SecondsStep) ? 59 : 0;
            }

            var newValue = new DateTime(CurrentDate.Year, CurrentDate.Month, CurrentDate.Day, newHour > 23 || newHour < 0 ? 0 : newHour, newMinute, newSecond);

            hour = newValue.Hour;
            await UpdateValueFromTime(newValue);
        }

        async Task UpdateMinutes(int v)
        {
            var newValue = new DateTime(CurrentDate.Year, CurrentDate.Month, CurrentDate.Day, CurrentDate.Hour, v, CurrentDate.Second);

            minutes = newValue.Minute;
            await UpdateValueFromTime(newValue);
        }

        async Task UpdateSeconds(int v)
        {
            var newValue = new DateTime(CurrentDate.Year, CurrentDate.Month, CurrentDate.Day, CurrentDate.Hour, CurrentDate.Minute, v);

            seconds = newValue.Second;
            await UpdateValueFromTime(newValue);
        }

        async Task OkClick(bool shouldClose = true)
        {
            if (shouldClose)
            {
                Close();
            }

            if(Min.HasValue && CurrentDate < Min.Value || Max.HasValue && CurrentDate > Max.Value)
            {
                return;
            }

            if (!Disabled)
            {
                DateTime date = CurrentDate;

                if (CurrentDate.Hour != hour && hour != null)
                {
                    date = new DateTime(CurrentDate.Year, CurrentDate.Month, CurrentDate.Day, hour.Value, CurrentDate.Minute, CurrentDate.Second);
                }

                if (CurrentDate.Minute != minutes && minutes != null)
                {
                    date = new DateTime(CurrentDate.Year, CurrentDate.Month, CurrentDate.Day, CurrentDate.Hour, minutes.Value, CurrentDate.Second);
                }

                if (CurrentDate.Second != seconds && seconds != null)
                {
                    date = new DateTime(CurrentDate.Year, CurrentDate.Month, CurrentDate.Day, CurrentDate.Hour, CurrentDate.Minute, seconds.Value);
                }

                Value = date;

                await OnChange();

                if (monthDropDown != null)
                {
                    await monthDropDown.PopupClose();
                }

                if (yearDropDown != null)
                {
                    await yearDropDown.PopupClose();
                }
            }
        }

        class NameValue
        {
            /// <summary>
            /// Gets or sets the name.
            /// </summary>
            /// <value>The name.</value>
            public string Name { get; set; }
            /// <summary>
            /// Gets or sets the value.
            /// </summary>
            /// <value>The value.</value>
            public int Value { get; set; }
        }

        IList<NameValue> months;
        IList<NameValue> years;

        int YearFrom { get; set; }
        int YearTo { get; set; }

        /// <inheritdoc />
        protected override void OnInitialized()
        {
            base.OnInitialized();

            UpdateYearsAndMonths(Min, Max);

#if NET6_0_OR_GREATER
            if (typeof(TValue) == typeof(TimeOnly) || typeof(TValue) == typeof(TimeOnly?))
            {
                TimeOnly = true;
                ShowTime = true;
            }
#endif
        }

        void UpdateYearsAndMonths(DateTime? min, DateTime? max)
        {
            YearFrom = min.HasValue ? min.Value.Year : int.Parse(YearRange.Split(':').First());
            YearTo = max.HasValue ? max.Value.Year : int.Parse(YearRange.Split(':').Last());
            months = Enumerable.Range(1, 12).Select(i => new NameValue() { Name = Culture.DateTimeFormat.GetMonthName(i), Value = i }).ToList();
            years = Enumerable.Range(YearFrom, YearTo - YearFrom + 1)
                .Select(i => new NameValue() { Name = $"{i}", Value = i }).ToList();
        }

        /// <summary>
        /// Gets or sets a value indicating whether value can be cleared.
        /// </summary>
        /// <value><c>true</c> if value can be cleared; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool AllowClear { get; set; }

        /// <summary>
        /// Gets or sets the tab index.
        /// </summary>
        /// <value>The tab index.</value>
        [Parameter]
        public int TabIndex { get; set; } = 0;

        /// <summary>
        /// Gets or sets the name of the form component.
        /// </summary>
        /// <value>The name.</value>
        [Parameter]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the input CSS class.
        /// </summary>
        /// <value>The input CSS class.</value>
        [Parameter]
        public string InputClass { get; set; }
        /// <summary>
        /// Gets or sets the button CSS class.
        /// </summary>
        /// <value>The button CSS class.</value>
        [Parameter]
        public string ButtonClass { get; set; }

        /// <summary>
        /// Gets or sets the Minimum Selectable Date.
        /// </summary>
        /// <value>The Minimum Selectable Date.</value>
        [Parameter]
        public DateTime? Min { get; set; }

        /// <summary>
        /// Gets or sets the Maximum Selectable Date.
        /// </summary>
        /// <value>The Maximum Selectable Date.</value>
        [Parameter]
        public DateTime? Max { get; set; }

        /// <summary>
        /// Gets or sets the Initial Date/Month View.
        /// </summary>
        /// <value>The Initial Date/Month View.</value>
        [Parameter]
        public DateTime? InitialViewDate { get; set; }

        DateTime? _dateTimeValue;

        DateTime? DateTimeValue
        {
            get
            {
                return _dateTimeValue;
            }
            set
            {
                if (_dateTimeValue != value)
                {
                    _dateTimeValue = value;
                    Value = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets the date render callback. Use it to set attributes.
        /// </summary>
        /// <value>The date render callback.</value>
        [Parameter]
        public Action<DateRenderEventArgs> DateRender { get; set; }

        DateRenderEventArgs DateAttributes(DateTime value)
        {
            var args = new DateRenderEventArgs() { Date = value, Disabled = (Min.HasValue && value < Min.Value) || (Max.HasValue && value > Max.Value) };

            if (DateRender != null)
            {
                DateRender(args);
            }

            return args;
        }

        /// <summary>
        /// Gets or sets the kind of DateTime bind to control
        /// </summary>
        [Parameter]
        public DateTimeKind Kind { get; set; } = DateTimeKind.Unspecified;

        object _value;

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        /// <value>The value.</value>
        [Parameter]
        public object Value
        {
            get
            {
                return _value;
            }
            set
            {
                if (!EqualityComparer<object>.Default.Equals(value, _value))
                {
                    _value = ConvertToTValue(value);
                    _currentDate = default(DateTime);

                    if (value is DateTimeOffset offset)
                    {
                        if (offset.Offset == TimeSpan.Zero && Kind == DateTimeKind.Local)
                        {
                            _dateTimeValue = offset.LocalDateTime;
                        }
                        else if (offset.Offset != TimeSpan.Zero && Kind == DateTimeKind.Utc)
                        {
                            _dateTimeValue = offset.UtcDateTime;
                        }
                        else
                        {
                            _dateTimeValue = DateTime.SpecifyKind(offset.DateTime, Kind);
                        }

                        _value = _dateTimeValue;
                    }
                    else
                    {
                        if (value is DateTime dateTime && dateTime != default(DateTime))
                        {
                            DateTimeValue = DateTime.SpecifyKind(dateTime, Kind);
                        }
#if NET6_0_OR_GREATER
                        else if (value is DateOnly dateOnly)
                        {
                            DateTimeValue = dateOnly.ToDateTime(System.TimeOnly.MinValue, Kind);
                        }
                        else if (value is TimeOnly timeOnly)
                        {
                            DateTimeValue = new DateTime(1,1,0001, timeOnly.Hour, timeOnly.Minute, timeOnly.Second, timeOnly.Millisecond, Kind);
                        }
#endif
                        else
                        {
                            DateTimeValue = null;
                        }
                    }
                }
            }
        }

        private static object ConvertToTValue(object value)
        {
#if NET6_0_OR_GREATER
            var typeofTValue = typeof(TValue);
            if (value is DateTime dt)
            {
                if (typeofTValue == typeof(DateOnly) || typeofTValue == typeof(DateOnly?))
                {
                    value = DateOnly.FromDateTime(dt);
                    return (TValue)value;
                }
                if (typeofTValue == typeof(TimeOnly) || typeofTValue == typeof(TimeOnly?))
                {
                    value = System.TimeOnly.FromDateTime(dt);
                    return (TValue)value;
                }
            }
#endif
            return value;
        }

        DateTime _currentDate;

        private DateTime CurrentDate
        {
            get
            {
                if (_currentDate == default(DateTime))
                {
                    _currentDate = HasValue ? DateTimeValue.Value : InitialViewDate ?? DateTime.Today;
                }
                return _currentDate;
            }
            set
            {
                _currentDate = value;
                FocusedDate = value;
                CurrentDateChanged.InvokeAsync(value);
            }
        }

        /// <summary>
        /// Gets or set the current date changed callback.
        /// </summary>
        [Parameter]
        public EventCallback<DateTime> CurrentDateChanged { get; set; }

        private DateTime StartDate
        {
            get
            {
                if (CurrentDate == DateTime.MinValue)
                {
                    return DateTime.MinValue;
                }

                var firstDayOfTheMonth = new DateTime(CurrentDate.Year, CurrentDate.Month, 1);

                if (firstDayOfTheMonth == DateTime.MinValue)
                {
                    return DateTime.MinValue;
                }

                int diff = (7 + (firstDayOfTheMonth.DayOfWeek - Culture.DateTimeFormat.FirstDayOfWeek)) % 7;
                return firstDayOfTheMonth.AddDays(-1 * diff).Date;
            }
        }

        IEnumerable<string> ShiftedAbbreviatedDayNames
        {
            get
            {
                for (int current = (int)Culture.DateTimeFormat.FirstDayOfWeek, to = current + 7; current < to; current++)
                {
                    yield return Culture.DateTimeFormat.AbbreviatedDayNames[current % 7];
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is bound (ValueChanged callback has delegate).
        /// </summary>
        /// <value><c>true</c> if this instance is bound; otherwise, <c>false</c>.</value>
        public bool IsBound
        {
            get
            {
                return ValueChanged.HasDelegate;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance has value.
        /// </summary>
        /// <value><c>true</c> if this instance has value; otherwise, <c>false</c>.</value>
        public bool HasValue
        {
            get
            {
                return DateTimeValue.HasValue && DateTimeValue != default(DateTime);
            }
        }

        /// <summary>
        /// Gets the formatted value.
        /// </summary>
        /// <value>The formatted value.</value>
        public string FormattedValue
        {
            get
            {
                return HasValue ? string.Format(Culture, "{0:" + DateFormat + "}", Value) : "";
            }
        }

        IRadzenForm _form;

        /// <summary>
        /// Gets or sets the form.
        /// </summary>
        /// <value>The form.</value>
        [CascadingParameter]
        public IRadzenForm Form
        {
            get
            {
                return _form;
            }
            set
            {
                if (_form != value && value != null)
                {
                    _form = value;
                    _form.AddComponent(this);
                }
            }
        }

        /// <summary>
        /// Gets input reference.
        /// </summary>
        protected ElementReference input;

        /// <summary>
        /// Parses the date.
        /// </summary>
        protected async Task ParseDate()
        {
            DateTime? newValue;
            var inputValue = await JSRuntime.InvokeAsync<string>("Radzen.getInputValue", input);
            bool valid = TryParseInput(inputValue, out DateTime value);

            var nullable = Nullable.GetUnderlyingType(typeof(TValue)) != null || AllowClear;

            if (valid && !DateAttributes(value).Disabled)
            {
                newValue = TimeOnly && CurrentDate != default(DateTime) ? new DateTime(CurrentDate.Year, CurrentDate.Month, CurrentDate.Day, value.Hour, value.Minute, value.Second) : value;
            }
            else
            {
                newValue = null;

                if (nullable)
                {
                    await JSRuntime.InvokeAsync<string>("Radzen.setInputValue", input, "");
                }
                else
                {
                    await JSRuntime.InvokeAsync<string>("Radzen.setInputValue", input, FormattedValue);
                }

            }

            if (DateTimeValue != newValue && (newValue != null || nullable))
            {
                DateTimeValue = newValue;
                if ((typeof(TValue) == typeof(DateTimeOffset) || typeof(TValue) == typeof(DateTimeOffset?)) && Value != null)
                {
                    DateTimeOffset? offset = DateTime.SpecifyKind((DateTime)Value, Kind);
                    await ValueChanged.InvokeAsync((TValue)(object)offset);
                }
                else if ((typeof(TValue) == typeof(DateTime) || typeof(TValue) == typeof(DateTime?)) && Value != null)
                {
                    await ValueChanged.InvokeAsync((TValue)(object)DateTime.SpecifyKind((DateTime)Value, Kind));
                }
                else
                {
                    await ValueChanged.InvokeAsync(Value == null ? default(TValue) : (TValue)Value);
                }

                if (FieldIdentifier.FieldName != null)
                {
                    EditContext?.NotifyFieldChanged(FieldIdentifier);
                }

                await Change.InvokeAsync(DateTimeValue);
                StateHasChanged();
            }
        }

        /// <summary>
        /// Parse the input using an function outside the Radzen-library
        /// </summary>
        [Parameter]
        public Func<string, DateTime?> ParseInput { get; set; }

        private bool TryParseInput(string inputValue, out DateTime value)
        {
            value = DateTime.MinValue;
            bool valid = false;

            if (ParseInput != null)
            {
                DateTime? custom = ParseInput.Invoke(inputValue);

                if (custom.HasValue)
                {
                    valid = true;
                    value = custom.Value;
                }
            }
            else
            {
                valid = DateTime.TryParseExact(inputValue, DateFormat, null, DateTimeStyles.None, out value);

                if (!valid)
                {
                    valid = DateTime.TryParse(inputValue, out value);
                }
            }

            return valid;
        }

        async Task Clear()
        {
            if (Disabled || ReadOnly)
                return;

            Value = null;

            await ValueChanged.InvokeAsync(default(TValue));

            if (FieldIdentifier.FieldName != null)
            {
                EditContext?.NotifyFieldChanged(FieldIdentifier);
            }

            await Change.InvokeAsync(DateTimeValue);
            StateHasChanged();
        }

        private string ButtonClasses
        {
            get => $"notranslate rz-button-icon-left rzi rzi-{(TimeOnly ? "time" : "calendar")}";
        }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="RadzenDatePicker{TValue}"/> is inline - only Calender.
        /// </summary>
        /// <value><c>true</c> if inline; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool Inline { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether time only can be set.
        /// </summary>
        /// <value><c>true</c> if time only can be set; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool TimeOnly { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether read only.
        /// </summary>
        /// <value><c>true</c> if read only; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool ReadOnly { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether input is allowed.
        /// </summary>
        /// <value><c>true</c> if input is allowed; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool AllowInput { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether popup datepicker button is shown.
        /// </summary>
        /// <value><c>true</c> if need show button open datepicker popup; <c>false</c> if need hide button, click for input field open datepicker popup.</value>
        [Parameter]
        public bool ShowButton { get; set; } = true;

        private bool IsReadonly => ReadOnly || !AllowInput;

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="RadzenDatePicker{TValue}"/> is disabled.
        /// </summary>
        /// <value><c>true</c> if disabled; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool Disabled { get; set; }

        /// <summary>
        /// Gets or sets the FormFieldContext of the component
        /// </summary>
        [CascadingParameter]
        public IFormFieldContext FormFieldContext { get; set; } = null;

        /// <summary>
        /// Gets or sets a value indicating whether days part is shown.
        /// </summary>
        /// <value><c>true</c> if days part is shown; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool ShowDays { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether time part is shown.
        /// </summary>
        /// <value><c>true</c> if time part is shown; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool ShowTime { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether seconds are shown.
        /// </summary>
        /// <value><c>true</c> if seconds are shown; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool ShowSeconds { get; set; }

        /// <summary>
        /// Gets or sets the hours step.
        /// </summary>
        /// <value>The hours step.</value>
        [Parameter]
        public string HoursStep { get; set; }

        /// <summary>
        /// Gets or sets the minutes step.
        /// </summary>
        /// <value>The minutes step.</value>
        [Parameter]
        public string MinutesStep { get; set; }

        /// <summary>
        /// Gets or sets the seconds step.
        /// </summary>
        /// <value>The seconds step.</value>
        [Parameter]
        public string SecondsStep { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the hour picker is padded with a leading zero.
        /// </summary>
        /// <value><c>true</c> if hour component is padded; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool PadHours { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the minute picker is padded with a leading zero.
        /// </summary>
        /// <value><c>true</c> if hour component is padded; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool PadMinutes { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the second picker is padded with a leading zero.
        /// </summary>
        /// <value><c>true</c> if hour component is padded; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool PadSeconds { get; set; }

        enum StepType
        {
            /// <summary>
            /// The hours
            /// </summary>
            Hours,
            /// <summary>
            /// The minutes
            /// </summary>
            Minutes,
            /// <summary>
            /// The seconds
            /// </summary>
            Seconds
        }

        double getStep(StepType type)
        {
            double step = 1;

            if (type == StepType.Hours)
            {
                step = parseStep(HoursStep);
            }
            else if (type == StepType.Minutes)
            {
                step = parseStep(MinutesStep);
            }
            else if (type == StepType.Seconds)
            {
                step = parseStep(SecondsStep);
            }

            return step;
        }

        double parseStep(string step)
        {
            return string.IsNullOrEmpty(step) || step == "any" ? 1 : double.Parse(step.Replace(",", "."), CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Gets or sets a value indicating whether time ok button is shown.
        /// </summary>
        /// <value><c>true</c> if time ok button is shown; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool ShowTimeOkButton { get; set; } = true;

        /// <summary>
        /// Gets or sets the date format.
        /// </summary>
        /// <value>The date format.</value>
        [Parameter]
        public string DateFormat { get; set; }

        /// <summary>
        /// Gets or sets the year range.
        /// </summary>
        /// <value>The year range.</value>
        [Parameter]
        public string YearRange { get; set; } = $"1950:{DateTime.Now.AddYears(30).Year}";

        /// <summary>
        /// Gets or sets the hour format.
        /// </summary>
        /// <value>The hour format.</value>
        [Parameter]
        public string HourFormat { get; set; } = "24";

        /// <summary>
        /// Gets or sets the input placeholder.
        /// </summary>
        /// <value>The input placeholder.</value>
        [Parameter]
        public string Placeholder { get; set; }

        /// <summary>
        /// Gets or sets the change callback.
        /// </summary>
        /// <value>The change callback.</value>
        [Parameter]
        public EventCallback<DateTime?> Change { get; set; }

        /// <summary>
        /// Gets or sets the value changed callback.
        /// </summary>
        /// <value>The value changed callback.</value>
        [Parameter]
        public EventCallback<TValue> ValueChanged { get; set; }

        /// <summary>
        /// Gets or sets the footer template.
        /// </summary>
        /// <value>The footer template.</value>
        [Parameter]
        public RenderFragment FooterTemplate { get; set; }

        string contentStyle = "display:none;";

        private string getStyle()
        {
            return $"{(Inline ? "overflow:auto;" : "")}{(Style != null ? Style : "")}";
        }

        /// <summary> Gets the current placeholder. Returns empty string if this component is inside a RadzenFormField.</summary>
        protected string CurrentPlaceholder => FormFieldContext?.AllowFloatingLabel == true ? " " : Placeholder;

        /// <summary>
        /// Closes this instance popup.
        /// </summary>
        public void Close()
        {
            if (Disabled || ReadOnly || Inline)
                return;

            if (PopupRenderMode == PopupRenderMode.OnDemand)
            {
                InvokeAsync(() => popup.CloseAsync(Element));
            }
            else
            {
                JSRuntime.InvokeVoidAsync("Radzen.closePopup", PopupID);
            }

            contentStyle = "display:none;";
            StateHasChanged();
        }

        private string PopupStyle
        {
            get
            {
                if (Inline)
                {
                    return "";
                }
                else
                {
                    return $"{contentStyle}";
                }
            }
        }

        async Task OnChange()
        {
            if ((typeof(TValue) == typeof(DateTimeOffset) || typeof(TValue) == typeof(DateTimeOffset?)) && Value != null)
            {
                DateTimeOffset? offset = DateTime.SpecifyKind((DateTime)Value, Kind);
                await ValueChanged.InvokeAsync((TValue)(object)offset);
            }
            else if ((typeof(TValue) == typeof(DateTime) || typeof(TValue) == typeof(DateTime?)) && Value != null)
            {
                await ValueChanged.InvokeAsync((TValue)(object)DateTime.SpecifyKind((DateTime)Value, Kind));
            }
            else
            {
                await ValueChanged.InvokeAsync((TValue)Value);
            }

            if (FieldIdentifier.FieldName != null) { EditContext?.NotifyFieldChanged(FieldIdentifier); }
            await Change.InvokeAsync(DateTimeValue);

        }

        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            return ClassList.Create("rz-datepicker")
                            .Add("rz-datepicker-inline", Inline)
                            .AddDisabled(Disabled)
                            .Add("rz-state-empty", !HasValue)
                            .Add(FieldIdentifier, EditContext)
                            .ToString();

        }

        private async Task SetDay(DateTime newValue)
        {
            if (ShowTimeOkButton)
            {
                CurrentDate = new DateTime(newValue.Year, newValue.Month, newValue.Day, CurrentDate.Hour, CurrentDate.Minute, CurrentDate.Second);
                await OkClick(!ShowTime);
            }
            else
            {
                var v = new DateTime(newValue.Year, newValue.Month, newValue.Day, CurrentDate.Hour, CurrentDate.Minute, CurrentDate.Second);
                if (v != DateTimeValue)
                {
                    DateTimeValue = v;
                    await OnChange();
                    Close();
                }
            }
            await FocusAsync();
        }

        private void SetMonth(int month)
        {
            var currentValue = CurrentDate;
            var newValue = new DateTime(currentValue.Year, month, Math.Min(currentValue.Day, DateTime.DaysInMonth(currentValue.Year, month)), currentValue.Hour, currentValue.Minute, currentValue.Second);

            CurrentDate = newValue;
        }

        private void SetYear(int year)
        {
            var currentValue = CurrentDate;
            var newValue = new DateTime(year, currentValue.Month, Math.Min(currentValue.Day, DateTime.DaysInMonth(year, currentValue.Month)), currentValue.Hour, currentValue.Minute, currentValue.Second);

            CurrentDate = newValue;
        }

        /// <summary>
        /// Gets or sets the edit context.
        /// </summary>
        /// <value>The edit context.</value>
        [CascadingParameter]
        public EditContext EditContext { get; set; }

        /// <summary>
        /// Gets the field identifier.
        /// </summary>
        /// <value>The field identifier.</value>
        public FieldIdentifier FieldIdentifier { get; private set; }

        /// <summary>
        /// Gets or sets the value expression.
        /// </summary>
        /// <value>The value expression.</value>
        [Parameter]
        public Expression<Func<TValue>> ValueExpression { get; set; }

        /// <inheritdoc />
        public override async Task SetParametersAsync(ParameterView parameters)
        {
            if (parameters.DidParameterChange(nameof(Min), Min) || parameters.DidParameterChange(nameof(Max), Max))
            {
                var min = parameters.GetValueOrDefault<DateTime?>(nameof(Min));
                var max = parameters.GetValueOrDefault<DateTime?>(nameof(Max));
                UpdateYearsAndMonths(min, max);
            }

            var shouldClose = false;

            if (parameters.DidParameterChange(nameof(Visible), Visible))
            {
                var visible = parameters.GetValueOrDefault<bool>(nameof(Visible));
                shouldClose = !visible;
            }

            var disabledChanged = parameters.DidParameterChange(nameof(Disabled), Disabled);

            await base.SetParametersAsync(parameters);

            if (disabledChanged)
            {
                FormFieldContext?.DisabledChanged(Disabled);
            }

            if (shouldClose && !firstRender && IsJSRuntimeAvailable)
            {
                await JSRuntime.InvokeVoidAsync("Radzen.destroyPopup", PopupID);
            }

            if (EditContext != null && ValueExpression != null && FieldIdentifier.Model != EditContext.Model)
            {
                FieldIdentifier = FieldIdentifier.Create(ValueExpression);
                EditContext.OnValidationStateChanged -= ValidationStateChanged;
                EditContext.OnValidationStateChanged += ValidationStateChanged;
            }
        }

        bool firstRender;

        /// <inheritdoc />
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);

            this.firstRender = firstRender;

            if (Visible && !Disabled && !ReadOnly && !Inline && PopupRenderMode == PopupRenderMode.Initial)
            {
                await JSRuntime.InvokeVoidAsync("Radzen.createDatePicker", Element, PopupID);
            }
        }

        private void ValidationStateChanged(object sender, ValidationStateChangedEventArgs e)
        {
            StateHasChanged();
        }

        /// <inheritdoc />
        public override void Dispose()
        {
            base.Dispose();

            if (EditContext != null)
            {
                EditContext.OnValidationStateChanged -= ValidationStateChanged;
            }

            Form?.RemoveComponent(this);

            if (IsJSRuntimeAvailable)
            {
                JSRuntime.InvokeVoidAsync("Radzen.destroyPopup", PopupID);
                JSRuntime.InvokeVoidAsync("Radzen.destroyDatePicker", UniqueID, Element);
            }
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <returns>System.Object.</returns>
        public object GetValue()
        {
            return Value;
        }

        private string PopupID
        {
            get
            {
                return $"popup{GetId()}";
            }
        }

        Popup popup;

        /// <summary>
        /// Gets or sets the render mode.
        /// </summary>
        /// <value>The render mode.</value>
        [Parameter]
        public PopupRenderMode PopupRenderMode { get; set; } = PopupRenderMode.Initial;

        async Task OnToggle()
        {
            if (PopupRenderMode == PopupRenderMode.OnDemand && !Disabled && !ReadOnly && !Inline)
            {
                await popup.ToggleAsync(Element);
                await FocusAsync();
            }
        }
        DateTime FocusedDate { get; set; } = DateTime.Now;

        string GetDayCssClass(DateTime date, DateRenderEventArgs dateArgs, bool forCell = true)
        {
            var list = ClassList.Create()
                               .Add("rz-state-default", !forCell)
                               .Add("rz-calendar-other-month", CurrentDate.Month != date.Month)
                               .Add("rz-state-active", !forCell && DateTimeValue.HasValue && DateTimeValue.Value.Date.CompareTo(date.Date) == 0)
                               .Add("rz-calendar-today", !forCell && DateTime.Now.Date.CompareTo(date.Date) == 0)
                               .Add("rz-state-focused", !forCell && FocusedDate.Date.CompareTo(date.Date) == 0)
                               .Add("rz-state-disabled", !forCell && dateArgs.Disabled);

            if (dateArgs.Attributes != null && dateArgs.Attributes.TryGetValue("class", out var @class) && !string.IsNullOrEmpty(Convert.ToString(@class)))
            {
                list.Add($"{@class}", true);
            }

            return list.ToString();
        }
        async Task OnCalendarKeyPress(KeyboardEventArgs args)
        {
            var key = args.Code != null ? args.Code : args.Key;

            if (key == "ArrowLeft" || key == "ArrowRight")
            {
                preventKeyPress = true;

                FocusedDate = FocusedDate.AddDays(key == "ArrowLeft" ? -1 : 1);
                CurrentDate = FocusedDate;
            }
            else if (key == "ArrowUp" || key == "ArrowDown")
            {
                preventKeyPress = true;

                FocusedDate = FocusedDate.AddDays(key == "ArrowUp" ? -7 : 7);
                CurrentDate = FocusedDate;
            }
            else if (key == "Enter")
            {
                preventKeyPress = true;

                if (!DateAttributes(FocusedDate).Disabled)
                {
                    await SetDay(FocusedDate);

                    await ClosePopup();
                    await FocusAsync();
                }
            }
            else if (key == "Escape")
            {
                preventKeyPress = false;

                await ClosePopup();
                await FocusAsync();
            }
            else if (key == "Tab")
            {
                preventKeyPress = false;

                await ClosePopup();
                await FocusAsync();
            }
            else
            {
                preventKeyPress = false;
            }
        }

        async Task OnPopupKeyDown(KeyboardEventArgs args)
        {
            var key = args.Code != null ? args.Code : args.Key;
            if(key == "Escape")
            {
                preventKeyPress = false;

                await ClosePopup();
                await FocusAsync();
            }
        }

        async Task OnKeyPress(KeyboardEventArgs args)
        {
            var key = args.Code != null ? args.Code : args.Key;

            if (args.AltKey && key == "ArrowDown")
            {
                preventKeyPress = true;

                if (PopupRenderMode == PopupRenderMode.Initial)
                {
                    await JSRuntime.InvokeVoidAsync("Radzen.openPopup", Element, PopupID, false, null, null, null, null, null, true, true);
                }
                else
                {
                    await popup.CloseAsync(Element);
                    await popup.ToggleAsync(Element);
                }
            }
            else if (key == "Enter")
            {
                preventKeyPress = true;

                await TogglePopup();
            }
            else if (key == "Escape")
            {
                preventKeyPress = false;

                await ClosePopup();
                await FocusAsync();
            }
            else
            {
                preventKeyPress = false;
            }
        }

        internal async Task TogglePopup()
        {
            if (Inline) return;

            if (PopupRenderMode == PopupRenderMode.Initial)
            {
                await JSRuntime.InvokeVoidAsync("Radzen.togglePopup", Element, PopupID, false, null, null, true, true);
            }
            else
            {
                await popup.ToggleAsync(Element);
            }
        }

        async Task ClosePopup()
        {
            if (Inline) return;

            if (PopupRenderMode == PopupRenderMode.Initial)
            {
                await JSRuntime.InvokeVoidAsync("Radzen.closePopup", PopupID);
            }
            else
            {
                await popup.CloseAsync(Element);
            }
        }

        bool preventKeyPress = false;

        /// <inheritdoc/>
        public async ValueTask FocusAsync()
        {
           try
           {
               await input.FocusAsync();
            }
            catch
            {}
        }
    }
}
