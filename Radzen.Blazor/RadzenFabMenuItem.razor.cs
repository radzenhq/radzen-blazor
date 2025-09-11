using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
	/// <summary>
	/// RadzenFabMenuItem component.
	/// </summary>
	/// <example>
	/// <code>
	/// &lt;RadzenFabMenuItem Text="Folder" Icon="folder" Click=@(args => Console.WriteLine("Item clicked")) /&gt;
	/// </code>
	/// </example>
	public partial class RadzenFabMenuItem : RadzenComponent
	{
		[CascadingParameter] internal RadzenFabMenu Parent { get; set; }

		/// <summary>
        /// Gets or sets the child content.
        /// </summary>
        /// <value>The child content.</value>
        [Parameter]
		public RenderFragment ChildContent { get; set; }

        
		/// <summary>
        /// Gets or sets the index of the tab.
        /// </summary>
        /// <value>The index of the tab.</value>
        [Parameter]
        public int TabIndex { get; set; } = 0;
		
        /// <summary>
        /// Gets or sets the text.
        /// </summary>
        /// <value>The text.</value>
        [Parameter]
        public string Text { get; set; } = "";

        /// <summary>
        /// Gets or sets the alt text of an image.
        /// </summary>
        /// <value>The alt text of an image.</value>
		[Parameter]
        public string ImageAlternateText { get; set; } = "button";

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
        /// Gets or sets the image.
        /// </summary>
        /// <value>The image.</value>
        [Parameter]
		public string Image { get; set; }


		/// <summary>
        /// Gets or sets the button style.
        /// </summary>
        /// <value>The button style.</value>
        [Parameter]
		public ButtonStyle ButtonStyle { get; set; } = ButtonStyle.Primary;

		/// <summary>
        /// Gets or sets the type of the button.
        /// </summary>
        /// <value>The type of the button.</value>
        [Parameter]
		public ButtonType ButtonType { get; set; } = ButtonType.Button;

		/// <summary>
        /// Gets or sets the design variant of the button.
        /// </summary>
        /// <value>The variant of the button.</value>
        [Parameter]
		public Variant Variant { get; set; } = Variant.Flat;

		/// <summary>
        /// Gets or sets the color shade of the button.
        /// </summary>
        /// <value>The color shade of the button.</value>
        [Parameter]
		public Shade Shade { get; set; } = Shade.Light;

		/// <summary>
        /// Gets or sets the size.
        /// </summary>
        /// <value>The size.</value>
        [Parameter]
		public ButtonSize Size { get; set; } = ButtonSize.Large;

		/// <summary>
        /// Gets or sets a value indicating whether this <see cref="RadzenButton"/> is disabled.
        /// </summary>
        /// <value><c>true</c> if disabled; otherwise, <c>false</c>.</value>
		[Parameter] public bool Disabled { get; set; }

        /// <summary>
        /// Gets or sets the click callback.
        /// </summary>
        /// <value>The click callback.</value>
        [Parameter]
		public EventCallback<MouseEventArgs> Click { get; set; }

		/// <summary>
		/// Handles the click event and closes the parent FabMenu.
		/// </summary>
		/// <param name="args">The <see cref="MouseEventArgs"/> instance containing the event data.</param>
		protected async Task OnClickProxy(MouseEventArgs args)
		{
			await Click.InvokeAsync(args);
			if (Parent != null)
			{
				await Parent.CloseFromItemAsync();
			}
		}
	}
} 