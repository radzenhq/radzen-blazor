namespace Radzen;

/// <summary>
/// Represents a grouping description. Used in components that support grouping.
/// </summary>
public class GroupDescriptor
{
    /// <summary>
    /// Gets or sets the property to group by.
    /// </summary>
    /// <value>The property.</value>
    public string Property { get; set; }

    /// <summary>
    /// Gets or sets the sort order.
    /// </summary>
    /// <value>The sort order.</value>
    public SortOrder? SortOrder { get; set; }

    /// <summary>
    /// Gets or sets the title displayed in the group.
    /// </summary>
    /// <value>The title.</value>
    public string Title { get; set; }

    /// <summary>
    /// Gets or sets the format string used to display the key in the group.
    /// </summary>
    /// <value>The format string.</value>
    public string FormatString { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to show the footer for the group.
    /// </summary>
    /// <value><c>true</c> if the footer should be shown; otherwise, <c>false</c>.</value>
    public bool? ShowFooter { get; set; }

    /// <summary>
    /// Gets the title of the group.
    /// </summary>
    /// <returns>System.String.</returns>
    public string GetTitle()
    {
        return !string.IsNullOrEmpty(Title) ? Title : Property;
    }
}

