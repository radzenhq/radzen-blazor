using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Threading;
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
    ///                  &lt;RadzenButton Text="Cancel" Click="() =&gt; ds.Close(false)" ButtonStyle="ButtonStyle.Base"  Style="margin-bottom: 10px; width: 150px"/&gt;
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

            if (sideDialogResultTask?.Task.IsCompleted == false)
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
        /// <param name="parameters">The dialog parameters.</param>
        /// <param name="options">The dialog options.</param>
        public virtual void Open<T>(string title, Dictionary<string, object> parameters = null, DialogOptions options = null) where T : ComponentBase
        {
            OpenDialog<T>(title, parameters, options);
        }

        /// <summary>
        /// Opens a dialog with the specified arguments.
        /// </summary>
        /// <param name="title">The text displayed in the title bar of the dialog.</param>
        /// <param name="componentType">The type of the component to be displayed in the dialog. Must inherit from <see cref="ComponentBase"/>.</param>
        /// <param name="parameters">The dialog parameters.</param>
        /// <param name="options">The dialog options.</param>
        public virtual void Open(string title, Type componentType, Dictionary<string, object> parameters = null, DialogOptions options = null)
        {
            if (!typeof(ComponentBase).IsAssignableFrom(componentType))
            {
                throw new ArgumentException("The component type must be a subclass of ComponentBase.", nameof(componentType));
            }

            var method = GetType().GetMethod(nameof(OpenDialog), BindingFlags.Instance | BindingFlags.NonPublic);

            method.MakeGenericMethod(componentType).Invoke(this, new object[] { title, parameters, options });
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
        private TaskCompletionSource<dynamic> sideDialogResultTask;

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
        /// Opens a dialog with the specified arguments dynamically.
        /// </summary>
        /// <param name="title">The text displayed in the title bar of the dialog.</param>
        /// <param name="componentType">The type of the Blazor component to be displayed in a dialog. Must inherit from <see cref="ComponentBase"/>.</param>
        /// <param name="parameters">The dialog parameters, passed as property values of the specified component.</param>
        /// <param name="options">The dialog options.</param>
        /// <returns>A task that represents the result passed as an argument to <see cref="Close"/>.</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="componentType"/> does not inherit from <see cref="ComponentBase"/>.</exception>
        public virtual Task<dynamic> OpenAsync(string title, Type componentType, Dictionary<string, object> parameters = null, DialogOptions options = null)
        {
            if (!typeof(ComponentBase).IsAssignableFrom(componentType))
            {
                throw new ArgumentException("The component type must be a subclass of ComponentBase.", nameof(componentType));
            }

            var task = new TaskCompletionSource<dynamic>();
            tasks.Add(task);

            var method = GetType().GetMethod(nameof(OpenDialog), BindingFlags.Instance | BindingFlags.NonPublic);

            method.MakeGenericMethod(componentType).Invoke(this, new object[] { title, parameters, options });

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
            sideDialogResultTask = new TaskCompletionSource<dynamic>();
            if (options == null)
            {
                options = new SideDialogOptions();
            }

            options.Title = title;
            OnSideOpen?.Invoke(typeof(T), parameters ?? new Dictionary<string, object>(), options);
            return sideDialogResultTask.Task;
        }

        /// <summary>
        /// Opens a side dialog with the specified arguments dynamically.
        /// </summary>
        /// <param name="title">The text displayed in the title bar of the side dialog.</param>
        /// <param name="componentType">The type of the Blazor component to be displayed in the side dialog. Must inherit from <see cref="ComponentBase"/>.</param>
        /// <param name="parameters">The dialog parameters, passed as property values of the specified component.</param>
        /// <param name="options">The side dialog options.</param>
        /// <returns>A task that represents the result passed as an argument to <see cref="CloseSide"/>.</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="componentType"/> does not inherit from <see cref="ComponentBase"/>.</exception>
        public Task<dynamic> OpenSideAsync(string title, Type componentType, Dictionary<string, object> parameters = null, SideDialogOptions options = null)
        {
            if (!typeof(ComponentBase).IsAssignableFrom(componentType))
            {
                throw new ArgumentException("The component type must be a subclass of ComponentBase.", nameof(componentType));
            }

            CloseSide();
            sideDialogResultTask = new TaskCompletionSource<dynamic>();

            if (options == null)
            {
                options = new SideDialogOptions();
            }

            options.Title = title;
            OnSideOpen?.Invoke(componentType, parameters ?? new Dictionary<string, object>(), options);

            return sideDialogResultTask.Task;
        }


        /// <summary>
        /// Opens a side dialog with the specified arguments
        /// </summary>
        /// <typeparam name="T">The type of Blazor component which will be displayed in the side dialog.</typeparam>
        /// <param name="title">The text displayed in the title bar of the side dialog.</param>
        /// <param name="parameters">The dialog parameters. Passed as property values of <typeparamref name="T"/></param>
        /// <param name="options">The side dialog options.</param>
        public void OpenSide<T>(string title, Dictionary<string, object> parameters = null, SideDialogOptions options = null)
            where T : ComponentBase
        {
            CloseSide();

            if (options == null)
            {
                options = new SideDialogOptions();
            }

            options.Title = title;
            OnSideOpen?.Invoke(typeof(T), parameters ?? new Dictionary<string, object>(), options);
        }

        /// <summary>
        /// Opens a side dialog with the specified arguments dynamically.
        /// </summary>
        /// <param name="title">The text displayed in the title bar of the side dialog.</param>
        /// <param name="componentType">The type of the Blazor component to be displayed in the side dialog. Must inherit from <see cref="ComponentBase"/>.</param>
        /// <param name="parameters">The dialog parameters, passed as property values of the specified component.</param>
        /// <param name="options">The side dialog options.</param>
        /// <exception cref="ArgumentException">Thrown if <paramref name="componentType"/> does not inherit from <see cref="ComponentBase"/>.</exception>
        public void OpenSide(string title, Type componentType, Dictionary<string, object> parameters = null, SideDialogOptions options = null)
        {
            if (!typeof(ComponentBase).IsAssignableFrom(componentType))
            {
                throw new ArgumentException("The component type must be a subclass of ComponentBase.", nameof(componentType));
            }

            CloseSide();

            if (options == null)
            {
                options = new SideDialogOptions();
            }

            options.Title = title;
            OnSideOpen?.Invoke(componentType, parameters ?? new Dictionary<string, object>(), options);
        }


        /// <summary>
        /// Closes the side dialog
        /// </summary>
        /// <param name="result">The result of the Dialog</param>
        public virtual void CloseSide(dynamic result = null)
        {
            if (sideDialogResultTask?.Task.IsCompleted == false)
            {
                sideDialogResultTask.TrySetResult(result);
            }

            OnSideClose?.Invoke(result);
        }

        private TaskCompletionSource sideDialogCloseTask;

        internal void OnSideCloseComplete()
        {
            sideDialogCloseTask?.TrySetResult();
            sideDialogCloseTask = null;
        }

        /// <summary>
        /// Closes the side dialog and waits for the closing animation to finish.
        /// </summary>
        /// <param name="result">The result of the Dialog</param>
        public async Task CloseSideAsync(dynamic result = null)
        {
            sideDialogCloseTask = new TaskCompletionSource();

            CloseSide(result);

            await sideDialogCloseTask.Task;
        }

        /// <summary>
        /// Opens a dialog with the specified content.
        /// </summary>
        /// <param name="title">The text displayed in the title bar of the dialog.</param>
        /// <param name="childContent">The content displayed in the dialog.</param>
        /// <param name="options">The dialog options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The value passed as argument to <see cref="Close" />.</returns>
        public virtual Task<dynamic> OpenAsync(string title, RenderFragment<DialogService> childContent, DialogOptions options = null, CancellationToken? cancellationToken = null)
        {
            var task = new TaskCompletionSource<dynamic>();

            // register the cancellation token
            if (cancellationToken.HasValue)
                cancellationToken.Value.Register(() => task.TrySetCanceled());

            tasks.Add(task);

            options ??= new DialogOptions();
            options.ChildContent = childContent;

            OpenDialog<object>(title, null, options);

            return task.Task;
        }

        /// <summary>
        /// Opens a dialog with the specified content.
        /// </summary>
        /// <param name="titleContent">The content displayed in the title bar of the dialog.</param>
        /// <param name="childContent">The content displayed in the dialog.</param>
        /// <param name="options">The dialog options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The value passed as argument to <see cref="Close" />.</returns>
        public virtual Task<dynamic> OpenAsync(RenderFragment<DialogService> titleContent, RenderFragment<DialogService> childContent, DialogOptions options = null, CancellationToken? cancellationToken = null)
        {
            var task = new TaskCompletionSource<dynamic>();

            // register the cancellation token
            if (cancellationToken.HasValue)
                cancellationToken.Value.Register(() => task.TrySetCanceled());

            tasks.Add(task);

            options ??= new DialogOptions();
            options.ChildContent = childContent;
            options.TitleContent = titleContent;

            OpenDialog<object>(null, null, options);

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

        internal void OpenDialog<T>(string title, Dictionary<string, object> parameters, DialogOptions options)
        {
            dialogs.Add(new object());

            // Validate and set default values for the dialog options
            options ??= new();
            options.Width = !String.IsNullOrEmpty(options.Width) ? options.Width : "600px";
            options.Left = !String.IsNullOrEmpty(options.Left) ? options.Left : "";
            options.Top = !String.IsNullOrEmpty(options.Top) ? options.Top : "";
            options.Bottom = !String.IsNullOrEmpty(options.Bottom) ? options.Bottom : "";
            options.Height = !String.IsNullOrEmpty(options.Height) ? options.Height : "";
            options.Style = !String.IsNullOrEmpty(options.Style) ? options.Style : "";
            options.CssClass = !String.IsNullOrEmpty(options.CssClass) ? options.CssClass : "";
            options.WrapperCssClass = !String.IsNullOrEmpty(options.WrapperCssClass) ? options.WrapperCssClass : "";
            options.ContentCssClass = !String.IsNullOrEmpty(options.ContentCssClass) ? options.ContentCssClass : "";

            OnOpen?.Invoke(title, typeof(T), parameters, options);
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
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns><c>true</c> if the user clicked the OK button, <c>false</c> otherwise.</returns>
        public virtual async Task<bool?> Confirm(string message = "Confirm?", string title = "Confirm", ConfirmOptions options = null, CancellationToken? cancellationToken = null)
        {
            // Validate and set default values for the dialog options
            options ??= new();
            options.OkButtonText = !String.IsNullOrEmpty(options.OkButtonText) ? options.OkButtonText : "Ok";
            options.CancelButtonText = !String.IsNullOrEmpty(options.CancelButtonText) ? options.CancelButtonText : "Cancel";
            options.Width = !String.IsNullOrEmpty(options.Width) ? options.Width : ""; // Width is set to 600px by default by OpenAsync
            options.Style = !String.IsNullOrEmpty(options.Style) ? options.Style : "";
            options.CssClass = !String.IsNullOrEmpty(options.CssClass) ? $"rz-dialog-confirm {options.CssClass}" : "rz-dialog-confirm";
            options.WrapperCssClass = !String.IsNullOrEmpty(options.WrapperCssClass) ? $"rz-dialog-wrapper {options.WrapperCssClass}" : "rz-dialog-wrapper";

            return await OpenAsync(title, ds =>
            {
                RenderFragment content = b =>
                {
                    var i = 0;
                    b.OpenElement(i++, "p");
                    b.AddAttribute(i++, "class", "rz-dialog-confirm-message");
                    b.AddContent(i++, message);
                    b.CloseElement();

                    b.OpenElement(i++, "div");
                    b.AddAttribute(i++, "class", "rz-dialog-confirm-buttons");

                    b.OpenComponent<Blazor.RadzenButton>(i++);
                    b.AddAttribute(i++, "Text", options.OkButtonText);
                    b.AddAttribute(i++, "Click", EventCallback.Factory.Create<Microsoft.AspNetCore.Components.Web.MouseEventArgs>(this, () => ds.Close(true)));
                    b.CloseComponent();

                    b.OpenComponent<Blazor.RadzenButton>(i++);
                    b.AddAttribute(i++, "Text", options.CancelButtonText);
                    b.AddAttribute(i++, "ButtonStyle", ButtonStyle.Base);
                    b.AddAttribute(i++, "Click", EventCallback.Factory.Create<Microsoft.AspNetCore.Components.Web.MouseEventArgs>(this, () => ds.Close(false)));
                    b.CloseComponent();

                    b.CloseElement();
                };
                return content;
            }, options, cancellationToken);
        }

        /// <summary>
        /// Displays a confirmation dialog.
        /// </summary>
        /// <param name="message">The message displayed to the user.</param>
        /// <param name="title">The text displayed in the title bar of the dialog.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns><c>true</c> if the user clicked the OK button, <c>false</c> otherwise.</returns>
        public virtual async Task<bool?> Confirm(RenderFragment message, string title = "Confirm", ConfirmOptions options = null, CancellationToken? cancellationToken = null)
        {
            // Validate and set default values for the dialog options
            options ??= new();
            options.OkButtonText = !String.IsNullOrEmpty(options.OkButtonText) ? options.OkButtonText : "Ok";
            options.CancelButtonText = !String.IsNullOrEmpty(options.CancelButtonText) ? options.CancelButtonText : "Cancel";
            options.Width = !String.IsNullOrEmpty(options.Width) ? options.Width : ""; // Width is set to 600px by default by OpenAsync
            options.Style = !String.IsNullOrEmpty(options.Style) ? options.Style : "";
            options.CssClass = !String.IsNullOrEmpty(options.CssClass) ? $"rz-dialog-confirm {options.CssClass}" : "rz-dialog-confirm";
            options.WrapperCssClass = !String.IsNullOrEmpty(options.WrapperCssClass) ? $"rz-dialog-wrapper {options.WrapperCssClass}" : "rz-dialog-wrapper";

            return await OpenAsync(title, ds =>
            {
                RenderFragment content = b =>
                {
                    var i = 0;
                    b.OpenElement(i++, "p");
                    b.AddAttribute(i++, "class", "rz-dialog-confirm-message");
                    b.AddContent(i++, message);
                    b.CloseElement();

                    b.OpenElement(i++, "div");
                    b.AddAttribute(i++, "class", "rz-dialog-confirm-buttons");

                    b.OpenComponent<Blazor.RadzenButton>(i++);
                    b.AddAttribute(i++, "Text", options.OkButtonText);
                    b.AddAttribute(i++, "Click", EventCallback.Factory.Create<Microsoft.AspNetCore.Components.Web.MouseEventArgs>(this, () => ds.Close(true)));
                    b.CloseComponent();

                    b.OpenComponent<Blazor.RadzenButton>(i++);
                    b.AddAttribute(i++, "Text", options.CancelButtonText);
                    b.AddAttribute(i++, "ButtonStyle", ButtonStyle.Base);
                    b.AddAttribute(i++, "Click", EventCallback.Factory.Create<Microsoft.AspNetCore.Components.Web.MouseEventArgs>(this, () => ds.Close(false)));
                    b.CloseComponent();

                    b.CloseElement();
                };
                return content;
            }, options, cancellationToken);
        }

        /// <summary>
        /// Displays a alert dialog.
        /// </summary>
        /// <param name="message">The message displayed to the user.</param>
        /// <param name="title">The text displayed in the title bar of the dialog.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns><c>true</c> if the user clicked the OK button, <c>false</c> otherwise.</returns>
        public virtual async Task<bool?> Alert(string message = "", string title = "Message", AlertOptions options = null, CancellationToken? cancellationToken = null)
        {
            // Validate and set default values for the dialog options
            options ??= new();
            options.OkButtonText = !String.IsNullOrEmpty(options.OkButtonText) ? options.OkButtonText : "Ok";
            options.Width = !String.IsNullOrEmpty(options.Width) ? options.Width : "";
            options.Style = !String.IsNullOrEmpty(options.Style) ? options.Style : "";
            options.CssClass = !String.IsNullOrEmpty(options.CssClass) ? $"rz-dialog-alert {options.CssClass}" : "rz-dialog-alert";
            options.WrapperCssClass = !String.IsNullOrEmpty(options.WrapperCssClass) ? $"rz-dialog-wrapper {options.WrapperCssClass}" : "rz-dialog-wrapper";
            options.ContentCssClass = !String.IsNullOrEmpty(options.ContentCssClass) ? $"rz-dialog-content {options.ContentCssClass}" : "rz-dialog-content";

            return await OpenAsync(title, ds =>
            {
                RenderFragment content = b =>
                {
                    var i = 0;
                    b.OpenElement(i++, "p");
                    b.AddAttribute(i++, "class", "rz-dialog-alert-message");
                    b.AddContent(i++, message);
                    b.CloseElement();

                    b.OpenElement(i++, "div");
                    b.AddAttribute(i++, "class", "rz-dialog-alert-buttons");

                    b.OpenComponent<Blazor.RadzenButton>(i++);
                    b.AddAttribute(i++, "Text", options.OkButtonText);
                    b.AddAttribute(i++, "Click", EventCallback.Factory.Create<Microsoft.AspNetCore.Components.Web.MouseEventArgs>(this, () => ds.Close(true)));
                    b.CloseComponent();

                    b.CloseElement();
                };
                return content;
            }, options, cancellationToken);
        }

        /// <summary>
        /// Displays a alert dialog.
        /// </summary>
        /// <param name="message">The message displayed to the user.</param>
        /// <param name="title">The text displayed in the title bar of the dialog.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns><c>true</c> if the user clicked the OK button, <c>false</c> otherwise.</returns>
        public virtual async Task<bool?> Alert(RenderFragment message, string title = "Message", AlertOptions options = null, CancellationToken? cancellationToken = null)
        {
            // Validate and set default values for the dialog options
            options ??= new();
            options.OkButtonText = !String.IsNullOrEmpty(options.OkButtonText) ? options.OkButtonText : "Ok";
            options.Width = !String.IsNullOrEmpty(options.Width) ? options.Width : "";
            options.Style = !String.IsNullOrEmpty(options.Style) ? options.Style : "";
            options.CssClass = !String.IsNullOrEmpty(options.CssClass) ? $"rz-dialog-alert {options.CssClass}" : "rz-dialog-alert";
            options.WrapperCssClass = !String.IsNullOrEmpty(options.WrapperCssClass) ? $"rz-dialog-wrapper {options.WrapperCssClass}" : "rz-dialog-wrapper";
            options.ContentCssClass = !String.IsNullOrEmpty(options.ContentCssClass) ? $"rz-dialog-content {options.ContentCssClass}" : "rz-dialog-content";

            return await OpenAsync(title, ds =>
            {
                RenderFragment content = b =>
                {
                    var i = 0;
                    b.OpenElement(i++, "p");
                    b.AddAttribute(i++, "class", "rz-dialog-alert-message");
                    b.AddContent(i++, message);
                    b.CloseElement();

                    b.OpenElement(i++, "div");
                    b.AddAttribute(i++, "class", "rz-dialog-alert-buttons");

                    b.OpenComponent<Blazor.RadzenButton>(i++);
                    b.AddAttribute(i++, "Text", options.OkButtonText);
                    b.AddAttribute(i++, "Click", EventCallback.Factory.Create<Microsoft.AspNetCore.Components.Web.MouseEventArgs>(this, () => ds.Close(true)));
                    b.CloseComponent();

                    b.CloseElement();
                };
                return content;
            }, options, cancellationToken);
        }
    }

    /// <summary>
    /// Base Class for dialog options
    /// </summary>
    public abstract class DialogOptionsBase : INotifyPropertyChanged
    {
        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Raises the <see cref="PropertyChanged" /> event.
        /// </summary>
        /// <param name="propertyName">The name of the property that changed.</param>
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private bool showTitle = true;
        /// <summary>
        /// Gets or sets a value indicating whether to show the title bar. Set to <c>true</c> by default.
        /// </summary>
        /// <value><c>true</c> if title bar is shown; otherwise, <c>false</c>. Default is <c>true</c>.</value>
        public bool ShowTitle
        {
            get => showTitle;
            set
            {
                if (showTitle != value)
                {
                    showTitle = value;
                    OnPropertyChanged(nameof(ShowTitle));
                }
            }
        }

        private bool showClose = true;
        /// <summary>
        /// Gets or sets a value indicating whether to show the close button. Set to <c>true</c> by default.
        /// </summary>
        /// <value><c>true</c> if the close button is shown; otherwise, <c>false</c>. Default is <c>true</c>.</value>
        public bool ShowClose
        {
            get => showClose;
            set
            {
                if (showClose != value)
                {
                    showClose = value;
                    OnPropertyChanged(nameof(ShowClose));
                }
            }
        }

        private string width;
        /// <summary>
        /// Gets or sets the width of the dialog.
        /// </summary>
        /// <value>The width.</value>
        public string Width
        {
            get => width;
            set
            {
                if (width != value)
                {
                    width = value;
                    OnPropertyChanged(nameof(Width));
                }
            }
        }

        private string height;
        /// <summary>
        /// Gets or sets the height of the dialog.
        /// </summary>
        /// <value>The height.</value>
        public string Height
        {
            get => height;
            set
            {
                if (height != value)
                {
                    height = value;
                    OnPropertyChanged(nameof(Height));
                }
            }
        }

        private string style;
        /// <summary>
        /// Gets or sets the CSS style of the dialog
        /// </summary>
        /// <value>The style.</value>
        public string Style
        {
            get => style;
            set
            {
                if (style != value)
                {
                    style = value;
                    OnPropertyChanged(nameof(Style));
                }
            }
        }

        private bool closeDialogOnOverlayClick = false;
        /// <summary>
        /// Gets or sets a value indicating whether the dialog should be closed by clicking the overlay.
        /// </summary>
        /// <value><c>true</c> if closeable; otherwise, <c>false</c>.</value>
        public bool CloseDialogOnOverlayClick
        {
            get => closeDialogOnOverlayClick;
            set
            {
                if (closeDialogOnOverlayClick != value)
                {
                    closeDialogOnOverlayClick = value;
                    OnPropertyChanged(nameof(CloseDialogOnOverlayClick));
                }
            }
        }

        private string cssClass;
        /// <summary>
        /// Gets or sets dialog box custom class
        /// </summary>
        public string CssClass
        {
            get => cssClass;
            set
            {
                if (cssClass != value)
                {
                    cssClass = value;
                    OnPropertyChanged(nameof(CssClass));
                }
            }
        }

        private string wrapperCssClass;
        /// <summary>
        /// Gets or sets the CSS classes added to the dialog's wrapper element.
        /// </summary>
        public string WrapperCssClass
        {
            get => wrapperCssClass;
            set
            {
                if (wrapperCssClass != value)
                {
                    wrapperCssClass = value;
                    OnPropertyChanged(nameof(WrapperCssClass));
                }
            }
        }

        private string contentCssClass;
        /// <summary>
        /// Gets or sets the CSS classes added to the dialog's content element.
        /// </summary>
        public string ContentCssClass
        {
            get => contentCssClass;
            set
            {
                if (contentCssClass != value)
                {
                    contentCssClass = value;
                    OnPropertyChanged(nameof(ContentCssClass));
                }
            }
        }

        private int closeTabIndex = 0;
        /// <summary>
        /// Gets or sets a value the dialog escape tabindex. Set to <c>0</c> by default.
        /// </summary>
        public int CloseTabIndex
        {
            get => closeTabIndex;
            set
            {
                if (closeTabIndex != value)
                {
                    closeTabIndex = value;
                    OnPropertyChanged(nameof(CloseTabIndex));
                }
            }
        }

        private RenderFragment<DialogService> titleContent;
        private bool resizable;

        /// <summary>
        /// Gets or sets the title content.
        /// </summary>
        /// <value>The title content.</value>
        public RenderFragment<DialogService> TitleContent
        {
            get => titleContent;
            set
            {
                if (titleContent != value)
                {
                    titleContent = value;
                    OnPropertyChanged(nameof(TitleContent));
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the dialog is resizable. Set to <c>false</c> by default.
        /// </summary>
        /// <value><c>true</c> if resizable; otherwise, <c>false</c>.</value>
        public bool Resizable
        {
            get => resizable;
            set
            {
                if (resizable != value)
                {
                    resizable = value;
                    OnPropertyChanged(nameof(Resizable));
                }
            }
        }
    }

    /// <summary>
    /// Class SideDialogOptions
    /// </summary>
    public class SideDialogOptions : DialogOptionsBase
    {
        private string title;

        /// <summary>
        /// The title displayed on the dialog.
        /// </summary>
        public string Title
        {
            get => title;
            set
            {
                if (title != value)
                {
                    title = value;
                    OnPropertyChanged(nameof(Title));
                }
            }
        }

        private DialogPosition position = DialogPosition.Right;

        /// <summary>
        /// The Position on which the dialog will be positioned
        /// </summary>
        public DialogPosition Position
        {
            get => position;
            set
            {
                if (position != value)
                {
                    position = value;
                    OnPropertyChanged(nameof(Position));
                }
            }
        }

        private bool showMask = true;


        /// <summary>
        /// Whether to show a mask on the background or not. Set to <c>true</c> by default.
        /// </summary>
        public bool ShowMask
        {
            get => showMask;
            set
            {
                if (showMask != value)
                {
                    showMask = value;
                    OnPropertyChanged(nameof(ShowMask));
                }
            }
        }

        private bool autoFocusFirstElement = false;

        /// <summary>
        /// Gets or sets a value indicating whether to focus the first focusable HTML element. Set to <c>true</c> by default.
        /// </summary>
        public bool AutoFocusFirstElement
        {
            get => autoFocusFirstElement;
            set
            {
                if (autoFocusFirstElement != value)
                {
                    autoFocusFirstElement = value;
                    OnPropertyChanged(nameof(AutoFocusFirstElement));
                }
            }
        }

        private double minWidth = 300.0;

        /// <summary>
        /// Gets or sets the minimum width (in pixels) enforced while resizing the side dialog.
        /// </summary>
        public double MinWidth
        {
            get => minWidth;
            set
            {
                if (Equals(value, minWidth)) return;
                minWidth = value;
                OnPropertyChanged(nameof(MinWidth));
            }
        }

        private double minHeight = 200.0;

        /// <summary>
        /// Gets or sets the minimum height (in pixels) enforced while resizing the side dialog.
        /// </summary>
        public double MinHeight
        {
            get => minHeight;
            set
            {
                if (Equals(value, minHeight)) return;
                minHeight = value;
                OnPropertyChanged(nameof(MinHeight));
            }
        }

        private string resizeBarTitle = "Drag to resize";

        /// <summary>
        /// Gets or sets the title of the resize bar.
        /// </summary>
        public string ResizeBarTitle
        {
            get => resizeBarTitle;
            set
            {
                if (value == resizeBarTitle) return;
                resizeBarTitle = value;
                OnPropertyChanged(nameof(ResizeBarTitle));
            }
        }

        private string resizeBarAriaLabel = "Resize side dialog";

        /// <summary>
        /// Gets or sets the aria label of the resize bar.
        /// </summary>
        public string ResizeBarAriaLabel
        {
            get => resizeBarAriaLabel;
            set
            {
                if (value == resizeBarAriaLabel) return;
                resizeBarAriaLabel = value;
                OnPropertyChanged(nameof(ResizeBarAriaLabel));
            }
        }
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
        /// Gets or sets the icon in Title.
        /// </summary>
        /// <value>The icon.</value>

        public string Icon { get; set; }
        /// <summary>
        /// Gets or sets the icon color in Title.
        /// </summary>
        /// <value>The icon color.</value>

        public string IconColor { get; set; }

        /// <summary>
        /// Gets or sets the CSS style of the Icon in Title.
        /// </summary>
        public string IconStyle { get; set; } = "margin-right: 0.75rem";

        private Action<Size> resize;

        /// <summary>
        /// Gets or sets the change.
        /// </summary>
        /// <value>The change.</value>
        public Action<Size> Resize
        {
            get => resize;
            set
            {
                if (resize != value)
                {
                    resize = value;
                    OnPropertyChanged(nameof(Resize));
                }
            }
        }

        private bool draggable;

        /// <summary>
        /// Gets or sets a value indicating whether the dialog is draggable. Set to <c>false</c> by default.
        /// </summary>
        /// <value><c>true</c> if draggable; otherwise, <c>false</c>.</value>
        public bool Draggable
        {
            get => draggable;
            set
            {
                if (draggable != value)
                {
                    draggable = value;
                    OnPropertyChanged(nameof(Draggable));
                }
            }
        }

        private Action<Point> drag;

        /// <summary>
        /// Gets or sets the change.
        /// </summary>
        /// <value>The change.</value>
        public Action<Point> Drag
        {
            get => drag;
            set
            {
                if (drag != value)
                {
                    drag = value;
                    OnPropertyChanged(nameof(Drag));
                }
            }
        }

        private string left;

        /// <summary>
        /// Gets or sets the X coordinate of the dialog. Maps to the <c>left</c> CSS attribute.
        /// </summary>
        /// <value>The left.</value>
        public string Left
        {
            get => left;
            set
            {
                if (left != value)
                {
                    left = value;
                    OnPropertyChanged(nameof(Left));
                }
            }
        }

        private string top;

        /// <summary>
        /// Gets or sets the Y coordinate of the dialog. Maps to the <c>top</c> CSS attribute.
        /// </summary>
        /// <value>The top.</value>
        public string Top
        {
            get => top;
            set
            {
                if (top != value)
                {
                    top = value;
                    OnPropertyChanged(nameof(Top));
                }
            }
        }

        private string bottom;

        /// <summary>
        /// Specifies the <c>bottom</c> CSS attribute.
        /// </summary>
        /// <value>The bottom.</value>
        public string Bottom
        {
            get => bottom;
            set
            {
                if (bottom != value)
                {
                    bottom = value;
                    OnPropertyChanged(nameof(Bottom));
                }
            }
        }

        private RenderFragment<DialogService> childContent;

        /// <summary>
        /// Gets or sets the child content.
        /// </summary>
        /// <value>The child content.</value>
        public RenderFragment<DialogService> ChildContent
        {
            get => childContent;
            set
            {
                if (childContent != value)
                {
                    childContent = value;
                    OnPropertyChanged(nameof(ChildContent));
                }
            }
        }

        private bool autoFocusFirstElement = true;

        /// <summary>
        /// Gets or sets a value indicating whether to focus the first focusable HTML element.
        /// </summary>
        /// <value><c>true</c> if the first focusable element is focused; otherwise, <c>false</c>. Default is <c>true</c>.</value>
        public bool AutoFocusFirstElement
        {
            get => autoFocusFirstElement;
            set
            {
                if (autoFocusFirstElement != value)
                {
                    autoFocusFirstElement = value;
                    OnPropertyChanged(nameof(AutoFocusFirstElement));
                }
            }
        }

        private bool closeDialogOnEsc = true;

        /// <summary>
        /// Gets or sets a value indicating whether the dialog should be closed on ESC key press.
        /// </summary>
        /// <value><c>true</c> if closeable; otherwise, <c>false</c>. Default is <c>true</c>.</value>
        public bool CloseDialogOnEsc
        {
            get => closeDialogOnEsc;
            set
            {
                if (closeDialogOnEsc != value)
                {
                    closeDialogOnEsc = value;
                    OnPropertyChanged(nameof(CloseDialogOnEsc));
                }
            }
        }
    }

    /// <summary>
    /// Class ConfirmOptions.
    /// </summary>
    public class AlertOptions : DialogOptions
    {
        private string okButtonText;

        /// <summary>
        /// Gets or sets the text of the OK button.
        /// </summary>
        public string OkButtonText
        {
            get => okButtonText;
            set
            {
                if (okButtonText != value)
                {
                    okButtonText = value;
                    OnPropertyChanged(nameof(OkButtonText));
                }
            }
        }
    }

    /// <summary>
    /// Class ConfirmOptions.
    /// </summary>
    public class ConfirmOptions : AlertOptions
    {
        private string cancelButtonText;

        /// <summary>
        /// Gets or sets the text of the Cancel button.
        /// </summary>
        public string CancelButtonText
        {
            get => cancelButtonText;
            set
            {
                if (cancelButtonText != value)
                {
                    cancelButtonText = value;
                    OnPropertyChanged(nameof(CancelButtonText));
                }
            }
        }
    }

    /// <summary>
    /// Class Dialog.
    /// </summary>
    public class Dialog : INotifyPropertyChanged
    {
        private string title;

        /// <summary>
        /// Gets or sets the title.
        /// </summary>
        /// <value>The title.</value>
        public string Title
        {
            get => title;
            set
            {
                if (title != value)
                {
                    title = value;
                    OnPropertyChanged(nameof(Title));
                }
            }
        }

        private Type type;

        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        /// <value>The type.</value>
        public Type Type
        {
            get => type;
            set
            {
                if (type != value)
                {
                    type = value;
                    OnPropertyChanged(nameof(Type));
                }
            }
        }

        private Dictionary<string, object> parameters;

        /// <summary>
        /// Gets or sets the parameters.
        /// </summary>
        /// <value>The parameters.</value>
        public Dictionary<string, object> Parameters
        {
            get => parameters;
            set
            {
                if (parameters != value)
                {
                    parameters = value;
                    OnPropertyChanged(nameof(Parameters));
                }
            }
        }

        private DialogOptions options;

        /// <summary>
        /// Gets or sets the options.
        /// </summary>
        /// <value>The options.</value>
        public DialogOptions Options
        {
            get => options;
            set
            {
                if (options != value)
                {
                    options = value;
                    OnPropertyChanged(nameof(Options));
                }
            }
        }

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Raises the <see cref="PropertyChanged"/> event.
        /// </summary>
        /// <param name="propertyName">The name of the property that changed.</param>
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}