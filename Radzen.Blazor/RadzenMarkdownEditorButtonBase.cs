using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Radzen.Blazor;

/// <summary>
/// Base class for <see cref="RadzenMarkdownEditor" /> toolbar tools.
/// </summary>
public abstract class RadzenMarkdownEditorButtonBase : ComponentBase, IDisposable
{
    [Inject]
    private IServiceProvider Services { get; set; } = default!;

    private Localizer? localizer;

    private Localizer Localizer => localizer ??= Services.GetService<Localizer>() ?? Localizer.Default;

    /// <summary>
    /// Returns the localized string for <paramref name="key" /> using the editor's UI culture.
    /// </summary>
    protected string Localize(string key) => Localizer.Get(key, Editor?.UICulture ?? CultureInfo.CurrentUICulture);

    /// <summary>
    /// The <see cref="RadzenMarkdownEditor" /> this tool belongs to.
    /// </summary>
    [CascadingParameter]
    public RadzenMarkdownEditor? Editor { get; set; }

    /// <summary>
    /// The command this tool executes. One of <see cref="MarkdownEditorCommands" />.
    /// </summary>
    protected virtual string? CommandName { get; }

    /// <summary>
    /// The keyboard shortcut of this tool, e.g. <c>Ctrl+B</c>. Only Ctrl/Cmd combinations are supported, e.g. <c>Ctrl+B</c> (Cmd on macOS).
    /// </summary>
    [Parameter]
    public virtual string? Shortcut { get; set; }

    /// <summary>
    /// Executes <see cref="CommandName" /> on the editor.
    /// </summary>
    protected virtual async Task OnClick()
    {
        if (Editor != null && CommandName != null)
        {
            await Editor.ExecuteCommandAsync(CommandName);
        }
    }

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        if (!string.IsNullOrEmpty(Shortcut))
        {
            Editor?.RegisterShortcut(Shortcut, OnClick);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (!string.IsNullOrEmpty(Shortcut))
        {
            Editor?.UnregisterShortcut(Shortcut);
        }

        GC.SuppressFinalize(this);
    }
}
