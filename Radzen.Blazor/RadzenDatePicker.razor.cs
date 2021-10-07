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

                if (!object.Equals(newValue, Value))
                {
                    Value = newValue;
                    await OnChange();
                }
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

                if (!object.Equals(newValue, Value))
                {
                    Value = newValue;
                    await OnChange();
                }
            }
        }

        int? hour;
        void OnUpdateHourInput(ChangeEventArgs args)
        {
            var value = $"{args.Value}";
            if (!string.IsNullOrWhiteSpace(value))
            {
                hour = (int)Convert.ChangeType(value, typeof(int));
            }
        }

        int? minutes;
        void OnUpdateHourMinutes(ChangeEventArgs args)
        {
            var value = $"{args.Value}";
            if (!string.IsNullOrWhiteSpace(value))
            {
                minutes = (int)Convert.ChangeType(value, typeof(int));
            }
        }

        int? seconds;
        void OnUpdateHourSeconds(ChangeEventArgs args)
        {
            var value = $"{args.Value}";
            if (!string.IsNullOrWhiteSpace(value))
            {
                seconds = (int)Convert.ChangeType(value, typeof(int));
            }
        }

        async Task UpdateHour(int v)
        {
            var newHour = HourFormat == "12" && CurrentDate.Hour > 12 ? v + 12 : v;

            var newValue = new DateTime(CurrentDate.Year, CurrentDate.Month, CurrentDate.Day, newHour > 23 || newHour < 0 ? 0 : newHour, CurrentDate.Minute, CurrentDate.Second);

            if (!object.Equals(newValue, Value))
            {
                hour = newValue.Hour;
                Value = newValue;
                await OnChange();
            }
        }

        async Task UpdateMinutes(int v)
        {
            var newValue = new DateTime(CurrentDate.Year, CurrentDate.Month, CurrentDate.Day, CurrentDate.Hour, v, CurrentDate.Second);

            if (!object.Equals(newValue, Value))
            {
                minutes = newValue.Minute;
                Value = newValue;
                await OnChange();
            }
        }

        async Task UpdateSeconds(int v)
        {
            var newValue = new DateTime(CurrentDate.Year, CurrentDate.Month, CurrentDate.Day, CurrentDate.Hour, CurrentDate.Minute, v);

            if (!object.Equals(newValue, Value))
            {
                seconds = newValue.Second;
                Value = newValue;
                await OnChange();
            }
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
            public string Name { get; set; }
            public int Value { get; set; }
        }

        IList<NameValue> months;
        IList<NameValue> years;

        protected override void OnInitialized()
        {
            base.OnInitialized();

            months = Enumerable.Range(1, 12).Select(i => new NameValue() { Name = Culture.DateTimeFormat.GetMonthName(i), Value = i }).ToList();
            years = Enumerable.Range(int.Parse(YearRange.Split(':').First()), int.Parse(YearRange.Split(':').Last()) - int.Parse(YearRange.Split(':').First()) + 1)
                .Select(i => new NameValue() { Name = $"{i}", Value = i }).ToList();
        }

        [Parameter]
        public bool AllowClear { get; set; }

        [Parameter]
        public int TabIndex { get; set; } = 0;

        string amPm = "am";

        [Parameter]
        public string Name { get; set; }

        [Parameter]
        public string InputClass { get; set; }

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

        [Parameter]
        public Action<DateRenderEventArgs> DateRender { get; set; }

        DateRenderEventArgs DateAttributes(DateTime value)
        {
            var args = new Radzen.DateRenderEventArgs() { Date = value, Disabled = false };

            if (DateRender != null)
            {
                DateRender(args);
            }

            return args;
        }

        object _value;

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

        private DateTime CurrentDate
        {
            get
            {
                return HasValue && DateTimeValue.Value != default(DateTime) ? DateTimeValue.Value : DateTime.Today;
            }
        }

        private DateTime StartDate
        {
            get
            {
                var firstDayOfTheMonth = new DateTime(CurrentDate.Year, CurrentDate.Month, 1);

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

        public bool IsBound
        {
            get
            {
                return ValueChanged.HasDelegate;
            }
        }

        public bool HasValue
        {
            get
            {
                return DateTimeValue.HasValue;
            }
        }

        public string FormattedValue
        {
            get
            {
                return string.Format("{0:" + DateFormat + "}", Value);
            }
        }

        IRadzenForm _form;

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

        protected ElementReference input;

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

            if (valid)
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

        [Parameter]
        public bool Inline { get; set; }

        [Parameter]
        public bool TimeOnly { get; set; }

        [Parameter]
        public bool ReadOnly { get; set; }

        [Parameter]
        public bool AllowInput { get; set; } = true;

        private bool IsReadonly => ReadOnly || !AllowInput;

        [Parameter]
        public bool Disabled { get; set; }

        [Parameter]
        public bool ShowTime { get; set; }

        [Parameter]
        public bool ShowSeconds { get; set; }

        [Parameter]
        public string HoursStep { get; set; }

        [Parameter]
        public string MinutesStep { get; set; }

        [Parameter]
        public string SecondsStep { get; set; }

        enum StepType
        {
            Hours,
            Minutes,
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

        [Parameter]
        public bool ShowTimeOkButton { get; set; } = true;

        [Parameter]
        public string DateFormat { get; set; }

        [Parameter]
        public string YearRange { get; set; } = "1950:2050";
        /*
        [Parameter]
        public string SelectionMode { get; set; } = "single";
        */
        [Parameter]
        public string HourFormat { get; set; } = "24";

        /*
        [Parameter]
        public bool Utc { get; set; } = true;
        */

        [Parameter]
        public string Placeholder { get; set; }

        [Parameter]
        public EventCallback<DateTime?> Change { get; set; }

        [Parameter]
        public EventCallback<TValue> ValueChanged { get; set; }

        string contentStyle = "display:none;";

        private string getStyle()
        {
            return $"display: inline-block;{(Inline ? "overflow:auto;" : "")}{(Style != null ? Style : "")}";
        }

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

        protected override string GetComponentCssClass()
        {
            return ClassList.Create()
                            .Add("rz-calendar-inline", Inline)
                            .Add(FieldIdentifier, EditContext)
                            .ToString();
        }

        private async System.Threading.Tasks.Task SetDay(DateTime newValue)
        {
            var currentValue = HasValue ? DateTimeValue.Value : CurrentDate;

            if (newValue != DateTimeValue)
            {
                DateTimeValue = newValue;
                await OnChange();
                Close();
            }
        }

        private async System.Threading.Tasks.Task SetMonth(int month)
        {
            var currentValue = HasValue ? DateTimeValue.Value : CurrentDate;
            var newValue = new DateTime(currentValue.Year, month, Math.Min(currentValue.Day, DateTime.DaysInMonth(currentValue.Year, month)), currentValue.Hour, currentValue.Minute, currentValue.Second);

            if (newValue != DateTimeValue)
            {
                DateTimeValue = newValue;
                await OnChange();
                Close();
            }
        }

        private async System.Threading.Tasks.Task SetYear(int year)
        {
            var currentValue = HasValue ? DateTimeValue.Value : CurrentDate;
            var newValue = new DateTime(year, currentValue.Month, Math.Min(currentValue.Day, DateTime.DaysInMonth(year, currentValue.Month)), currentValue.Hour, currentValue.Minute, currentValue.Second);

            if (newValue != DateTimeValue)
            {
                DateTimeValue = newValue;
                await OnChange();
                Close();
            }
        }

        private string getOpenPopup()
        {
            return !Disabled && !ReadOnly && !Inline ? $"Radzen.togglePopup(this.parentNode, '{PopupID}')" : "";
        }

        [CascadingParameter]
        public EditContext EditContext { get; set; }

        public FieldIdentifier FieldIdentifier { get; private set; }

        [Parameter]
        public Expression<Func<TValue>> ValueExpression { get; set; }

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

        protected override Task OnAfterRenderAsync(bool firstRender)
        {
            this.firstRender = firstRender;

            return base.OnAfterRenderAsync(firstRender);
        }
    }
}