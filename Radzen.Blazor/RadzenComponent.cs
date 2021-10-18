using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.AspNetCore.Components;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Radzen
{
    /// <summary>
    /// Class RadzenComponent.
    /// Implements the <see cref="ComponentBase" />
    /// Implements the <see cref="IDisposable" />
    /// </summary>
    /// <seealso cref="ComponentBase" />
    /// <seealso cref="IDisposable" />
    public class RadzenComponent : ComponentBase, IDisposable
    {
        /// <summary>
        /// Gets or sets the attributes.
        /// </summary>
        /// <value>The attributes.</value>
        [Parameter(CaptureUnmatchedValues = true)]
        public IReadOnlyDictionary<string, object> Attributes { get; set; }

        /// <summary>
        /// Gets the element.
        /// </summary>
        /// <value>The element.</value>
        public ElementReference Element { get; internal set; }

        /// <summary>
        /// Gets or sets the mouse enter.
        /// </summary>
        /// <value>The mouse enter.</value>
        [Parameter]
        public EventCallback<ElementReference> MouseEnter { get; set; }

        /// <summary>
        /// Gets or sets the mouse leave.
        /// </summary>
        /// <value>The mouse leave.</value>
        [Parameter]
        public EventCallback<ElementReference> MouseLeave { get; set; }

        /// <summary>
        /// Gets or sets the context menu.
        /// </summary>
        /// <value>The context menu.</value>
        [Parameter]
        public EventCallback<Microsoft.AspNetCore.Components.Web.MouseEventArgs> ContextMenu { get; set; }

        /// <summary>
        /// Gets or sets the culture.
        /// </summary>
        /// <value>The culture.</value>
        [Parameter]
        public CultureInfo Culture
        {
            get => culture ?? DefaultCulture ?? CultureInfo.CurrentCulture;
            set => culture = value;
        }

        /// <summary>
        /// Gets or sets the default culture.
        /// </summary>
        /// <value>The default culture.</value>
        [CascadingParameter(Name = nameof(DefaultCulture))]
        public CultureInfo DefaultCulture { get; set; }

        /// <summary>
        /// The culture
        /// </summary>
        private CultureInfo culture;

        /// <summary>
        /// Called when [mouse enter].
        /// </summary>
        public async Task OnMouseEnter()
        {
            await MouseEnter.InvokeAsync(Element);
        }

        /// <summary>
        /// Called when [mouse leave].
        /// </summary>
        public async Task OnMouseLeave()
        {
            await MouseLeave.InvokeAsync(Element);
        }

        /// <summary>
        /// Handles the <see cref="E:ContextMenu" /> event.
        /// </summary>
        /// <param name="args">The <see cref="Microsoft.AspNetCore.Components.Web.MouseEventArgs"/> instance containing the event data.</param>
        public virtual async Task OnContextMenu(Microsoft.AspNetCore.Components.Web.MouseEventArgs args)
        {
            await ContextMenu.InvokeAsync(args);
        }

        /// <summary>
        /// Gets or sets the style.
        /// </summary>
        /// <value>The style.</value>
        [Parameter]
        public virtual string Style { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="RadzenComponent"/> is visible.
        /// </summary>
        /// <value><c>true</c> if visible; otherwise, <c>false</c>.</value>
        [Parameter]
        public virtual bool Visible { get; set; } = true;

        /// <summary>
        /// Gets the CSS class.
        /// </summary>
        /// <returns>System.String.</returns>
        protected string GetCssClass()
        {
            if (Attributes != null && Attributes.TryGetValue("class", out var @class) && !string.IsNullOrEmpty(Convert.ToString(@class)))
            {
                return $"{GetComponentCssClass()} {@class}";
            }

            return GetComponentCssClass();
        }

        /// <summary>
        /// Gets the identifier.
        /// </summary>
        /// <returns>System.String.</returns>
        protected string GetId()
        {
            if (Attributes != null && Attributes.TryGetValue("id", out var id) && !string.IsNullOrEmpty(Convert.ToString(@id)))
            {
                return $"{@id}";
            }

            return UniqueID;
        }

        /// <summary>
        /// Gets the component CSS class.
        /// </summary>
        /// <returns>System.String.</returns>
        protected virtual string GetComponentCssClass()
        {
            return "";
        }

        /// <summary>
        /// The debouncer
        /// </summary>
        Debouncer debouncer = new Debouncer();

        /// <summary>
        /// Debounces the specified action.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <param name="milliseconds">The milliseconds.</param>
        protected void Debounce(Func<Task> action, int milliseconds = 500)
        {
            debouncer.Debounce(milliseconds, action);
        }

        /// <summary>
        /// Gets or sets the unique identifier.
        /// </summary>
        /// <value>The unique identifier.</value>
        public string UniqueID { get; set; }

        /// <summary>
        /// Gets or sets the js runtime.
        /// </summary>
        /// <value>The js runtime.</value>
        [Inject]
        protected IJSRuntime JSRuntime { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is js runtime available.
        /// </summary>
        /// <value><c>true</c> if this instance is js runtime available; otherwise, <c>false</c>.</value>
        protected bool IsJSRuntimeAvailable { get; set; }

        /// <summary>
        /// Called when [initialized].
        /// </summary>
        protected override void OnInitialized()
        {
            UniqueID = Convert.ToBase64String(Guid.NewGuid().ToByteArray()).Replace("/", "-").Replace("+", "-").Substring(0, 10);            
        }

        /// <summary>
        /// The visible changed
        /// </summary>
        private bool visibleChanged = false;
        /// <summary>
        /// The first render
        /// </summary>
        private bool firstRender = true;

        /// <summary>
        /// Set parameters as an asynchronous operation.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        public override async Task SetParametersAsync(ParameterView parameters)
        {
            visibleChanged = parameters.DidParameterChange(nameof(Visible), Visible);

            await base.SetParametersAsync(parameters);

            if (visibleChanged && !firstRender)
            {
                if (Visible == false)
                {
                    Dispose();
                }
            }
        }

        /// <summary>
        /// The reference
        /// </summary>
        private DotNetObjectReference<RadzenComponent> reference;

        /// <summary>
        /// Gets the reference.
        /// </summary>
        /// <value>The reference.</value>
        protected DotNetObjectReference<RadzenComponent> Reference
        {
            get
            {
                if (reference == null)
                {
                    reference = DotNetObjectReference.Create(this);
                }

                return reference;
            }
        }

        /// <summary>
        /// On after render as an asynchronous operation.
        /// </summary>
        /// <param name="firstRender">if set to <c>true</c> [first render].</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            IsJSRuntimeAvailable = true;

            this.firstRender = firstRender;

            if (firstRender || visibleChanged)
            {
                visibleChanged = false;

                if (Visible)
                {
                    if (ContextMenu.HasDelegate)
                    {
                        await JSRuntime.InvokeVoidAsync("Radzen.addContextMenu", UniqueID, Reference);
                    }

                    if (MouseEnter.HasDelegate)
                    {
                        await JSRuntime.InvokeVoidAsync("Radzen.addMouseEnter", UniqueID, Reference);
                    }

                    if (MouseLeave.HasDelegate)
                    {
                        await JSRuntime.InvokeVoidAsync("Radzen.addMouseLeave", UniqueID, Reference);
                    }
                }
            }
        }

        /// <summary>
        /// Raises the context menu.
        /// </summary>
        /// <param name="e">The <see cref="Microsoft.AspNetCore.Components.Web.MouseEventArgs"/> instance containing the event data.</param>
        [JSInvokable("RadzenComponent.RaiseContextMenu")]
        public async System.Threading.Tasks.Task RaiseContextMenu(Microsoft.AspNetCore.Components.Web.MouseEventArgs e)
        {
            if (ContextMenu.HasDelegate)
            {
                await OnContextMenu(e);
            }
        }

        /// <summary>
        /// Raises the mouse enter.
        /// </summary>
        [JSInvokable("RadzenComponent.RaiseMouseEnter")]
        public async System.Threading.Tasks.Task RaiseMouseEnter()
        {
            if (MouseEnter.HasDelegate)
            {
                await OnMouseEnter();
            }
        }

        /// <summary>
        /// Raises the mouse leave.
        /// </summary>
        [JSInvokable("RadzenComponent.RaiseMouseLeave")]
        public async System.Threading.Tasks.Task RaiseMouseLeave()
        {
            if (MouseLeave.HasDelegate)
            {
                await OnMouseLeave();
            }
        }

        /// <summary>
        /// Disposes this instance.
        /// </summary>
        public virtual void Dispose()
        {
            reference?.Dispose();
            reference = null;

            if (IsJSRuntimeAvailable)
            {
                if (ContextMenu.HasDelegate)
                {
                    JSRuntime.InvokeVoidAsync("Radzen.removeContextMenu", UniqueID);
                }

                if (MouseEnter.HasDelegate)
                {
                    JSRuntime.InvokeVoidAsync("Radzen.removeMouseEnter", UniqueID);
                }

                if (MouseLeave.HasDelegate)
                {
                    JSRuntime.InvokeVoidAsync("Radzen.removeMouseLeave", UniqueID);
                }
            }
        }

        /// <summary>
        /// Gets the current style.
        /// </summary>
        /// <value>The current style.</value>
        protected IDictionary<string, string> CurrentStyle
        {
            get
            {
                var currentStyle = new Dictionary<string, string>();

                if (!String.IsNullOrEmpty(Style))
                {
                    foreach (var pair in Style.Split(';'))
                    {
                        var keyAndValue = pair.Split(':');
                        if (keyAndValue.Length == 2)
                        {
                            var key = keyAndValue[0].Trim();
                            var value = keyAndValue[1].Trim();

                            currentStyle[key] = value;
                        }
                    }

                }

                return currentStyle;
            }
        }
    }
}
