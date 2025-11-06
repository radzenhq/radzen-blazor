using System;
using System.Collections;
using Microsoft.AspNetCore.Components;
using Radzen.Blazor;

namespace Radzen;

/// <summary>
/// The configuration used by a <see cref="RadzenTreeItem" /> to create child items.
/// </summary>
public class TreeItemSettings
{
    /// <summary>
    /// Gets or sets the data from which child items will be created. The parent node enumerates the data and creates a new RadzenTreeItem for every item.
    /// </summary>
    public IEnumerable Data { get; set; }

    /// <summary>
    /// Gets or sets the function which returns a value for the <see cref="RadzenTreeItem.Text" /> of a child item.
    /// </summary>
    public Func<object, string> Text { get; set; }

    /// <summary>
    /// Gets or sets the function which returns a value for the <see cref="RadzenTreeItem.Checkable" /> of a child item.
    /// </summary>
    public Func<object, bool> Checkable { get; set; }

    /// <summary>
    /// Gets or sets the name of the property which provides the value for the <see cref="RadzenTreeItem.Text" /> of a child item.
    /// </summary>
    public string TextProperty { get; set; }

    /// <summary>
    /// Gets or sets the name of the property which provides the value for the <see cref="RadzenTreeItem.Checkable" /> of a child item.
    /// </summary>
    public string CheckableProperty { get; set; }

    /// <summary>
    /// Gets or sets a function which returns whether a child item has children of its own. Called with an item from <see cref="Data" />.
    /// By default all items are considered to have children.
    /// </summary>
    public Func<object, bool> HasChildren { get; set; } = value => true;

    /// <summary>
    /// Gets or sets a function which determines whether a child item is expanded. Called with an item from <see cref="Data" />.
    /// By default all items are collapsed.
    /// </summary>
    public Func<object, bool> Expanded { get; set; } = value => false;

    /// <summary>
    /// Gets or sets a function which determines whether a child item is selected. Called with an item from <see cref="Data" />.
    /// By default all items are not selected.
    /// </summary>
    public Func<object, bool> Selected { get; set; } = value => false;

    /// <summary>
    /// Gets or sets the <see cref="RadzenTreeItem.Template" /> of a child item.
    /// </summary>
    /// <value>The template.</value>
    public RenderFragment<RadzenTreeItem> Template { get; set; }
}

