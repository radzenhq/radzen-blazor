using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.AspNetCore.Components;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Radzen
{
    /// <summary>
    ///   Base class of Radzen Blazor components.
    /// </summary>
    public class RadzenComponent : ComponentBase, IDisposable
    {
        /// <summary>
        /// Specifies additional custom attributes that will be rendered by the component.
        /// </summary>
        /// <value>The attributes.</value>
        [Parameter(CaptureUnmatchedValues = true)]
        public IReadOnlyDictionary<string, object> Attributes { get; set; }

        /// <summary>
        /// Gets a reference to the HTML element rendered by the component.
        /// </summary>
        public ElementReference Element { get; protected internal set; }

        /// <summary>
        /// A callback that will be invoked when the user hovers the component. Commonly used to display a tooltip via 
        /// <see cref="TooltipService.Open(ElementReference, string, TooltipOptions)" />.
        /// </summary>
        [Parameter]
        public EventCallback<ElementReference> MouseEnter { get; set; }

        /// <summary>
        /// A callback that will be invoked when the user moves the mouse out of the component. Commonly used to hide a tooltip via 
        /// <see cref="TooltipService.Close" />.
        /// </summary>
        [Parameter]
        public EventCallback<ElementReference> MouseLeave { get; set; }

        /// <summary>
        /// A callback that will be invoked when the user right-clicks the component. Commonly used to display a context menu via 
        /// <see cref="ContextMenuService.Open(Microsoft.AspNetCore.Components.Web.MouseEventArgs, IEnumerable{ContextMenuItem}, Action{MenuItemEventArgs})" />.
        /// </summary>
        [Parameter]
        public EventCallback<Microsoft.AspNetCore.Components.Web.MouseEventArgs> ContextMenu { get; set; }

        /// <summary>
        /// Gets or sets the culture used to display localizable data (numbers, dates). Set by default to <see cref="CultureInfo.CurrentCulture" />.
        /// </summary>
        [Parameter]
        public CultureInfo Culture
        {
            get => culture ?? DefaultCulture ?? CultureInfo.CurrentCulture;
            set => culture = value;
        }

        /// <summary>
        /// Gets or sets the culture set by a parent component.
        /// </summary>
        [CascadingParameter(Name = nameof(DefaultCulture))]
        public CultureInfo DefaultCulture { get; set; }

        private CultureInfo culture;

        /// <summary>
        /// Raises <see cref="MouseEnter" />.
        /// </summary>
        public async Task OnMouseEnter()
        {
            await MouseEnter.InvokeAsync(Element);
        }

        /// <summary>
        /// Raises <see cref="MouseLeave" />.
        /// </summary>
        public async Task OnMouseLeave()
        {
            await MouseLeave.InvokeAsync(Element);
        }

        /// <summary>
        /// Raises <see cref="ContextMenu" />.
        /// </summary>
        /// <param name="args">The <see cref="Microsoft.AspNetCore.Components.Web.MouseEventArgs"/> instance containing the event data.</param>
        public virtual async Task OnContextMenu(Microsoft.AspNetCore.Components.Web.MouseEventArgs args)
        {
            await ContextMenu.InvokeAsync(args);
        }

        /// <summary>
        /// Gets or sets the inline CSS style.
        /// </summary>
        /// <value>The style.</value>
        [Parameter]
        public virtual string Style { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="RadzenComponent"/> is visible. Invisible components are not rendered.
        /// </summary>
        /// <value><c>true</c> if visible; otherwise, <c>false</c>.</value>
        [Parameter]
        public virtual bool Visible { get; set; } = true;

        /// <summary>
        /// Gets the final CSS class rendered by the component. Combines it with a <c>class</c> custom attribute.
        /// </summary>
        protected string GetCssClass()
        {
            if (Attributes != null && Attributes.TryGetValue("class", out var @class) && !string.IsNullOrEmpty(Convert.ToString(@class)))
            {
                return $"{GetComponentCssClass()} {@class}";
            }

            return GetComponentCssClass();
        }

        /// <summary>
        /// Gets the unique identifier. 
        /// </summary>
        /// <returns>Returns the <c>id</c> attribute (if specified) or generates a random one.</returns>
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
        protected virtual string GetComponentCssClass()
        {
            return "";
        }

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
        /// Gets or sets a value indicating whether <see cref="JSRuntime" /> is available.
        /// </summary>
        protected bool IsJSRuntimeAvailable { get; set; }

        /// <summary>
        /// Called by the Blazor runtime.
        /// </summary>
        protected override void OnInitialized()
        {
            UniqueID = Convert.ToBase64String(Guid.NewGuid().ToByteArray()).Replace("/", "-").Replace("+", "-").Substring(0, 10);
        }

        private bool visibleChanged = false;
        private bool firstRender = true;

        /// <summary>
        /// Called by the Blazor runtime when parameters are set.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
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

        private DotNetObjectReference<RadzenComponent> reference;

        /// <summary>
        /// Gets the reference for the current component.
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
        /// Called by the Blazor runtime.
        /// </summary>
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
                        await JSRuntime.InvokeVoidAsync("Radzen.addContextMenu", GetId(), Reference);
                    }

                    if (MouseEnter.HasDelegate)
                    {
                        await JSRuntime.InvokeVoidAsync("Radzen.addMouseEnter", GetId(), Reference);
                    }

                    if (MouseLeave.HasDelegate)
                    {
                        await JSRuntime.InvokeVoidAsync("Radzen.addMouseLeave", GetId(), Reference);
                    }
                }
            }
        }

        /// <summary>
        /// Invoked via interop when the browser "oncontextmenu" event is raised for this component.
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
        /// Invoked via interop when the browser "onmouseenter" event is raised for this component.
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
        /// Invoked via interop when the browser "onmouseleave" event is raised for this component.
        /// </summary>
        [JSInvokable("RadzenComponent.RaiseMouseLeave")]
        public async System.Threading.Tasks.Task RaiseMouseLeave()
        {
            if (MouseLeave.HasDelegate)
            {
                await OnMouseLeave();
            }
        }

        internal bool disposed = false;

        /// <summary>
        /// Detaches event handlers and disposes <see cref="Reference" />.
        /// </summary>
        public virtual void Dispose()
        {
            disposed = true;

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
        /// Gets the current style as a dictionary.
        /// </summary>
        /// <value>The current style as a dictionary of keys and values.</value>
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
