using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Radzen
{
    public class RadzenComponent : ComponentBase, IDisposable
    {
        [Parameter(CaptureUnmatchedValues = true)]
        public IReadOnlyDictionary<string, object> Attributes { get; set; }

        public ElementReference Element { get; internal set; }

        [Parameter]
        public EventCallback<ElementReference> MouseEnter { get; set; }

        [Parameter]
        public EventCallback<ElementReference> MouseLeave { get; set; }

        [Parameter]
        public EventCallback<Microsoft.AspNetCore.Components.Web.MouseEventArgs> ContextMenu { get; set; }

        public async Task OnMouseEnter()
        {
            await MouseEnter.InvokeAsync(Element);
        }

        public async Task OnMouseLeave()
        {
            await MouseLeave.InvokeAsync(Element);
        }

        public virtual async Task OnContextMenu(Microsoft.AspNetCore.Components.Web.MouseEventArgs args)
        {
            await ContextMenu.InvokeAsync(args);
        }

        [Parameter]
        public virtual string Style { get; set; }

        [Parameter]
        public virtual bool Visible { get; set; } = true;

        protected string GetCssClass()
        {
            if (Attributes != null && Attributes.TryGetValue("class", out var @class) && !string.IsNullOrEmpty(Convert.ToString(@class)))
            {
                return $"{GetComponentCssClass()} {@class}";
            }

            return GetComponentCssClass();
        }

        protected string GetId()
        {
            if (Attributes != null && Attributes.TryGetValue("id", out var id) && !string.IsNullOrEmpty(Convert.ToString(@id)))
            {
                return $"{@id}";
            }

            return UniqueID;
        }

        protected virtual string GetComponentCssClass()
        {
            return "";
        }

        Debouncer debouncer = new Debouncer();

        protected void Debounce(Func<Task> action, int milliseconds = 500)
        {
            debouncer.Debounce(milliseconds, action);
        }

        public string UniqueID { get; set; }

        [Inject]
        protected IJSRuntime JSRuntime { get; set; }

        protected override void OnInitialized()
        {
            UniqueID = Convert.ToBase64String(Guid.NewGuid().ToByteArray()).Replace("/", "-").Replace("+", "-").Substring(0, 10);            
        }

        private bool visibleChanged = false;
        private bool firstRender = true;

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

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
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

        [JSInvokable("RadzenComponent.RaiseContextMenu")]
        public async System.Threading.Tasks.Task RaiseContextMenu(Microsoft.AspNetCore.Components.Web.MouseEventArgs e)
        {
            if (ContextMenu.HasDelegate)
            {
                await OnContextMenu(e);
            }
        }

        [JSInvokable("RadzenComponent.RaiseMouseEnter")]
        public async System.Threading.Tasks.Task RaiseMouseEnter()
        {
            if (MouseEnter.HasDelegate)
            {
                await OnMouseEnter();
            }
        }

        [JSInvokable("RadzenComponent.RaiseMouseLeave")]
        public async System.Threading.Tasks.Task RaiseMouseLeave()
        {
            if (MouseLeave.HasDelegate)
            {
                await OnMouseLeave();
            }
        }

        public virtual void Dispose()
        {
            reference?.Dispose();
            reference = null;

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
