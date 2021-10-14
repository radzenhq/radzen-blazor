using Microsoft.AspNetCore.Components;
using Radzen.Blazor.Rendering;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    /// <summary>
    /// Class RadzenColorPicker.
    /// Implements the <see cref="Radzen.FormComponent{System.String}" />
    /// </summary>
    /// <seealso cref="Radzen.FormComponent{System.String}" />
    public partial class RadzenColorPicker : FormComponent<string>
    {
        /// <summary>
        /// Gets or sets the open.
        /// </summary>
        /// <value>The open.</value>
        [Parameter]
        public EventCallback Open { get; set; }

        /// <summary>
        /// Gets or sets the close.
        /// </summary>
        /// <value>The close.</value>
        [Parameter]
        public EventCallback Close { get; set; }

        /// <summary>
        /// Gets or sets the icon.
        /// </summary>
        /// <value>The icon.</value>
        [Parameter]
        public string Icon { get; set; }

        /// <summary>
        /// Gets or sets the hexadecimal text.
        /// </summary>
        /// <value>The hexadecimal text.</value>
        [Parameter]
        public string HexText { get; set; } = "Hex";

        /// <summary>
        /// Gets or sets the red text.
        /// </summary>
        /// <value>The red text.</value>
        [Parameter]
        public string RedText { get; set; } = "R";

        /// <summary>
        /// Gets or sets the green text.
        /// </summary>
        /// <value>The green text.</value>
        [Parameter]
        public string GreenText { get; set; } = "G";

        /// <summary>
        /// Gets or sets the blue text.
        /// </summary>
        /// <value>The blue text.</value>
        [Parameter]
        public string BlueText { get; set; } = "B";

        /// <summary>
        /// Gets or sets the alpha text.
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

        /// <summary>
        /// Gets or sets the popup.
        /// </summary>
        /// <value>The popup.</value>
        Popup Popup { get; set; }

        /// <summary>
        /// Gets the alpha gradient start.
        /// </summary>
        /// <value>The alpha gradient start.</value>
        string AlphaGradientStart
        {
            get
            {
                var rgb = RGB.Parse(Color);
                rgb.Alpha = 0;
                return rgb.ToCSS();
            }
        }

        /// <summary>
        /// Gets the alpha gradient end.
        /// </summary>
        /// <value>The alpha gradient end.</value>
        string AlphaGradientEnd
        {
            get
            {
                var rgb = RGB.Parse(Color);
                rgb.Alpha = 1;
                return rgb.ToCSS();
            }
        }

        /// <summary>
        /// Gets the hexadecimal.
        /// </summary>
        /// <value>The hexadecimal.</value>
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


        /// <summary>
        /// Gets the red.
        /// </summary>
        /// <value>The red.</value>
        double Red
        {
            get
            {
                var rgb = RGB.Parse(Color);
                return rgb.Red;
            }
        }

        /// <summary>
        /// Gets the alpha.
        /// </summary>
        /// <value>The alpha.</value>
        double Alpha
        {
            get
            {
                return Math.Round(AlphaHandleLeft * 100);
            }
        }

        /// <summary>
        /// Gets the green.
        /// </summary>
        /// <value>The green.</value>
        double Green
        {
            get
            {
                var rgb = RGB.Parse(Color);
                return rgb.Green;
            }
        }

        /// <summary>
        /// Gets the blue.
        /// </summary>
        /// <value>The blue.</value>
        double Blue
        {
            get
            {
                var rgb = RGB.Parse(Color);
                return rgb.Blue;
            }
        }


        /// <summary>
        /// Handles the <see cref="E:SaturationMove" /> event.
        /// </summary>
        /// <param name="args">The <see cref="DraggableEventArgs"/> instance containing the event data.</param>
        void OnSaturationMove(DraggableEventArgs args)
        {
            SaturationHandleLeft = Math.Clamp((args.ClientX - args.Rect.Left) / args.Rect.Width, 0, 1);
            SaturationHandleTop = Math.Clamp((args.ClientY - args.Rect.Top) / args.Rect.Height, 0, 1);

            var hsv = new HSV { Hue = HSV.Hue, Saturation = SaturationHandleLeft, Value = 1 - SaturationHandleTop, Alpha = AlphaHandleLeft };

            Color = hsv.ToRGB().ToCSS();

            TriggerChange();
        }

        /// <summary>
        /// Triggers the change.
        /// </summary>
        void TriggerChange()
        {
            if (!ShowButton)
            {
                ValueChanged.InvokeAsync(Color);
                Change.InvokeAsync(Color);
            }

            StateHasChanged();
        }

        /// <summary>
        /// Changes the RGB.
        /// </summary>
        /// <param name="value">The value.</param>
        void ChangeRGB(object value)
        {
            SetValue(value as string);
        }

        /// <summary>
        /// Sets the value.
        /// </summary>
        /// <param name="value">The value.</param>
        void SetValue(string value)
        {
            var rgb = RGB.Parse(value);

            if (rgb != null)
            {
                Color = rgb.ToCSS();
                UpdateColor(rgb);
            }
        }

        /// <summary>
        /// Selects the color.
        /// </summary>
        /// <param name="value">The value.</param>
        internal async Task SelectColor(string value)
        {
            SetValue(value);

            if (!ShowButton)
            {
                await Popup.CloseAsync();
            }
        }

        /// <summary>
        /// Updates the color.
        /// </summary>
        /// <param name="rgb">The RGB.</param>
        void UpdateColor(RGB rgb)
        {
            Color = rgb.ToCSS();

            HSV = rgb.ToHSV();

            SaturationHandleLeft = HSV.Saturation;
            SaturationHandleTop = 1 - HSV.Value;
            HueHandleLeft = HSV.Hue;

            TriggerChange();
        }

        /// <summary>
        /// Changes the alpha.
        /// </summary>
        /// <param name="value">The value.</param>
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

        /// <summary>
        /// Changes the alpha.
        /// </summary>
        /// <param name="alpha">The alpha.</param>
        void ChangeAlpha(object alpha)
        {
            if (Double.TryParse((string)alpha, out var value))
            {
                ChangeAlpha(value);
            }
        }

        /// <summary>
        /// Changes the color.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="update">The update.</param>
        void ChangeColor(double value, Action<RGB, double> update)
        {
            if (value >= 0 && value <= 255)
            {
                var rgb = RGB.Parse(Color);

                update(rgb, value);

                UpdateColor(rgb);
            }
        }

        /// <summary>
        /// Changes the color.
        /// </summary>
        /// <param name="color">The color.</param>
        /// <param name="update">The update.</param>
        void ChangeColor(object color, Action<RGB, double> update)
        {
            if (Double.TryParse((string)color, out var value))
            {
                ChangeColor(value, update);
            }
        }

        /// <summary>
        /// Handles the <see cref="E:AlphaMove" /> event.
        /// </summary>
        /// <param name="args">The <see cref="DraggableEventArgs"/> instance containing the event data.</param>
        void OnAlphaMove(DraggableEventArgs args)
        {
            AlphaHandleLeft = Math.Round(Math.Clamp((args.ClientX - args.Rect.Left) / args.Rect.Width, 0, 1), 2);

            HSV.Alpha = AlphaHandleLeft;

            var hsv = new HSV { Hue = HSV.Hue, Saturation = SaturationHandleLeft, Value = 1 - SaturationHandleTop, Alpha = AlphaHandleLeft };

            Color = hsv.ToRGB().ToCSS();

            TriggerChange();
        }

        /// <summary>
        /// Handles the <see cref="E:HueMove" /> event.
        /// </summary>
        /// <param name="args">The <see cref="DraggableEventArgs"/> instance containing the event data.</param>
        void OnHueMove(DraggableEventArgs args)
        {
            HueHandleLeft = Math.Clamp((args.ClientX - args.Rect.Left) / args.Rect.Width, 0, 1);

            HSV.Hue = HueHandleLeft;
            var hsv = new HSV { Hue = HSV.Hue, Saturation = SaturationHandleLeft, Value = 1 - SaturationHandleTop, Alpha = AlphaHandleLeft };

            Color = hsv.ToRGB().ToCSS();

            TriggerChange();
        }

        /// <summary>
        /// Called when [click].
        /// </summary>
        async Task OnClick()
        {
            await ValueChanged.InvokeAsync(Color);
            await Change.InvokeAsync(Color);
            await Popup.CloseAsync();
        }

        /// <summary>
        /// Gets or sets a value indicating whether [show button].
        /// </summary>
        /// <value><c>true</c> if [show button]; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool ShowButton { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [show HSV].
        /// </summary>
        /// <value><c>true</c> if [show HSV]; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool ShowHSV { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether [show rgba].
        /// </summary>
        /// <value><c>true</c> if [show rgba]; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool ShowRGBA { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether [show colors].
        /// </summary>
        /// <value><c>true</c> if [show colors]; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool ShowColors { get; set; } = true;

        /// <summary>
        /// Gets or sets the child content.
        /// </summary>
        /// <value>The child content.</value>
        [Parameter]
        public RenderFragment ChildContent { get; set; }

        /// <summary>
        /// Gets or sets the saturation handle left.
        /// </summary>
        /// <value>The saturation handle left.</value>
        double SaturationHandleLeft { get; set; }
        /// <summary>
        /// Gets or sets the hue handle left.
        /// </summary>
        /// <value>The hue handle left.</value>
        double HueHandleLeft { get; set; }
        /// <summary>
        /// Gets or sets the alpha handle left.
        /// </summary>
        /// <value>The alpha handle left.</value>
        double AlphaHandleLeft { get; set; } = 1;
        /// <summary>
        /// Gets or sets the saturation handle top.
        /// </summary>
        /// <value>The saturation handle top.</value>
        double SaturationHandleTop { get; set; }

        /// <summary>
        /// Gets or sets the HSV.
        /// </summary>
        /// <value>The HSV.</value>
        HSV HSV { get; set; } = new HSV { Hue = 0, Saturation = 1, Value = 1 };

        /// <summary>
        /// Gets or sets the color.
        /// </summary>
        /// <value>The color.</value>
        string Color { get; set; } = "rgb(255, 255, 255)";

        /// <summary>
        /// Toggles this instance.
        /// </summary>
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
        /// Called when [initialized].
        /// </summary>
        protected override void OnInitialized()
        {
            Init();

            base.OnInitialized();
        }

        /// <summary>
        /// Initializes this instance.
        /// </summary>
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