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
    /// Class RadzenDatePicker.
    /// Implements the <see cref="Radzen.RadzenComponent" />
    /// Implements the <see cref="Radzen.IRadzenFormComponent" />
    /// </summary>
    /// <typeparam name="TValue">The type of the t value.</typeparam>
    /// <seealso cref="Radzen.RadzenComponent" />
    /// <seealso cref="Radzen.IRadzenFormComponent" />
    public partial class RadzenDatePicker<TValue> : RadzenComponent, IRadzenFormComponent
    {
        /// <summary>
        /// The month drop down
        /// </summary>
        RadzenDropDown<int> monthDropDown;
        /// <summary>
        /// The year drop down
        /// </summary>
        RadzenDropDown<int> yearDropDown;

        /// <summary>
        /// Ams to pm.
        /// </summary>
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

                if (!object.Equals(newValue, Value))
                {
                    await UpdateValueFromTime(newValue);
                }
            }
        }

        /// <summary>
        /// Pms to am.
        /// </summary>
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

                if (!object.Equals(newValue, Value))
                {
                    await UpdateValueFromTime(newValue);
                }
            }
        }

        /// <summary>
        /// The hour
        /// </summary>
        int? hour;
        /// <summary>
        /// Handles the <see cref="E:UpdateHourInput" /> event.
        /// </summary>
        /// <param name="args">The <see cref="ChangeEventArgs"/> instance containing the event data.</param>
        void OnUpdateHourInput(ChangeEventArgs args)
        {
            var value = $"{args.Value}";
            if (!string.IsNullOrWhiteSpace(value))
            {
                hour = (int)Convert.ChangeType(value, typeof(int));
            }
        }

        /// <summary>
        /// The minutes
        /// </summary>
        int? minutes;
        /// <summary>
        /// Handles the <see cref="E:UpdateHourMinutes" /> event.
        /// </summary>
        /// <param name="args">The <see cref="ChangeEventArgs"/> instance containing the event data.</param>
        void OnUpdateHourMinutes(ChangeEventArgs args)
        {
            var value = $"{args.Value}";
            if (!string.IsNullOrWhiteSpace(value))
            {
                minutes = (int)Convert.ChangeType(value, typeof(int));
            }
        }

        /// <summary>
        /// The seconds
        /// </summary>
        int? seconds;
        /// <summary>
        /// Handles the <see cref="E:UpdateHourSeconds" /> event.
        /// </summary>
        /// <param name="args">The <see cref="ChangeEventArgs"/> instance containing the event data.</param>
        void OnUpdateHourSeconds(ChangeEventArgs args)
        {
            var value = $"{args.Value}";
            if (!string.IsNullOrWhiteSpace(value))
            {
                seconds = (int)Convert.ChangeType(value, typeof(int));
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

        /// <summary>
        /// Updates the hour.
        /// </summary>
        /// <param name="v">The v.</param>
        async Task UpdateHour(int v)
        {
            var newHour = HourFormat == "12" && CurrentDate.Hour > 12 ? v + 12 : v;

            var newValue = new DateTime(CurrentDate.Year, CurrentDate.Month, CurrentDate.Day, newHour > 23 || newHour < 0 ? 0 : newHour, CurrentDate.Minute, CurrentDate.Second);

            if (!object.Equals(newValue, Value))
            {
                hour = newValue.Hour;
                await UpdateValueFromTime(newValue);
            }
        }

        /// <summary>
        /// Updates the minutes.
        /// </summary>
        /// <param name="v">The v.</param>
        async Task UpdateMinutes(int v)
        {
            var newValue = new DateTime(CurrentDate.Year, CurrentDate.Month, CurrentDate.Day, CurrentDate.Hour, v, CurrentDate.Second);

            if (!object.Equals(newValue, Value))
            {
                minutes = newValue.Minute;
                await UpdateValueFromTime(newValue);
            }
        }

        /// <summary>
        /// Updates the seconds.
        /// </summary>
        /// <param name="v">The v.</param>
        async Task UpdateSeconds(int v)
        {
            var newValue = new DateTime(CurrentDate.Year, CurrentDate.Month, CurrentDate.Day, CurrentDate.Hour, CurrentDate.Minute, v);

            if (!object.Equals(newValue, Value))
            {
                seconds = newValue.Second;
                await UpdateValueFromTime(newValue);
            }
        }

        /// <summary>
        /// Oks the click.
        /// </summary>
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

        /// <summary>
        /// Class NameValue.
        /// </summary>
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

        /// <summary>
        /// The months
        /// </summary>
        IList<NameValue> months;
        /// <summary>
        /// The years
        /// </summary>
        IList<NameValue> years;

        /// <summary>
        /// Called when [initialized].
        /// </summary>
        protected override void OnInitialized()
        {
            base.OnInitialized();

            months = Enumerable.Range(1, 12).Select(i => new NameValue() { Name = Culture.DateTimeFormat.GetMonthName(i), Value = i }).ToList();
            years = Enumerable.Range(int.Parse(YearRange.Split(':').First()), int.Parse(YearRange.Split(':').Last()) - int.Parse(YearRange.Split(':').First()) + 1)
                .Select(i => new NameValue() { Name = $"{i}", Value = i }).ToList();
        }

        /// <summary>
        /// Gets or sets a value indicating whether [allow clear].
        /// </summary>
        /// <value><c>true</c> if [allow clear]; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool AllowClear { get; set; }

        /// <summary>
        /// Gets or sets the index of the tab.
        /// </summary>
        /// <value>The index of the tab.</value>
        [Parameter]
        public int TabIndex { get; set; } = 0;

        /// <summary>
        /// The am pm
        /// </summary>
        string amPm = "am";

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        [Parameter]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the input class.
        /// </summary>
        /// <value>The input class.</value>
        [Parameter]
        public string InputClass { get; set; }

        /// <summary>
        /// The date time value
        /// </summary>
        DateTime? _dateTimeValue;

        /// <summary>
        /// Gets or sets the date time value.
        /// </summary>
        /// <value>The date time value.</value>
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
        /// Gets or sets the date render.
        /// </summary>
        /// <value>The date render.</value>
        [Parameter]
        public Action<DateRenderEventArgs> DateRender { get; set; }

        /// <summary>
        /// Dates the attributes.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>DateRenderEventArgs.</returns>
        DateRenderEventArgs DateAttributes(DateTime value)
        {
            var args = new Radzen.DateRenderEventArgs() { Date = value, Disabled = false };

            if (DateRender != null)
            {
                DateRender(args);
            }

            return args;
        }

        /// <summary>
        /// The value
        /// </summary>
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

                    DateTimeOffset? offset = value as DateTimeOffset?;
                    if (offset != null && offset.HasValue)
                    {
                        _dateTimeValue = offset.Value.DateTime;
                        _value = _dateTimeValue;
                    }
                    else
                    {
                        DateTimeValue = value as DateTime?;

                        if (DateTimeValue.HasValue && DateTimeValue.Value == default(DateTime))
                        {
                            _value = null;
                            _dateTimeValue = null;
                        }
                    }
                }
            }
        }

        DateTime _currentDate;
        /// <summary>
        /// Gets the current date.
        /// </summary>
        /// <value>The current date.</value>
        private DateTime CurrentDate
        {
            get
            {
                if (_currentDate == default(DateTime))
                {
                    _currentDate = HasValue && DateTimeValue.Value != default(DateTime) ? DateTimeValue.Value : DateTime.Today;
                }
                return _currentDate;
            }
            set 
            {
                _currentDate = value;
            }
        }

        /// <summary>
        /// Gets the start date.
        /// </summary>
        /// <value>The start date.</value>
        private DateTime StartDate
        {
            get
            {
                var firstDayOfTheMonth = new DateTime(CurrentDate.Year, CurrentDate.Month, 1);

                int diff = (7 + (firstDayOfTheMonth.DayOfWeek - Culture.DateTimeFormat.FirstDayOfWeek)) % 7;
                return firstDayOfTheMonth.AddDays(-1 * diff).Date;
            }
        }

        /// <summary>
        /// The abbreviated day names
        /// </summary>
        IList<string> _abbreviatedDayNames;
        /// <summary>
        /// Gets the abbreviated day names.
        /// </summary>
        /// <value>The abbreviated day names.</value>
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
        /// Gets a value indicating whether this instance is bound.
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

        /// <summary>
        /// The form
        /// </summary>
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
        /// The input
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
                newValue = value;
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

        /// <summary>
        /// Clears this instance.
        /// </summary>
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
        /// Gets or sets a value indicating whether this <see cref="RadzenDatePicker{TValue}"/> is inline.
        /// </summary>
        /// <value><c>true</c> if inline; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool Inline { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [time only].
        /// </summary>
        /// <value><c>true</c> if [time only]; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool TimeOnly { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [read only].
        /// </summary>
        /// <value><c>true</c> if [read only]; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool ReadOnly { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [allow input].
        /// </summary>
        /// <value><c>true</c> if [allow input]; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool AllowInput { get; set; } = true;

        /// <summary>
        /// Gets a value indicating whether this instance is readonly.
        /// </summary>
        /// <value><c>true</c> if this instance is readonly; otherwise, <c>false</c>.</value>
        private bool IsReadonly => ReadOnly || !AllowInput;

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="RadzenDatePicker{TValue}"/> is disabled.
        /// </summary>
        /// <value><c>true</c> if disabled; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool Disabled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [show time].
        /// </summary>
        /// <value><c>true</c> if [show time]; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool ShowTime { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [show seconds].
        /// </summary>
        /// <value><c>true</c> if [show seconds]; otherwise, <c>false</c>.</value>
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
        /// Enum StepType
        /// </summary>
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

        /// <summary>
        /// Gets the step.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>System.Double.</returns>
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


        /// <summary>
        /// Parses the step.
        /// </summary>
        /// <param name="step">The step.</param>
        /// <returns>System.Double.</returns>
        double parseStep(string step)
        {
            return string.IsNullOrEmpty(step) || step == "any" ? 1 : double.Parse(step.Replace(",", "."), System.Globalization.CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Gets or sets a value indicating whether [show time ok button].
        /// </summary>
        /// <value><c>true</c> if [show time ok button]; otherwise, <c>false</c>.</value>
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
        public string YearRange { get; set; } = "1950:2050";
        /*
        [Parameter]
        public string SelectionMode { get; set; } = "single";
        */
        /// <summary>
        /// Gets or sets the hour format.
        /// </summary>
        /// <value>The hour format.</value>
        [Parameter]
        public string HourFormat { get; set; } = "24";

        /*
        [Parameter]
        public bool Utc { get; set; } = true;
        */

        /// <summary>
        /// Gets or sets the placeholder.
        /// </summary>
        /// <value>The placeholder.</value>
        [Parameter]
        public string Placeholder { get; set; }

        /// <summary>
        /// Gets or sets the change.
        /// </summary>
        /// <value>The change.</value>
        [Parameter]
        public EventCallback<DateTime?> Change { get; set; }

        /// <summary>
        /// Gets or sets the value changed.
        /// </summary>
        /// <value>The value changed.</value>
        [Parameter]
        public EventCallback<TValue> ValueChanged { get; set; }

        /// <summary>
        /// The content style
        /// </summary>
        string contentStyle = "display:none;";

        /// <summary>
        /// Gets the style.
        /// </summary>
        /// <returns>System.String.</returns>
        private string getStyle()
        {
            return $"display: inline-block;{(Inline ? "overflow:auto;" : "")}{(Style != null ? Style : "")}";
        }

        /// <summary>
        /// Closes this instance.
        /// </summary>
        public void Close()
        {
            if (!Disabled)
            {
                contentStyle = "display:none;";
                StateHasChanged();
            }
        }

        /// <summary>
        /// Gets the popup style.
        /// </summary>
        /// <value>The popup style.</value>
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

        /// <summary>
        /// Called when [change].
        /// </summary>
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

        /// <summary>
        /// Gets the component CSS class.
        /// </summary>
        /// <returns>System.String.</returns>
        protected override string GetComponentCssClass()
        {
            return ClassList.Create()
                            .Add("rz-calendar-inline", Inline)
                            .Add(FieldIdentifier, EditContext)
                            .ToString();
        }

        /// <summary>
        /// Sets the day.
        /// </summary>
        /// <param name="newValue">The new value.</param>
        private async System.Threading.Tasks.Task SetDay(DateTime newValue)
        {
            if (ShowTimeOkButton)
            {
                CurrentDate = new DateTime(CurrentDate.Year, newValue.Month,newValue.Day, CurrentDate.Hour, CurrentDate.Minute, CurrentDate.Second);
                await OkClick();
            }
            else
            {
                if (newValue != DateTimeValue)
                {
                    DateTimeValue = newValue;
                    await OnChange();
                    Close();
                }
            }
        }

        /// <summary>
        /// Sets the month.
        /// </summary>
        /// <param name="month">The month.</param>
        private async System.Threading.Tasks.Task SetMonth(int month)
        {
            var currentValue = CurrentDate;
            var newValue = new DateTime(currentValue.Year, month, Math.Min(currentValue.Day, DateTime.DaysInMonth(currentValue.Year, month)), currentValue.Hour, currentValue.Minute, currentValue.Second);

            CurrentDate = newValue;
            Close();
        }

        /// <summary>
        /// Sets the year.
        /// </summary>
        /// <param name="year">The year.</param>
        private async System.Threading.Tasks.Task SetYear(int year)
        {
            var currentValue = CurrentDate;
            var newValue = new DateTime(year, currentValue.Month, Math.Min(currentValue.Day, DateTime.DaysInMonth(year, currentValue.Month)), currentValue.Hour, currentValue.Minute, currentValue.Second);

            CurrentDate = newValue;
            Close();
        }

        /// <summary>
        /// Gets the open popup.
        /// </summary>
        /// <returns>System.String.</returns>
        private string getOpenPopup()
        {
            return !Disabled && !ReadOnly && !Inline ? $"Radzen.togglePopup(this.parentNode, '{PopupID}')" : "";
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

        /// <summary>
        /// Set parameters as an asynchronous operation.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
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

        /// <summary>
        /// Validations the state changed.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="ValidationStateChangedEventArgs"/> instance containing the event data.</param>
        private void ValidationStateChanged(object sender, ValidationStateChangedEventArgs e)
        {
            StateHasChanged();
        }

        /// <summary>
        /// Disposes this instance.
        /// </summary>
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

        /// <summary>
        /// Gets the popup identifier.
        /// </summary>
        /// <value>The popup identifier.</value>
        private string PopupID
        {
            get
            {
                return $"popup{UniqueID}";
            }
        }

        /// <summary>
        /// The first render
        /// </summary>
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