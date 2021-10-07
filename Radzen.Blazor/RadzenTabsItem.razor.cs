using Microsoft.AspNetCore.Components;
using Radzen.Blazor.Rendering;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    public partial class RadzenTabsItem : IDisposable
    {
        [Parameter(CaptureUnmatchedValues = true)]
        public IDictionary<string, object> Attributes { get; set; }

        [Parameter]
        public string Style { get; set; }

        [Parameter]
        public bool Visible { get; set; } = true;

        [Parameter]
        public string Text { get; set; }

        [Parameter]
        public RenderFragment<RadzenTabsItem> Template { get; set; }

        [Parameter]
        public string Icon { get; set; }

        [Parameter]
        public bool Selected { get; set; }

        [Parameter]
        public bool Disabled { get; set; }

        ClassList ClassList => ClassList.Create("rz-state-active")
                                        .Add("rz-tabview-selected", IsSelected)
                                        .AddDisabled(Disabled)
                                        .Add(Attributes);

        public int Index
        {
            get
            {
                return Tabs.IndexOf(this);
            }
        }

        public bool IsSelected
        {
            get
            {
                return Tabs.IsSelected(this);
            }
        }

        [Parameter]
        public RenderFragment ChildContent { get; set; }

        [CascadingParameter]
        public RadzenTabs Tabs { get; set; }

        protected override async Task OnInitializedAsync()
        {
            await Tabs.AddTab(this);
        }

        async Task OnClick()
        {
            if (!Disabled)
            {
                if (Tabs.RenderMode == TabRenderMode.Server)
                {
                    await Tabs.SelectTab(this, true);
                }
                else
                {
                    await Tabs.SelectTabOnClient(this);
                }
            }
        }

        public override async Task SetParametersAsync(ParameterView parameters)
        {
            var selectedChanged = parameters.DidParameterChange(nameof(Selected), Selected);
            var visibleChanged = parameters.DidParameterChange(nameof(Visible), Visible);

            await base.SetParametersAsync(parameters);

            if (selectedChanged)
            {
                if (Selected)
                {
                    Tabs?.SelectTab(this);
                }
            }

            if (visibleChanged && IsSelected)
            {
                Tabs?.SelectTab(this);
            }
        }

        public void Dispose()
        {
            Tabs?.RemoveItem(this);
        }
    }
}