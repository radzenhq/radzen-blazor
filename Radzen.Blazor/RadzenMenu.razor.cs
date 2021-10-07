using Microsoft.AspNetCore.Components;
using System.Collections.Generic;

namespace Radzen.Blazor
{
    public partial class RadzenMenu : RadzenComponentWithChildren
    {
        [Parameter]
        public bool Responsive { get; set; } = true;

        private bool IsOpen { get; set; } = false;

        protected override string GetComponentCssClass()
        {
            var classList = new List<string>();

            classList.Add("rz-menu");

            if (Responsive)
            {
                if (IsOpen)
                {
                    classList.Add("rz-menu-open");
                }
                else
                {
                    classList.Add("rz-menu-closed");
                }
            }

            return string.Join(" ", classList);
        }

        void OnToggle()
        {
            IsOpen = !IsOpen;
        }

        [Parameter]
        public EventCallback<MenuItemEventArgs> Click { get; set; }
    }
}