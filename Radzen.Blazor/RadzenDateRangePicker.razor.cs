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
    /// <example>
    /// <code>
    /// &lt;RadzenDatePicker @bind-Value=@someValue Change=@(args => Console.WriteLine($"Selected date: {args}")) /&gt;
    /// </code>
    /// </example>
    public partial class RadzenDateRangePicker : RadzenComponent, IRadzenFormComponent
    {
        /// <summary>
        /// Specifies additional custom attributes that will be rendered by the input.
        /// </summary>
        /// <value>The attributes.</value>
        public IReadOnlyDictionary<string, object> InputAttributes { get; set; }

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

        DateTime? _dateTimeStartValue;

        DateTime? DateTimeStartValue
        {
            get
            {
                return _dateTimeStartValue;
            }
            set
            {
                if (_dateTimeStartValue != value)
                {
                    _dateTimeStartValue = value;
                    StartDateValue = value;
                }
            }
        }

        DateTime? _dateTimeEndValue;

        DateTime? DateTimeEndValue
        {
            get
            {
                return _dateTimeEndValue;
            }
            set
            {
                if (_dateTimeEndValue != value)
                {
                    _dateTimeEndValue = value;
                    EndDateValue = value;
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

        object _startDateValue;

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        /// <value>The value.</value>
        [Parameter]
        public object StartDateValue
        {
            get
            {
                return _startDateValue;
            }
            set
            {
                if (_startDateValue != value)
                {
                    _startDateValue = value;
                    _startDate = default(DateTime);

                    if (value is DateTimeOffset offset)
                    {
                        if (offset.Offset == TimeSpan.Zero && Kind == DateTimeKind.Local)
                        {
                            _dateTimeStartValue = offset.LocalDateTime;
                        }
                        else if (offset.Offset != TimeSpan.Zero && Kind == DateTimeKind.Utc)
                        {
                            _dateTimeStartValue = offset.UtcDateTime;
                        }
                        else
                        {
                            _dateTimeStartValue = DateTime.SpecifyKind(offset.DateTime, Kind);
                        }

                        _startDateValue = _dateTimeStartValue;
                    }
                    else
                    {
                        if (value is DateTime dateTime && dateTime != default(DateTime))
                        {
                            DateTimeStartValue = DateTime.SpecifyKind(dateTime, Kind);
                        }
                        else
                        {
                            DateTimeStartValue = null;
                        }
                    }
                }
            }
        }

        object _endDateValue;

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        /// <value>The value.</value>
        [Parameter]
        public object EndDateValue
        {
            get
            {
                return _endDateValue;
            }
            set
            {
                if (_endDateValue != value)
                {
                    _endDateValue = value;
                    _endDate = default(DateTime);

                    if (value is DateTimeOffset offset)
                    {
                        if (offset.Offset == TimeSpan.Zero && Kind == DateTimeKind.Local)
                        {
                            _dateTimeEndValue = offset.LocalDateTime;
                        }
                        else if (offset.Offset != TimeSpan.Zero && Kind == DateTimeKind.Utc)
                        {
                            _dateTimeEndValue = offset.UtcDateTime;
                        }
                        else
                        {
                            _dateTimeEndValue = DateTime.SpecifyKind(offset.DateTime, Kind);
                        }

                        _endDateValue = _dateTimeEndValue;
                    }
                    else
                    {
                        if (value is DateTime dateTime && dateTime != default(DateTime))
                        {
                            DateTimeEndValue = DateTime.SpecifyKind(dateTime, Kind);
                        }
                        else
                        {
                            DateTimeEndValue = null;
                        }
                    }
                }
            }
        }

        DateTime _startDate;
        private DateTime StartDate
        {
            get
            {
                if (_startDate == default(DateTime))
                {
                    _startDate = HasValue ? DateTimeStartValue.Value : InitialViewDate ?? DateTime.Today;
                }
                return _startDate;
            }
            set
            {
                _startDate = value;
                DateRangeChanged.InvokeAsync(value);
            }
        }

        DateTime _endDate;
        private DateTime EndDate
        {
            get
            {
                if (_endDate == default(DateTime))
                {
                    _endDate = HasValue ? DateTimeEndValue.Value : InitialViewDate ?? DateTime.Today;
                }
                return _endDate;
            }
            set
            {
                _endDate = value;
                DateRangeChanged.InvokeAsync(value);
            }
        }

        /// <summary>
        /// Gets or set the current date changed callback.
        /// </summary>
        [Parameter]
        public EventCallback<DateTime> DateRangeChanged { get; set; }

        private DateTime InitialStartDate
        {
            get
            {
                if (StartDate == DateTime.MinValue)
                {
                    return DateTime.MinValue;
                }

                var firstDayOfTheMonth = new DateTime(StartDate.Year, StartDate.Month, 1);

                if (firstDayOfTheMonth == DateTime.MinValue)
                {
                    return DateTime.MinValue;
                }

                int diff = (7 + (firstDayOfTheMonth.DayOfWeek - Culture.DateTimeFormat.FirstDayOfWeek)) % 7;
                return firstDayOfTheMonth.AddDays(-1 * diff).Date;
            }
        }

        private DateTime InitialEndDate
        {
            get
            {
                if (EndDate == DateTime.MinValue)
                {
                    return DateTime.MinValue;
                }

                var firstDayOfTheMonth = new DateTime(EndDate.Year, EndDate.Month, 1);

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
                return DateTimeStartValue.HasValue && DateTimeStartValue != default(DateTime) && DateTimeEndValue.HasValue && DateTimeEndValue != default(DateTime);
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
                return HasValue ? $"{string.Format(Culture, "{0:" + DateFormat + "}", StartDateValue)} - {string.Format(Culture, "{0:" + DateFormat + "}", EndDateValue)}" : "";
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

            StartDateValue = null;
            EndDateValue = null;

            await ValueChanged.InvokeAsync(new RadzenDateRange() { StartDate = DateTimeStartValue, EndDate = DateTimeEndValue });

            if (FieldIdentifier.FieldName != null)
            {
                EditContext?.NotifyFieldChanged(FieldIdentifier);
            }

            await Change.InvokeAsync(new RadzenDateRange() { StartDate = DateTimeStartValue, EndDate = DateTimeEndValue });
            StateHasChanged();
        }

        private string ButtonClasses
        {
            get => $"rz-button-icon-left rzi rzi-calendar";
        }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="RadzenDatePicker{TValue}"/> is inline - only Calender.
        /// </summary>
        /// <value><c>true</c> if inline; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool Inline { get; set; }

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
        public EventCallback<RadzenDateRange> Change { get; set; }

        /// <summary>
        /// Gets or sets the value changed callback.
        /// </summary>
        /// <value>The value changed callback.</value>
        [Parameter]
        public EventCallback<RadzenDateRange> ValueChanged { get; set; }

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
            if (PopupRenderMode == PopupRenderMode.OnDemand && !Disabled && !ReadOnly && !Inline)
            {
                InvokeAsync(() => popup.CloseAsync(Element));
            }

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
                    return $"width: 680px !important; {contentStyle}";
                }
            }
        }

        async Task OnChange()
        {
            await ValueChanged.InvokeAsync(new RadzenDateRange() { StartDate = DateTimeStartValue, EndDate = DateTimeEndValue });

            if (FieldIdentifier.FieldName != null) { EditContext?.NotifyFieldChanged(FieldIdentifier); }
            await Change.InvokeAsync(new RadzenDateRange() { StartDate = DateTimeStartValue, EndDate = DateTimeEndValue });

        }

        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            return ClassList.Create()
                            .Add("rz-calendar-inline", Inline)
                            .Add(FieldIdentifier, EditContext)
                            .ToString();
        }

        private async Task SetStartDay(DateTime newValue)
        {
            var v = new DateTime(newValue.Year, newValue.Month, newValue.Day);
            if (v != DateTimeStartValue)
            {
                DateTimeStartValue = v;
                await OnChange();
                //Close();
            }
        }

        private async Task SetEndDay(DateTime newValue)
        {
            var v = new DateTime(newValue.Year, newValue.Month, newValue.Day);
            if (v != DateTimeEndValue)
            {
                DateTimeEndValue = v;
                await OnChange();
                Close();
            }
        }

        private void SetStartDateMonth(int month)
        {
            var currentValue = StartDate;
            var newValue = new DateTime(currentValue.Year, month, Math.Min(currentValue.Day, DateTime.DaysInMonth(currentValue.Year, month)));

            StartDate = newValue;
        }

        private void SetStartDateYear(int year)
        {
            var currentValue = StartDate;
            var newValue = new DateTime(year, currentValue.Month, Math.Min(currentValue.Day, DateTime.DaysInMonth(year, currentValue.Month)));

            StartDate = newValue;
        }

        private void SetEndDateMonth(int month)
        {
            var currentValue = EndDate;
            var newValue = new DateTime(currentValue.Year, month, Math.Min(currentValue.Day, DateTime.DaysInMonth(currentValue.Year, month)));

            EndDate = newValue;
        }

        private void SetEndDateYear(int year)
        {
            var currentValue = EndDate;
            var newValue = new DateTime(year, currentValue.Month, Math.Min(currentValue.Day, DateTime.DaysInMonth(year, currentValue.Month)));

            EndDate = newValue;
        }

        private string getOpenPopup()
        {
            return PopupRenderMode == PopupRenderMode.Initial && !Disabled && !ReadOnly && !Inline ? $"Radzen.togglePopup(this.parentNode, '{PopupID}')" : "";
        }

        private string getOpenPopupForInput()
        {
            return PopupRenderMode == PopupRenderMode.Initial && !Disabled && !ReadOnly && !Inline && (!AllowInput || !ShowButton) ? $"Radzen.togglePopup(this.parentNode, '{PopupID}')" : "";
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
        public Expression<Func<RadzenDateRange>> ValueExpression { get; set; }

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

            await base.SetParametersAsync(parameters);

            if (shouldClose && !firstRender)
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
            return HasValue ? FormattedValue : null;
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
            }
        }

        string GetStartDayCssClass(DateTime date, DateRenderEventArgs dateArgs, bool forCell = true)
        {
            return ClassList.Create()
                               .Add("rz-state-default", !forCell)
                               .Add("rz-datepicker-other-month", StartDate.Month != date.Month)
                               .Add("rz-state-active", !forCell && DateTimeStartValue.HasValue && DateTimeStartValue.Value.Date.CompareTo(date.Date) == 0)
                               .Add("rz-datepicker-today", !forCell && DateTime.Now.Date.CompareTo(date.Date) == 0)
                               .Add("rz-state-disabled", !forCell && dateArgs.Disabled)
                               .ToString();
        }

        string GetEndDayCssClass(DateTime date, DateRenderEventArgs dateArgs, bool forCell = true)
        {
            return ClassList.Create()
                               .Add("rz-state-default", !forCell)
                               .Add("rz-datepicker-other-month", EndDate.Month != date.Month)
                               .Add("rz-state-active", !forCell && DateTimeEndValue.HasValue && DateTimeEndValue.Value.Date.CompareTo(date.Date) == 0)
                               .Add("rz-datepicker-today", !forCell && DateTime.Now.Date.CompareTo(date.Date) == 0)
                               .Add("rz-state-disabled", !forCell && dateArgs.Disabled)
                               .ToString();
        }

        /// <summary>
        /// Parses the date.
        /// </summary>
        protected async Task ParseDate()
        {
            await JSRuntime.InvokeAsync<string>("Radzen.setInputValue", input, FormattedValue);

            var value = new RadzenDateRange() { StartDate = DateTimeStartValue, EndDate = DateTimeEndValue };

            await ValueChanged.InvokeAsync(value);

            if (FieldIdentifier.FieldName != null)
            {
                EditContext?.NotifyFieldChanged(FieldIdentifier);
            }

            await Change.InvokeAsync(value);
            StateHasChanged();
        }

#if NET5_0_OR_GREATER
        /// <inheritdoc/>
        public async ValueTask FocusAsync()
        {
            await input.FocusAsync();
        }
#endif
    }

    public class RadzenDateRange
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}