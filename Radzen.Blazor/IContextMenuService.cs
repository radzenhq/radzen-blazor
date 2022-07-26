using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System;
using System.Collections.Generic;

namespace Radzen
{
    /// <summary>
    /// Interface IContextMenuService. Contains various methods with options to open and close context menus. 
    /// Should be added as scoped service in the application services and RadzenContextMenu should be added in application main layout.
    /// Implements the <see cref="IDisposable" />
    /// </summary>
    /// <seealso cref="IDisposable" />
    /// <example>
    /// <code>
    /// @inject IContextMenuService ContextMenuService
    /// &lt;RadzenButton Text="Show context menu" ContextMenu=@(args => ShowContextMenuWithContent(args)) /&gt;
    /// @code {
    ///     void ShowContextMenuWithContent(MouseEventArgs args) =&gt; ContextMenuService.Open(args, ds =&gt;
    ///       @&lt;RadzenMenu Click="OnMenuItemClick"&gt;
    ///         &lt;RadzenMenuItem Text="Item1" Value="1"&gt;&lt;/RadzenMenuItem&gt;
    ///         &lt;RadzenMenuItem Text="Item2" Value="2"&gt;&lt;/RadzenMenuItem&gt;
    ///         &lt;RadzenMenuItem Text="More items" Value="3"&gt;
    ///             &lt;RadzenMenuItem Text="More sub items" Value="4"&gt;
    ///                 &lt;RadzenMenuItem Text="Item1" Value="5"&gt;&lt;/RadzenMenuItem&gt;
    ///                 &lt;RadzenMenuItem Text="Item2" Value="6"&gt;&lt;/RadzenMenuItem&gt;
    ///             &lt;/RadzenMenuItem&gt;
    ///         &lt;/RadzenMenuItem&gt;
    ///       &lt;/RadzenMenu&gt;);
    /// 
    ///     void OnMenuItemClick(MenuItemEventArgs args)
    ///     {
    ///       Console.WriteLine($"Menu item with Value={args.Value} clicked");
    ///     }
    /// }
    /// </code>
    /// </example>
    public interface IContextMenuService
    {
        /// <summary>
        /// Raises the Close event.
        /// </summary>
        event Action OnClose;

        /// <summary>
        /// Occurs when [on navigate].
        /// </summary>
        event Action OnNavigate;

        /// <summary>
        /// Occurs when [on open].
        /// </summary>
        event Action<MouseEventArgs, ContextMenuOptions> OnOpen;

        /// <summary>
        /// Closes this instance.
        /// </summary>
        void Close();

        /// <summary>
        /// Opens the specified arguments.
        /// </summary>
        /// <param name="args">The <see cref="MouseEventArgs"/> instance containing the event data.</param>
        /// <param name="items">The items.</param>
        /// <param name="click">The click.</param>
        void Open(MouseEventArgs args, IEnumerable<ContextMenuItem> items, Action<MenuItemEventArgs> click = null);

        /// <summary>
        /// Opens the specified arguments.
        /// </summary>
        /// <param name="args">The <see cref="MouseEventArgs"/> instance containing the event data.</param>
        /// <param name="childContent">Content of the child.</param>
        void Open(MouseEventArgs args, RenderFragment<ContextMenuService> childContent);
    }
}