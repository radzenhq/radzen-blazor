using Microsoft.AspNetCore.Components;
using Radzen.Blazor.Rendering;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenColorPicker component.
    /// Implements the <see cref="Radzen.FormComponent{System.String}" />
    /// </summary>
    /// <seealso cref="Radzen.FormComponent{System.String}" />
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

        void OnSaturationMove(DraggableEventArgs args)
        {
            SaturationHandleLeft = Math.Clamp((args.ClientX - args.Rect.Left) / args.Rect.Width, 0, 1);
            SaturationHandleTop = Math.Clamp((args.ClientY - args.Rect.Top) / args.Rect.Height, 0, 1);

            var hsv = new HSV { Hue = HSV.Hue, Saturation = SaturationHandleLeft, Value = 1 - SaturationHandleTop, Alpha = AlphaHandleLeft };

            Color = hsv.ToRGB().ToCSS();

            TriggerChange();
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
            SetValue(value as string);
        }

        void SetValue(string value)
        {
            var rgb = RGB.Parse(value);

            if (rgb != null)
            {
                Color = rgb.ToCSS();
                UpdateColor(rgb);
            }
        }

        internal async Task SelectColor(string value)
        {
            SetValue(value);

            if (!ShowButton)
            {
                await Popup.CloseAsync();
            }
        }

        void UpdateColor(RGB rgb)
        {
            Color = rgb.ToCSS();

            HSV = rgb.ToHSV();

            SaturationHandleLeft = HSV.Saturation;
            SaturationHandleTop = 1 - HSV.Value;
            HueHandleLeft = HSV.Hue;

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

            HSV.Alpha = AlphaHandleLeft;

            var hsv = new HSV { Hue = HSV.Hue, Saturation = SaturationHandleLeft, Value = 1 - SaturationHandleTop, Alpha = AlphaHandleLeft };

            Color = hsv.ToRGB().ToCSS();

            TriggerChange();
        }

        void OnHueMove(DraggableEventArgs args)
        {
            HueHandleLeft = Math.Clamp((args.ClientX - args.Rect.Left) / args.Rect.Width, 0, 1);

            HSV.Hue = HueHandleLeft;
            var hsv = new HSV { Hue = HSV.Hue, Saturation = SaturationHandleLeft, Value = 1 - SaturationHandleTop, Alpha = AlphaHandleLeft };

            Color = hsv.ToRGB().ToCSS();

            TriggerChange();
        }

        /// <summary>
        async Task OnClick()
        {
            await ValueChanged.InvokeAsync(Color);
            await Change.InvokeAsync(Color);
            await Popup.CloseAsync();
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

        double SaturationHandleLeft { get; set; }
        double HueHandleLeft { get; set; }
        double AlphaHandleLeft { get; set; } = 1;
        double SaturationHandleTop { get; set; }
        HSV HSV { get; set; } = new HSV { Hue = 0, Saturation = 1, Value = 1 };
        string Color { get; set; } = "rgb(255, 255, 255)";

        async Task Toggle()
        {
            if (!Disabled)
            {
                await Popup.ToggleAsync(Element);
            }
        }
        /// <summary>
        /// Gets the component CSS class.
        /// </summary>
        /// <returns>System.String.</returns>
        protected override string GetComponentCssClass()
        {
            var classList = new List<string>() { "rz-colorpicker" };

            if (Disabled)
            {
                classList.Add("rz-disabled");
            }

            return string.Join(" ", classList);
        }

        /// <summary>
        /// Called when initialized.
        /// </summary>
        protected override void OnInitialized()
        {
            Init();

            base.OnInitialized();
        }

        void Init()
        {
            var value = Value;
            if (String.IsNullOrEmpty(Value))
            {
                value = "rgb(255, 255, 255)";
            }

            if (value != Color)
            {
                Color = value;

                HSV = RGB.Parse(Color).ToHSV();
                SaturationHandleLeft = HSV.Saturation;
                SaturationHandleTop = 1 - HSV.Value;
                HSV.Saturation = 1;
                HSV.Value = 1;
                HueHandleLeft = HSV.Hue;

                if (value.StartsWith("rgba"))
                {
                    AlphaHandleLeft = HSV.Alpha;
                }
            }
        }

        /// <summary>
        /// Set parameters as an asynchronous operation.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        public override async Task SetParametersAsync(ParameterView parameters)
        {
            var valueChanged = parameters.DidParameterChange(nameof(Value), Value);

            await base.SetParametersAsync(parameters);

            if (valueChanged)
            {
                Init();
            }
        }
    }
}