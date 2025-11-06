namespace Radzen;

/// <summary>
/// Represents a group of data.
/// </summary>
public class Group
{
    /// <summary>
    /// Gets or sets the data.
    /// </summary>
    /// <value>The data.</value>
    public GroupResult Data { get; set; }

    /// <summary>
    /// Gets or sets the group descriptor.
    /// </summary>
    /// <value>The group descriptor.</value>
    public GroupDescriptor GroupDescriptor { get; set; }

    /// <summary>
    /// Gets or sets the level.
    /// </summary>
    /// <value>The level.</value>
    public int Level { get; set; }
}

