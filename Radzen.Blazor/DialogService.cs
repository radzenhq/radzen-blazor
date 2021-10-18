using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Radzen
{
    /// <summary>
    /// Class DialogService. Contains variuos methods with options to open and close dialogs. 
    /// Should be added as scoped service in the application services and RadzenDialog should be added in application main layout.
    /// Implements the <see cref="IDisposable" />
    /// </summary>
    /// <seealso cref="IDisposable" />
    /// <example>
    /// <code>
    /// @inject DialogService DialogService
    /// &lt;RadzenButton Text="Show dialog with inline Blazor content" Click=@ShowInlineDialog /&gt;
    /// @code {
    ///  async Task ShowInlineDialog()
    ///  {
    ///    var result = await DialogService.OpenAsync("Simple Dialog", ds =>
    ///      @<div>
    ///          <p Style="margin-bottom: 1rem">Confirm?</p>
    ///          <div class="row">
    ///              <div class="col-md-12">
    ///                  <RadzenButton Text="Ok" Click="() => ds.Close(true)" Style="margin-bottom: 10px; width: 150px" />
    ///                  <RadzenButton Text="Cancel" Click="() => ds.Close(false)" ButtonStyle="ButtonStyle.Secondary"  Style="margin-bottom: 10px; width: 150px"/>
    ///                  <RadzenButton Text="Refresh" Click="(() => { orderID = 10249; ds.Refresh(); })" ButtonStyle="ButtonStyle.Info"  Style="margin-bottom: 10px; width: 150px"/>
    ///                  Order ID: @orderID
    ///              </div>
    ///          </div>
    ///      </div>);
    ///    Console.WriteLine($"Dialog result: {result}");
    ///  }
    /// }
    /// </code>
    /// </example>
    public class DialogService : IDisposable
    {
        /// <summary>
        /// Gets or sets the URI helper.
        /// </summary>
        /// <value>The URI helper.</value>
        NavigationManager UriHelper { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DialogService"/> class.
        /// </summary>
        /// <param name="uriHelper">The URI helper.</param>
        public DialogService(NavigationManager uriHelper)
        {
            UriHelper = uriHelper;

            if (UriHelper != null)
            {
                UriHelper.LocationChanged += UriHelper_OnLocationChanged;
            }
        }

        /// <summary>
        /// Handles the OnLocationChanged event of the UriHelper control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="Microsoft.AspNetCore.Components.Routing.LocationChangedEventArgs"/> instance containing the event data.</param>
        private void UriHelper_OnLocationChanged(object sender, Microsoft.AspNetCore.Components.Routing.LocationChangedEventArgs e)
        {
            if (dialogs.Count > 0)
            {
                this.Close();
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
        /// Occurs when [on open].
        /// </summary>
        public event Action<string, Type, Dictionary<string, object>, DialogOptions> OnOpen;

        /// <summary>
        /// Opens the specified title.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="title">The title.</param>
        /// <param name="parameters">The parameters.</param>
        /// <param name="options">The options.</param>
        public void Open<T>(string title, Dictionary<string, object> parameters = null, DialogOptions options = null) where T : ComponentBase
        {
            OpenDialog<T>(title, parameters, options);
        }

        /// <summary>
        /// Refreshes this instance.
        /// </summary>
        public void Refresh()
        {
            OnRefresh?.Invoke();
        }

        /// <summary>
        /// The tasks
        /// </summary>
        protected List<TaskCompletionSource<dynamic>> tasks = new List<TaskCompletionSource<dynamic>>();

        /// <summary>
        /// Opens the asynchronous.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="title">The title.</param>
        /// <param name="parameters">The parameters.</param>
        /// <param name="options">The options.</param>
        /// <returns>Task&lt;dynamic&gt;.</returns>
        public Task<dynamic> OpenAsync<T>(string title, Dictionary<string, object> parameters = null, DialogOptions options = null) where T : ComponentBase
        {
            var task = new TaskCompletionSource<dynamic>();
            tasks.Add(task);

            OpenDialog<T>(title, parameters, options);

            return task.Task;
        }
        /// <summary>
        /// Opens the asynchronous.
        /// </summary>
        /// <param name="title">The title.</param>
        /// <param name="childContent">Content of the child.</param>
        /// <param name="options">The options.</param>
        /// <returns>Task&lt;dynamic&gt;.</returns>
        public Task<dynamic> OpenAsync(string title, RenderFragment<DialogService> childContent, DialogOptions options = null)
        {
            var task = new TaskCompletionSource<dynamic>();
            tasks.Add(task);

            options = options ?? new DialogOptions();

            options.ChildContent = childContent;

            OpenDialog<object>(title, null, options);

            return task.Task;
        }

        /// <summary>
        /// Opens the specified title.
        /// </summary>
        /// <param name="title">The title.</param>
        /// <param name="childContent">Content of the child.</param>
        /// <param name="options">The options.</param>
        public void Open(string title, RenderFragment<DialogService> childContent, DialogOptions options = null)
        {
            options = options ?? new DialogOptions();

            options.ChildContent = childContent;

            OpenDialog<object>(title, null, options);
        }

        /// <summary>
        /// The dialogs
        /// </summary>
        protected List<object> dialogs = new List<object>();
        /// <summary>
        /// Opens the dialog.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="title">The title.</param>
        /// <param name="parameters">The parameters.</param>
        /// <param name="options">The options.</param>
        private void OpenDialog<T>(string title, Dictionary<string, object> parameters, DialogOptions options)
        {
            dialogs.Add(new object());
            OnOpen?.Invoke(title, typeof(T), parameters, new DialogOptions()
            {
                Width = options != null && !string.IsNullOrEmpty(options.Width) ? options.Width : "600px",
                Left = options != null && !string.IsNullOrEmpty(options.Left) ? options.Left : "",
                Top = options != null && !string.IsNullOrEmpty(options.Top) ? options.Top : "",
                Height = options != null && !string.IsNullOrEmpty(options.Height) ? options.Height : "",
                ShowTitle = options != null ? options.ShowTitle : true,
                ShowClose = options != null ? options.ShowClose : true,
                Resizable = options != null ? options.Resizable : false,
                Draggable = options != null ? options.Draggable : false,
                ChildContent = options?.ChildContent,
                Style = options != null ? options.Style : "",
                AutoFocusFirstElement = options != null ? options.AutoFocusFirstElement : true,
                CloseDialogOnOverlayClick = options != null ? options.CloseDialogOnOverlayClick : false,
            });
        }

        /// <summary>
        /// Closes the specified result.
        /// </summary>
        /// <param name="result">The result.</param>
        public void Close(dynamic result = null)
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

        /// <summary>
        /// Disposes this instance.
        /// </summary>
        public void Dispose()
        {
            UriHelper.LocationChanged -= UriHelper_OnLocationChanged;
        }

        /// <summary>
        /// Confirms the specified message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="title">The title.</param>
        /// <param name="options">The options.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        public async Task<bool?> Confirm(string message = "Confirm?", string title = "Confirm", ConfirmOptions options = null) => await OpenAsync(title, ds => {
            RenderFragment content = b =>
            {
                var i = 0;
                b.OpenElement(i++, "div");
                b.OpenElement(i++, "p");
                b.AddAttribute(i++, "style", "margin-bottom: 20px;");
                b.AddContent(i++, message);
                b.CloseElement();

                b.OpenElement(i++, "div");
                b.AddAttribute(i++, "class", "row");
                b.OpenElement(i++, "div");
                b.AddAttribute(i++, "class", "col-md-12");

                b.OpenComponent<Blazor.RadzenButton>(i++);
                b.AddAttribute(i++, "Text", options != null ? options.OkButtonText : "Ok");
                b.AddAttribute(i++, "Style", "margin-bottom: 10px; width: 150px");
                b.AddAttribute(i++, "Click", EventCallback.Factory.Create<Microsoft.AspNetCore.Components.Web.MouseEventArgs>(this, () => ds.Close(true)));
                b.CloseComponent();

                b.OpenComponent<Blazor.RadzenButton>(i++);
                b.AddAttribute(i++, "Text", options != null ? options.CancelButtonText : "Cancel");
                b.AddAttribute(i++, "ButtonStyle", ButtonStyle.Secondary);
                b.AddAttribute(i++, "Style", "margin-bottom: 10px; margin-left: 10px; width: 150px");
                b.AddAttribute(i++, "Click", EventCallback.Factory.Create<Microsoft.AspNetCore.Components.Web.MouseEventArgs>(this, () => ds.Close(false)));
                b.CloseComponent();
                
                b.CloseElement();
                b.CloseElement();
                b.CloseElement();
            };
            return content;
        }, new DialogOptions() 
        {
            Width = options != null ? !string.IsNullOrEmpty(options.Width) ? options.Width : "355px" : "355px",
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
        });
    }

    /// <summary>
    /// Class DialogOptions.
    /// </summary>
    public class DialogOptions
    {
        /// <summary>
        /// Gets or sets a value indicating whether [show title].
        /// </summary>
        /// <value><c>true</c> if [show title]; otherwise, <c>false</c>.</value>
        public bool ShowTitle { get; set; } = true;
        /// <summary>
        /// Gets or sets a value indicating whether [show close].
        /// </summary>
        /// <value><c>true</c> if [show close]; otherwise, <c>false</c>.</value>
        public bool ShowClose { get; set; } = true;
        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="DialogOptions"/> is resizable.
        /// </summary>
        /// <value><c>true</c> if resizable; otherwise, <c>false</c>.</value>
        public bool Resizable { get; set; } = false;
        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="DialogOptions"/> is draggable.
        /// </summary>
        /// <value><c>true</c> if draggable; otherwise, <c>false</c>.</value>
        public bool Draggable { get; set; } = false;
        /// <summary>
        /// Gets or sets the left.
        /// </summary>
        /// <value>The left.</value>
        public string Left { get; set; }
        /// <summary>
        /// Gets or sets the top.
        /// </summary>
        /// <value>The top.</value>
        public string Top { get; set; }
        /// <summary>
        /// Gets or sets the bottom.
        /// </summary>
        /// <value>The bottom.</value>
        public string Bottom { get; set; }
        /// <summary>
        /// Gets or sets the width.
        /// </summary>
        /// <value>The width.</value>
        public string Width { get; set; }
        /// <summary>
        /// Gets or sets the height.
        /// </summary>
        /// <value>The height.</value>
        public string Height { get; set; }
        /// <summary>
        /// Gets or sets the style.
        /// </summary>
        /// <value>The style.</value>
        public string Style { get; set; }
        /// <summary>
        /// Gets or sets the child content.
        /// </summary>
        /// <value>The child content.</value>
        public RenderFragment<DialogService> ChildContent { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether [automatic focus first element].
        /// </summary>
        /// <value><c>true</c> if [automatic focus first element]; otherwise, <c>false</c>.</value>
        public bool AutoFocusFirstElement { get; set; } = true;
        /// <summary>
        /// Gets or sets a value indicating whether the dialog should be closed by clicking the overlay.
        /// </summary>
        /// <value><c>true</c> if closeable; otherwise, <c>false</c>.</value>
        public bool CloseDialogOnOverlayClick { get; set; } = false;
    }

    /// <summary>
    /// Class ConfirmOptions.
    /// Implements the <see cref="Radzen.DialogOptions" />
    /// </summary>
    /// <seealso cref="Radzen.DialogOptions" />
    public class ConfirmOptions : DialogOptions
    {
        /// <summary>
        /// Gets or sets the ok button text.
        /// </summary>
        /// <value>The ok button text.</value>
        public string OkButtonText { get; set; }
        /// <summary>
        /// Gets or sets the cancel button text.
        /// </summary>
        /// <value>The cancel button text.</value>
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
