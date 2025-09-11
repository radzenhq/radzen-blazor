using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
	/// <summary>
	/// RadzenFabMenu component.
	/// </summary>
	/// <example>
	/// <code>
	/// &lt;RadzenFabMenu Icon="add" ToggleIcon="close"&gt;
	///     &lt;RadzenFabMenuItem Text="Folder" Icon="folder" Click=@(args => Console.WriteLine("Folder clicked")) /&gt;
	///     &lt;RadzenFabMenuItem Text="Chat" Icon="chat" Click=@(args => Console.WriteLine("Chat clicked")) /&gt;
	/// &lt;/RadzenFabMenu&gt;
	/// </code>
	/// </example>
	public partial class RadzenFabMenu : RadzenComponent, IAsyncDisposable
	{

		/// <summary>
		/// Gets or sets the child content.
		/// </summary>
		/// <value>The child content.</value>
		[Parameter]
		public RenderFragment ChildContent { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether the menu is open.
		/// </summary>
		/// <value><c>true</c> if the menu is open; otherwise, <c>false</c>.</value>
		[Parameter]
		public bool IsOpen { get; set; }
		
		/// <summary>
		/// Gets or sets the IsOpen changed callback.
		/// </summary>
		/// <value>The IsOpen changed callback.</value>
		[Parameter]
		public EventCallback<bool> IsOpenChanged { get; set; }

		/// <summary>
		/// Gets or sets the icon.
		/// </summary>
		/// <value>The icon.</value>
		[Parameter]
		public string Icon { get; set; } = "add";

		/// <summary>
		/// Gets or sets the toggle icon.
		/// </summary>
		/// <value>The toggle icon.</value>
		[Parameter]
		public string ToggleIcon { get; set; } = "close";

		/// <summary>
		/// Gets or sets the button style.
		/// </summary>
		/// <value>The button style.</value>
		[Parameter]
		public ButtonStyle ButtonStyle { get; set; } = ButtonStyle.Primary;

		/// <summary>
		/// Gets or sets the size.
		/// </summary>
		/// <value>The size.</value>
		[Parameter]
		public ButtonSize Size { get; set; } = ButtonSize.Large;

		/// <summary>
		/// Gets or sets the variant.
		/// </summary>
		/// <value>The variant.</value>
		[Parameter]
		public Variant Variant { get; set; } = Variant.Filled;
		
		/// <summary>
		/// Gets or sets the shade.
		/// </summary>
		/// <value>The shade.</value>
		[Parameter]
		public Shade Shade { get; set; } = Shade.Default;

		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="RadzenFabMenu"/> is disabled.
		/// </summary>
		/// <value><c>true</c> if disabled; otherwise, <c>false</c>.</value>
		[Parameter]
		public bool Disabled { get; set; }

		/// <summary>
		/// Gets or sets the button class.
		/// </summary>
		/// <value>The button class.</value>
		[Parameter]
		public string ButtonClass { get; set; }

		/// <summary>
		/// Gets or sets the button style CSS.
		/// </summary>
		/// <value>The button style CSS.</value>
		[Parameter]
		public string ButtonStyleCss { get; set; }

		/// <summary>
		/// Gets or sets the gap.
		/// </summary>
		/// <value>The gap.</value>
		[Parameter]
		public string Gap { get; set; } = "0.25rem";

		/// <summary>
		/// Gets or sets the items style.
		/// </summary>
		/// <value>The items style.</value>
		[Parameter]
		public string ItemsStyle { get; set; }

		/// <summary>
		/// Gets or sets the direction in which the menu items expand.
		/// </summary>
		/// <value>The expansion direction.</value>
		[Parameter]
		public FabMenuDirection Direction { get; set; } = FabMenuDirection.Top;

		/// <summary>
		/// Gets or sets the FAB button reference.
		/// </summary>
		/// <value>The FAB button reference.</value>
		protected RadzenToggleButton FabButton { get; set; }

		DotNetObjectReference<RadzenFabMenu> dotNetRef;

		/// <summary>
		/// Gets the computed items style.
		/// </summary>
		/// <value>The computed items style.</value>
		protected string ComputedItemsStyle
		{
			get
			{
				return string.IsNullOrEmpty(ItemsStyle) ? string.Empty : ItemsStyle;
			}
		}

		/// <summary>
		/// Gets the stack orientation based on the direction.
		/// </summary>
		/// <returns>The stack orientation.</returns>
		protected Orientation GetStackOrientation()
		{
			return Direction switch
			{
				FabMenuDirection.Top or FabMenuDirection.Bottom => Orientation.Vertical,
				FabMenuDirection.Left or FabMenuDirection.Right or FabMenuDirection.Start or FabMenuDirection.End => Orientation.Horizontal,
				_ => Orientation.Vertical
			};
		}

		/// <summary>
		/// Gets the stack alignment based on the direction.
		/// </summary>
		/// <returns>The stack alignment.</returns>
		protected AlignItems GetStackAlignment()
		{
			return Direction switch
			{
				FabMenuDirection.Top => AlignItems.End,
				FabMenuDirection.Bottom => AlignItems.End,
				FabMenuDirection.Left => AlignItems.End,
				FabMenuDirection.Right => AlignItems.Start,
				FabMenuDirection.Start => AlignItems.End,
				FabMenuDirection.End => AlignItems.Start,
				_ => AlignItems.End
			};
		}

		/// <summary>
		/// Gets the stack CSS classes based on the direction.
		/// </summary>
		/// <returns>The stack CSS classes.</returns>
		protected string GetStackClasses()
		{
			var baseClasses = "rz-fabmenu-items rz-open";
			var directionClass = Direction switch
			{
				FabMenuDirection.Top => "rz-flex-column-reverse rz-fabmenu-direction-top",
				FabMenuDirection.Bottom => "rz-flex-column rz-fabmenu-direction-bottom",
				FabMenuDirection.Left => "rz-flex-row-reverse rz-fabmenu-direction-left",
				FabMenuDirection.Right => "rz-flex-row rz-fabmenu-direction-right",
				FabMenuDirection.Start => "rz-flex-row-reverse rz-fabmenu-direction-start",
				FabMenuDirection.End => "rz-flex-row rz-fabmenu-direction-end",
				_ => "rz-flex-column-reverse rz-fabmenu-direction-top"
			};
			return $"{baseClasses} {directionClass}";
		}

		/// <summary>
		/// Called after the component has rendered.
		/// </summary>
		/// <param name="firstRender">Set to <c>true</c> if this is the first time <see cref="ComponentBase.OnAfterRender(bool)"/> has been invoked on this component instance; otherwise <c>false</c>.</param>
		protected override async Task OnAfterRenderAsync(bool firstRender)
		{
			await base.OnAfterRenderAsync(firstRender);

			if (firstRender)
			{
				dotNetRef = DotNetObjectReference.Create(this);
			}

			if (IsOpen)
			{
				JSRuntime.InvokeVoid("Radzen.radzenFabMenu.registerOutsideClick", Element, dotNetRef);
			}
			else
			{
				JSRuntime.InvokeVoid("Radzen.radzenFabMenu.unregisterOutsideClick", Element);
			}
		}

		/// <summary>
		/// Handles the FAB button click event.
		/// </summary>
		/// <param name="args">The <see cref="MouseEventArgs"/> instance containing the event data.</param>
		public async Task OnFabClick(MouseEventArgs args)
		{
			await ToggleAsync();
		}

		/// <summary>
		/// Handles the toggle changed event.
		/// </summary>
		/// <param name="value">The new toggle value.</param>
		public async Task OnToggleChanged(bool value)
		{
			IsOpen = value;
			await IsOpenChanged.InvokeAsync(IsOpen);
			StateHasChanged();
		}

		/// <summary>
		/// Toggles the menu open/closed state.
		/// </summary>
		public async Task ToggleAsync()
		{
			IsOpen = !IsOpen;
			await IsOpenChanged.InvokeAsync(IsOpen);
			StateHasChanged();
		}

		/// <summary>
		/// Closes the menu.
		/// </summary>
		[JSInvokable]
		public async Task CloseAsync()
		{
			if (IsOpen)
			{
				IsOpen = false;
				await IsOpenChanged.InvokeAsync(IsOpen);
				StateHasChanged();
			}
		}

		/// <summary>
		/// Closes the menu from a menu item.
		/// </summary>
		internal async Task CloseFromItemAsync()
		{
			await CloseAsync();
		}

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		/// <returns>A task that represents the dispose operation.</returns>
		public ValueTask DisposeAsync()
		{
			try
			{
				if (IsOpen)
				{
					JSRuntime.InvokeVoid("Radzen.radzenFabMenu.unregisterOutsideClick", Element);
				}
			}
			catch { }
			finally
			{
				dotNetRef?.Dispose();
			}
			
			return ValueTask.CompletedTask;
		}
	}
} 