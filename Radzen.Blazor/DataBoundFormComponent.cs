using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Radzen.Blazor;
using Radzen.Blazor.Rendering;
using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Radzen
{
    /// <summary>
    /// Class DataBoundFormComponent.
    /// Implements the <see cref="Radzen.RadzenComponent" />
    /// Implements the <see cref="Radzen.IRadzenFormComponent" />
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <seealso cref="Radzen.RadzenComponent" />
    /// <seealso cref="Radzen.IRadzenFormComponent" />
    [UnconditionalSuppressMessage(TrimMessages.Trimming, TrimMessages.IL2026, Justification = TrimMessages.DataTypePreserved)]
    [UnconditionalSuppressMessage(TrimMessages.Trimming, TrimMessages.IL2067, Justification = TrimMessages.DataTypePreserved)]
    [UnconditionalSuppressMessage(TrimMessages.Trimming, TrimMessages.IL2070, Justification = TrimMessages.DataTypePreserved)]
    [UnconditionalSuppressMessage(TrimMessages.Trimming, TrimMessages.IL2072, Justification = TrimMessages.DataTypePreserved)]
    public class DataBoundFormComponent<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicFields)] T> : RadzenComponent, IRadzenFormComponent
    {
        /// <summary>
        /// Gets or sets the index of the tab.
        /// </summary>
        /// <value>The index of the tab.</value>
        [Parameter]
        public int TabIndex { get; set; } = 0;

        /// <summary>
        /// Gets or sets the filter case sensitivity.
        /// </summary>
        /// <value>The filter case sensitivity.</value>
        [Parameter]
        public FilterCaseSensitivity FilterCaseSensitivity { get; set; } = FilterCaseSensitivity.Default;

        /// <summary>
        /// Gets or sets the filter operator.
        /// </summary>
        /// <value>The filter operator.</value>
        [Parameter]
        public StringFilterOperator FilterOperator { get; set; } = StringFilterOperator.Contains;

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        [Parameter]
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the placeholder.
        /// </summary>
        /// <value>The placeholder.</value>
        [Parameter]
        public string? Placeholder { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="DataBoundFormComponent{T}"/> is disabled.
        /// </summary>
        /// <value><c>true</c> if disabled; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool Disabled { get; set; }

        /// <summary>
        /// Gets or sets the change.
        /// </summary>
        /// <value>The change.</value>
        [Parameter]
        public EventCallback<object> Change { get; set; }

        /// <summary>
        /// Gets or sets the load data.
        /// </summary>
        /// <value>The load data.</value>
        [Parameter]
        public EventCallback<Radzen.LoadDataArgs> LoadData { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance loading indicator is shown.
        /// </summary>
        /// <value><c>true</c> if this instance loading indicator is shown; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool IsLoading { get; set; }

        /// <summary>
        /// Gets or sets the loading template shown while <see cref="IsLoading"/> is <c>true</c>.
        /// </summary>
        /// <value>The loading template.</value>
        [Parameter]
        public RenderFragment? LoadingTemplate { get; set; }

        /// <summary>
        /// The form
        /// </summary>
        IRadzenForm? form;

        /// <summary>
        /// Gets or sets the form.
        /// </summary>
        /// <value>The form.</value>
        [CascadingParameter]
        public IRadzenForm? Form
        {
            get
            {
                return form;
            }
            set
            {
                form = value;
                form?.AddComponent(this);
            }
        }

        /// <summary>
        /// The value
        /// </summary>
        private T? _value;
        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        /// <value>The value.</value>
        [Parameter]
        public T? Value
        {
            get
            {
                return _value;
            }
            set
            {
                if (value == null || value.Equals("null"))
                {
                    _value = default;
                    return;
                }

                if (!value.Equals(_value))
                {
                    _value = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets the value changed.
        /// </summary>
        /// <value>The value changed.</value>
        [Parameter]
        public EventCallback<T> ValueChanged { get; set; }

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
        public virtual bool HasValue
        {
            get
            {
                if (typeof(T) == typeof(string))
                {
                    return !string.IsNullOrEmpty($"{Value}");
                }
                return Value != null;
            }
        }

        /// <summary>
        /// Gets or sets the name of the property used as the display text of each item.
        /// Required when <see cref="Data"/> contains objects and no <c>Template</c> is set; otherwise the item's
        /// <see cref="object.ToString"/> result is displayed, which for plain classes is the type name.
        /// </summary>
        /// <value>The text property.</value>
        [Parameter]
        public string? TextProperty { get; set; }

        /// <summary>
        /// The data
        /// </summary>
        IEnumerable? _data;
        /// <summary>
        /// Gets or sets the data.
        /// </summary>
        /// <value>The data.</value>
        [Parameter]
        public virtual IEnumerable? Data
        {
            get
            {
                return _data;
            }
            set
            {
                if (_data != value)
                {
                    _view = null;
                    _value = default;
                    _data = value;
                    StateHasChanged();
                }
            }
        }

        /// <summary>
        /// Called when [data changed].
        /// </summary>
        protected virtual async Task OnDataChanged()
        {
            await Task.CompletedTask;
        }

        /// <summary>
        /// Called when the <see cref="SearchText"/> parameter is changed by the parent component.
        /// </summary>
        protected virtual Task OnSearchTextChanged()
        {
            _view = null;
            return Task.CompletedTask;
        }

        bool parametersSet;

        /// <summary>
        /// Gets the query.
        /// </summary>
        /// <value>The query.</value>
        protected virtual IQueryable? Query
        {
            get
            {
                return Data != null ? Data.AsQueryable() : null;
            }
        }

        /// <summary>
        /// Gets or sets the search text
        /// </summary>
        [Parameter]
        public string? SearchText
        {
            get
            {
                return searchText;
            }
            set
            {
                if (searchText != value)
                {
                    searchText = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets the search text changed.
        /// </summary>
        /// <value>The search text changed.</value>
        [Parameter]
        public EventCallback<string> SearchTextChanged { get; set; }

        /// <summary>
        /// The search text
        /// </summary>
        internal string? searchText;

        /// <summary>
        /// The view
        /// </summary>
        protected IQueryable? _view;
        /// <summary>
        /// Gets the view.
        /// </summary>
        /// <value>The view.</value>
        protected virtual IEnumerable? View
        {
            get
            {
                var query = Query;
                if (_view == null && query != null)
                {
                    if (!string.IsNullOrEmpty(searchText) && !string.IsNullOrEmpty(TextProperty))
                    {
                        _view = query.Where(TextProperty, searchText, FilterOperator, FilterCaseSensitivity);
                    }
                    else
                    {
                        _view = Data is IQueryable ? query.Cast<object>().ToList().AsQueryable() : query;
                    }
                }

                return _view;
            }
        }

        internal IEnumerable? GetView() => View;

        /// <summary>
        /// Gets or sets the edit context.
        /// </summary>
        /// <value>The edit context.</value>
        [CascadingParameter]
        public EditContext? EditContext { get; set; }

        EditContext? previousEditContext;

        /// <summary>
        /// Gets the field identifier.
        /// </summary>
        /// <value>The field identifier.</value>
        [Parameter]
        public FieldIdentifier FieldIdentifier { get; set; }

        /// <summary>
        /// Gets or sets the value expression.
        /// </summary>
        /// <value>The value expression.</value>
        [Parameter]
        public Expression<Func<T>>? ValueExpression { get; set; }
        /// <summary>
        /// Set parameters as an asynchronous operation.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        public override async Task SetParametersAsync(ParameterView parameters)
        {
            // check for changes before setting the properties through the base call
            var dataChanged = parameters.DidParameterChange(nameof(Data), Data);
            var disabledChanged = parameters.DidParameterChange(nameof(Disabled), Disabled);
            var searchTextChanged = parametersSet && parameters.DidParameterChange(nameof(SearchText), SearchText);

            // allow the base class to process parameters and set the properties
            // after this call the parameters object should be considered stale
            await base.SetParametersAsync(parameters);

            parametersSet = true;

            // handle changes
            if (dataChanged)
            {
                await OnDataChanged();
            }

            if (searchTextChanged)
            {
                await OnSearchTextChanged();
            }

            if (EditContext != null && (ValueExpression != null || ValueChanged.HasDelegate) &&
                (FieldIdentifier.Model != EditContext.Model || EditContext != previousEditContext))
            {
                if (previousEditContext != null && previousEditContext != EditContext)
                {
                    previousEditContext.OnValidationStateChanged -= ValidationStateChanged;
                }

                FieldIdentifier = ValueExpression != null
                    ? FieldIdentifier.Create(ValueExpression)
                    : FieldIdentifier.Create(() => Value);
                EditContext.OnValidationStateChanged -= ValidationStateChanged;
                EditContext.OnValidationStateChanged += ValidationStateChanged;
                previousEditContext = EditContext;
            }

            if (disabledChanged)
            {
                FormFieldContext?.DisabledChanged(Disabled);
            }
        }

        /// <inheritdoc />
        protected override void OnParametersSet()
        {
            base.OnParametersSet();

            FormFieldContext?.SetLabelTarget(this, Visible ? LabelTargetId : null);
        }

        /// <summary>
        /// Validations the state changed.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="ValidationStateChangedEventArgs"/> instance containing the event data.</param>
        private void ValidationStateChanged(object? sender, ValidationStateChangedEventArgs e)
        {
            StateHasChanged();
        }

        /// <summary>
        /// Disposes this instance.
        /// </summary>
        public override void Dispose()
        {
            base.Dispose();

            if (previousEditContext != null)
            {
                previousEditContext.OnValidationStateChanged -= ValidationStateChanged;
            }

            if (EditContext != null && EditContext != previousEditContext)
            {
                EditContext.OnValidationStateChanged -= ValidationStateChanged;
            }

            Form?.RemoveComponent(this);

            FormFieldContext?.SetLabelTarget(this, null);

            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <returns>System.Object.</returns>
        public virtual object? GetValue()
        {
            return Value;
        }

        /// <summary>
        /// Notifies the <see cref="EditContext"/> that the field has changed while validators observe the specified value.
        /// The stored value is restored afterwards so that two-way binding parameter re-flow keeps working as expected.
        /// </summary>
        /// <param name="value">The value validators should observe during the notification.</param>
        protected void NotifyFieldChanged(T? value)
        {
            if (FieldIdentifier.FieldName == null || EditContext == null)
            {
                return;
            }

            var current = _value;
            _value = value;

            try
            {
                EditContext.NotifyFieldChanged(FieldIdentifier);
            }
            finally
            {
                _value = current;
            }
        }


        /// <summary>
        /// Gets the class list.
        /// </summary>
        /// <param name="className">Name of the class.</param>
        /// <returns>ClassList.</returns>
        protected ClassList GetClassList(string className) => ClassList.Create(className)
                                                                       .AddDisabled(Disabled)
                                                                       .Add(FieldIdentifier, EditContext)
                                                                       .Add("rz-state-empty", !HasValue);

        /// <inheritdoc/>
        public virtual async ValueTask FocusAsync()
        {
            await Element.FocusAsync();
        }

        /// <summary> Provides support for RadzenFormField integration. </summary>
        [CascadingParameter]
        public IFormFieldContext? FormFieldContext { get; set; }

        /// <summary> Gets the current placeholder. Returns empty string if this component is inside a RadzenFormField.</summary>
        protected string? CurrentPlaceholder => FormFieldContext?.AllowFloatingLabel == true ? " " : Placeholder;

        /// <summary>
        /// Gets the id of the element a <see cref="RadzenFormField"/> label points to with its <c>for</c> attribute.
        /// Defaults to the component id; components that render their input inside a wrapper element return the input id instead.
        /// </summary>
        protected virtual string? LabelTargetId => GetId();

        /// <summary>
        /// Gets the id of the enclosing <see cref="RadzenFormField"/> label for use as <c>aria-labelledby</c> on elements that cannot be the target of a label,
        /// or <c>null</c> when the component is not the one the form field label points to or the consumer supplied <c>aria-label</c> or <c>aria-labelledby</c> via <see cref="RadzenComponent.Attributes"/>.
        /// </summary>
        protected string? FormFieldLabelledBy => Attributes != null && (Attributes.ContainsKey("aria-label") || Attributes.ContainsKey("aria-labelledby")) ? null : FormFieldContext?.GetLabelId(this);

        private string? linkedLabelName;
        private bool linkedLabelVisible;

        /// <summary>
        /// Gets a value indicating whether a label whose <c>for</c> attribute targets <see cref="Name"/> should name the component on the client:
        /// the component has a name, is visible, is not named by a <see cref="RadzenFormField"/> and the consumer did not supply <c>aria-label</c> or <c>aria-labelledby</c>.
        /// Components with an aria label parameter of their own override it to require that the parameter is not set.
        /// </summary>
        protected virtual bool ShouldLinkLabel => !string.IsNullOrEmpty(Name) && Visible && FormFieldContext?.GetLabelId(this) == null
            && (Attributes == null || !(Attributes.ContainsKey("aria-label") || Attributes.ContainsKey("aria-labelledby")));

        /// <summary>
        /// Asks the client to point <c>aria-labelledby</c> of <paramref name="element"/> at the label whose <c>for</c> attribute targets <see cref="Name"/>.
        /// Call it from <c>OnAfterRenderAsync</c>; it talks to the client only when <see cref="Name"/> or <see cref="RadzenComponent.Visible"/> changed since the last call.
        /// Use it for components whose focusable element is not labelable, so a <see cref="RadzenLabel"/> or a plain label associated through the name names it anyway.
        /// </summary>
        /// <param name="element">The element that carries the accessible name.</param>
        protected async Task LinkLabelAsync(ElementReference element)
        {
            if (Name == linkedLabelName && Visible == linkedLabelVisible)
            {
                return;
            }

            if (LabelToLink() == null || !IsJSRuntimeAvailable || JSRuntime == null)
            {
                return;
            }

            try
            {
                await JSRuntime.InvokeVoidAsync("Radzen.linkLabel", element, Name);
            }
            catch (JSDisconnectedException)
            {
            }
        }

        /// <summary>
        /// Returns the name whose label the client should link to the component, or <c>null</c> when no label should be linked, and records the link as done
        /// so that a later <see cref="LinkLabelAsync"/> talks to the client only when <see cref="Name"/> or <see cref="RadzenComponent.Visible"/> change again.
        /// Components that already create client-side state after render pass the result to that call instead of making a separate one.
        /// </summary>
        protected string? LabelToLink()
        {
            linkedLabelName = Name;
            linkedLabelVisible = Visible;

            return ShouldLinkLabel ? Name : null;
        }

        /// <summary>
        /// Handles the ContextMenu event.
        /// </summary>
        /// <param name="args">The <see cref="MouseEventArgs"/> instance containing the event data.</param>
        /// <returns>Task.</returns>
        public override Task OnContextMenu(MouseEventArgs args)
        {
            if (!Disabled)
            {
                return base.OnContextMenu(args);
            }

            return Task.CompletedTask;
        }
    }
}
