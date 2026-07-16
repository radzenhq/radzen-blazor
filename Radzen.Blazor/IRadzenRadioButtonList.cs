using Microsoft.AspNetCore.Components;
using Radzen.Blazor;
using System.Collections.Generic;

namespace Radzen;

/// <summary>
/// Represents the common <see cref="RadzenRadioButtonList{TValue}" /> API used by
/// its items. Injected as a cascading property in <see cref="RadzenRadioButtonListItem{TValue}" />.
/// </summary>
public interface IRadzenRadioButtonList
{
    /// <summary>
    /// Adds the specified item to the list.
    /// </summary>
    /// <param name="item">The item to add.</param>
    void AddItem(IRadzenRadioButtonListItem item);

    /// <summary>
    /// Removes the specified item from the list.
    /// </summary>
    /// <param name="item">The item.</param>
    void RemoveItem(IRadzenRadioButtonListItem item);

    /// <summary>
    /// Refreshes this instance.
    /// </summary>
    void Refresh();
}

/// <summary>
/// Represents the common <see cref="RadzenRadioButtonListItem{TValue}" /> API used by
/// <see cref="RadzenRadioButtonList{TValue}" /> regardless of the item value type. Allows items whose
/// declared value type differs from the list value type (e.g. <c>bool</c> items in a <c>bool?</c> list).
/// </summary>
public interface IRadzenRadioButtonListItem
{
    /// <summary>
    /// Gets the text.
    /// </summary>
    string? Text { get; }

    /// <summary>
    /// Gets the value.
    /// </summary>
    object? Value { get; }

    /// <summary>
    /// Gets a value indicating whether the item is disabled.
    /// </summary>
    bool Disabled { get; }

    /// <summary>
    /// Gets a value indicating whether the item is visible.
    /// </summary>
    bool Visible { get; }

    /// <summary>
    /// Gets the style.
    /// </summary>
    string? Style { get; }

    /// <summary>
    /// Gets the custom attributes rendered by the item.
    /// </summary>
    IReadOnlyDictionary<string, object>? Attributes { get; }

    /// <summary>
    /// Gets the custom attributes rendered by the input.
    /// </summary>
    IReadOnlyDictionary<string, object>? InputAttributes { get; }

    /// <summary>
    /// Gets or sets the item element reference.
    /// </summary>
    ElementReference Element { get; set; }

    /// <summary>
    /// Gets the item template.
    /// </summary>
    RenderFragment? Template { get; }

    /// <summary>
    /// Gets the item id.
    /// </summary>
    string? GetItemId();

    /// <summary>
    /// Gets the item CSS class.
    /// </summary>
    string GetItemCssClass();
}
