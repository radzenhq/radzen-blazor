using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Radzen
{
  /// <summary>
  /// Class SidePanelService. Contains method to interact with Sidepanel
  /// Should be added as scoped service in the application services.
  /// </summary>
  public class SidepanelService
  {
    /// <summary>
    /// Occurs when the sidepanel is closed.
    /// </summary>
    public Action<dynamic> OnClose;

    /// <summary>
    /// Occurs when the sidepanel is opened.
    /// </summary>
    public event Action<Type, Dictionary<string, object>, SidepanelOptions> OnOpen;

    private TaskCompletionSource<dynamic> _task = null;

    /// <summary>
    /// Closes the sidepanel when it is currently open with optional result.
    /// </summary>
    /// <param name="result">The result.,</param>
    public void Close(dynamic result = null)
    {
      if (_task != null && !_task.Task.IsCompleted)
      {
        _task.SetResult(result);
        OnClose?.Invoke(result);
      }
    }

    /// <summary>
    /// Open the Sidepanel with a component content.
    /// Closes the sidepanel with a null result if it is currently open.
    /// </summary>
    /// <typeparam name="T">The ChildContent Component.</typeparam>
    /// <param name="title">The Title of the Dialog</param>
    /// <param name="parameter">Optional parameters passed to the sidepanel component.</param>
    /// <param name="options">Optional options</param>
    /// <returns></returns>
    public Task<dynamic> OpenAsync<T>(string title, Dictionary<string, object> parameter = null, SidepanelOptions options = null) where T : ComponentBase
    {
      Close(null);
      _task = new TaskCompletionSource<dynamic>();
      if (options == null)
      {
        options = new SidepanelOptions();
      }
      options.Title = title;
      OnOpen?.Invoke(typeof(T), parameter, options);
      return _task.Task;
    }

    /// <summary>
    /// Open the Sidepanel with a component content and a expected result.
    /// Closes the sidepanel with a null result if it is currently open.
    /// </summary>
    /// <typeparam name="T">The ChildContent Component.</typeparam>
    /// <typeparam name="TResult">The expected result.</typeparam>
    /// <param name="title">The Title of the Dialog</param>
    /// <param name="parameter">Optional parameters passed to the sidepanel component.</param>
    /// <param name="options">Optional options</param>
    /// <returns></returns>
    public async Task<TResult> OpenAsync<T, TResult>(string title, Dictionary<string, object> parameter = null, SidepanelOptions options = null)
      where T : ComponentBase
      where TResult : class
    {
      dynamic result = await OpenAsync<T>(title, parameter, options);

      return result as TResult;
    }

  }

  /// <summary>
  /// Class SidepanelOptions
  /// </summary>
  public class SidepanelOptions
  {
    /// <summary>
    /// The title of the Dialog
    /// Set by the Service
    /// </summary>
    internal string Title { get; set; } = string.Empty;

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
    /// Gets or sets the width of the sidepanel.
    /// </summary>
    /// <value>The width.</value>
    public string Width { get; set; }

    /// <summary>
    /// Gets or sets the CSS style of the sidepanel
    /// </summary>
    /// <value>The style.</value>
    public string Style { get; set; }

    /// <summary>
    /// Gets or sets sidepanel box custom class
    /// </summary>
    public string CssClass { get; set; }
  }
}
