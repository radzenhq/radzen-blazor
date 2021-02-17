using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Radzen
{
    public class DialogService : IDisposable
    {
        NavigationManager UriHelper { get; set; }

        public DialogService(NavigationManager uriHelper)
        {
            UriHelper = uriHelper;

            if (UriHelper != null)
            {
                UriHelper.LocationChanged += UriHelper_OnLocationChanged;
            }
        }

        private void UriHelper_OnLocationChanged(object sender, Microsoft.AspNetCore.Components.Routing.LocationChangedEventArgs e)
        {
            if (dialogs.Count > 0)
            {
                this.Close();
            }
        }

        public event Action<dynamic> OnClose;

        public event Action OnRefresh;

        public event Action<string, Type, Dictionary<string, object>, DialogOptions> OnOpen;

        public void Open<T>(string title, Dictionary<string, object> parameters = null, DialogOptions options = null) where T : ComponentBase
        {
            OpenDialog<T>(title, parameters, options);
        }

        public void Refresh()
        {
            OnRefresh?.Invoke();
        }

        protected List<TaskCompletionSource<dynamic>> tasks = new List<TaskCompletionSource<dynamic>>();

        public Task<dynamic> OpenAsync<T>(string title, Dictionary<string, object> parameters = null, DialogOptions options = null) where T : ComponentBase
        {
            var task = new TaskCompletionSource<dynamic>();
            tasks.Add(task);

            OpenDialog<T>(title, parameters, options);

            return task.Task;
        }
        public Task<dynamic> OpenAsync(string title, RenderFragment<DialogService> childContent, DialogOptions options = null)
        {
            var task = new TaskCompletionSource<dynamic>();
            tasks.Add(task);

            options = options ?? new DialogOptions();

            options.ChildContent = childContent;

            OpenDialog<object>(title, null, options);

            return task.Task;
        }

        public void Open(string title, RenderFragment<DialogService> childContent, DialogOptions options = null)
        {
            options = options ?? new DialogOptions();

            options.ChildContent = childContent;

            OpenDialog<object>(title, null, options);
        }

        protected List<object> dialogs = new List<object>();
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
                ChildContent = options?.ChildContent
            });
        }

        public void Close(dynamic result = null)
        {
            OnClose?.Invoke(result);

            var dialog = dialogs.LastOrDefault();
            if (dialog != null)
            {
                dialogs.Remove(dialog);
            }

            var task = tasks.LastOrDefault();
            if (task != null && task.Task != null && !task.Task.IsCompleted)
            {
                task.SetResult(result);
                tasks.Remove(task);
            }
        }

        public void Dispose()
        {
            UriHelper.LocationChanged -= UriHelper_OnLocationChanged;
        }

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
            ShowClose = options != null ? options.ShowClose : true
        });
    }

    public class DialogOptions
    {
        public bool ShowTitle { get; set; } = true;
        public bool ShowClose { get; set; } = true;
        public string Left { get; set; }
        public string Top { get; set; }
        public string Bottom { get; set; }
        public string Width { get; set; }
        public string Height { get; set; }
        public RenderFragment<DialogService> ChildContent { get; set; }
    }

    public class ConfirmOptions : DialogOptions
    {
        public string OkButtonText { get; set; }
        public string CancelButtonText { get; set; }
    }

    public class Dialog
    {
        public string Title { get; set; }
        public Type Type { get; set; }
        public Dictionary<string, object> Parameters { get; set; }
        public DialogOptions Options { get; set; }
    }
}
