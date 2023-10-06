using Microsoft.AspNetCore.Components;
using Radzen.Blazor.Rendering;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenColorPicker component.
    /// </summary>
    /// <example>
    /// <code>
    /// &lt;RadzenColorPicker @bind-Value=@color Change=@(args => Console.WriteLine($"Selected color: {args}")) /&gt;
    /// </code>
    /// </example>
    public partial class RadzenColorPicker : FormComponent<string>
    {
        /// <summary>
        /// Gets or sets the open callback.
        /// </summary>
        /// <value>The open callback.</value>
        [Parameter]
        public EventCallback Open { get; set; }

        /// <summary>
        /// Gets or sets the close callback.
        /// </summary>
        /// <value>The close callback.</value>
        [Parameter]
        public EventCallback Close { get; set; }

        /// <summary>
        /// Gets or sets the icon.
        /// </summary>
        /// <value>The icon.</value>
        [Parameter]
        public string Icon { get; set; }

        /// <summary>
        /// Gets or sets the icon color.
        /// </summary>
        /// <value>The icon color.</value>
        [Parameter]
        public string IconColor { get; set; }

        /// <summary>
        /// Gets or sets the hexadecimal color label text.
        /// </summary>
        /// <value>The hexadecimal text.</value>
        [Parameter]
        public string HexText { get; set; } = "Hex";

        /// <summary>
        /// Gets or sets the red color label text.
        /// </summary>
        /// <value>The red text.</value>
        [Parameter]
        public string RedText { get; set; } = "R";

        /// <summary>
        /// Gets or sets the green color label text.
        /// </summary>
        /// <value>The green text.</value>
        [Parameter]
        public string GreenText { get; set; } = "G";

        /// <summary>
        /// Gets or sets the blue color label text.
        /// </summary>
        /// <value>The blue text.</value>
        [Parameter]
        public string BlueText { get; set; } = "B";

        /// <summary>
        /// Gets or sets the alpha label text.
        /// </summary>
        /// <value>The alpha text.</value>
        [Parameter]
        public string AlphaText { get; set; } = "A";

        /// <summary>
        /// Gets or sets the button text.
        /// </summary>
        /// <value>The button text.</value>
        [Parameter]
        public string ButtonText { get; set; } = "OK";

        Popup Popup { get; set; }

        string AlphaGradientStart
        {
            get
            {
                var rgb = RGB.Parse(Color);
                rgb.Alpha = 0;
                return rgb.ToCSS();
            }
        }

        string AlphaGradientEnd
        {
            get
            {
                var rgb = RGB.Parse(Color);
                rgb.Alpha = 1;
                return rgb.ToCSS();
            }
        }

        string Hex
        {
            get
            {
                var rgb = RGB.Parse(Color);

                if (rgb != null)
                {
                    return rgb.ToHex();
                }

                return String.Empty;
            }
        }

        double Red
        {
            get
            {
                var rgb = RGB.Parse(Color);
                return rgb.Red;
            }
        }

        double Alpha
        {
            get
            {
                return Math.Round(AlphaHandleLeft * 100);
            }
        }

        double Green
        {
            get
            {
                var rgb = RGB.Parse(Color);
                return rgb.Green;
            }
        }

        double Blue
        {
            get
            {
                var rgb = RGB.Parse(Color);
                return rgb.Blue;
            }
        }

        void UpdateColorUsingHsvHandles()
        {
            var hsv = new HSV {
                Hue = HueHandleLeft,
                Saturation = SaturationHandleLeft,
                Value = 1 - SaturationHandleTop,
                Alpha = AlphaHandleLeft
            };
            Color = hsv.ToRGB().ToCSS();
            TriggerChange();
        }

        void OnSaturationMove(DraggableEventArgs args)
        {
            SaturationHandleLeft = Math.Clamp((args.ClientX - args.Rect.Left) / args.Rect.Width, 0, 1);
            SaturationHandleTop = Math.Clamp((args.ClientY - args.Rect.Top) / args.Rect.Height, 0, 1);

            UpdateColorUsingHsvHandles();
        }

        void TriggerChange()
        {
            if (!ShowButton)
            {
                ValueChanged.InvokeAsync(Color);
                Change.InvokeAsync(Color);
            }

            StateHasChanged();
        }

        void ChangeRGB(object value)
        {
            var rgb = RGB.Parse(value as string);
            if (rgb != null)
            {
                rgb.Alpha = AlphaHandleLeft;
                UpdateColor(rgb);
            }
        }

        internal async Task SelectColor(string value)
        {
            UpdateColor(RGB.Parse(value));

            if (!ShowButton)
            {
                await Popup.CloseAsync();
            }
        }

        void UpdateColor(RGB rgb)
        {
            Color = rgb.ToCSS();

            var hsv = rgb.ToHSV();

            SaturationHandleLeft = hsv.Saturation;
            SaturationHandleTop = 1 - hsv.Value;
            HueHandleLeft = hsv.Hue;
            AlphaHandleLeft = hsv.Alpha;

            TriggerChange();
        }

        void ChangeAlpha(double value)
        {
            if (value >= 0 && value <= 100)
            {
                var rgb = RGB.Parse(Color);
                AlphaHandleLeft = rgb.Alpha = Math.Round(value / 100, 2);

                Color = rgb.ToCSS();

                TriggerChange();
            }
        }

        void ChangeAlpha(object alpha)
        {
            if (Double.TryParse((string)alpha, out var value))
            {
                ChangeAlpha(value);
            }
        }

        void ChangeColor(double value, Action<RGB, double> update)
        {
            if (value >= 0 && value <= 255)
            {
                var rgb = RGB.Parse(Color);

                update(rgb, value);

                UpdateColor(rgb);
            }
        }

        void ChangeColor(object color, Action<RGB, double> update)
        {
            if (Double.TryParse((string)color, out var value))
            {
                ChangeColor(value, update);
            }
        }

        void OnAlphaMove(DraggableEventArgs args)
        {
            AlphaHandleLeft = Math.Round(Math.Clamp((args.ClientX - args.Rect.Left) / args.Rect.Width, 0, 1), 2);

            UpdateColorUsingHsvHandles();
        }

        void OnHueMove(DraggableEventArgs args)
        {
            HueHandleLeft = Math.Clamp((args.ClientX - args.Rect.Left) / args.Rect.Width, 0, 1);

            UpdateColorUsingHsvHandles();
        }

        async Task OnClick()
        {
            await ValueChanged.InvokeAsync(Color);
            await Change.InvokeAsync(Color);
            await Popup.CloseAsync();
        }

        async Task OnClosePopup()
        {
            if (ShowButton)
            {
                SetInitialValue();
            }
            await Close.InvokeAsync(null);
        }

        /// <summary>
        /// Gets or sets a value indicating whether button is shown.
        /// </summary>
        /// <value><c>true</c> if button shown; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool ShowButton { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether HSV is shown.
        /// </summary>
        /// <value><c>true</c> if HSV is shown; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool ShowHSV { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether RGBA is shown.
        /// </summary>
        /// <value><c>true</c> if RGBA is shown; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool ShowRGBA { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether colors are shown.
        /// </summary>
        /// <value><c>true</c> if colors are shown; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool ShowColors { get; set; } = true;

        /// <summary>
        /// Gets or sets the child content.
        /// </summary>
        /// <value>The child content.</value>
        [Parameter]
        public RenderFragment ChildContent { get; set; }

        /// <summary>
        /// Gets or sets the render mode.
        /// </summary>
        /// <value>The render mode.</value>
        [Parameter]
        public PopupRenderMode PopupRenderMode { get; set; } = PopupRenderMode.Initial;

        double SaturationHandleLeft { get; set; } = 0;
        double SaturationHandleTop { get; set; } = 0;
        double HueHandleLeft { get; set; } = 0;
        double AlphaHandleLeft { get; set; } = 1;
        string Color { get; set; } = "rgb(255, 255, 255)";

        async Task Toggle()
        {
            if (!Disabled)
            {
                await Popup.ToggleAsync(Element);
            }
        }
        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            var classList = new List<string>() { "rz-colorpicker" };

            if (Disabled)
            {
                classList.Add("rz-state-disabled");
            }

            return string.Join(" ", classList);
        }

        /// <inheritdoc />
        protected override void OnInitialized()
        {
            SetInitialValue();

            base.OnInitialized();
        }

        void SetInitialValue()
        {
            var value = Value;

            if (String.IsNullOrEmpty(Value) || RGB.Parse(Value) == null)
            {
                value = "rgb(255, 255, 255)";
            }

            if (value != Color)
            {
                Color = value;

                var hsv = RGB.Parse(Color).ToHSV();
                SaturationHandleLeft = hsv.Saturation;
                SaturationHandleTop = 1 - hsv.Value;
                HueHandleLeft = hsv.Hue;
                AlphaHandleLeft = hsv.Alpha;
            }
        }

        /// <inheritdoc />
        public override async Task SetParametersAsync(ParameterView parameters)
        {
            var valueChanged = parameters.DidParameterChange(nameof(Value), Value);

            await base.SetParametersAsync(parameters);

            if (valueChanged)
            {
                SetInitialValue();
            }
        }
    }
}
