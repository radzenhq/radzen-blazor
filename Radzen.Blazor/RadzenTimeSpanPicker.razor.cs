using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Radzen.Blazor.Rendering;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenTimeSpanPicker component.
    /// </summary>
    /// <typeparam name="TValue"><see cref="TimeSpan"/> and nullable <see cref="TimeSpan"/> are supported.</typeparam>
    /// <example>
    /// <code>
    /// &lt;RadzenTimeSpanPicker @bind-Value="@someValue" TValue="TimeSpan" Change=@(args => Console.WriteLine($"Selected time span: {args}")) /&gt;
    /// </code>
    /// </example>
    public partial class RadzenTimeSpanPicker<TValue> : RadzenComponent, IRadzenFormComponent
    {
        #region Parameters: value
        private TValue valueField;
        /// <summary>
        /// Specifies the value of the component.
        /// </summary>
        [Parameter]
        public TValue Value
        {
            get => valueField;
            set
            {
                if (EqualityComparer<object>.Default.Equals(valueField, value))
                {
                    return;
                }

                valueField = value;

                if (valueField is null)
                {
                    ConfirmedValue = null;
                    return;
                }

                ConfirmedValue = (TimeSpan?)(object)valueField;
            }
        }
        /// <summary>
        /// Specifies the minimum time span allowed.
        /// </summary>
        [Parameter]
        public TimeSpan Min { get; set; } = TimeSpan.MinValue;

        /// <summary>
        /// Specifies the maximum time span allowed.
        /// </summary>
        [Parameter]
        public TimeSpan Max { get; set; } = TimeSpan.MaxValue;
        #endregion

        #region Parameters: input field config
        /// <summary>
        /// Specifies whether the value can be cleared.
        /// </summary>
        /// <value><c>true</c> if value can be cleared; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool AllowClear { get; set; }

        /// <summary>
        /// Specifies whether input in the input field is allowed.
        /// Set to <c>true</c> by default.
        /// </summary>
        /// <value><c>true</c> if input is allowed; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool AllowInput { get; set; } = true;

        /// <summary>
        /// Specifies whether the input field is disabled.
        /// </summary>
        /// <value><c>true</c> if disabled; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool Disabled { get; set; }

        /// <summary>
        /// Specifies whether the input field is read only.
        /// </summary>
        /// <value><c>true</c> if read only; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool ReadOnly { get; set; }

        /// <summary>
        /// Specifies whether to display popup icon button in the input field.
        /// </summary>
        /// <value><c>true</c> to display the button to open the popup;
        /// <c>false</c> to hide the button to open the popup, clicking the input field opens the popup instead.</value>
        [Parameter]
        public bool ShowPopupButton { get; set; } = true;

        /// <summary>
        /// Specifies the popup toggle button CSS classes, separated with spaces.
        /// </summary>
        [Parameter]
        public string PopupButtonClass { get; set; }

        /// <summary>
        /// Specifies additional custom attributes that will be rendered by the input.
        /// </summary>
        [Parameter]
        public IReadOnlyDictionary<string, object> InputAttributes { get; set; }

        /// <summary>
        /// Specifies the input CSS classes, separated with spaces.
        /// </summary>
        [Parameter]
        public string InputClass { get; set; }

        /// <summary>
        /// Specifies the name of the input field.
        /// </summary>
        [Parameter]
        public string Name { get; set; }

        /// <summary>
        /// Specifies the tab index.
        /// </summary>
        [Parameter]
        public int TabIndex { get; set; } = 0;

        /// <summary>
        /// Specifies the time span format in the input field.
        /// For more details, see the documentation of
        /// <see href="https://learn.microsoft.com/en-us/dotnet/standard/base-types/standard-timespan-format-strings">standard</see>
        /// and <see href="https://learn.microsoft.com/en-us/dotnet/standard/base-types/custom-timespan-format-strings">custom</see>
        /// time span format strings.
        /// </summary>
        [Parameter]
        public string TimeSpanFormat { get; set; }

        /// <summary>
        /// Specifies custom function to parse the input.
        /// If it's not defined or the function it returns <c>null</c>, a built-in parser us used instead.
        /// </summary>
        [Parameter]
        public Func<string, TimeSpan?> ParseInput { get; set; }
        #endregion

        #region Parameters: input field texts
        /// <summary>
        /// Specifies the input placeholder.
        /// </summary>
        [Parameter]
        public string Placeholder { get; set; }

        /// <summary>
        /// Specifies the aria label for the toggle popup button.
        /// </summary>
        [Parameter]
        public string TogglePopupAriaLabel { get; set; } = "Toggle popup";
        #endregion

        #region Parameters: panel config
        /// <summary>
        /// Specifies the render mode of the popup.
        /// </summary>
        [Parameter]
        public PopupRenderMode PopupRenderMode { get; set; } = PopupRenderMode.Initial;

        /// <summary>
        /// Specifies whether the component is inline or shows a popup.
        /// </summary>
        /// <value><c>true</c> if inline; <c>false</c> if shows a popup.</value>
        [Parameter]
        public bool Inline { get; set; }

        /// <summary>
        /// Specifies whether to display the confirmation button in the panel to accept changes.
        /// </summary>
        /// <value><c>true</c> if the confirmation button is shown; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool ShowConfirmationButton { get; set; } = false;

        /// <summary>
        /// Specifies whether the time fields in the panel, except for the days field, are padded with leading zeros.
        /// </summary>
        /// <value><c>true</c> if fields are padded; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool PadTimeValues { get; set; }

        /// <summary>
        /// Specifies the most precise time unit field in the picker panel. Set to <see cref="TimeSpanUnit.Second"/> by default.
        /// </summary>
        [Parameter]
        public TimeSpanUnit FieldPrecision { get; set; } = TimeSpanUnit.Second;

        /// <summary>
        /// Specifies the step of the days field in the picker panel.
        /// </summary>
        [Parameter]
        public string DaysStep { get; set; }

        /// <summary>
        /// Specifies the step of the hours field in the picker panel.
        /// </summary>
        [Parameter]
        public string HoursStep { get; set; }

        /// <summary>
        /// Specifies the step of the minutes field in the picker panel.
        /// </summary>
        [Parameter]
        public string MinutesStep { get; set; }

        /// <summary>
        /// Specifies the step of the seconds field in the picker panel.
        /// </summary>
        [Parameter]
        public string SecondsStep { get; set; }

        /// <summary>
        /// Specifies the step of the milliseconds field in the picker panel.
        /// </summary>
        [Parameter]
        public string MillisecondsStep { get; set; }

        #if NET7_0_OR_GREATER
        /// <summary>
        /// Specifies the step of the microseconds field in the picker panel.
        /// </summary>
        [Parameter]
        public string MicrosecondsStep { get; set; }
#endif
        #endregion

        #region Parameters: panel texts
        /// <summary>
        /// Specifies the text of the confirmation button. Used only if <see cref="ShowConfirmationButton"/> is <code>true</code>.
        /// </summary>
        [Parameter]
        public string ConfirmationButtonText { get; set; } = "OK";

        /// <summary>
        /// Specifies the text of the positive value button.
        /// </summary>
        [Parameter]
        public string PositiveButtonText { get; set; } = "+";

        /// <summary>
        /// Specifies the text of the negative value button.
        /// </summary>
        [Parameter]
        public string NegativeButtonText { get; set; } = "−";

        /// <summary>
        /// Specifies the text displayed next to the fields in the panel when the value is positive and there's no sign picker.
        /// </summary>
        [Parameter]
        public string PositiveValueText { get; set; } = string.Empty;

        /// <summary>
        /// Specifies the text displayed next to the fields in the panel when the value is negative and there's no sign picker.
        /// </summary>
        [Parameter]
        public string NegativeValueText { get; set; } = "−";

        /// <summary>
        /// Specifies the days label text.
        /// </summary>
        [Parameter]
        public string DaysUnitText { get; set; } = "Days";

        /// <summary>
        /// Specifies the hours label text.
        /// </summary>
        [Parameter]
        public string HoursUnitText { get; set; } = "Hours";

        /// <summary>
        /// Specifies the minutes label text.
        /// </summary>
        [Parameter]
        public string MinutesUnitText { get; set; } = "Minutes";

        /// <summary>
        /// Specifies the seconds label text.
        /// </summary>
        [Parameter]
        public string SecondsUnitText { get; set; } = "Seconds";

        /// <summary>
        /// Specifies the milliseconds label text.
        /// </summary>
        [Parameter]
        public string MillisecondsUnitText { get; set; } = "Milliseconds";

#if NET7_0_OR_GREATER
        /// <summary>
        /// Specifies the microseconds label text.
        /// </summary>
        [Parameter]
        public string MicrosecondsUnitText { get; set; } = "Microseconds";
        #endif
        #endregion

        #region Parameters: other config
        /// <summary>
        /// Specifies the value expression used while creating the <see cref="FieldIdentifier"/>.
        /// </summary>
        [Parameter]
        public Expression<Func<TValue>> ValueExpression { get; set; }
        #endregion

        #region Parameters: events
        /// <summary>
        /// Specifies the callback of the value change.
        /// </summary>
        [Parameter]
        public EventCallback<TValue> ValueChanged { get; set; }

        /// <summary>
        /// Specifies the callback of the underlying nullable <see cref="TimeSpan"/> value.
        /// </summary>
        [Parameter]
        public EventCallback<TimeSpan?> Change { get; set; }
        #endregion


        #region Form fields
        private IRadzenForm form;
        /// <summary>
        /// Specifies the form this component belongs to.
        /// </summary>
        [CascadingParameter]
        public IRadzenForm Form
        {
            get => form;
            set
            {
                if (form == value || value is null)
                    return;

                form = value;
                form.AddComponent(this);
            }
        }

        /// <summary>
        /// Specifies the edit context of this component.
        /// </summary>
        [CascadingParameter]
        public EditContext EditContext { get; set; }

        /// <summary>
        /// Specifies the <see cref="RadzenFormField"/> context of this component.
        /// </summary>
        public IFormFieldContext FormFieldContext { get; set; } = null;
        #endregion


        #region Calculated properties and references
        private static readonly bool _isNullable = Nullable.GetUnderlyingType(typeof(TValue)) is not null;

        private bool PreventValueChange => Disabled || ReadOnly;
        private bool PreventPopupToggle => Disabled || ReadOnly || Inline;

        /// <summary>
        /// Indicates whether this instance has a confirmed value.
        /// </summary>
        /// <value><c>true</c> if this instance has value; otherwise, <c>false</c>.</value>
        public bool HasValue => ConfirmedValue.HasValue;

        /// <summary>
        /// Indicates whether this instance is bound <see cref="ValueChanged"/> callback has delegate).
        /// </summary>
        /// <value><c>true</c> if this instance is bound; otherwise, <c>false</c>.</value>
        public bool IsBound => ValueChanged.HasDelegate;

        /// <summary>
        /// Gets the formatted value.
        /// </summary>
        public string FormattedValue => HasValue ? string.Format(Culture, "{0:" + TimeSpanFormat + "}", ConfirmedValue) : "";

        /// <summary>
        /// Gets the field identifier.
        /// </summary>
        [Parameter]
        public FieldIdentifier FieldIdentifier { get; set; }

        /// <summary>
        /// Gets the input reference.
        /// </summary>
        protected ElementReference input;
        #endregion


        #region Underlying values
        private TimeSpan? _confirmedValue;
        private TimeSpan? ConfirmedValue
        {
            get => _confirmedValue;
            set
            {
                if (_confirmedValue == value)
                {
                    return;
                }

                TimeSpan? newValue = value.HasValue ? AdjustToBounds(value.Value) :
                    _isNullable ? null : DefaultNonNullValue;
                if (_confirmedValue == newValue)
                {
                    return;
                }
                _confirmedValue = newValue;

                Value = (TValue) (object)_confirmedValue;

                if (ShowConfirmationButton is false)
                {
                    UnconfirmedValue = ConfirmedValue ?? DefaultNonNullValue;
                }
            }
        }

        private TimeSpan unconfirmedValue;
        private TimeSpan UnconfirmedValue
        {
            get => unconfirmedValue;
            set
            {
                _lastFieldInput.Value = null;

                if (unconfirmedValue == value)
                {
                    return;
                }

                var newValue = AdjustToBounds(value);

                if (unconfirmedValue == newValue)
                {
                    return;
                }

                if (newValue != TimeSpan.Zero || canBeEitherPositiveOrNegative is false)
                {
                    isUnconfirmedValueNegative = newValue < TimeSpan.Zero;
                }

                unconfirmedValue = newValue;
            }
        }

        private bool isUnconfirmedValueNegative = false;
        private int UnconformedValueSign => isUnconfirmedValueNegative ? -1 : 1;

        private TimeSpan DefaultNonNullValue => AdjustToBounds(TimeSpan.Zero);

        private void ResetUnconfirmedValue()
        {
            UnconfirmedValue = ConfirmedValue ?? DefaultNonNullValue;
            isUnconfirmedValueNegative = UnconfirmedValue < TimeSpan.Zero;
        }
        private TimeSpan AdjustToBounds(TimeSpan value) => value < Min ? Min : value > Max ? Max : value;
        #endregion


        #region Methods: component general
        /// <inheritdoc />
        protected override void OnInitialized()
        {
            // initial synchronization: necessary when T is not nullable and Value is default(T)
            ConfirmedValue = (TimeSpan?)(object)Value;
            base.OnInitialized();
        }

        private bool firstRender;
        /// <inheritdoc />
        protected override Task OnAfterRenderAsync(bool firstRender)
        {
            this.firstRender = firstRender;
            return base.OnAfterRenderAsync(firstRender);
        }

        /// <inheritdoc />
        public override async Task SetParametersAsync(ParameterView parameters)
        {
            if (parameters.DidParameterChange(nameof(Min), Min) || parameters.DidParameterChange(nameof(Max), Max))
            {
                var min = parameters.GetValueOrDefault(nameof(Min), Min);
                var max = parameters.GetValueOrDefault(nameof(Max), Max);

                SetPanelFieldsSetup(min, max);
            }

            var shouldClose =
                parameters.DidParameterChange(nameof(Visible), Visible) && parameters.GetValueOrDefault<bool>(nameof(Visible)) is false
                || parameters.DidParameterChange(nameof(Inline), Inline) && parameters.GetValueOrDefault<bool>(nameof(Inline))
                || parameters.DidParameterChange(nameof(Disabled), Disabled) && parameters.GetValueOrDefault<bool>(nameof(Disabled))
                || parameters.DidParameterChange(nameof(ReadOnly), ReadOnly) && parameters.GetValueOrDefault<bool>(nameof(ReadOnly));

            await base.SetParametersAsync(parameters);

            if (shouldClose && !firstRender && IsJSRuntimeAvailable)
            {
                await ClosePopup();
            }

            if (EditContext != null && ValueExpression != null && FieldIdentifier.Model != EditContext.Model)
            {
                FieldIdentifier = FieldIdentifier.Create(ValueExpression);
                EditContext.OnValidationStateChanged -= ValidationStateChanged;
                EditContext.OnValidationStateChanged += ValidationStateChanged;
            }
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
        }

        private async Task OnChange()
        {
            await ValueChanged.InvokeAsync(Value);

            if (FieldIdentifier.FieldName != null)
            {
                EditContext?.NotifyFieldChanged(FieldIdentifier);
            }

            await Change.InvokeAsync(ConfirmedValue);
        }

        private void ValidationStateChanged(object sender, ValidationStateChangedEventArgs e)
        {
            StateHasChanged();
        }
        #endregion

        #region Methods: frontend interactions used externally
        /// <summary>
        /// Closes this instance popup.
        /// </summary>
        public async Task Close()
        {
            if (Inline)
            {
                return;
            }

            await ClosePopup();
        }

        /// <inheritdoc/>
        public async ValueTask FocusAsync()
        {
            try
            {
                await input.FocusAsync();
            }
            catch
            { }
        }
        #endregion

        #region Methods: other
        /// <summary>
        /// Gets the value of the component.
        /// </summary>
        /// <returns>System.Object.</returns>
        public object GetValue() => Value;
        #endregion


        #region Internal: input text handling
        private bool IsInputAllowed => ReadOnly || !AllowInput;

        private async Task Clear()
        {
            if (PreventValueChange)
            {
                return;
            }

            ConfirmedValue = null;
            await ValueChanged.InvokeAsync(Value);

            if (FieldIdentifier.FieldName != null)
            {
                EditContext?.NotifyFieldChanged(FieldIdentifier);
            }

            await Change.InvokeAsync(ConfirmedValue);
        }

        private async Task SetValueFromInput(string inputValue)
        {
            if (PreventValueChange)
            {
                return;
            }

            bool valid = TryParseInput(inputValue, out TimeSpan value);

            TimeSpan? newValue = valid ? AdjustToBounds(value) : null;

            if (ConfirmedValue != newValue && (newValue is not null || _isNullable))
            {
                ConfirmedValue = newValue;

                // sometimes onchange is triggered after the popup opens so it won't synchronize the value while opening
                UnconfirmedValue = ConfirmedValue ?? DefaultNonNullValue;

                await OnChange();
            }
            else
            {
                await JSRuntime.InvokeAsync<string>("Radzen.setInputValue", input, FormattedValue);
            }
        }

        private bool TryParseInput(string inputValue, out TimeSpan value)
        {
            value = TimeSpan.Zero;
            bool valid = false;

            if (ParseInput != null)
            {
                TimeSpan? custom = ParseInput.Invoke(inputValue);

                if (custom.HasValue)
                {
                    valid = true;
                    value = custom.Value;
                }
            }
            else
            {
                valid = TimeSpan.TryParseExact(inputValue, TimeSpanFormat, Culture, TimeSpanStyles.None, out value);

                if (!valid)
                {
                    valid = TimeSpan.TryParse(inputValue, Culture, out value);
                }
            }

            return valid;
        }
        #endregion

        #region Internal: input mouse and keyboard events
        private async Task ClickPopupButton()
        {
            if (PreventPopupToggle)
            {
                return;
            }

            await TogglePopup();
            await FocusAsync();
        }

        private Task ClickInputField()
            => ShowPopupButton ? Task.CompletedTask : ClickPopupButton();

        private bool preventKeyPress = false;
        private async Task PressKey(KeyboardEventArgs args)
        {
            if (PreventPopupToggle)
            {
                return;
            }

            var key = args.Code ?? args.Key;

            if (key == "Enter")
            {
                await TogglePopup();
            }
            else if (key == "Escape")
            {
                await ClosePopup();
                await FocusAsync();
            }
        }
        #endregion

        #region Internal: popup general actions
        private Popup popup;

        private Task TogglePopup()
            => Inline ? Task.CompletedTask : popup?.ToggleAsync(Element) ?? Task.CompletedTask;

        private Task ClosePopup()
            => Inline ? Task.CompletedTask : popup?.CloseAsync(Element) ?? Task.CompletedTask;

        private async Task PopupKeyDown(KeyboardEventArgs args)
        {
            var key = args.Code ?? args.Key;
            if (key == "Escape")
            {
                await ClosePopup();
                await FocusAsync();
            }
        }

        private void OnPopupOpen()
        {
            ResetUnconfirmedValue();
            preventKeyPress = true;
        }
        private void OnPopupClose()
        {
            ResetUnconfirmedValue();
            preventKeyPress = false;
        }
        #endregion

        #region Internal: panel fields setup
        private static TimeSpan GetTimeSpanFromUnit(TimeSpanUnit unit, int value)
            => unit switch
            {
                TimeSpanUnit.Day => TimeSpan.FromDays(value),
                TimeSpanUnit.Hour => TimeSpan.FromHours(value),
                TimeSpanUnit.Minute => TimeSpan.FromMinutes(value),
                TimeSpanUnit.Second => TimeSpan.FromSeconds(value),
                TimeSpanUnit.Millisecond => TimeSpan.FromMilliseconds(value),
                #if NET7_0_OR_GREATER
                TimeSpanUnit.Microsecond => TimeSpan.FromMicroseconds(value),
                #endif
                _ => TimeSpan.Zero,
            };
        private static int GetTimeSpanUnitValue(TimeSpanUnit unit, TimeSpan timeSpan)
            => unit switch
            {
                TimeSpanUnit.Day => timeSpan.Days,
                TimeSpanUnit.Hour => timeSpan.Hours,
                TimeSpanUnit.Minute => timeSpan.Minutes,
                TimeSpanUnit.Second => timeSpan.Seconds,
                TimeSpanUnit.Millisecond => timeSpan.Milliseconds,
                #if NET7_0_OR_GREATER
                TimeSpanUnit.Microsecond => timeSpan.Microseconds,
                #endif
                _ => 0,
            };
        private static readonly Dictionary<TimeSpanUnit, int> _timeUnitMaxAbsoluteValues = new()
            {
                { TimeSpanUnit.Day, TimeSpan.MaxValue.Days },
                { TimeSpanUnit.Hour, 23 },
                { TimeSpanUnit.Minute, 59 },
                { TimeSpanUnit.Second, 59 },
                { TimeSpanUnit.Millisecond, 999 }
                #if NET7_0_OR_GREATER
                , { TimeSpanUnit.Microsecond, 999 }
                #endif
            };
        private static readonly Dictionary<TimeSpanUnit, int> _timeUnitZeroValues = Enum
            .GetValues<TimeSpanUnit>()
            .ToDictionary(x => x, x => 0);

        private Dictionary<TimeSpanUnit, int> _negativeTimeFieldsMaxValues = new(_timeUnitMaxAbsoluteValues);
        private Dictionary<TimeSpanUnit, int> _positiveTimeFieldsMaxValues = new(_timeUnitMaxAbsoluteValues);
        private Dictionary<TimeSpanUnit, int> TimeFieldsMaxValues
            => isUnconfirmedValueNegative ? _negativeTimeFieldsMaxValues : _positiveTimeFieldsMaxValues;

        private bool canBeEitherPositiveOrNegative = true;

        private void SetPanelFieldsSetup(TimeSpan min, TimeSpan max)
        {
            var canBeNegative = min < TimeSpan.Zero;
            var canBePositive = max > TimeSpan.Zero;
            canBeEitherPositiveOrNegative = canBeNegative && canBePositive;

            _negativeTimeFieldsMaxValues = canBeNegative ? GetTimeUnitMaxValues(min) : new (_timeUnitZeroValues);
            _positiveTimeFieldsMaxValues = canBePositive ? GetTimeUnitMaxValues(max) : new(_timeUnitZeroValues);
        }

        private static Dictionary<TimeSpanUnit, int> GetTimeUnitMaxValues(TimeSpan boundary)
        {
            var timeUnitMaxValues = new Dictionary<TimeSpanUnit, int>(_timeUnitMaxAbsoluteValues);

            if (boundary.Days != 0)
            {
                timeUnitMaxValues[TimeSpanUnit.Day] = Math.Abs(boundary.Days);
                return timeUnitMaxValues;
            }
            timeUnitMaxValues[TimeSpanUnit.Day] = 0;

            if (boundary.Hours != 0)
            {
                timeUnitMaxValues[TimeSpanUnit.Hour] = Math.Abs(boundary.Hours);
                return timeUnitMaxValues;
            }
            timeUnitMaxValues[TimeSpanUnit.Hour] = 0;

            if (boundary.Minutes != 0)
            {
                timeUnitMaxValues[TimeSpanUnit.Minute] = Math.Abs(boundary.Minutes);
                return timeUnitMaxValues;
            }
            timeUnitMaxValues[TimeSpanUnit.Minute] = 0;

            if (boundary.Seconds != 0)
            {
                timeUnitMaxValues[TimeSpanUnit.Second] = Math.Abs(boundary.Seconds);
                return timeUnitMaxValues;
            }
            timeUnitMaxValues[TimeSpanUnit.Second] = 0;

            if (boundary.Milliseconds != 0)
            {
                timeUnitMaxValues[TimeSpanUnit.Millisecond] = Math.Abs(boundary.Milliseconds);
                return timeUnitMaxValues;
            }
            timeUnitMaxValues[TimeSpanUnit.Millisecond] = 0;

#if NET7_0_OR_GREATER
            if (boundary.Microseconds != 0)
            {
                timeUnitMaxValues[TimeSpanUnit.Microsecond] = Math.Abs(boundary.Microseconds);
                return timeUnitMaxValues;
            }
            timeUnitMaxValues[TimeSpanUnit.Microsecond] = 0;
#endif

            return timeUnitMaxValues;
        }
#endregion

        #region Internal: panel value changes
        private Task UpdateSign(bool isNegative)
        {
            if (isNegative && UnconfirmedValue < TimeSpan.Zero
                || !isNegative && UnconfirmedValue > TimeSpan.Zero)
            {
                return Task.CompletedTask;
            }

            if (UnconfirmedValue == TimeSpan.Zero)
            {
                isUnconfirmedValueNegative = isNegative;
                return Task.CompletedTask;
            }

            return UpdateValueFromPanelFields(UnconfirmedValue.Negate());
        }

        private (TimeSpanUnit Unit, string Value) _lastFieldInput = (TimeSpanUnit.Day, null);

        private void SetLastFieldInput(TimeSpanUnit unit, string value)
            => _lastFieldInput = (unit, value);

        private Task UpdateValueOfUnit(TimeSpanUnit unit, string stringValue)
        {
            if (string.IsNullOrEmpty(stringValue)
                || int.TryParse(stringValue, NumberStyles.Any, Culture, out int value) is false)
            {
                return Task.CompletedTask;
            }

            value = Math.Min(Math.Max(value, 0), TimeFieldsMaxValues[unit]);
            return UpdateValueOfUnit(unit, value);
        }

        private Task UpdateValueOfUnit(TimeSpanUnit unit, int value)
        {
            var newValue = UnconfirmedValue
                - GetTimeSpanFromUnit(unit, GetTimeSpanUnitValue(unit, UnconfirmedValue))
                + GetTimeSpanFromUnit(unit, value * UnconformedValueSign);

            return UpdateValueFromPanelFields(newValue);
        }

        private Task UpdateValueFromPanelFields(TimeSpan newValue)
        {
            if (PreventValueChange)
            {
                return Task.CompletedTask;
            }

            UnconfirmedValue = newValue;

            if (ShowConfirmationButton || UnconfirmedValue == ConfirmedValue)
            {
                return Task.CompletedTask;
            }

            ConfirmedValue = UnconfirmedValue;
            return OnChange();
        }

        private async Task ConfirmValue()
        {
            await UpdateValueOfUnit(_lastFieldInput.Unit, _lastFieldInput.Value);

            if (ConfirmedValue != UnconfirmedValue)
            {
                ConfirmedValue = UnconfirmedValue;
                await OnChange();
            }
            await ClosePopup();
            await FocusAsync();
        }
        #endregion

        #region Internal: styles
        /// <inheritdoc />
        protected override string GetComponentCssClass()
             => ClassList.Create("rz-timespanpicker")
                .Add("rz-timespanpicker-inline", Inline)
                .AddDisabled(Disabled)
                .Add("rz-state-empty", !HasValue)
                .Add(FieldIdentifier, EditContext)
                .ToString();

        private string GetInputClass()
            => ClassList.Create("rz-inputtext")
                .Add(InputClass)
                .Add("rz-readonly", ReadOnly && !Disabled)
                .ToString();

        private string GetTogglePopupButtonClass()
            => ClassList.Create("rz-timespanpicker-trigger rz-button rz-button-icon-only")
                .Add(PopupButtonClass)
                .Add("rz-state-disabled", Disabled)
                .ToString();
        #endregion
    }
}
