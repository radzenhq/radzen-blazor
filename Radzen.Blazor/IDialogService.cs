using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Radzen
{
    /// <summary>
    /// Interface IDialogService. Contains various methods with options to open and close dialogs. 
    /// Should be added as scoped service in the application services and RadzenDialog should be added in application main layout.
    /// </summary>
    /// <example>
    /// <code>
    /// @inject IDialogService DialogService
    /// &lt;RadzenButton Text="Show dialog with inline Blazor content" Click=@ShowInlineDialog /&gt;
    /// @code {
    ///  async Task ShowInlineDialog()
    ///  {
    ///    var result = await DialogService.OpenAsync("Simple Dialog", ds =&gt;
    ///      @&lt;div&gt;
    ///          &lt;p Style="margin-bottom: 1rem"&gt;Confirm?&lt;/p&gt;
    ///          &lt;div class="row"&gt;
    ///              &lt;div class="col-md-12"&gt;
    ///                  &lt;RadzenButton Text="Ok" Click="() =&gt; ds.Close(true)" Style="margin-bottom: 10px; width: 150px" /&gt;
    ///                  &lt;RadzenButton Text="Cancel" Click="() =&gt; ds.Close(false)" ButtonStyle="ButtonStyle.Secondary"  Style="margin-bottom: 10px; width: 150px"/&gt;
    ///                  &lt;RadzenButton Text="Refresh" Click="(() =&gt; { orderID = 10249; ds.Refresh(); })" ButtonStyle="ButtonStyle.Info"  Style="margin-bottom: 10px; width: 150px"/&gt;
    ///                  Order ID: @orderID
    ///              &lt;/div&gt;
    ///          &lt;/div&gt;
    ///      &lt;/div&gt;);
    ///    Console.WriteLine($"Dialog result: {result}");
    ///  }
    /// }
    /// </code>
    /// </example>
    public interface IDialogService
    {
        /// <summary>
        /// Raises the Close event.
        /// </summary>
        event Action<dynamic> OnClose;

        /// <summary>
        /// Occurs when a new dialog is open.
        /// </summary>
        event Action<string, Type, Dictionary<string, object>, DialogOptions> OnOpen;

        /// <summary>
        /// Occurs when [on refresh].
        /// </summary>
        event Action OnRefresh;

        /// <summary>
        /// Closes the last opened dialog with optional result.
        /// </summary>
        /// <param name="result">The result.</param>
        void Close(dynamic result = null);

        /// <summary>
        /// Displays a confirmation dialog.
        /// </summary>
        /// <param name="message">The message displayed to the user.</param>
        /// <param name="title">The text displayed in the title bar of the dialog.</param>
        /// <param name="options">The options.</param>
        /// <returns><c>true</c> if the user clicked the OK button, <c>false</c> otherwise.</returns>
        Task<bool?> Confirm(string message = "Confirm?", string title = "Confirm", ConfirmOptions options = null);

        /// <summary>
        /// Opens a dialog with the specified content.
        /// </summary>
        /// <param name="title">The text displayed in the title bar of the dialog.</param>
        /// <param name="childContent">The content displayed in the dialog.</param>
        /// <param name="options">The dialog options.</param>
        /// <returns>The value passed as argument to <see cref="Close" />.</returns>
        void Open(string title, RenderFragment<DialogService> childContent, DialogOptions options = null);

        /// <summary>
        /// Opens a dialog with the specified arguments.
        /// </summary>
        /// <typeparam name="T">The type of the Blazor component which will be displayed in a dialog.</typeparam>
        /// <param name="title">The text displayed in the title bar of the dialog.</param>
        /// <param name="parameters">The dialog parameters. Passed as property values of <typeparamref name="T" />.</param>
        /// <param name="options">The dialog options.</param>
        void Open<T>(string title, Dictionary<string, object> parameters = null, DialogOptions options = null) where T : ComponentBase;

        /// <summary>
        /// Opens a dialog with the specified content.
        /// </summary>
        /// <param name="title">The text displayed in the title bar of the dialog.</param>
        /// <param name="childContent">The content displayed in the dialog.</param>
        /// <param name="options">The dialog options.</param>
        /// <returns>The value passed as argument to <see cref="Close" />.</returns>
        Task<dynamic> OpenAsync(string title, RenderFragment<DialogService> childContent, DialogOptions options = null);

        /// <summary>
        /// Opens a dialog with the specified arguments.
        /// </summary>
        /// <typeparam name="T">The type of the Blazor component which will be displayed in a dialog.</typeparam>
        /// <param name="title">The text displayed in the title bar of the dialog.</param>
        /// <param name="parameters">The dialog parameters. Passed as property values of <typeparamref name="T" />.</param>
        /// <param name="options">The dialog options.</param>
        /// <returns>The value passed as argument to <see cref="Close" />.</returns>
        Task<dynamic> OpenAsync<T>(string title, Dictionary<string, object> parameters = null, DialogOptions options = null) where T : ComponentBase;

        /// <summary>
        /// Invokes <see cref="OnRefresh" />.
        /// </summary>
        void Refresh();
    }
}