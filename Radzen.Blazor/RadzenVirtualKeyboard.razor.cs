using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Radzen.Blazor
{
    /// <summary>
    /// An on-screen virtual keyboard for touch HMI and kiosk scenarios. Attaches to the inputs rendered inside it and
    /// opens automatically when one of them gets focus. Key presses insert text in the focused input and raise the native
    /// <c>input</c> and <c>change</c> events so <c>@bind-Value</c> of the wrapped components updates as if typed on a physical keyboard.
    /// </summary>
    /// <example>
    /// <code>
    /// &lt;RadzenVirtualKeyboard&gt;
    ///     &lt;RadzenTextBox @bind-Value=@name /&gt;
    ///     &lt;RadzenNumeric @bind-Value=@quantity /&gt;
    /// &lt;/RadzenVirtualKeyboard&gt;
    /// </code>
    /// </example>
    public partial class RadzenVirtualKeyboard : RadzenComponent
    {
#nullable disable

        IJSObjectReference jsRef;
        bool visibleChanged;

        /// <summary>
        /// Gets or sets the content containing the inputs the keyboard attaches to.
        /// </summary>
        [Parameter]
        public RenderFragment ChildContent { get; set; }

        /// <summary>
        /// Gets or sets the set of keys displayed by the keyboard. Set to <see cref="VirtualKeyboardType.Alphanumeric" /> by default.
        /// </summary>
        [Parameter]
        public VirtualKeyboardType Type { get; set; } = VirtualKeyboardType.Alphanumeric;

        /// <summary>
        /// Gets or sets where the keyboard is displayed. Set to <see cref="VirtualKeyboardPlacement.Bottom" /> by default.
        /// </summary>
        [Parameter]
        public VirtualKeyboardPlacement Placement { get; set; } = VirtualKeyboardPlacement.Bottom;

        /// <summary>
        /// Gets or sets a custom key layout which replaces the default one for the current <see cref="Type" />.
        /// Use the <see cref="VirtualKeyboardLayout.Qwerty" />, <see cref="VirtualKeyboardLayout.Qwertz" /> and
        /// <see cref="VirtualKeyboardLayout.Azerty" /> presets for locale-aware layouts or
        /// <see cref="VirtualKeyboardLayout.FromRows(string[])" /> to define your own keys.
        /// </summary>
        [Parameter]
        public VirtualKeyboardLayout Layout { get; set; }

        /// <summary>
        /// Gets or sets the text inserted by the <c>{decimal}</c> key. The decimal separator of <see cref="RadzenComponent.Culture" /> is used by default.
        /// </summary>
        [Parameter]
        public string DecimalSeparator { get; set; }

        string ariaLabel;
        /// <summary>
        /// Gets or sets the aria-label of the keyboard panel.
        /// </summary>
        [Parameter]
        public string AriaLabel { get => ariaLabel ?? Localize(nameof(RadzenStrings.VirtualKeyboard_AriaLabel)); set => ariaLabel = value; }

        string backspaceAriaLabel;
        /// <summary>
        /// Gets or sets the aria-label of the backspace key.
        /// </summary>
        [Parameter]
        public string BackspaceAriaLabel { get => backspaceAriaLabel ?? Localize(nameof(RadzenStrings.VirtualKeyboard_BackspaceAriaLabel)); set => backspaceAriaLabel = value; }

        string enterAriaLabel;
        /// <summary>
        /// Gets or sets the aria-label of the enter key.
        /// </summary>
        [Parameter]
        public string EnterAriaLabel { get => enterAriaLabel ?? Localize(nameof(RadzenStrings.VirtualKeyboard_EnterAriaLabel)); set => enterAriaLabel = value; }

        string shiftAriaLabel;
        /// <summary>
        /// Gets or sets the aria-label of the shift key.
        /// </summary>
        [Parameter]
        public string ShiftAriaLabel { get => shiftAriaLabel ?? Localize(nameof(RadzenStrings.VirtualKeyboard_ShiftAriaLabel)); set => shiftAriaLabel = value; }

        string spaceAriaLabel;
        /// <summary>
        /// Gets or sets the aria-label of the space key.
        /// </summary>
        [Parameter]
        public string SpaceAriaLabel { get => spaceAriaLabel ?? Localize(nameof(RadzenStrings.VirtualKeyboard_SpaceAriaLabel)); set => spaceAriaLabel = value; }

        string tabAriaLabel;
        /// <summary>
        /// Gets or sets the aria-label of the tab key.
        /// </summary>
        [Parameter]
        public string TabAriaLabel { get => tabAriaLabel ?? Localize(nameof(RadzenStrings.VirtualKeyboard_TabAriaLabel)); set => tabAriaLabel = value; }

        string clearAriaLabel;
        /// <summary>
        /// Gets or sets the aria-label of the clear key.
        /// </summary>
        [Parameter]
        public string ClearAriaLabel { get => clearAriaLabel ?? Localize(nameof(RadzenStrings.VirtualKeyboard_ClearAriaLabel)); set => clearAriaLabel = value; }

        string closeAriaLabel;
        /// <summary>
        /// Gets or sets the aria-label of the close key.
        /// </summary>
        [Parameter]
        public string CloseAriaLabel { get => closeAriaLabel ?? Localize(nameof(RadzenStrings.VirtualKeyboard_CloseAriaLabel)); set => closeAriaLabel = value; }

        /// <inheritdoc />
        protected override string GetComponentCssClass() => "rz-virtual-keyboard";

        string PanelCssClass => $"rz-virtual-keyboard-panel rz-virtual-keyboard-{Placement.ToString().ToLowerInvariant()}";

        string PanelStyle => Placement == VirtualKeyboardPlacement.Inline ? null : "display:none";

        IEnumerable<VirtualKeyboardLayout> Sections
        {
            get
            {
                if (Type == VirtualKeyboardType.Numpad)
                {
                    yield return Layout ?? VirtualKeyboardLayout.Numpad;
                }
                else
                {
                    yield return Layout ?? VirtualKeyboardLayout.Qwerty;

                    if (Type == VirtualKeyboardType.All)
                    {
                        yield return VirtualKeyboardLayout.Numpad;
                    }
                }
            }
        }

        class KeyInfo
        {
            public string Text { get; set; }
            public string ShiftText { get; set; }
            public string Action { get; set; }
            public string Icon { get; set; }
            public string Label { get; set; }
            public string ShiftLabel { get; set; }
            public string AriaLabel { get; set; }
            public string CssClass { get; set; }
        }

        KeyInfo GetKey(VirtualKeyboardLayout section, int rowIndex, int keyIndex)
        {
            var token = section.Rows[rowIndex][keyIndex];

            switch (token)
            {
                case "{backspace}":
                    return SpecialKey("backspace", action: "backspace", icon: "backspace", ariaLabel: BackspaceAriaLabel);
                case "{enter}":
                    return SpecialKey("enter", action: "enter", icon: "keyboard_return", ariaLabel: EnterAriaLabel);
                case "{shift}":
                    return SpecialKey("shift", action: "shift", icon: "shift", ariaLabel: ShiftAriaLabel);
                case "{clear}":
                    return SpecialKey("clear", action: "clear", label: "C", ariaLabel: ClearAriaLabel);
                case "{close}":
                    return SpecialKey("close", action: "close", icon: "keyboard_hide", ariaLabel: CloseAriaLabel);
                case "{space}":
                    return SpecialKey("space", text: " ", icon: "space_bar", ariaLabel: SpaceAriaLabel);
                case "{tab}":
                    return SpecialKey("tab", text: "\t", icon: "keyboard_tab", ariaLabel: TabAriaLabel);
                case "{decimal}":
                    var separator = DecimalSeparator ?? Culture.NumberFormat.NumberDecimalSeparator;
                    return new KeyInfo { Text = separator, Label = separator, CssClass = "rz-virtual-keyboard-key" };
                default:
                    var shiftToken = section.ShiftRows != null && rowIndex < section.ShiftRows.Count && keyIndex < section.ShiftRows[rowIndex].Length
                        ? section.ShiftRows[rowIndex][keyIndex] : null;

                    if (shiftToken == token || (shiftToken != null && shiftToken.StartsWith('{')))
                    {
                        shiftToken = null;
                    }

                    return new KeyInfo { Text = token, ShiftText = shiftToken, Label = token, ShiftLabel = shiftToken, CssClass = "rz-virtual-keyboard-key" };
            }
        }

        static KeyInfo SpecialKey(string name, string action = null, string text = null, string icon = null, string label = null, string ariaLabel = null)
        {
            return new KeyInfo
            {
                Text = text,
                Action = action,
                Icon = icon,
                Label = label,
                AriaLabel = ariaLabel,
                CssClass = $"rz-virtual-keyboard-key {(action != null ? "rz-virtual-keyboard-key-action " : "")}rz-virtual-keyboard-key-{name}"
            };
        }

        /// <inheritdoc />
        public override async Task SetParametersAsync(ParameterView parameters)
        {
            visibleChanged = parameters.DidParameterChange(nameof(Visible), Visible);

            await base.SetParametersAsync(parameters);
        }

        /// <inheritdoc />
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);

            if (firstRender || visibleChanged)
            {
                visibleChanged = false;

                if (jsRef != null)
                {
                    try
                    {
                        await jsRef.InvokeVoidAsync("dispose");
                        await jsRef.DisposeAsync();
                    }
                    catch (JSException)
                    {
                        //
                    }

                    jsRef = null;
                }

                if (Visible && JSRuntime != null)
                {
                    jsRef = await JSRuntime.InvokeAsync<IJSObjectReference>("Radzen.createVirtualKeyboard", GetId(), Element);
                }
            }
        }

        /// <inheritdoc />
        public override void Dispose()
        {
            base.Dispose();

            jsRef?.InvokeVoidAsync("dispose");
            jsRef?.DisposeAsync();
            jsRef = null;

            GC.SuppressFinalize(this);
        }
    }
}
