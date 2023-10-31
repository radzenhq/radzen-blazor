using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Radzen
{
    /// <summary>
    /// Class DialogService. Contains various methods with options to open and close dialogs. 
    /// Should be added as scoped service in the application services and RadzenDialog should be added in application main layout.
    /// </summary>
    /// <example>
    /// <code>
    /// @inject DialogService DialogService
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
    public class DialogService : IDisposable
    {
        private DotNetObjectReference<DialogService> reference;
        internal DotNetObjectReference<DialogService> Reference
        {
            get
            {
                if (reference == null)
                {
                    reference = DotNetObjectReference.Create(this);
                }

                return reference;
            }
        }

        /// <summary>
        /// Gets or sets the URI helper.
        /// </summary>
        /// <value>The URI helper.</value>
        NavigationManager UriHelper { get; set; }
        IJSRuntime JSRuntime { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DialogService"/> class.
        /// </summary>
        /// <param name="uriHelper">The URI helper.</param>
        /// <param name="jsRuntime">IJSRuntime instance.</param>
        public DialogService(NavigationManager uriHelper, IJSRuntime jsRuntime)
        {
            UriHelper = uriHelper;
            JSRuntime = jsRuntime;

            if (UriHelper != null)
            {
                UriHelper.LocationChanged += UriHelper_OnLocationChanged;
            }
        }

        private void UriHelper_OnLocationChanged(object sender, Microsoft.AspNetCore.Components.Routing.LocationChangedEventArgs e)
        {
            while (dialogs.Any())
            {
                Close();
            }

            if (_sideDialogTask?.Task.IsCompleted == false)
            {
                CloseSide();
            }
        }

        /// <summary>
        /// Raises the Close event.
        /// </summary>
        public event Action<dynamic> OnClose;

        /// <summary>
        /// Occurs when [on refresh].
        /// </summary>
        public event Action OnRefresh;

        /// <summary>
        /// Occurs when a new dialog is open.
        /// </summary>
        public event Action<string, Type, Dictionary<string, object>, DialogOptions> OnOpen;

        /// <summary>
        /// Raises the Close event for the side dialog
        /// </summary>
        public event Action<dynamic> OnSideClose;

        /// <summary>
        /// Raises the Open event for the side dialog
        /// </summary>
        public event Action<Type, Dictionary<string, object>, SideDialogOptions> OnSideOpen;

        /// <summary>
        /// Opens a dialog with the specified arguments.
        /// </summary>
        /// <typeparam name="T">The type of the Blazor component which will be displayed in a dialog.</typeparam>
        /// <param name="title">The text displayed in the title bar of the dialog.</param>
        /// <param name="parameters">The dialog parameters. Passed as property values of <typeparamref name="T" />.</param>
        /// <param name="options">The dialog options.</param>
        public virtual void Open<T>(string title, Dictionary<string, object> parameters = null, DialogOptions options = null) where T : ComponentBase
        {
            OpenDialog<T>(title, parameters, options);
        }

        /// <summary>
        /// Invokes <see cref="OnRefresh" />.
        /// </summary>
        public void Refresh()
        {
            OnRefresh?.Invoke();
        }

        /// <summary>
        /// The tasks
        /// </summary>
        protected List<TaskCompletionSource<dynamic>> tasks = new List<TaskCompletionSource<dynamic>>();
        private TaskCompletionSource<dynamic> _sideDialogTask;

        /// <summary>
        /// Opens a dialog with the specified arguments.
        /// </summary>
        /// <typeparam name="T">The type of the Blazor component which will be displayed in a dialog.</typeparam>
        /// <param name="title">The text displayed in the title bar of the dialog.</param>
        /// <param name="parameters">The dialog parameters. Passed as property values of <typeparamref name="T" />.</param>
        /// <param name="options">The dialog options.</param>
        /// <returns>The value passed as argument to <see cref="Close" />.</returns>
        public virtual Task<dynamic> OpenAsync<T>(string title, Dictionary<string, object> parameters = null, DialogOptions options = null) where T : ComponentBase
        {
            var task = new TaskCompletionSource<dynamic>();
            tasks.Add(task);

            OpenDialog<T>(title, parameters, options);

            return task.Task;
        }

        /// <summary>
        /// Opens a side dialog with the specified arguments
        /// </summary>
        /// <typeparam name="T">The type of Blazor component which will be displayed in the side dialog.</typeparam>
        /// <param name="title">The text displayed in the title bar of the side dialog.</param>
        /// <param name="parameters">The dialog parameters. Passed as property values of <typeparamref name="T"/></param>
        /// <param name="options">The side dialog options.</param>
        /// <returns>A task that completes when the dialog is closed or a new one opened</returns>
        public Task<dynamic> OpenSideAsync<T>(string title, Dictionary<string, object> parameters = null, SideDialogOptions options = null)
            where T : ComponentBase
        {
            CloseSide();
            _sideDialogTask = new TaskCompletionSource<dynamic>();
            if (options == null)
            {
                options = new SideDialogOptions();
            }

            options.Title = title;
            OnSideOpen?.Invoke(typeof(T), parameters ?? new Dictionary<string, object>(), options);
            return _sideDialogTask.Task;
        }

        /// <summary>
        /// Closes the side dialog
        /// </summary>
        /// <param name="result">The result of the Dialog</param>
        public void CloseSide(dynamic result = null)
        {
            if (_sideDialogTask?.Task.IsCompleted == false)
            {
                _sideDialogTask.TrySetResult(result);
                OnSideClose?.Invoke(result);
            }
        }

        /// <summary>
        /// Opens a dialog with the specified content.
        /// </summary>
        /// <param name="title">The text displayed in the title bar of the dialog.</param>
        /// <param name="childContent">The content displayed in the dialog.</param>
        /// <param name="options">The dialog options.</param>
        /// <returns>The value passed as argument to <see cref="Close" />.</returns>
        public virtual Task<dynamic> OpenAsync(string title, RenderFragment<DialogService> childContent, DialogOptions options = null)
        {
            var task = new TaskCompletionSource<dynamic>();
            tasks.Add(task);

            options = options ?? new DialogOptions();

            options.ChildContent = childContent;

            OpenDialog<object>(title, null, options);

            return task.Task;
        }

        /// <summary>
        /// Opens a dialog with the specified content.
        /// </summary>
        /// <param name="title">The text displayed in the title bar of the dialog.</param>
        /// <param name="childContent">The content displayed in the dialog.</param>
        /// <param name="options">The dialog options.</param>
        public virtual void Open(string title, RenderFragment<DialogService> childContent, DialogOptions options = null)
        {
            options = options ?? new DialogOptions();

            options.ChildContent = childContent;

            OpenDialog<object>(title, null, options);
        }

        /// <summary>
        /// The dialogs
        /// </summary>
        protected List<object> dialogs = new List<object>();

        private void OpenDialog<T>(string title, Dictionary<string, object> parameters, DialogOptions options)
        {
            dialogs.Add(new object());
            OnOpen?.Invoke(title, typeof(T), parameters, new DialogOptions()
            {
                Width = options != null && !string.IsNullOrEmpty(options.Width) ? options.Width : "600px",
                Left = options != null && !string.IsNullOrEmpty(options.Left) ? options.Left : "",
                Top = options != null && !string.IsNullOrEmpty(options.Top) ? options.Top : "",
                Bottom = options != null && !string.IsNullOrEmpty(options.Bottom) ? options.Bottom : "",
                Height = options != null && !string.IsNullOrEmpty(options.Height) ? options.Height : "",
                ShowTitle = options != null ? options.ShowTitle : true,
                ShowClose = options != null ? options.ShowClose : true,
                Resizable = options != null ? options.Resizable : false,
                Draggable = options != null ? options.Draggable : false,
                ChildContent = options?.ChildContent,
                Style = options != null ? options.Style : "",
                AutoFocusFirstElement = options != null ? options.AutoFocusFirstElement : true,
                CloseDialogOnOverlayClick = options != null ? options.CloseDialogOnOverlayClick : false,
                CloseDialogOnEsc = options != null ? options.CloseDialogOnEsc : true,
                CssClass = options != null ? options.CssClass : "",
                WrapperCssClass = options != null ? options.WrapperCssClass : "",
                CloseTabIndex = options != null ? options.CloseTabIndex : 0,
            });
        }

        /// <summary>
        /// Closes the last opened dialog with optional result.
        /// </summary>
        /// <param name="result">The result.</param>
        [JSInvokable("DialogService.Close")]
        public virtual void Close(dynamic result = null)
        {
            var dialog = dialogs.LastOrDefault();

            if (dialog != null)
            {
                OnClose?.Invoke(result);
                dialogs.Remove(dialog);
            }

            var task = tasks.LastOrDefault();
            if (task != null && task.Task != null && !task.Task.IsCompleted)
            {
                tasks.Remove(task);
                task.SetResult(result);
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            reference?.Dispose();
            reference = null;

            UriHelper.LocationChanged -= UriHelper_OnLocationChanged;
        }

        /// <summary>
        /// Displays a confirmation dialog.
        /// </summary>
        /// <param name="message">The message displayed to the user.</param>
        /// <param name="title">The text displayed in the title bar of the dialog.</param>
        /// <param name="options">The options.</param>
        /// <returns><c>true</c> if the user clicked the OK button, <c>false</c> otherwise.</returns>
        public virtual async Task<bool?> Confirm(string message = "Confirm?", string title = "Confirm", ConfirmOptions options = null)
        {
            var dialogOptions = new DialogOptions()
            {
                Width = options != null ? !string.IsNullOrEmpty(options.Width) ? options.Width : "" : "",
                Height = options != null ? options.Height : null,
                Left = options != null ? options.Left : null,
                Top = options != null ? options.Top : null,
                Bottom = options != null ? options.Bottom : null,
                ChildContent = options != null ? options.ChildContent : null,
                ShowTitle = options != null ? options.ShowTitle : true,
                ShowClose = options != null ? options.ShowClose : true,
                Resizable = options != null ? options.Resizable : false,
                Draggable = options != null ? options.Draggable : false,
                Style = options != null ? options.Style : "",
                AutoFocusFirstElement = options != null ? options.AutoFocusFirstElement : true,
                CloseDialogOnOverlayClick = options != null ? options.CloseDialogOnOverlayClick : false,
                CloseDialogOnEsc = options != null ? options.CloseDialogOnEsc : true,
                CssClass = options != null ? $"rz-dialog-confirm {options.CssClass}" : "rz-dialog-confirm",
                WrapperCssClass = options != null ? $"rz-dialog-wrapper {options.WrapperCssClass}" : "rz-dialog-wrapper",
                CloseTabIndex = options != null ? options.CloseTabIndex : 0,
            };

            return await OpenAsync(title, ds =>
            {
                RenderFragment content = b =>
                {
                    var i = 0;
                    b.OpenElement(i++, "p");
                    b.AddAttribute(i++, "class", "rz-dialog-confirm-message");
                    b.AddContent(i++, (MarkupString)message);
                    b.CloseElement();

                    b.OpenElement(i++, "div");
                    b.AddAttribute(i++, "class", "rz-dialog-confirm-buttons");

                    b.OpenComponent<Blazor.RadzenButton>(i++);
                    b.AddAttribute(i++, "Text", options != null ? options.OkButtonText : "Ok");
                    b.AddAttribute(i++, "Click", EventCallback.Factory.Create<Microsoft.AspNetCore.Components.Web.MouseEventArgs>(this, () => ds.Close(true)));
                    b.CloseComponent();

                    b.OpenComponent<Blazor.RadzenButton>(i++);
                    b.AddAttribute(i++, "Text", options != null ? options.CancelButtonText : "Cancel");
                    b.AddAttribute(i++, "ButtonStyle", ButtonStyle.Secondary);
                    b.AddAttribute(i++, "Click", EventCallback.Factory.Create<Microsoft.AspNetCore.Components.Web.MouseEventArgs>(this, () => ds.Close(false)));
                    b.CloseComponent();

                    b.CloseElement();
                };
                return content;
            }, dialogOptions);
        }

        /// <summary>
        /// Displays a alert dialog.
        /// </summary>
        /// <param name="message">The message displayed to the user.</param>
        /// <param name="title">The text displayed in the title bar of the dialog.</param>
        /// <param name="options">The options.</param>
        /// <returns><c>true</c> if the user clicked the OK button, <c>false</c> otherwise.</returns>
        public virtual async Task<bool?> Alert(string message = "", string title = "Message", AlertOptions options = null)
        {
            var dialogOptions = new DialogOptions()
            {
                Width = options != null ? !string.IsNullOrEmpty(options.Width) ? options.Width : "" : "",
                Height = options != null ? options.Height : null,
                Left = options != null ? options.Left : null,
                Top = options != null ? options.Top : null,
                Bottom = options != null ? options.Bottom : null,
                ChildContent = options != null ? options.ChildContent : null,
                ShowTitle = options != null ? options.ShowTitle : true,
                ShowClose = options != null ? options.ShowClose : true,
                Resizable = options != null ? options.Resizable : false,
                Draggable = options != null ? options.Draggable : false,
                Style = options != null ? options.Style : "",
                AutoFocusFirstElement = options != null ? options.AutoFocusFirstElement : true,
                CloseDialogOnOverlayClick = options != null ? options.CloseDialogOnOverlayClick : false,
                CloseDialogOnEsc = options != null ? options.CloseDialogOnEsc : true,
                CssClass = options != null ? $"rz-dialog-alert {options.CssClass}" : "rz-dialog-alert",
                WrapperCssClass = options != null ? $"rz-dialog-wrapper {options.WrapperCssClass}" : "rz-dialog-wrapper",
                CloseTabIndex = options != null ? options.CloseTabIndex : 0,
            };

            return await OpenAsync(title, ds =>
            {
                RenderFragment content = b =>
                {
                    var i = 0;
                    b.OpenElement(i++, "p");
                    b.AddAttribute(i++, "class", "rz-dialog-alert-message");
                    b.AddContent(i++, (MarkupString)message);
                    b.CloseElement();

                    b.OpenElement(i++, "div");
                    b.AddAttribute(i++, "class", "rz-dialog-alert-buttons");

                    b.OpenComponent<Blazor.RadzenButton>(i++);
                    b.AddAttribute(i++, "Text", options != null ? options.OkButtonText : "Ok");
                    b.AddAttribute(i++, "Click", EventCallback.Factory.Create<Microsoft.AspNetCore.Components.Web.MouseEventArgs>(this, () => ds.Close(true)));
                    b.CloseComponent();

                    b.CloseElement();
                };
                return content;
            }, dialogOptions);
        }
    }

    /// <summary>
    /// Base Class for dialog options
    /// </summary>
    public abstract class DialogOptionsBase
    {
        /// <summary>
        /// Gets or sets a value indicating whether to show the title bar. Set to <c>true</c> by default.
        /// </summary>
        /// <value><c>true</c> if title bar is shown; otherwise, <c>false</c>.</value>
        public bool ShowTitle { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to show the close button. Set to <c>true</c> by default.
        /// </summary>
        /// <value><c>true</c> if the close button is shown; otherwise, <c>false</c>.</value>
        public bool ShowClose { get; set; } = true;
        /// <summary>
        /// Gets or sets the width of the dialog.
        /// </summary>
        /// <value>The width.</value>
        public string Width { get; set; }
        /// <summary>
        /// Gets or sets the height of the dialog.
        /// </summary>
        /// <value>The height.</value>
        public string Height { get; set; }
        /// <summary>
        /// Gets or sets the CSS style of the dialog
        /// </summary>
        /// <value>The style.</value>
        public string Style { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the dialog should be closed by clicking the overlay.
        /// </summary>
        /// <value><c>true</c> if closeable; otherwise, <c>false</c>.</value>
        public bool CloseDialogOnOverlayClick { get; set; } = false;

        /// <summary>
        /// Gets or sets dialog box custom class
        /// </summary>
        public string CssClass { get; set; }

        /// <summary>
        /// Gets or sets the CSS classes added to the dialog's wrapper element.
        /// </summary>
        public string WrapperCssClass { get; set; }
        
        /// <summary>
        /// Gets or sets a value the dialog escape tabindex. Set to <c>0</c> by default.
        /// </summary>
        public int CloseTabIndex { get; set; } = 0;
    }

    /// <summary>
    /// Class SideDialogOptions
    /// </summary>
    public class SideDialogOptions : DialogOptionsBase
    {
        /// <summary>
        /// The title displayed on the dialog.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// The Position on which the dialog will be positioned
        /// </summary>
        public DialogPosition Position { get; set; } = DialogPosition.Right;

        /// <summary>
        /// Whether to show a mask on the background or not
        /// </summary>
        public bool ShowMask { get; set; } = true;
    }

    /// <summary>
    /// DialogPosition enum
    /// </summary>
    public enum DialogPosition
    {
        /// <summary>
        /// Dialog will be positioned on the right side
        /// </summary>
        Right,
        /// <summary>
        /// Dialog will be positioned on the left side
        /// </summary>
        Left,
        /// <summary>
        /// Dialog will be positioned on the top of the page
        /// </summary>
        Top,
        /// <summary>
        /// Dialog will be positioned at the bottom of the page
        /// </summary>
        Bottom
    }

    /// <summary>
    /// Class DialogOptions.
    /// </summary>
    public class DialogOptions : DialogOptionsBase
    {
        /// <summary>
        /// Gets or sets a value indicating whether the dialog is resizable. Set to <c>false</c> by default.
        /// </summary>
        /// <value><c>true</c> if resizable; otherwise, <c>false</c>.</value>
        public bool Resizable { get; set; } = false;
        /// <summary>
        /// Gets or sets a value indicating whether the dialog is draggable. Set to <c>false</c> by default.
        /// </summary>
        /// <value><c>true</c> if draggable; otherwise, <c>false</c>.</value>
        public bool Draggable { get; set; } = false;
        /// <summary>
        /// Gets or sets the X coordinate of the dialog. Maps to the <c>left</c> CSS attribute.
        /// </summary>
        /// <value>The left.</value>
        public string Left { get; set; }
        /// <summary>
        /// Gets or sets the Y coordinate of the dialog. Maps to the <c>top</c> CSS attribute.
        /// </summary>
        /// <value>The top.</value>
        public string Top { get; set; }
        /// <summary>
        /// Specifies the <c>bottom</c> CSS attribute.
        /// </summary>
        /// <value>The bottom.</value>
        public string Bottom { get; set; }
        /// <summary>
        /// Gets or sets the child content.
        /// </summary>
        /// <value>The child content.</value>
        public RenderFragment<DialogService> ChildContent { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether to focus the first focusable HTML element. Set to <c>true</c> by default.
        /// </summary>
        public bool AutoFocusFirstElement { get; set; } = true;
        /// <summary>
        /// Gets or sets a value indicating whether the dialog should be closed on ESC key press.
        /// </summary>
        /// <value><c>true</c> if closeable; otherwise, <c>false</c>.</value>
        public bool CloseDialogOnEsc { get; set; } = true;
    }

    /// <summary>
    /// Class ConfirmOptions.
    /// </summary>
    public class AlertOptions : DialogOptions
    {
        /// <summary>
        /// Gets or sets the text of the OK button.
        /// </summary>
        public string OkButtonText { get; set; }
    }

    /// <summary>
    /// Class ConfirmOptions.
    /// </summary>
    public class ConfirmOptions : AlertOptions
    {
        /// <summary>
        /// Gets or sets the text of the Cancel button.
        /// </summary>
        public string CancelButtonText { get; set; }
    }

    /// <summary>
    /// Class Dialog.
    /// </summary>
    public class Dialog
    {
        /// <summary>
        /// Gets or sets the title.
        /// </summary>
        /// <value>The title.</value>
        public string Title { get; set; }
        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        /// <value>The type.</value>
        public Type Type { get; set; }
        /// <summary>
        /// Gets or sets the parameters.
        /// </summary>
        /// <value>The parameters.</value>
        public Dictionary<string, object> Parameters { get; set; }
        /// <summary>
        /// Gets or sets the options.
        /// </summary>
        /// <value>The options.</value>
        public DialogOptions Options { get; set; }
    }
}
