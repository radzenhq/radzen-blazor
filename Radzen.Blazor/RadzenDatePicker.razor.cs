using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
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
        RadzenDropDown<int> monthDropDown;
        RadzenDropDown<int> yearDropDown;

        async Task AmToPm()
        {
            if (amPm == "am" && !Disabled)
            {
                amPm = "pm";

                var currentHour = ((CurrentDate.Hour + 11) % 12) + 1;

                var newHour = currentHour - 12;

                if (newHour < 1)
                {
                    newHour = currentHour;
                }

                var newValue = new DateTime(CurrentDate.Year, CurrentDate.Month, CurrentDate.Day, newHour, CurrentDate.Minute, CurrentDate.Second);

                await UpdateValueFromTime(newValue);
            }
        }

        async Task PmToAm()
        {
            if (amPm == "pm" && !Disabled)
            {
                amPm = "am";

                var currentHour = ((CurrentDate.Hour + 11) % 12) + 1;

                var newHour = currentHour + 12;

                if (newHour > 23)
                {
                    newHour = 0;
                }

                var newValue = new DateTime(CurrentDate.Year, CurrentDate.Month, CurrentDate.Day, newHour, CurrentDate.Minute, CurrentDate.Second);

                await UpdateValueFromTime(newValue);
            }
        }

        int? hour;

        void OnUpdateHourInput(ChangeEventArgs args)
        {
            var value = $"{args.Value}";
            if (!string.IsNullOrWhiteSpace(value))
            {
                int outValue;
                hour = int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out outValue) ? (int?)outValue : null;
            }
        }


        int? minutes;

        void OnUpdateHourMinutes(ChangeEventArgs args)
        {
            var value = $"{args.Value}";
            if (!string.IsNullOrWhiteSpace(value))
            {
                int outValue;
                minutes = int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out outValue) ? (int?)outValue : null;

            }
        }

        int? seconds;

        void OnUpdateHourSeconds(ChangeEventArgs args)
        {
            var value = $"{args.Value}";
            if (!string.IsNullOrWhiteSpace(value))
            {
                int outValue;
                seconds = int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out outValue) ? (int?)outValue : null;
            }
        }

        async Task UpdateValueFromTime(DateTime newValue)
        {
            if (ShowTimeOkButton)
            {
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
            var newHour = HourFormat == "12" && CurrentDate.Hour > 12 ? v + 12 : v;
            var newMinute = CurrentDate.Minute;
            var newSecond = CurrentDate.Second;

            if (v < 0)
            {
                newHour = 23;
                newMinute = 59;
                newSecond = 59;
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

        async Task OkClick()
        {
            if (!Disabled)
            {
                DateTime date = CurrentDate;

                if (CurrentDate.Hour != hour && hour != null)
                {
                    var newHour = HourFormat == "12" && CurrentDate.Hour > 12 ? hour.Value + 12 : hour.Value;
                    date = new DateTime(CurrentDate.Year, CurrentDate.Month, CurrentDate.Day, newHour > 23 || newHour < 0 ? 0 : newHour, CurrentDate.Minute, CurrentDate.Second);
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
                    await monthDropDown.ClosePopup();
                }

                if (yearDropDown != null)
                {
                    await yearDropDown.ClosePopup();
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

            YearFrom = Min.HasValue ? Min.Value.Year : int.Parse(YearRange.Split(':').First());
            YearTo = Max.HasValue ? Max.Value.Year : int.Parse(YearRange.Split(':').Last());
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

        string amPm = "am";

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
            var args = new Radzen.DateRenderEventArgs() { Date = value, Disabled = (Min.HasValue && value < Min.Value) || (Max.HasValue && value > Max.Value) };

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
                if (_value != value)
                {
                    _value = value;
                    _currentDate = default(DateTime);

                    DateTimeOffset? offset = value as DateTimeOffset?;
                    if (offset != null && offset.HasValue)
                    {
                        _dateTimeValue = offset.Value.DateTime;
                        _value = _dateTimeValue;
                    }
                    else
                    {
                        if (value is DateTime dateTime)
                        {
                            DateTimeValue = DateTime.SpecifyKind(dateTime, Kind);
                        }
                        else
                        {
                            DateTimeValue = null;
                        }
                    }
                }
            }
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

        IList<string> _abbreviatedDayNames;

        IList<string> AbbreviatedDayNames
        {
            get
            {
                if (_abbreviatedDayNames == null)
                {
                    _abbreviatedDayNames = new List<string>();

                    for (int i = (int)Culture.DateTimeFormat.FirstDayOfWeek; i < 7; i++)
                    {
                        _abbreviatedDayNames.Add(Culture.DateTimeFormat.AbbreviatedDayNames[i]);
                    }

                    for (int i = 0; i < (int)Culture.DateTimeFormat.FirstDayOfWeek; i++)
                    {
                        _abbreviatedDayNames.Add(Culture.DateTimeFormat.AbbreviatedDayNames[i]);
                    }
                }
                return _abbreviatedDayNames;
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
                return DateTimeValue.HasValue;
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
                return string.Format("{0:" + DateFormat + "}", Value);
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
            DateTime value;
            var inputValue = await JSRuntime.InvokeAsync<string>("Radzen.getInputValue", input);

            var valid = DateTime.TryParseExact(inputValue, DateFormat, null, DateTimeStyles.None, out value);
            var nullable = Nullable.GetUnderlyingType(typeof(TValue)) != null;

            if (!valid)
            {
                valid = DateTime.TryParse(inputValue, out value);
            }

            if (valid && !DateAttributes(value).Disabled)
            {
                newValue = TimeOnly && CurrentDate != null ? new DateTime(CurrentDate.Year, CurrentDate.Month, CurrentDate.Day, value.Hour, value.Minute, value.Second) : value;
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
                    DateTimeOffset? offset = DateTime.SpecifyKind((DateTime)Value, DateTimeKind.Utc);
                    await ValueChanged.InvokeAsync((TValue)(object)offset);
                }
                else
                {
                    await ValueChanged.InvokeAsync((TValue)Value);
                }

                if (FieldIdentifier.FieldName != null)
                {
                    EditContext?.NotifyFieldChanged(FieldIdentifier);
                }

                await Change.InvokeAsync(DateTimeValue);
                StateHasChanged();
            }
        }

        async Task Clear()
        {
            Value = null;

            await ValueChanged.InvokeAsync(default(TValue));

            if (FieldIdentifier.FieldName != null)
            {
                EditContext?.NotifyFieldChanged(FieldIdentifier);
            }

            await Change.InvokeAsync(DateTimeValue);
            StateHasChanged();
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

        private bool IsReadonly => ReadOnly || !AllowInput;

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="RadzenDatePicker{TValue}"/> is disabled.
        /// </summary>
        /// <value><c>true</c> if disabled; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool Disabled { get; set; }

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
            return string.IsNullOrEmpty(step) || step == "any" ? 1 : double.Parse(step.Replace(",", "."), System.Globalization.CultureInfo.InvariantCulture);
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
            return $"display: inline-block;{(Inline ? "overflow:auto;" : "")}{(Style != null ? Style : "")}";
        }

        /// <summary>
        /// Closes this instance popup.
        /// </summary>
        public void Close()
        {
            if (!Disabled)
            {
                contentStyle = "display:none;";
                StateHasChanged();
            }
        }

        private string PopupStyle
        {
            get
            {
                if (Inline)
                {
                    return "white-space: nowrap";
                }
                else
                {
                    return $"width: 320px; {contentStyle}";
                }
            }
        }

        async System.Threading.Tasks.Task OnChange()
        {
            if ((typeof(TValue) == typeof(DateTimeOffset) || typeof(TValue) == typeof(DateTimeOffset?)) && Value != null)
            {
                DateTimeOffset? offset = DateTime.SpecifyKind((DateTime)Value, DateTimeKind.Utc);
                await ValueChanged.InvokeAsync((TValue)(object)offset);
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
            return ClassList.Create()
                            .Add("rz-calendar-inline", Inline)
                            .Add(FieldIdentifier, EditContext)
                            .ToString();
        }

        private async System.Threading.Tasks.Task SetDay(DateTime newValue)
        {
            if (ShowTimeOkButton)
            {
                CurrentDate = new DateTime(newValue.Year, newValue.Month, newValue.Day, CurrentDate.Hour, CurrentDate.Minute, CurrentDate.Second);
                await OkClick();
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
        }

        private void SetMonth(int month)
        {
            var currentValue = CurrentDate;
            var newValue = new DateTime(currentValue.Year, month, Math.Min(currentValue.Day, DateTime.DaysInMonth(currentValue.Year, month)), currentValue.Hour, currentValue.Minute, currentValue.Second);

            CurrentDate = newValue;
            Close();
        }

        private void SetYear(int year)
        {
            var currentValue = CurrentDate;
            var newValue = new DateTime(year, currentValue.Month, Math.Min(currentValue.Day, DateTime.DaysInMonth(year, currentValue.Month)), currentValue.Hour, currentValue.Minute, currentValue.Second);

            CurrentDate = newValue;
            Close();
        }

        private string getOpenPopup()
        {
            return !Disabled && !ReadOnly && !Inline ? $"Radzen.togglePopup(this.parentNode, '{PopupID}')" : "";
        }

        private string getOpenPopupForInput()
        {
            return !Disabled && !ReadOnly && !Inline && !AllowInput ? $"Radzen.togglePopup(this.parentNode, '{PopupID}')" : "";
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
            var shouldClose = false;

            if (parameters.DidParameterChange(nameof(Visible), Visible))
            {
                var visible = parameters.GetValueOrDefault<bool>(nameof(Visible));
                shouldClose = !visible;
            }

            await base.SetParametersAsync(parameters);

            if (shouldClose && !firstRender)
            {
                await JSRuntime.InvokeVoidAsync("Radzen.destroyPopup", PopupID);
            }

            if (EditContext != null && ValueExpression != null && FieldIdentifier.Model != EditContext.Model)
            {
                FieldIdentifier = FieldIdentifier.Create(ValueExpression);
                EditContext.OnValidationStateChanged += ValidationStateChanged;
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
                return $"popup{UniqueID}";
            }
        }

        private bool firstRender = true;

        /// <summary>
        /// Called when [after render asynchronous].
        /// </summary>
        /// <param name="firstRender">if set to <c>true</c> [first render].</param>
        /// <returns>Task.</returns>
        protected override Task OnAfterRenderAsync(bool firstRender)
        {
            this.firstRender = firstRender;

            return base.OnAfterRenderAsync(firstRender);
        }
    }
}
